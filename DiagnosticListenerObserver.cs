namespace DiagnosticsPlayground
{
    using System;
    using System.Collections.Generic;
    using System.Diagnostics;

    public class DiagnosticListenerObserver : IObserver<DiagnosticListener>
    {
        public Dictionary<string, DiagnosticEventObserver> Observers { get; set; } = new Dictionary<string, DiagnosticEventObserver>(StringComparer.Ordinal);

        public void OnCompleted()
        {
            foreach (var pair in Observers)
            {
                pair.Value.OnCompleted();
            }
            Observers.Clear();
        }

        public void OnError(Exception error) => OnCompleted();

        public void OnNext(DiagnosticListener value)
        {
            if (
                !Observers.TryGetValue(value.Name,out var observer) 
                || 
                (observer!=null && observer.IsCompleted) 
            )
            {
                observer = new DiagnosticEventObserver(value);
                Observers[value.Name] = observer;
                value.Subscribe(observer);
            }
        }

    }

}
