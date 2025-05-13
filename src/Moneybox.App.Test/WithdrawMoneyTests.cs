using FluentAssertions;
using Moneybox.App.DataAccess;
using Moneybox.App.Domain.Services;
using Moneybox.App.Features;
using Moq;

namespace Moneybox.App.Test;

public class TransferMoneyTests
{
    private readonly Guid _fromAccountId = Guid.NewGuid();
    private readonly Guid _toAccountId = Guid.NewGuid();
    private readonly Account _fromAccount;
    private readonly Account _toAccount;
    private readonly Mock<INotificationService> _mockNotificationService;
    private readonly IAccountRepository _mockRepo;

    public TransferMoneyTests()
    {
        _fromAccount = Account.Create(_fromAccountId, new User(), 10000);
        _toAccount = Account.Create(_toAccountId, new User(), 500);

        _mockRepo = Mock.Of<IAccountRepository>(repo =>
            repo.GetAccountById(_fromAccountId) == _fromAccount &&
            repo.GetAccountById(_toAccountId) == _toAccount);

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
        var transferMoney = new TransferMoney(_mockRepo, _mockNotificationService.Object);
        transferMoney.Execute(_fromAccountId, _toAccountId, amount: 100);

        _fromAccount.Balance.Should().Be(9900);
        _fromAccount.Withdrawn.Should().Be(100);
        
        _fromAccount.Withdrawn.Should().Be(100);
        _toAccount.PaidIn.Should().Be(100);

        _mockNotificationService.Verify(service => service.NotifyFundsLow(It.IsAny<string>()), Times.Never);
        _mockNotificationService.Verify(service => service.NotifyApproachingPayInLimit(It.IsAny<string>()), Times.Never);
    }

    [Fact]
    public void It_Transfers_Money_And_Notifies_On_Low_Balance()
    {
        _fromAccount.Withdraw(9000);
        _fromAccount.Withdrawn.Should().Be(9000);
        _fromAccount.Balance.Should().Be(1000);
        
        var transferMoney = new TransferMoney(_mockRepo, _mockNotificationService.Object);
        transferMoney.Execute(_fromAccountId, _toAccountId, amount: 800);

        _fromAccount.Balance.Should().Be(200);
        _toAccount.Balance.Should().Be(1300);
        
        _fromAccount.Withdrawn.Should().Be(9800);
        _toAccount.PaidIn.Should().Be(800);

        _mockNotificationService.Verify(service => service.NotifyFundsLow(It.IsAny<string>()), Times.Once);
        _mockNotificationService.Verify(service => service.NotifyApproachingPayInLimit(It.IsAny<string>()), Times.Never);
    }
    
    [Fact]
    public void It_Transfers_Money_And_Notifies_On_Approaching_Pay_In_Limit()
    {
        const decimal amountNearPayInLimit = Account.PayInLimit - 100;
        
        var transferMoney = new TransferMoney(_mockRepo, _mockNotificationService.Object);
        transferMoney.Execute(_fromAccountId, _toAccountId, amountNearPayInLimit);

        _fromAccount.Balance.Should().Be(6100);
        _toAccount.Balance.Should().Be(4400);
        
        _fromAccount.Withdrawn.Should().Be(3900);
        _toAccount.PaidIn.Should().Be(3900);

        _mockNotificationService.Verify(service => service.NotifyFundsLow(It.IsAny<string>()), Times.Never);
        _mockNotificationService.Verify(service => service.NotifyApproachingPayInLimit(It.IsAny<string>()), Times.Once);
    }
    
    [Fact]
    public void It_Does_Not_Transfer_Money_When_From_Balance_Is_Too_Low()
    {
        _fromAccount.Withdraw(9000);
        _fromAccount.Withdrawn.Should().Be(9000);
        _fromAccount.Balance.Should().Be(1000);
        
        var amountGreaterThanBalance = _fromAccount.Balance + 500;
        
        var transferMoney = new TransferMoney(_mockRepo, _mockNotificationService.Object);
        var act = () => transferMoney.Execute(_fromAccountId, _toAccountId, amountGreaterThanBalance);
        act.Should().Throw<InvalidOperationException>().WithMessage("Insufficient funds to make transfer");

        _fromAccount.Balance.Should().Be(1000);
        _toAccount.Balance.Should().Be(500);
        
        _fromAccount.Withdrawn.Should().Be(9000);
        _toAccount.PaidIn.Should().Be(0);

        _mockNotificationService.Verify(service => service.NotifyFundsLow(It.IsAny<string>()), Times.Never);
        _mockNotificationService.Verify(service => service.NotifyApproachingPayInLimit(It.IsAny<string>()), Times.Never);
    }
    
    [Fact]
    public void It_Does_Not_Transfer_Money_When_Pay_In_Limit_Reached()
    { 
        const decimal amountGreaterThanLimit = Account.PayInLimit + 1000;
        
        var transferMoney = new TransferMoney(_mockRepo, _mockNotificationService.Object);
        var act = () => transferMoney.Execute(_fromAccountId, _toAccountId, amountGreaterThanLimit);
        act.Should().Throw<InvalidOperationException>().WithMessage("Account pay in limit reached");

        _fromAccount.Balance.Should().Be(10000);
        _toAccount.Balance.Should().Be(500);
        
        _fromAccount.Withdrawn.Should().Be(0);
        _toAccount.PaidIn.Should().Be(0);

        _mockNotificationService.Verify(service => service.NotifyFundsLow(It.IsAny<string>()), Times.Never);
        _mockNotificationService.Verify(service => service.NotifyApproachingPayInLimit(It.IsAny<string>()), Times.Never);
    }
    
    [Fact]
    public void It_Transfers_Money_In_Multiple_Transactions_Until_Pay_In_Limit_Reached()
    { 
        var transferMoney = new TransferMoney(_mockRepo, _mockNotificationService.Object);
    
        transferMoney.Execute(_fromAccountId, _toAccountId, 1500);
        
        _fromAccount.Balance.Should().Be(8500);
        _toAccount.Balance.Should().Be(2000);

        _fromAccount.Withdrawn.Should().Be(1500);
        _toAccount.PaidIn.Should().Be(1500);
        
        _mockNotificationService.Verify(service => service.NotifyFundsLow(It.IsAny<string>()), Times.Never);
        _mockNotificationService.Verify(service => service.NotifyApproachingPayInLimit(It.IsAny<string>()), Times.Never);
        
        transferMoney.Execute(_fromAccountId, _toAccountId, 2500);
        
        _fromAccount.Balance.Should().Be(6000);
        _toAccount.Balance.Should().Be(4500);
        
        _fromAccount.Withdrawn.Should().Be(4000);
        _toAccount.PaidIn.Should().Be(4000);
        
        _mockNotificationService.Verify(service => service.NotifyFundsLow(It.IsAny<string>()), Times.Never);
        _mockNotificationService.Verify(service => service.NotifyApproachingPayInLimit(It.IsAny<string>()), Times.Once);

        var act = () =>  transferMoney.Execute(_fromAccountId, _toAccountId, 1500);
        act.Should().Throw<InvalidOperationException>().WithMessage("Account pay in limit reached");

        _fromAccount.Balance.Should().Be(6000);
        _toAccount.Balance.Should().Be(4500);
        
        _fromAccount.Withdrawn.Should().Be(4000);
        _toAccount.PaidIn.Should().Be(4000);

        _mockNotificationService.Verify(service => service.NotifyFundsLow(It.IsAny<string>()), Times.Never);
        _mockNotificationService.Verify(service => service.NotifyApproachingPayInLimit(It.IsAny<string>()), Times.Once);
    }
}