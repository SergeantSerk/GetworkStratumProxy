﻿using GetworkStratumProxy.Node;
using System;
using System.Net.Sockets;

namespace GetworkStratumProxy.Proxy
{
    public class NicehashProxy : BaseProxy
    {
        public override bool IsListening { get; protected set; }
        protected override TcpListener Server { get; set; }

        public NicehashProxy(BaseNode node) : base(node)
        {
            throw new NotImplementedException();
        }

        protected override void HandleTcpClient(IAsyncResult ar)
        {
            throw new NotImplementedException();
        }
    }
}