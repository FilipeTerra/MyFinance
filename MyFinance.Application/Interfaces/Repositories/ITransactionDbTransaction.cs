using System;
using System.Threading.Tasks;

namespace MyFinance.Application.Interfaces.Repositories;

public interface ITransactionDbTransaction : IAsyncDisposable
{
    Task CommitAsync();
    Task RollbackAsync();
}
