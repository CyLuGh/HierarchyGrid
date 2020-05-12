using HierarchyGrid.Definitions;
using ReactiveUI;
using ReactiveUI.Fody.Helpers;
using System;
using System.Reactive.Disposables;
using System.Reactive.Linq;

namespace HierarchyGrid
{
    public class HierarchyGridCellViewModel : ReactiveObject, IActivatableViewModel
    {
        public ViewModelActivator Activator { get; }

        //[Reactive] public string Content { get; set; }

        public string Result { [ObservableAsProperty]get; }

        [Reactive] public ProducerDefinition Producer { get; set; }
        [Reactive] public ConsumerDefinition Consumer { get; set; }

        public ReactiveCommand<(ProducerDefinition producer, ConsumerDefinition consumer), string> ResolveCommand { get; private set; }

        public HierarchyGridCellViewModel()
        {
            Activator = new ViewModelActivator();

            InitializeCommands(this);

            this.WhenActivated(disposables =>
            {
                this.WhenAnyValue(o => o.Producer).WhereNotNull()
                .CombineLatest(this.WhenAnyValue(o => o.Consumer).WhereNotNull(),
                (p, c) => (p, c))
                .Throttle(TimeSpan.FromMilliseconds(50))
                .InvokeCommand(ResolveCommand)
                .DisposeWith(disposables);

                ResolveCommand.ToPropertyEx(this, x => x.Result)
                    .DisposeWith(disposables);
            });
        }

        private static void InitializeCommands(HierarchyGridCellViewModel @this)
        {
            @this.ResolveCommand = ReactiveCommand.CreateFromObservable<(ProducerDefinition producer, ConsumerDefinition consumer), string>(input
               => Observable.Start(() =>
               {
                   var (producer, consumer) = input;
                   return $"{producer} {consumer}";
               }));
        }
    }
}