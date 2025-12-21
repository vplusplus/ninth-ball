
using NinthBall;
using NinthBall.Core;
using Moq;

namespace UnitTests
{
    [TestClass]
    public class StrategyTests
    {
        [TestMethod]
        public void RMDStrategy_Bumps_Up_Low_Withdrawal()
        {
            // Arrange
            var options = new RMD(StartAge: 73);
            ISimObjective strategyObj = new RMDStrategy(options);
            var strategy = strategyObj.CreateStrategy(0);

            var mockContext = new Mock<ISimContext>();
            var mockBalance = new Mock<IBalance>();
            
            // Age 73, Balance 1,000,000 => RMD Factor 26.5 => RMD = 37,735.85
            mockContext.Setup(c => c.Age).Returns(73);
            mockBalance.Setup(b => b.Amount).Returns(1_000_000);
            mockContext.Setup(c => c.PreTaxBalance).Returns(mockBalance.Object);
            
            // Current withdrawal is only 10,000
            var currentWithdrawals = new Withdrawals { PreTax = 10_000 };
            mockContext.SetupProperty(c => c.Withdrawals, currentWithdrawals);

            // Act
            strategy.Apply(mockContext.Object);

            // Assert
            Assert.AreEqual(1_000_000 / 26.5, mockContext.Object.Withdrawals.PreTax, 0.01);
        }

        [TestMethod]
        public void RMDStrategy_Does_Not_Change_High_Withdrawal()
        {
            // Arrange
            var options = new RMD(StartAge: 73);
            ISimObjective strategyObj = new RMDStrategy(options);
            var strategy = strategyObj.CreateStrategy(0);

            var mockContext = new Mock<ISimContext>();
            var mockBalance = new Mock<IBalance>();
            
            mockContext.Setup(c => c.Age).Returns(73);
            mockBalance.Setup(b => b.Amount).Returns(1_000_000);
            mockContext.Setup(c => c.PreTaxBalance).Returns(mockBalance.Object);
            
            // Current withdrawal is 50,000 (already > RMD of 37,735)
            var currentWithdrawals = new Withdrawals { PreTax = 50_000 };
            mockContext.SetupProperty(c => c.Withdrawals, currentWithdrawals);

            // Act
            strategy.Apply(mockContext.Object);

            // Assert
            Assert.AreEqual(50_000, mockContext.Object.Withdrawals.PreTax);
        }

        [TestMethod]
        public void RMDStrategy_Does_Not_Kick_In_Before_StartAge()
        {
            // Arrange
            var options = new RMD(StartAge: 73);
            ISimObjective strategyObj = new RMDStrategy(options);
            var strategy = strategyObj.CreateStrategy(0);

            var mockContext = new Mock<ISimContext>();
            mockContext.Setup(c => c.Age).Returns(70); // Before 73
            
            var currentWithdrawals = new Withdrawals { PreTax = 0 };
            mockContext.SetupProperty(c => c.Withdrawals, currentWithdrawals);

            // Act
            strategy.Apply(mockContext.Object);

            // Assert
            Assert.AreEqual(0, mockContext.Object.Withdrawals.PreTax);
        }
    }
}
