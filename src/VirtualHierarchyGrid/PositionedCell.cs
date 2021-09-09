﻿using HierarchyGrid.Definitions;
using System;
using System.Diagnostics.CodeAnalysis;

namespace VirtualHierarchyGrid
{
    public class PositionedCell : IEquatable<PositionedCell>
    {
        public ProducerDefinition ProducerDefinition { get; set; }
        public ConsumerDefinition ConsumerDefinition { get; set; }
        public int HorizontalPosition { get; set; }
        public int VerticalPosition { get; set; }
        public double Top { get; set; }
        public double Left { get; set; }
        public double Height { get; set; }
        public double Width { get; set; }

        public bool Equals( [AllowNull] PositionedCell other )
        {
            if ( other == null )
                return false;

            return ProducerDefinition?.Guid == other.ProducerDefinition?.Guid
                && ConsumerDefinition?.Guid == other.ConsumerDefinition?.Guid;
        }

        public override bool Equals( object obj )
            => Equals( obj as PositionedCell );

        public override int GetHashCode()
            => HashCode.Combine( ProducerDefinition.Guid , ConsumerDefinition.Guid );
    }
}