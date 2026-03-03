

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
        public async Task<Response<int>> CreateBookingAsync(CreateBookingViewModel model)
        {
            // Begin transaction for atomic operation
            await unitOfWork.BeginTransactionAsync();
            
            try
            {
                var worker = await unitOfWork.AppUsers.GetByIdAsync(model.WorkerId);
                if (worker == null)
                {
                    await unitOfWork.RollbackTransactionAsync();
                    return new Response<int>(0, false, "Worker not found");
                }

                // Check for overlapping bookings with locking
                var ovverlaping = await unitOfWork.Bookings.FindAsync(b =>
                    b.WorkerId == model.WorkerId
                    && b.Status != BookingStatus.Cancelled
                    && b.StartTime < model.EndTime
                    && b.EndTime > model.StartTime);
                    
                if (ovverlaping.Any())
                {
                    await unitOfWork.RollbackTransactionAsync();
                    return new Response<int>(0, false, "Worker is not available during the requested time");
                }
                
                var booking = mapper.Map<Booking>(model);
                booking.WorkerId = model.WorkerId;
                booking.Status = BookingStatus.Scheduled;
                booking.CreatedAt = DateTime.UtcNow;
                
                await unitOfWork.Bookings.AddAsync(booking);
                await unitOfWork.SaveAsync();
                
                // Commit transaction
                await unitOfWork.CommitTransactionAsync();
                
                return new Response<int>(booking.Id, true, null);
            }
            catch (Exception ex)
            {
                // Rollback on any error
                await unitOfWork.RollbackTransactionAsync();
                return new Response<int>(0, false, $"Failed to create booking: {ex.Message}");
            }
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


            booking.Update(model.StartTime.Value, model.EndTime.Value, model.AgreedRate,model.Status);
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
            
            public async Task<Response<BookingDetailViewModel>> GetBookingByIdAsync(int bookingId)
            {
                var booking = await unitOfWork.Bookings.GetByIdAsync(bookingId);
                if (booking == null)
                {
                   return new Response<BookingDetailViewModel>(null, false, "Booking not found");
                }
                var bookingDetails = mapper.Map<BookingDetailViewModel>(booking);
                return new Response<BookingDetailViewModel>(bookingDetails, true, null);
            }

        public async Task<Response<List<BookingDashboardViewModel>>> GetBookingsByPosterIdAsync(string PosterId)
        {
            var bookings = await unitOfWork.Bookings.GetBookingsWithPosterAsync(PosterId);

            var mapped = mapper.Map<List<BookingDashboardViewModel>>(bookings);

            // Optimize: Use single GroupBy to get all counts in one pass
            var statusCounts = bookings
                .GroupBy(b => b.Status)
                .Select(g => new { Status = g.Key, Count = g.Count() })
                .ToDictionary(x => x.Status, x => x.Count);

            var pendingCount = statusCounts.GetValueOrDefault(BookingStatus.Scheduled);
            var inProgressCount = statusCounts.GetValueOrDefault(BookingStatus.InProgress);
            var completedCount = statusCounts.GetValueOrDefault(BookingStatus.Completed);
            var cancelledCount = statusCounts.GetValueOrDefault(BookingStatus.Cancelled);
            var disputedCount = statusCounts.GetValueOrDefault(BookingStatus.Disputed);

            mapped.ForEach(b =>
            {
                b.PendingCount = pendingCount;
                b.InProgressCount = inProgressCount;
                b.CompletedCount = completedCount;
                b.CancelledCount = cancelledCount;
                b.DisputedCount = disputedCount;
            });

            return new Response<List<BookingDashboardViewModel>>(mapped, true, null);
        }

        public async Task<Response<List<BookingDashboardViewModel>>> GetBookingsByWorkerIdAsync(string workerId)
        {
            var bookings = await unitOfWork.Bookings.GetBookingsWithWorkerAsync(b => b.WorkerId == workerId);

            var mapped = mapper.Map<List<BookingDashboardViewModel>>(bookings);

            // Optimize: Use single GroupBy to get all counts in one pass
            var statusCounts = bookings
                .GroupBy(b => b.Status)
                .Select(g => new { Status = g.Key, Count = g.Count() })
                .ToDictionary(x => x.Status, x => x.Count);

            var pendingCount = statusCounts.GetValueOrDefault(BookingStatus.Scheduled);
            var inProgressCount = statusCounts.GetValueOrDefault(BookingStatus.InProgress);
            var completedCount = statusCounts.GetValueOrDefault(BookingStatus.Completed);
            var cancelledCount = statusCounts.GetValueOrDefault(BookingStatus.Cancelled);
            var disputedCount = statusCounts.GetValueOrDefault(BookingStatus.Disputed);

            mapped.ForEach(b =>
            {
                b.PendingCount = pendingCount;
                b.InProgressCount = inProgressCount;
                b.CompletedCount = completedCount;
                b.CancelledCount = cancelledCount;
                b.DisputedCount = disputedCount;
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
        public async Task<Response<bool>> CancelBookingAsync(int bookingId)
        {
            var booking=await unitOfWork.Bookings.GetByIdAsync(bookingId);
            if (booking == null)
            {
                return new Response<bool>(false, false, "Booking not found");
            }
            if (booking.Status == BookingStatus.Completed || booking.Status == BookingStatus.Cancelled)
            {
                return new Response<bool>(false, false, "Cannot cancel a completed or already cancelled booking");
            }
            booking.Status = BookingStatus.Cancelled;
            await unitOfWork.Bookings.UpdateAsync(booking);
            await unitOfWork.SaveAsync();

            return new Response<bool>(true, true, null);
        }

        public async Task<Response<bool>> StartWorkBookingAsync(int bookingId)
        {
            var booking =await unitOfWork.Bookings.GetByIdAsync(bookingId);
            var chick1 = booking.PosterId;
            var chick2= booking.WorkerId;
            if(chick1==chick2)
                {
                return new Response<bool>(false, false, "Poster cannot start the work");
            }
            if (booking == null)
            {
                return new Response<bool>(false, false, "Booking Not Found");
            }
            if (booking.Status != BookingStatus.Scheduled)
            {
                return new Response<bool>(false, false, "Only scheduled bookings can be started");
            }
            
            if(booking.PosterId==booking.WorkerId)
            {
                return new Response<bool>(false, false, "Poster cannot start the work");
            }
            booking.Status= BookingStatus.InProgress;
            await unitOfWork.Bookings.UpdateAsync(booking);
            await unitOfWork.SaveAsync();
            return new Response<bool>(true, true, null);
        }

        public async Task<Response<bool>> CompleteBookingByWorkerAsync(int bookingId)
        {
            var booking = await unitOfWork.Bookings.GetByIdAsync(bookingId);
            if (booking == null)
                return new Response<bool>(false, false, "Booking Not Found");

            // ضيف الـ check ده
            if (booking.Status != BookingStatus.InProgress)
                return new Response<bool>(false, false, "Only in-progress bookings can be completed");

            booking.Status = BookingStatus.CompletedfromWorker;
            await unitOfWork.Bookings.UpdateAsync(booking);
            await unitOfWork.SaveAsync();
            return new Response<bool>(true, true, null);
        }
        public async Task<Response<IEnumerable<BookingDashboardViewModel>>> GetBookingsByUserIdAsync(string userId)
        {
            var workerBookings = await GetBookingsByWorkerIdAsync(userId);
            var posterBookings = await GetBookingsByPosterIdAsync(userId);

            var allBookings = new List<BookingDashboardViewModel>();

            if (workerBookings.Success && workerBookings.Result != null)
                allBookings.AddRange(workerBookings.Result);

            if (posterBookings.Success && posterBookings.Result != null)
                allBookings.AddRange(posterBookings.Result);

            // إزالة التكرار
            allBookings = allBookings
                .GroupBy(b => b.Id)
                .Select(g => g.First())
                .ToList();

            return new Response<IEnumerable<BookingDashboardViewModel>>(allBookings, true, null);
                
        }

        public async Task<Response<bool>> CompleteBookingByPosterAsync(int bookingId)
        {
            var booking =await unitOfWork.Bookings.GetByIdAsync(bookingId);
            if (booking == null)
            {
                return (new Response<bool>(false, false, "Booking Not Found"));
            }
            if(booking.Status != BookingStatus.CompletedfromWorker)
            {
                return new Response<bool>(false, false, "Booking must be marked as completed by worker first");
            }
            booking.Status = BookingStatus.Completed;

           await unitOfWork.Bookings.UpdateAsync(booking);
           await unitOfWork.SaveAsync();
            return new Response<bool>(true, true, null);
        }

    }
}
