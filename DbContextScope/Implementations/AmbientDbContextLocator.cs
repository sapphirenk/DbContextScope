using System;
using Microsoft.EntityFrameworkCore;

namespace EntityFrameworkCore.DbContextScope.Implementations
{
  internal class AmbientDbContextLocator : IAmbientDbContextLocator
  {
    public TDbContext Get<TDbContext>() where TDbContext : DbContext
    {
      var ambientDbContextScope = AmbientContextScopeMagic.GetAmbientScope();

      if (ambientDbContextScope == null)
      {
        throw new InvalidOperationException("No open ambient DbContextScope was found.");
      }

      return ambientDbContextScope.Get<TDbContext>();
    }

    public IContextMetaData<TDbContext> GetWithMetaData<TDbContext>() where TDbContext : DbContext
    {
        var ambientDbContextScope = AmbientContextScopeMagic.GetAmbientScope();

        if (ambientDbContextScope == null)
        {
            throw new InvalidOperationException("No open ambient DbContextScope was found.");
        }

        if (ambientDbContextScope is IDbContextScope)
        {
            return new ContextMetaData<TDbContext>(ambientDbContextScope.Get<TDbContext>(), false);
        }

        return new ContextMetaData<TDbContext>(ambientDbContextScope.Get<TDbContext>(), true);
    }

    class ContextMetaData<TDbContext> : IContextMetaData<TDbContext> where TDbContext : DbContext
    {
        public ContextMetaData(TDbContext dbContext, bool isReadOnly)
        {
            DbContext = dbContext;
            IsReadOnly = isReadOnly;
        }

        public TDbContext DbContext { get; private set; }

        public bool IsReadOnly { get; private set; }
    }
  }
}
