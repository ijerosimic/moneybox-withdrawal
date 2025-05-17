using FluentAssertions;
using Moneybox.App.DataAccess;
using Moneybox.App.Domain.Services;
using Moneybox.App.Features;
using Moq;

namespace Moneybox.App.Test.WithdrawMoneyTests;

public class WithdrawMoneyFailureTests
{
    private readonly Mock<INotificationService> _mockNotificationService;

    public WithdrawMoneyFailureTests()
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
    public void It_Does_Not_Withdraw_Money_When_From_Balance_Is_Too_Low()
    {
        var fromAccount = Account.Create(Guid.NewGuid(), new User(), 0);

        var mockRepo = new Mock<IAccountRepository>();
        mockRepo
            .Setup(repo => repo.GetAccountById(fromAccount.Id))
            .Returns(fromAccount);
        mockRepo
            .Setup(repo => repo.Update(It.IsAny<Account>()))
            .Verifiable();
        
        var withdrawMoney = new WithdrawMoney(mockRepo.Object, _mockNotificationService.Object);
        
        var act = () => withdrawMoney.Execute(fromAccount.Id, 100);
        act.Should().ThrowExactly<InvalidOperationException>().WithMessage("Insufficient funds to make transfer");

        fromAccount.Withdrawn.Should().Be(0);
        fromAccount.Balance.Should().Be(0);
        
        mockRepo.Verify(repo => repo.Update(It.IsAny<Account>()), Times.Never());
        
        _mockNotificationService.Verify(service => service.NotifyFundsLow(It.IsAny<string>()), Times.Never);
        _mockNotificationService.Verify(service => service.NotifyApproachingPayInLimit(It.IsAny<string>()), Times.Never);
    }
}