using System;
using System.Collections.Generic;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Controls.Primitives;
using System.Windows.Data;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Shapes;
using HierarchyGrid.Definitions;
using HierarchyGrid.Skia;
using LanguageExt;
using ReactiveUI;
using ReactiveUI.Primitives;
using ReactiveUI.Primitives.Disposables;
using ReactiveUI.Primitives.Signals;
using SkiaSharp;
using SkiaSharp.Views.Desktop;
using Splat;
using TextCopy;

namespace HierarchyGrid
{
    public partial class Grid : IEnableLogger
    {
        private readonly ToolTip _tooltip = new();

        public double ScreenScale { get; private set; }

        public Grid()
        {
            InitializeComponent();

            this.WhenActivated(disposables =>
            {
                this.WhenAnyValue(x => x.ViewModel)
                    .BindTo(this, x => x.DataContext)
                    .DisposeWith(disposables);

                this.WhenAnyValue(x => x.ViewModel)
                    .Where(x => x is not null)
                    .Do(vm => PopulateFromViewModel(this, vm, disposables))
                    .Subscribe()
                    .DisposeWith(disposables);
            });
        }

        private static void PopulateFromViewModel(
            Grid view,
            HierarchyGridViewModel viewModel,
            MultipleDisposable disposables
        )
        {
            ApplyDependencyProperties(view, viewModel);

            view.OneWayBind(
                    viewModel,
                    vm => vm.IsCopyingToClipboard,
                    v => v.BorderBusy.Visibility,
                    b => b ? Visibility.Visible : Visibility.Collapsed
                )
                .DisposeWith(disposables);

            viewModel
                .DrawGridInteraction.RegisterHandler(ctx =>
                {
                    view.SkiaElement.InvalidateVisual();
                    DrawSplitters(view, viewModel);
                    ctx.SetOutput(RxVoid.Default);
                })
                .DisposeWith(disposables);

            viewModel
                .FillClipboardInteraction.RegisterHandler(async ctx =>
                {
                    await ClipboardService.SetTextAsync(ctx.Input);
                    ctx.SetOutput(RxVoid.Default);
                })
                .DisposeWith(disposables);

            RegisterToolTipInteractions(view, viewModel, disposables);

            viewModel.DrawEditionTextBoxInteraction.RegisterHandler(ctx =>
            {
                DrawEditingTextBox(view, viewModel, ctx.Input, disposables);
            });

            Signal
                .FromEventPattern<SKPaintSurfaceEventArgs>(
                    handler => view.SkiaElement.PaintSurface += handler,
                    handler => view.SkiaElement.PaintSurface -= handler
                )
                .Subscribe(t =>
                {
                    var args = t.EventArgs;
                    SKImageInfo info = args.Info;
                    SKSurface surface = args.Surface;
                    SKCanvas canvas = surface.Canvas;

                    // Find screen scale
                    PresentationSource? source = PresentationSource.FromVisual(view);
                    view.ScreenScale = source?.CompositionTarget?.TransformToDevice.M11 ?? 1;

                    HierarchyGridDrawer.Draw(
                        viewModel,
                        canvas,
                        info.Width,
                        info.Height,
                        view.ScreenScale
                    );
                })
                .DisposeWith(disposables);

            Signal
                .FromEventPattern<MouseEventHandler, MouseEventArgs>(
                    handler => view.SkiaElement.MouseMove += handler,
                    handler => view.SkiaElement.MouseMove -= handler
                )
                .Subscribe(t =>
                {
                    var position = t.EventArgs.GetPosition(view.SkiaElement);
                    viewModel.HandleMouseOver(
                        position.X,
                        position.Y,
                        viewModel.Scale * view.ScreenScale
                    );
                })
                .DisposeWith(disposables);

            Signal
                .FromEventPattern<MouseEventHandler, MouseEventArgs>(
                    handler => view.SkiaElement.MouseLeave += handler,
                    handler => view.SkiaElement.MouseLeave -= handler
                )
                .Subscribe(_ => viewModel.HandleMouseLeft())
                .DisposeWith(disposables);

            Signal
                .FromEventPattern<MouseButtonEventHandler, MouseButtonEventArgs>(
                    handler => view.SkiaElement.MouseLeftButtonDown += handler,
                    handler => view.SkiaElement.MouseLeftButtonDown -= handler
                )
                .Subscribe(t =>
                {
                    var args = t.EventArgs;
                    var position = args.GetPosition(view.SkiaElement);
                    if (args.ClickCount == 2)
                    {
                        viewModel.HandleDoubleClick(position.X, position.Y, view.ScreenScale);
                    }
                    else
                    {
                        var ctrl =
                            Keyboard.IsKeyDown(Key.LeftCtrl) || Keyboard.IsKeyDown(Key.RightCtrl);
                        var shift =
                            Keyboard.IsKeyDown(Key.LeftShift) || Keyboard.IsKeyDown(Key.RightShift);

                        viewModel.HandleMouseDown(
                            position.X,
                            position.Y,
                            shift,
                            ctrl,
                            screenScale: view.ScreenScale
                        );
                    }

                    args.Handled = true;
                })
                .DisposeWith(disposables);

            Signal
                .FromEventPattern<MouseButtonEventHandler, MouseButtonEventArgs>(
                    handler => view.SkiaElement.MouseRightButtonDown += handler,
                    handler => view.SkiaElement.MouseRightButtonDown -= handler
                )
                .Subscribe(t =>
                {
                    var args = t.EventArgs;

                    var position = args.GetPosition(view.SkiaElement);
                    viewModel.HandleMouseDown(
                        position.X,
                        position.Y,
                        false,
                        false,
                        true,
                        view.ScreenScale
                    );

                    // Show context menu
                    if (viewModel is { IsValid: true, HasData: true })
                    {
                        var contextMenu = BuildContextMenu(
                            viewModel,
                            position.X,
                            position.Y,
                            view.ScreenScale
                        );
                        contextMenu.IsOpen = true;
                    }
                })
                .DisposeWith(disposables);

            Signal
                .FromEventPattern<MouseWheelEventHandler, MouseWheelEventArgs>(
                    handler => view.SkiaElement.MouseWheel += handler,
                    handler => view.SkiaElement.MouseWheel -= handler
                )
                .Subscribe(t =>
                {
                    var e = t.EventArgs;
                    if (Keyboard.IsKeyDown(Key.LeftCtrl) || Keyboard.IsKeyDown(Key.RightCtrl))
                    {
                        var scale = viewModel.Scale + (.05 * (e.Delta < 0 ? 1 : -1));

                        viewModel.Scale = scale switch
                        {
                            < .75 => .75,
                            > 1 => 1,
                            _ => scale
                        };
                    }
                    else if (
                        Keyboard.IsKeyDown(Key.LeftShift) || Keyboard.IsKeyDown(Key.RightShift)
                    )
                    {
                        var ho = viewModel.HorizontalOffset + (5 * (e.Delta < 0 ? 1 : -1));
                        if (ho < 0)
                            viewModel.HorizontalOffset = 0;
                        else if (ho > viewModel.MaxHorizontalOffset)
                            viewModel.HorizontalOffset = viewModel.MaxHorizontalOffset;
                        else
                            viewModel.HorizontalOffset = ho;
                    }
                    else
                    {
                        var vo = viewModel.VerticalOffset + (5 * (e.Delta < 0 ? 1 : -1));
                        if (vo < 0)
                            viewModel.VerticalOffset = 0;
                        else if (vo > viewModel.MaxVerticalOffset)
                            viewModel.VerticalOffset = viewModel.MaxVerticalOffset;
                        else
                            viewModel.VerticalOffset = vo;
                    }
                })
                .DisposeWith(disposables);

            var horizontalScrollSignal = Signal
                .FromEventPattern<ScrollEventHandler, ScrollEventArgs>(
                    handler => view.HorizontalScrollBar.Scroll += handler,
                    handler => view.HorizontalScrollBar.Scroll -= handler
                )
                .Publish()
                .RefCount();

            horizontalScrollSignal.Subscribe().DisposeWith(disposables);

            view.Bind(
                    viewModel,
                    vm => vm.HorizontalOffset,
                    v => v.HorizontalScrollBar.Value,
                    horizontalScrollSignal,
                    viewModelToViewConverter: Convert.ToDouble,
                    viewToViewModelConverter: Convert.ToInt32
                )
                .DisposeWith(disposables);

            var verticalScrollSignal = Signal
                .FromEventPattern<ScrollEventHandler, ScrollEventArgs>(
                    handler => view.VerticalScrollBar.Scroll += handler,
                    handler => view.VerticalScrollBar.Scroll -= handler
                )
                .Publish()
                .RefCount();

            verticalScrollSignal.Subscribe().DisposeWith(disposables);

            view.Bind(
                    viewModel,
                    vm => vm.VerticalOffset,
                    v => v.VerticalScrollBar.Value,
                    verticalScrollSignal,
                    viewModelToViewConverter: Convert.ToDouble,
                    viewToViewModelConverter: Convert.ToInt32
                )
                .DisposeWith(disposables);

            view.OneWayBind(
                    viewModel,
                    vm => vm.MaxHorizontalOffset,
                    v => v.HorizontalScrollBar.Maximum
                )
                .DisposeWith(disposables);

            view.OneWayBind(viewModel, vm => vm.MaxVerticalOffset, v => v.VerticalScrollBar.Maximum)
                .DisposeWith(disposables);

            view.SkiaElement.InvalidateVisual();
        }

