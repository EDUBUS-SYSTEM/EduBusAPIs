using DocumentFormat.OpenXml.Office2010.Excel;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Services.Contracts;
using Services.Implementations;
using Services.Models.Student;
using Services.Models.StudentGrade;

namespace APIs.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class StudentGradeController : ControllerBase
    {
        private readonly IStudentGradeService _studentGradeService;
        public StudentGradeController(IStudentGradeService studentGradeService)
        {
            _studentGradeService = studentGradeService;
        }
        [HttpPost]
        public async Task<IActionResult> CreateStudentGrade([FromBody] CreateStudentGradeRequest request)
        {
            try
            {
                var studentGrade = await _studentGradeService.CreateStudentGradeAsync(request);
                return Ok(new { newStudentGrade = studentGrade });
            }
            catch (ArgumentException ex)
            {
                return BadRequest(new { message = ex.Message });
            }
            catch (KeyNotFoundException ex)
            {
                return NotFound(new { message = ex.Message });
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { message = "An unexpected error occurred.", detail = ex.Message });
            }
            
        }

        [HttpPut]
        public async Task<IActionResult> UpdateStudentGrade([FromBody] UpdateStudentGradeResponse request)
        {
            try
            {
                var result = await _studentGradeService.UpdateStudentGradeAsync(request);
                return Ok(result);
            }
            catch (ArgumentException ex)
            {
                return BadRequest(new { message = ex.Message });
            }
            catch (KeyNotFoundException ex)
            {
                return NotFound(new { message = ex.Message });
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { message = "An unexpected error occurred.", detail = ex.Message });
            }
        }
        [HttpGet]
        public async Task<IActionResult> GetAllStudentGrades()
        {
            var students = await _studentGradeService.GetAllStudentGradesAsync();
            return Ok(students);
        }
    }
}
