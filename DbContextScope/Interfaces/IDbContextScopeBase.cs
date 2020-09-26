using Microsoft.EntityFrameworkCore;
using System;

namespace EntityFrameworkCore.DbContextScope
{
    public interface IDbContextScopeBase : IDisposable
    {
        TDbContext Get<TDbContext>() where TDbContext : DbContext;
    }
}
