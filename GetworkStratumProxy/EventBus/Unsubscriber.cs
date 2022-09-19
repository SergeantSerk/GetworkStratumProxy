using System;
using System.Collections.Generic;

namespace GetworkStratumProxy.EventBus
{
    internal class Unsubscriber : IDisposable
    {
        private IObserver<Payload> Observer { get; }
        private IList<IObserver<Payload>> Observers { get; }

        public Unsubscriber(IObserver<Payload> observer, IList<IObserver<Payload>> observers)
        {
            Observer = observer;
            Observers = observers;
        }

        public void Dispose()
        {
            if (Observer != null && Observers.Contains(Observer))
            {
                Observers.Remove(Observer);
            }
        }
    }
}
