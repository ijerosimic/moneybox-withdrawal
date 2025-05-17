using FluentAssertions;
using Moneybox.App.DataAccess;
using Moneybox.App.Domain.Services;
using Moneybox.App.Features;
using Moq;

namespace Moneybox.App.Test.TransferMoneyTests;

public class TransferMoneyFailureTests
{
    private readonly Mock<INotificationService> _mockNotificationService;

    public TransferMoneyFailureTests()
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
    public void It_Does_Not_Transfer_Money_When_From_Balance_Is_Too_Low()
    {
        var fromAccount = Account.Create(Guid.NewGuid(), new User(), 0);
        var toAccount = Account.Create(Guid.NewGuid(), new User(), 500);

        var mockRepo = new Mock<IAccountRepository>();
        mockRepo
            .Setup(repo => repo.GetAccountById(fromAccount.Id))
            .Returns(fromAccount)
            .Verifiable();
        mockRepo
            .Setup(repo => repo.GetAccountById(toAccount.Id))
            .Returns(toAccount)
            .Verifiable();
        mockRepo
            .Setup(repo => repo.Update(It.IsAny<Account>()))
            .Verifiable();
        
        var transferMoney = new TransferMoney(mockRepo.Object, _mockNotificationService.Object);
        
        var act = () => transferMoney.Execute(fromAccount.Id, toAccount.Id, 100);
        act.Should().ThrowExactly<InvalidOperationException>().WithMessage("Insufficient funds to make transfer");

        fromAccount.Withdrawn.Should().Be(0);
        toAccount.PaidIn.Should().Be(0);
        
        fromAccount.Balance.Should().Be(0);
        toAccount.Balance.Should().Be(500);
         
        mockRepo.Verify(repo => repo.Update(It.IsAny<Account>()), Times.Never());
        
        _mockNotificationService.Verify(service => service.NotifyFundsLow(It.IsAny<string>()), Times.Never);
        _mockNotificationService.Verify(service => service.NotifyApproachingPayInLimit(It.IsAny<string>()), Times.Never);
    }
    
    [Fact]
    public void It_Does_Not_Transfer_Money_When_Pay_In_Limit_Reached()
    { 
        var fromAccount = Account.Create(Guid.NewGuid(), new User(), 10000);
        var toAccount = Account.Create(Guid.NewGuid(), new User(), 500);

        var mockRepo = new Mock<IAccountRepository>();
        mockRepo
            .Setup(repo => repo.GetAccountById(fromAccount.Id))
            .Returns(fromAccount)
            .Verifiable();
        mockRepo
            .Setup(repo => repo.GetAccountById(toAccount.Id))
            .Returns(toAccount)
            .Verifiable();
        mockRepo
            .Setup(repo => repo.Update(It.IsAny<Account>()))
            .Verifiable();
        
        // Current limit is set to 4000
        const decimal amountGreaterThanLimit = 5000;
        var transferMoney = new TransferMoney(mockRepo.Object, _mockNotificationService.Object);
        
        var act = () => transferMoney.Execute(fromAccount.Id, toAccount.Id, amountGreaterThanLimit);
        act.Should().ThrowExactly<InvalidOperationException>().WithMessage("Account pay in limit reached");

        fromAccount.Withdrawn.Should().Be(0);
        toAccount.PaidIn.Should().Be(0);
        
        fromAccount.Balance.Should().Be(10000);
        toAccount.Balance.Should().Be(500);
        
        mockRepo.Verify(repo => repo.Update(It.IsAny<Account>()), Times.Never());
        
        _mockNotificationService.Verify(service => service.NotifyFundsLow(It.IsAny<string>()), Times.Never);
        _mockNotificationService.Verify(service => service.NotifyApproachingPayInLimit(It.IsAny<string>()), Times.Never);
    }
}