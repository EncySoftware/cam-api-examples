using CsvHelper;
using CsvHelper.Configuration;
using CsvHelper.Configuration.Attributes;
using System.Globalization;

namespace DIN4000ImportPlugin
{

    public class ToolRecord
    {
        /// <summary>
        /// Tool name
        /// </summary>
        [Name("J21")]
        public string J21 { get; set; }

        /// <summary>
        /// Standard number of properties layout
        /// </summary>
        [Name("NSM")]
        public string NSM { get; set; }

        /// <summary>
        /// Standard number of properties layout
        /// </summary>
        [Name("BLD")]
        public string BLD { get; set; }

        /// <summary>
        /// Overall length 
        /// </summary>
        [Name("B5")]
        [Optional]
        [Default(0)]
        public double B5 { get; set; }

        /// <summary>
        /// Protruding length 
        /// </summary>
        [Name("B3")]
        [Optional]
        [Default(0)]
        public double B3 { get; set; }

        /// <summary>
        /// Depth of cut maximum 
        /// </summary>
        [Name("B2")]
        [Optional]
        [Default(0)]
        public double B2 { get; set; }

        /// <summary>
        /// Usable length 
        /// </summary>
        [Name("B4")]
        [Optional]
        [Default(0)]
        public double B4 { get; set; }

        /// <summary>
        /// Cutting diameter
        /// </summary>
        [Name("A1")]
        [Optional]
        [Default(0)]
        public double A1 { get; set; }

        /// <summary>
        /// Cutting diameter of step 1
        /// </summary>
        [Name("A11")]
        [Optional]
        [Default(0)]
        public double A11 { get; set; }

        /// <summary>
        /// Connection diameter machine side
        /// </summary>
        [Name("C3")]
        [Optional]
        [Default(0)]
        public double C3 { get; set; }
  
        /// <summary>
        /// Shank length
        /// </summary>
        [Name("C4")]
        [Optional]
        [Default(0)]
        public double C4 { get; set; }

        /// <summary>
        /// Corner radius
        /// </summary>
        [Name("G1")]
        [Optional]
        [Default(0)]
        public double G1 { get; set; }

        /// <summary>
        /// Point angle 1st step
        /// </summary>
        [Name("E1")]
        [Optional]
        [Default(0)]
        public double E1 { get; set; }
    }


    public class DIN4000CsvReader
    {

        public static void ReadCsvFile(string fileName, List<ToolRecord> toolRecs) 
        {
            if (!File.Exists(fileName))
                return;
            var config = new CsvConfiguration(CultureInfo.InvariantCulture)
            {
                Delimiter = ";",
                HasHeaderRecord = true,
                HeaderValidated = OnHeaderValidated,
                MissingFieldFound = null
            };
            using (var reader = new StreamReader(fileName))
            using (var csv = new CsvReader(reader, config))
            {
                var recs = csv.GetRecords<ToolRecord>();
                foreach (ToolRecord rec in recs)
                    toolRecs.Add(rec);
            }
        }

        private static void OnHeaderValidated(HeaderValidatedArgs args)
        {

        }

    }
}