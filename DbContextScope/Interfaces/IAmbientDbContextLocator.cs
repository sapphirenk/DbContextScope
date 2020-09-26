﻿using Microsoft.EntityFrameworkCore;

namespace EntityFrameworkCore.DbContextScope
{
  /// <summary>
  /// Convenience methods to retrieve ambient DbContext instances.
  /// </summary>
  public interface IAmbientDbContextLocator
  {
    /// <summary>
    /// If called within the scope of a DbContextScope, gets or creates
    /// the ambient DbContext instance for the provided DbContext type.
    /// Otherwise returns null.
    /// </summary>
    TDbContext Get<TDbContext>() where TDbContext : DbContext;

    IContextMetaData<TDbContext> GetWithMetaData<TDbContext>() where TDbContext : DbContext;
  
   }

  public interface IContextMetaData<TDbContext> where TDbContext : DbContext
  {
    TDbContext DbContext { get; }

    bool IsReadOnly { get; }
  }


}
