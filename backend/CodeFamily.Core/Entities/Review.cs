using CodeFamily.Core.Enums;

namespace CodeFamily.Core.Entities
{
    public class Review
    {
        // Primary Key
        public Guid Id { get; set; }

        // Data Columns
        public ReviewState State { get; set; }
        public DateTimeOffset SubmittedAt { get; set; }

        // Foreign Key for the Pull Request
        public Guid PrId { get; set; }

        // Foreign Key for the User who performed the review
        public Guid ReviewerId { get; set; }

        // Navigation Properties
        public PullRequest PullRequest { get; set; } = null!;
        public User Reviewer { get; set; } = null!;
    }
}