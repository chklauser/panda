using System;
using System.Collections.Generic;
using System.Runtime.Serialization;
using System.Threading.Tasks;
using ServiceStack.Service;
using ServiceStack.ServiceHost;

namespace Panda.UI
{
    public static class Extensions
    {
        public static bool IsEmpty<T>(this IEnumerable<T> sequence)
        {
            if (sequence == null)
                throw new ArgumentNullException("sequence");
            
            using (var e = sequence.GetEnumerator())
                return !e.MoveNext();
        }

         public static Task<T> GetAsync<T>(this IRestClientAsync serviceClient, IReturn<T> requestMessage)
         {
             if (serviceClient == null)
                 throw new ArgumentNullException("serviceClient");
             var tcs = new TaskCompletionSource<T>();
             serviceClient.GetAsync(requestMessage, tcs.SetResult, 
                 (r, e) => tcs.SetException(e));
             return tcs.Task;
         }

         public static Task<T> PostAsync<T>(this IRestClientAsync serviceClient, IReturn<T> requestMessage)
         {
             if (serviceClient == null)
                 throw new ArgumentNullException("serviceClient");
             var tcs = new TaskCompletionSource<T>();
             serviceClient.PostAsync(requestMessage, tcs.SetResult,
                 (r, e) => tcs.SetException(e));
             return tcs.Task;
         }

         public static Task<T> PutAsync<T>(this IRestClientAsync serviceClient, IReturn<T> requestMessage)
         {
             if (serviceClient == null)
                 throw new ArgumentNullException("serviceClient");
             var tcs = new TaskCompletionSource<T>();
             serviceClient.PutAsync(requestMessage, tcs.SetResult,
                 (r, e) => tcs.SetException(e));
             return tcs.Task;
         }

         public static Task<T> HeadAsync<T>(this IRestClientAsync serviceClient, IReturn<T> requestMessage)
         {
             if (serviceClient == null)
                 throw new ArgumentNullException("serviceClient");
             var tcs = new TaskCompletionSource<T>();
             serviceClient.CustomMethodAsync("HEAD",requestMessage, tcs.SetResult,
                 (r, e) => tcs.SetException(e));
             return tcs.Task;
         }
    }
}