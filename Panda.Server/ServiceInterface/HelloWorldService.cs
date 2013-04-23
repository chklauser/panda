using Panda.ServiceModel;
using ServiceStack.ServiceInterface;

namespace Panda.Server.ServiceInterface
{
    public class HelloService : Service
    {
         public object Any(Hello request)
         {
             return new HelloResponse {Text = "Hello " + request.Name + "!"};
         }
    }
}