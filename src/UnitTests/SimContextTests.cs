
using NinthBall;

namespace UnitTests
{
    [TestClass]
    public class SimContextTests
    {
        [TestMethod]
        public void Grow_ShouldNotGoNegative()
        {
            var context = new SimContext();
            var initial = new InitialBalance(
                new InitialBalance.AA(1000, 1.0),
                new InitialBalance.AA(1000, 1.0),
                new InitialBalance.AA(1000, 1.0)
            );
            var store = new SimYear[1];
            context.Reset(initial, 0, 60, store);

            // Apply a very negative ROI (-150%)
            context.ROI = new ROI(0, -1.5, -1.5, -1.5);
            
            // This should not throw and should result in zero balances
            context.ImplementStrategies();

            Assert.AreEqual(0, context.PreTaxBalance.Amount);
            Assert.AreEqual(0, context.PostTaxBalance.Amount);
            Assert.AreEqual(0, context.CashBalance.Amount);
        }
    }
}
