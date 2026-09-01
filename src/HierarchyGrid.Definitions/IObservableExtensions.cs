using System;
using ReactiveUI.Primitives;

namespace HierarchyGrid.Definitions;

// From cheatsheet: https://github.com/cabauman/Rx.Net-ReactiveUI-CheatSheet#sample-projects-1
public static class IObservableExtensions
{
    /// <summary>
    /// Convenience method for Select(_ => Unit.Default).
    /// </summary>
    // Credit: Kent Boogaart
    public static IObservable<RxVoid> ToSignal<T>(this IObservable<T> @this)
    {
        return LinqExtensions.Select(@this, _ => RxVoid.Default);
    }
}
