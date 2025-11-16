using CodeFamily.Data;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System.Security.Claims;

namespace CodeFamily.API.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    [Authorize] // <--- This is new and very important
    public class RepositoryController : ControllerBase
    {
        private readonly CodeFamilyDbContext _context;
        private readonly ILogger<RepositoryController> _logger;

        public RepositoryController(
            CodeFamilyDbContext context,
            ILogger<RepositoryController> logger)
        {
            _context = context;
            _logger = logger;
        }

        // Helper method to get the currently logged-in user's ID
        private Guid GetUserId()
        {
            var userIdString = User.FindFirstValue(ClaimTypes.NameIdentifier);
            if (string.IsNullOrEmpty(userIdString))
            {
                throw new InvalidOperationException("User ID not found in token.");
            }
            return new Guid(userIdString);
        }

        public class ConnectRepoRequest
        {
            public string Owner { get; set; } = string.Empty;
            public string RepoName { get; set; } = string.Empty;
            public long GithubRepoId { get; set; }
        }

        [HttpPost("connect")]
        public async Task<IActionResult> ConnectRepository([FromBody] ConnectRepoRequest request)
        {
            try
            {
                // 1. Get the ID of the currently logged-in user
                var userId = GetUserId();

                // 2. Check if this repository is already connected
                var existingRepo = await _context.Repositories
                    .FirstOrDefaultAsync(r => r.GithubRepoId == request.GithubRepoId);

                if (existingRepo != null)
                {
                    // It's already in our system. Just make sure this user is a collaborator.
                    var isCollaborator = await _context.RepositoryCollaborators
                        .AnyAsync(rc => rc.RepositoryId == existingRepo.Id && rc.UserId == userId);

                    if (!isCollaborator)
                    {
                        // Add this user as a collaborator to the existing repo
                        _context.RepositoryCollaborators.Add(new Core.Entities.RepositoryCollaborator
                        {
                            RepositoryId = existingRepo.Id,
                            UserId = userId
                        });
                        await _context.SaveChangesAsync();
                    }

                    return Ok(existingRepo); // Return the existing repo
                }

                // 3. If it's a new repo, create the entity
                var newRepo = new Core.Entities.Repository
                {
                    Id = Guid.NewGuid(),
                    GithubRepoId = request.GithubRepoId,
                    Name = request.RepoName,
                    OwnerUsername = request.Owner,
                    ConnectedByUserId = userId,
                    Status = Core.Enums.RepoStatus.Queued // Set the initial status to 'Queued'
                };

                // 4. Also add the connecting user as the first collaborator
                var newCollaborator = new Core.Entities.RepositoryCollaborator
                {
                    RepositoryId = newRepo.Id,
                    UserId = userId
                };

                // 5. Add them to the database and save
                _context.Repositories.Add(newRepo);
                _context.RepositoryCollaborators.Add(newCollaborator);
                await _context.SaveChangesAsync();

                // 6. Return the newly created repository object
                return CreatedAtAction(nameof(GetRepository), new { repoId = newRepo.Id }, newRepo);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error connecting new repository for user {UserId}", GetUserId());
                return StatusCode(500, "An internal error occurred.");
            }
        }

        // This is a "dummy" method for CreatedAtAction. We will build this out later.
        [HttpGet("{repoId}")]
        public async Task<IActionResult> GetRepository(Guid repoId)
        {
            var userId = GetUserId();
            var isCollaborator = await _context.RepositoryCollaborators
                .AnyAsync(rc => rc.RepositoryId == repoId && rc.UserId == userId);

            if (!isCollaborator)
            {
                return Forbid(); // 403 Forbidden
            }

            var repo = await _context.Repositories.FindAsync(repoId);
            if (repo == null)
            {
                return NotFound();
            }

            return Ok(repo);
        }

        // We will add our API endpoints (methods) here in the next steps...
        // e.g., POST /api/repository/connect
        // e.g., GET /api/repository/{id}/status
    }
}