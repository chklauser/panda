using System;
using System.Runtime.Serialization;
using System.Threading.Tasks;
using ServiceStack.Service;
using ServiceStack.ServiceHost;

namespace Panda.UI
{
    public static class Extensions
    {
         public static Task<T> SendAsync<T>(this IServiceClientAsync serviceClient, IReturn<T> requestMessage)
         {
             if (serviceClient == null)
                 throw new ArgumentNullException("serviceClient");
             var tcs = new TaskCompletionSource<T>();
             serviceClient.GetAsync(requestMessage, tcs.SetResult, 
                 (r, e) => tcs.SetException(new RemoteServerException(r,e)));
             return tcs.Task;
         }         
    }

    [Serializable]
    public class RemoteServerException : Exception
    {
        public RemoteServerException(object responseMessage, Exception inner) : base(inner.Message, inner)
        {
            ResponseMessage = responseMessage;
        }

        public object ResponseMessage { get; set; }
    }
}