using Data.Models;
using Microsoft.EntityFrameworkCore;
using NetTopologySuite.Geometries;

namespace Data.Contexts.SqlServer
{
    public partial class EduBusSqlContext : DbContext
    {
        public EduBusSqlContext()
        {
        }

        public EduBusSqlContext(DbContextOptions<EduBusSqlContext> options)
            : base(options)
        {
        }

        public virtual DbSet<Admin> Admins { get; set; }

        public virtual DbSet<Driver> Drivers { get; set; }

        public virtual DbSet<DriverVehicle> DriverVehicles { get; set; }

        public virtual DbSet<Grade> Grades { get; set; }

        public virtual DbSet<Image> Images { get; set; }

        public virtual DbSet<Parent> Parents { get; set; }

        public virtual DbSet<PickupPoint> PickupPoints { get; set; }

        public virtual DbSet<Student> Students { get; set; }

        public virtual DbSet<StudentGradeEnrollment> StudentGradeEnrollments { get; set; }

        public virtual DbSet<StudentPickupPoint> StudentPickupPoints { get; set; }

        public virtual DbSet<Transaction> Transactions { get; set; }

        public virtual DbSet<TransportFeeItem> TransportFeeItems { get; set; }

        public virtual DbSet<UnitPrice> UnitPrices { get; set; }

        public virtual DbSet<UserAccount> UserAccounts { get; set; }

        public virtual DbSet<Vehicle> Vehicles { get; set; }

        protected override void OnConfiguring(DbContextOptionsBuilder optionsBuilder)
#warning To protect potentially sensitive information in your connection string, you should move it out of source code. You can avoid scaffolding the connection string by using the Name= syntax to read it from configuration - see https://go.microsoft.com/fwlink/?linkid=2131148. For more guidance on storing connection strings, see https://go.microsoft.com/fwlink/?LinkId=723263.
            => optionsBuilder.UseSqlServer(
                "Server=LAPTOP-DVKPB8S9;Database=edubus_dev;User Id=sa;Password=123;Trusted_Connection=True;TrustServerCertificate=True",
                sql => sql.UseNetTopologySuite()
            );

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            // Configure TPT inheritance
            modelBuilder.Entity<UserAccount>()
                .ToTable("UserAccounts");

            modelBuilder.Entity<Admin>()
                .ToTable("Admins");

            modelBuilder.Entity<Parent>()
                .ToTable("Parents");

            modelBuilder.Entity<Driver>()
                .ToTable("Drivers");
            modelBuilder.Entity<Admin>(entity =>
            {
                entity.HasOne<UserAccount>()
                    .WithOne()
                    .HasForeignKey<Admin>(e => e.Id);
            });

            modelBuilder.Entity<Driver>(entity =>
            {
                entity.HasIndex(e => e.HashedLicenseNumber, "UQ_Drivers_HashedLicenseNumber").IsUnique();

                entity.Property(e => e.HashedLicenseNumber).HasMaxLength(256);

                entity.HasOne<UserAccount>()
                    .WithOne()
                    .HasForeignKey<Driver>(e => e.Id);
            });

            modelBuilder.Entity<DriverVehicle>(entity =>
            {
                entity.HasIndex(e => e.DriverId, "IX_DriverVehicles_DriverId");

                entity.HasIndex(e => e.VehicleId, "IX_DriverVehicles_VehicleId");

                entity.HasIndex(e => e.VehicleId, "UQ_DriverVehicles_PrimaryPerVehicle")
                    .IsUnique()
                    .HasFilter("([IsPrimaryDriver]=(1) AND [EndTimeUtc] IS NULL)");

                entity.Property(e => e.Id).HasDefaultValueSql("(newsequentialid())");
                entity.Property(e => e.EndTimeUtc).HasPrecision(3);
                entity.Property(e => e.IsDeleted).HasDefaultValue(false);
                entity.Property(e => e.StartTimeUtc).HasPrecision(3);
                entity.Property(e => e.UpdatedAt)
                    .HasPrecision(3);

                entity.HasOne(d => d.Driver).WithMany(p => p.DriverVehicles).HasForeignKey(d => d.DriverId);

                entity.HasOne(d => d.Vehicle).WithOne(p => p.DriverVehicle).HasForeignKey<DriverVehicle>(d => d.VehicleId);
            });

