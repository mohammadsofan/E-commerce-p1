using System;
using System.Linq;
using System.Text.RegularExpressions;
using System.Threading;
using System.Threading.Tasks;
using AutoMapper;
using Ecommerce.Application.Common;
using Ecommerce.Application.Common.Commands;
using Ecommerce.Application.DTOs;
using Ecommerce.Application.Interfaces;
using Ecommerce.Domain.Entities;
using Ecommerce.Domain.Exceptions;
using Microsoft.EntityFrameworkCore;

namespace Ecommerce.Application.Commands.Admin
{
    public class CreateCategoryCommandHandler : ICommandHandler<CreateCategoryCommand, CategoryDto>
    {
        private readonly IApplicationDbContext _db;
        private readonly IMapper _mapper;

        public CreateCategoryCommandHandler(IApplicationDbContext db, IMapper mapper)
        {
            _db = db;
            _mapper = mapper;
        }

        public async Task<CategoryDto> Handle(CreateCategoryCommand command, CancellationToken cancellationToken = default)
        {
            var name = command.Name.Trim();
            var slug = string.IsNullOrWhiteSpace(command.Slug) ? Slugify(name) : command.Slug.Trim().ToLower();

            var existing = await _db.Categories
                .FirstOrDefaultAsync(c => c.Slug == slug, cancellationToken);
            if (existing != null)
                throw new DomainException($"Category with slug '{slug}' already exists");

            var maxOrder = await _db.Categories
                .Where(c => c.ParentCategoryId == command.ParentCategoryId)
                .MaxAsync(c => (int?)c.DisplayOrder, cancellationToken) ?? 0;

            var category = new Category
            {
                Id = Guid.NewGuid(),
                ParentCategoryId = command.ParentCategoryId,
                Name = name,
                Slug = slug,
                Description = command.Description,
                ImageUrl = command.ImageUrl,
                DisplayOrder = command.DisplayOrder > 0 ? command.DisplayOrder : maxOrder + 1,
                IsActive = command.IsActive,
                IsFeatured = command.IsFeatured,
                MetaTitle = command.MetaTitle,
                MetaDescription = command.MetaDescription,
                CreatedAt = DateTimeOffset.UtcNow,
                UpdatedAt = DateTimeOffset.UtcNow,
                IsDeleted = false
            };

            _db.Categories.Add(category);
            await _db.SaveChangesAsync(cancellationToken);

            return _mapper.Map<CategoryDto>(category);
        }

        private static string Slugify(string name)
        {
            var slug = name.Trim().ToLower();
            slug = Regex.Replace(slug, @"[^a-z0-9\s-]", string.Empty);
            slug = Regex.Replace(slug, @"[\s-]+", "-");
            return slug.Trim('-');
        }
    }

    public class UpdateCategoryCommandHandler : ICommandHandler<UpdateCategoryCommand, CategoryDto>
    {
        private readonly IApplicationDbContext _db;
        private readonly IMapper _mapper;

        public UpdateCategoryCommandHandler(IApplicationDbContext db, IMapper mapper)
        {
            _db = db;
            _mapper = mapper;
        }

        public async Task<CategoryDto> Handle(UpdateCategoryCommand command, CancellationToken cancellationToken = default)
        {
            var category = await _db.Categories
                .FirstOrDefaultAsync(c => c.Id == command.Id, cancellationToken);
            if (category == null)
                throw new DomainException("Category not found");

            var name = command.Name.Trim();
            var slug = string.IsNullOrWhiteSpace(command.Slug) ? Slugify(name) : command.Slug.Trim().ToLower();

            var conflict = await _db.Categories
                .FirstOrDefaultAsync(c => c.Slug == slug && c.Id != command.Id, cancellationToken);
            if (conflict != null)
                throw new DomainException($"Category with slug '{slug}' already exists");

            if (command.ParentCategoryId.HasValue)
            {
                if (command.ParentCategoryId.Value == command.Id)
                    throw new DomainException("Category cannot be its own parent.");

                var currentParentId = command.ParentCategoryId.Value;
                while (true)
                {
                    var parent = await _db.Categories.FirstOrDefaultAsync(c => c.Id == currentParentId, cancellationToken);
                    if (parent == null) break;
                    if (parent.Id == command.Id)
                        throw new DomainException("Category cannot be a descendant of itself.");
                    if (!parent.ParentCategoryId.HasValue) break;
                    currentParentId = parent.ParentCategoryId.Value;
                }
            }

            category.ParentCategoryId = command.ParentCategoryId;
            category.Name = name;
            category.Slug = slug;
            category.Description = command.Description;
            category.ImageUrl = command.ImageUrl;
            category.DisplayOrder = command.DisplayOrder;
            category.IsActive = command.IsActive;
            category.IsFeatured = command.IsFeatured;
            category.MetaTitle = command.MetaTitle;
            category.MetaDescription = command.MetaDescription;
            category.UpdatedAt = DateTimeOffset.UtcNow;

            await _db.SaveChangesAsync(cancellationToken);

            return _mapper.Map<CategoryDto>(category);
        }

        private static string Slugify(string name)
        {
            var slug = name.Trim().ToLower();
            slug = Regex.Replace(slug, @"[^a-z0-9\s-]", string.Empty);
            slug = Regex.Replace(slug, @"[\s-]+", "-");
            return slug.Trim('-');
        }
    }

    public class DeleteCategoryCommandHandler : ICommandHandler<DeleteCategoryCommand, Unit>
    {
        private readonly IApplicationDbContext _db;

        public DeleteCategoryCommandHandler(IApplicationDbContext db)
        {
            _db = db;
        }

        public async Task<Unit> Handle(DeleteCategoryCommand command, CancellationToken cancellationToken = default)
        {
            var category = await _db.Categories
                .FirstOrDefaultAsync(c => c.Id == command.Id, cancellationToken);
            if (category == null)
                throw new DomainException("Category not found");

            // Check if category has children
            var hasChildren = await _db.Categories
                .AnyAsync(c => c.ParentCategoryId == command.Id, cancellationToken);
            if (hasChildren)
                throw new DomainException("Cannot delete category with children. Delete children first.");

            // Check if category has products. Soft-deleted products must not block the
            // delete, otherwise the category becomes permanently undeletable.
            var hasProducts = await _db.Products
                .AnyAsync(p => p.CategoryId == command.Id && !p.IsDeleted, cancellationToken);
            if (hasProducts)
                throw new DomainException("Cannot delete category with products. Reassign products first.");

            // Detach any soft-deleted products so the FK does not block the delete.
            var archivedProducts = await _db.Products
                .Where(p => p.CategoryId == command.Id && p.IsDeleted)
                .ToListAsync(cancellationToken);
            foreach (var archived in archivedProducts)
            {
                archived.CategoryId = null;
            }

            _db.Categories.Remove(category);
            await _db.SaveChangesAsync(cancellationToken);

            return Unit.Value;
        }
    }
}