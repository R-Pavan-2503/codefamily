namespace CodeFamily.Core.Entities
{
    public class PrFileChange
    {
        // Foreign Key for the Pull Request
        public Guid PrId { get; set; }

        // Foreign Key for the File
        public Guid FileId { get; set; }

        // Navigation Properties
        public PullRequest PullRequest { get; set; } = null!;
        public RepositoryFile File { get; set; } = null!;
    }
}