            modelBuilder.Entity<Grade>(entity =>
            {
                entity.HasIndex(e => e.Name, "UQ_Grades_Name").IsUnique();

                entity.Property(e => e.Id).HasDefaultValueSql("(newsequentialid())");
                entity.Property(e => e.IsDeleted).HasDefaultValue(false);
                entity.Property(e => e.Name).HasMaxLength(100);
            });

            modelBuilder.Entity<Image>(entity =>
            {
                entity.HasIndex(e => e.StudentId, "IX_Images_StudentId");

                entity.HasIndex(e => e.HashedUrl, "UQ_Images_HashedURL").IsUnique();

                entity.Property(e => e.Id).HasDefaultValueSql("(newsequentialid())");
                entity.Property(e => e.CreatedAt)
                    .HasPrecision(3)
                    .HasDefaultValueSql("(sysutcdatetime())");
                entity.Property(e => e.HashedUrl)
                    .HasMaxLength(256)
                    .HasColumnName("HashedURL");
                entity.Property(e => e.IsDeleted).HasDefaultValue(false);
                entity.Property(e => e.UpdatedAt)
                    .HasPrecision(3);

                entity.HasOne(d => d.Student).WithMany(p => p.Images).HasForeignKey(d => d.StudentId);
            });

            modelBuilder.Entity<Parent>(entity =>
            {
                entity.Property(e => e.Address).HasMaxLength(500);

                entity.HasOne<UserAccount>()
                    .WithOne()
                    .HasForeignKey<Parent>(e => e.Id);
            });

            modelBuilder.Entity<PickupPoint>(entity =>
            {
                entity.HasIndex(e => e.Description, "IX_PickupPoints_Description");

                entity.Property(e => e.Id).HasDefaultValueSql("(newsequentialid())");
                entity.Property(e => e.CreatedAt)
                    .HasPrecision(3)
                    .HasDefaultValueSql("(sysutcdatetime())");
                entity.Property(e => e.Description).HasMaxLength(500);
                entity.Property(e => e.IsDeleted).HasDefaultValue(false);
                entity.Property(e => e.Location).HasMaxLength(500);
                entity.Property(e => e.UpdatedAt)
                    .HasPrecision(3);
                entity.Property(e => e.Geog)
                      .HasColumnType("geography");
            });

            modelBuilder.Entity<Student>(entity =>
            {
                entity.HasIndex(e => e.ParentId, "IX_Students_ParentId");

                entity.Property(e => e.Id).HasDefaultValueSql("(newsequentialid())");
                entity.Property(e => e.CreatedAt)
                    .HasPrecision(3)
                    .HasDefaultValueSql("(sysutcdatetime())");
                entity.Property(e => e.FirstName).HasMaxLength(200);
                entity.Property(e => e.IsActive).HasDefaultValue(true);
                entity.Property(e => e.IsDeleted).HasDefaultValue(false);
                entity.Property(e => e.LastName).HasMaxLength(200);
                entity.Property(e => e.UpdatedAt)
                    .HasPrecision(3);

                entity.HasOne(d => d.Parent).WithMany(p => p.Students)
                    .HasForeignKey(d => d.ParentId)
                    .OnDelete(DeleteBehavior.ClientSetNull);
            });