        private static void ApplyDependencyProperties(Grid view, HierarchyGridViewModel viewModel)
        {
            viewModel.DefaultColumnWidth = view.DefaultColumnWidth;
            viewModel.DefaultRowHeight = view.DefaultRowHeight;
            viewModel.DefaultHeaderHeight = view.DefaultHeaderHeight;
            viewModel.DefaultHeaderWidth = view.DefaultHeaderWidth;
            viewModel.StatusMessage = view.StatusMessage ?? "No message";
            viewModel.EnableCrosshair = view.EnableCrosshair;
        }

        private static void RegisterToolTipInteractions(
            Grid view,
            HierarchyGridViewModel viewModel,
            MultipleDisposable disposables
        )
        {
            viewModel
                .ShowTooltipInteraction.RegisterHandler(ctx =>
                {
                    view._tooltip.IsOpen = false;

                    var text = string.Join(
                        Environment.NewLine,
                        ctx.Input.ResultSet.TooltipText.Match(text => text, () => string.Empty),
                        viewModel
                            .FocusCells.Find(ctx.Input)
                            .Match(fci => fci.TooltipInfo, () => string.Empty)
                    );

                    if (!string.IsNullOrWhiteSpace(text))
                    {
                        view._tooltip.Content = text.Trim(); /* Trim gets rid of the extra line if one of the text is empty */
                        view._tooltip.Placement = System
                            .Windows
                            .Controls
                            .Primitives
                            .PlacementMode
                            .Mouse;
                        view._tooltip.IsOpen = true;
                    }

                    ctx.SetOutput(RxVoid.Default);
                })
                .DisposeWith(disposables);

            viewModel
                .ShowHeaderTooltipInteraction.RegisterHandler(ctx =>
                {
                    view._tooltip.IsOpen = false;

                    if (!string.IsNullOrWhiteSpace(ctx.Input.Definition.Tooltip))
                    {
                        view._tooltip.Content = ctx.Input.Definition.Tooltip.Trim(); /* Trim gets rid of the extra line if one of the text is empty */
                        view._tooltip.Placement = System
                            .Windows
                            .Controls
                            .Primitives
                            .PlacementMode
                            .Mouse;
                        view._tooltip.IsOpen = true;
                    }

                    ctx.SetOutput(RxVoid.Default);
                })
                .DisposeWith(disposables);

            viewModel
                .CloseTooltipInteraction.RegisterHandler(ctx =>
                {
                    view._tooltip.IsOpen = false;
                    ctx.SetOutput(RxVoid.Default);
                })
                .DisposeWith(disposables);
        }

