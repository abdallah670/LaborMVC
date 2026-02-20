

namespace LaborDAL.DB.Configuration
{
    public class class_RatingConfiguration : IEntityTypeConfiguration<Rating>
    {
        public void Configure(EntityTypeBuilder<Rating> builder)
        {
            builder.HasKey(r => r.Id);
            builder.Property(r => r.Score)
                .IsRequired()
                .HasAnnotation("Range", new []{  1,  5 });
            builder.HasOne(r => r.Rater)
                .WithMany(u => u.RatingsGiven)
                .HasForeignKey(r => r.RaterId)
                .OnDelete(DeleteBehavior.Restrict);
            builder.HasOne(r => r.Rated)
                .WithMany(r=>r.RatingsReceived)
                .HasForeignKey(r => r.RateeId)
                .OnDelete(DeleteBehavior.Restrict);
            builder.HasOne(r => r.Booking)
                .WithMany(b => b.Ratings)
                .HasForeignKey(r => r.bookingId)
                .OnDelete(DeleteBehavior.Cascade);
            builder.HasIndex(r => new { r.RaterId, r.RateeId, r.bookingId })
            .IsUnique();
        }
    }
}
