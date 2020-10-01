using EntityFrameworkCore.DbContextScope;
using NLog;
using System;
using System.Collections.Generic;
using System.Text;

namespace EntityFrameworkCore.DbContextScope.Implementations
{
    class LoggerFactory: ILoggerFactory
    {
        public ILogger Create(string fullyQualifiedClassName)
            => LogManager.GetLogger(fullyQualifiedClassName);

        public ILogger Create<T>() => Create(typeof(T).AssemblyQualifiedName);
    }
}
