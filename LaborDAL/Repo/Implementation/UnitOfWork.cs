

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

        public ITaskRepository Tasks { get; private set; }

        public UnitOfWork(
            ApplicationDbContext context,
            UserManager<AppUser> userManager,
            IMapper mapper,
            ILoggerFactory loggerFactory,
            IBookingRepo bookingRepo
            )
        {
            _context = context;
            _userManager = userManager;
            _mapper = mapper;
            _loggerFactory = loggerFactory;
            Bookings = bookingRepo;

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
        }

        public async Task<int> SaveAsync()
        {
            return await _context.SaveChangesAsync();
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
    }
}
