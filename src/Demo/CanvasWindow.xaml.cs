using HierarchyGrid.Definitions;
using LanguageExt;
using ReactiveUI;
using Splat;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Reactive.Linq;
using System.Windows;
using SelectionMode = HierarchyGrid.Definitions.SelectionMode;

namespace Demo
{
    /// <summary>
    /// Interaction logic for CanvasWindow.xaml
    /// </summary>
    public partial class CanvasWindow : Window, IEnableLogger
    {
        private HierarchyGridState _gridState;
        private readonly CalendarBuilder _calendarBuilder;

        public CanvasWindow()
        {
            InitializeComponent();
            _calendarBuilder = new( "#1" , "#2" , "#3" );
            HierarchyGrid.ViewModel = new HierarchyGridViewModel();
            FoldedSampleHierarchyGrid.ViewModel = new HierarchyGridViewModel();
            TestGrid.ViewModel = new HierarchyGridViewModel();
            TestGrid.ViewModel.TextAlignment = CellTextAlignment.Left;
            TestGrid.ViewModel.Set( new HierarchyDefinitions( BuildRows() , BuildColumns() ) );
            TestGrid.ViewModel.SelectionMode = SelectionMode.Single;

            HierarchyGrid.ViewModel.SelectionChanged
                .ObserveOn( RxApp.MainThreadScheduler )
                .Subscribe( selections =>
                {
                    TextBlockSelection.Text = $"Selection count: {selections.Length}";
                } );
        }

        private IEnumerable<ProducerDefinition> BuildRows()
        {
            return Enumerable.Range( 0 , 20 ).Select( x =>
                {
                    var hpd = new ProducerDefinition
                    {
                        Content = x.ToString() ,
                        Producer = () => x ,
                        IsExpanded = true
                    };

                    if ( x == 0 )
                    {
                        //hpd.ContextMenuBuilder = () =>
                        //{
                        //    var mi = new MenuItem { Header = "Freeze" };
                        //    mi.Click += (s, a) => hpd.Freeze(!hpd.Frozen);
                        //    return new[] { mi };
                        //};

                        hpd.Frozen = true;
                    }
                    else
                        AddChildRows( hpd , 3 );
                    return hpd;
                } );
        }

        private void AddChildRows( ProducerDefinition parent , int childCount , bool addChild = true )
        {
            for ( int i = 0 ; i < childCount ; i++ )
            {
                var idx = i;
                var node = parent.Add( new ProducerDefinition
                {
                    Content = idx.ToString() ,
                    Producer = () => idx ,
                    //Qualify = () => idx == 3 ? Qualification.Remark : Qualification.Normal
                } );

                if ( addChild )
                    AddChildRows( node , 4 , false );
            }
        }

        private IEnumerable<ConsumerDefinition> BuildColumns()
        {
            return Enumerable.Range( 0 , 10 ).Select( a =>
                {
                    var hdef = new ConsumerDefinition
                    {
                        Content = string.Format( "Parent {0}" , a ) ,
                        IsExpanded = a != 3 ,
                        Consumer = o => o is int idx ? (object) ( idx * a ) : "Oops" ,
                        Formatter = o => $"Parent: {o}"
                    };

                    if ( a > 1 )
                        foreach ( var child in Enumerable.Range( 0 , a ).Select( x => new ConsumerDefinition
                        {
                            Content = x.ToString() ,
                            Consumer = o => o is int idx ? (object) ( idx + ( 2 * x ) ) : "Oops" ,
                            Formatter = o => $"Res: {o}" ,
                            Qualify = o => int.TryParse( o.ToString() , out var i ) ? i switch
                            {
                                4 => Qualification.Remark,
                                6 => Qualification.Warning,
                                9 => Qualification.Error,
                                10 => Qualification.ReadOnly,
                                17 => Qualification.Custom,
                                18 => Qualification.Custom,
                                _ => Qualification.Normal
                            } : Qualification.Normal ,
                            Colorize = o => int.TryParse( o.ToString() , out var i ) ? i switch
                            {
                                17 => (new ThemeColor( 150 , 100 , 120 , 0 ), new ThemeColor( 255 , 0 , 0 , 0 )),
                                18 => (new ThemeColor( 150 , 0 , 100 , 120 ), new ThemeColor( 255 , 255 , 0 , 0 )),
                                _ => (new ThemeColor( 0 , 0 , 0 , 0 ), new ThemeColor( 0 , 255 , 0 , 0 ))
                            } : (new ThemeColor( 0 , 0 , 0 , 0 ), new ThemeColor( 0 , 0 , 0 , 0 )) ,
                            Editor = ( p , c , s ) =>
                            {
                                this.Log().Debug( $"{p} _ {c} _ {s}" );
                                return !string.IsNullOrWhiteSpace( s );
                            }
                        } ) )
                        {
                            hdef.Add( child );
                        }
                    else
                        hdef.Frozen = true;

                    return hdef;
                } );
        }

        private void FillButton_Click( object sender , RoutedEventArgs e )
        {
            var dg = new DataGenerator();
            HierarchyGrid.ViewModel.Set( dg.GenerateSample() );
            HierarchyGrid.ViewModel.EnableCrosshair = true;
            HierarchyGrid.ViewModel.SelectionMode = SelectionMode.MultiExtended;
        }

        private void FillFoldedGrid_Click( object sender , RoutedEventArgs e )
        {
            var definitions = new HierarchyDefinitions( _calendarBuilder.GetProducers() , _calendarBuilder.GetConsumers() );
            FoldedSampleHierarchyGrid.ViewModel.Set( definitions , true );
            FoldedSampleHierarchyGrid.ViewModel.SetColumnsWidths( 50 );
            FoldedSampleHierarchyGrid.ViewModel.EnableCrosshair = true;
            FoldedSampleHierarchyGrid.ViewModel.TextAlignment = CellTextAlignment.Center;
            FoldedSampleHierarchyGrid.ViewModel.SelectionMode = SelectionMode.MultiSimple;
        }

        private void SaveStateClick( object sender , RoutedEventArgs e )
        {
            _gridState = FoldedSampleHierarchyGrid.ViewModel.GridState;
        }

        private void RestoreStateClick( object sender , RoutedEventArgs e )
        {
            FoldedSampleHierarchyGrid.ViewModel.GridState = _gridState;
        }

        private void DefaultThemeClick( object sender , RoutedEventArgs e )
        {
            TestGrid.ViewModel.Theme = HierarchyGridTheme.Default;
        }

        private void OtherThemeClick( object sender , RoutedEventArgs e )
        {
            TestGrid.ViewModel.Theme = new OtherTheme();
        }
    }
}