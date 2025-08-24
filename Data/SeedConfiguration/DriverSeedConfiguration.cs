using Data.Models;
using Data.Models.Enums;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using System.Text;

namespace Data.SeedConfiguration
{
    public class DriverSeedConfiguration : IEntityTypeConfiguration<Driver>
    {
        public void Configure(EntityTypeBuilder<Driver> builder)
        {
            // Seed Driver account
            var driverId = Guid.Parse("550e8400-e29b-41d4-a716-446655440002");
            // Pre-hashed password for "password" - generated once and used statically
            var hashedPasswordBytes = Encoding.UTF8.GetBytes("$2a$10$92IXUNpkjO0rOQ5byMi.Ye4oKoEa3Ro9llC/.og/at2.uheWG/igi");
            // Pre-hashed license number for "51A-123456" - generated once and used statically
            var hashedLicenseBytes = Encoding.UTF8.GetBytes("$2a$11$PQv3c1yqBWVHxkd0LHAkCOYz6TtxMQJqhN8/LewdBPj4J/HS.iK8O");

            builder.HasData(new Driver
            {
                Id = driverId,
                Email = "driver@edubus.com",
                HashedPassword = hashedPasswordBytes,
                FirstName = "Tran",
                LastName = "Van Driver",
                PhoneNumber = "0901234568",
                Address = "456 Trần Phú, Quận Hải Châu, Đà Nẵng, Vietnam",
                DateOfBirth = new DateTime(1985, 5, 20),
                Gender = Gender.Male,
                HashedLicenseNumber = hashedLicenseBytes,
                CreatedAt = new DateTime(2024, 1, 15, 10, 0, 0, DateTimeKind.Utc),
                UpdatedAt = new DateTime(2024, 1, 15, 10, 0, 0, DateTimeKind.Utc),
                IsDeleted = false
            });
        }
    }
}
