using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Net;
using System.Text;
using System.Threading.Tasks;

namespace Panda.Server
{
    class Program
    {
        static int Main(string[] args)
        {
            var baseUrl = "http://*:8997/";

            var appHost = new AppHost();
            appHost.Init();

            try
            {
                appHost.Start(baseUrl);
            }
            catch (HttpListenerException ex)
            {
                if (ex.ErrorCode == 5)
                {
                    var userName = Environment.GetEnvironmentVariable("USERNAME");
                    var userDomain = Environment.GetEnvironmentVariable("USERDOMAIN");

                    Console.WriteLine("You need to run the following command:");
                    var cmd = String.Format("  netsh http add urlacl url={0} user={1} listen=yes", baseUrl,
                        userName);
                    Console.WriteLine(cmd);
                    Console.WriteLine("On some systems/in some configurations, the user's Windows domain ({0} in this case) is also required.",userDomain);
                    Trace.WriteLine(cmd);
                    if (Debugger.IsAttached)
                        _waitForKey();
                    return -1;
                }
                else
                {
                    throw;
                }
            }

            
            Console.WriteLine("Panda.Server started, reachable at ");
            Console.WriteLine(baseUrl);
            _waitForKey();
            appHost.Stop();
            return 0;
        }

        private static void _waitForKey()
        {
            Console.WriteLine("Press any key to quit.");
            Console.ReadKey(true);
        }
    }
}
