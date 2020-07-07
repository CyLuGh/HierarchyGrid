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
                var node = parent.Add(new ProducerDefinition
                {
                    Content = idx.ToString(),
                    Producer = () => idx,
                    //Classification = () => idx == 3 ? CellClassification.Remark : CellClassification.Normal
                });

                if (addChild)
                    AddChildRows(node, 4, false);
            }
        }

        private IEnumerable<ConsumerDefinition> BuildColumns()
        {
            return Enumerable.Range(0, 10).Select(a =>
            {
                var hdef = new ConsumerDefinition
                {
                    Content = string.Format("Parent {0}", a),
                    IsExpanded = a != 3,
                    Consumer = o => o is int idx ? (object)(idx * a) : "Oops",
                    Formatter = o => $"Parent: {o}"
                };

                if (a > 1)
                    foreach (var child in Enumerable.Range(0, a).Select(x => new ConsumerDefinition
                    {
                        Content = x.ToString(),
                        Consumer = o => o is int idx ? (object)(idx + (2 * x)) : "Oops",
                        Formatter = o => $"Res: {o}",
                        Qualify = o => int.TryParse(o.ToString(), out var i) ? i switch
                        {
                            4 => Qualification.Remark,
                            5 => Qualification.Warning,
                            9 => Qualification.Error,
                            _ => Qualification.Normal
                        } : Qualification.Normal
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
                else
                    hdef.Frozen = true;

                return hdef;
            });
        }

        private void FillButton_Click(object sender, RoutedEventArgs e)
        {
            HierarchyGrid.ViewModel.Set(new HierarchyDefinitions(BuildRows(), BuildColumns()));
        }
    }
}