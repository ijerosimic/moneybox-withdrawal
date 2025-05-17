using FluentAssertions;
using Moneybox.App.DataAccess;
using Moneybox.App.Domain.Services;
using Moneybox.App.Features;
using Moq;

namespace Moneybox.App.Test.WithdrawMoneyTests;

public class WithdrawMoneySuccessTests
{
    private readonly Mock<INotificationService> _mockNotificationService;

    public WithdrawMoneySuccessTests()
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
    public void It_Withdraws_Money()
    {
        var fromAccount = Account.Create(Guid.NewGuid(), new User(), 5000);

        var mockRepo = new Mock<IAccountRepository>();
        mockRepo
            .Setup(repo => repo.GetAccountById(fromAccount.Id))
            .Returns(fromAccount);
        mockRepo
            .Setup(repo => repo.Update(It.IsAny<Account>()))
            .Verifiable();
        
        var withdrawMoney = new WithdrawMoney(mockRepo.Object, _mockNotificationService.Object);
        withdrawMoney.Execute(fromAccount.Id,  amount: 30);

        fromAccount.Withdrawn.Should().Be(30);
        fromAccount.Balance.Should().Be(4970);
        
        mockRepo.Verify(repo => repo.Update(It.IsAny<Account>()), Times.Once);

        _mockNotificationService.Verify(service => service.NotifyFundsLow(It.IsAny<string>()), Times.Never);
        _mockNotificationService.Verify(service => service.NotifyApproachingPayInLimit(It.IsAny<string>()), Times.Never);
    }

    [Fact]
    public void It_Withdraws_Money_And_Notifies_On_Low_Balance()
    {
        var fromAccount = Account.Create(Guid.NewGuid(), new User(), 100);

        var mockRepo = new Mock<IAccountRepository>();
        mockRepo
            .Setup(repo => repo.GetAccountById(fromAccount.Id))
            .Returns(fromAccount);
        mockRepo
            .Setup(repo => repo.Update(It.IsAny<Account>()))
            .Verifiable();
        
        var withdrawMoney = new WithdrawMoney(mockRepo.Object, _mockNotificationService.Object);
        withdrawMoney.Execute(fromAccount.Id,  amount: 30);

        fromAccount.Withdrawn.Should().Be(30);
        fromAccount.Balance.Should().Be(70);
        
        mockRepo.Verify(repo => repo.Update(It.IsAny<Account>()), Times.Once);

        // Notification triggers at < 500
        _mockNotificationService.Verify(service => service.NotifyFundsLow(It.IsAny<string>()), Times.Once);
        _mockNotificationService.Verify(service => service.NotifyApproachingPayInLimit(It.IsAny<string>()), Times.Never);
    }
}