using HierarchyGrid.Definitions;
using LanguageExt;
using LanguageExt.Common;
using NLog;
using ReactiveUI;
using ReactiveUI.Fody.Helpers;
using System;
using System.Reactive.Disposables;
using System.Reactive.Linq;
using System.Threading;

namespace HierarchyGrid
{
    public class HierarchyGridCellViewModel : ReactiveObject, IActivatableViewModel
    {
        private static readonly NLog.ILogger _logger = LogManager.GetCurrentClassLogger();

        public ViewModelActivator Activator { get; }

        [Reactive] public Option<string> Result { get; set; }

        [Reactive] public ProducerDefinition Producer { get; set; }
        [Reactive] public ConsumerDefinition Consumer { get; set; }

        public ReactiveCommand<(ProducerDefinition producer, ConsumerDefinition consumer), System.Reactive.Unit> ResolveCommand { get; private set; }

        public HierarchyGridCellViewModel()
        {
            Activator = new ViewModelActivator();

            InitializeCommands(this);

#if DEBUG
            //this.Activator.Activated.Throttle(TimeSpan.FromMilliseconds(20)).SubscribeSafe(_ => _logger.Debug($"ACTIVATED P:{Producer?.Position} C:{Consumer?.Position}"));
            //this.Activator.Deactivated.Throttle(TimeSpan.FromMilliseconds(20)).SubscribeSafe(_ => _logger.Debug($"DEACTIVATED P:{Producer?.Position} C:{Consumer?.Position}"));

#endif

            this.WhenActivated(disposables =>
            {
                this.WhenAnyValue(o => o.Producer).WhereNotNull()
                .CombineLatest(this.WhenAnyValue(o => o.Consumer).WhereNotNull(),
                    this.WhenAnyValue(o => o.Result),
                (prd, cns, res) => (prd, cns, res))
                .Throttle(TimeSpan.FromMilliseconds(5))
                .SubscribeSafe(input => input.res.Match(_ => { }, () => Observable.Return((input.prd, input.cns)).InvokeCommand(ResolveCommand)))
                .DisposeWith(disposables);
            });
        }

        private static void InitializeCommands(HierarchyGridCellViewModel @this)
        {
            @this.ResolveCommand = ReactiveCommand.CreateFromObservable<(ProducerDefinition producer, ConsumerDefinition consumer), System.Reactive.Unit>(input
               => Observable.Start(() =>
               {
                   var (producer, consumer) = input;
                   @this.Result = Option<string>.Some($"{producer} {consumer}");
               }));
        }
    }
}