namespace CodeFamily.Core.Entities
{
    public class RepositoryCollaborator
    {
        // Foreign Key for the Repository
        public Guid RepositoryId { get; set; }

        // Foreign Key for the User
        public Guid UserId { get; set; }

        // Navigation Properties
        public Repository Repository { get; set; } = null!;
        public User User { get; set; } = null!;
    }
}