        private static IEnumerable<MenuItem> BuildCustomItems(
            (string, Action<ResultSet>)[] commands,
            ResultSet resultSet
        )
        {
            var items = new Dictionary<(int, string), MenuItem>();

            foreach (var t in commands)
            {
                var (header, command) = t;
                var splits = header.Split('|');

                if (splits.Length == 1)
                {
                    yield return new MenuItem
                    {
                        Header = header,
                        Command = ReactiveCommand.Create((ResultSet r) => command(r)),
                    };
                }
                else
                {
                    MenuItem? parent = null;
                    for (int i = 0; i < splits.Length; i++)
                    {
                        if (i == splits.Length - 1 && parent != null)
                        {
                            parent.Items.Add(
                                new MenuItem
                                {
                                    Header = splits[i],
                                    Command = ReactiveCommand.Create((ResultSet r) => command(r)),
                                    CommandParameter = resultSet,
                                }
                            );
                        }
                        else
                        {
                            if (items.TryGetValue((0, splits[i]), out var mi))
                            {
                                parent = mi;
                            }
                            else
                            {
                                var menuItem = new MenuItem { Header = splits[i] };
                                if (parent != null)
                                    parent.Items.Add(menuItem);

                                parent = menuItem;
                                items.Add((i, splits[i]), menuItem);
                            }
                        }
                    }
                }
            }

            foreach (var i in items.Values.Where(x => x.Parent == null))
                yield return i;
        }

