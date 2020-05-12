using DynamicData;
using HierarchyGrid.Definitions;
using ReactiveUI;
using ReactiveUI.Fody.Helpers;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Reactive;
using System.Reactive.Disposables;
using System.Reactive.Linq;

namespace HierarchyGrid
{
    public class HierarchyGridViewModel : ReactiveObject, IActivatableViewModel
    {
        private SourceCache<ProducerDefinition, int> ProducersCache { get; } = new SourceCache<ProducerDefinition, int>(x => x.Position);
        private SourceCache<ConsumerDefinition, int> ConsumersCache { get; } = new SourceCache<ConsumerDefinition, int>(x => x.Position);

        private ReadOnlyObservableCollection<HierarchyDefinition> _producers;
        private ReadOnlyObservableCollection<HierarchyDefinition> _consumers;

        public ReadOnlyObservableCollection<HierarchyDefinition> Producers => _producers;
        public ReadOnlyObservableCollection<HierarchyDefinition> Consumers => _consumers;

        [Reactive] public bool IsTransposed { get; set; }
        [Reactive] public bool HasNoData { get; set; } = true;
        [Reactive] public int HScrollPos { get; set; }
        [Reactive] public int VScrollPos { get; set; }

        [Reactive] public double ScaleFactor { get; set; } = 1d;
        [Reactive] public bool ColumnsOnly { get; set; }

        public ViewModelActivator Activator { get; }

        internal HierarchyDefinition[] ColumnsElements => (!IsTransposed ? Consumers : Producers).ToArray();

        internal HierarchyDefinition[] RowsElements => (!IsTransposed ? Producers : Consumers).ToArray();

        public ReactiveCommand<int, Unit> VerticalScrollCommand { get; }
        public ReactiveCommand<int, Unit> HorizontalScrollCommand { get; }
        public ReactiveCommand<Unit, Unit> DrawGridCommand { get; }

        public Interaction<int, Unit> VerticalScrollInteraction { get; }
            = new Interaction<int, Unit>(RxApp.MainThreadScheduler);

        public Interaction<int, Unit> HorizontalScrollInteraction { get; }
            = new Interaction<int, Unit>(RxApp.MainThreadScheduler);

        public Interaction<Unit, Unit> DrawGridInteraction { get; }
            = new Interaction<Unit, Unit>(RxApp.MainThreadScheduler);

        public HierarchyGridViewModel()
        {
            Activator = new ViewModelActivator();
            VerticalScrollInteraction.RegisterHandler(ctx => ctx.SetOutput(Unit.Default));
            HorizontalScrollInteraction.RegisterHandler(ctx => ctx.SetOutput(Unit.Default));
            DrawGridInteraction.RegisterHandler(ctx => ctx.SetOutput(Unit.Default));

            DrawGridCommand = ReactiveCommand.CreateFromObservable(() => DrawGridInteraction.Handle(Unit.Default));
            VerticalScrollCommand = ReactiveCommand.CreateFromObservable<int, Unit>(pos => VerticalScrollInteraction.Handle(pos));
            HorizontalScrollCommand = ReactiveCommand.CreateFromObservable<int, Unit>(pos => HorizontalScrollInteraction.Handle(pos));

            this.WhenActivated(disposables =>
            {
                ProducersCache.Connect()
                    .Transform(s => (HierarchyDefinition)s)
                    .ObserveOn(RxApp.MainThreadScheduler)
                    .Bind(out _producers)
                    .DisposeMany()
                    .Subscribe()
                    .DisposeWith(disposables);

                ConsumersCache.Connect()
                    .Transform(s => (HierarchyDefinition)s)
                    .ObserveOn(RxApp.MainThreadScheduler)
                    .Bind(out _consumers)
                    .DisposeMany()
                    .Subscribe()
                    .DisposeWith(disposables);

                ProducersCache.Connect().Select(_ => Unit.Default)
                    .Merge(ConsumersCache.Connect().Select(_ => Unit.Default))
                    .Throttle(TimeSpan.FromMilliseconds(150))
                    .InvokeCommand(DrawGridCommand)
                    .DisposeWith(disposables);

                this.WhenAnyValue(o => o.HScrollPos)
                    //.Throttle(TimeSpan.FromMilliseconds(150))
                    .DistinctUntilChanged()
                    .InvokeCommand(HorizontalScrollCommand)
                    .DisposeWith(disposables);

                this.WhenAnyValue(o => o.VScrollPos)
                    //.Throttle(TimeSpan.FromMilliseconds(150))
                    .DistinctUntilChanged()
                    .InvokeCommand(VerticalScrollCommand)
                    .DisposeWith(disposables);
            });
        }

        public void Set(HierarchyDefinitions hierarchyDefinitions)
        {
            Clear();

            ProducersCache.AddOrUpdate(hierarchyDefinitions.Producers);
            ConsumersCache.AddOrUpdate(hierarchyDefinitions.Consumers);

            CacheLayout(hierarchyDefinitions.Producers, hierarchyDefinitions.Consumers);
        }

        public void Clear()
        {
            HasNoData = true;

            ProducersCache.Clear();
            ConsumersCache.Clear();
        }

        #region Layout cache

        private Dictionary<int, LinkedList<ConsumerDefinition>> ConsumerLevels { get; }
            = new Dictionary<int, LinkedList<ConsumerDefinition>>();

        private Dictionary<int, LinkedList<ProducerDefinition>> ProducerLevels { get; }
            = new Dictionary<int, LinkedList<ProducerDefinition>>();

        public Dictionary<int, LinkedList<HierarchyDefinition>> ColumnLevels
            => GetColumnsLevels();

        public Dictionary<int, LinkedList<HierarchyDefinition>> RowLevels
            => GetRowsLevels();

        public List<ConsumerDefinition> ConsumersFlat { get; private set; }
        public List<ProducerDefinition> ProducersFlat { get; private set; }

        private void CacheLayout(IEnumerable<ProducerDefinition> producers, IEnumerable<ConsumerDefinition> consumers)
        {
            ProducerLevels.Clear();
            ConsumerLevels.Clear();

            ProducersFlat = producers.FlatList();
            foreach (int lvl in ProducersFlat.Select(o => o.Level).Distinct())
                ProducerLevels.Add(lvl, ProducersFlat.FlatList(lvl));

            ConsumersFlat = consumers.FlatList();
            foreach (int lvl in ConsumersFlat.Select(o => o.Level).Distinct())
                ConsumerLevels.Add(lvl, ConsumersFlat.FlatList(lvl));
        }

        private Dictionary<int, LinkedList<HierarchyDefinition>> GetColumnsLevels()
        {
            var dic = new Dictionary<int, LinkedList<HierarchyDefinition>>();

            if (!IsTransposed)
                foreach (var kvp in ConsumerLevels)
                    dic.Add(kvp.Key, new LinkedList<HierarchyDefinition>(kvp.Value));
            else
                foreach (var kvp in ProducerLevels)
                    dic.Add(kvp.Key, new LinkedList<HierarchyDefinition>(kvp.Value));

            return dic;
        }

        private Dictionary<int, LinkedList<HierarchyDefinition>> GetRowsLevels()
        {
            var dic = new Dictionary<int, LinkedList<HierarchyDefinition>>();

            if (!IsTransposed)
                foreach (var kvp in ProducerLevels)
                    dic.Add(kvp.Key, new LinkedList<HierarchyDefinition>(kvp.Value));
            else
                foreach (var kvp in ConsumerLevels)
                    dic.Add(kvp.Key, new LinkedList<HierarchyDefinition>(kvp.Value));

            return dic;
        }

        #endregion Layout cache
    }
}