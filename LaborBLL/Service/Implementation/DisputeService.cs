using AutoMapper;
using LaborBLL.ModelVM;
using LaborBLL.Response;
using LaborBLL.Service.Abstract;
using LaborDAL.Entities;
using LaborDAL.Enums;
using LaborDAL.Repo.Abstract;

namespace LaborBLL.Service.Implementation
{
    /// <summary>
    /// Service implementation for dispute management
    /// </summary>
    public class DisputeService : IDisputeService
    {
        private readonly IDisputeRepo _disputeRepo;
        private readonly IBookingRepo _bookingRepo;
        private readonly IUnitOfWork _unitOfWork;
        private readonly IMapper _mapper;
        private readonly ILogger<DisputeService> _logger;

        public DisputeService(
            IDisputeRepo disputeRepo,
            IBookingRepo bookingRepo,
            IUnitOfWork unitOfWork,
            IMapper mapper,
            ILogger<DisputeService> logger)
        {
            _disputeRepo = disputeRepo;
            _bookingRepo = bookingRepo;
            _unitOfWork = unitOfWork;
            _mapper = mapper;
            _logger = logger;
        }

        #region User Actions

        /// <inheritdoc />
        public async Task<Response<DisputeDetailsViewModel>> RaiseDisputeAsync(CreateDisputeViewModel model, string userId)
        {
            try
            {
                // Validate that the booking exists and user is a participant
                var booking = await _bookingRepo.FindAsync(b => b.Id == model.BookingId);
                var bookingWithTask = booking.FirstOrDefault();
                if (bookingWithTask == null)
                {
                    return new Response<DisputeDetailsViewModel>(default, false, "Booking not found.");
                }

                // Get the booking with includes for task info
                var bookingsWithWorker = await _bookingRepo.GetBookingsWithWorkerAsync(b => b.Id == model.BookingId);
                var bookingFull = bookingsWithWorker.FirstOrDefault();

                // Check if user is either the poster or the worker
                var isWorker = bookingFull?.WorkerId == userId;

                if (bookingFull == null)
                {
                    return new Response<DisputeDetailsViewModel>(default, false, "Booking not found.");
                }

                // Check if booking is completed
                if (bookingFull.Status != BookingStatus.Completed)
                {
                    return new Response<DisputeDetailsViewModel>(default, false, "Disputes can only be raised for completed bookings.");
                }

                // Check 48-hour window
                if (bookingFull.EndTime.HasValue)
                {
                    var hoursSinceCompletion = (DateTime.UtcNow - bookingFull.EndTime.Value).TotalHours;
                    if (hoursSinceCompletion > 48)
                    {
                        return new Response<DisputeDetailsViewModel>(default, false, "Disputes must be raised within 48 hours of booking completion.");
                    }
                }

                // Check if dispute already exists
                var disputeExists = await _disputeRepo.ExistsForBookingAsync(model.BookingId);
                if (disputeExists)
                {
                    return new Response<DisputeDetailsViewModel>(default, false, "A dispute already exists for this booking.");
                }

                // Create the dispute
                var dispute = new Dispute(model.BookingId, userId, model.Reason);
                await _disputeRepo.AddAsync(dispute);

                // Update booking status to Disputed
                bookingFull.Status = BookingStatus.Disputed;

                await _unitOfWork.SaveAsync();

                _logger.LogInformation("Dispute {DisputeId} raised by user {UserId} for booking {BookingId}", 
                    dispute.Id, userId, model.BookingId);

                var details = await GetDisputeDetailsAsync(dispute.Id);
                return new Response<DisputeDetailsViewModel>(details!, true, null);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error raising dispute for booking {BookingId}", model.BookingId);
                return new Response<DisputeDetailsViewModel>(default, false, "An error occurred while raising the dispute.");
            }
        }

        /// <inheritdoc />
        public async Task<DisputeDetailsViewModel?> GetDisputeDetailsAsync(int disputeId)
        {
            var dispute = await _disputeRepo.GetByIdWithIncludesAsync(disputeId);
            if (dispute == null)
            {
                return null;
            }

            return MapToDetailsViewModel(dispute);
        }

