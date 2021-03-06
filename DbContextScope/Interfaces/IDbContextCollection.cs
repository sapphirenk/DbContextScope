﻿using System;
using Microsoft.EntityFrameworkCore;

namespace EntityFrameworkCore.DbContextScope
{
  /// <summary>
  /// Maintains a list of lazily-created DbContext instances.
  /// </summary>
  public interface IDbContextCollection : IDisposable
  {
    /// <summary>
    /// Get or create a DbContext instance of the specified type.
    /// </summary>
    TDbContext GetOrCreate<TDbContext>() where TDbContext : DbContext;
  }
}
