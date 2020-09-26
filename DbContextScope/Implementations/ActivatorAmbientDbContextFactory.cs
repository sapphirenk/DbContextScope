﻿using System;
using Microsoft.EntityFrameworkCore;

namespace EntityFrameworkCore.DbContextScope.Implementations
{
  public class ActivatorAmbientDbContextFactory : IAmbientDbContextFactory
  {
    public TDbContext CreateDbContext<TDbContext>(IDbContextScopeBase dbContextScope, bool readOnly) where TDbContext : DbContext
    {
      return Activator.CreateInstance<TDbContext>();
    }
  }
}
