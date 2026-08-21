using System;
using System.Collections.Generic;
using Ecommerce.Application.Common.Commands;
using Ecommerce.Application.DTOs;

namespace Ecommerce.Application.Commands.HeroBanners
{
    public class CreateHeroBannerCommand : ICommand<HeroBannerDto>
    {
        public string BadgeText { get; set; } = string.Empty;
        public string Title { get; set; } = string.Empty;
        public string Subtitle { get; set; } = string.Empty;
        public string PrimaryButtonText { get; set; } = string.Empty;
        public string PrimaryButtonLink { get; set; } = string.Empty;
        public string SecondaryButtonText { get; set; } = string.Empty;
        public string SecondaryButtonLink { get; set; } = string.Empty;
        public string? ImageUrl { get; set; }
        public int DisplayOrder { get; set; } = 0;
        public bool IsActive { get; set; } = true;
    }

    public class UpdateHeroBannerCommand : ICommand<HeroBannerDto>
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
        public bool IsActive { get; set; } = true;
    }

    public class ReorderHeroBannersCommand : ICommand<Ecommerce.Application.Common.Unit>
    {
        public List<Guid> BannerIds { get; set; } = new();
    }

    public class SetActiveHeroBannerCommand : ICommand<HeroBannerDto>
    {
        public Guid Id { get; set; }
    }

    public class DeleteHeroBannerCommand : ICommand<Ecommerce.Application.Common.Unit>
    {
        public Guid Id { get; set; }
    }
}
