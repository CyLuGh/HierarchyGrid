using System.Reactive.Disposables;
using System.Reactive.Linq;
using System.Windows.Input;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Controls.Shapes;
using Avalonia.Data;
using Avalonia.Input;
using Avalonia.Media;
using Avalonia.ReactiveUI;
using Avalonia.VisualTree;
using HierarchyGrid.Definitions;
using HierarchyGrid.Skia;
using LanguageExt;
using ReactiveUI;
using SkiaSharp;

namespace HierarchyGrid.Avalonia;

public partial class Grid : ReactiveUserControl<HierarchyGridViewModel>
{
    private readonly Flyout _tooltip;
    private Rectangle _tooltipRectangle;
    private ContextMenu? _contextMenu;

    public Grid()
    {
        InitializeComponent();
        _tooltip = new()
        {
            ShowMode = FlyoutShowMode.Transient,
            OverlayInputPassThroughElement = this
        };

        _tooltipRectangle = new()
        {
            Width = 40,
            Height = 25,
            Fill = Brushes.Transparent,
            IsHitTestVisible = false
        };
        Canvas.Children.Add(_tooltipRectangle);

        this.WhenActivated(disposables =>
        {
            this.WhenAnyValue(x => x.ViewModel)
                .WhereNotNull()
                .Throttle(TimeSpan.FromMilliseconds(50))
                .ObserveOn(RxApp.MainThreadScheduler)
                .Do(vm => PopulateFromViewModel(this, vm, disposables))
                .Subscribe()
                .DisposeWith(disposables);
        });
    }

