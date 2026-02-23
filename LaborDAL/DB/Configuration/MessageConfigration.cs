

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
        }
    }
}
