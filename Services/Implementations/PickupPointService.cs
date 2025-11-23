using AutoMapper;
using Data.Models;
using Data.Models.Enums;
using Data.Repos.Interfaces;
using Microsoft.EntityFrameworkCore;
using Services.Contracts;
using Services.Models.PickupPoint;

namespace Services.Implementations
{
	public class PickupPointService : IPickupPointService
	{
		private readonly IPickupPointRepository _pickupPointRepository;
		private readonly IStudentRepository _studentRepository;
		private readonly IMongoRepository<Route> _routeRepository;
		private readonly IStudentPickupPointRepository _studentPickupPointRepository;
		private readonly IMongoRepository<PickupPointResetLog> _resetLogRepository;
		private readonly IMapper _mapper;

		public PickupPointService(
			IPickupPointRepository pickupPointRepository,
			IStudentRepository studentRepository,
			IMongoRepository<Route> routeRepository,
			IStudentPickupPointRepository studentPickupPointRepository,
			IMongoRepository<PickupPointResetLog> resetLogRepository,
			IMapper mapper)
		{
			_pickupPointRepository = pickupPointRepository;
			_studentRepository = studentRepository;
			_routeRepository = routeRepository;
			_studentPickupPointRepository = studentPickupPointRepository;
			_resetLogRepository = resetLogRepository;
			_mapper = mapper;
		}

		public async Task<PickupPointsResponse> GetUnassignedPickupPointsAsync()
		{
			// Get all active routes to find assigned pickup points
			var activeRoutes = await _routeRepository.FindByConditionAsync(r =>
				!r.IsDeleted && r.IsActive);

			// Get all pickup point IDs that are assigned to routes
			var assignedPickupPointIds = activeRoutes
				.SelectMany(r => r.PickupPoints)
				.Select(pp => pp.PickupPointId)
				.Distinct()
				.ToHashSet();

			// Get all pickup points that are NOT assigned to any route
			var unassignedPickupPoints = await _pickupPointRepository.FindByConditionAsync(pp =>
				!pp.IsDeleted && !assignedPickupPointIds.Contains(pp.Id));

			// Get student counts for each unassigned pickup point
			var pickupPointIds = unassignedPickupPoints.Select(pp => pp.Id).ToList();
			var studentCounts = await _studentRepository.FindByConditionAsync(s =>
				pickupPointIds.Contains(s.CurrentPickupPointId ?? Guid.Empty) &&
				s.Status == StudentStatus.Active &&
				!s.IsDeleted);

			// Group students by pickup point ID and count them
			var studentCountsByPickupPoint = studentCounts
				.Where(s => s.CurrentPickupPointId.HasValue)
				.GroupBy(s => s.CurrentPickupPointId!.Value)
				.ToDictionary(g => g.Key, g => g.Count());

			// Map to DTOs with student counts
			var pickupPointDtos = unassignedPickupPoints.Select(pp => new PickupPointDto
			{
				Id = pp.Id,
				Description = pp.Description,
				Location = pp.Location,
				Latitude = pp.Geog.Y, // NetTopologySuite uses Y for latitude
				Longitude = pp.Geog.X, // NetTopologySuite uses X for longitude
				StudentCount = studentCountsByPickupPoint.GetValueOrDefault(pp.Id, 0),
				CreatedAt = pp.CreatedAt,
				UpdatedAt = pp.UpdatedAt
			}).ToList();

			return new PickupPointsResponse
			{
				PickupPoints = pickupPointDtos,
				TotalCount = pickupPointDtos.Count
			};
		}

