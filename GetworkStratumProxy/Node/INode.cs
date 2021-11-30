using System;

namespace GetworkStratumProxy.Node
{
    public interface INode : IDisposable
    {
        public void Start();
        public void Stop();
    }
}
