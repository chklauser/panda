using System.Reflection;
using Funq;
using JetBrains.Annotations;
using Panda.Server.Persistence;
using ServiceStack.CacheAccess;
using ServiceStack.CacheAccess.Providers;
using ServiceStack.OrmLite;
using ServiceStack.ServiceInterface;
using ServiceStack.ServiceInterface.Auth;
using ServiceStack.WebHost.Endpoints;

namespace Panda.Server
{
    public class AppHost : AppHostHttpListenerBase
    {
        public AppHost() : base("Panda.Server", typeof(AppHost).Assembly)
        {
        }

        public override void Configure(Container container)
        {
            // Makes the a disk repository available to service implementations
            container.RegisterAs<DiskRepository,IDiskRepository>();
        }
    }
}