		/// <summary>
		/// Admin creates a pickup point directly (auto-approved, no approval workflow needed)
		/// </summary>
		public async Task<AdminCreatePickupPointResponse> AdminCreatePickupPointAsync(
			AdminCreatePickupPointRequest request, Guid adminId)
		{
			if (request == null)
				throw new ArgumentNullException(nameof(request));

			// Validate students exist and belong to the parent
			var students = new List<Student>();
			foreach (var studentId in request.StudentIds)
			{
				var student = await _studentRepository.FindAsync(studentId);
				if (student == null)
					throw new KeyNotFoundException($"Student with ID {studentId} not found");

				if (student.ParentId != request.ParentId)
					throw new InvalidOperationException(
						$"Student {student.FirstName} {student.LastName} does not belong to the specified parent");

				students.Add(student);
			}

			// Create pickup point using NetTopologySuite Point
			var geometryFactory = NetTopologySuite.NtsGeometryServices.Instance.CreateGeometryFactory(srid: 4326);
			var location = geometryFactory.CreatePoint(new NetTopologySuite.Geometries.Coordinate(
				request.Longitude, // X = Longitude
				request.Latitude   // Y = Latitude
			));

			var pickupPoint = new PickupPoint
			{
				Description = request.Description ?? $"Pickup point for parent at {request.AddressText}",
				Location = request.AddressText,
				Geog = location,
				CreatedAt = DateTime.UtcNow
			};

			var createdPickupPoint = await _pickupPointRepository.AddAsync(pickupPoint);

			// Create StudentPickupPoint records for each student
			// Note: This does NOT assign to CurrentPickupPointId - that will be done via ResetPickupPointBySemester API
			var now = DateTime.UtcNow;
			foreach (var student in students)
			{
				// Create StudentPickupPoint record
				// Note: Semester information should be provided via ResetPickupPointBySemester API
				// For now, we create a record without semester info (can be updated later)
				await _studentPickupPointRepository.AddAsync(new StudentPickupPoint
				{
					StudentId = student.Id,
					PickupPointId = createdPickupPoint.Id,
					AssignedAt = now,
					ChangeReason = "Created by admin",
					ChangedBy = $"Admin:{adminId}",
					// Semester information will be set when admin resets pickup point by semester
					SemesterName = string.Empty,
					SemesterCode = string.Empty,
					AcademicYear = string.Empty,
					SemesterStartDate = DateTime.MinValue,
					SemesterEndDate = DateTime.MinValue,
					TotalSchoolDays = 0
				});
			}

			return new AdminCreatePickupPointResponse
			{
				Id = createdPickupPoint.Id,
				ParentId = request.ParentId,
				AddressText = request.AddressText,
				Latitude = request.Latitude,
				Longitude = request.Longitude,
				DistanceKm = request.DistanceKm,
				Description = createdPickupPoint.Description,
				CreatedAt = createdPickupPoint.CreatedAt,
				AssignedStudentIds = request.StudentIds,
				Message = $"Pickup point created successfully and {students.Count} student(s) assigned"
			};
		}

