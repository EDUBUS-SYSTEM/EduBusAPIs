using Data.Models;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using System.Text;

namespace Data.SeedConfiguration
{
    public class DriverLicenseSeedConfiguration : IEntityTypeConfiguration<DriverLicense>
    {
        public void Configure(EntityTypeBuilder<DriverLicense> builder)
        {
            // Seed DriverLicense for the existing driver
            var driverId = Guid.Parse("550e8400-e29b-41d4-a716-446655440002");
            // Pre-hashed license number for "51A-123456" - generated once and used statically
            var hashedLicenseBytes = Encoding.UTF8.GetBytes("$2a$11$PQv3c1yqBWVHxkd0LHAkCOYz6TtxMQJqhN8/LewdBPj4J/HS.iK8O");

            builder.HasData(new DriverLicense
            {
                Id = Guid.Parse("550e8400-e29b-41d4-a716-446655440004"),
                DriverId = driverId,
                HashedLicenseNumber = hashedLicenseBytes,
                DateOfIssue = new DateTime(2020, 1, 15, 0, 0, 0, DateTimeKind.Unspecified),
                IssuedBy = "Cục Đăng kiểm Việt Nam",
                CreatedBy = Guid.Parse("550e8400-e29b-41d4-a716-446655440001"), // Admin ID
                CreatedAt = new DateTime(2024, 1, 15, 10, 0, 0, DateTimeKind.Utc),
                UpdatedAt = new DateTime(2024, 1, 15, 10, 0, 0, DateTimeKind.Utc),
                IsDeleted = false
            });
        }
    }
}
