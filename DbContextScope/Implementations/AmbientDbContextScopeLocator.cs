using EntityFrameworkCore.DbContextScope.Interfaces;
using System;
using System.Collections.Generic;
using System.Text;

namespace EntityFrameworkCore.DbContextScope.Implementations
{
    internal class AmbientDbContextScopeLocator : IAmbientDbContextScopeLocator
    {
        public IDbContextScope Get()
        {
            var ambientDbContextScope = AmbientContextScopeMagic.GetAmbientScope();

            if (ambientDbContextScope == null)
            {
                throw new InvalidOperationException("No open ambient DbContextScope was found.");
            }

            return ambientDbContextScope;
        }
    }
}
