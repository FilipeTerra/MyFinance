using Moq;
using MyFinance.Application.Interfaces.Repositories;

namespace MyFinance.Application.Tests.TestHelpers;

/// <summary>
/// Fábrica de mock para <see cref="ITransactionDbTransaction"/>, usado pelos services
/// que envolvem operações atômicas via BeginTransactionAsync/Commit/Rollback.
/// </summary>
public static class MockDbTransaction
{
    public static Mock<ITransactionDbTransaction> Create()
    {
        var mock = new Mock<ITransactionDbTransaction>();
        mock.Setup(t => t.CommitAsync()).Returns(Task.CompletedTask);
        mock.Setup(t => t.RollbackAsync()).Returns(Task.CompletedTask);
        mock.Setup(t => t.DisposeAsync()).Returns(ValueTask.CompletedTask);
        return mock;
    }
}
