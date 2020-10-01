using NLog;
using System;
using System.Collections.Generic;
using System.Text;

namespace EntityFrameworkCore.DbContextScope
{
    interface ILoggerFactory
    {
        ILogger Create(string fullyQualifiedClassName);
        ILogger Create<T>();
    }
}
