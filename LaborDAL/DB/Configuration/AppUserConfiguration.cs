using LaborDAL.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace LaborDAL.DB.Configuration
{
    /// <summary>
    /// Entity configuration for AppUser
    /// </summary>
    public class AppUserConfiguration : IEntityTypeConfiguration<AppUser>
    {
        public void Configure(EntityTypeBuilder<AppUser> builder)
        {
            // Table name
            builder.ToTable("AspNetUsers");

            // Properties
            builder.Property(u => u.FirstName)
                .HasMaxLength(100)
                .IsRequired(false);

            builder.Property(u => u.LastName)
                .HasMaxLength(100)
                .IsRequired(false);

            builder.Property(u => u.IDVerified)
                .HasDefaultValue(false);

            builder.Property(u => u.AverageRating)
                .HasPrecision(3, 2)
                .IsRequired(false);

            builder.Property(u => u.Location)
                .HasMaxLength(500)
                .IsRequired(false);

            builder.Property(u => u.LocationUrl)
                .HasMaxLength(1000)
                .IsRequired(false);

            builder.Property(u => u.ProfilePictureUrl)
                .HasMaxLength(500)
                .IsRequired(false);

            builder.Property(u => u.Bio)
                .HasMaxLength(2000)
                .IsRequired(false);

            builder.Property(u => u.Skills)
                .HasMaxLength(1000)
                .IsRequired(false);

            builder.Property(u => u.CreatedAt)
                .HasDefaultValueSql("GETUTCDATE()");

            builder.Property(u => u.UpdatedAt)
                .IsRequired(false);

            builder.Property(u => u.IsDeleted)
                .HasDefaultValue(false);

            builder.Property(u => u.DeletedAt)
                .IsRequired(false);

            builder.Property(u => u.DeletedBy)
                .HasMaxLength(450)
                .IsRequired(false);

            builder.Property(u => u.UpdatedBy)
                .HasMaxLength(450)
                .IsRequired(false);

            builder.Property(u => u.CreatedBy)
                .HasMaxLength(450)
                .IsRequired(false);

            // Indexes
            builder.HasIndex(u => u.IsDeleted);
            builder.HasIndex(u => u.CreatedAt);
            builder.HasIndex(u => u.IDVerified);

            // Global query filter for soft delete
            builder.HasQueryFilter(u => !u.IsDeleted);
        }
    }
}