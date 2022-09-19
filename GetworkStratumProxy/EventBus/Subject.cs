using System;
using System.Collections.Generic;

namespace GetworkStratumProxy.EventBus
{
    internal class Subject : IObservable<Payload>
    {
        public IList<IObserver<Payload>> Observers { get; set; }

        public Subject()
        {
            Observers = new List<IObserver<Payload>>();
        }

        public IDisposable Subscribe(IObserver<Payload> observer)
        {
            if (!Observers.Contains(observer))
            {
                Observers.Add(observer);
            }
            return new Unsubscriber(observer, Observers);
        }

        public void SendMessage(Payload message)
        {
            foreach (var observer in Observers)
            {
                observer.OnNext(message);
            }
        }
    }
}
