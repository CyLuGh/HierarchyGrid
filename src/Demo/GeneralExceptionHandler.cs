using System;
using System.Diagnostics;
using ReactiveUI.Primitives.Signals;

namespace Demo;

public class GeneralExceptionHandler : IObserver<Exception>
{
    private readonly Signal<Exception> _alerts = new();
    public IObservable<Exception> Alerts => _alerts;

    public void OnCompleted()
    {
        if (Debugger.IsAttached)
            Debugger.Break();
    }

    public void OnError(Exception error)
    {
        if (Debugger.IsAttached)
            Debugger.Break();
    }

    public void OnNext(Exception value)
    {
        if (Debugger.IsAttached)
            Debugger.Break();

        _alerts.OnNext(value);
    }
}
