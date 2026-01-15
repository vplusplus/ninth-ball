

using NinthBall.Core;

namespace UnitTests
{
    [TestClass]

    public class AssetChangeTests
    {

        [TestMethod]
        public void PostTest()
        {
            var a = new Asset(1000, 0.6);
            Print(a);

            a = a.Rebalance(0.7, 0.05);
            Print(a);

            a = a.Post(100);
            Print(a);

            a = a.Post(-100);
            Print(a);

            static void Print(Asset a)
            {
                Console.WriteLine($"Stocks: {a.Stocks:C2} Bonds: {a.Bonds:C2} Alloc: {a.Allocation:P0}");
            }
        }

    }


}