		/// <summary>
		/// Reset pickup points by semester - Admin selects semester (e.g., S1 2025-2026) with start/end dates
		/// System finds all StudentPickupPoint records matching the semester criteria
		/// and assigns PickupPointId to CurrentPickupPointId in Student table
		/// </summary>
		public async Task<ResetPickupPointBySemesterResponse> ResetPickupPointBySemesterAsync(
			ResetPickupPointBySemesterRequest request, Guid adminId)
		{
			if (request == null)
				throw new ArgumentNullException(nameof(request));

			// Validate date range
			if (request.SemesterStartDate >= request.SemesterEndDate)
				throw new ArgumentException("Semester start date must be before end date");

			// Find all StudentPickupPoint records matching the semester criteria
			// Match by SemesterCode, AcademicYear, and date range
			var matchingRecords = await _studentPickupPointRepository.FindByConditionAsync(spp =>
				spp.SemesterCode == request.SemesterCode &&
				spp.AcademicYear == request.AcademicYear &&
				spp.SemesterStartDate.Date == request.SemesterStartDate.Date &&
				spp.SemesterEndDate.Date == request.SemesterEndDate.Date &&
				!spp.IsDeleted);

			// If SemesterName is provided, also filter by it
			if (!string.IsNullOrWhiteSpace(request.SemesterName))
			{
				matchingRecords = matchingRecords.Where(spp => 
					spp.SemesterName == request.SemesterName).ToList();
			}

			var totalRecordsFound = matchingRecords.Count();
			
			// Validate: If no records found, throw error
			if (totalRecordsFound == 0)
			{
				throw new ArgumentException(
					$"No semester records found with SemesterCode: '{request.SemesterCode}', " +
					$"AcademicYear: '{request.AcademicYear}', " +
					$"SemesterStartDate: '{request.SemesterStartDate:yyyy-MM-dd}', " +
					$"SemesterEndDate: '{request.SemesterEndDate:yyyy-MM-dd}'. " +
					$"Please verify the semester information.");
			}
			
			var updatedStudentIds = new List<Guid>();
			var failedStudentIds = new List<StudentUpdateFailure>();
			var now = DateTime.UtcNow;

			// Group by StudentId to get the most recent assignment for each student
			// (in case there are multiple records for the same student)
			var recordsByStudent = matchingRecords
				.GroupBy(spp => spp.StudentId)
				.ToDictionary(g => g.Key, g => g.OrderByDescending(spp => spp.AssignedAt).First());

			// Update each student's CurrentPickupPointId
			foreach (var kvp in recordsByStudent)
			{
				var studentId = kvp.Key;
				var studentPickupPoint = kvp.Value;

				try
				{
					var student = await _studentRepository.FindAsync(studentId);
					if (student == null)
					{
						failedStudentIds.Add(new StudentUpdateFailure
						{
							StudentId = studentId,
							Reason = "Student not found"
						});
						continue;
					}

					if (student.IsDeleted)
					{
						failedStudentIds.Add(new StudentUpdateFailure
						{
							StudentId = studentId,
							Reason = "Student is deleted"
						});
						continue;
					}

					// Update student's CurrentPickupPointId
					student.CurrentPickupPointId = studentPickupPoint.PickupPointId;
					student.PickupPointAssignedAt = now;
					await _studentRepository.UpdateAsync(student);

					updatedStudentIds.Add(studentId);
				}
				catch (Exception ex)
				{
					failedStudentIds.Add(new StudentUpdateFailure
					{
						StudentId = studentId,
						Reason = $"Error updating student: {ex.Message}"
					});
				}
			}

			var response = new ResetPickupPointBySemesterResponse
			{
				SemesterCode = request.SemesterCode,
				AcademicYear = request.AcademicYear,
				SemesterStartDate = request.SemesterStartDate,
				SemesterEndDate = request.SemesterEndDate,
				TotalRecordsFound = totalRecordsFound,
				StudentsUpdated = updatedStudentIds.Count,
				StudentsFailed = failedStudentIds.Count,
				UpdatedStudentIds = updatedStudentIds,
				FailedStudentIds = failedStudentIds,
				Message = $"Reset pickup points for semester {request.SemesterCode} {request.AcademicYear}. " +
						  $"Updated {updatedStudentIds.Count} student(s), " +
						  $"Failed {failedStudentIds.Count} student(s)."
			};

			// Log the reset operation to MongoDB
			try
			{
				var resetLog = new PickupPointResetLog
				{
					AdminId = adminId,
					SemesterCode = request.SemesterCode,
					AcademicYear = request.AcademicYear,
					SemesterName = request.SemesterName,
					SemesterStartDate = request.SemesterStartDate,
					SemesterEndDate = request.SemesterEndDate,
					TotalRecordsFound = totalRecordsFound,
					StudentsUpdated = updatedStudentIds.Count,
					StudentsFailed = failedStudentIds.Count,
					UpdatedStudentIds = updatedStudentIds,
					FailedStudentIds = failedStudentIds.Select(f => new StudentUpdateFailureLog
					{
						StudentId = f.StudentId,
						Reason = f.Reason
					}).ToList(),
					ResetAt = now,
					Status = failedStudentIds.Count == 0 ? "Completed" : 
							 (updatedStudentIds.Count > 0 ? "Partial" : "Failed"),
					Message = response.Message
				};

				await _resetLogRepository.AddAsync(resetLog);
			}
			catch (Exception ex)
			{
				// Log error but don't fail the operation
				// In production, you might want to use a proper logging framework
				System.Diagnostics.Debug.WriteLine($"Failed to log reset operation to MongoDB: {ex.Message}");
			}

			return response;
		}

