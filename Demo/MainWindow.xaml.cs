using HierarchyGrid.Definitions;
using ReactiveUI;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Reactive.Linq;
using System.Windows;

namespace Demo
{
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindow : Window
    {
        public MainWindow()
        {
            InitializeComponent();

            var viewModel = new HierarchyGrid.HierarchyGridViewModel();
            viewModel.Activator.Activated.SubscribeSafe(_ =>
            {
                Console.WriteLine("ACTIVATED");
            });
            viewModel.Activator.Deactivated.SubscribeSafe(_ =>
            {
                Console.WriteLine("DEACTIVATED");
            });
            HGrid.ViewModel = viewModel;

            var cmd = ReactiveCommand.CreateFromObservable(() => Observable.Start(() =>
            {
                //var rootProducers = new ProducerDefinition { Content = "Root" };
                //rootProducers.Add(new ProducerDefinition { Content = "X1" });
                //rootProducers.Add(new ProducerDefinition { Content = "Y1" });

                //var rootConsumers = new ConsumerDefinition { Content = "Root" };
                //rootConsumers.Add(new ConsumerDefinition { Content = "A1" })
                //    .Add(new ConsumerDefinition { Content = "AA1" });
                //rootConsumers.Add(new ConsumerDefinition { Content = "B1" });
                //var rootConsumers2 = new ConsumerDefinition { Content = "Root2" };
                //rootConsumers2.Add(new ConsumerDefinition { Content = "A2" });
                //rootConsumers2.Add(new ConsumerDefinition { Content = "B2" });
                //rootConsumers2.Add(new ConsumerDefinition { Content = "C2" });
                //rootConsumers2.Add(new ConsumerDefinition { Content = "D2" });

                //var producers = new[] { rootProducers };
                //var consumers = new[] { rootConsumers, rootConsumers2 };

                //viewModel.Set(new HierarchyDefinitions(producers, consumers));

                viewModel.Set(new HierarchyDefinitions(BuildRows(), BuildColumns()));

                Console.WriteLine("test");
            }));
            TestButton.Command = cmd;
        }

        private IEnumerable<ProducerDefinition> BuildRows()
        {
            return Enumerable.Range(0, 20).Select(x =>
            {
                var hpd = new ProducerDefinition
                {
                    Content = x.ToString(),
                    //Producer = () => x,
                    IsExpanded = true
                };

                if (x == 0)
                {
                    //hpd.ContextMenuBuilder = () =>
                    //{
                    //    var mi = new MenuItem { Header = "Freeze" };
                    //    mi.Click += (s, a) => hpd.Freeze(!hpd.Frozen);
                    //    return new[] { mi };
                    //};

                    //hpd.Freeze(true);
                }

                AddChildRows(hpd, x);
                return hpd;
            });
        }

        private void AddChildRows(ProducerDefinition parent, int childCount)
        {
            for (int i = 0; i < childCount; i++)
            {
                var idx = i;
                parent.Add(new ProducerDefinition
                {
                    Content = idx.ToString(),
                    //Producer = () =>
                    //{
                    //    System.Threading.Thread.Sleep(SleepTime);
                    //    return idx;
                    //},
                    //Classification = () => idx == 3 ? CellClassification.Remark : CellClassification.Normal
                });
            }
        }

        private IEnumerable<ConsumerDefinition> BuildColumns()
        {
            return Enumerable.Range(0, 10).Select(a =>
            {
                var hdef = new ConsumerDefinition { Content = string.Format("Parent {0}", a), IsExpanded = true };

                foreach (var child in Enumerable.Range(0, 20).Select(x => new ConsumerDefinition
                {
                    Content = x.ToString(),
                    //Consumer = o =>
                    //{
                    //    return string.Format("R {0} C {1}", o, x);
                    //},
                    //Edit = (o, s) => false
                }))
                {
                    hdef.Add(child);
                    //child.ContextMenuBuilder = o =>
                    //{
                    //    var mi = new MenuItem { Header = "Test menu" };
                    //    return new[] { mi };
                    //};
                    //child.Classification = o =>
                    //{
                    //    if (o.ToString().Equals(child.Content))
                    //        return CellClassification.Warning;
                    //    return CellClassification.Normal;
                    //};
                }

                return hdef;
            });
        }
    }
}