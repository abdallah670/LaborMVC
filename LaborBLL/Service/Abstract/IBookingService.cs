
namespace LaborBLL.Service.Abstract
{
    public interface IBookingService 
    {
        Task<Response<BookingDetailViewModel>> GetBookingByIdAsync(int bookingId);
        Task<Response<List<BookingDetailViewModel>>> GetAllBookingAsync();


        Task<Response<bool>> CreateBookingAsync(CreateBookingViewModel model);
        Task<Response<bool>> DeleteBookingAsync(int BookingId);
        Task<Response<bool>> UpdateBookingAsync(UpdateBookingViewModel model);

        Task<Response<List<BookingDashboardViewModel>>> GetBookingsByWorkerIdAsync(string workerId);
            Task<Response<List<BookingDetailViewModel>>> GetBookingsByPosterIdAsync(string posterId);
            Task<Response<List<BookingDashboardViewModel>>> GetOverlappingBookingsAsync(string workerId, DateTime start, DateTime end);



    }
}
