using Data.Models;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using System.Text;

namespace Data.SeedConfiguration
{
    public class AdminSeedConfiguration : IEntityTypeConfiguration<Admin>
    {
        public void Configure(EntityTypeBuilder<Admin> builder)
        {
            // Seed Admin account
            var adminId = Guid.Parse("550e8400-e29b-41d4-a716-446655440001");
            // Pre-hashed password for "password" - generated once and used statically
            var hashedPasswordBytes = Encoding.UTF8.GetBytes("$2a$10$92IXUNpkjO0rOQ5byMi.Ye4oKoEa3Ro9llC/.og/at2.uheWG/igi");

            builder.HasData(new Admin
            {
                Id = adminId,
                Email = "admin@edubus.com",
                HashedPassword = hashedPasswordBytes,
                FirstName = "Nguyen",
                LastName = "Van Admin",
                PhoneNumber = "0901234567",
                CreatedAt = new DateTime(2024, 1, 15, 10, 0, 0, DateTimeKind.Utc),
                UpdatedAt = new DateTime(2024, 1, 15, 10, 0, 0, DateTimeKind.Utc),
                IsDeleted = false
            });
        }
    }
}
