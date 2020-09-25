using System;
using System.Collections.Generic;
using System.Text;

namespace EntityFrameworkCore.DbContextScope
{
    public interface IAmbientDbContextScopeLocator
    {
        IDbContextScope Get();
    }
}
