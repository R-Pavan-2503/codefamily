namespace CodeFamily.Core.Entities
{
    public class PullRequest
    {
        // Primary Key
        public Guid Id { get; set; }

        // Data Columns
        public long GithubPrId { get; set; }
        public int PrNumber { get; set; }
        public string State { get; set; } = string.Empty;
        public DateTimeOffset CreatedAt { get; set; }
        public DateTimeOffset? MergedAt { get; set; }
        public DateTimeOffset? ClosedAt { get; set; }

        // Foreign Key for the Repository
        public Guid RepositoryId { get; set; }

        // Foreign Key for the Author (nullable)
        public Guid? AuthorId { get; set; }

        // Navigation Properties
        public Repository Repository { get; set; } = null!;

        // A PR's author might not be a user in our system, so it's nullable
        public User? Author { get; set; }

        // A PR changes many Files
        public ICollection<PrFileChange> FilesChanged { get; set; } = new List<PrFileChange>();

        // A PR has many Reviews
        public ICollection<Review> Reviews { get; set; } = new List<Review>();
    }
}