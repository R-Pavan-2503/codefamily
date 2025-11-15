namespace CodeFamily.Core.Entities
{
    public class FileOwnership
    {
        // Data Columns
        public decimal Score { get; set; } // Using decimal for high precision
        public DateTimeOffset LastUpdated { get; set; } = DateTimeOffset.UtcNow;

        // Foreign Key for the File
        public Guid FileId { get; set; }

        // Foreign Key for the User
        public Guid UserId { get; set; }

        // Navigation Properties
        public RepositoryFile File { get; set; } = null!;
        public User User { get; set; } = null!;
    }
}