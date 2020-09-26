using DbContextScope.Demo.DatabaseContext;
using EntityFrameworkCore.DbContextScope;
using Microsoft.EntityFrameworkCore;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;
using System.Threading.Tasks;

namespace DbContextScope.Demo.Repositories
{
    public class ReadOnlyRepository<T> : IReadonlyRepository<T> where T : class
    {
        private readonly IAmbientDbContextLocator _ambientScopeLocator;

        public ReadOnlyRepository(IAmbientDbContextLocator contexLocator)
        {
            _ambientScopeLocator = contexLocator ?? throw new ArgumentNullException(nameof(contexLocator));
        }

        private IQueryable<T> getQuery()
        {
            IContextMetaData<UserManagementDbContext> contextMeta = _ambientScopeLocator.GetWithMetaData<UserManagementDbContext>() ?? throw new InvalidOperationException("No open ambient DbContext was found.");
            
            var dbSet = contextMeta.DbContext.Set<T>();
            
            return contextMeta.IsReadOnly ? dbSet.AsNoTracking() : dbSet;
        }

        public T Get(Expression<Func<T, bool>> predicate) => getQuery().FirstOrDefault(predicate);

        public Task<T> GetAsync(Expression<Func<T, bool>> predicate) => getQuery().FirstOrDefaultAsync(predicate);

        public List<T> GetWithInclude(params Expression<Func<T, object>>[] includeProperties)
            => Include(includeProperties).ToList();

        public Task<List<T>> GetWithIncludeAsync(params Expression<Func<T, object>>[] includeProperties)
            => Include(includeProperties).ToListAsync();

        public List<T> GetWithInclude(Expression<Func<T, bool>> predicate, params Expression<Func<T, object>>[] includeProperties)
            => Include(includeProperties).Where(predicate).ToList();

        public Task<List<T>> GetWithIncludeAsync(Expression<Func<T, bool>> predicate, params Expression<Func<T, object>>[] includeProperties)
            => Include(includeProperties).Where(predicate).ToListAsync();

        private IQueryable<T> Include(params Expression<Func<T, object>>[] includeProperties)
            => includeProperties.Aggregate(getQuery(), (current, includeProperty) => current.Include(includeProperty));

        public IQueryable<T> GetAll() => getQuery();

        public IQueryable<T> GetAll(Expression<Func<T, bool>> predicate) => getQuery().Where(predicate);
    }
}