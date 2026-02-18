using LaborDAL.Entities;
using LaborDAL.Enums;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace LaborDAL.DB.Configuration
{
    /// <summary>
    /// Configuration for TaskItem entity
    /// </summary>
    public class TaskItemConfiguration : IEntityTypeConfiguration<TaskItem>
    {
        public void Configure(EntityTypeBuilder<TaskItem> builder)
        {
            builder.ToTable("Tasks");

            // Primary key
            builder.HasKey(t => t.Id);

            // Properties
            builder.Property(t => t.Title)
                .IsRequired()
                .HasMaxLength(200);

            builder.Property(t => t.Description)
                .IsRequired()
                .HasMaxLength(5000);

            builder.Property(t => t.Location)
                .HasMaxLength(500);

            builder.Property(t => t.LocationUrl)
                .HasMaxLength(1000);

            builder.Property(t => t.Country)
                .HasMaxLength(100);

            builder.Property(t => t.City)
                .HasMaxLength(100);

            builder.Property(t => t.RequiredSkills)
                .HasMaxLength(1000);

            builder.Property(t => t.CancellationReason)
                .HasMaxLength(500);

            // Enum conversions
            builder.Property(t => t.Category)
                .HasConversion<string>()
                .HasMaxLength(50);

            builder.Property(t => t.Status)
                .HasConversion<string>()
                .HasMaxLength(50);

            builder.Property(t => t.BudgetType)
                .HasConversion<string>()
                .HasMaxLength(50);

            // Relationships
            builder.HasOne(t => t.Poster)
                .WithMany(u => u.PostedTasks)
                .HasForeignKey(t => t.PosterId)
                .OnDelete(DeleteBehavior.Restrict);

          

            // Indexes
            builder.HasIndex(t => t.PosterId);
         
            builder.HasIndex(t => t.Status);
            builder.HasIndex(t => t.Category);
            builder.HasIndex(t => t.DueDate);
            builder.HasIndex(t => t.CreatedAt);
            builder.HasIndex(t => t.IsFeatured);
            builder.HasIndex(t => t.IsUrgent);
            builder.HasIndex(t => new { t.Status, t.Category });
            builder.HasIndex(t => new { t.Latitude, t.Longitude });

            // Spatial index for location-based queries (SQL Server specific)
            // Note: For true spatial queries, consider using NetTopologySuite
            // This is a basic index for coordinate-based filtering
        }
    }
}