        /// <inheritdoc />
        public async Task<IEnumerable<DisputeListViewModel>> GetUserDisputesAsync(string userId)
        {
            var disputes = await _disputeRepo.GetDisputesByUserAsync(userId);
            return disputes.Select(d => new DisputeListViewModel
            {
                Id = d.Id,
                BookingId = d.BookingId,
                TaskTitle = d.Booking?.Task?.Title ?? "Unknown Task",
                RaisedByName = $"{d.RaisedByUser?.FirstName} {d.RaisedByUser?.LastName}",
                Status = d.Status,
                CreatedAt = d.CreatedAt,
                ResolvedAt = d.ResolvedAt,
                Resolution = d.Resolution
            });
        }

        /// <inheritdoc />
        public async Task<bool> CanRaiseDisputeAsync(int bookingId, string userId)
        {
            var bookings = await _bookingRepo.GetBookingsWithWorkerAsync(b => b.Id == bookingId);
            var booking = bookings.FirstOrDefault();
            if (booking == null) return false;

            // Check if user is a participant (worker or poster via task)
            var isWorker = booking.WorkerId == userId;
            if (!isWorker) return false;

            // Check if booking is completed
            if (booking.Status != BookingStatus.Completed) return false;

            // Check 48-hour window
            if (booking.EndTime.HasValue)
            {
                var hoursSinceCompletion = (DateTime.UtcNow - booking.EndTime.Value).TotalHours;
                if (hoursSinceCompletion > 48) return false;
            }

            // Check if dispute already exists
            if (await _disputeRepo.ExistsForBookingAsync(bookingId)) return false;

            return true;
        }

        #endregion

        #region Admin Actions

        /// <inheritdoc />
        public async Task<IEnumerable<DisputeListViewModel>> GetAllDisputesAsync(DisputeStatus? status = null)
        {
            IEnumerable<Dispute> disputes;

            if (status.HasValue)
            {
                disputes = await _disputeRepo.GetDisputesByStatusAsync(status.Value);
            }
            else
            {
                disputes = await _disputeRepo.GetAllWithIncludesAsync();
            }

            return disputes.Select(d => new DisputeListViewModel
            {
                Id = d.Id,
                BookingId = d.BookingId,
                TaskTitle = d.Booking?.Task?.Title ?? "Unknown Task",
                RaisedByName = $"{d.RaisedByUser?.FirstName} {d.RaisedByUser?.LastName}",
                Status = d.Status,
                CreatedAt = d.CreatedAt,
                ResolvedAt = d.ResolvedAt,
                Resolution = d.Resolution
            });
        }

        /// <inheritdoc />
        public async Task<Response<bool>> UpdateStatusAsync(int disputeId, DisputeStatus status)
        {
            try
            {
                var dispute = await _disputeRepo.GetByIdAsync(disputeId);
                if (dispute == null)
                {
                    return new Response<bool>(false, false, "Dispute not found.");
                }

                if (dispute.Status == DisputeStatus.Resolved)
                {
                    return new Response<bool>(false, false, "Cannot update a resolved dispute.");
                }

                dispute.Status = status;
                dispute.UpdatedAt = DateTime.UtcNow;
                
                await _unitOfWork.SaveAsync();

                _logger.LogInformation("Dispute {DisputeId} status updated to {Status}", disputeId, status);
                return new Response<bool>(true, true, null);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error updating dispute {DisputeId} status", disputeId);
                return new Response<bool>(false, false, "An error occurred while updating the dispute status.");
            }
        }

        /// <inheritdoc />
        public async Task<Response<bool>> ResolveDisputeAsync(ResolveDisputeViewModel model, string adminId)
        {
            try
            {
                var dispute = await _disputeRepo.GetByIdWithIncludesAsync(model.DisputeId);
                if (dispute == null)
                {
                    return new Response<bool>(false, false, "Dispute not found.");
                }

                if (dispute.Status == DisputeStatus.Resolved)
                {
                    return new Response<bool>(false, false, "Dispute is already resolved.");
                }

                // Validate custom split percentage
                if (model.ResolutionType == ResolutionType.CustomSplit)
                {
                    if (!model.WorkerPercentage.HasValue || model.WorkerPercentage < 0 || model.WorkerPercentage > 100)
                    {
                        return new Response<bool>(false, false, "Worker percentage must be between 0 and 100 for custom splits.");
                    }
                }

                // Update dispute
                dispute.Status = DisputeStatus.Resolved;
                dispute.Resolution = model.Resolution;
                dispute.ResolutionType = model.ResolutionType;
                dispute.WorkerPercentage = model.ResolutionType == ResolutionType.CustomSplit 
                    ? model.WorkerPercentage 
                    : GetDefaultPercentage(model.ResolutionType);
                dispute.ResolvedBy = adminId;
                dispute.ResolvedAt = DateTime.UtcNow;
                dispute.UpdatedAt = DateTime.UtcNow;

                // Update booking status based on resolution
                if (dispute.Booking != null)
                {
                    dispute.Booking.Status = BookingStatus.Completed;
                }

                await _unitOfWork.SaveAsync();

                _logger.LogInformation("Dispute {DisputeId} resolved by admin {AdminId} with resolution type {ResolutionType}", 
                    model.DisputeId, adminId, model.ResolutionType);

                return new Response<bool>(true, true, null);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error resolving dispute {DisputeId}", model.DisputeId);
                return new Response<bool>(false, false, "An error occurred while resolving the dispute.");
            }
        }

