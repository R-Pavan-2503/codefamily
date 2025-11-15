namespace CodeFamily.Core.Entities
{
    public class FileChange
    {
        // Data Columns
        public int Additions { get; set; }
        public int Deletions { get; set; }

        // Foreign Key for the Commit
        public Guid CommitId { get; set; }

        // Foreign Key for the File
        public Guid FileId { get; set; }

        // Navigation Properties
        public Commit Commit { get; set; } = null!;
        public RepositoryFile File { get; set; } = null!;
    }
}