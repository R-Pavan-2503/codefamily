using CodeFamily.Core.Entities;
using CodeFamily.Core.Enums;
using Microsoft.EntityFrameworkCore;

namespace CodeFamily.Data
{
    public class CodeFamilyDbContext : DbContext
    {
        // 1. DbContext Constructor
        public CodeFamilyDbContext(DbContextOptions<CodeFamilyDbContext> options)
            : base(options)
        {
        }

        // 2. DbSet Properties (Tables)
        public DbSet<User> Users { get; set; } = null!;
        public DbSet<Repository> Repositories { get; set; } = null!;
        public DbSet<RepositoryCollaborator> RepositoryCollaborators { get; set; } = null!;
        public DbSet<Commit> Commits { get; set; } = null!;
        public DbSet<RepositoryFile> RepositoryFiles { get; set; } = null!;
        public DbSet<FileChange> FileChanges { get; set; } = null!;
        public DbSet<FileOwnership> FileOwnerships { get; set; } = null!;
        public DbSet<Dependency> Dependencies { get; set; } = null!;
        public DbSet<PullRequest> PullRequests { get; set; } = null!;
        public DbSet<PrFileChange> PrFileChanges { get; set; } = null!;
        public DbSet<Review> Reviews { get; set; } = null!;
        public DbSet<WebhookQueueItem> WebhookQueue { get; set; } = null!;

        // 3. OnModelCreating (Configuration)
        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            // === ENUM MAPPING ===
            // Map C# enums to PostgreSQL enum types
            modelBuilder.HasPostgresEnum<RepoStatus>();
            modelBuilder.HasPostgresEnum<DependencyType>();
            modelBuilder.HasPostgresEnum<ReviewState>();
            modelBuilder.HasPostgresEnum<WebhookStatus>();

            // === COMPOSITE PRIMARY KEYS ===
            // Define primary keys that are made of multiple columns

            // RepositoryCollaborator: (RepositoryId, UserId)
            modelBuilder.Entity<RepositoryCollaborator>()
                .HasKey(rc => new { rc.RepositoryId, rc.UserId });

            // FileChange: (CommitId, FileId)
            modelBuilder.Entity<FileChange>()
                .HasKey(fc => new { fc.CommitId, fc.FileId });

            // FileOwnership: (FileId, UserId)
            modelBuilder.Entity<FileOwnership>()
                .HasKey(fo => new { fo.FileId, fo.UserId });

            // Dependency: (SourceFileId, TargetFileId, DependencyType)
            modelBuilder.Entity<Dependency>()
                .HasKey(d => new { d.SourceFileId, d.TargetFileId, d.DependencyType });

            // PrFileChange: (PrId, FileId)
            modelBuilder.Entity<PrFileChange>()
                .HasKey(pfc => new { pfc.PrId, pfc.FileId });


            // === RELATIONSHIP CONFIGURATION ===
            // Define complex relationships (many-to-many, etc.)

            // User -> RepositoryCollaborator <- Repository
            modelBuilder.Entity<RepositoryCollaborator>()
                .HasOne(rc => rc.User)
                .WithMany(u => u.Collaborations)
                .HasForeignKey(rc => rc.UserId);

            modelBuilder.Entity<RepositoryCollaborator>()
                .HasOne(rc => rc.Repository)
                .WithMany(r => r.Collaborators)
                .HasForeignKey(rc => rc.RepositoryId);

            // Repository -> ConnectedByUser
            modelBuilder.Entity<Repository>()
                .HasOne(r => r.ConnectedByUser)
                .WithMany(u => u.OwnedRepositories)
                .HasForeignKey(r => r.ConnectedByUserId);

            // Commit -> FileChange <- RepositoryFile
            modelBuilder.Entity<FileChange>()
                .HasOne(fc => fc.Commit)
                .WithMany(c => c.FileChanges)
                .HasForeignKey(fc => fc.CommitId);

            modelBuilder.Entity<FileChange>()
                .HasOne(fc => fc.File)
                .WithMany(f => f.FileChanges)
                .HasForeignKey(fc => fc.FileId);

            // RepositoryFile -> FileOwnership <- User
            modelBuilder.Entity<FileOwnership>()
                .HasOne(fo => fo.File)
                .WithMany(f => f.FileOwnerships)
                .HasForeignKey(fo => fo.FileId);

            modelBuilder.Entity<FileOwnership>()
                .HasOne(fo => fo.User)
                .WithMany(u => u.FileOwnerships)
                .HasForeignKey(fo => fo.UserId);

            // RepositoryFile -> Dependency -> RepositoryFile
            modelBuilder.Entity<Dependency>()
                .HasOne(d => d.SourceFile)
                .WithMany(f => f.SourceDependencies)
                .HasForeignKey(d => d.SourceFileId)
                .OnDelete(DeleteBehavior.Restrict); // Prevent cascade delete

            modelBuilder.Entity<Dependency>()
                .HasOne(d => d.TargetFile)
                .WithMany(f => f.TargetDependencies)
                .HasForeignKey(d => d.TargetFileId)
                .OnDelete(DeleteBehavior.Restrict); // Prevent cascade delete

            // PullRequest -> PrFileChange <- RepositoryFile
            modelBuilder.Entity<PrFileChange>()
                .HasOne(pfc => pfc.PullRequest)
                .WithMany(pr => pr.FilesChanged)
                .HasForeignKey(pfc => pfc.PrId);

            modelBuilder.Entity<PrFileChange>()
                .HasOne(pfc => pfc.File)
                .WithMany(f => f.PrFileChanges)
                .HasForeignKey(pfc => pfc.FileId);

            // Review -> PullRequest & User
            modelBuilder.Entity<Review>()
                .HasOne(r => r.PullRequest)
                .WithMany(pr => pr.Reviews)
                .HasForeignKey(r => r.PrId);

            modelBuilder.Entity<Review>()
                .HasOne(r => r.Reviewer)
                .WithMany(u => u.Reviews)
                .HasForeignKey(r => r.ReviewerId);

            modelBuilder.Entity<WebhookQueueItem>()
            .Property(w => w.Payload)
            .HasColumnType("jsonb");
        }
    }
}