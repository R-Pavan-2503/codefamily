using CodeFamily.Core.Enums;

namespace CodeFamily.Core.Entities
{
    public class Dependency
    {
        // Data Columns
        public DependencyType DependencyType { get; set; }
        public int Strength { get; set; }

        // Foreign Key for the source file (e.g., login.cs)
        public Guid SourceFileId { get; set; }

        // Foreign Key for the target file (e.g., base.cs)
        public Guid TargetFileId { get; set; }

        // Navigation Properties
        public RepositoryFile SourceFile { get; set; } = null!;
        public RepositoryFile TargetFile { get; set; } = null!;
    }
}