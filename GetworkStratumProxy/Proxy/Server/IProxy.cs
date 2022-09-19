using System;

namespace GetworkStratumProxy.Proxy.Server
{
    public interface IProxy : IDisposable
    {
        public void Start();
        public void Stop();
    }
}
