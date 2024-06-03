using MoreLinq;
using System.Collections.Generic;
using System.Linq;

namespace HierarchyGrid.Definitions;

public static class HierarchyDefinitionExtensions
{
    public static void Invalidate( this IEnumerable<HierarchyDefinition> definitions )
        => definitions.ForEach( hdef => hdef.Invalidate() );

    /// <summary>
    /// Returns total elements in hierarchy, according to leaves and folded elements.
    /// </summary>
    /// <param name="ignoreState">If true, will take collapsed element into account. If false, will only take expanded items.</param>
    /// <returns></returns>
    public static int TotalCount( this IEnumerable<HierarchyDefinition> hdefs , bool ignoreState = false )
    {
        int sum = 0;
        foreach ( var hdef in hdefs )
            sum += hdef.Count( ignoreState );
        return sum;
    }

    /// <summary>
    /// Returns max layers found in the hierarchy.
    /// </summary>
    /// <param name="ignoreState">If true, will take collapsed element into account. If false, will only take expanded items.</param>
    /// <returns></returns>
    public static int TotalDepth( this IEnumerable<HierarchyDefinition> hdefs , bool ignoreState = true )
    {
        var hierarchyDefinitions = hdefs as HierarchyDefinition[] ?? hdefs.ToArray();
        return hierarchyDefinitions?.Any() == true ? hierarchyDefinitions.Max( o => o.Depth( ignoreState ) ) : 0;
    }

    /// <summary>
    /// Sets size parameter to all elements in the hierarchy.
    /// </summary>
    /// <param name="hdefs"></param>
    /// <param name="size"></param>
    public static void Size( this IEnumerable<HierarchyDefinition> hdefs , double size )
        => hdefs.ForEach( hdef =>
        {
            hdef.Size = size;
            hdef.Children.Size( size );
        } );

    /// <summary>
    /// Returns hierarchy definitions on root level.
    /// </summary>
    public static IEnumerable<X> Roots<X>( this IEnumerable<X> hdefs ) where X : HierarchyDefinition
        => hdefs.Select( hdef => (X) hdef.Root ).Distinct();

    /// <summary>
    /// Returns all elements that are either leaves or folded.
    /// </summary>
    /// <param name="hdefs"></param>
    /// <returns></returns>
    public static IEnumerable<T> Leaves<T>( this IEnumerable<T>? hdefs , bool isTrueLeaf = false ) where T : HierarchyDefinition
    {
        var leaves = new List<T>();

        if ( hdefs == null ) return leaves;
        
        var hierarchyDefinitions = hdefs as T[] ?? hdefs.ToArray();
        foreach ( var hdef in hierarchyDefinitions.Where( o => o.Frozen ) )
        {
            if ( !hdef.HasChild || ( !isTrueLeaf && !hdef.IsExpanded ) )
                leaves.Add( hdef );
            else
                leaves.AddRange( hdef.Children.OfType<T>().Leaves() );
        }

        foreach ( var hdef in hierarchyDefinitions.Where( o => !o.Frozen ) )
        {
            if ( !hdef.HasChild || ( !isTrueLeaf && !hdef.IsExpanded ) )
                leaves.Add( hdef );
            else
                leaves.AddRange( hdef.Children.OfType<T>().Leaves() );
        }

        return leaves;
    }

    /// <summary>
    /// Finds a hierarchy element based on the position in the grid. Its span might be altered.
    /// </summary>
    /// <param name="hdefs"></param>
    /// <param name="position"></param>
    /// <returns></returns>
    public static T? At<T>( this LinkedList<T> hdefs , int position ) where T : HierarchyDefinition
    {
        foreach ( var hdef in hdefs )
        {
            if ( position < hdef.Count() )
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
    public static List<T> FlatList<T>( this IEnumerable<T> hdefs , bool includeAll = true ) where T : HierarchyDefinition
    {
        var flat = new List<T>();

        foreach ( var hdef in hdefs )
        {
            flat.Add( hdef );
            if ( includeAll || hdef.IsExpanded )
                flat.AddRange( hdef.Children.OfType<T>().FlatList( includeAll ) );
        }

        return flat;
    }

    /// <summary>
    /// Returns the hierarchy on a single list, filtered by level.
    /// </summary>
    /// <param name="hdefs"></param>
    /// <param name="level"></param>
    /// <returns></returns>
    public static LinkedList<T> FlatList<T>( this IEnumerable<T> hdefs , int level ) where T : HierarchyDefinition, new()
    {
        var flat = new LinkedList<T>();

        if ( level == 0 )
        {
            var hierarchyDefinitions = hdefs as T[] ?? hdefs.ToArray();
            foreach ( var hdef in hierarchyDefinitions.Where( o => o.Frozen && o.Level == level ) )
                flat.AddLast( hdef );

            foreach ( var hdef in hierarchyDefinitions.Where( o => !o.Frozen && o.Level == level ) )
                flat.AddLast( hdef );
        }
        else
        {
            foreach ( var hdef in hdefs.FlatList( level - 1 ) )
            {
                if ( hdef.HasChild && hdef.IsExpanded )
                {
                    foreach ( var child in hdef.Children.OfType<T>().Where( o => o.Frozen ) )
                        flat.AddLast( child );

                    foreach ( var child in hdef.Children.OfType<T>().Where( o => !o.Frozen ) )
                        flat.AddLast( child );
                }
                else
                {
                    flat.AddLast( new T { Content = "Dummy" } );
                }
            }
        }

        return flat;
    }

    public static int GetPosition<T>( this IEnumerable<T> hdefs , T hdef ) where T : HierarchyDefinition
        => hdefs.Leaves().Count( x => x.Position < hdef.Position );

    public static void ExpandAll<T>( this IEnumerable<T> hdefs ) where T : HierarchyDefinition
    {
        foreach ( var hdef in hdefs )
            hdef.ExpandAll();
    }

    public static void FoldAll<T>( this IEnumerable<T> hdefs ) where T : HierarchyDefinition
    {
        foreach ( var hdef in hdefs )
            hdef.FoldAll();
    }
}