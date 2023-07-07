using FluentAssertions;
using System;
using System.Linq;
using Xunit;

namespace HierarchyGrid.Definitions.Tests
{
    public class HierarchyDefinitionTests
    {
        [Fact]
        public void TestRelativePosition()
        {
            var root = new ProducerDefinition();

            var child1 = root.Add( new ProducerDefinition() );
            var child2 = root.Add( new ProducerDefinition() );

            child1.RelativePosition.Should().Be( 0 );
            child2.RelativePosition.Should().Be( 1 );

            var child1child1 = child1.Add( new ProducerDefinition() );
            var child1child2 = child1.Add( new ProducerDefinition() );
            var child1child3 = child1.Add( new ProducerDefinition() );
            child1child1.RelativePosition.Should().Be( 0 );
            child1child3.RelativePosition.Should().Be( 2 );
        }

        [Fact]
        public void TestRelativePositionFromRoot()
        {
            var root = new ProducerDefinition();

            var child1 = root.Add( new ProducerDefinition() );
            var child2 = root.Add( new ProducerDefinition() );

            child1.RelativePositionFromRoot.Should().Be( 0 );
            child2.RelativePositionFromRoot.Should().Be( 1 );

            var child1child1 = child1.Add( new ProducerDefinition() );
            var child1child2 = child1.Add( new ProducerDefinition() );
            var child1child3 = child1.Add( new ProducerDefinition() );
            child1child1.RelativePositionFromRoot.Should().Be( 0 );
            child1child3.RelativePositionFromRoot.Should().Be( 2 );
            child2.RelativePositionFromRoot.Should().Be( 1 );
        }

        [Fact]
        public void TestRelativePositionFrom()
        {
            var root = new ProducerDefinition();

            var child1 = root.Add( new ProducerDefinition() );
            var child2 = root.Add( new ProducerDefinition() );

            child1.RelativePositionFrom( root ).Should().Be( 0 );
            child2.RelativePositionFrom( root ).Should().Be( 1 );

            var child1child1 = child1.Add( new ProducerDefinition() );
            var child1child2 = child1.Add( new ProducerDefinition() );
            var child1child3 = child1.Add( new ProducerDefinition() );

            child1child1.RelativePositionFrom( root ).Should().Be( 0 );
            child1child3.RelativePositionFrom( root ).Should().Be( 2 );
            child2.RelativePositionFrom( root ).Should().Be( 3 );
        }

        [Fact]
        public void TestDepth()
        {
            var root = new ProducerDefinition();
            var child1 = root.Add( new ProducerDefinition() );
            var child2 = root.Add( new ProducerDefinition() );
            var child1child1 = child1.Add( new ProducerDefinition() );
            var child1child2 = child1.Add( new ProducerDefinition() );
            var child1child3 = child1.Add( new ProducerDefinition() );

            root.Depth().Should().Be( 3 );
            child1.IsExpanded = false;
            child2.IsExpanded = false;
            root.Depth().Should().Be( 3 );
            root.Depth( false ).Should().Be( 2 );
        }

        [Fact]
        public void TestCount()
        {
            var root = new ProducerDefinition();
            var child1 = root.Add( new ProducerDefinition() );
            var child2 = root.Add( new ProducerDefinition() );
            var child1child1 = child1.Add( new ProducerDefinition() );
            var child1child2 = child1.Add( new ProducerDefinition() );
            var child1child3 = child1.Add( new ProducerDefinition() );

            root.Count().Should().Be( 4 );
            child1.IsExpanded = false;
            root.Count().Should().Be( 2 );
            root.Count( true ).Should().Be( 4 );
        }

        [Fact]
        public void TestLeaves()
        {
            var rootConsumers = new ConsumerDefinition { Content = "Root" };
            rootConsumers.Add( new ConsumerDefinition { Content = "A1" } );
            rootConsumers.Add( new ConsumerDefinition { Content = "B1" } );

            var consumers = new[] { rootConsumers };
            consumers.Leaves().Count().Should().Be( 2 );

            var builder = new CalendarBuilder( "#1" , "#2" , "#3" );
            var producers = builder.GetProducers().ToArray();
            producers.Leaves().Length().Should().Be( 16 );
        }

        [Fact]
        public void TestCompare()
        {
            var rootConsumers1 = new ConsumerDefinition { Content = "Root" };
            rootConsumers1.Add( new ConsumerDefinition { Content = "A1" } );
            var consumer1 = rootConsumers1.Add( new ConsumerDefinition { Content = "B1" } );

            var rootConsumers2 = new ConsumerDefinition { Content = "Root" };
            rootConsumers2.Add( new ConsumerDefinition { Content = "A1" } );
            var consumer2 = rootConsumers2.Add( new ConsumerDefinition { Content = "B1" } );

            consumer1.Equals( consumer2 ).Should().BeFalse();
            consumer1.CompareTo( consumer2 ).Should().Be( 0 );

            var rootProducers = new ProducerDefinition { Content = "Root" };
            rootProducers.Add( new ProducerDefinition { Content = "A1" } );
            var producer = rootProducers.Add( new ProducerDefinition { Content = "B1" } );

            consumer1.CompareTo( producer ).Should().NotBe( 0 );
        }
    }
}