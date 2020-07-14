using HierarchyGrid.Definitions;
using ReactiveUI;
using Splat;
using System;
using System.Linq;
using System.Reactive;
using System.Reactive.Disposables;
using System.Reactive.Linq;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Controls.Primitives;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Navigation;

namespace VirtualHierarchyGrid
{
    public partial class HierarchyGrid : IEnableLogger
    {
        public HierarchyGrid()
        {
            InitializeComponent();

            this.WhenActivated(disposables =>
            {
                this.WhenAnyValue(x => x.ViewModel)
                    .WhereNotNull()
                    .Do(vm => PopulateFromViewModel(this, vm, disposables))
                    .SubscribeSafe()
                    .DisposeWith(disposables);
            });
        }

        private static void PopulateFromViewModel(HierarchyGrid hierarchyGrid, HierarchyGridViewModel vm, CompositeDisposable disposables)
        {
            vm.DrawGridInteraction.RegisterHandler(ctx =>
            {
                vm.Log().Debug("Drawing grid");
                hierarchyGrid.DrawGrid(hierarchyGrid.RenderSize);
                ctx.SetOutput(Unit.Default);
            })
                .DisposeWith(disposables);

            vm.EndEditionInteraction.RegisterHandler(ctx =>
            {
                var textBoxes = hierarchyGrid.HierarchyGridCanvas.Children.OfType<TextBox>().ToArray();
                foreach (var tb in textBoxes)
                {
                    if (tb.Tag is IDisposable d)
                        d.Dispose();
                    hierarchyGrid.HierarchyGridCanvas.Children.Remove(tb);
                }

                ctx.SetOutput(Unit.Default);
            })
                .DisposeWith(disposables);

            vm.EditInteraction.RegisterHandler(ctx =>
            {
                var (rowIdx, colIdx, resultSet) = ctx.Input;

                var cell = hierarchyGrid.HierarchyGridCanvas.Children.OfType<HierarchyGridCell>()
                    .FirstOrDefault(o => o.ViewModel.RowIndex == rowIdx && o.ViewModel.ColumnIndex == colIdx);

                if (cell != null)
                {
                    var textBox = new TextBox();
                    textBox.Width = cell.Width;
                    textBox.Height = cell.Height;
                    Canvas.SetLeft(textBox, Canvas.GetLeft(cell));
                    Canvas.SetTop(textBox, Canvas.GetTop(cell));
                    hierarchyGrid.HierarchyGridCanvas.Children.Add(textBox);

                    textBox.Tag = textBox.Events().KeyDown.Subscribe(e =>
                    {
                        if (e.Key == Key.Enter || e.Key == Key.Return)
                            if (resultSet.Editor.Match(edt => edt(textBox.Text), () => false))
                                Observable.Return(Unit.Default)
                                    .Delay(TimeSpan.FromMilliseconds(50))
                                    .InvokeCommand(vm, x => x.BuildResultSetsCommand);
                        if (e.Key == Key.Escape || e.Key == Key.Enter || e.Key == Key.Return)
                            vm.IsEditing = false;
                    });

                    textBox.Focus();
                }

                ctx.SetOutput(Unit.Default);
            })
                .DisposeWith(disposables);

            hierarchyGrid.Background = (Brush)hierarchyGrid.TryFindResource("GridBackground") ?? Brushes.LightGray;
            hierarchyGrid.Corner.Fill = (Brush)hierarchyGrid.TryFindResource("GridBackground") ?? Brushes.LightGray;

            hierarchyGrid.OneWayBind(vm,
                vm => vm.Scale,
                v => v.ScaleTransform.ScaleX)
                .DisposeWith(disposables);

            hierarchyGrid.OneWayBind(vm,
                vm => vm.Scale,
                v => v.ScaleTransform.ScaleY)
                .DisposeWith(disposables);

            hierarchyGrid.HierarchyGridCanvas.Events()
                .SizeChanged
                .Throttle(TimeSpan.FromMilliseconds(75))
                .ObserveOn(RxApp.MainThreadScheduler)
                .SubscribeSafe(e =>
                {
                    hierarchyGrid.DrawGrid(e.NewSize);
                })
                .DisposeWith(disposables);

            hierarchyGrid.HierarchyGridCanvas.Events()
                .MouseWheel
                .SubscribeSafe(e =>
                {
                    if (Keyboard.IsKeyDown(Key.LeftCtrl) || Keyboard.IsKeyDown(Key.RightCtrl))
                        vm.Scale += .05 * (e.Delta < 0 ? 1 : -1);
                    else if (Keyboard.IsKeyDown(Key.LeftShift) || Keyboard.IsKeyDown(Key.RightShift))
                        vm.HorizontalOffset += 5 * (e.Delta < 0 ? 1 : -1);
                    else
                        vm.VerticalOffset += 5 * (e.Delta < 0 ? 1 : -1);
                })
                .DisposeWith(disposables);

            hierarchyGrid.Bind(vm,
                vm => vm.HorizontalOffset,
                v => v.HorizontalScrollBar.Value,
                hierarchyGrid.HorizontalScrollBar.Events().Scroll,
                vmToViewConverter: i => Convert.ToDouble(i),
                viewToVmConverter: d => Convert.ToInt32(d))
                .DisposeWith(disposables);

            hierarchyGrid.Bind(vm,
                vm => vm.VerticalOffset,
                v => v.VerticalScrollBar.Value,
                hierarchyGrid.VerticalScrollBar.Events().Scroll,
                vmToViewConverter: i => Convert.ToDouble(i),
                viewToVmConverter: d => Convert.ToInt32(d))
                .DisposeWith(disposables);

            hierarchyGrid.OneWayBind(vm,
                vm => vm.MaxHorizontalOffset,
                v => v.HorizontalScrollBar.Maximum)
                .DisposeWith(disposables);

            hierarchyGrid.OneWayBind(vm,
                vm => vm.MaxVerticalOffset,
                v => v.VerticalScrollBar.Maximum)
                .DisposeWith(disposables);
        }
    }
}