    private static void PopulateFromViewModel(
        Grid view,
        HierarchyGridViewModel viewModel,
        CompositeDisposable disposables
    )
    {
        ApplyDependencyProperties(view, viewModel);

        viewModel
            .DrawGridInteraction.RegisterHandler(ctx =>
            {
                System.Diagnostics.Debug.WriteLine("DrawGridInteraction");
                view.SkiaElement.Invalidate();
                DrawSplitters(view, viewModel);
                ctx.SetOutput(System.Reactive.Unit.Default);
            })
            .DisposeWith(disposables);

        viewModel
            .DrawEditionTextBoxInteraction.RegisterHandler(ctx =>
            {
                DrawEditingTextBox(view, viewModel, ctx.Input, disposables);
                ctx.SetOutput(System.Reactive.Unit.Default);
            })
            .DisposeWith(disposables);

        viewModel
            .FillClipboardInteraction.RegisterHandler(async ctx =>
            {
                var clipboard = TopLevel.GetTopLevel(view)?.Clipboard;
                if (clipboard is not null)
                {
                    var dataObject = new DataObject();
                    dataObject.Set(DataFormats.Text, ctx.Input);
                    await clipboard.SetDataObjectAsync(dataObject);
                }
                ctx.SetOutput(System.Reactive.Unit.Default);
            })
            .DisposeWith(disposables);

        Observable
            .FromEventPattern<EventHandler<SKPaintSurfaceEventArgs>, SKPaintSurfaceEventArgs>(
                handler => async (sender, args) => await SkiaElement_PaintSurface(args, viewModel),
                handler => view.SkiaElement.PaintSurface += handler,
                handler => view.SkiaElement.PaintSurface -= handler
            )
            .ObserveOn(RxApp.MainThreadScheduler)
            .Subscribe()
            .DisposeWith(disposables);

        Observable
            .FromEventPattern<EventHandler<PointerEventArgs>, PointerEventArgs>(
                handler =>
                    (sender, args) => SkiaElement_PointerMove(args, view.SkiaElement, viewModel),
                handler => view.SkiaElement.PointerMoved += handler,
                handler => view.SkiaElement.PointerMoved -= handler
            )
            .ObserveOn(RxApp.MainThreadScheduler)
            .Subscribe()
            .DisposeWith(disposables);

        Observable
            .FromEventPattern<EventHandler<PointerEventArgs>, PointerEventArgs>(
                handler => (sender, args) => SkiaElement_PointerExit(viewModel),
                handler => view.SkiaElement.PointerExited += handler,
                handler => view.SkiaElement.PointerExited -= handler
            )
            .Subscribe()
            .DisposeWith(disposables);

        Observable
            .FromEventPattern<EventHandler<PointerPressedEventArgs>, PointerPressedEventArgs>(
                handler =>
                    (sender, args) => SkiaElement_PointerPressed(args, view.SkiaElement, viewModel),
                handler => view.SkiaElement.PointerPressed += handler,
                handler => view.SkiaElement.PointerPressed -= handler
            )
            .ObserveOn(RxApp.MainThreadScheduler)
            .Subscribe()
            .DisposeWith(disposables);

        Observable
            .FromEventPattern<EventHandler<PointerWheelEventArgs>, PointerWheelEventArgs>(
                handler => (sender, args) => SkiaElement_PointerWheel(args, viewModel),
                handler => view.SkiaElement.PointerWheelChanged += handler,
                handler => view.SkiaElement.PointerWheelChanged -= handler
            )
            .ObserveOn(RxApp.MainThreadScheduler)
            .Subscribe()
            .DisposeWith(disposables);

        viewModel
            .ShowTooltipInteraction.RegisterHandler(ctx =>
            {
                view.ShowTooltip(ctx.Input);
                ctx.SetOutput(System.Reactive.Unit.Default);
            })
            .DisposeWith(disposables);

        viewModel
            .ShowHeaderTooltipInteraction.RegisterHandler(ctx =>
            {
                view.ShowHeaderTooltip(ctx.Input);
                ctx.SetOutput(System.Reactive.Unit.Default);
            })
            .DisposeWith(disposables);

        viewModel
            .CloseTooltipInteraction.RegisterHandler(ctx =>
            {
                view._tooltip.Hide();
                ctx.SetOutput(System.Reactive.Unit.Default);
            })
            .DisposeWith(disposables);

        view.Bind(
                viewModel,
                vm => vm.HorizontalOffset,
                v => v.HorizontalScrollBar.Value,
                vmToViewConverter: i => Convert.ToDouble(i),
                viewToVmConverter: d => Convert.ToInt32(d)
            )
            .DisposeWith(disposables);

        view.Bind(
                viewModel,
                vm => vm.VerticalOffset,
                v => v.VerticalScrollBar.Value,
                vmToViewConverter: i => Convert.ToDouble(i),
                viewToVmConverter: d => Convert.ToInt32(d)
            )
            .DisposeWith(disposables);

        view.OneWayBind(viewModel, vm => vm.MaxHorizontalOffset, v => v.HorizontalScrollBar.Maximum)
            .DisposeWith(disposables);

        view.OneWayBind(viewModel, vm => vm.MaxVerticalOffset, v => v.VerticalScrollBar.Maximum)
            .DisposeWith(disposables);

        view.SkiaElement.Invalidate();
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

    private static async Task SkiaElement_PaintSurface(
        SKPaintSurfaceEventArgs args,
        HierarchyGridViewModel viewModel
    )
    {
        SKImageInfo info = args.Info;
        SKSurface surface = args.Surface;
        SKCanvas canvas = surface.Canvas;

        var scale = 1d;
        await HierarchyGridDrawer.Draw(viewModel, canvas, info.Width, info.Height, scale, false);
    }

    private static void SkiaElement_PointerMove(
        PointerEventArgs args,
        SKXamlCanvas element,
        HierarchyGridViewModel viewModel
    )
    {
        var position = args.GetPosition(element);
        viewModel.HandleMouseOver(position.X, position.Y, 1);
    }

    private static void SkiaElement_PointerExit(HierarchyGridViewModel viewModel)
    {
        viewModel.HandleMouseLeft();
    }

    private static void SkiaElement_PointerWheel(
        PointerWheelEventArgs args,
        HierarchyGridViewModel viewModel
    )
    {
        var delta = args.Delta.Y;

        if (args.KeyModifiers.HasFlag(KeyModifiers.Control))
            viewModel.Scale += .05 * (delta < 0 ? 1 : -1);
        else if (args.KeyModifiers.HasFlag(KeyModifiers.Shift))
            viewModel.HorizontalOffset += 5 * (delta < 0 ? 1 : -1);
        else
            viewModel.VerticalOffset += 5 * (delta < 0 ? 1 : -1);

        args.Handled = true;
    }

    private static void SkiaElement_PointerPressed(
        PointerPressedEventArgs args,
        SKXamlCanvas element,
        HierarchyGridViewModel viewModel
    )
    {
        var position = args.GetPosition(element);
        var point = args.GetCurrentPoint(element);

        if (point.Properties.IsLeftButtonPressed)
        {
            if (args.ClickCount == 2)
            {
                viewModel.HandleDoubleClick(position.X, position.Y, 1);
            }
            else
            {
                var ctrl = args.KeyModifiers.HasFlag(KeyModifiers.Control);
                var shift = args.KeyModifiers.HasFlag(KeyModifiers.Shift);

                viewModel.HandleMouseDown(position.X, position.Y, shift, ctrl, screenScale: 1);
            }
        }
        else
        {
            viewModel.HandleMouseDown(
                position.X,
                position.Y,
                isShiftPressed: false,
                isCtrlPressed: false,
                isRightClick: true,
                screenScale: 1
            );

            // Show context menu
            if (viewModel.IsValid && viewModel.HasData)
            {
                var view = (args.Source as Visual).FindAncestorOfType<Grid>();
                if (view is not null)
                {
                    view._contextMenu?.Close();
                    view._contextMenu = BuildContextMenu(viewModel, position.X, position.Y, 1);
                    view._contextMenu.Open(element);
                }
            }
        }

        args.Handled = true;
    }

    private void ShowTooltip(PositionedCell pCell)
    {
        _tooltip.Hide();

        if (ViewModel is null || pCell.ResultSet == ResultSet.Default)
            return;

        var text = string.Join(
            Environment.NewLine,
            pCell.ResultSet.TooltipText.Match(text => text, () => string.Empty),
            ViewModel.FocusCells.Find(pCell).Match(fci => fci.TooltipInfo, () => string.Empty)
        );

        if (!string.IsNullOrWhiteSpace(text))
        {
            _tooltipRectangle.Width = pCell.Width - 6;
            _tooltipRectangle.Height = pCell.Height - 6;
            Canvas.SetLeft(_tooltipRectangle, pCell.Left + 3);
            Canvas.SetTop(_tooltipRectangle, pCell.Top + 3);

            _tooltip.Content = text.Trim();
            _tooltip.Placement = PlacementMode.Bottom;
            _tooltip.ShowAt(_tooltipRectangle);
        }
    }

    private void ShowHeaderTooltip(PositionedDefinition pDefinition)
    {
        _tooltip.Hide();

        if (ViewModel is null)
            return;

        var text = pDefinition.Definition.Tooltip;

        if (!string.IsNullOrWhiteSpace(text))
        {
            _tooltipRectangle.Width = pDefinition.Coordinates.Width - 6;
            _tooltipRectangle.Height = pDefinition.Coordinates.Height - 6;
            Canvas.SetLeft(_tooltipRectangle, pDefinition.Coordinates.Left + 3);
            Canvas.SetTop(_tooltipRectangle, pDefinition.Coordinates.Top + 3);

            _tooltip.Content = text.Trim();
            _tooltip.Placement =
                pDefinition.Definition is ConsumerDefinition
                    ? PlacementMode.Bottom
                    : PlacementMode.Right;
            _tooltip.ShowAt(_tooltipRectangle);
        }
    }

    private static IEnumerable<MenuItem> BuildCustomItems(
        (string, ReactiveCommand<ResultSet, System.Reactive.Unit>)[] commands,
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
                    Command = command,
                    CommandParameter = resultSet
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
                                Command = command,
                                CommandParameter = resultSet
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
                //IsChecked = viewModel.EnableCrosshair ,
                //IsCheckable = true ,
                Command = viewModel.ToggleCrosshairCommand
            }
        );
        highlightsMenuItem.Items.Add(
            new MenuItem { Header = "Clear highlights", Command = viewModel.ClearHighlightsCommand }
        );

