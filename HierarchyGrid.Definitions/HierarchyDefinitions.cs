using System.Collections.Generic;
using System.Linq;

namespace HierarchyGrid.Definitions
{
    /// <summary>
    /// Represents the default structure of a grid, with rows as producers and columns as consumers.
    /// </summary>
    public class HierarchyDefinitions
    {
        public IEnumerable<ProducerDefinition> Rows { get; set; }
        public IEnumerable<ConsumerDefinition> Columns { get; set; }

        public bool HasDefinitions => Rows?.Any() == true && Columns?.Any() == true;
    }
}