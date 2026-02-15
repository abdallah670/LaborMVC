using LaborDAL.Repo.Abstract;
using Microsoft.EntityFrameworkCore;
using System.Linq.Expressions;
using Microsoft.EntityFrameworkCore.Query;
using LaborDAL.DB;
using AutoMapper;

namespace LaborDAL.Repo.Implementation
{
    public class Repository<T> : IRepository<T> where T : class
    {
        protected readonly ApplicationDbContext _context;
        protected readonly DbSet<T> _dbSet;
        protected readonly IMapper _mapper;

        public Repository(ApplicationDbContext context)
        {
            _context = context;
            _dbSet = _context.Set<T>();
        }

        public Repository(ApplicationDbContext context, IMapper mapper) : this(context)
        {
            _mapper = mapper;
        }

        public virtual async Task<T> GetByIdAsync(int id) => await _dbSet.FindAsync(id);
        public virtual async Task<T> GetByIdAsync(string id) => await _dbSet.FindAsync(id);
        public virtual async Task<IEnumerable<T>> GetAllAsync() => await _dbSet.ToListAsync();
        public virtual async Task<IEnumerable<T>> FindAsync(Expression<Func<T, bool>> predicate)
            => await _dbSet.Where(predicate).ToListAsync();

        public virtual async Task<T> SingleOrDefaultAsync(Expression<Func<T, bool>> predicate)
            => await _dbSet.SingleOrDefaultAsync(predicate);

        public virtual async Task<T> FirstOrDefaultAsync(Expression<Func<T, bool>> predicate)
            => await _dbSet.FirstOrDefaultAsync(predicate);

        public virtual async Task AddAsync(T entity) => await _dbSet.AddAsync(entity);
        public virtual async Task AddRangeAsync(IEnumerable<T> entities) => await _dbSet.AddRangeAsync(entities);
        public virtual async Task<int> CountAsync() => await _dbSet.CountAsync();
        public virtual async Task<bool> AnyAsync(Expression<Func<T, bool>> predicate)
            => await _dbSet.AnyAsync(predicate);

        public virtual async Task<IEnumerable<T>> GetPagedAsync(int page, int pageSize)
        {
            return await _dbSet
                .Skip((page - 1) * pageSize)
                .Take(pageSize)
                .ToListAsync();
        }

        public virtual IQueryable<T> Get(Expression<Func<T, bool>> filter = null,
            Func<IQueryable<T>, IOrderedQueryable<T>> orderBy = null,
            Func<IQueryable<T>, IIncludableQueryable<T, object>> include = null)
        {
            IQueryable<T> query = _dbSet;

            if (filter != null)
            {
                query = query.Where(filter);
            }

            if (include != null)
            {
                query = include(query);
            }

            if (orderBy != null)
            {
                return orderBy(query);
            }

            return query;
        }

        public async Task UpdateAsync(T entity)
        {
            _dbSet.Update(entity);
            await _context.SaveChangesAsync();
        }

        public async Task RemoveAsync(T entity)
        {
            _dbSet.Remove(entity);
            await _context.SaveChangesAsync();
        }

        public async Task RemoveRangeAsync(IEnumerable<T> entities)
        {
            _dbSet.RemoveRange(entities);
            await _context.SaveChangesAsync();
        }
    }
}
