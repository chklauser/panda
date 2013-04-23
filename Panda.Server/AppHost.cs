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
            _configureDatabase(container);

            container.RegisterAs<DiskRepository,IDiskRepository>();

            Plugins.Add(new AuthFeature(() => new AuthUserSession(), new IAuthProvider[]
                {
                    new BasicAuthProvider(), 
                    new CredentialsAuthProvider(), 
                    new DigestAuthProvider()
                }));

            container.Register<ICacheClient>(new MemoryCacheClient());
            var userRep = new OrmLiteAuthRepository(container.Resolve<IDbConnectionFactory>());
            container.Register<IUserAuthRepository>(userRep);
            userRep.CreateMissingTables();

            Plugins.Add(new RegistrationFeature());

            using (var db = container.Resolve<IDbConnectionFactory>().OpenDbConnection())
            {
                db.CreateTableIfNotExists<DiskAssociation>();
            }
        }

        private static void _configureDatabase(Container container)
        {
            var dbFactory = new OrmLiteConnectionFactory("panda-server.sqlite", false, SqliteDialect.Provider);
            container.Register<IDbConnectionFactory>(dbFactory);
        }
    }
}