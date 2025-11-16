using CodeFamily.Data;
using Microsoft.EntityFrameworkCore;
using CodeFamily.Data.Services;

namespace CodeFamily.API.Workers
{
    public class RepositoryIngestionWorker : BackgroundService
    {
        private readonly ILogger<RepositoryIngestionWorker> _logger;
        private readonly IServiceProvider _serviceProvider;

        public RepositoryIngestionWorker(
            ILogger<RepositoryIngestionWorker> logger,
            IServiceProvider serviceProvider)
        {
            _logger = logger;
            _serviceProvider = serviceProvider;
        }

        protected override async Task ExecuteAsync(CancellationToken stoppingToken)
        {
            _logger.LogInformation("Repository Ingestion Worker starting up...");

            while (!stoppingToken.IsCancellationRequested)
            {
                Core.Entities.Repository? repoToProcess = null;
                Guid repoId = Guid.Empty;

                try
                {
                    // === SCOPE 1: Find and Lock a Job ===
                    using (var scope = _serviceProvider.CreateScope())
                    {
                        var dbContext = scope.ServiceProvider.GetRequiredService<CodeFamilyDbContext>();

                        repoToProcess = await dbContext.Repositories
                            .FirstOrDefaultAsync(r => r.Status == Core.Enums.RepoStatus.Queued, stoppingToken);

                        if (repoToProcess != null)
                        {
                            // Lock the repo so no other worker picks it up
                            _logger.LogInformation("[{RepoName}] Found. Setting status to Cloning.", repoToProcess.Name);
                            repoToProcess.Status = Core.Enums.RepoStatus.Cloning;
                            await dbContext.SaveChangesAsync(stoppingToken);

                            // Save the ID for use outside the scope
                            repoId = repoToProcess.Id;
                        }
                    } // Scope 1 closes, DbContext is disposed

                    // === If we locked a job, process it ===
                    if (repoToProcess != null)
                    {
                        _logger.LogInformation("[{RepoName}] Starting ingestion process...", repoToProcess.Name);

                        // === SCOPE 2: Do the Heavy Lifting ===
                        // Create a new scope for the ingestion service
                        using (var scope = _serviceProvider.CreateScope())
                        {
                            var ingestionService = scope.ServiceProvider.GetRequiredService<IRepositoryIngestionService>();

                            // This is where the actual work happens
                            await ingestionService.ProcessRepositoryAsync(repoToProcess, stoppingToken);

                            // Work is done, now update the status
                            var dbContext = scope.ServiceProvider.GetRequiredService<CodeFamilyDbContext>();
                            _logger.LogInformation("[{RepoName}] Ingestion complete. Setting status to Ready.", repoToProcess.Name);

                            // We must 'Attach' the entity to this new DbContext
                            dbContext.Repositories.Attach(repoToProcess);
                            repoToProcess.Status = Core.Enums.RepoStatus.Ready;
                            await dbContext.SaveChangesAsync(stoppingToken);
                        }
                    }
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "An error occurred processing repository {RepoId}", repoId);

                    // === SCOPE 3: Handle Failure ===
                    if (repoId != Guid.Empty)
                    {
                        try
                        {
                            // Create a new, clean scope just to set the error status
                            using (var scope = _serviceProvider.CreateScope())
                            {
                                var dbContext = scope.ServiceProvider.GetRequiredService<CodeFamilyDbContext>();
                                var failedRepo = await dbContext.Repositories.FindAsync(repoId);
                                if (failedRepo != null)
                                {
                                    _logger.LogError("[{RepoName}] Setting status to Error.", failedRepo.Name);
                                    failedRepo.Status = Core.Enums.RepoStatus.Error;
                                    await dbContext.SaveChangesAsync(stoppingToken);
                                }
                            }
                        }
                        catch (Exception e)
                        {
                            _logger.LogError(e, "CRITICAL: Failed to set repo {RepoId} status to Error.", repoId);
                        }
                    }
                }

                // === Wait before checking for a new job ===
                if (repoToProcess == null)
                {
                    _logger.LogInformation("No new repos found. Waiting 10 seconds...");
                    await Task.Delay(TimeSpan.FromSeconds(10), stoppingToken);
                }
                // If we DID process a repo, don't wait. Immediately check for the next one.
            }
        }
    }
}