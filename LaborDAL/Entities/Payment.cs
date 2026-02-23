namespace LaborDAL.Entities
    {
        public class Payment
        {
            public Payment() { }
            public int Id { get; set; }
            public int BookingId { get; set; }
            public decimal Amount { get; set; }
            public string StripePaymentIntentId { get; set; }
            public Enums.paymentStatus Status { get; set; }
        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
        public DateTime? UpdatedAt { get; set; }

        public DateTime? ReleasedAt { get; set; }
        public virtual Booking Booking { get; set; }



    }
}
