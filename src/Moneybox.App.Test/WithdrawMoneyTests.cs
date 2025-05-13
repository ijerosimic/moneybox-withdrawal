using FluentAssertions;
using Moneybox.App.DataAccess;
using Moneybox.App.Domain.Services;
using Moneybox.App.Features;
using Moq;

namespace Moneybox.App.Test;

public class TransferMoneyTests
{
    [Fact]
    public void It_Transfers_Money()
    {
        var fromAccountId = Guid.NewGuid();
        var fromAccount = Account.Create(fromAccountId, new User(), 1000);
        
        var toAccountId = Guid.NewGuid();
        var toAccount = Account.Create(toAccountId, new User(), 500);

        var mockRepository = new Mock<IAccountRepository>();
        mockRepository
            .Setup(repo => repo.GetAccountById(fromAccountId))
            .Returns(fromAccount);
        mockRepository
            .Setup(repo => repo.GetAccountById(toAccountId))
            .Returns(toAccount);
        var mockRepo = mockRepository.Object;
        
        var mockNotificationService = new Mock<INotificationService>();
        mockNotificationService
            .Setup(service => service.NotifyFundsLow(It.IsAny<string>()))
            .Verifiable();
        
        var from = mockRepo.GetAccountById(fromAccountId)!;
        var to = mockRepo.GetAccountById(toAccountId)!;
        
        var transferMoney = new TransferMoney(mockRepo, mockNotificationService.Object);
        transferMoney.Execute(fromAccountId, toAccountId, amount: 100);
        
        from.Balance.Should().Be(900);
        to.Balance.Should().Be(600);
    }
}