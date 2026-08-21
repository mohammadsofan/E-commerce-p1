using System;

namespace Ecommerce.Domain.Entities
{
    public class HeroBanner
    {
        public Guid Id { get; set; }
        public string BadgeText { get; set; } = string.Empty;
        public string Title { get; set; } = string.Empty;
        public string Subtitle { get; set; } = string.Empty;
        public string PrimaryButtonText { get; set; } = string.Empty;
        public string PrimaryButtonLink { get; set; } = string.Empty;
        public string SecondaryButtonText { get; set; } = string.Empty;
        public string SecondaryButtonLink { get; set; } = string.Empty;
        public string? ImageUrl { get; set; }
        public int DisplayOrder { get; set; } = 0;
        public bool IsActive { get; set; }
        public DateTimeOffset CreatedAt { get; set; }
        public DateTimeOffset? UpdatedAt { get; set; }

        public HeroBanner()
        {
            Id = Guid.NewGuid();
            CreatedAt = DateTimeOffset.UtcNow;
            DisplayOrder = 0;
            IsActive = true;
        }
    }
}
