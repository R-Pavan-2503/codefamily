using CodeFamily.Core.Entities;
using CodeFamily.Data;
using Microsoft.Extensions.Logging;
using LibGit2Sharp;
using System.IO;
using Microsoft.EntityFrameworkCore;

namespace CodeFamily.Data.Services
{
    public class RepositoryIngestionService : IRepositoryIngestionService
    {
        private readonly CodeFamilyDbContext _context;
        private readonly ILogger<RepositoryIngestionService> _logger;

        public RepositoryIngestionService(
            CodeFamilyDbContext context,
            ILogger<RepositoryIngestionService> logger)
        {
            _context = context;
            _logger = logger;
        }

        public async Task ProcessRepositoryAsync(CodeFamily.Core.Entities.Repository repository, CancellationToken stoppingToken)
        {
            _logger.LogInformation("[{RepoName}] Starting processing...", repository.Name);

            var cloneUrl = $"https://github.com/{repository.OwnerUsername}/{repository.Name}.git";
            var localPath = Path.Combine(Path.GetTempPath(), "codefamily_repos", repository.Id.ToString());

            // --- Caching ---
            // These dictionaries are a in-memory cache.
            // This prevents us from hitting the database 10,000 times for the same user or file.
            var userCache = new Dictionary<string, Core.Entities.User>();
            var fileCache = new Dictionary<string, Core.Entities.RepositoryFile>();

            try
            {
                if (Directory.Exists(localPath))
                {
                    _logger.LogWarning("[{RepoName}] Deleting existing directory: {Path}", repository.Name, localPath);
                    Directory.Delete(localPath, recursive: true);
                }
                Directory.CreateDirectory(localPath);

                _logger.LogInformation("[{RepoName}] Cloning from {Url} into {Path}...", repository.Name, cloneUrl, localPath);
                LibGit2Sharp.Repository.Clone(cloneUrl, localPath, new CloneOptions { IsBare = true });

                _logger.LogInformation("[{RepoName}] Clone complete. Opening repository...", repository.Name);

                using (var repo = new LibGit2Sharp.Repository(localPath))
                {
                    _logger.LogInformation("[{RepoName}] Iterating commits...", repository.Name);

                    var commitCount = 0;
                    foreach (var gitCommit in repo.Commits.QueryBy(new CommitFilter { SortBy = CommitSortStrategies.Topological }))
                    {
                        if (stoppingToken.IsCancellationRequested) throw new OperationCanceledException();

                        // --- 1. Find or Create the User (Author) ---
                        var authorEmail = gitCommit.Author.Email;
                        if (!userCache.TryGetValue(authorEmail, out var commitAuthor))
                        {
                            commitAuthor = await _context.Users.FirstOrDefaultAsync(u => u.Email == authorEmail);
                            if (commitAuthor == null)
                            {
                                // This is a new user we haven't seen. Create a "stub" user.
                                commitAuthor = new Core.Entities.User
                                {
                                    Id = Guid.NewGuid(),
                                    GithubId = 0, // We don't know their GitHub ID yet
                                    Username = gitCommit.Author.Name,
                                    Email = authorEmail
                                };
                                _context.Users.Add(commitAuthor);
                            }
                            userCache[authorEmail] = commitAuthor;
                        }

                        // --- 2. Create the Commit Entity ---
                        var dbCommit = new Core.Entities.Commit
                        {
                            Id = Guid.NewGuid(),
                            RepositoryId = repository.Id,
                            Sha = gitCommit.Sha,
                            AuthorName = gitCommit.Author.Name,
                            AuthorEmail = authorEmail,
                            Message = gitCommit.MessageShort,
                            CommittedAt = gitCommit.Author.When
                        };
                        _context.Commits.Add(dbCommit);

                        // --- 3. Find File Changes (Diff) ---
                        if (gitCommit.Parents.Any())
                        {
                            var parent = gitCommit.Parents.First();
                            var changes = repo.Diff.Compare<Patch>(parent.Tree, gitCommit.Tree);
                            foreach (var change in changes)
                            {
                                if (change.Status == ChangeKind.Deleted) continue; // Skip deleted files

                                // --- 4. Find or Create the RepositoryFile ---
                                var filePath = change.Path;
                                if (!fileCache.TryGetValue(filePath, out var dbFile))
                                {
                                    dbFile = await _context.RepositoryFiles.FirstOrDefaultAsync(f => f.RepositoryId == repository.Id && f.FilePath == filePath);
                                    if (dbFile == null)
                                    {
                                        dbFile = new Core.Entities.RepositoryFile
                                        {
                                            Id = Guid.NewGuid(),
                                            RepositoryId = repository.Id,
                                            FilePath = filePath,
                                            TotalLines = 0 // We'll update this later
                                        };
                                        _context.RepositoryFiles.Add(dbFile);
                                    }
                                    fileCache[filePath] = dbFile;
                                }

                                // --- 5. Create the FileChange Entity ---
                                // Note: LibGit2Sharp doesn't give line counts for bare repos easily.
                                // We'll set placeholder values for now. This is a known limitation.
                                var dbFileChange = new Core.Entities.FileChange
                                {
                                    CommitId = dbCommit.Id,
                                    FileId = dbFile.Id,
                                    Additions = change.LinesAdded,
                                    Deletions = change.LinesDeleted
                                };
                                _context.FileChanges.Add(dbFileChange);
                            }
                        }

                        commitCount++;
                        if (commitCount % 100 == 0) // Save changes every 100 commits
                        {
                            _logger.LogInformation("[{RepoName}]... processed {Count} commits. Saving changes...", repository.Name, commitCount);
                            await _context.SaveChangesAsync(stoppingToken);
                        }
                    }

                    // Save any remaining changes
                    _logger.LogInformation("[{RepoName}] Processed {Count} total commits. Saving final changes...", repository.Name, commitCount);
                    await _context.SaveChangesAsync(stoppingToken);
                }

                _logger.LogInformation("[{RepoName}] Processing complete.", repository.Name);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "[{RepoName}] Failed during repository processing.", repository.Name);
                throw; // Re-throw the exception so the worker can catch it
            }
            finally
            {
                try
                {
                    if (Directory.Exists(localPath))
                    {
                        Directory.Delete(localPath, recursive: true);
                        _logger.LogInformation("[{RepoName}] Cleaned up local directory: {Path}", repository.Name, localPath);
                    }
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "[{RepoName}] Failed to clean up directory: {Path}", repository.Name, localPath);
                }
            }
        }
    }
}