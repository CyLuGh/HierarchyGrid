using Avalonia.Controls;
using Avalonia.ReactiveUI;
using Avalonia.Interactivity;
using HierarchyGrid.Definitions;
using ReactiveUI;
using System;
using System.Reactive.Linq;

namespace Demo.Avalonia
{
    public partial class MainWindow : ReactiveWindow<DemoViewModel>
    {
        public MainWindow()
        {
            InitializeComponent();

            SimpleDemoGrid.ViewModel = new HierarchyGridViewModel();

            //Observable.Timer( TimeSpan.FromMilliseconds( 200 ) )
            //    .Select( _ => new HierarchyGridViewModel() )
            //    .ObserveOn( RxApp.MainThreadScheduler )
            //    .Subscribe( vm => SimpleDemoGrid.ViewModel = vm );

            Observable.FromEventPattern<RoutedEventArgs>(
                x => ButtonFillSimple.Click += x ,
                x => ButtonFillSimple.Click -= x )
                .Subscribe( _ =>
                {
                    if ( SimpleDemoGrid.ViewModel != null )
                    {
                        var dg = new DataGenerator();
                        SimpleDemoGrid.ViewModel.Set( dg.GenerateSample() );
                        SimpleDemoGrid.ViewModel.EnableCrosshair = true;
                        SimpleDemoGrid.ViewModel.SelectionMode = HierarchyGrid.Definitions.SelectionMode.MultiExtended;
                    }
                } );
        }
    }
}