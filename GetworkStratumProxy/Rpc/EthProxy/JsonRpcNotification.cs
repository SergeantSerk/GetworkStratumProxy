using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace GetworkStratumProxy.Rpc.EthProxy
{
    public class JsonRpcNotification : JsonRpcResponse
    {
        public JsonRpcNotification()
        {
            Id = 0;
            Error = null;
        }
    }
}
