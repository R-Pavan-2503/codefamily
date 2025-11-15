using CodeFamily.Core.Enums;

namespace CodeFamily.Core.Entities
{
    public class Repository
    {
        // Primary Key
        public Guid Id { get; set; }

        // Data Columns
        public long GithubRepoId { get; set; }
        public string Name { get; set; } = string.Empty;
        public string OwnerUsername { get; set; } = string.Empty;
        public RepoStatus Status { get; set; } = RepoStatus.Queued;
        public DateTimeOffset CreatedAt { get; set; } = DateTimeOffset.UtcNow;

        // Foreign Key for the User who connected this repo
        public Guid ConnectedByUserId { get; set; }

        // Navigation Properties
        // A Repository is connected by one User
        public User ConnectedByUser { get; set; } = null!;

        // A Repository has many Collaborators
        public ICollection<RepositoryCollaborator> Collaborators { get; set; } = new List<RepositoryCollaborator>();

        // A Repository has many Commits
        public ICollection<Commit> Commits { get; set; } = new List<Commit>();

        // A Repository has many Files
        public ICollection<RepositoryFile> Files { get; set; } = new List<RepositoryFile>();

        // A Repository has many Pull Requests
        public ICollection<PullRequest> PullRequests { get; set; } = new List<PullRequest>();
    }
}