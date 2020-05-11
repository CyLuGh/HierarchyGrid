using FluentAssertions;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Xunit;

namespace HierarchyGrid.Definitions.Tests
{
    public class HierarchyDefinitionsTests
    {
        [Fact]
        public void TestBuild()
        {
            var rootProducers = new ProducerDefinition { Content = "Root" };
            var rootConsumers = new ConsumerDefinition { Content = "Root" };
            rootConsumers.Add(new ConsumerDefinition { Content = "A1" })
                .Add(new ConsumerDefinition { Content = "AA1" });
            rootConsumers.Add(new ConsumerDefinition { Content = "B1" });
            var rootConsumers2 = new ConsumerDefinition { Content = "Root2" };
            rootConsumers2.Add(new ConsumerDefinition { Content = "A2" });
            rootConsumers2.Add(new ConsumerDefinition { Content = "B2" });
            rootConsumers2.Add(new ConsumerDefinition { Content = "C2" });
            rootConsumers2.Add(new ConsumerDefinition { Content = "D2" });

            var producers = new[] { rootProducers };
            var consumers = new[] { rootConsumers, rootConsumers2 };

            var definitions = new HierarchyDefinitions(producers, consumers);

            definitions.Consumers.Count.Should().Be(2);
            definitions.Consumers.FlatList().Count.Should().Be(9);
            definitions.Consumers.FlatList().Select(x => x.Position).Distinct().Count().Should().Be(9);
        }
    }
}