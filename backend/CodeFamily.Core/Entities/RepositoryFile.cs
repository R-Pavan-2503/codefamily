namespace CodeFamily.Core.Entities
{
    public class RepositoryFile
    {
        // Primary Key
        public Guid Id { get; set; }

        // Data Columns
        public string FilePath { get; set; } = string.Empty;
        public int TotalLines { get; set; }

        // Foreign Key for the Repository
        public Guid RepositoryId { get; set; }

        // Navigation Properties
        // A File belongs to one Repository
        public Repository Repository { get; set; } = null!;

        // A File is involved in many Commits (FileChanges)
        public ICollection<FileChange> FileChanges { get; set; } = new List<FileChange>();

        // A File has many Owners
        public ICollection<FileOwnership> FileOwnerships { get; set; } = new List<FileOwnership>();

        // A File can be the source of many Dependencies
        public ICollection<Dependency> SourceDependencies { get; set; } = new List<Dependency>();

        // A File can be the target of many Dependencies
        public ICollection<Dependency> TargetDependencies { get; set; } = new List<Dependency>();

        // A File can be part of many Pull Requests
        public ICollection<PrFileChange> PrFileChanges { get; set; } = new List<PrFileChange>();
    }
}