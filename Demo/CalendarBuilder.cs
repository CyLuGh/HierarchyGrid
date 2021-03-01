using System.Linq;
using System.Collections.Generic;
using HierarchyGrid.Definitions;
using System;
using MoreLinq;

namespace Demo
{
    public class CalendarBuilder
    {
        private string[] _users;

        public CalendarBuilder(params string[] users)
        {
            _users = users;
        }

        public IEnumerable<ProducerDefinition> GetProducers()
            => Enumerable.Range( DateTime.Today.Year - 1 , 3 )
                .Select( year =>
                {
                    var yearlyProducer = new ProducerDefinition { Content = $"{year}" };

                    Enumerable.Range( 1 , 12 )
                        .ForEach( month =>
                        {
                            var monthlyProducer = yearlyProducer.Add( new ProducerDefinition { Content = $"{month}" } );
                            _users?.OrderBy( x => x )
                                .ForEach( user =>
                                {
                                    var userProducer = monthlyProducer.Add( new ProducerDefinition
                                    {
                                        Content = $"{user}" ,
                                        Producer = () => (year, month, user)
                                    } );
                                } );
                            monthlyProducer.IsExpanded = year == DateTime.Today.Year && month == DateTime.Today.Month;
                        } );

                    yearlyProducer.IsExpanded = year == DateTime.Today.Year;
                    return yearlyProducer;
                });

        public IEnumerable<ConsumerDefinition> GetConsumers() 
            => Enumerable.Range(1,31)
                .Select ( day =>
                {
                    var consumer = new ConsumerDefinition
                    {
                        Content = $"{day}",
                        Consumer = o =>
                        {
                            if ( o is ValueTuple<int , int , string> tuple )
                            {
                                var (year, month, user) = tuple;
                                
                                if ( day <= DateTime.DaysInMonth( year , month ) ) 
                                {
                                    var date = new DateTime( year , month , day );
                                    return (date,user);
                                }
                            }

                            return string.Empty;
                        }
                    };

                    return consumer;
                });
    }
}