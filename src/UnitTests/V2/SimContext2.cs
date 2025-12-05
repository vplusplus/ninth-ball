using NinthBall;
using System;
using System.Collections.Generic;
using System.Text;
using System.Transactions;

namespace UnitTests.V2
{

    public record class Asset(double InitialBalance, double InitialAllocation, double InitialMaxDrift)
    {
        public double StockBalance { get; private set; } = InitialBalance * InitialAllocation;
        public double BondBalance { get; private set; } = InitialBalance * (1 - InitialAllocation);
        public double CurrentBalance => StockBalance + BondBalance;
        public double TargetAllocation { get; private set; } = InitialAllocation;
        public double CurrentAllocation => StockBalance / CurrentBalance;
        public double TargetMaxDrift { get; private set; } = InitialMaxDrift;
        public double CurrentDrift => Math.Abs(CurrentAllocation - TargetAllocation) * 2;

        public bool Rebalance()
        {
            if (Math.Abs(CurrentDrift) > Math.Abs(TargetMaxDrift))
            {
                var tmpTotalBalance = CurrentBalance;
                StockBalance = tmpTotalBalance * TargetAllocation;
                BondBalance = tmpTotalBalance - StockBalance;
                return true;
            }
            else
            {
                return false;
            }
        }

        public void Reallocate(double newAllocation, double newMaxDrift)
        {
            if (newAllocation < 0.0 || newAllocation > 1.0) throw new ArgumentException("AllocationPct must be between 0.0 and 1.0");
            if (newMaxDrift < 0.0 || newMaxDrift > 1.0) throw new ArgumentException("MaxDrift must be between 0.0 and 1.0");

            TargetAllocation = newAllocation;
            TargetMaxDrift = newMaxDrift;
            this.Rebalance();
        }

        public void Post(double amount)
        {
            if (amount == 0) return; else if (amount > 0) Deposit(amount); else Withdraw(amount); 

            void Deposit(double amount)
            {
                var stockChange = amount * TargetAllocation;
                var bondChange = amount - stockChange;

                StockBalance += stockChange;
                BondBalance += bondChange;
            }

            void Withdraw(double amount)
            {
                if (amount < 0) throw new ArgumentException("Reduce: Amount must be positive.");
                if (CurrentBalance < amount) throw new InvalidOperationException("Reduce: Can't reduce more than what we have.");

                // If nothing to reduce.
                if (0 == amount) return;

                // This is how much we plan to reduce from stock and bond balance.
                double fromStock = amount * TargetAllocation;
                double fromBond = amount - fromStock;

                // Try take from correct asset.
                TryTakeFromStock(ref fromStock);
                TryTakeFromBond(ref fromBond);

                // There may be leftover. Try otherway.
                TryTakeFromBond(ref fromStock);
                TryTakeFromStock(ref fromBond);

                // By now, both must be zero.
                if (fromStock + fromBond > 0) throw new InvalidOperationException("SimBalance.Reduce: Unexpected mismatch in Reduce calculation.");
                return;

                void TryTakeFromStock(ref double amt)
                {
                    var took = Math.Min(amt, StockBalance);
                    StockBalance -= took;
                    amt -= took;
                }

                void TryTakeFromBond(ref double amt)
                {
                    var took = Math.Min(amt, BondBalance);
                    BondBalance -= took;
                    amt -= took;
                }
            }
        }

        public double Change(double stocksROI, double bondsROI)
        {
            var stockChange = StockBalance * stocksROI;
            var bondChange = BondBalance * bondsROI;
            StockBalance += stockChange;
            BondBalance += bondChange;
            return stockChange + bondChange;
        }

        public override string ToString() => $"{CurrentBalance:C0} = {StockBalance:C0} + {BondBalance:C0}";
    }




}


