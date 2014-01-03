using Lucene.Net.Linq.Fluent;
using Lucene.Net.Linq.Mapping;
using Lucene.Net.Store;
using NUnit.Framework;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Text;

namespace Lucene.Net.Linq.Tests.Samples
{
    [TestFixture]
    public class NumericDateTimeTest
    {
        [Test]
        public void FluentMappingsWithNumericFields_ShouldUseCustomTypeConverterForBothAddsAndQueries()
        {
            /*   This dosen't work. It seems that the converter is used for addinng the document but 
             *   custom converter isn't used for the parsing when querying (DateTimeConverter is)
             *   
             *    DateTimeConverter.ConvertFrom... --> FormatException: String was not recognized as a valid DateTime
             *    (and why string? shouldn't it have been saved as a long/int64? (being numerical field))
             */
            var luceneVersion = Lucene.Net.Util.Version.LUCENE_30;

            var map = new ClassMap<Person1>(luceneVersion);
            map.Property(p => p.Id).Analyzed().Stored();
            map.Property(p => p.Birthdate).AsNumericField().ConvertWith(new CustomNumericalDateTimeTypeConverter());

            // Create index and add a document
            using (Directory indexDir = new RAMDirectory())
            {
                var provider = new LuceneDataProvider(indexDir, luceneVersion);

                using (var session = provider.OpenSession<Person1>(map.ToDocumentMapper()))
                {
                    session.Add(new Person1()
                    {
                        Id = "1111111111",
                        Birthdate = new DateTime(2013, 1, 2)
                    });
                }

                var persons = provider.AsQueryable<Person1>();

                var personsWithCpr = from p in persons
                                     where p.Id == "1111111111"
                                     select p;

                Assert.AreEqual(1, personsWithCpr.Count(), "number of persons found");
                Assert.AreEqual("1111111111", personsWithCpr.First().Id, "Cpr of the first person");
                Assert.AreEqual(new DateTime(2013, 1, 2), personsWithCpr.First().Birthdate, "birthdate of the first person");
            }
        }

        public class Person1
        {
            public string Id { get; set; }

            public DateTime Birthdate { get; set; }
        }

        [Test]
        public void AttributeMappingsOfNumericField_ShouldUseCustomTypeConverterForBothAddsAndQueries()
        {
            /*
             *  Seems to work
             */
            var luceneVersion = Lucene.Net.Util.Version.LUCENE_30;

            // Create index and add a document
            using (Directory indexDir = new RAMDirectory())
            {
                var provider = new LuceneDataProvider(indexDir, luceneVersion);

                using (var session = provider.OpenSession<Person>())
                {
                    session.Add(new Person()
                    {
                        Id = "1111111111",
                        Birthdate = new DateTime(2013, 1, 2)
                    });
                }

                var persons = provider.AsQueryable<Person>();

                var personsWithCpr = from p in persons
                                     where p.Id == "1111111111"
                                     select p;

                Assert.AreEqual(1, personsWithCpr.Count(), "number of persons found");
                Assert.AreEqual("1111111111", personsWithCpr.First().Id, "Cpr of the first person");
                Assert.AreEqual(new DateTime(2013, 1, 2), personsWithCpr.First().Birthdate, "birthdate of the first person");
            }
        }

        public class Person
        {
            [Field()]
            public string Id { get; set; }

            [NumericField(Converter = typeof(CustomNumericalDateTimeTypeConverter))]
            public DateTime Birthdate { get; set; }
        }

        public class CustomNumericalDateTimeTypeConverter : TypeConverter
        {
            public override bool CanConvertFrom(ITypeDescriptorContext context, Type sourceType)
            {
                return sourceType == typeof(Int64);
            }

            public override object ConvertFrom(ITypeDescriptorContext context, System.Globalization.CultureInfo culture, object value)
            {
                return DateTime.FromBinary((long)value);
            }

            public override bool CanConvertTo(ITypeDescriptorContext context, Type destinationType)
            {
                return destinationType == typeof(Int64);
            }

            public override object ConvertTo(ITypeDescriptorContext context, System.Globalization.CultureInfo culture, object value, Type destinationType)
            {
                return Convert.ToInt64(((DateTime)value).ToBinary());
            }
        }
    }
}
