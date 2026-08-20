using System;

namespace Ecommerce.Application.DTOs
{
    public class HeroBannerDto
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
        public bool IsActive { get; set; }
        public DateTimeOffset CreatedAt { get; set; }
        public DateTimeOffset? UpdatedAt { get; set; }
    }
}