            modelBuilder.Entity<StudentGradeEnrollment>(entity =>
            {
                entity.HasIndex(e => e.GradeId, "IX_SGE_GradeId");

                entity.HasIndex(e => e.StudentId, "IX_SGE_StudentId");

                entity.HasIndex(e => new { e.StudentId, e.GradeId, e.StartTimeUtc }, "UQ_SGE_Student_Grade_Start").IsUnique();

                entity.Property(e => e.Id).HasDefaultValueSql("(newsequentialid())");
                entity.Property(e => e.EndTimeUtc).HasPrecision(3);
                entity.Property(e => e.IsDeleted).HasDefaultValue(false);
                entity.Property(e => e.StartTimeUtc).HasPrecision(3);

                entity.HasOne(d => d.Grade).WithMany(p => p.StudentGradeEnrollments)
                    .HasForeignKey(d => d.GradeId)
                    .OnDelete(DeleteBehavior.ClientSetNull);

                entity.HasOne(d => d.Student).WithMany(p => p.StudentGradeEnrollments).HasForeignKey(d => d.StudentId);
            });

            modelBuilder.Entity<StudentPickupPoint>(entity =>
            {
                entity.HasIndex(e => e.PickupPointId, "IX_StudentPickupPoints_PickupPointId");

                entity.HasIndex(e => e.StudentId, "IX_StudentPickupPoints_StudentId");

                entity.HasIndex(e => e.StudentId, "UQ_StudentPickupPoints_Active")
                    .IsUnique()
                    .HasFilter("([IsActive]=(1) AND [EndTimeUtc] IS NULL)");

                entity.Property(e => e.Id).HasDefaultValueSql("(newsequentialid())");
                entity.Property(e => e.EndTimeUtc).HasPrecision(3);
                entity.Property(e => e.IsActive).HasDefaultValue(true);
                entity.Property(e => e.IsDeleted).HasDefaultValue(false);
                entity.Property(e => e.StartTimeUtc).HasPrecision(3);

                entity.HasOne(d => d.PickupPoint).WithMany(p => p.StudentPickupPoints)
                    .HasForeignKey(d => d.PickupPointId)
                    .OnDelete(DeleteBehavior.ClientSetNull);

                entity.HasOne(d => d.Student).WithOne(p => p.StudentPickupPoint).HasForeignKey<StudentPickupPoint>(d => d.StudentId);
            });

            modelBuilder.Entity<Transaction>(entity =>
            {
                entity.HasIndex(e => new { e.ParentId, e.CreatedAt }, "IX_Transactions_ParentId_CreatedAt");

                entity.HasIndex(e => e.TransactionCode, "UQ_Transactions_TransactionCode").IsUnique();

                entity.Property(e => e.Id).HasDefaultValueSql("(newsequentialid())");
                entity.Property(e => e.Amount).HasColumnType("decimal(19, 4)");
                entity.Property(e => e.CreatedAt)
                    .HasPrecision(3)
                    .HasDefaultValueSql("(sysutcdatetime())");
                entity.Property(e => e.IsDeleted).HasDefaultValue(false);
                entity.Property(e => e.TransactionCode).HasMaxLength(100);

                entity.HasOne(d => d.Parent).WithMany(p => p.Transactions)
                    .HasForeignKey(d => d.ParentId)
                    .OnDelete(DeleteBehavior.ClientSetNull);
            });

            modelBuilder.Entity<TransportFeeItem>(entity =>
            {
                entity.HasIndex(e => new { e.StudentId, e.Status }, "IX_TransportFeeItems_Student_Status");

                entity.HasIndex(e => e.TransactionId, "IX_TransportFeeItems_TransactionId");

                entity.Property(e => e.Id).HasDefaultValueSql("(newsequentialid())");
                entity.Property(e => e.Amount).HasColumnType("decimal(19, 4)");
                entity.Property(e => e.Content).HasMaxLength(1000);
                entity.Property(e => e.CreatedAt)
                    .HasPrecision(3)
                    .HasDefaultValueSql("(sysutcdatetime())");
                entity.Property(e => e.IsDeleted).HasDefaultValue(false);
                entity.Property(e => e.Status).HasMaxLength(50);

                entity.HasOne(d => d.Student).WithMany(p => p.TransportFeeItems)
                    .HasForeignKey(d => d.StudentId)
                    .OnDelete(DeleteBehavior.ClientSetNull);

                entity.HasOne(d => d.Transaction).WithMany(p => p.TransportFeeItems)
                    .HasForeignKey(d => d.TransactionId)
                    .OnDelete(DeleteBehavior.SetNull);
            });

