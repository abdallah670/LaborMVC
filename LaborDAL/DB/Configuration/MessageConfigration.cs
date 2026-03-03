

using LaborDAL.DB;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace LaborDAL.DB.Configuration
{
    public class MessageConfigration : IEntityTypeConfiguration<Message>
    {
        public void Configure(EntityTypeBuilder<Message> builder)
        {
            builder.HasKey(m => m.Id);
            builder.Property(m => m.Content).IsRequired().HasMaxLength(1000);
            builder.Property(m => m.SenderId).IsRequired();
            builder.HasOne(m => m.Booking)
                   .WithMany()
                   .HasForeignKey(m => m.bookingId)
                   .OnDelete(DeleteBehavior.Restrict);
            builder.HasOne(m => m.Sender)
                   .WithMany()
                   .HasForeignKey(m => m.SenderId)
                   .OnDelete(DeleteBehavior.Restrict);
            
            // Indexes for performance
            builder.HasIndex(m => new { m.bookingId, m.SentAt })
                   .HasDatabaseName("IX_Messages_BookingId_SentAt");
            builder.HasIndex(m => m.SenderId)
                   .HasDatabaseName("IX_Messages_SenderId");
            builder.HasIndex(m => m.IsRead)
                   .HasDatabaseName("IX_Messages_IsRead");
        }
    }
}
