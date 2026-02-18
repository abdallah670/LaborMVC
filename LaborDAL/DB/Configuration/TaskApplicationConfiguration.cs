using LaborDAL.Entities;
using LaborDAL.Enums;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace LaborDAL.DB.Configuration
{
    /// <summary>
    /// Configuration for TaskApplication entity
    /// </summary>
    public class TaskApplicationConfiguration : IEntityTypeConfiguration<TaskApplication>
    {
        public void Configure(EntityTypeBuilder<TaskApplication> builder)
        {
            builder.ToTable("TaskApplications");

            // Primary key
            builder.HasKey(ta => ta.Id);

            // Properties
            builder.Property(ta => ta.Message)
                .HasMaxLength(2000);

            builder.Property(ta => ta.RejectionReason)
                .HasMaxLength(500);

            // Enum conversion
            builder.Property(ta => ta.Status)
                .HasConversion<string>()
                .HasMaxLength(50);

            // Relationships
            builder.HasOne(ta => ta.Task)
                .WithMany(t => t.Applications)
                .HasForeignKey(ta => ta.TaskItemId)
                .OnDelete(DeleteBehavior.Cascade);

            builder.HasOne(ta => ta.Worker)
                .WithMany(u => u.Applications)
                .HasForeignKey(ta => ta.WorkerId)
                .OnDelete(DeleteBehavior.Cascade);

            // Indexes
            builder.HasIndex(ta => ta.TaskItemId);
            builder.HasIndex(ta => ta.WorkerId);
            builder.HasIndex(ta => ta.Status);
            builder.HasIndex(ta => new { ta.TaskItemId, ta.WorkerId }).IsUnique();
        }
    }
}
