using Panda.ServiceModel;
using ServiceStack.ServiceHost;
using ServiceStack.ServiceInterface;

namespace Panda.Server
{
    public class HelloService : Service
    {
         public object Any(Hello request)
         {
             return new HelloResponse {Text = "Hello " + request.Name + "!"};
         }
    }
}