using NinthBall.Core.PrettyPrint;
using System.Collections;
using System.Data;

namespace UnitTests.PrettyTables
{
    [TestClass]
    public class PrettyPrintTests
    {
        [TestMethod]
        public void TestGridRendering()
        {
            var dt = new DataTable();
            dt.WithColumn("Feature").WithColumn("Value", typeof(double), format: "P2");
            dt.Rows.Add("S&P 500", 0.12);
            dt.Rows.Add("Bond Yield", 0.045);

            var sw = new StringWriter();
            sw.PrintMarkdownTitle3("Market Summary")
              .PrintMarkdownTitle4("Key Indices");
            
            sw.PrintMarkdownTable(dt);

            var output = sw.ToString();
            Console.WriteLine(output);

            Assert.IsTrue(output.Contains("### Market Summary"));
            Assert.IsTrue(output.Contains("#### Key Indices"));
            Assert.IsTrue(output.Contains("12.00%"));
            Assert.IsTrue(output.Contains("4.50%"));
        }

        [TestMethod]
        public void TestRecordOneLine()
        {
            var config = new { NumClusters = 5, DiscoverySeed = 42, Threshold = 0.12345 };
            var sw = new StringWriter();
            
            sw.PrintMarkdownRecordOneLine(config);
            sw.PrintMarkdownRecordOneLine(config);
            sw.PrintMarkdownRecordOneLine(config);

            var output = sw.ToString();
            Console.WriteLine(output);

            // Verify diagnostic formatting (N4 for double)
            Assert.IsTrue(output.Contains("NumClusters : 5"));
            Assert.IsTrue(output.Contains("Threshold : 0.12")); 
            Assert.IsTrue(output.Contains("|"));
        }

        [TestMethod]
        public void TestRecordWide()
        {
            var config = new { NumClusters = 5, Algorithm = "K-Means++" };
            var sw = new StringWriter();
            
            sw.PrintMarkdownRecordWide(config);
            
            var output = sw.ToString();
            Console.WriteLine(output);

            Assert.IsTrue(output.Contains("NumClusters"));
            Assert.IsTrue(output.Contains("Algorithm"));
            Assert.IsTrue(output.Contains("5"));
            Assert.IsTrue(output.Contains("K-Means++"));
            // Basic GFM table check
            Assert.IsTrue(output.Contains("|---"));
        }

        [TestMethod]
        public void TestRecordTall()
        {
            var config = new { NumClusters = 5, Algorithm = "K-Means++" };
            var sw = new StringWriter();
            
            sw.PrintMarkdownRecordTall(config);
            
            var output = sw.ToString();
            Console.WriteLine(output);

            Assert.IsTrue(output.Contains("Property"));
            Assert.IsTrue(output.Contains("Value"));
            Assert.IsTrue(output.Contains("NumClusters"));
            Assert.IsTrue(output.Contains("5"));
            // Tall format check (two columns)
            Assert.IsTrue(output.Contains("|:--")); 
        }

        [TestMethod]
        public void TestDictionaryWideAndTall()
        {
            var dict = new Hashtable { ["S&P 500"] = 4500.25, ["Allocation"] = 0.60 };
            var sw = new StringWriter();
            
            sw.PrintMarkdownTitle3("Dictionary View");
            
            sw.PrintMarkdownTitle4("Wide");
            sw.PrintMarkdownRecordWide(dict);

            sw.PrintMarkdownTitle4("Tall");
            sw.PrintMarkdownRecordTall(dict);
            
            var output = sw.ToString();
            Console.WriteLine(output);

            Assert.IsTrue(output.Contains("4,500.2500")); // Diagnostic N4
            Assert.IsTrue(output.Contains("Allocation"));
            Assert.IsTrue(output.Contains("0.6000"));     // Diagnostic N4
        }

        [TestMethod]
        public void TestFluentHeaders()
        {
            var sw = new StringWriter();
            sw.PrintMarkdownTitle3("Main Report")
              .PrintMarkdownTitle4("Sub Section")
              .Write("Content");

            var output = sw.ToString();
            Assert.IsTrue(output.Contains("### Main Report"));
            Assert.IsTrue(output.Contains("#### Sub Section"));
            Assert.IsTrue(output.Contains("Content"));
        }

        [TestMethod]
        public void TestCollectionPOCO()
        {
            var collection = new[]
            {
                new { Metric = "Alpha", Value = 1200 },
                new { Metric = "Beta", Value = 2500000 }
            };

            var sw = new StringWriter();
            sw.PrintMarkdownTable(collection);

            var output = sw.ToString();
            Console.WriteLine(output);

            Assert.IsTrue(output.Contains("Metric"));
            Assert.IsTrue(output.Contains("Value"));
            Assert.IsTrue(output.Contains("1,200"));      // N0 Integer
            Assert.IsTrue(output.Contains("2,500,000"));  // N0 Integer
        }

        [TestMethod]
        public void TestCollectionDictionary()
        {
            var collection = new List<Hashtable>
            {
                new Hashtable { ["Metric"] = "Return", ["Val"] = 0.0825 },
                new Hashtable { ["Metric"] = "Vol", ["Val"] = 0.15 }
            };

            var sw = new StringWriter();
            sw.PrintMarkdownTable(collection);

            var output = sw.ToString();
            Console.WriteLine(output);

            Assert.IsTrue(output.Contains("Metric"));
            Assert.IsTrue(output.Contains("Val"));
            Assert.IsTrue(output.Contains("0.0825"));
            Assert.IsTrue(output.Contains("0.1500"));
        }
    }
}
