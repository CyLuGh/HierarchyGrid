using System.Collections.Generic;
using System.Linq;
using LanguageExt;
using MoreLinq;

namespace HierarchyGrid.Definitions;

public static class HierarchyDefinitionExtensions
{
    public static void Invalidate(this IEnumerable<HierarchyDefinition> definitions) =>
        definitions.ForEach(definition => definition.Invalidate());

    /// <summary>
    /// Returns total elements in hierarchy, according to leaves and folded elements.
    /// </summary>
    /// <param name="ignoreState">If true, will take collapsed element into account. If false, will only take expanded items.</param>
    /// <returns></returns>
    public static int TotalCount(
        this IEnumerable<HierarchyDefinition> definitions,
        bool ignoreState = false
    ) => definitions.Sum(definition => definition.Count(ignoreState));

    /// <summary>
    /// Returns max layers found in the hierarchy.
    /// </summary>
    /// <param name="ignoreState">If true, will take collapsed element into account. If false, will only take expanded items.</param>
    /// <returns></returns>
    public static int TotalDepth(
        this IEnumerable<HierarchyDefinition> definitions,
        bool ignoreState = true
    )
    {
        var hierarchyDefinitions = definitions as HierarchyDefinition[] ?? definitions.ToArray();
        return hierarchyDefinitions?.Length > 0
            ? hierarchyDefinitions.Max(o => o.Depth(ignoreState))
            : 0;
    }

    /// <summary>
    /// Returns hierarchy definitions on root level.
    /// </summary>
    public static IEnumerable<T> Roots<T>(this IEnumerable<T> definitions)
        where T : HierarchyDefinition =>
        definitions.Select(definition => (T)definition.Root).Distinct();

    /// <summary>
    /// Returns all elements that are either leaves or folded.
    /// </summary>
    /// <param name="definitions"></param>
    /// <param name="isTrueLeaf">If true, folded elements won't be considered as leaves.</param>
    /// <returns></returns>
    public static Seq<T> Leaves<T>(this IEnumerable<T>? definitions, bool isTrueLeaf = false)
        where T : HierarchyDefinition
    {
        if (definitions == null)
            return Seq<T>.Empty;

        var leaves = new List<T>();

        var hierarchyDefinitions = definitions as T[] ?? definitions.ToArray();
        foreach (var definition in hierarchyDefinitions.Where(o => o.Frozen))
        {
            if (!definition.HasChild || (!isTrueLeaf && !definition.IsExpanded))
                leaves.Add(definition);
            else
                leaves.AddRange(definition.Children.OfType<T>().Leaves());
        }

        foreach (var definition in hierarchyDefinitions.Where(o => !o.Frozen))
        {
            if (!definition.HasChild || (!isTrueLeaf && !definition.IsExpanded))
                leaves.Add(definition);
            else
                leaves.AddRange(definition.Children.OfType<T>().Leaves());
        }

        return leaves.ToSeq();
    }

    /// <summary>
    /// Returns the hierarchy on a single list.
    /// </summary>
    /// <param name="definitions">Collection of definitions to be flattened.</param>
    /// <param name="includeAll">Whether or not the list should include the children of collapsed elements. True by default.</param>
    /// <returns></returns>
    public static Seq<T> FlatList<T>(this IEnumerable<T> definitions, bool includeAll = true)
        where T : HierarchyDefinition
    {
        var flat = new Seq<T>();

        foreach (var definition in definitions)
        {
            flat = flat.Add(definition);

            if (includeAll || definition.IsExpanded)
                flat = flat.Append(definition.Children.OfType<T>().FlatList(includeAll));
        }

        return flat;
    }

    public static int GetPosition<T>(this IEnumerable<T> definitions, T definition)
        where T : HierarchyDefinition =>
        definitions.Leaves().Count(x => x.Position < definition.Position);

    public static void ExpandAll<T>(this IEnumerable<T> definitions)
        where T : HierarchyDefinition
    {
        foreach (var definition in definitions)
            definition.ExpandAll();
    }

    public static void FoldAll<T>(this IEnumerable<T> definitions)
        where T : HierarchyDefinition
    {
        foreach (var definition in definitions)
            definition.FoldAll();
    }
}
