using LaborDAL.Entities;
using LaborDAL.Enums;
using Microsoft.EntityFrameworkCore;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace LaborDAL.DB.Configuration
{
    public class BookingConfegration : IEntityTypeConfiguration<Booking>
    {
        public void Configure(Microsoft.EntityFrameworkCore.Metadata.Builders.EntityTypeBuilder<Booking> builder)
         {
             builder.ToTable("Bookings");
             builder.Property(b => b.AgreedRate)
                 .HasPrecision(18, 2)
                 .IsRequired();
             builder.Property(b => b.StartTime)

                 .IsRequired(false);
             builder.Property(b => b.EndTime)
                 .IsRequired(false);
             builder.Property(b => b.Status)
                 .HasConversion<string>()
                 .IsRequired();
             builder.Property(b => b.CreatedAt)
                 .HasDefaultValueSql("GETUTCDATE()");
             builder.Property(b => b.RowVersion)
                 .IsRowVersion()
                 .IsConcurrencyToken();

            builder.HasIndex(b => b.CreatedAt);
            builder.HasIndex(b => b.TaskId);
            builder.HasIndex(b => b.WorkerId);
            builder.HasIndex(b => b.Status);

            // Global query filter for soft delete
            builder.HasQueryFilter(b => b.Status != BookingStatus.Cancelled);
        }

    }
}
