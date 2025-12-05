namespace UnitTests.V2
{
    [TestClass]
    public class CYStateTests
    {
        [TestMethod]
        public void HelloCYState()
        {
            var state = new CYState(Jan4K: 1200, JanInv: 800, JanSav: 200);

            Console.WriteLine("Take from SS:");
            state.AddIncome(In.SS, 100);
            Assert.IsTrue(state.ZeroSum == 0 && state.CashInHand == 100);
            Console.WriteLine(state);

            Console.WriteLine("Take from Annuity:");
            state.AddIncome(In.Ann, 50);
            Assert.IsTrue(state.ZeroSum == 0 && state.CashInHand == 150);
            Console.WriteLine(state);

            Console.WriteLine("Update Expense:");
            state.AddExpense(Exp.CYExp, 200);
            Assert.IsTrue(state.ZeroSum == 0 && state.CashInHand == -50);
            Console.WriteLine(state);
        }

        [TestMethod]
        public void HelloFundTransfer()
        {
            var state = new CYState(Jan4K: 1200, JanInv: 800, JanSav: 200);

            // Transfer from investment to cash
            state.Transfer(IO.Inv, IO.Sav, 100);
            Assert.IsTrue(state.ZeroSum == 0);
            Assert.IsTrue(state.CashInHand == 0);
        }

        /// <summary>
        /// Comprehensive test validating transactional correctness for all possible asset movements.
        /// Tests all API methods and ensures ZeroSum invariant is maintained throughout.
        /// </summary>
        [TestMethod]
        public void TransactionalCorrectness_AllOperations_MaintainsZeroSum()
        {
            // Arrange - Initialize with starting balances
            var state = new CYState(
                Jan4K: 100000,   // Starting 401K balance
                JanInv: 50000,   // Starting Investment balance
                JanSav: 10000    // Starting Savings balance
            );

            // Initial state should have ZeroSum = 0
            Assert.AreEqual(0.0, state.ZeroSum, 1e-6, "Initial ZeroSum should be zero");
            Assert.AreEqual(0.0, state.CashInHand, 1e-6, "Initial CashInHand should be zero");

            // Test 1: AddIncome - Social Security
            state.AddIncome(In.SS, 2000);
            Assert.AreEqual(2000, state.SS, 1e-6, "SS income should be 2000");
            Assert.AreEqual(2000, state.CashInHand, 1e-6, "CashInHand should be 2000 (received income)");
            Assert.AreEqual(0.0, state.ZeroSum, 1e-6, "ZeroSum after AddIncome(SS)");

            // Test 2: AddIncome - Annuity
            state.AddIncome(In.Ann, 1500);
            Assert.AreEqual(1500, state.ANN, 1e-6, "Annuity income should be 1500");
            Assert.AreEqual(3500, state.CashInHand, 1e-6, "CashInHand should be 3500");
            Assert.AreEqual(0.0, state.ZeroSum, 1e-6, "ZeroSum after AddIncome(Ann)");

            // Test 3: AddIncome - 401K withdrawal
            state.AddIncome(In.FourK, 5000);
            Assert.AreEqual(5000, state.FourK, 1e-6, "401K income should be 5000");
            Assert.AreEqual(8500, state.CashInHand, 1e-6, "CashInHand should be 8500");
            Assert.AreEqual(8500, state.TotalIncome, 1e-6, "Total income should be 8500");
            Assert.AreEqual(0.0, state.ZeroSum, 1e-6, "ZeroSum after AddIncome(FourK)");

            // Test 4: AddExpense - Current year expenses
            state.AddExpense(Exp.CYExp, 4000);
            Assert.AreEqual(-4000, state.CYExp, 1e-6, "CYExp should be -4000");
            Assert.AreEqual(4500, state.CashInHand, 1e-6, "CashInHand should be 4500 (8500-4000)");
            Assert.AreEqual(0.0, state.ZeroSum, 1e-6, "ZeroSum after AddExpense(CYExp)");

            // Test 5: AddExpense - Prior year tax
            state.AddExpense(Exp.PYTax, 1200);
            Assert.AreEqual(-1200, state.PYTax, 1e-6, "PYTax should be -1200");
            Assert.AreEqual(3300, state.CashInHand, 1e-6, "CashInHand should be 3300 (4500-1200)");
            Assert.AreEqual(0.0, state.ZeroSum, 1e-6, "ZeroSum after AddExpense(PYTax)");

            // Test 6: AddExpense - Fees
            state.AddExpense(Exp.Fees, 300);
            Assert.AreEqual(-300, state.Fees, 1e-6, "Fees should be -300");
            Assert.AreEqual(3000, state.CashInHand, 1e-6, "CashInHand should be 3000 (3300-300)");
            Assert.AreEqual(-5500, state.TotalExpenses, 1e-6, "Total expenses should be -5500");
            Assert.AreEqual(0.0, state.ZeroSum, 1e-6, "ZeroSum after AddExpense(Fees)");

            // Test 7: Deposit to Investment account
            state.Deposit(IO.Inv, 2000);
            Assert.AreEqual(2000, state.Inv, 1e-6, "Investment delta should be 2000");
            Assert.AreEqual(1000, state.CashInHand, 1e-6, "CashInHand should be 1000 (3000-2000)");
            Assert.AreEqual(0.0, state.ZeroSum, 1e-6, "ZeroSum after Deposit(Inv)");

            // Test 8: Deposit to Savings account
            state.Deposit(IO.Sav, 500);
            Assert.AreEqual(500, state.Sav, 1e-6, "Savings delta should be 500");
            Assert.AreEqual(500, state.CashInHand, 1e-6, "CashInHand should be 500 (1000-500)");
            Assert.AreEqual(2500, state.AssetChanges, 1e-6, "Total assets delta should be 2500");
            Assert.AreEqual(0.0, state.ZeroSum, 1e-6, "ZeroSum after Deposit(Sav)");

            // Test 9: Withdraw from Investment account
            state.Withdraw(IO.Inv, 1000);
            Assert.AreEqual(1000, state.Inv, 1e-6, "Investment delta should be 1000 (2000-1000)");
            Assert.AreEqual(1500, state.CashInHand, 1e-6, "CashInHand should be 1500 (500+1000)");
            Assert.AreEqual(0.0, state.ZeroSum, 1e-6, "ZeroSum after Withdraw(Inv)");

            // Test 10: Withdraw from Savings account
            state.Withdraw(IO.Sav, 200);
            Assert.AreEqual(300, state.Sav, 1e-6, "Savings delta should be 300 (500-200)");
            Assert.AreEqual(1700, state.CashInHand, 1e-6, "CashInHand should be 1700 (1500+200)");
            Assert.AreEqual(0.0, state.ZeroSum, 1e-6, "ZeroSum after Withdraw(Sav)");

            // Test 11: ReduceIncome - Adjust Social Security downward
            state.ReduceIncome(In.SS, 500);
            Assert.AreEqual(1500, state.SS, 1e-6, "SS income should be 1500 (2000-500)");
            Assert.AreEqual(1200, state.CashInHand, 1e-6, "CashInHand should be 1200 (1700-500)");
            Assert.AreEqual(0.0, state.ZeroSum, 1e-6, "ZeroSum after ReduceIncome(SS)");

            // Test 12: ReduceExpense - Expense adjustment/refund
            state.ReduceExpense(Exp.CYExp, 1000);
            Assert.AreEqual(-3000, state.CYExp, 1e-6, "CYExp should be -3000 (-4000+1000)");
            Assert.AreEqual(2200, state.CashInHand, 1e-6, "CashInHand should be 2200 (1200+1000)");
            Assert.AreEqual(0.0, state.ZeroSum, 1e-6, "ZeroSum after ReduceExpense(CYExp)");

            // Test 13: Transfer between income and expense (e.g., direct tax payment from SS)
            state.Transfer(In.SS, Exp.PYTax, 500);
            Assert.AreEqual(2000, state.SS, 1e-6, "SS income should be 2000 (1500+500)");
            Assert.AreEqual(-1700, state.PYTax, 1e-6, "PYTax should be -1700 (-1200-500)");
            Assert.AreEqual(2200, state.CashInHand, 1e-6, "CashInHand should remain 2200 (net zero)");
            Assert.AreEqual(0.0, state.ZeroSum, 1e-6, "ZeroSum after Transfer(In->Exp)");

            // Test 14: Transfer between assets (move money from Sav to Inv)
            state.Transfer(IO.Sav, IO.Inv, 200);
            Assert.AreEqual(100, state.Sav, 1e-6, "Savings delta should be 100 (300-200)");
            Assert.AreEqual(1200, state.Inv, 1e-6, "Investment delta should be 1200 (1000+200)");
            Assert.AreEqual(2200, state.CashInHand, 1e-6, "CashInHand should remain 2200 (net zero)");
            Assert.AreEqual(0.0, state.ZeroSum, 1e-6, "ZeroSum after Transfer(IO->IO)");

            // Test 15: Large withdrawal that creates surplus scenario
            state.Withdraw(IO.Inv, 3500);
            Assert.AreEqual(-2300, state.Inv, 1e-6, "Investment delta should be -2300 (1200-3500)");
            Assert.AreEqual(5700, state.CashInHand, 1e-6, "CashInHand should be 5700 (2200+3500)");
            Assert.AreEqual(0.0, state.ZeroSum, 1e-6, "ZeroSum after large Withdraw");

            // Test 16: Add more income
            state.AddIncome(In.Ann, 800);
            Assert.AreEqual(2300, state.ANN, 1e-6, "Annuity income should be 2300 (1500+800)");
            Assert.AreEqual(6500, state.CashInHand, 1e-6, "CashInHand should be 6500 (5700+800)");
            Assert.AreEqual(0.0, state.ZeroSum, 1e-6, "ZeroSum after additional AddIncome");

            // Test 17: Deposit surplus to assets
            state.Deposit(IO.Sav, 6500);
            Assert.AreEqual(6600, state.Sav, 1e-6, "Savings delta should be 6600 (100+6500)");
            Assert.AreEqual(0, state.CashInHand, 1e-6, "CashInHand should be zero (balanced)");
            Assert.AreEqual(0.0, state.ZeroSum, 1e-6, "Final ZeroSum should be zero");

            // Validate final aggregate values
            double expectedIncome = state.SS + state.ANN + state.FourK;
            double expectedExpenses = state.CYExp + state.PYTax + state.Fees;
            double expectedAssets = state.Inv + state.Sav;

            Assert.AreEqual(expectedIncome, state.TotalIncome, 1e-6, "Income aggregate");
            Assert.AreEqual(expectedExpenses, state.TotalExpenses, 1e-6, "Expenses aggregate");
            Assert.AreEqual(expectedAssets, state.AssetChanges, 1e-6, "Assets aggregate");

            // Final validation: EnsureZeroSum should not throw
            state.EnsureZeroSum();
        }
    }
}
