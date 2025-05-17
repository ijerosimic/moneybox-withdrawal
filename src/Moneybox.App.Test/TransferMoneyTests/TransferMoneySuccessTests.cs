using FluentAssertions;
using Moneybox.App.DataAccess;
using Moneybox.App.Domain.Services;
using Moneybox.App.Features;
using Moq;

namespace Moneybox.App.Test.TransferMoneyTests;

public class TransferMoneySuccessTests
{
    private readonly Mock<INotificationService> _mockNotificationService;

    public TransferMoneySuccessTests()
    {
        _mockNotificationService = new Mock<INotificationService>();
        _mockNotificationService
            .Setup(service => service.NotifyFundsLow(It.IsAny<string>()))
            .Verifiable();
        _mockNotificationService
            .Setup(service => service.NotifyApproachingPayInLimit(It.IsAny<string>()))
            .Verifiable();
    }

    [Fact]
    public void It_Transfers_Money()
    {
        var fromAccount = Account.Create(Guid.NewGuid(), new User(), 5000);
        var toAccount = Account.Create(Guid.NewGuid(), new User(), 0);

        var mockRepo = Mock.Of<IAccountRepository>(repo =>
            repo.GetAccountById(fromAccount.Id) == fromAccount &&
            repo.GetAccountById(toAccount.Id) == toAccount);
        
        var transferMoney = new TransferMoney(mockRepo, _mockNotificationService.Object);
        transferMoney.Execute(fromAccount.Id, toAccount.Id, amount: 30);

        fromAccount.Withdrawn.Should().Be(30);
        toAccount.PaidIn.Should().Be(30);
        
        fromAccount.Balance.Should().Be(4970);
        toAccount.Balance.Should().Be(30);

        _mockNotificationService.Verify(service => service.NotifyFundsLow(It.IsAny<string>()), Times.Never);
        _mockNotificationService.Verify(service => service.NotifyApproachingPayInLimit(It.IsAny<string>()), Times.Never);
    }

    [Fact]
    public void It_Transfers_Money_And_Notifies_On_Low_Balance()
    {
        var fromAccount = Account.Create(Guid.NewGuid(), new User(), 100);
        var toAccount = Account.Create(Guid.NewGuid(), new User(), 0);

        var mockRepo = Mock.Of<IAccountRepository>(repo =>
            repo.GetAccountById(fromAccount.Id) == fromAccount &&
            repo.GetAccountById(toAccount.Id) == toAccount);
        
        var transferMoney = new TransferMoney(mockRepo, _mockNotificationService.Object);
        transferMoney.Execute(fromAccount.Id, toAccount.Id, amount: 30);

        fromAccount.Withdrawn.Should().Be(30);
        toAccount.PaidIn.Should().Be(30);
        
        fromAccount.Balance.Should().Be(70);
        toAccount.Balance.Should().Be(30);

        // Notification triggers at < 500
        _mockNotificationService.Verify(service => service.NotifyFundsLow(It.IsAny<string>()), Times.Once);
        _mockNotificationService.Verify(service => service.NotifyApproachingPayInLimit(It.IsAny<string>()), Times.Never);
    }
    
    [Fact]
    public void It_Transfers_Money_And_Notifies_On_Approaching_Pay_In_Limit()
    {
        var fromAccount = Account.Create(Guid.NewGuid(), new User(), 6000);
        var toAccount = Account.Create(Guid.NewGuid(), new User(), 0);

        var mockRepo = Mock.Of<IAccountRepository>(repo =>
            repo.GetAccountById(fromAccount.Id) == fromAccount &&
            repo.GetAccountById(toAccount.Id) == toAccount);
        
        // Current limit is set to 4000
        const decimal amountNearPayInLimit = 3900;
        
        var transferMoney = new TransferMoney(mockRepo, _mockNotificationService.Object);
        transferMoney.Execute(fromAccount.Id, toAccount.Id, amountNearPayInLimit);
        
        fromAccount.Withdrawn.Should().Be(3900);
        toAccount.PaidIn.Should().Be(3900);
        
        fromAccount.Balance.Should().Be(2100);
        toAccount.Balance.Should().Be(3900);

        _mockNotificationService.Verify(service => service.NotifyFundsLow(It.IsAny<string>()), Times.Never);
        _mockNotificationService.Verify(service => service.NotifyApproachingPayInLimit(It.IsAny<string>()), Times.Once);
    }
}