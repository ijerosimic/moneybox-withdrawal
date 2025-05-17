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

            if (!from.CanWithdraw(amount))
            {
                throw new InvalidOperationException("Insufficient funds to make transfer");
            }
            
            if (from.IsFundsLow())
            {
                notificationService.NotifyFundsLow(from.User.Email);
            }
            
            if (!to.CanDeposit(amount))
            {
                throw new InvalidOperationException("Account pay in limit reached");
            }

            if (to.IsApproachingPayInLimit(amount))
            {
                notificationService.NotifyApproachingPayInLimit(to.User.Email);
            }

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
