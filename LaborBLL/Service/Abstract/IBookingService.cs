
namespace LaborBLL.Service.Abstract
{
    public interface IBookingService 
    {
        Task<Response<BookingDetailViewModel>> GetBookingByIdAsync(int bookingId);
        Task<Response<List<BookingDetailViewModel>>> GetAllBookingAsync();


        Task<Response<bool>> CreateBookingAsync(CreateBookingViewModel model);
        Task<Response<bool>> DeleteBookingAsync(int BookingId);
        Task<Response<bool>> UpdateBookingAsync(UpdateBookingViewModel model);
        Task<Response<bool>> CancelBookingAsync(int bookingId);
        Task<Response<bool>> StartWorkBookingAsync(int bookingId);
        Task<Response<bool>> CompleteBookingByWorkerAsync(int bookingId);
        Task<Response<bool>> CompleteBookingByPosterAsync(int bookingId);

        Task<Response<List<BookingDashboardViewModel>>> GetBookingsByWorkerIdAsync(string workerId);
            Task<Response<List<BookingDashboardViewModel>>> GetBookingsByPosterIdAsync(string posterId);
            Task<Response<List<BookingDashboardViewModel>>> GetOverlappingBookingsAsync(string workerId, DateTime start, DateTime end);
        Task<Response<IEnumerable<BookingDashboardViewModel>>>GetBookingsByUserIdAsync(string userId);




    }
}
