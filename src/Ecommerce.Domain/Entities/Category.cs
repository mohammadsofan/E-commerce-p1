using System;
using System.Collections.Generic;

namespace Ecommerce.Domain.Entities
{
    public class Category
    {
        public Guid Id { get; set; }
        public Guid? ParentCategoryId { get; set; }
        public string Name { get; set; }
        public string Slug { get; set; }
        public string Description { get; set; }
        public string ImageUrl { get; set; }
        public int DisplayOrder { get; set; }
        public bool IsActive { get; set; }
        public bool IsFeatured { get; set; }
        public string MetaTitle { get; set; }
        public string MetaDescription { get; set; }
        public DateTimeOffset CreatedAt { get; set; }
        public DateTimeOffset UpdatedAt { get; set; }
        public bool IsDeleted { get; set; }

        public ICollection<Category> Children { get; set; } = new List<Category>();
    }
}
