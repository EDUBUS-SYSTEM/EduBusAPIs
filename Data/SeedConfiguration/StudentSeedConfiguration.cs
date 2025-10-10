using Data.Models;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Data.SeedConfiguration
{
    public class StudentSeedConfiguration : IEntityTypeConfiguration<Student>
    {
        public void Configure(EntityTypeBuilder<Student> builder)
        {
            // Get the parent ID from the existing parent seed data
            var parentId = Guid.Parse("550e8400-e29b-41d4-a716-446655440003");

            // Seed Student 1
            var student1Id = Guid.Parse("550e8400-e29b-41d4-a716-446655440010");
            builder.HasData(new Student
            {
                Id = student1Id,
                ParentId = parentId,
                ParentEmail = "parent@edubus.com",
                FirstName = "Nguyen",
                LastName = "Van An",
                Status = Models.Enums.StudentStatus.Active,
                CreatedAt = new DateTime(2024, 1, 15, 10, 0, 0, DateTimeKind.Utc),
                UpdatedAt = new DateTime(2024, 1, 15, 10, 0, 0, DateTimeKind.Utc),
                IsDeleted = false
            });

            // Seed Student 2
            var student2Id = Guid.Parse("550e8400-e29b-41d4-a716-446655440011");
            builder.HasData(new Student
            {
                Id = student2Id,
                ParentId = parentId,
                ParentEmail = "parent@edubus.com",
                FirstName = "Tran",
                LastName = "Thi Binh",
                Status = Models.Enums.StudentStatus.Active,
                CreatedAt = new DateTime(2024, 1, 15, 10, 0, 0, DateTimeKind.Utc),
                UpdatedAt = new DateTime(2024, 1, 15, 10, 0, 0, DateTimeKind.Utc),
                IsDeleted = false
            });

            // Seed Student 3
            var student3Id = Guid.Parse("550e8400-e29b-41d4-a716-446655440012");
            builder.HasData(new Student
            {
                Id = student3Id,
                ParentId = parentId,
                ParentEmail = "parent@edubus.com",
                FirstName = "Le",
                LastName = "Van Cuong",
                Status = Models.Enums.StudentStatus.Active,
                CreatedAt = new DateTime(2024, 1, 15, 10, 0, 0, DateTimeKind.Utc),
                UpdatedAt = new DateTime(2024, 1, 15, 10, 0, 0, DateTimeKind.Utc),
                IsDeleted = false
            });
        }
    }
}
