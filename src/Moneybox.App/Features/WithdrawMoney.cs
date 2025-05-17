using Moneybox.App.DataAccess;
using Moneybox.App.Domain.Services;
using System;
using System.Transactions;

namespace Moneybox.App.Features
{
    public class WithdrawMoney
    {
        private IAccountRepository accountRepository;
        private INotificationService notificationService;

        public WithdrawMoney(IAccountRepository accountRepository, INotificationService notificationService)
        {
            this.accountRepository = accountRepository;
            this.notificationService = notificationService;
        }

        public void Execute(Guid fromAccountId, decimal amount)
        {
            var from = this.accountRepository.GetAccountById(fromAccountId);
            
            if (!from.CanWithdraw(amount))
            {
                throw new InvalidOperationException("Insufficient funds to make transfer");
            }
            
            if (from.IsFundsLow())
            {
                notificationService.NotifyFundsLow(from.User.Email);
            }
            
            using var transaction = new TransactionScope();
            try
            {
                from.Withdraw(amount);
                this.accountRepository.Update(from);
            }
            catch (Exception e)
            {
                //Handle exception
            }
            
            transaction.Complete();
        }
    }
}