using System;
using System.Collections.Generic;
using System.Linq;
using HierarchyGrid.Definitions;
using ReactiveUI;
using ReactiveUI.Primitives;
using ReactiveUI.SourceGenerators;
using Splat;

namespace Demo.AvaloniaApplication.ViewModels;

public partial class MainViewModel : ViewModelBase
{
    public HierarchyGridViewModel DemoViewModel { get; } =
        new HierarchyGridViewModel
        {
            SelectionMode = SelectionMode.MultiExtended,
            CellFontSize = 18f,
            HeaderFontSize = 22f,
        };
    public HierarchyGridViewModel TestViewModel { get; } =
        new HierarchyGridViewModel { SelectionMode = SelectionMode.Single };

    // [ObservableAsProperty(ReadOnly = false)]
    // private HierarchyDefinitions _sampleDefinitions;

    public ReactiveCommand<RxVoid, HierarchyDefinitions> BuildSampleDefinitions { get; }
    public ReactiveCommand<RxVoid, HierarchyDefinitions> BuildTestDefinitions { get; }
    public ReactiveCommand<RxVoid, RxVoid> SwitchTestTheme { get; }
    public ReactiveCommand<RxVoid, RxVoid> CycleRowHeights { get; }
    public ReactiveCommand<RxVoid, RxVoid> CycleFontSizes { get; }
    public ReactiveCommand<RxVoid, RxVoid> TransposeGrid { get; }

    public MainViewModel()
    {
        BuildSampleDefinitions = ReactiveCommand.CreateRunInBackground(() =>
        {
            var dg = new DataGenerator();
            return dg.GenerateSample();
        });
        // _sampleDefinitionsHelper = BuildSampleDefinitions.ToProperty(
        //     this,
        //     x => x.SampleDefinitions,
        //     scheduler: RxSchedulers.MainThreadScheduler
        // );

        TransposeGrid = ReactiveCommand.Create(() =>
        {
            DemoViewModel.IsTransposed = !DemoViewModel.IsTransposed;
        });

        BuildTestDefinitions = ReactiveCommand.CreateRunInBackground(
            () => new HierarchyDefinitions(BuildRows(), BuildColumns())
        );

        SwitchTestTheme = ReactiveCommand.Create(() =>
        {
            if (TestViewModel.Theme != HierarchyGridTheme.Default)
                TestViewModel.Theme = HierarchyGridTheme.Default;
            else
                TestViewModel.Theme = new OtherTheme();
        });

        CycleRowHeights = ReactiveCommand.Create(() =>
        {
            var currentHeight =
                TestViewModel.RowsHeights.Count != 0 ? TestViewModel.RowsHeights[0] : 30d;
            if (currentHeight < 50d)
                TestViewModel.SetRowsHeights(currentHeight + 5);
            else
                TestViewModel.SetRowsHeights(20);
        });

        CycleFontSizes = ReactiveCommand.Create(() =>
        {
            var currentSize = TestViewModel.CellFontSize;
            if (currentSize <= 18)
                TestViewModel.SetFontSize(currentSize + 1);
            else
                TestViewModel.SetFontSize(10);
        });

        this.WhenActivated(disposables =>
        {
            BuildSampleDefinitions
                .Subscribe(defs => DemoViewModel.Set(defs))
                .DisposeWith(disposables);
            // ObservableMixins
            //     .WhereNotNull(BuildSampleDefinitions)
            //     .Subscribe(defs =>
            //     {
            //         DemoViewModel.Set(defs);
            //     })
            //     .DisposeWith(disposables);

            ObservableMixins
                .WhereNotNull(BuildTestDefinitions)
                .Subscribe(defs =>
                {
                    TestViewModel.Set(defs);
                })
                .DisposeWith(disposables);
        });
    }

    private IEnumerable<ProducerDefinition> BuildRows()
    {
        return Enumerable
            .Range(0, 20)
            .Select(x =>
            {
                var hpd = new ProducerDefinition
                {
                    Content = x.ToString(),
                    Producer = () => x,
                    IsExpanded = true,
                };

                if (x == 0)
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
                    AddChildRows(hpd, 3);
                return hpd;
            });
    }

    private void AddChildRows(ProducerDefinition parent, int childCount, bool addChild = true)
    {
        for (int i = 0; i < childCount; i++)
        {
            var idx = i;
            var node = parent.Add(
                new ProducerDefinition
                {
                    Content = idx.ToString(),
                    Producer = () => idx,
                    //Qualify = () => idx == 3 ? Qualification.Remark : Qualification.Normal
                }
            );

            if (addChild)
                AddChildRows(node, 4, false);
        }
    }

    private IEnumerable<ConsumerDefinition> BuildColumns()
    {
        return Enumerable
            .Range(0, 10)
            .Select(a =>
            {
                var hdef = new ConsumerDefinition
                {
                    Content = $"Parent {a}",
                    IsExpanded = a != 3,
                    Consumer = o => o is int idx ? (object)(idx * a) : "Oops",
                    Formatter = o => $"Parent: {o}",
                };

                if (a > 1)
                    foreach (
                        var child in Enumerable
                            .Range(0, a)
                            .Select(x =>
                            {
                                var cdef = new ConsumerDefinition
                                {
                                    Content = x.ToString(),
                                    Consumer = o => o is int idx ? (object)(idx + (2 * x)) : "Oops",
                                    Formatter = o => $"Res: {o}",
                                    Qualify = o =>
                                        int.TryParse(o.ToString(), out var i)
                                            ? i switch
                                            {
                                                4 => Qualification.Remark,
                                                6 => Qualification.Warning,
                                                9 => Qualification.Error,
                                                10 => Qualification.ReadOnly,
                                                17 => Qualification.Custom,
                                                18 => Qualification.Custom,
                                                _ => Qualification.Normal,
                                            }
                                            : Qualification.Normal,
                                    Colorize = o =>
                                        int.TryParse(o.ToString(), out var i)
                                            ? i switch
                                            {
                                                17
                                                    => (
                                                        new ThemeColor(150, 100, 120, 0),
                                                        new ThemeColor(255, 0, 0, 0)
                                                    ),
                                                18
                                                    => (
                                                        new ThemeColor(150, 0, 100, 120),
                                                        new ThemeColor(255, 255, 0, 0)
                                                    ),
                                                _
                                                    => (
                                                        new ThemeColor(0, 0, 0, 0),
                                                        new ThemeColor(0, 255, 0, 0)
                                                    ),
                                            }
                                            : (
                                                new ThemeColor(0, 0, 0, 0),
                                                new ThemeColor(0, 0, 0, 0)
                                            ),
                                };

                                switch (x)
                                {
                                    case 3:
                                        cdef.RightDecor = (_, o) =>
                                            o switch
                                            {
                                                int i
                                                    => i % 2 == 0
                                                        ? "Resources/comment.svg"
                                                        : string.Empty,
                                                _ => string.Empty,
                                            };
                                        cdef.Editor = (p, c, s) =>
                                        {
                                            this.Log().Debug($"{p} _ {c} _ {s}");
                                            return !string.IsNullOrWhiteSpace(s);
                                        };
                                        break;
                                    case 5:
                                        cdef.LeftDecor = (_, o) => "Resources/comment.svg";
                                        break;
                                    case 6:
                                        cdef.RightDecor = (_, o) => "Resources/comment.svg";
                                        cdef.LeftDecor = (_, o) => "Resources/edit.svg";
                                        break;
                                }

                                return cdef;
                            })
                    )
                    {
                        hdef.Add(child);
                    }
                else
                    hdef.Frozen = true;

                return hdef;
            });
    }
}
