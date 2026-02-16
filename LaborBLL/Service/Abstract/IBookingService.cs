
namespace LaborBLL.Service.Abstract
{
    public interface IBookingService 
    {
        Task<Response<BookingDetailsViewModel>> GetBookingByIdAsync(int bookingId);
        Task<Response<List<BookingDetailsViewModel>>> GetAllBookingAsync();


        Task<Response<bool>> CreateBookingAsync(CreateBookingViewModel model);
        Task<Response<bool>> DeleteBookingAsync(int BookingId);
        Task<Response<bool>> UpdateBookingAsync(UpdateBookingViewModel model);

        Task<Response<List<BookingDetailsViewModel>>> GetBookingsByWorkerIdAsync(string workerId);
            Task<Response<List<BookingDetailsViewModel>>> GetBookingsByPosterIdAsync(string posterId);
            Task<Response<List<BookingDetailsViewModel>>> GetOverlappingBookingsAsync(int workerId, DateTime start, DateTime end);



    }
}