        private static ContextMenu BuildContextMenu(
            HierarchyGridViewModel viewModel,
            double x,
            double y,
            double screenScale
        )
        {
            var coord = viewModel.FindCoordinates(x, y, screenScale);
            var contextMenu = new ContextMenu();

            var items = coord.Match(
                r =>
                    r.Match(
                        c =>
                            c.ResultSet.ContextCommands.Match(
                                cmds => BuildCustomItems(cmds, c.ResultSet).ToArray(),
                                () => Array.Empty<MenuItem>()
                            ),
                        () => Array.Empty<MenuItem>()
                    ),
                _ => Array.Empty<MenuItem>()
            );

            if (items.Length > 0)
            {
                foreach (var i in items)
                    contextMenu.Items.Add(i);
                contextMenu.Items.Add(new Separator());
            }

            MenuItem highlightsMenuItem = new() { Header = "Highlights" };
            highlightsMenuItem.Items.Add(
                new MenuItem
                {
                    Header = "Enable crosshair",
                    IsChecked = viewModel.EnableCrosshair,
                    IsCheckable = true,
                    Command = viewModel.ToggleCrosshairCommand,
                }
            );
            highlightsMenuItem.Items.Add(
                new MenuItem
                {
                    Header = "Clear highlights",
                    Command = viewModel.ClearHighlightsCommand,
                }
            );

            contextMenu.Items.Add(highlightsMenuItem);

            contextMenu.Items.Add(
                new MenuItem
                {
                    Header = "Clear selection",
                    Command = ReactiveCommand.Create(() => viewModel.SelectedCells.Clear()),
                }
            );

            contextMenu.Items.Add(
                new MenuItem
                {
                    Header = "Expand all",
                    Command = viewModel.ToggleStatesCommand,
                    CommandParameter = true,
                }
            );
            contextMenu.Items.Add(
                new MenuItem
                {
                    Header = "Collapse all",
                    Command = viewModel.ToggleStatesCommand,
                    CommandParameter = false,
                }
            );
            contextMenu.Items.Add(
                new MenuItem
                {
                    Header = "Transpose",
                    IsChecked = viewModel.IsTransposed,
                    IsCheckable = true,
                    Command = viewModel.ToggleTransposeCommand,
                }
            );

            contextMenu.Items.Add(new Separator());

            MenuItem copyMenuItem = new() { Header = "Copy to clipboard" };
            copyMenuItem.Items.Add(
                new MenuItem
                {
                    Header = "with tree structure",
                    Command = viewModel.CopyToClipboardCommand,
                    CommandParameter = CopyMode.Structure,
                }
            );
            copyMenuItem.Items.Add(
                new MenuItem
                {
                    Header = "without tree structure",
                    Command = viewModel.CopyToClipboardCommand,
                    CommandParameter = CopyMode.Flat,
                }
            );
            copyMenuItem.Items.Add(
                new MenuItem
                {
                    Header = "highlighted elements",
                    Command = viewModel.CopyToClipboardCommand,
                    CommandParameter = CopyMode.Highlights,
                }
            );
            copyMenuItem.Items.Add(
                new MenuItem
                {
                    Header = "selection",
                    Command = viewModel.CopyToClipboardCommand,
                    CommandParameter = CopyMode.Selection,
                }
            );
            contextMenu.Items.Add(copyMenuItem);

            return contextMenu;
        }

