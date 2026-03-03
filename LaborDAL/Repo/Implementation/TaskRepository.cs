using LaborDAL.DB;
using LaborDAL.Entities;
using LaborDAL.Enums;
using LaborDAL.Repo.Abstract;
using Microsoft.EntityFrameworkCore;
using System.Linq.Expressions;
using TaskStatus = LaborDAL.Enums.TaskStatus;

namespace LaborDAL.Repo.Implementation
{
    /// <summary>
    /// Repository implementation for TaskItem with specialized query methods
    /// </summary>
    public class TaskRepository : Repository<TaskItem>, ITaskRepository
    {
        public TaskRepository(ApplicationDbContext context) : base(context)
        {
        }

        /// <summary>
        /// Get tasks within a radius from a given point using Haversine formula
        /// </summary>
        public async Task<IEnumerable<TaskItem>> GetTasksWithinRadiusAsync(decimal latitude, decimal longitude, double radiusKm)
        {
            // Earth's radius in kilometers
            const double earthRadius = 6371.0;

            // Convert to radians
            double latRad = (double)latitude * Math.PI / 180.0;
            double lonRad = (double)longitude * Math.PI / 180.0;

            // Get all tasks with coordinates (in production, use spatial index or database function)
            var tasksWithCoords = await _dbSet
                .Where(t => t.Latitude.HasValue && t.Longitude.HasValue && !t.IsDeleted)
                .ToListAsync();

            // Filter by distance using Haversine formula
            var tasksWithinRadius = tasksWithCoords.Where(t =>
            {
                double taskLatRad = (double)t.Latitude!.Value * Math.PI / 180.0;
                double taskLonRad = (double)t.Longitude!.Value * Math.PI / 180.0;

                double dLat = taskLatRad - latRad;
                double dLon = taskLonRad - lonRad;

                double a = Math.Sin(dLat / 2) * Math.Sin(dLat / 2) +
                           Math.Cos(latRad) * Math.Cos(taskLatRad) *
                           Math.Sin(dLon / 2) * Math.Sin(dLon / 2);

                double c = 2 * Math.Atan2(Math.Sqrt(a), Math.Sqrt(1 - a));
                double distance = earthRadius * c;

                return distance <= radiusKm;
            });

            return tasksWithinRadius;
        }

        /// <summary>
        /// Get tasks by category
        /// </summary>
        public async Task<IEnumerable<TaskItem>> GetByCategoryAsync(TaskCategory category)
        {
            return await _dbSet
                .Include(t => t.Poster)
                .Where(t => t.Category == category && !t.IsDeleted)
                .OrderByDescending(t => t.CreatedAt)
                .ToListAsync();
        }

        /// <summary>
        /// Get tasks by status
        /// </summary>
        public async Task<IEnumerable<TaskItem>> GetByStatusAsync(TaskStatus status)
        {
            return await _dbSet
                .Include(t => t.Poster)
                .Where(t => t.Status == status && !t.IsDeleted)
                .OrderByDescending(t => t.CreatedAt)
                .ToListAsync();
        }

        /// <summary>
        /// Get tasks posted by a specific user
        /// </summary>
        public async Task<IEnumerable<TaskItem>> GetByPosterIdAsync(string posterId)
        {
            return await _dbSet
                .Include(t => t.Poster)
                .Include(t => t.AssignedWorker)
                .Include(t => t.Applications)
                .Where(t => t.PosterId == posterId && !t.IsDeleted)
                .OrderByDescending(t => t.CreatedAt)
                .ToListAsync();
        }

        /// <summary>
        /// Get tasks assigned to a specific worker
        /// </summary>
        public async Task<IEnumerable<TaskItem>> GetByWorkerIdAsync(string workerId)
        {
            return await _dbSet
                .Include(t => t.Poster)
                .Include(t => t.AssignedWorker)
                .Where(t => t.AssignedWorker.Any(w => w.Id == workerId) && !t.IsDeleted)
                .OrderByDescending(t => t.AssignedAt)
                .ToListAsync();
        }

        /// <summary>
        /// Search tasks by keyword in title or description
        /// </summary>
        public async Task<IEnumerable<TaskItem>> SearchAsync(string keyword)
        {
            var lowerKeyword = keyword.ToLower();
            return await _dbSet
                .Include(t => t.Poster)
                .Where(t => !t.IsDeleted &&
                           (t.Title.ToLower().Contains(lowerKeyword) ||
                            t.Description.ToLower().Contains(lowerKeyword)))
                .OrderByDescending(t => t.CreatedAt)
                .ToListAsync();
        }

        /// <summary>
        /// Get featured tasks
        /// </summary>
        public async Task<IEnumerable<TaskItem>> GetFeaturedTasksAsync()
        {
            return await _dbSet
                .Include(t => t.Poster)
                .Where(t => t.IsFeatured && t.Status == TaskStatus.Open && !t.IsDeleted)
                .OrderByDescending(t => t.CreatedAt)
                .Take(10)
                .ToListAsync();
        }

