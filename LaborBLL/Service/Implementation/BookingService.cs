

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

                var booking =mapper.Map<Booking>(model);
            await unitOfWork.Bookings .AddAsync(booking);
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

        public async Task<Response<List<BookingDetailsViewModel>>> GetAllBookingAsync()
        {
            var bookings = await unitOfWork.Bookings.GetAllAsync();
            var mappedBookings = mapper.Map<List<BookingDetailsViewModel>>(bookings);
            return new Response<List<BookingDetailsViewModel>>(mappedBookings, true, null);
        }

        public async Task<Response<BookingDetailsViewModel>> GetBookingByIdAsync(int bookingId)
        {
            var booking = await unitOfWork.Bookings.GetByIdAsync(bookingId);
            if (booking == null)
            {
               return new Response<BookingDetailsViewModel>(null, false, "Booking not found");
            }
            var bookingDetails = mapper.Map<BookingDetailsViewModel>(booking);
            return new Response<BookingDetailsViewModel>(bookingDetails, true, null);
        }

        public Task<Response<List<BookingDetailsViewModel>>> GetBookingsByPosterIdAsync(string posterId)
        {
            throw new NotImplementedException();
        }

        public Task<Response<List<BookingDetailsViewModel>>> GetBookingsByWorkerIdAsync(string workerId)
        {
            throw new NotImplementedException();
        }

        public Task<Response<List<BookingDetailsViewModel>>> GetOverlappingBookingsAsync(int workerId, DateTime start, DateTime end)
        {
            throw new NotImplementedException();
        }

        public async Task<Response<bool>> UpdateBookingAsync(UpdateBookingViewModel model)
        {
            var booking =await unitOfWork.Bookings.GetByIdAsync(model.Id);
            if (booking == null)
            {
                               return new Response<bool>(false, false, "Booking not found");

            }
            booking.Update(model.StartTime.Value, model.EndTime.Value ,model.AgreedRate);
               await unitOfWork.Bookings.UpdateAsync(booking);
                await unitOfWork.SaveAsync();


            return new Response<bool>(true, true, null);


        }
    }
}
