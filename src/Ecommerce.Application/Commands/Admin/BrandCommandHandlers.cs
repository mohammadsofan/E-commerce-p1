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
    public class CreateBrandCommandHandler : ICommandHandler<CreateBrandCommand, BrandDto>
    {
        private readonly IApplicationDbContext _db;
        private readonly IMapper _mapper;

        public CreateBrandCommandHandler(IApplicationDbContext db, IMapper mapper)
        {
            _db = db;
            _mapper = mapper;
        }

        public async Task<BrandDto> Handle(CreateBrandCommand command, CancellationToken cancellationToken = default)
        {
            var name = command.Name.Trim();
            var slug = string.IsNullOrWhiteSpace(command.Slug) ? Slugify(name) : command.Slug.Trim().ToLower();

            var existing = await _db.Brands
                .FirstOrDefaultAsync(b => b.Slug == slug, cancellationToken);
            if (existing != null)
                throw new DomainException($"Brand with slug '{slug}' already exists");

            var brand = new Brand
            {
                Id = Guid.NewGuid(),
                Name = name,
                Slug = slug,
                Description = command.Description,
                ImageUrl = command.ImageUrl,
                IsActive = command.IsActive,
                CreatedAt = DateTimeOffset.UtcNow,
                UpdatedAt = DateTimeOffset.UtcNow,
                IsDeleted = false
            };

            _db.Brands.Add(brand);
            await _db.SaveChangesAsync(cancellationToken);

            return _mapper.Map<BrandDto>(brand);
        }

        private static string Slugify(string name)
        {
            var slug = name.Trim().ToLower();
            slug = Regex.Replace(slug, @"[^a-z0-9\s-]", string.Empty);
            slug = Regex.Replace(slug, @"[\s-]+", "-");
            return slug.Trim('-');
        }
    }

    public class UpdateBrandCommandHandler : ICommandHandler<UpdateBrandCommand, BrandDto>
    {
        private readonly IApplicationDbContext _db;
        private readonly IMapper _mapper;

        public UpdateBrandCommandHandler(IApplicationDbContext db, IMapper mapper)
        {
            _db = db;
            _mapper = mapper;
        }

        public async Task<BrandDto> Handle(UpdateBrandCommand command, CancellationToken cancellationToken = default)
        {
            var brand = await _db.Brands
                .FirstOrDefaultAsync(b => b.Id == command.Id, cancellationToken);
            if (brand == null)
                throw new DomainException("Brand not found");

            var name = command.Name.Trim();
            var slug = string.IsNullOrWhiteSpace(command.Slug) ? Slugify(name) : command.Slug.Trim().ToLower();

            var conflict = await _db.Brands
                .FirstOrDefaultAsync(b => b.Slug == slug && b.Id != command.Id, cancellationToken);
            if (conflict != null)
                throw new DomainException($"Brand with slug '{slug}' already exists");

            brand.Name = name;
            brand.Slug = slug;
            brand.Description = command.Description;
            brand.ImageUrl = command.ImageUrl;
            brand.IsActive = command.IsActive;
            brand.UpdatedAt = DateTimeOffset.UtcNow;

            await _db.SaveChangesAsync(cancellationToken);

            return _mapper.Map<BrandDto>(brand);
        }

        private static string Slugify(string name)
        {
            var slug = name.Trim().ToLower();
            slug = Regex.Replace(slug, @"[^a-z0-9\s-]", string.Empty);
            slug = Regex.Replace(slug, @"[\s-]+", "-");
            return slug.Trim('-');
        }
    }

    public class DeleteBrandCommandHandler : ICommandHandler<DeleteBrandCommand, Unit>
    {
        private readonly IApplicationDbContext _db;

        public DeleteBrandCommandHandler(IApplicationDbContext db)
        {
            _db = db;
        }

        public async Task<Unit> Handle(DeleteBrandCommand command, CancellationToken cancellationToken = default)
        {
            var brand = await _db.Brands
                .FirstOrDefaultAsync(b => b.Id == command.Id, cancellationToken);
            if (brand == null)
                throw new DomainException("Brand not found");

            // Check if brand has products. Soft-deleted products must not block the delete,
            // otherwise the brand becomes permanently undeletable.
            var hasProducts = await _db.Products
                .AnyAsync(p => p.BrandId == command.Id && !p.IsDeleted, cancellationToken);
            if (hasProducts)
                throw new DomainException("Cannot delete brand with products. Reassign products first.");

            // Detach any soft-deleted products so the FK does not block the delete.
            var archivedProducts = await _db.Products
                .Where(p => p.BrandId == command.Id && p.IsDeleted)
                .ToListAsync(cancellationToken);
            foreach (var archived in archivedProducts)
            {
                archived.BrandId = null;
            }

            _db.Brands.Remove(brand);
            await _db.SaveChangesAsync(cancellationToken);

            return Unit.Value;
        }
    }
}