        contextMenu.Items.Add(highlightsMenuItem);

        contextMenu.Items.Add(
            new MenuItem
            {
                Header = "Clear selection",
                Command = ReactiveCommand.Create(() => viewModel.SelectedCells.Clear())
            }
        );

        contextMenu.Items.Add(
            new MenuItem
            {
                Header = "Expand all",
                Command = viewModel.ToggleStatesCommand,
                CommandParameter = true
            }
        );
        contextMenu.Items.Add(
            new MenuItem
            {
                Header = "Collapse all",
                Command = viewModel.ToggleStatesCommand,
                CommandParameter = false
            }
        );

        contextMenu.Items.Add(new Separator());

        MenuItem copyMenuItem = new() { Header = "Copy to clipboard" };
        copyMenuItem.Items.Add(
            new MenuItem
            {
                Header = "with tree structure",
                Command = viewModel.CopyToClipboardCommand,
                CommandParameter = CopyMode.Structure
            }
        );
        copyMenuItem.Items.Add(
            new MenuItem
            {
                Header = "without tree structure",
                Command = viewModel.CopyToClipboardCommand,
                CommandParameter = CopyMode.Flat
            }
        );
        copyMenuItem.Items.Add(
            new MenuItem
            {
                Header = "highlighted elements",
                Command = viewModel.CopyToClipboardCommand,
                CommandParameter = CopyMode.Highlights
            }
        );
        copyMenuItem.Items.Add(
            new MenuItem
            {
                Header = "selection",
                Command = viewModel.CopyToClipboardCommand,
                CommandParameter = CopyMode.Selection
            }
        );
        contextMenu.Items.Add(copyMenuItem);

        return contextMenu;
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
                    Opacity = 0
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
            splitter.Height = coord.Height;
            splitter.Width = 2;
            splitter.ResizeDirection = GridResizeDirection.Columns;

            var dsp = Observable
                .FromEventPattern<EventHandler<VectorEventArgs>, VectorEventArgs>(
                    handler =>
                        (sender, args) => Splitter_DragComplete(args, viewModel, def.Definition),
                    handler => splitter.DragCompleted += handler,
                    handler => splitter.DragCompleted -= handler
                )
                .Subscribe();
            viewModel.ResizeObservables.Enqueue(dsp);

