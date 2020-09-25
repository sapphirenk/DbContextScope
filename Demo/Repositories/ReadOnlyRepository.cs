using DbContextScope.Demo.DatabaseContext;
using EntityFrameworkCore.DbContextScope;
using EntityFrameworkCore.DbContextScope.Interfaces;
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
        private readonly IAmbientDbContextScopeLocator _ambientScopeLocator;

        public ReadOnlyRepository(IAmbientDbContextScopeLocator scopeLocator)
        {
            _ambientScopeLocator = scopeLocator ?? throw new ArgumentNullException(nameof(scopeLocator));
        }

        private IQueryable<T> getQuery()
        {
            IDbContextScope scope = _ambientScopeLocator.Get() ?? throw new InvalidOperationException("No open ambient DbContextScope was found.");
            DbContext context = scope.Get<UserManagementDbContext>() ?? throw new InvalidOperationException("No open ambient DbContext was found.");
            var dbSet = context.Set<T>();
           
            if (scope.IsReadOnly)
            {
                return dbSet.AsNoTracking();
            }
            return dbSet.AsQueryable();
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