        /// <inheritdoc />
        public async Task<int> GetOpenDisputeCountAsync()
        {
            return await _disputeRepo.GetCountByStatusAsync(DisputeStatus.Open);
        }

        /// <inheritdoc />
        public async Task<Dictionary<string, int>> GetDisputeStatsAsync()
        {
            return new Dictionary<string, int>
            {
                { "Open", await _disputeRepo.GetCountByStatusAsync(DisputeStatus.Open) },
                { "UnderReview", await _disputeRepo.GetCountByStatusAsync(DisputeStatus.UnderReview) },
                { "Resolved", await _disputeRepo.GetCountByStatusAsync(DisputeStatus.Resolved) },
                { "Total", await _disputeRepo.CountAsync() }
            };
        }

        #endregion

        #region Private Helpers

        private DisputeDetailsViewModel MapToDetailsViewModel(Dispute dispute)
        {
            return new DisputeDetailsViewModel
            {
                Id = dispute.Id,
                BookingId = dispute.BookingId,
                TaskId = dispute.Booking?.Task?.Id ?? 0,
                TaskTitle = dispute.Booking?.Task?.Title ?? "Unknown Task",
                TaskDescription = dispute.Booking?.Task?.Description ?? "",
                PosterId = dispute.Booking?.Task?.PosterId ?? "",
                PosterName = dispute.Booking?.Task?.Poster != null 
                    ? $"{dispute.Booking.Task.Poster.FirstName} {dispute.Booking.Task.Poster.LastName}" 
                    : "Unknown",
                PosterPhone = dispute.Booking?.Task?.Poster?.PhoneNumber,
                WorkerId = dispute.Booking?.WorkerId ?? "",
                WorkerName = dispute.Booking?.Worker != null 
                    ? $"{dispute.Booking.Worker.FirstName} {dispute.Booking.Worker.LastName}" 
                    : "Unknown",
                WorkerPhone = dispute.Booking?.Worker?.PhoneNumber,
                RaisedBy = dispute.RaisedBy,
                RaisedByName = dispute.RaisedByUser != null 
                    ? $"{dispute.RaisedByUser.FirstName} {dispute.RaisedByUser.LastName}" 
                    : "Unknown",
                Reason = dispute.Reason,
                Status = dispute.Status,
                Resolution = dispute.Resolution,
                ResolutionType = dispute.ResolutionType,
                WorkerPercentage = dispute.WorkerPercentage,
                ResolvedByName = dispute.ResolvedByUser != null 
                    ? $"{dispute.ResolvedByUser.FirstName} {dispute.ResolvedByUser.LastName}" 
                    : null,
                ResolvedAt = dispute.ResolvedAt,
                AgreedRate = dispute.Booking?.AgreedRate ?? 0,
                StartTime = dispute.Booking?.StartTime,
                EndTime = dispute.Booking?.EndTime,
                BookingStatus = dispute.Booking?.Status ?? BookingStatus.Scheduled,
                CreatedAt = dispute.CreatedAt
            };
        }

        private int? GetDefaultPercentage(ResolutionType resolutionType)
        {
            return resolutionType switch
            {
                ResolutionType.WorkerWins => 100,
                ResolutionType.PosterWins => 0,
                ResolutionType.SplitEvenly => 50,
                ResolutionType.CustomSplit => null,
                _ => null
            };
        }

        #endregion
    }
}
