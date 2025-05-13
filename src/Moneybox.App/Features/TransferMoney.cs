using Moneybox.App.DataAccess;
using Moneybox.App.Domain.Services;
using System;
using System.Transactions;

namespace Moneybox.App.Features
{
    public class TransferMoney
    {
        private IAccountRepository accountRepository;
        private INotificationService notificationService;

        public TransferMoney(IAccountRepository accountRepository, INotificationService notificationService)
        {
            this.accountRepository = accountRepository;
            this.notificationService = notificationService;
        }

        public void Execute(Guid fromAccountId, Guid toAccountId, decimal amount)
        {
            var from = this.accountRepository.GetAccountById(fromAccountId);
            var to = this.accountRepository.GetAccountById(toAccountId);

            // var fromBalance = from.Balance - amount;
            // if (fromBalance < 0m)
            // {
            //     throw new InvalidOperationException("Insufficient funds to make transfer");
            // }
            
            if (!from.CanWithdraw(amount))
            {
                throw new InvalidOperationException("Insufficient funds to make transfer");
            }
            
            // if (fromBalance < 500m)
            // {
            //     this.notificationService.NotifyFundsLow(from.User.Email);
            // }
            
            if (!from.IsFundsLow())
            {
                notificationService.NotifyFundsLow(from.User.Email);
            }
            
            // var paidIn = to.PaidIn + amount;
            // if (paidIn > Account.PayInLimit)
            // {
            //     throw new InvalidOperationException("Account pay in limit reached");
            // }
            
            if (!to.CanDeposit(amount))
            {
                throw new InvalidOperationException("Account pay in limit reached");
            }

            // if (Account.PayInLimit - paidIn < 500m)
            // {
            //     this.notificationService.NotifyApproachingPayInLimit(to.User.Email);
            // }
            
            if (to.IsApproachingPayInLimit(amount))
            {
                notificationService.NotifyApproachingPayInLimit(to.User.Email);
            }

            // from.Balance = from.Balance - amount;
            // from.Withdrawn = from.Withdrawn - amount;
            
            using var transaction = new TransactionScope();
            try
            {
                from.Withdraw(amount);
                to.Deposit(amount);
                this.accountRepository.Update(from);
                this.accountRepository.Update(to);
            }
            catch (Exception e)
            {
                //Handle exception
            }

            transaction.Complete();
        }
    }
}
