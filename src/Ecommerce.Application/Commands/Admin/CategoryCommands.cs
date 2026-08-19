using System;
using Ecommerce.Application.Common;
using Ecommerce.Application.Common.Commands;
using Ecommerce.Application.DTOs;

namespace Ecommerce.Application.Commands.Admin
{
    public class CreateCategoryCommand : ICommand<CategoryDto>
    {
        public Guid? ParentCategoryId { get; set; }
        public string Name { get; set; } = string.Empty;
        public string Slug { get; set; } = string.Empty;
        public string Description { get; set; } = string.Empty;
        public string ImageUrl { get; set; } = string.Empty;
        public int DisplayOrder { get; set; }
        public bool IsActive { get; set; } = true;
        public bool IsFeatured { get; set; }
        public string MetaTitle { get; set; } = string.Empty;
        public string MetaDescription { get; set; } = string.Empty;
    }

    public class UpdateCategoryCommand : ICommand<CategoryDto>
    {
        public Guid Id { get; set; }
        public Guid? ParentCategoryId { get; set; }
        public string Name { get; set; } = string.Empty;
        public string Slug { get; set; } = string.Empty;
        public string Description { get; set; } = string.Empty;
        public string ImageUrl { get; set; } = string.Empty;
        public int DisplayOrder { get; set; }
        public bool IsActive { get; set; } = true;
        public bool IsFeatured { get; set; }
        public string MetaTitle { get; set; } = string.Empty;
        public string MetaDescription { get; set; } = string.Empty;
    }

    public class DeleteCategoryCommand : ICommand<Unit>
    {
        public Guid Id { get; set; }
    }
}