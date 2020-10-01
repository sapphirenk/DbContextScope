using System.Data;
using Microsoft.EntityFrameworkCore;

namespace EntityFrameworkCore.DbContextScope.Implementations
{
  internal class DbContextReadOnlyScope : DbContextScopeBase, IDbContextReadOnlyScope
  {
        public DbContextReadOnlyScope(DbContextScopeOption joiningOption, IsolationLevel? isolationLevel, IAmbientDbContextFactory ambientDbContextFactory, ILoggerFactory loggerFactory, IScopeDiagnostic scopeDiagnostic)
                : base(joiningOption, true, isolationLevel, ambientDbContextFactory, loggerFactory, scopeDiagnostic)
        {

        }

        protected override void SetScope()
        {
            AmbientContextScopeMagic.SetAmbientScope(this);
        }
    }
}
