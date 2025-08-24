using Data.Models;
using Data.Models.Enums;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using System.Text;

namespace Data.SeedConfiguration
{
    public class ParentSeedConfiguration : IEntityTypeConfiguration<Parent>
    {
        public void Configure(EntityTypeBuilder<Parent> builder)
        {
            // Seed Parent account
            var parentId = Guid.Parse("550e8400-e29b-41d4-a716-446655440003");
            // Pre-hashed password for "password" - generated once and used statically
            var hashedPasswordBytes = Encoding.UTF8.GetBytes("$2a$10$92IXUNpkjO0rOQ5byMi.Ye4oKoEa3Ro9llC/.og/at2.uheWG/igi");

            builder.HasData(new Parent
            {
                Id = parentId,
                Email = "parent@edubus.com",
                HashedPassword = hashedPasswordBytes,
                FirstName = "Le",
                LastName = "Thi Parent",
                PhoneNumber = "0901234569",
                Address = "123 Nguyen Van Linh, District 7, Ho Chi Minh City",
                DateOfBirth = new DateTime(1984, 6, 12),
                Gender = Gender.Female,
                CreatedAt = new DateTime(2024, 1, 15, 10, 0, 0, DateTimeKind.Utc),
                UpdatedAt = new DateTime(2024, 1, 15, 10, 0, 0, DateTimeKind.Utc),
                IsDeleted = false
            });
        }
    }
}
