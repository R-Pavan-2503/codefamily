using CodeFamily.Core.Enums;

namespace CodeFamily.Core.Entities
{
    public class WebhookQueueItem
    {
        // Primary Key (auto-incrementing)
        public long Id { get; set; }

        // Data Columns
        public string? Payload { get; set; } // Stores the raw JSON from GitHub
        public WebhookStatus Status { get; set; } = WebhookStatus.Pending;
        public DateTimeOffset ReceivedAt { get; set; } = DateTimeOffset.UtcNow;
        public DateTimeOffset? ProcessedAt { get; set; }
    }
}