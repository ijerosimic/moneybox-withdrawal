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

        var mockRepo = Mock.Of<IAccountRepository>(repo =>
            repo.GetAccountById(fromAccountId) == fromAccount &&
            repo.GetAccountById(toAccountId) == toAccount);
        
        var mockNotificationService = new Mock<INotificationService>();
        mockNotificationService
            .Setup(service => service.NotifyFundsLow(It.IsAny<string>()))
            .Verifiable();
        mockNotificationService
            .Setup(service => service.NotifyApproachingPayInLimit(It.IsAny<string>()))
            .Verifiable();
        
        var from = mockRepo.GetAccountById(fromAccountId)!;
        var to = mockRepo.GetAccountById(toAccountId)!;
        
        var transferMoney = new TransferMoney(mockRepo, mockNotificationService.Object);
        transferMoney.Execute(fromAccountId, toAccountId, amount: 100);
        
        from.Balance.Should().Be(900);
        to.Balance.Should().Be(600);
        
        mockNotificationService.Verify(service => service.NotifyFundsLow(It.IsAny<string>()), Times.Never);
        mockNotificationService.Verify(service => service.NotifyApproachingPayInLimit(It.IsAny<string>()), Times.Never);
    }
    
    [Fact]
    public void It_Does_Not_Transfer_Money_When_From_Balance_Is_Too_Low()
    {
        var fromAccountId = Guid.NewGuid();
        var fromAccount = Account.Create(fromAccountId, new User(), 1000);
        
        var toAccountId = Guid.NewGuid();
        var toAccount = Account.Create(toAccountId, new User(), 500);

        var mockRepo = Mock.Of<IAccountRepository>(repo =>
            repo.GetAccountById(fromAccountId) == fromAccount &&
            repo.GetAccountById(toAccountId) == toAccount);
        
        var mockNotificationService = new Mock<INotificationService>();
        mockNotificationService
            .Setup(service => service.NotifyFundsLow(It.IsAny<string>()))
            .Verifiable();
        mockNotificationService
            .Setup(service => service.NotifyApproachingPayInLimit(It.IsAny<string>()))
            .Verifiable();
        
        var from = mockRepo.GetAccountById(fromAccountId)!;
        var to = mockRepo.GetAccountById(toAccountId)!;
        
        var transferMoney = new TransferMoney(mockRepo, mockNotificationService.Object);

        var act = () => transferMoney.Execute(fromAccountId, toAccountId, amount: 2000);
        act.Should().Throw<InvalidOperationException>().WithMessage("Insufficient funds to make transfer");
        
        from.Balance.Should().Be(1000);
        to.Balance.Should().Be(500);
        
        mockNotificationService.Verify(service => service.NotifyFundsLow(It.IsAny<string>()), Times.Never);
        mockNotificationService.Verify(service => service.NotifyApproachingPayInLimit(It.IsAny<string>()), Times.Never);
    }
    
    [Fact]
    public void It_Transfers_Money_And_Notifies_On_Low_Balance()
    {
        var fromAccountId = Guid.NewGuid();
        var fromAccount = Account.Create(fromAccountId, new User(), 1000);
        
        var toAccountId = Guid.NewGuid();
        var toAccount = Account.Create(toAccountId, new User(), 500);

        var mockRepo = Mock.Of<IAccountRepository>(repo =>
            repo.GetAccountById(fromAccountId) == fromAccount &&
            repo.GetAccountById(toAccountId) == toAccount);
        
        var mockNotificationService = new Mock<INotificationService>();
        mockNotificationService
            .Setup(service => service.NotifyFundsLow(It.IsAny<string>()))
            .Verifiable();
        mockNotificationService
            .Setup(service => service.NotifyApproachingPayInLimit(It.IsAny<string>()))
            .Verifiable();
        
        var from = mockRepo.GetAccountById(fromAccountId)!;
        var to = mockRepo.GetAccountById(toAccountId)!;
        
        var transferMoney = new TransferMoney(mockRepo, mockNotificationService.Object);
        transferMoney.Execute(fromAccountId, toAccountId, amount: 800);
        
        from.Balance.Should().Be(200);
        to.Balance.Should().Be(1300);
        
        mockNotificationService.Verify(service => service.NotifyFundsLow(It.IsAny<string>()), Times.Once);
        mockNotificationService.Verify(service => service.NotifyApproachingPayInLimit(It.IsAny<string>()), Times.Never);
    }
}