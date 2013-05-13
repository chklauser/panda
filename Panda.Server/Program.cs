using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Net;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;

namespace Panda.Server
{
    class Program
    {
        static int Main(string[] args)
        {
            var baseUrl = "http://*:8997/";

            if (args.Length > 0)
            {
                baseUrl = args[0];
            }

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
                    Console.WriteLine();
                    Console.WriteLine("Additionally, if you want to make the server accessible from external machines, you might need to a corresponding rule to the Windows firewall.");
                    Console.WriteLine("The following command would do the trick");
                    Console.WriteLine("  netsh advfirewall firewall add rule name=\"Panda.Server\" dir=in action=allow protocol=TCP localport={0}", _extractPort(baseUrl));
                    Console.WriteLine();
                    Console.WriteLine("Note that these commands might need to be run with administrative privileges");

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

        private static int _extractPort(string baseUrl)
        {
            var pat = new Regex(@"http://[^:]+:(\d+)/");
            int result;
            if (!Int32.TryParse(pat.Match(baseUrl).Groups[1].ToString(), out result))
                result = 8997;
            return result;
        }

        private static void _waitForKey()
        {
            Console.WriteLine("Press any key to quit.");
            Console.ReadKey(true);
        }
    }
}
