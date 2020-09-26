using System.Threading;
using System.Threading.Tasks;
using Castle.DynamicProxy;
using Microsoft.EntityFrameworkCore;

namespace EntityFrameworkCore.DbContextScope.Implementations.Proxy
{
  internal class DbContextInterceptor : DbContextInterceptorBase
  {
    public override int GetHashCode() => typeof(DbContextInterceptor).GetHashCode();

    protected override void OnHandleDispose(IInvocation invocation) => CurrentDbContextScope.Dispose();

    protected override int OnHandleSaveChanges(IInvocation invocation)
    {
      var dbContext = (DbContext)invocation.Proxy;
      var parentUpdater = new DetectModifiedEntitiesAndUpdateParentScope(dbContext, GetCurrentScope());

      var changes = GetCurrentScope().SaveChanges();
      parentUpdater.UpdateParent();

      return changes;
    }

    protected override Task<int> OnHandleSaveChangesAsync(IInvocation invocation)
    {
      var dbContext = (DbContext)invocation.Proxy;
      var parentUpdater = new DetectModifiedEntitiesAndUpdateParentScope(dbContext, GetCurrentScope());

      Task<int> returnValue;

      var maybeCancellationToken = GetCancellationTokenFromArgs(invocation);
      if (maybeCancellationToken.HasValue)
      {
        returnValue = saveChangesAndUpdateParentScopeAsync(parentUpdater, maybeCancellationToken.Value);
      }
      else
      {
        returnValue = saveChangesAndUpdateParentScopeAsync(parentUpdater);
      }

      return returnValue;
    }

    private async Task<int> saveChangesAndUpdateParentScopeAsync(DetectModifiedEntitiesAndUpdateParentScope parentUpdater, CancellationToken cancellationToken = default(CancellationToken))
    {
      var changes = await GetCurrentScope().SaveChangesAsync(cancellationToken);
      await parentUpdater.UpdateParentAsync(cancellationToken);

      return changes;
    }

    private DbContextScope GetCurrentScope() => CurrentDbContextScope as DbContextScope;


   }
}