using System.Collections.Generic;
using System.Linq;
using LanguageExt;
using MoreLinq;

namespace HierarchyGrid.Definitions;

public static class HierarchyDefinitionExtensions
{
    extension( IEnumerable<HierarchyDefinition> definitions )
    {
        public void Invalidate() => definitions.ForEach( definition => definition.Invalidate() );

        /// <summary>
        /// Returns total elements in hierarchy, according to leaves and folded elements.
        /// </summary>
        /// <param name="ignoreState">If true, will take collapsed element into account. If false, will only take expanded items.</param>
        /// <returns></returns>
        public int TotalCount(
            bool ignoreState = false
        ) => definitions.Sum( definition => definition.Count( ignoreState ) );

        /// <summary>
        /// Returns max layers found in the hierarchy.
        /// </summary>
        /// <param name="ignoreState">If true, will take collapsed element into account. If false, will only take expanded items.</param>
        /// <returns></returns>
        public int TotalDepth(
            bool ignoreState = true
        )
        {
            var hierarchyDefinitions = definitions as HierarchyDefinition[] ?? [.. definitions];
            return hierarchyDefinitions.Length > 0
                ? hierarchyDefinitions.Max( o => o.Depth( ignoreState ) )
                : 0;
        }
    }

    extension<T>( IEnumerable<T> definitions ) where T : HierarchyDefinition
    {
        /// <summary>
        /// Returns hierarchy definitions on root level.
        /// </summary>
        public IEnumerable<T> Roots() =>
            definitions.Select( definition => (T) definition.Root ).Distinct();

        /// <summary>
        /// Returns the hierarchy on a single list.
        /// </summary>
        /// <param name="definitions">Collection of definitions to be flattened.</param>
        /// <param name="includeAll">Whether or not the list should include the children of collapsed elements. True by default.</param>
        /// <returns></returns>
        public Seq<T> FlatList( bool includeAll = true )
        {
            var flat = new Seq<T>();

            foreach ( var definition in definitions )
            {
                flat = flat.Add( definition );

                if ( includeAll || definition.IsExpanded )
                    flat = flat.Append( definition.Children.OfType<T>().FlatList( includeAll ) );
            }

            return flat;
        }

        public int GetPosition( T definition ) =>
            definitions.Leaves().Count( x => x.Position < definition.Position );

        public void ExpandAll()
        {
            foreach ( var definition in definitions )
                definition.ExpandAll();
        }

        public void FoldAll()
        {
            foreach ( var definition in definitions )
                definition.FoldAll();
        }

        /// <summary>
        /// Get definitions position/index on its level
        /// </summary>
        public int GetRelativePosition( T definition )
        {
            if ( definition.Parent is null )
                return definitions
                    .Roots()
                    .Where( d => d.Position < definition.Position )
                    .Sum( d => d.Count() );

            return definition
                    .Parent.Children.Where( d => d.Position < definition.Position )
                    .Sum( d => d.Count() ) + definitions.GetRelativePosition( definition.Parent );
        }
    }

    extension<T>( IEnumerable<T>? definitions ) where T : HierarchyDefinition
    {
        /// <summary>
        /// Returns all elements that are either leaves or folded.
        /// </summary>
        /// <param name="definitions"></param>
        /// <param name="isTrueLeaf">If true, folded elements won't be considered as leaves.</param>
        /// <returns></returns>
        public Seq<T> Leaves( bool isTrueLeaf = false )
        {
            if ( definitions == null )
                return Seq<T>.Empty;

            var leaves = new List<T>();

            var hierarchyDefinitions = definitions as T[] ?? definitions.ToArray();
            foreach ( var definition in hierarchyDefinitions.Where( o => o.Frozen ) )
            {
                if ( !definition.HasChild || ( !isTrueLeaf && !definition.IsExpanded ) )
                    leaves.Add( definition );
                else
                    leaves.AddRange( definition.Children.OfType<T>().Leaves() );
            }

            foreach ( var definition in hierarchyDefinitions.Where( o => !o.Frozen ) )
            {
                if ( !definition.HasChild || ( !isTrueLeaf && !definition.IsExpanded ) )
                    leaves.Add( definition );
                else
                    leaves.AddRange( definition.Children.OfType<T>().Leaves() );
            }

            return leaves.ToSeq();
        }
    }
}
