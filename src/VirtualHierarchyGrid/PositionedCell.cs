using HierarchyGrid.Definitions;

namespace VirtualHierarchyGrid
{
    public class PositionedCell
    {
        public ProducerDefinition ProducerDefinition { get; set; }
        public ConsumerDefinition ConsumerDefinition { get; set; }
        public double Top { get; set; }
        public double Left { get; set; }
        public double Height { get; set; }
        public double Width { get; set; }
    }
}
