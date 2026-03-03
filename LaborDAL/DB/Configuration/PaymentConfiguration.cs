namespace LaborDAL.DB.Configuration
{
    public class PaymentConfiguration : IEntityTypeConfiguration<Payment>
    {
        public void Configure(EntityTypeBuilder<Payment> builder)
        {
            builder.HasKey(p => p.Id);

            builder.Property(p => p.Amount)
                .HasColumnType("decimal(18,2)")
                .IsRequired();

            builder.Property(p => p.Status).HasConversion<string>();

            // Relationship with Booking
            builder.HasOne(p => p.Booking)
                .WithOne(b => b.Payment)
                .HasForeignKey<Payment>(p => p.BookingId)
                .OnDelete(DeleteBehavior.Restrict);
            
            // Indexes for performance
            builder.HasIndex(p => new { p.Status, p.CreatedAt })
                .HasDatabaseName("IX_Payments_Status_CreatedAt");
            
            builder.HasIndex(p => p.BookingId)
                .HasDatabaseName("IX_Payments_BookingId");
            
            builder.HasIndex(p => p.UserId)
                .HasDatabaseName("IX_Payments_UserId");
            
            builder.HasIndex(p => p.TransactionId)
                .HasDatabaseName("IX_Payments_TransactionId");

            // Unique index on IdempotencyKey to prevent duplicate payments
            builder.HasIndex(p => p.IdempotencyKey)
                .IsUnique()
                .HasFilter("[IdempotencyKey] IS NOT NULL")
                .HasDatabaseName("IX_Payments_IdempotencyKey");
        }
    }
}
