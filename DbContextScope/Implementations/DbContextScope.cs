using System;
using System.Collections;
using System.Data;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;

namespace EntityFrameworkCore.DbContextScope.Implementations
{
  internal class DbContextScope : DbContextScopeBase, IDbContextScope
  {
    /// <summary>
    /// The parent scope uses the same DbContext instances as we do - no need to refresh anything
    /// </summary>
    private readonly IScopeDiagnostic _scopeDiagnostic;

    public DbContextScope(DbContextScopeOption joiningOption, IsolationLevel? isolationLevel, IAmbientDbContextFactory ambientDbContextFactory, ILoggerFactory loggerFactory, IScopeDiagnostic scopeDiagnostic)
            :base(joiningOption, false, isolationLevel, ambientDbContextFactory, loggerFactory, scopeDiagnostic)
    {
      _scopeDiagnostic = scopeDiagnostic;
    }
    
    public int SaveChanges()
    {
      if (_disposed)
      {
        throw new ObjectDisposedException("DbContextScope");
      }

      if (_completed)
      {
        throw new InvalidOperationException("You cannot call SaveChanges() more than once on a DbContextScope. "
                                          + "A DbContextScope is meant to encapsulate a business transaction: create the "
                                          + "scope at the start of the business transaction and then call SaveChanges() at "
                                          + "the end. Calling SaveChanges() mid-way through a business transaction doesn't "
                                          + "make sense and most likely mean that you should refactor your service method "
                                          + "into two separate service method that each create their own DbContextScope and "
                                          + "each implement a single business transaction.");
      }

      _scopeDiagnostic?.CalledMethods.Add("SaveChanges");

      // Only save changes if we're not a nested scope. Otherwise, let the top-level scope 
      // decide when the changes should be saved.
      var c = 0;
      if (!_nested)
      {
        c = commitInternal();
      }

      _completed = true;

      return c;
    }

    public Task<int> SaveChangesAsync()
    {
      return SaveChangesAsync(CancellationToken.None);
    }

    public async Task<int> SaveChangesAsync(CancellationToken cancelToken)
    {
      if (cancelToken == null)
      {
        throw new ArgumentNullException(nameof(cancelToken));
      }

      if (_disposed)
      {
        throw new ObjectDisposedException("DbContextScope");
      }

      if (_completed)
      {
        throw new InvalidOperationException("You cannot call SaveChanges() more than once on a DbContextScope. "
                                          + "A DbContextScope is meant to encapsulate a business transaction: create the "
                                          + "scope at the start of the business transaction and then call SaveChanges() at "
                                          + "the end. Calling SaveChanges() mid-way through a business transaction doesn't "
                                          + "make sense and most likely mean that you should refactor your service method "
                                          + "into two separate service method that each create their own DbContextScope and "
                                          + "each implement a single business transaction.");
      }

      _scopeDiagnostic?.CalledMethods.Add("SaveChangesAsync");

      // Only save changes if we're not a nested scope. Otherwise, let the top-level scope 
      // decide when the changes should be saved.
      var c = 0;
      if (!_nested)
      {
        c = await commitInternalAsync(cancelToken).ConfigureAwait(false);
      }

      _completed = true;

      return c;
    }

    public void RefreshEntitiesInParentScope(IEnumerable entities)
    {
      if (entities == null || _parentScope == null || _nested)
      {
        _scopeDiagnostic?.CalledMethods.Add("RefreshEntitiesInParentScope-SKIP");
        return;
      }

      // OK, so we must loop through all the DbContext instances in the parent scope
      // and see if their first-level cache (i.e. their ObjectStateManager) contains the provided entities. 
      // If they do, we'll need to force a refresh from the database. 

      // I'm sorry for this code but it's the only way to do this with the current version of Entity Framework 
      // as far as I can see.

      // What would be much nicer would be to have a way to merge all the modified / added / deleted
      // entities from one DbContext instance to another. NHibernate has support for this sort of stuff 
      // but EF still lags behind in this respect. But there is hope: https://entityframework.codeplex.com/workitem/864

      // NOTE: DbContext implements the ObjectContext property of the IObjectContextAdapter interface explicitely.
      // So we must cast the DbContext instances to IObjectContextAdapter in order to access their ObjectContext.
      // This cast is completely safe.

      _scopeDiagnostic?.CalledMethods.Add("RefreshEntitiesInParentScope");

      var entitiesToRefresh = entities as object[] ?? entities.Cast<object>().ToArray();
      foreach (var contextInCurrentScope in _dbContexts.InitializedDbContexts.Values)
      {
        var correspondingParentContext = _parentScope._dbContexts
           .InitializedDbContexts
           .Values
           .SingleOrDefault(parentContext => parentContext.DbContext.GetType() == contextInCurrentScope.DbContext.GetType());

        if (correspondingParentContext.DbContext == null)
        {
          continue; // No DbContext of this type has been created in the parent scope yet. So no need to refresh anything for this DbContext type.
        }

        var refreshStrategy = getRefreshStrategy(contextInCurrentScope.DbContext, correspondingParentContext.DbContext);
        // Both our scope and the parent scope have an instance of the same DbContext type. 
        // We can now look in the parent DbContext instance for entities that need to
        // be refreshed.
        foreach (var toRefresh in entitiesToRefresh)
        {
          refreshStrategy.Refresh(toRefresh);
        }
      }
    }

    public async Task RefreshEntitiesInParentScopeAsync(IEnumerable entities, CancellationToken cancellationToken = default(CancellationToken))
    {
      // See comments in the sync version of this method for an explanation of what we're doing here.

      if (entities == null || _parentScope == null || _nested)
      {
        _scopeDiagnostic?.CalledMethods.Add("RefreshEntitiesInParentScopeAsync-SKIP");
        return;
      }

      _scopeDiagnostic?.CalledMethods.Add("RefreshEntitiesInParentScopeAsync");

      var entitiesToRefresh = entities as object[] ?? entities.Cast<object>().ToArray();
      foreach (var contextInCurrentScope in _dbContexts.InitializedDbContexts.Values)
      {
        var correspondingParentContext =
          _parentScope
           ._dbContexts
           .InitializedDbContexts
           .Values
           .SingleOrDefault(parentContext => parentContext.DbContext.GetType() == contextInCurrentScope.DbContext.GetType());

        if (correspondingParentContext.DbContext == null)
        {
          continue;
        }

        var refreshStrategy = getRefreshStrategy(contextInCurrentScope.DbContext, correspondingParentContext.DbContext);
        foreach (var toRefresh in entitiesToRefresh)
        {
          await refreshStrategy.RefreshAsync(toRefresh, cancellationToken);
        }
      }
    }

    private static IEntityRefresh getRefreshStrategy(DbContext contextInCurrentScope, DbContext correspondingParentContext)
    {
      return new EntityRefresh(contextInCurrentScope, correspondingParentContext);
    }

   
    private Task<int> commitInternalAsync(CancellationToken cancelToken)
    {
      return _dbContexts.CommitAsync(cancelToken);
    }

    protected override void SetScope()
    {
        AmbientContextScopeMagic.SetAmbientScope(this);
    }
  }
}
