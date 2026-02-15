using Microsoft.VisualStudio.TestTools.UnitTesting;
using NinthBall.Core;
using System.Collections;
using System.Collections.Generic;
using System.Data;
using System.IO;

namespace UnitTests.PrettyTables
{
    [TestClass]
    public class PrettyPrintTests
    {
        [TestMethod]
        public void TestGridRendering()
        {
            var dt = new DataTable();
            dt.WithColumn("Feature").WithColumn("Value", typeof(double));
            dt.Rows.Add("S&P 500", 0.12);
            dt.Rows.Add("Bond Yield", 0.045);

            dt.WithFormat("Value", "P1");

            var sw = new StringWriter();
            sw.PrintMarkdownPageTitle("Market Summary")
              .PrintMarkdownSectionTitle("Key Indices");
            
            sw.PrintMarkdownTable(dt);

            var output = sw.ToString();
            Console.WriteLine(output);

            Assert.IsTrue(output.Contains("# Market Summary"));
            Assert.IsTrue(output.Contains("## Key Indices"));
            Assert.IsTrue(output.Contains("12.0%"));
            Assert.IsTrue(output.Contains("4.5%"));
        }

        [TestMethod]
        public void TestRecordOneLine()
        {
            var config = new { NumClusters = 5, DiscoverySeed = 42, Threshold = 0.00012345 };
            var sw = new StringWriter();
            
            sw.PrintMarkdownRecordOneLine(config);
            
            var output = sw.ToString();
            Console.WriteLine(output);

            // Verify diagnostic formatting (N4 for double)
            Assert.IsTrue(output.Contains("NumClusters : 5"));
            Assert.IsTrue(output.Contains("Threshold : 0.0001")); 
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
            
            sw.PrintMarkdownPageTitle("Dictionary View");
            
            sw.PrintMarkdownSectionTitle("Wide");
            sw.PrintMarkdownRecordWide(dict);

            sw.PrintMarkdownSectionTitle("Tall");
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
            sw.PrintMarkdownPageTitle("Main Report")
              .PrintMarkdownSectionTitle("Sub Section")
              .Write("Content");

            var output = sw.ToString();
            Assert.IsTrue(output.Contains("# Main Report"));
            Assert.IsTrue(output.Contains("## Sub Section"));
            Assert.IsTrue(output.Contains("Content"));
        }
    }
}
