using System;

namespace Moneybox.App
{
    public class Account
    {
        private Account(Guid id, User user, decimal balance)
        {
            if (balance < 0)
            {
                throw new ArgumentException("Balance cannot be negative.");
            }

            Id = id;
            User = user ?? throw new ArgumentNullException(nameof(user));
            Balance = balance;
            Withdrawn = 0;
            PaidIn = 0;
        }

        public static Account Create(Guid id, User user, decimal initialBalance)
        {
            return new Account(id, user, initialBalance);
        }
        
        public const decimal PayInLimit = 4000m;

        public Guid Id { get; private set; }

        public User User { get; private set; }

        public decimal Balance { get; private set; }

        public decimal Withdrawn { get; private set; }

        public decimal PaidIn { get; private set; }
        
        public bool CanWithdraw(decimal amount)
        {
            return Balance - amount >= 0m;
        }

        public bool CanDeposit(decimal amount)
        {
            return PaidIn + amount <= PayInLimit;
        }
        
        public bool IsFundsLow()
        {
            return Balance < 500m;
        }
        
        public bool IsApproachingPayInLimit(decimal amount)
        {
            return PayInLimit - (PaidIn + amount) < 500m;
        }

        public void Withdraw(decimal amount)
        {
            if (!CanWithdraw(amount))
            {
                throw new InvalidOperationException("Insufficient funds to make transfer");
            }

            Balance -= amount;
            Withdrawn += amount;
        }

        public void Deposit(decimal amount)
        {
            if (!CanDeposit(amount))
            {
                throw new InvalidOperationException("Account pay in limit reached");
            }

            Balance += amount;
            PaidIn += amount;
        }
    }
}