		/// <summary>
		/// Get all pickup points with their assigned students by semester
		/// Returns pickup points from StudentPickupPoint table filtered by semester criteria
		/// </summary>
		public async Task<GetPickupPointsBySemesterResponse> GetPickupPointsBySemesterAsync(
			GetPickupPointsBySemesterRequest request)
		{
			if (request == null)
				throw new ArgumentNullException(nameof(request));

			// Find all StudentPickupPoint records matching the semester criteria
			var matchingRecords = await _studentPickupPointRepository.FindByConditionAsync(spp =>
				spp.SemesterCode == request.SemesterCode &&
				spp.AcademicYear == request.AcademicYear &&
				spp.SemesterStartDate.Date == request.SemesterStartDate.Date &&
				spp.SemesterEndDate.Date == request.SemesterEndDate.Date &&
				!spp.IsDeleted);

			// If SemesterName is provided, also filter by it
			if (!string.IsNullOrWhiteSpace(request.SemesterName))
			{
				matchingRecords = matchingRecords.Where(spp =>
					spp.SemesterName == request.SemesterName).ToList();
			}

			// Get all unique pickup point IDs
			var pickupPointIds = matchingRecords.Select(spp => spp.PickupPointId).Distinct().ToList();

			if (pickupPointIds.Count == 0)
			{
				return new GetPickupPointsBySemesterResponse
				{
					SemesterCode = request.SemesterCode,
					AcademicYear = request.AcademicYear,
					SemesterStartDate = request.SemesterStartDate,
					SemesterEndDate = request.SemesterEndDate,
					SemesterName = request.SemesterName,
					PickupPoints = new List<PickupPointWithStudentsDto>(),
					TotalPickupPoints = 0,
					TotalStudents = 0
				};
			}

			// Get all pickup points
			var pickupPoints = await _pickupPointRepository.FindByConditionAsync(pp =>
				pickupPointIds.Contains(pp.Id) && !pp.IsDeleted);

			// Get all student IDs
			var studentIds = matchingRecords.Select(spp => spp.StudentId).Distinct().ToList();
			var students = await _studentRepository.FindByConditionAsync(s =>
				studentIds.Contains(s.Id) && !s.IsDeleted);

			// Create a dictionary for quick student lookup
			var studentsDict = students.ToDictionary(s => s.Id);

			// Group StudentPickupPoint records by PickupPointId
			var recordsByPickupPoint = matchingRecords
				.GroupBy(spp => spp.PickupPointId)
				.ToDictionary(g => g.Key, g => g.ToList());

			// Build the response
			var pickupPointsWithStudents = new List<PickupPointWithStudentsDto>();

			foreach (var pickupPoint in pickupPoints)
			{
				if (!recordsByPickupPoint.TryGetValue(pickupPoint.Id, out var records))
					continue;

				var studentAssignments = new List<StudentAssignmentDto>();

				foreach (var record in records)
				{
					if (!studentsDict.TryGetValue(record.StudentId, out var student))
						continue;

					studentAssignments.Add(new StudentAssignmentDto
					{
						StudentId = student.Id,
						FirstName = student.FirstName,
						LastName = student.LastName,
						ParentId = student.ParentId,
						ParentEmail = student.ParentEmail,
						AssignedAt = record.AssignedAt,
						ChangeReason = record.ChangeReason,
						ChangedBy = record.ChangedBy
					});
				}

				pickupPointsWithStudents.Add(new PickupPointWithStudentsDto
				{
					PickupPointId = pickupPoint.Id,
					Description = pickupPoint.Description,
					Location = pickupPoint.Location,
					Latitude = pickupPoint.Geog.Y, // NetTopologySuite uses Y for latitude
					Longitude = pickupPoint.Geog.X, // NetTopologySuite uses X for longitude
					CreatedAt = pickupPoint.CreatedAt,
					UpdatedAt = pickupPoint.UpdatedAt,
					Students = studentAssignments,
					StudentCount = studentAssignments.Count
				});
			}

			var totalStudents = pickupPointsWithStudents.Sum(pp => pp.StudentCount);

			return new GetPickupPointsBySemesterResponse
			{
				SemesterCode = request.SemesterCode,
				AcademicYear = request.AcademicYear,
				SemesterStartDate = request.SemesterStartDate,
				SemesterEndDate = request.SemesterEndDate,
				SemesterName = request.SemesterName,
				PickupPoints = pickupPointsWithStudents,
				TotalPickupPoints = pickupPointsWithStudents.Count,
				TotalStudents = totalStudents
			};
		}

		/// <summary>
		/// Get all available semesters from StudentPickupPoint table
		/// Returns distinct semesters that have pickup point assignments
		/// </summary>
		public async Task<GetAvailableSemestersResponse> GetAvailableSemestersAsync()
		{
			// Get all non-deleted StudentPickupPoint records
			var allRecords = await _studentPickupPointRepository.FindByConditionAsync(spp => !spp.IsDeleted);

			// Group by semester criteria to get distinct semesters
			var semesterGroups = allRecords
				.GroupBy(spp => new
				{
					spp.SemesterCode,
					spp.AcademicYear,
					SemesterStartDate = spp.SemesterStartDate.Date,
					SemesterEndDate = spp.SemesterEndDate.Date,
					spp.SemesterName
				})
				.Select(g => new AvailableSemesterDto
				{
					SemesterCode = g.Key.SemesterCode,
					AcademicYear = g.Key.AcademicYear,
					SemesterStartDate = g.Key.SemesterStartDate,
					SemesterEndDate = g.Key.SemesterEndDate,
					SemesterName = g.Key.SemesterName,
					StudentCount = g.Select(spp => spp.StudentId).Distinct().Count()
				})
				.OrderByDescending(s => s.AcademicYear)
				.ThenByDescending(s => s.SemesterCode)
				.ThenByDescending(s => s.SemesterStartDate)
				.ToList();

			return new GetAvailableSemestersResponse
			{
				Semesters = semesterGroups,
				TotalCount = semesterGroups.Count
			};
		}
	}
}