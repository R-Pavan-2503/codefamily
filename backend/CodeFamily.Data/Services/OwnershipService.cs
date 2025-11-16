using CodeFamily.Core.Entities;
using CodeFamily.Data; // Already in this namespace
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using CodeFamily.Core.Services; // Add this to find the interface

namespace CodeFamily.Data.Services // <-- This namespace is new
{
    public class OwnershipService : IOwnershipService // <-- Implements the interface
    {
        private readonly CodeFamilyDbContext _context;
        private readonly ILogger<OwnershipService> _logger;

        public OwnershipService(
            CodeFamilyDbContext context,
            ILogger<OwnershipService> logger)
        {
            _context = context;
            _logger = logger;
        }

        // We use the full name to avoid ambiguity
        public async Task CalculateOwnershipAsync(Guid repositoryId, CancellationToken stoppingToken)
        {
            _logger.LogInformation("Starting ownership calculation for repository: {RepoId}", repositoryId);

            // 1. Clear all old ownership data for this repo
            _logger.LogInformation("[{RepoId}] Deleting old ownership data...", repositoryId);
            await _context.FileOwnerships
                .Where(fo => fo.File.RepositoryId == repositoryId)
                .ExecuteDeleteAsync(stoppingToken);

            // 2. This is the Temporal Ownership Algorithm, written in SQL
            // It calculates a score for every (user, file) pair and inserts it
            // directly into the "FileOwnership" table.
            _logger.LogInformation("[{RepoId}] Running ownership calculation query...", repositoryId);

            var sql = $"""
                WITH
                FileChangesWithAuthor AS (
                    -- Step 1: Get all file changes and join with the commit author (user)
                    SELECT 
                        fc."FileId",
                        c."AuthorEmail",
                        c."CommittedAt",
                        (fc."Additions" + fc."Deletions") AS lines_changed
                    FROM "FileChanges" fc
                    JOIN "Commits" c ON fc."CommitId" = c."Id"
                    JOIN "RepositoryFiles" rf ON fc."FileId" = rf."Id"
                    WHERE rf."RepositoryId" = {repositoryId}
                ),
                ContributionStats AS (
                    -- Step 2: Calculate recency and total lines for each (user, file)
                    SELECT
                        fc."FileId",
                        u."Id" AS "UserId",
                        MAX(fc."CommittedAt") AS last_commit_date,
                        SUM(fc.lines_changed) AS total_lines_changed
                    FROM FileChangesWithAuthor fc
                    JOIN "Users" u ON fc."AuthorEmail" = u."Email"
                    GROUP BY fc."FileId", u."Id"
                ),
                ScoredContributions AS (
                    -- Step 3: Apply the scoring algorithm
                    -- (0.4 * Recency) + (0.3 * Volume)
                    -- We will add the 0.2 Review and 0.1 Author bonus later
                    SELECT
                        cs."FileId",
                        cs."UserId",
                        -- Recency Score (40% weight): Exponential decay, 180-day half-life
                        (0.4 * EXP(-EXTRACT(EPOCH FROM (NOW() - cs.last_commit_date)) / (180.0 * 86400.0))) 
                        AS recency_score,
                        
                        -- Volume Score (30% weight): Normalized by total lines in file
                        -- (We'll use total_lines_changed for now as a simple proxy)
                        (0.3 * (cs.total_lines_changed / (SUM(cs.total_lines_changed) OVER (PARTITION BY cs."FileId") + 1.0)))
                        AS volume_score
                    FROM ContributionStats cs
                )
                -- Step 4: Insert the final scores into the FileOwnership table
                INSERT INTO "FileOwnerships" ("FileId", "UserId", "Score", "LastUpdated")
                SELECT
                    sc."FileId",
                    sc."UserId",
                    -- Final score = (recency_score + volume_score)
                    -- We multiply by 100 to get a 0-100 scale
                    (sc.recency_score + sc.volume_score) * 100 AS total_score,
                    NOW()
                FROM ScoredContributions sc
                WHERE (sc.recency_score + sc.volume_score) > 0.01; -- Ignore tiny scores
            """;

            // 3. Execute the raw SQL query
            await _context.Database.ExecuteSqlRawAsync(sql, stoppingToken);

            _logger.LogInformation("Finished ownership calculation for repository: {RepoId}", repositoryId);
        }
    }
}