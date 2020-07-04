using VirtualHierarchyGrid;
using System;
using System.Collections.Generic;
using System.Text;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Shapes;
using HierarchyGrid.Definitions;
using System.Linq;

namespace Demo
{
    /// <summary>
    /// Interaction logic for CanvasWindow.xaml
    /// </summary>
    public partial class CanvasWindow : Window
    {
        public CanvasWindow()
        {
            InitializeComponent();
            HierarchyGrid.ViewModel = new HierarchyGridViewModel();
        }

        private void GridSplitter_DragCompleted(object sender, System.Windows.Controls.Primitives.DragCompletedEventArgs e)
        {
            Console.WriteLine("Test");
        }

        private void Thumb_DragDelta(object sender, System.Windows.Controls.Primitives.DragDeltaEventArgs e)
        {
        }

        private void Thumb_DragStarted(object sender, System.Windows.Controls.Primitives.DragStartedEventArgs e)
        {
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
                var hdef = new ConsumerDefinition { Content = string.Format("Parent {0}", a), IsExpanded = a != 1 };

                foreach (var child in Enumerable.Range(0, 4).Select(x => new ConsumerDefinition
                {
                    Content = x.ToString()
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

        private void FillButton_Click(object sender, RoutedEventArgs e)
        {
            HierarchyGrid.ViewModel.Set(new HierarchyDefinitions(BuildRows(), BuildColumns()));
        }
    }
}