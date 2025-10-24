using Microsoft.AspNetCore.Identity.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore;
using MyWebApp.Models;

namespace MyWebApp.Data
{
    public class ApplicationDbContext : IdentityDbContext<ApplicationUser>
    {
        public ApplicationDbContext(DbContextOptions<ApplicationDbContext> options)
            : base(options)
        {
        }

        public DbSet<Application> Applications { get; set; }
        public DbSet<Officer> Officers { get; set; }
        public DbSet<Document> Documents { get; set; }
        public DbSet<Address> Addresses { get; set; }
        public DbSet<Appointment> Appointments { get; set; }
        public DbSet<OfficerAssignment> OfficerAssignments { get; set; }
        public DbSet<Payment> Payments { get; set; }
        public DbSet<OtpVerification> OtpVerifications { get; set; }
        public DbSet<Qualification> Qualifications { get; set; }
        public DbSet<Experience> Experiences { get; set; }
        public DbSet<Transaction> Transactions { get; set; }
        public DbSet<Challan> Challans { get; set; }

        protected override void OnModelCreating(ModelBuilder builder)
        {
            base.OnModelCreating(builder);

            // Configure Application
            builder.Entity<Application>(entity =>
            {
                entity.HasKey(e => e.Id);
                entity.HasOne(e => e.Applicant)
                    .WithMany()
                    .HasForeignKey(e => e.ApplicantId)
                    .OnDelete(DeleteBehavior.Restrict);

                entity.HasOne(e => e.PermanentAddress)
                    .WithMany()
                    .HasForeignKey(e => e.PermanentAddressId)
                    .OnDelete(DeleteBehavior.Restrict);

                entity.HasOne(e => e.CurrentAddress)
                    .WithMany()
                    .HasForeignKey(e => e.CurrentAddressId)
                    .OnDelete(DeleteBehavior.Restrict);
            });

            // Configure Officer with one-to-one relationship to ApplicationUser
            builder.Entity<Officer>(entity =>
            {
                entity.HasKey(e => e.Id);

                entity.HasOne(e => e.User)
                    .WithOne()  // One-to-one relationship
                    .HasForeignKey<Officer>(e => e.UserId)
                    .OnDelete(DeleteBehavior.Cascade);

                entity.HasOne(e => e.CurrentAddress)
                    .WithMany()
                    .HasForeignKey(e => e.CurrentAddressId)
                    .OnDelete(DeleteBehavior.Restrict);
            });

            // Configure Document
            builder.Entity<Document>(entity =>
            {
                entity.HasKey(e => e.Id);
                entity.HasOne(e => e.Application)
                    .WithMany(a => a.Documents)
                    .HasForeignKey(e => e.ApplicationId)
                    .OnDelete(DeleteBehavior.Cascade);

                entity.HasOne(e => e.VerifiedByOfficer)
                    .WithMany()
                    .HasForeignKey(e => e.VerifiedByOfficerId)
                    .OnDelete(DeleteBehavior.Restrict);

                // Configure optional relationships with Qualification and Experience
                entity.HasOne<Qualification>()
                    .WithMany()
                    .HasForeignKey(d => d.QualificationId)
                    .OnDelete(DeleteBehavior.ClientSetNull);

                entity.HasOne<Experience>()
                    .WithMany()
                    .HasForeignKey(d => d.ExperienceId)
                    .OnDelete(DeleteBehavior.ClientSetNull);
            });

            // Configure Appointment
            builder.Entity<Appointment>(entity =>
            {
                entity.HasKey(e => e.Id);
                entity.HasOne(e => e.Application)
                    .WithOne(a => a.Appointment)
                    .HasForeignKey<Appointment>(e => e.ApplicationId)
                    .OnDelete(DeleteBehavior.Cascade);

                entity.HasOne(e => e.ScheduledByOfficer)
                    .WithMany()
                    .HasForeignKey(e => e.ScheduledByOfficerId)
                    .OnDelete(DeleteBehavior.Restrict);
            });

            // Configure OfficerAssignment
            builder.Entity<OfficerAssignment>(entity =>
            {
                entity.HasKey(e => e.Id);
                entity.HasOne(e => e.Application)
                    .WithMany(a => a.OfficerAssignments)
                    .HasForeignKey(e => e.ApplicationId)
                    .OnDelete(DeleteBehavior.Cascade);

                entity.HasOne(e => e.Officer)
                    .WithMany()
                    .HasForeignKey(e => e.OfficerId)
                    .OnDelete(DeleteBehavior.Restrict);
            });

            // Configure Payment
            builder.Entity<Payment>(entity =>
            {
                entity.HasKey(e => e.Id);
                entity.HasOne(e => e.Application)
                    .WithOne(a => a.Payment)
                    .HasForeignKey<Payment>(e => e.ApplicationId)
                    .OnDelete(DeleteBehavior.Cascade);
            });

            // Configure OtpVerification
            builder.Entity<OtpVerification>(entity =>
            {
                entity.HasKey(e => e.Id);
                entity.Property(e => e.Otp).IsRequired();
                entity.Property(e => e.ExpiryTime).IsRequired();
            });

            // Configure Address
            builder.Entity<Address>(entity =>
            {
                entity.HasKey(e => e.Id);
                entity.Property(e => e.AddressLine1).IsRequired();
                entity.Property(e => e.City).IsRequired();
                entity.Property(e => e.State).IsRequired();
                entity.Property(e => e.Country).IsRequired();
                entity.Property(e => e.PinCode).IsRequired();
            });

            // Configure Qualification
            builder.Entity<Qualification>(entity =>
            {
                entity.HasKey(e => e.Id);
                entity.HasOne(e => e.Application)
                    .WithMany(a => a.Qualifications)
                    .HasForeignKey(e => e.ApplicationId)
                    .OnDelete(DeleteBehavior.Cascade);

                entity.Property(e => e.DegreeName).IsRequired();
                entity.Property(e => e.InstituteName).IsRequired();
                entity.Property(e => e.UniversityName).IsRequired();
                entity.Property(e => e.YearOfPassing).IsRequired();
            });

            // Configure Experience
            builder.Entity<Experience>(entity =>
            {
                entity.HasKey(e => e.Id);
                entity.HasOne(e => e.Application)
                    .WithMany(a => a.Experiences)
                    .HasForeignKey(e => e.ApplicationId)
                    .OnDelete(DeleteBehavior.Cascade);

                entity.Property(e => e.CompanyName).IsRequired();
                entity.Property(e => e.Position).IsRequired();
                entity.Property(e => e.FromDate).IsRequired();
                entity.Property(e => e.ToDate).IsRequired();
            });

            // Configure Transaction
            builder.Entity<Transaction>(entity =>
            {
                entity.HasKey(e => e.Id);
                entity.HasOne(e => e.Application)
                    .WithMany()
                    .HasForeignKey(e => e.ApplicationId)
                    .OnDelete(DeleteBehavior.Cascade);

                entity.Property(e => e.TransactionId).IsRequired();
                entity.Property(e => e.Status).IsRequired();
                entity.Property(e => e.FirstName).IsRequired();
                entity.Property(e => e.LastName).IsRequired();
                entity.Property(e => e.Email).IsRequired();
                entity.Property(e => e.PhoneNumber).IsRequired();
            });

            // Configure Challan
            builder.Entity<Challan>(entity =>
            {
                entity.HasKey(e => e.Id);
                entity.HasOne(e => e.Application)
                    .WithOne()
                    .HasForeignKey<Challan>(e => e.ApplicationId)
                    .OnDelete(DeleteBehavior.Cascade);

                entity.Property(e => e.ChallanNumber).IsRequired();
                entity.Property(e => e.Name).IsRequired();
                entity.Property(e => e.Position).IsRequired();
                entity.Property(e => e.Amount).IsRequired();
                entity.Property(e => e.AmountInWords).IsRequired();
                entity.Property(e => e.ChallanDate).IsRequired();
            });
        }
    }
}
