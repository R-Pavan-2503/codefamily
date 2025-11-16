using CodeFamily.Core.Entities;

namespace CodeFamily.Data.Services
{
    public interface IRepositoryIngestionService
    {
        Task ProcessRepositoryAsync(Repository repository, CancellationToken stoppingToken);
    }
}