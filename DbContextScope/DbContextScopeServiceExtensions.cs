using EntityFrameworkCore.DbContextScope.Implementations;
using Microsoft.Extensions.DependencyInjection;
using Prism.Ioc;

namespace EntityFrameworkCore.DbContextScope
{
  public static class DbContextScopeServiceExtensions
  {
    public static IServiceCollection AddDbContextScope(this IServiceCollection self)
    {
      self.AddScoped<IDbContextScopeFactory, DbContextScopeFactory>();
      self.AddScoped<IAmbientDbContextLocator, AmbientDbContextLocator>();
      self.AddScoped<IAmbientDbContextFactory, ProxyAmbientDbContextFactory>();
      self.AddSingleton<ILoggerFactory, LoggerFactory>();

      return self;
    }

    public static IContainerRegistry AddDbContextScope(this IContainerRegistry self)
    {
        self.Register<IDbContextScopeFactory, DbContextScopeFactory>();
        self.Register<IAmbientDbContextLocator, AmbientDbContextLocator>();
        self.Register<IAmbientDbContextFactory, ProxyAmbientDbContextFactory>();
        self.RegisterSingleton<ILoggerFactory, LoggerFactory>();

        return self;
    }
    }
}
