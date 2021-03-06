﻿using System;
using System.Diagnostics;
using System.IO;
using System.Net;
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
            if (container == null)
                throw new ArgumentNullException("container");
            
            // Makes the a disk repository available to service implementations
            container.RegisterAs<DiskRepository,IDiskRepository>();

#if DEBUG
            Trace.Listeners.Add(new ConsoleTraceListener(true));
#endif
        }
    }
}