        private static void DrawEditingTextBox(
            Grid view,
            HierarchyGridViewModel viewModel,
            Seq<PositionedCell> drawnCells,
            MultipleDisposable disposables
        )
        {
            /* Make sure there's no editing textbox when there is no edition */
            if (!viewModel.IsEditing)
            {
                Clear<TextBox>(view);
                return;
            }

            var currentPositionEditedCell =
                from editedCell in viewModel.EditedCell
                from drawnCell in drawnCells.Find(x => x.Equals(editedCell))
                from editor in drawnCell.ResultSet.Editor
                select (drawnCell, editor);

            currentPositionEditedCell
                .Some(t =>
                {
                    var (cell, editor) = t;

                    /* Create or reuse textbox */
                    var textBox = FindUniqueComponent<TextBox>(
                        view,
                        v =>
                        {
                            var tb = new TextBox();

                            var binding = new Binding(nameof(HierarchyGridViewModel.EditionContent))
                            {
                                Source = viewModel,
                                Mode = BindingMode.TwoWay,
                                UpdateSourceTrigger = UpdateSourceTrigger.PropertyChanged,
                            };

                            Signal
                                .FromEventPattern<KeyEventHandler, KeyEventArgs>(
                                    handler => tb.KeyDown += handler,
                                    handler => tb.KeyDown -= handler
                                )
                                .Subscribe(eventPattern =>
                                {
                                    switch (eventPattern.EventArgs.Key)
                                    {
                                        case Key.Escape:
                                            viewModel.EditedCell = Option<PositionedCell>.None;
                                            break;

                                        case Key.Enter:
                                            var content = viewModel.EditionContent;
                                            viewModel.EditedCell = Option<PositionedCell>.None;
                                            Signal
#if DEBUG
                                                .Return((editor(content), "Editor"))
#else
                                                .Return(editor(content))
#endif
                                                .InvokeCommand(viewModel.DrawGridCommand);
                                            break;
                                    }
                                })
                                .DisposeWith(disposables);

                            tb.SetBinding(TextBox.TextProperty, binding);

                            v.Canvas.Children.Add(tb);
                            return tb;
                        }
                    );

                    textBox.Width = cell.Width;
                    textBox.Height = cell.Height;
                    textBox.VerticalContentAlignment = VerticalAlignment.Center;
                    textBox.TextAlignment = TextAlignment.Right;

                    Canvas.SetLeft(textBox, cell.Left);
                    Canvas.SetTop(textBox, cell.Top);

                    textBox.Focus();
                })
                .None(() =>
                {
                    Clear<TextBox>(view);
                });
        }