            Canvas.SetTop(splitter, coord.Top);
            Canvas.SetLeft(splitter, coord.Right - 2);
        }

        var currentX = 0d;
        var currentY =
            (viewModel.ColumnsHeadersHeight?.Take(viewModel.ColumnsHeadersHeight.Length - 1).Sum())
            ?? 0d;
        var height = viewModel.ColumnsHeadersHeight?.LastOrDefault(0d) ?? 0d;

        for (int i = 0; i < viewModel.RowsHeadersWidth?.Length; i++)
        {
            var currentIndex = i;
            var width = viewModel.RowsHeadersWidth[currentIndex];
            var splitter = GetSplitter(splitterCount++);
            splitter.Height = height;
            splitter.Width = 2;
            splitter.ResizeDirection = GridResizeDirection.Columns;
            currentX += width;

            var dsp = Observable
                .FromEventPattern<EventHandler<VectorEventArgs>, VectorEventArgs>(
                    handler =>
                        (sender, args) =>
                            Splitter_Header_DragComplete(args, viewModel, currentIndex),
                    handler => splitter.DragCompleted += handler,
                    handler => splitter.DragCompleted -= handler
                )
                .Subscribe();
            viewModel.ResizeObservables.Enqueue(dsp);

            Canvas.SetTop(splitter, currentY);
            Canvas.SetLeft(splitter, currentX - 2);
        }

        var exceeding = splitters.Skip(splitterCount).ToArray();
        Clear(view, exceeding);
    }

    private static void Splitter_DragComplete(
        VectorEventArgs args,
        HierarchyGridViewModel viewModel,
        HierarchyDefinition definition
    )
    {
        var pos = viewModel.ColumnsDefinitions.GetPosition(definition);
        viewModel.ColumnsWidths[pos] = Math.Max(viewModel.ColumnsWidths[pos] + args.Vector.X, 10d);

        Observable
            .Return(false)
            .Delay(TimeSpan.FromMilliseconds(100))
            .InvokeCommand(viewModel, x => x.DrawGridCommand);
    }

    private static void Splitter_Header_DragComplete(
        VectorEventArgs args,
        HierarchyGridViewModel viewModel,
        int currentIndex
    )
    {
        viewModel.RowsHeadersWidth[currentIndex] = Math.Max(
            viewModel.RowsHeadersWidth[currentIndex] + args.Vector.X,
            10d
        );

        Observable
            .Return(false)
            .Delay(TimeSpan.FromMilliseconds(100))
            .InvokeCommand(viewModel, x => x.DrawGridCommand);
    }

    private static void EditorKeyDown(
        TextBox tb,
        KeyEventArgs args,
        HierarchyGridViewModel viewModel,
        Func<string, bool> editor
    )
    {
        switch (args.Key)
        {
            case Key.Escape:
                viewModel.EditedCell = Option<PositionedCell>.None;
                break;

            case Key.Enter:
                var content = viewModel.EditionContent;
                viewModel.EditedCell = Option<PositionedCell>.None;
                Observable
                    .Return(editor(content ?? string.Empty))
                    .InvokeCommand(viewModel.DrawGridCommand);
                break;
        }
    }

    private static void DrawEditingTextBox(
        Grid view,
        HierarchyGridViewModel viewModel,
        Seq<PositionedCell> drawnCells,
        CompositeDisposable disposables
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

                        var binding = new Binding
                        {
                            Source = viewModel,
                            Mode = BindingMode.TwoWay,
                            Path = nameof(HierarchyGridViewModel.EditionContent)
                        };

                        Observable
                            .FromEventPattern<EventHandler<KeyEventArgs>, KeyEventArgs>(
                                handler =>
                                    (sender, args) => EditorKeyDown(tb, args, viewModel, editor),
                                handler => tb.KeyDown += handler,
                                handler => tb.KeyDown -= handler
                            )
                            .Subscribe()
                            .DisposeWith(disposables);

                        tb.Bind(TextBox.TextProperty, binding).DisposeWith(disposables);

                        v.Canvas.Children.Add(tb);
                        return tb;
                    }
                );

                textBox.Width = cell.Width;
                textBox.Height = cell.Height;
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

    private static void Clear<T>(Grid view)
        where T : Control
    {
        foreach (var o in view.Canvas.Children.OfType<T>().ToArray())
            view.Canvas.Children.Remove(o);
    }

    private static void Clear<T>(Grid view, IEnumerable<T> items)
        where T : Control
    {
        foreach (var o in items)
            view.Canvas.Children.Remove(o);
    }

    private static T FindUniqueComponent<T>(Grid view, Func<Grid, T> create)
        where T : Control
    {
        return view.Canvas.Children.OfType<T>().SingleOrDefault() ?? create(view);
    }
}
