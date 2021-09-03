﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Windows.Media;
using HierarchyGrid.Definitions;
using MoreLinq;

namespace Demo
{
    public class DataGenerator
    {
        private static readonly Dictionary<string , string[]> _regions;
        private static readonly Dictionary<string , string[]> _vehicles;

        private Dictionary<(string, string) , int> Data { get; }

        static DataGenerator()
        {
            _regions = new Dictionary<string , string[]>();
            _regions.Add( "Europe" , new[] { "Benelux" , "France" , "Germany" , "United Kingdom" , "Italy" , "Spain" } );
            _regions.Add( "Benelux" , new[] { "Belgium" , "Netherlands" , "Luxemburg" } );
            _regions.Add( "North America" , new[] { "USA" , "Canada" , "Mexico" } );
            _regions.Add( "Asia" , new[] { "Japan" , "China" , "Thailand" , "Korea" } );

            _vehicles = new Dictionary<string , string[]>();
            _vehicles.Add( "Without motors" , new[] { "Bicycles" , "Scooters" } );
            _vehicles.Add( "With motors" , new[] { "Motorbikes" , "Cars" , "Lorries" } );
        }

        public DataGenerator()
        {
            Data = new Dictionary<(string, string) , int>();

            var rnd = new Random( 0 );

            _regions.Values
                .SelectMany( o => o )
                .Where( o => !_regions.Keys.Contains( o ) )
                .ForEach( region =>
                 {
                     _vehicles.Values
                         .SelectMany( o => o )
                         .Where( o => !_vehicles.Keys.Contains( o ) )
                         .ForEach( vehicle => Data.Add( (region, vehicle) , rnd.Next( 10_000_000 ) ) );
                 } );
        }

        private int GetData( string region , string vehicle )
        {
            if ( Data.TryGetValue( (region, vehicle) , out int res ) )
                return res;

            var regions = _regions.TryGetValue( region , out var rs ) ? rs : new[] { region };
            var vehicles = _vehicles.TryGetValue( vehicle , out var vs ) ? vs : new[] { vehicle };

            if ( regions.Length == 1 && vehicles.Length == 1 && regions[0].Equals( region ) && vehicles[0].Equals( vehicle ) )
                return 0;

            return regions.Sum( r => vehicles.Sum( v => GetData( r , v ) ) );
        }

        public HierarchyDefinitions GenerateSample()
            => new HierarchyDefinitions( BuildProducers() , BuildConsumers() );

        private IEnumerable<ProducerDefinition> BuildProducers() => new[] {
            BuildProducer("Europe"),
            BuildProducer("North America"),
            BuildProducer("Asia"),
            };

        private ProducerDefinition BuildProducer( string region )
        {
            var prd = new ProducerDefinition { Content = region };

            if ( _regions.TryGetValue( region , out var innerRegions ) )
            {
                foreach ( var iRegion in innerRegions )
                    prd.Add( BuildProducer( iRegion ) );

                prd.Producer = () => innerRegions;
                prd.IsLocked = true;
            }
            else
            {
                prd.Producer = () => region;
            }

            return prd;
        }

        private IEnumerable<ConsumerDefinition> BuildConsumers() => new[] {
            BuildConsumer( "With motors"),
            BuildConsumer( "Without motors")
        };

        private ConsumerDefinition BuildConsumer( string vehicle )
        {
            var csr = new ConsumerDefinition { Content = vehicle };

            if ( _vehicles.TryGetValue( vehicle , out var vehicles ) )
            {
                foreach ( var vhc in vehicles )
                    csr.Add( BuildConsumer( vhc ) );

                csr.IsLocked = ( _ , __ ) => true;
            }
            else
            {
                csr.Editor = ( data , consumed , input ) =>
                {
                    if ( Data.TryGetValue( ((string) data, vehicle) , out var _ ) && int.TryParse( input , out var newValue ) )
                    {
                        Data[((string) data, vehicle)] = newValue;
                        return true;
                    }
                    return false;
                };
            }

            csr.Consumer = o =>
              o switch
              {
                  string region => GetData( region , vehicle ),
                  string[] regions => regions.Sum( r => GetData( r , vehicle ) ),
                  _ => 0,
              };

            csr.Formatter = o =>
                o switch
                {
                    int i => i.ToString( "N0" ),
                    _ => o.ToString()
                };

            csr.ContextItems = o =>
                o switch
                {
                    string region => new (string description, Action<ResultSet> action)[] {
                        ( $"Show {region}", (ResultSet rs) => {
                            Console.WriteLine(rs.Result);
                        })
                    },
                    _ => new (string description, Action<ResultSet> action)[0]
                };

            csr.Qualify = o =>
                o switch
                {
                    int i => i < 1_000_000 ? Qualification.Custom : Qualification.Normal,
                    _ => Qualification.Normal
                };

            csr.Colorize = o =>
                o switch
                {
                    _ => ((Brushes.LightGray.Color.A, Brushes.LightGray.Color.R, Brushes.LightGray.Color.G, Brushes.LightGray.Color.B),
                    (Brushes.IndianRed.Color.A, Brushes.IndianRed.Color.R, Brushes.IndianRed.Color.G, Brushes.IndianRed.Color.B))
                };

            csr.TooltipCreator = ( p , c ) => $"{p} x {c}";

            return csr;
        }
    }
}