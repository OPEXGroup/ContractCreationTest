using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Text;
using System.Threading.Tasks;
using Nethereum.JsonRpc.Client;
using Newtonsoft.Json;

namespace ContractCreationTest
{
    public class LoggingRequestInterceptor : RequestInterceptor
    {
        public override async Task InterceptSendRequestAsync(Func<RpcRequest, string, Task> interceptedSendRequestAsync, RpcRequest request, string route = null)
        {
            var id = GetId();
            var sw = Stopwatch.StartNew();
            Log($"{id} Sending request:\n{JsonConvert.SerializeObject(request, Formatting.Indented)}");
            await base.InterceptSendRequestAsync(interceptedSendRequestAsync, request, route);
            Log($"{id} {sw.ElapsedMilliseconds} ms: done");
        }

        public override async Task InterceptSendRequestAsync(Func<string, string, object[], Task> interceptedSendRequestAsync, string method, string route = null,
            params object[] paramList)
        {
            var id = GetId();
            var sw = Stopwatch.StartNew();
            Log($"{id} Sending request with method: {method}");
            await base.InterceptSendRequestAsync(interceptedSendRequestAsync, method, route, paramList);
            Log($"{id} {sw.ElapsedMilliseconds} ms: done");
        }

        public override async Task<object> InterceptSendRequestAsync<T>(Func<RpcRequest, string, Task<T>> interceptedSendRequestAsync, RpcRequest request, string route = null)
        {
            var id = GetId();
            var sw = Stopwatch.StartNew();
            Log($"{id} Sending request:\n{JsonConvert.SerializeObject(request, Formatting.Indented)}");
            var response = await base.InterceptSendRequestAsync(interceptedSendRequestAsync, request, route);
            Log($"{id} {sw.ElapsedMilliseconds} ms: got response:\n{JsonConvert.SerializeObject(response, Formatting.Indented)}");
            return response;
        }

        public override async Task<object> InterceptSendRequestAsync<T>(Func<string, string, object[], Task<T>> interceptedSendRequestAsync, string method, string route = null,
            params object[] paramList)
        {
            var id = GetId();
            var sw = Stopwatch.StartNew();
            Log($"{id} Sending request with method: {method}");
            var response = await base.InterceptSendRequestAsync(interceptedSendRequestAsync, method, route, paramList);
            Log($"{id} {sw.ElapsedMilliseconds} ms: got response:\n{JsonConvert.SerializeObject(response, Formatting.Indented)}");
            return response;
        }

        private static void Log(string message) => Console.WriteLine(message);

        private static string GetId() => $"[{Guid.NewGuid().ToString().Substring(0, 4)}]";
    }
}
