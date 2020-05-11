using MoreLinq;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace HierarchyGrid.Definitions
{
    public static class HierarchyDefinitionExtensions
    {
        public static void Invalidate(this IEnumerable<HierarchyDefinition> definitions)
            => definitions.ForEach(hdef => hdef.Invalidate());

        /// <summary>
        /// Returns total elements in hierarchy, according to leaves and folded elements.
        /// </summary>
        /// <param name="ignoreState">If true, will take collapsed element into account. If false, will only take expanded items.</param>
        /// <returns></returns>
        public static int TotalCount(this IEnumerable<HierarchyDefinition> hdefs, bool ignoreState = false)
        {
            int sum = 0;
            foreach (var hdef in hdefs)
                sum += hdef.Count(ignoreState);
            return sum;
        }

        /// <summary>
        /// Returns max layers found in the hierarchy.
        /// </summary>
        /// <param name="ignoreState">If true, will take collapsed element into account. If false, will only take expanded items.</param>
        /// <returns></returns>
        public static int TotalDepth(this IEnumerable<HierarchyDefinition> hdefs, bool ignoreState = true)
            => hdefs?.Any() == true ? hdefs.Max(o => o.Depth(ignoreState)) : 0;

        /// <summary>
        /// Sets size parameter to all elements in the hierarchy.
        /// </summary>
        /// <param name="hdefs"></param>
        /// <param name="size"></param>
        public static void Size(this IEnumerable<HierarchyDefinition> hdefs, double size)
            => hdefs.ForEach(hdef =>
            {
                hdef.Size = size;
                hdef.Children.Size(size);
            });

        /// <summary>
        /// Returns hierarchy definitions on root level.
        /// </summary>
        public static IEnumerable<X> Roots<X>(this IEnumerable<X> hdefs) where X : HierarchyDefinition
            => hdefs.Where(x => x.Parent == null);

        /// <summary>
        /// Returns all elements that are either leaves or folded.
        /// </summary>
        /// <param name="hdefs"></param>
        /// <returns></returns>
        public static IEnumerable<X> Leaves<X>(this IEnumerable<X> hdefs, bool isTrueLeaf = false) where X : HierarchyDefinition
        {
            var leaves = new List<X>();

            if (hdefs != null)
            {
                foreach (var hdef in hdefs.Where(o => o.Frozen))
                {
                    if (!hdef.HasChild || (!isTrueLeaf && !hdef.IsExpanded))
                        leaves.Add(hdef);
                    else
                        leaves.AddRange(hdef.Children.OfType<X>().Leaves());
                }

                foreach (var hdef in hdefs.Where(o => !o.Frozen))
                {
                    if (!hdef.HasChild || (!isTrueLeaf && !hdef.IsExpanded))
                        leaves.Add(hdef);
                    else
                        leaves.AddRange(hdef.Children.OfType<X>().Leaves());
                }
            }

            return leaves;
        }

        /// <summary>
        /// Finds a hierarchy element based on the position in the grid. Its span might be altered.
        /// </summary>
        /// <param name="hdefs"></param>
        /// <param name="position"></param>
        /// <returns></returns>
        public static X At<X>(this LinkedList<X> hdefs, int position) where X : HierarchyDefinition
        {
            foreach (var hdef in hdefs)
            {
                if (position < hdef.Count())
                {
                    hdef.Span = hdef.Count() - position;
                    return hdef;
                }
                else
                    position -= hdef.Count();
            }

            return null;
        }

        /// <summary>
        /// Returns the hierarchy on a single list.
        /// </summary>
        /// <param name="hdefs">Collection of definitions to be flattened.</param>
        /// <param name="includeAll">Whether or not the list should include the children of collapsed elements. True by default.</param>
        /// <returns></returns>
        public static List<X> FlatList<X>(this IEnumerable<X> hdefs, bool includeAll = true) where X : HierarchyDefinition
        {
            var flat = new List<X>();

            foreach (var hdef in hdefs)
            {
                flat.Add(hdef);
                if (includeAll || hdef.IsExpanded)
                    flat.AddRange(hdef.Children.OfType<X>().FlatList(includeAll));
            }

            return flat;
        }

        /// <summary>
        /// Returns the hierarchy on a single list, filtered by level.
        /// </summary>
        /// <param name="hdefs"></param>
        /// <param name="level"></param>
        /// <returns></returns>
        public static LinkedList<X> FlatList<X>(this IEnumerable<X> hdefs, int level) where X : HierarchyDefinition, new()
        {
            var flat = new LinkedList<X>();

            if (level == 0)
            {
                foreach (var hdef in hdefs.Where(o => o.Frozen && o.Level == level))
                    flat.AddLast(hdef);

                foreach (var hdef in hdefs.Where(o => !o.Frozen && o.Level == level))
                    flat.AddLast(hdef);
            }
            else
            {
                foreach (var hdef in hdefs.FlatList(level - 1))
                {
                    if (hdef.HasChild && hdef.IsExpanded)
                    {
                        foreach (var child in hdef.Children.OfType<X>().Where(o => o.Frozen))
                            flat.AddLast(child);

                        foreach (var child in hdef.Children.OfType<X>().Where(o => !o.Frozen))
                            flat.AddLast(child);
                    }
                    else
                    {
                        flat.AddLast(new X { Content = "Dummy" });
                    }
                }
            }

            return flat;
        }
    }
}