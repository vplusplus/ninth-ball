using NinthBall.Utils;

namespace UnitTests
{
    [TestClass]
    public class PlaceholderTest
    {
        [TestMethod]
        public void ResolveTest()
        {
            var values = new Dictionary<string, object>
            {
                ["customerId"] = 12345,
                ["survival"] = 0.98,
                ["Date"] = new DateTime(2025, 06, 05)
            };

            string input = "/reports/{customerId}-summary-srate{survival:P0}-{Date:yyyyMMdd}.html";

            string result = PlaceHolders.ResolvePlaceholders(input, values);
            Console.WriteLine(result);
        }
    }




}


