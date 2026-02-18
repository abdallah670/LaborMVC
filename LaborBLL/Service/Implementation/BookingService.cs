

namespace LaborBLL.Service.Implementation
{
    public class BookingService : IBookingService
    {
        private readonly IUnitOfWork unitOfWork;
        private readonly IMapper mapper;

        public BookingService(IUnitOfWork unitOfWork ,IMapper mapper)
        {
            this.unitOfWork = unitOfWork;
            this.mapper = mapper;
        }
        public async Task<Response<bool>> CreateBookingAsync(CreateBookingViewModel model)
                {
            var worker = await unitOfWork.AppUsers.GetByIdAsync(model.WorkerId);
            if (worker == null)
            {
                return new Response<bool>(false, false, "Worker not found");
            }

            var ovverlaping=await unitOfWork.Bookings.FindAsync(b=>
            b.WorkerId==model.WorkerId
            && b.StartTime < model.EndTime
            && b.EndTime > model.StartTime);
            if (ovverlaping.Any())
            {
                return new Response<bool>(false, false, "Worker is not available during the requested time");
            }
            var booking =mapper.Map<Booking>(model);
            booking.WorkerId=model.WorkerId;
            booking.Status=BookingStatus.Scheduled;
            booking.CreatedAt=DateTime.UtcNow;
            await unitOfWork.Bookings .AddAsync(booking);
            await unitOfWork.SaveAsync();
            return new Response<bool>(true, true, null);
        }
        public async Task<Response<bool>> UpdateBookingAsync(UpdateBookingViewModel model)
        {
            var booking = await unitOfWork.Bookings.GetByIdAsync(model.Id);
            if (booking == null)
            {
                return new Response<bool>(false, false, "booking not found");
            }
            var overlapping = await unitOfWork.Bookings.FindAsync(b =>
               b.Id != model.Id &&
               b.WorkerId == booking.WorkerId &&
               b.StartTime < model.EndTime &&
               b.EndTime > model.StartTime
           );
            if (overlapping.Any())
            {
                return new Response<bool>(false, false, "Worker is not available during the requested time");
            }


            booking.Update(model.StartTime.Value, model.EndTime.Value, model.AgreedRate);
            await unitOfWork.Bookings.UpdateAsync(booking);
            await unitOfWork.SaveAsync();


            return new Response<bool>(true, true, null);


        }

        public async Task<Response<bool>> DeleteBookingAsync(int BookingId)
        {
            var booking = await unitOfWork.Bookings .GetByIdAsync (BookingId);
            if (booking == null)
            {
                return new Response<bool>(false, false, "Booking not found");
            }
            await unitOfWork.Bookings.RemoveAsync(booking);
                await unitOfWork.SaveAsync();
            return new Response<bool>(true, true, null);
        }

        public async Task<Response<List<BookingDetailViewModel>>> GetAllBookingAsync()
        {
            var bookings = await unitOfWork.Bookings.GetAllAsync();
            var mappedBookings = mapper.Map<List<BookingDetailViewModel>>(bookings);
            return new Response<List<BookingDetailViewModel>>(mappedBookings, true, null);
        }

        public async Task<Response<List<BookingDetailViewModel>>> GetBookingByIdAsync(int bookingId)
        {
            var booking = await unitOfWork.Bookings.GetByIdAsync(bookingId);
            if (booking == null)
            {
               return new Response<List<BookingDetailViewModel>>(null, false, "Booking not found");
            }
            var bookingDetails = mapper.Map<List<BookingDetailViewModel>>(booking);
            return new Response<List<BookingDetailViewModel>>(bookingDetails, true, null);
        }

        public Task<Response<List<BookingDetailViewModel>>> GetBookingsByPosterIdAsync(string posterId)
        {
            throw new NotImplementedException();
        }

        public async Task<Response<List<BookingDashboardViewModel>>> GetBookingsByWorkerIdAsync(string workerId)
        {
            var bookings = await unitOfWork.Bookings.GetBookingsWithWorkerAsync(b => b.WorkerId == workerId);

            var mapped = mapper.Map<List<BookingDashboardViewModel>>(bookings);

            mapped.ForEach(b =>
            {
                
                b.PendingCount = bookings.Count(x => x.Status == BookingStatus.Scheduled);
                b.InProgressCount = bookings.Count(x => x.Status == BookingStatus.InProgress);
                b.CompletedCount = bookings.Count(x => x.Status == BookingStatus.Completed);
                b.CancelledCount = bookings.Count(x => x.Status == BookingStatus.Cancelled);
                b.DisputedCount = bookings.Count(x => x.Status == BookingStatus.Disputed);
            });

            return new Response<List<BookingDashboardViewModel>>(mapped, true, null);
        }

     public async Task<Response<List<BookingDashboardViewModel>>> GetOverlappingBookingsAsync(string workerId, DateTime start, DateTime end)
    {
        var overlapping = await unitOfWork.Bookings.FindAsync(b =>
            b.WorkerId == workerId &&
            b.StartTime < end &&
            b.EndTime > start);

        var mapped = mapper.Map<List<BookingDashboardViewModel>>(overlapping);

        mapped.ForEach(b =>
        {
            b.PendingCount = overlapping.Count(x => x.Status == BookingStatus.Scheduled);
            b.InProgressCount = overlapping.Count(x => x.Status == BookingStatus.InProgress);
            b.CompletedCount = overlapping.Count(x => x.Status == BookingStatus.Completed);
            b.CancelledCount = overlapping.Count(x => x.Status == BookingStatus.Cancelled);
            b.DisputedCount = overlapping.Count(x => x.Status == BookingStatus.Disputed);
        });

        return new Response<List<BookingDashboardViewModel>>(mapped, true, null);
    }

       
    }
}
