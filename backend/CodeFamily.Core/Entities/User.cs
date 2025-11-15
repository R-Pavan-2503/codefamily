namespace CodeFamily.Core.Entities
{
    public class User
    {
        // Primary Key
        public Guid Id { get; set; }

        // Data Columns
        public long GithubId { get; set; }
        public string Username { get; set; } = string.Empty;
        public string? Email { get; set; }
        public string? AvatarUrl { get; set; }
        public DateTimeOffset CreatedAt { get; set; } = DateTimeOffset.UtcNow;

        // Navigation Properties
        // A User can be a collaborator on many Repositories
        public ICollection<RepositoryCollaborator> Collaborations { get; set; } = new List<RepositoryCollaborator>();

        // A User can be the primary owner of many Repositories
        public ICollection<Repository> OwnedRepositories { get; set; } = new List<Repository>();

        // A User can have ownership of many Files
        public ICollection<FileOwnership> FileOwnerships { get; set; } = new List<FileOwnership>();

        // A User can author many Pull Requests
        public ICollection<PullRequest> PullRequests { get; set; } = new List<PullRequest>();

        // A User can perform many Reviews
        public ICollection<Review> Reviews { get; set; } = new List<Review>();
    }
}