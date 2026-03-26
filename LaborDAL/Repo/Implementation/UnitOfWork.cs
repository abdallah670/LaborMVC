

namespace LaborDAL.Repo.Implementation
{
    public class UnitOfWork : IUnitOfWork
    {
        private readonly ApplicationDbContext _context;
        private readonly UserManager<AppUser> _userManager;
        private IDbContextTransaction? _transaction;
        private readonly IMapper _mapper;
        private readonly ILoggerFactory _loggerFactory;


        // Repositories
        public IAppUserRepository AppUsers { get; private set; }

        public IBookingRepo Bookings { get; private set; }
        public IRepository<Rating> Ratings => new Repository<Rating>(_context);

        public ITaskRepository Tasks { get; private set; }

        public IDisputeRepo Disputes { get; private set; }
        public IPaymentRepo Payments { get; private set; }
        public IMessageRepo Messages { get; private set; }
        public IchatRepo chatrepo { get; private set; }
        public IRatingRepo RatingRepo { get; private set; }

        // Distributed transaction repositories
        public IOutboxMessageRepository OutboxMessages { get; private set; }
        public IPendingTransferRepository PendingTransfers { get; private set; }
        public ISagaRepository Sagas { get; private set; }

        // Notification system
        public INotificationRepo Notifications { get; private set; }

        // File upload audit
        public IFileUploadAuditRepo FileUploadAudits { get; private set; }

        // ID Verification (KYC)
        public IIDVerificationRepo IDVerifications { get; private set; }

        public UnitOfWork(
            ApplicationDbContext context,
            UserManager<AppUser> userManager,
            IMapper mapper,
            ILoggerFactory loggerFactory,
            IBookingRepo bookingRepo,
             IPaymentRepo paymentRepo,
             IMessageRepo messageRepo,
            IchatRepo chatRepo,
            IRatingRepo ratingRepo
            )
        {
            _context = context;
            _userManager = userManager;
            _mapper = mapper;
            _loggerFactory = loggerFactory;
            Bookings = bookingRepo;
            Payments = paymentRepo;
            Messages = messageRepo;
            chatrepo = chatRepo;
            RatingRepo = ratingRepo;

            // Initialize repositories
            InitializeRepositories();
        }

        private void InitializeRepositories()
        {
            AppUsers = new AppUserRepository(
                _context,
                _userManager,
                _mapper,
                _loggerFactory.CreateLogger<AppUserRepository>());

            Tasks = new TaskRepository(_context);

            Disputes = new DisputeRepo(_context);

            // Initialize distributed transaction repositories
            OutboxMessages = new OutboxMessageRepository(_context);
            PendingTransfers = new PendingTransferRepository(_context);
            Sagas = new SagaRepository(_context);

            // Initialize notification repository
            Notifications = new NotificationRepo(_context);

            // Initialize file upload audit repository
            FileUploadAudits = new FileUploadAuditRepo(_context);

            // Initialize ID verification repository
            IDVerifications = new IDVerificationRepo(_context);
        }
        public async Task<int> SaveAsync()
        {
            try
            {
                return await _context.SaveChangesAsync();
            }
            catch (DbUpdateException ex)
            {
                // جيب الخطأ الحقيقي
                var innerException = ex.InnerException?.Message;

                // اطبع الخطأ في الـ Output
                Console.WriteLine($"ERROR: {innerException}");

                // ارمي الخطأ عشان تشوفه
                throw new Exception($"Database error: {innerException}", ex);
            }
        }
        public async Task BeginTransactionAsync()
        {
            _transaction = await _context.Database.BeginTransactionAsync();
        }

        public async Task CommitTransactionAsync()
        {
            try
            {
                await _context.SaveChangesAsync();
                _transaction?.Commit();
            }
            catch
            {
                _transaction?.Rollback();
                throw;
            }
            finally
            {
                _transaction?.Dispose();
                _transaction = null;
            }
        }

        public async Task RollbackTransactionAsync()
        {
            if (_transaction != null)
            {
                await _transaction.RollbackAsync();
                _transaction.Dispose();
                _transaction = null;
            }
        }

        public void Dispose()
        {
            _transaction?.Dispose();
            _context?.Dispose();
        }

        /// <summary>
        /// Gets a soft-deleted user by email (bypasses global query filter)
        /// </summary>
        public async Task<AppUser?> GetDeletedUserByEmailAsync(string email)
        {
            if (string.IsNullOrWhiteSpace(email))
                return null;

            return await _context.Users
                .IgnoreQueryFilters()
                .FirstOrDefaultAsync(u => u.Email == email && u.IsDeleted);
        }

        /// <summary>
        /// Gets a soft-deleted user by ID (bypasses global query filter)
        /// </summary>
        public async Task<AppUser?> GetDeletedUserByIdAsync(string userId)
        {
            if (string.IsNullOrWhiteSpace(userId))
                return null;

            return await _context.Users
                .IgnoreQueryFilters()
                .FirstOrDefaultAsync(u => u.Id == userId && u.IsDeleted);
        }
    }
}