        /// <summary>
        /// Get urgent tasks
        /// </summary>
        public async Task<IEnumerable<TaskItem>> GetUrgentTasksAsync()
        {
            return await _dbSet
                .Include(t => t.Poster)
                .Where(t => t.IsUrgent && t.Status == TaskStatus.Open && !t.IsDeleted)
                .OrderByDescending(t => t.CreatedAt)
                .Take(10)
                .ToListAsync();
        }

        /// <summary>
        /// Get tasks with filters
        /// </summary>
        public async Task<IEnumerable<TaskItem>> GetFilteredAsync(
            TaskCategory? category = null,
            TaskStatus? status = null,
            decimal? minBudget = null,
            decimal? maxBudget = null,
            bool? isRemote = null,
            bool?isUrgent = null,
            decimal? latitude = null,
            decimal? longitude = null,
            double? radiusKm = null,
            int page = 1,
            int pageSize = 20)
        {
            var query = _dbSet
                .Include(t => t.Poster)
                .Include(t => t.Applications)
                .Where(t => !t.IsDeleted);

            // Apply filters
            if (category.HasValue)
            {
                query = query.Where(t => t.Category == category.Value);
            }

            if (status.HasValue)
            {
                query = query.Where(t => t.Status == status.Value);
            }

            if (minBudget.HasValue)
            {
                query = query.Where(t => t.Budget >= minBudget.Value);
            }

            if (maxBudget.HasValue)
            {
                query = query.Where(t => t.Budget <= maxBudget.Value);
            }


            if (isRemote.HasValue)
            {
                query = query.Where(t => t.IsRemote == isRemote.Value);
            }
            if (isUrgent.HasValue)
            {
                query = query.Where(t => t.IsUrgent == isUrgent.Value);
            }

            // Apply location filter if coordinates provided
            if (latitude.HasValue && longitude.HasValue && radiusKm.HasValue)
            {
                var tasksInRadius = await GetTasksWithinRadiusAsync(latitude.Value, longitude.Value, radiusKm.Value);
                var taskIds = tasksInRadius.Select(t => t.Id).ToHashSet();
                query = query.Where(t => taskIds.Contains(t.Id));
            }

            // Order by created date and paginate
            return await query
                .OrderByDescending(t => t.IsFeatured)
                .ThenByDescending(t => t.IsUrgent)
                .ThenByDescending(t => t.CreatedAt)
                .Skip((page - 1) * pageSize)
                .Take(pageSize)
                .ToListAsync();
        }

        /// <summary>
        /// Get task with applications
        /// </summary>
        public async Task<TaskItem?> GetWithApplicationsAsync(int taskId)
        {
            return await _dbSet
                .Include(t => t.Poster)
                .Include(t => t.AssignedWorker)
                .Include(t => t.Applications)
                    .ThenInclude(a => a.Worker)
                .FirstOrDefaultAsync(t => t.Id == taskId && !t.IsDeleted);
        }

        /// <summary>
        /// Increment view count for a task
        /// </summary>
        public async Task IncrementViewCountAsync(int taskId)
        {
            var task = await _dbSet.FindAsync(taskId);
            if (task != null)
            {
                task.ViewCount++;
                await _context.SaveChangesAsync();
            }
        }

        /// <summary>
        /// Get tasks count by status
        /// </summary>
        public async Task<int> CountByStatusAsync(TaskStatus status)
        {
            return await _dbSet
                .CountAsync(t => t.Status == status && !t.IsDeleted);
        }

        /// <summary>
        /// Get similar tasks based on category, budget range, and location
        /// </summary>
        public async Task<IEnumerable<TaskItem>> GetSimilarTasksAsync(
            int taskId,
            TaskCategory category,
            decimal budget,
            bool isRemote,
            string? city = null,
            int count = 5)
        {
            // Calculate budget range (±30%)
            var minBudget = budget * 0.7m;
            var maxBudget = budget * 1.3m;

            // Get tasks with the same category, open status, excluding current task
            var query = _dbSet
                .Include(t => t.Poster)
                .Include(t => t.Applications)
                .Where(t => t.Id != taskId &&
                           t.Category == category &&
                           t.Status == TaskStatus.Open &&
                           !t.IsDeleted &&
                           t.IsRemote == isRemote &&
                           t.Budget >= minBudget &&
                           t.Budget <= maxBudget);

            // Get results and sort by relevance
            var tasks = await query.ToListAsync();

            // Sort by: Same city (if on-site), then budget proximity, then creation date
            var sortedTasks = tasks
                .OrderByDescending(t => !isRemote && !string.IsNullOrEmpty(city) &&
                                        !string.IsNullOrEmpty(t.City) &&
                                        t.City.Equals(city, StringComparison.OrdinalIgnoreCase))
                .ThenBy(t => Math.Abs((double)(t.Budget - budget))) // Closest budget first
                .ThenByDescending(t => t.CreatedAt) // Newest first
                .Take(count)
                .ToList();

            return sortedTasks;
        }

