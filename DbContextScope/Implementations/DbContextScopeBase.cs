using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using System;
using System.Data;

namespace EntityFrameworkCore.DbContextScope.Implementations
{
    internal abstract class DbContextScopeBase: IDbContextScopeBase
    {
        internal readonly DbContextCollection _dbContexts;
        protected readonly DbContextScopeBase _parentScope;

        protected readonly bool _nested;
        protected bool _completed;
        protected bool _disposed;

        private readonly IScopeDiagnostic _scopeDiagnostic;
        private readonly ILogger<DbContextScopeBase> _logger;
        private readonly bool _readonly;

        public DbContextScopeBase(DbContextScopeOption joiningOption, bool readOnly, IsolationLevel? isolationLevel, IAmbientDbContextFactory ambientDbContextFactory, ILoggerFactory loggerFactory, IScopeDiagnostic scopeDiagnostic)
        {
            if (isolationLevel.HasValue && joiningOption == DbContextScopeOption.JoinExisting)
            {
                throw new ArgumentException("Cannot join an ambient DbContextScope when an explicit database transaction "
                                          + "is required. When requiring explicit database transactions to be used (i.e. when the "
                                          + "'isolationLevel' parameter is set), you must not also ask to join the ambient context "
                                          + "(i.e. the 'joinAmbient' parameter must be set to false).");
            }

            _scopeDiagnostic = scopeDiagnostic;
            _logger = loggerFactory.CreateLogger<DbContextScopeBase>();

            _readonly = readOnly;

            _disposed = false;
            _completed = false;

            _parentScope = AmbientContextScopeMagic.GetAmbientScope();
            if (_parentScope != null && joiningOption == DbContextScopeOption.JoinExisting)
            {
                if (_parentScope._readonly && !_readonly)
                {
                    throw new InvalidOperationException("Cannot nest a read/write DbContextScope within a read-only DbContextScope.");
                }

                _nested = true;
                _dbContexts = _parentScope._dbContexts;
            }
            else
            {
                _nested = false;
                _dbContexts = new DbContextCollection(this, ambientDbContextFactory, loggerFactory, readOnly, isolationLevel);
            }

            SetScope();
        }

        public TDbContext Get<TDbContext>() where TDbContext : DbContext
        {
            return _dbContexts.GetOrCreate<TDbContext>();
        }

        protected abstract void SetScope();

        protected int commitInternal()
        {
            return _dbContexts.Commit();
        }

        protected void rollbackInternal()
        {
            _dbContexts.Rollback();
        }

        public void Dispose()
        {
            if (_disposed)
            {
                return;
            }

            _scopeDiagnostic?.CalledMethods.Add("Dispose");

            // Commit / Rollback and dispose all of our DbContext instances
            if (!_nested)
            {
                if (!_completed)
                {
                    // Do our best to clean up as much as we can but don't throw here as it's too late anyway.
                    try
                    {
                        if (_readonly)
                        {
                            // Disposing a read-only scope before having called its SaveChanges() method
                            // is the normal and expected behavior. Read-only scopes get committed automatically.
                            commitInternal();
                        }
                        else
                        {
                            _logger.LogWarning("The read/write DbContextScope was disposed without calling SaveChanges/Async! Attempt to rollback the changes.");

                            // Disposing a read/write scope before having called its SaveChanges() method
                            // indicates that something went wrong and that all changes should be rolled-back.
                            rollbackInternal();
                        }
                    }
                    catch (Exception e)
                    {
                        _logger.LogError(e, "Error while disposing DbContextScope.");
                        // TODO: throw exception?
                    }

                    _completed = true;
                }

                _dbContexts.Dispose();
            }

            // Pop ourself from the ambient scope stack
            var currentAmbientScope = AmbientContextScopeMagic.GetAmbientScope();
            if (currentAmbientScope != this)
            {
                // This is a serious programming error. Worth throwing here.
                throw new InvalidOperationException("DbContextScope instances must be disposed of in the order in which they were created!");
            }

            AmbientContextScopeMagic.RemoveAmbientScope();

            if (_parentScope != null)
            {
                if (_parentScope._disposed)
                {
                    /*
                     * If our parent scope has been disposed before us, it can only mean one thing:
                     * someone started a parallel flow of execution and forgot to suppress the
                     * ambient context before doing so. And we've been created in that parallel flow.
                     * 
                     * Since the CallContext flows through all async points, the ambient scope in the 
                     * main flow of execution ended up becoming the ambient scope in this parallel flow
                     * of execution as well. So when we were created, we captured it as our "parent scope". 
                     * 
                     * The main flow of execution then completed while our flow was still ongoing. When 
                     * the main flow of execution completed, the ambient scope there (which we think is our 
                     * parent scope) got disposed of as it should.
                     * 
                     * So here we are: our parent scope isn't actually our parent scope. It was the ambient
                     * scope in the main flow of execution from which we branched off. We should never have seen 
                     * it. Whoever wrote the code that created this parallel task should have suppressed
                     * the ambient context before creating the task - that way we wouldn't have captured
                     * this bogus parent scope.
                     * 
                     * While this is definitely a programming error, it's not worth throwing here. We can only 
                     * be in one of two scenario:
                     * 
                     * - If the developer who created the parallel task was mindful to force the creation of 
                     * a new scope in the parallel task (with IDbContextScopeFactory.CreateNew() instead of 
                     * JoinOrCreate()) then no harm has been done. We haven't tried to access the same DbContext
                     * instance from multiple threads.
                     * 
                     * - If this was not the case, they probably already got an exception complaining about the same
                     * DbContext or ObjectContext being accessed from multiple threads simultaneously (or a related
                     * error like multiple active result sets on a DataReader, which is caused by attempting to execute
                     * several queries in parallel on the same DbContext instance). So the code has already blow up.
                     * 
                     * So just record a warning here. Hopefully someone will see it and will fix the code.
                     */

                    var message = @"PROGRAMMING ERROR - When attempting to dispose a DbContextScope, we found that our parent DbContextScope has already been disposed! This means that someone started a parallel flow of execution (e.g. created a TPL task, created a thread or enqueued a work item on the ThreadPool) within the context of a DbContextScope without suppressing the ambient context first. 

In order to fix this:
1) Look at the stack trace below - this is the stack trace of the parallel task in question.
2) Find out where this parallel task was created.
3) Change the code so that the ambient context is suppressed before the parallel task is created. You can do this with IDbContextScopeFactory.SuppressAmbientContext() (wrap the parallel task creation code block in this). 

Stack Trace:
"
                                + Environment.StackTrace;

                    throw new InvalidOperationException(message);
                }

                AmbientContextScopeMagic.SetAmbientScope(_parentScope);
            }

            _disposed = true;
        }

        /*
       * The idea of using an object reference as our instance identifier 
       * instead of simply using a unique string (which we could have generated
       * with Guid.NewGuid() for example) comes from the TransactionScope
       * class. As far as I can make out, a string would have worked just fine.
       * I'm guessing that this is done for optimization purposes. Creating
       * an empty class is cheaper and uses up less memory than generating
       * a unique string.
      */
        internal readonly InstanceIdentifier InstanceIdentifier = new InstanceIdentifier();
    }
}
