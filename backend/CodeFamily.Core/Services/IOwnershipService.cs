using CodeFamily.Core.Entities;

namespace CodeFamily.Core.Services
{
    public interface IOwnershipService
    {
        Task CalculateOwnershipAsync(Guid repositoryId, CancellationToken stoppingToken);
    }
}