namespace CodeFamily.Core.Entities
{
    public class Commit
    {
        // Primary Key
        public Guid Id { get; set; }

        // Data Columns
        public string Sha { get; set; } = string.Empty;
        public string AuthorName { get; set; } = string.Empty;
        public string AuthorEmail { get; set; } = string.Empty;
        public string? Message { get; set; }
        public DateTimeOffset CommittedAt { get; set; }

        // Foreign Key for the Repository
        public Guid RepositoryId { get; set; }

        // Navigation Properties
        // A Commit belongs to one Repository
        public Repository Repository { get; set; } = null!;

        // A Commit involves changes to many Files
        public ICollection<FileChange> FileChanges { get; set; } = new List<FileChange>();
    }
}