            modelBuilder.Entity<UnitPrice>(entity =>
            {
                entity.HasIndex(e => e.AdminId, "IX_UnitPrices_AdminId");

                entity.HasIndex(e => e.StartTimeUtc, "IX_UnitPrices_ValidFrom");

                entity.Property(e => e.Id).HasDefaultValueSql("(newsequentialid())");
                entity.Property(e => e.CreatedAt)
                    .HasPrecision(3)
                    .HasDefaultValueSql("(sysutcdatetime())");
                entity.Property(e => e.DepartureTime).HasPrecision(0);
                entity.Property(e => e.EndTimeUtc).HasPrecision(3);
                entity.Property(e => e.IsDeleted).HasDefaultValue(false);
                entity.Property(e => e.ScheduleType).HasMaxLength(50);
                entity.Property(e => e.StartTimeUtc).HasPrecision(3);
                entity.Property(e => e.UpdatedAt)
                    .HasPrecision(3);

                entity.HasOne(d => d.Admin).WithMany(p => p.UnitPrices)
                    .HasForeignKey(d => d.AdminId)
                    .OnDelete(DeleteBehavior.ClientSetNull);
            });

            modelBuilder.Entity<UserAccount>(entity =>
            {
                entity.HasIndex(e => e.PhoneNumber, "IX_UserAccounts_PhoneNumber");

                entity.HasIndex(e => e.Email, "UQ_UserAccounts_Email").IsUnique();

                entity.Property(e => e.Id).HasDefaultValueSql("(newsequentialid())");
                entity.Property(e => e.CreatedAt)
                    .HasPrecision(3)
                    .HasDefaultValueSql("(sysutcdatetime())");
                entity.Property(e => e.Email).HasMaxLength(320);
                entity.Property(e => e.FirstName).HasMaxLength(200);
                entity.Property(e => e.HashedPassword).HasMaxLength(256);
                entity.Property(e => e.IsDeleted).HasDefaultValue(false);
                entity.Property(e => e.LastName).HasMaxLength(200);
                entity.Property(e => e.PhoneNumber).HasMaxLength(32);
                entity.Property(e => e.UpdatedAt)
                    .HasPrecision(3);
            });

            modelBuilder.Entity<Vehicle>(entity =>
            {
                entity.HasIndex(e => e.AdminId, "IX_Vehicles_AdminId");

                entity.HasIndex(e => e.HashedLicensePlate, "UQ_Vehicles_HashedLicensePlate").IsUnique();

                entity.Property(e => e.Id).HasDefaultValueSql("(newsequentialid())");
                entity.Property(e => e.CreatedAt)
                    .HasPrecision(3)
                    .HasDefaultValueSql("(sysutcdatetime())");
                entity.Property(e => e.HashedLicensePlate).HasMaxLength(256);
                entity.Property(e => e.IsDeleted).HasDefaultValue(false);
                entity.Property(e => e.Status).HasMaxLength(50);
                entity.Property(e => e.UpdatedAt)
                    .HasPrecision(3);

                entity.HasOne(d => d.Admin).WithMany(p => p.Vehicles)
                    .HasForeignKey(d => d.AdminId)
                    .OnDelete(DeleteBehavior.ClientSetNull);
            });

            OnModelCreatingPartial(modelBuilder);
        }

        partial void OnModelCreatingPartial(ModelBuilder modelBuilder);
    }
}