        /// <summary>
        /// Get filtered tasks with pagination and total count
        /// </summary>
        public async Task<(IEnumerable<TaskItem> Items, int TotalCount)> GetFilteredPagedAsync(
            TaskCategory? category = null,
            TaskStatus? status = null,
            decimal? minBudget = null,
            decimal? maxBudget = null,
            bool? isRemote = null,
            bool? isUrgent = null,
            decimal? latitude = null,
            decimal? longitude = null,
            double? radiusKm = null,
            string? searchKeyword = null,
            string? sortBy = null,
            string? sortDirection = "asc",
            int page = 1,
            int pageSize = 20)
        {
            // Validate pagination parameters
            page = Math.Max(1, page);
            pageSize = Math.Clamp(pageSize, 1, 100);

            var query = _dbSet
                .Include(t => t.Poster)
                .Where(t => !t.IsDeleted);

            // Apply filters
            if (category.HasValue)
            {
                query = query.Where(t => t.Category == category.Value);
            }

            if (status.HasValue)
            {
                query = query.Where(t => t.Status == status.Value);
            }

            if (minBudget.HasValue)
            {
                query = query.Where(t => t.Budget >= minBudget.Value);
            }

            if (maxBudget.HasValue)
            {
                query = query.Where(t => t.Budget <= maxBudget.Value);
            }

            if (isRemote.HasValue)
            {
                query = query.Where(t => t.IsRemote == isRemote.Value);
            }

            if (isUrgent.HasValue)
            {
                query = query.Where(t => t.IsUrgent == isUrgent.Value);
            }

            // Apply keyword search
            if (!string.IsNullOrWhiteSpace(searchKeyword))
            {
                var lowerKeyword = searchKeyword.ToLower();
                query = query.Where(t =>
                    t.Title.ToLower().Contains(lowerKeyword) ||
                    t.Description.ToLower().Contains(lowerKeyword));
            }

            // Apply location filter if coordinates provided
            if (latitude.HasValue && longitude.HasValue && radiusKm.HasValue)
            {
                var tasksInRadius = await GetTasksWithinRadiusAsync(latitude.Value, longitude.Value, radiusKm.Value);
                var taskIds = tasksInRadius.Select(t => t.Id).ToHashSet();
                query = query.Where(t => taskIds.Contains(t.Id));
            }

            // Get total count before pagination
            var totalCount = await query.CountAsync();

            // Apply sorting
            query = ApplySorting(query, sortBy, sortDirection);

            // Apply pagination
            var items = await query
                .Skip((page - 1) * pageSize)
                .Take(pageSize)
                .ToListAsync();

            return (items, totalCount);
        }

        /// <summary>
        /// Get filtered tasks with custom filter expression
        /// </summary>
        public async Task<(IEnumerable<TaskItem> Items, int TotalCount)> GetFilteredPagedAsync(
            System.Linq.Expressions.Expression<Func<TaskItem, bool>>? filter,
            int page,
            int pageSize,
            Func<IQueryable<TaskItem>, IOrderedQueryable<TaskItem>>? orderBy,
            bool ascending = true)
        {
            // Validate pagination parameters
            page = Math.Max(1, page);
            pageSize = Math.Clamp(pageSize, 1, 1000);

            var query = _dbSet
                .Include(t => t.Poster)
                .Where(t => !t.IsDeleted);

            // Apply custom filter
            if (filter != null)
            {
                query = query.Where(filter);
            }

            // Get total count before pagination
            var totalCount = await query.CountAsync();

            // Apply ordering
            if (orderBy != null)
            {
                query = orderBy(query);
            }
            else
            {
                query = query.OrderByDescending(t => t.CreatedAt);
            }

            // Apply pagination
            var items = await query
                .Skip((page - 1) * pageSize)
                .Take(pageSize)
                .ToListAsync();

            return (items, totalCount);
        }

        /// <summary>
        /// Apply dynamic sorting to task query
        /// </summary>
        private IQueryable<TaskItem> ApplySorting(IQueryable<TaskItem> query, string? sortBy, string? sortDirection)
        {
            var isDescending = sortDirection?.ToLower() == "desc";

            switch (sortBy?.ToLower())
            {
                case "budget":
                    return isDescending
                        ? query.OrderByDescending(t => t.Budget)
                        : query.OrderBy(t => t.Budget);
                
                case "createdat":
                case "date":
                    return isDescending
                        ? query.OrderByDescending(t => t.CreatedAt)
                        : query.OrderBy(t => t.CreatedAt);
                
                case "title":
                    return isDescending
                        ? query.OrderByDescending(t => t.Title)
                        : query.OrderBy(t => t.Title);
                
                case "viewcount":
                case "views":
                    return isDescending
                        ? query.OrderByDescending(t => t.ViewCount)
                        : query.OrderBy(t => t.ViewCount);
                
                default:
                    // Default: Featured first, then urgent, then newest
                    return query
                        .OrderByDescending(t => t.IsFeatured)
                        .ThenByDescending(t => t.IsUrgent)
                        .ThenByDescending(t => t.CreatedAt);
            }
        }
    }
}
