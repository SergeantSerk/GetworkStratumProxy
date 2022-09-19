using System;

namespace GetworkStratumProxy.Proxy
{
    public interface IProxy : IDisposable
    {
        public void Start();
        public void Stop();
    }
}
