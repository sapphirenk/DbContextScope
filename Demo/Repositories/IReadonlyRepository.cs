using System;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;
using System.Threading.Tasks;

namespace DbContextScope.Demo.Repositories
{
    public interface IReadonlyRepository<T>
    {
        T Get(Expression<Func<T, bool>> predicate);

        Task<T> GetAsync(Expression<Func<T, bool>> predicate);

        List<T> GetWithInclude(params Expression<Func<T, object>>[] includeProperties);

        Task<List<T>> GetWithIncludeAsync(params Expression<Func<T, object>>[] includeProperties);

        List<T> GetWithInclude(Expression<Func<T, bool>> predicate, params Expression<Func<T, object>>[] includeProperties);

        Task<List<T>> GetWithIncludeAsync(Expression<Func<T, bool>> predicate, params Expression<Func<T, object>>[] includeProperties);

        IQueryable<T> GetAll();

        IQueryable<T> GetAll(Expression<Func<T, bool>> predicate);
    }
}
