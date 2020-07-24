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
using LanguageExt;
using Splat;

namespace Demo
{
    /// <summary>
    /// Interaction logic for CanvasWindow.xaml
    /// </summary>
    public partial class CanvasWindow : Window, IEnableLogger
    {
        public CanvasWindow()
        {
            InitializeComponent();
            HierarchyGrid.ViewModel = new HierarchyGridViewModel();
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
                            6 => Qualification.Warning,
                            9 => Qualification.Error,
                            10 => Qualification.ReadOnly,
                            17 => Qualification.Custom,
                            18 => Qualification.Custom,
                            _ => Qualification.Normal
                        } : Qualification.Normal,
                        Colorize = o => int.TryParse(o.ToString(), out var i) ? i switch
                        {
                            17 => ((byte)150, (byte)100, (byte)120, (byte)0),
                            18 => ((byte)150, (byte)0, (byte)100, (byte)120),
                            _ => ((byte)0, (byte)0, (byte)0, (byte)0)
                        } : ((byte)0, (byte)0, (byte)0, (byte)0),
                        Editor = (p, c, s) =>
                        {
                            this.Log().Debug($"{p} _ {c} _ {s}");
                            return !string.IsNullOrWhiteSpace(s);
                        }
                    }))
                    {
                        hdef.Add(child);
                    }
                else
                    hdef.Frozen = true;

                return hdef;
            });
        }

        private void FillButton_Click(object sender, RoutedEventArgs e)
        {
            var dg = new DataGenerator();
            HierarchyGrid.ViewModel.Set(dg.GenerateSample());
        }
    }
}