        private static void DrawSplitters(Grid view, HierarchyGridViewModel viewModel)
        {
            /* Dispose previous resize events */
            foreach (var disposables in viewModel.ResizeObservables)
                disposables.Dispose();

            viewModel.ResizeObservables.Clear();

            var splitters = view.Canvas.Children.OfType<GridSplitter>().ToArray();

            GridSplitter GetSplitter(int idx)
            {
                if (idx < splitters.Length)
                {
                    return splitters[idx];
                }
                else
                {
                    var splitter = new GridSplitter
                    {
                        BorderThickness = new Thickness(2d),
                        BorderBrush = Brushes.Transparent,
                    };
                    view.Canvas.Children.Add(splitter);
                    return splitter;
                }
            }

            int splitterCount = 0;

            var headers = viewModel
                .HeadersCoordinates.Where(x => x.Definition.Definition.Count() == 1)
                .ToArray();

            foreach (var c in headers.Where(t => t.Definition.Definition is ConsumerDefinition))
            {
                var (coord, def) = c;
                var splitter = GetSplitter(splitterCount++);
                splitter.Height = coord.Height * viewModel.Scale;
                splitter.Width = 2;
                splitter.ResizeDirection = GridResizeDirection.Columns;

                var dsp = Signal
                    .FromEventPattern<DragCompletedEventHandler, DragCompletedEventArgs>(
                        handler => splitter.DragCompleted += handler,
                        handler => splitter.DragCompleted -= handler
                    )
                    .Subscribe(eventPattern =>
                    {
                        var args = eventPattern.EventArgs;
                        var pos = viewModel.ColumnsDefinitions.GetPosition(def.Definition);
                        viewModel.ColumnsWidths[pos] = Math.Max(
                            viewModel.ColumnsWidths[pos] + args.HorizontalChange,
                            10d
                        );
                        Clear<Rectangle>(view);
                    });

                viewModel.ResizeObservables.Enqueue(dsp);

                var posX = coord.Right;

                var delta = Signal
                    .FromEventPattern<DragDeltaEventHandler, DragDeltaEventArgs>(
                        handler => splitter.DragDelta += handler,
                        handler => splitter.DragDelta -= handler
                    )
                    .Subscribe(eventPattern =>
                    {
                        var args = eventPattern.EventArgs;
                        Clear<Rectangle>(view);
                        var rect = new Rectangle
                        {
                            Fill = Brushes.DarkSlateGray,
                            Height = coord.Height * viewModel.Scale,
                            Width = 2d,
                        };
                        view.Canvas.Children.Add(rect);

                        Canvas.SetTop(rect, coord.Top * viewModel.Scale);
                        Canvas.SetLeft(rect, (posX + args.HorizontalChange) * viewModel.Scale);
                    });

                viewModel.ResizeObservables.Enqueue(delta);

                Canvas.SetTop(splitter, coord.Top * viewModel.Scale);
                Canvas.SetLeft(splitter, coord.Right * viewModel.Scale);
            }

            foreach (var p in headers.Where(t => t.Definition.Definition is ProducerDefinition))
            {
                var (coord, def) = p;
                var splitter = GetSplitter(splitterCount++);
                splitter.Height = 2;
                splitter.Width = coord.Width * viewModel.Scale;
                splitter.ResizeDirection = GridResizeDirection.Rows;

                var dsp = Signal
                    .FromEventPattern<DragCompletedEventHandler, DragCompletedEventArgs>(
                        handler => splitter.DragCompleted += handler,
                        handler => splitter.DragCompleted -= handler
                    )
                    .Subscribe(eventPattern =>
                    {
                        var args = eventPattern.EventArgs;
                        var pos = viewModel.RowsDefinitions.GetPosition(def.Definition);
                        viewModel.RowsHeights[pos] = Math.Max(
                            viewModel.RowsHeights[pos] + args.VerticalChange,
                            10d
                        );
                        Clear<Rectangle>(view);
                    });

                viewModel.ResizeObservables.Enqueue(dsp);

                var posY = coord.Bottom;

                var delta = Signal
                    .FromEventPattern<DragDeltaEventHandler, DragDeltaEventArgs>(
                        handler => splitter.DragDelta += handler,
                        handler => splitter.DragDelta -= handler
                    )
                    .Subscribe(eventPattern =>
                    {
                        var args = eventPattern.EventArgs;
                        Clear<Rectangle>(view);
                        var rect = new Rectangle
                        {
                            Fill = Brushes.DarkSlateGray,
                            Height = 2d,
                            Width = coord.Width * viewModel.Scale,
                        };
                        view.Canvas.Children.Add(rect);

                        Canvas.SetTop(rect, (posY + args.VerticalChange) * viewModel.Scale);
                        Canvas.SetLeft(rect, coord.Left * viewModel.Scale);
                    });

                viewModel.ResizeObservables.Enqueue(delta);

                Canvas.SetTop(splitter, coord.Bottom * viewModel.Scale);
                Canvas.SetLeft(splitter, coord.Left * viewModel.Scale);
            }

            var currentX = 0d;
            var currentY =
                (
                    viewModel
                        .ColumnsHeadersHeight?.Take(viewModel.ColumnsHeadersHeight.Length - 1)
                        .Sum()
                ) ?? 0d;
            var height = viewModel.ColumnsHeadersHeight?.LastOrDefault(0d) ?? 0d;
            for (int i = 0; i < viewModel.RowsHeadersWidth.Length; i++)
            {
                var currentIndex = i;
                var width = viewModel.RowsHeadersWidth[currentIndex];
                var splitter = GetSplitter(splitterCount++);
                splitter.Height = height * viewModel.Scale;
                splitter.Width = 2;
                splitter.ResizeDirection = GridResizeDirection.Columns;
                currentX += width;

                var dsp = Signal
                    .FromEventPattern<DragCompletedEventHandler, DragCompletedEventArgs>(
                        handler => splitter.DragCompleted += handler,
                        handler => splitter.DragCompleted -= handler
                    )
                    .Do(eventPattern =>
                    {
                        var args = eventPattern.EventArgs;
                        viewModel.RowsHeadersWidth[currentIndex] = Math.Max(
                            viewModel.RowsHeadersWidth[currentIndex] + args.HorizontalChange,
                            10d
                        );
                        Clear<Rectangle>(view);
                    })
#if DEBUG
                    .Select(_ => (false, "Splitter Drag Complete"))
#else
                    .Select(_ => false)
#endif
                    .InvokeCommand(viewModel, x => x.DrawGridCommand);

                viewModel.ResizeObservables.Enqueue(dsp);

                var posX = currentX;

                var delta = Signal
                    .FromEventPattern<DragDeltaEventHandler, DragDeltaEventArgs>(
                        handler => splitter.DragDelta += handler,
                        handler => splitter.DragDelta -= handler
                    )
                    .Subscribe(eventPattern =>
                    {
                        var args = eventPattern.EventArgs;
                        Clear<Rectangle>(view);
                        var rect = new Rectangle
                        {
                            Fill = Brushes.DarkSlateGray,
                            Height = height * viewModel.Scale,
                            Width = 2d,
                        };
                        view.Canvas.Children.Add(rect);

                        Canvas.SetTop(rect, currentY * viewModel.Scale);
                        Canvas.SetLeft(rect, (posX + args.HorizontalChange) * viewModel.Scale);
                    });

                viewModel.ResizeObservables.Enqueue(delta);

                Canvas.SetTop(splitter, currentY * viewModel.Scale);
                Canvas.SetLeft(splitter, currentX * viewModel.Scale);
            }

            var exceeding = splitters.Skip(splitterCount).ToArray();
            Clear(view, exceeding);
        }

        private static void Clear<T>(Grid view)
            where T : UIElement
        {
            foreach (var o in view.Canvas.Children.OfType<T>().ToArray())
                view.Canvas.Children.Remove(o);
        }

        private static void Clear<T>(Grid view, IEnumerable<T> items)
            where T : UIElement
        {
            foreach (var o in items)
                view.Canvas.Children.Remove(o);
        }

        private static T FindUniqueComponent<T>(Grid view, Func<Grid, T> create)
            where T : UIElement
        {
            return view.Canvas.Children.OfType<T>().SingleOrDefault() ?? create(view);
        }
    }
}
