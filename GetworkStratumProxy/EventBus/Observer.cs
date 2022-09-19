using GetworkStratumProxy.Rpc.Eth;
using System;

namespace GetworkStratumProxy.EventBus
{
    internal class Observer : IObserver<Payload>
    {
        public EthWork EthWork { get; private set; }

        public void OnCompleted()
        {
        }

        public void OnError(Exception error)
        {
        }

        public void OnNext(Payload value)
        {
            EthWork = value.EthWork;
        }

        public IDisposable Register(Subject subject)
        {
            return subject.Subscribe(this);
        }
    }
}
