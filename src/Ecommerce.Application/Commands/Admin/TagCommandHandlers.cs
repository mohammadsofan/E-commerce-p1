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
    public class CreateTagCommandHandler : ICommandHandler<CreateTagCommand, TagDto>
    {
        private readonly IApplicationDbContext _db;
        private readonly IMapper _mapper;

        public CreateTagCommandHandler(IApplicationDbContext db, IMapper mapper)
        {
            _db = db;
            _mapper = mapper;
        }

        public async Task<TagDto> Handle(CreateTagCommand command, CancellationToken cancellationToken = default)
        {
            var name = command.Name.Trim();
            var slug = !string.IsNullOrWhiteSpace(command.Slug)
                ? Slugify(command.Slug)
                : Slugify(name);

            if (string.IsNullOrWhiteSpace(slug))
            {
                slug = Guid.NewGuid().ToString("N")[..8];
            }

            var existing = await _db.Tags
                .FirstOrDefaultAsync(t => t.Slug == slug, cancellationToken);
            if (existing != null)
                throw new DomainException($"Tag with slug '{slug}' already exists");

            var tag = new Tag
            {
                Id = Guid.NewGuid(),
                Name = name,
                Slug = slug
            };

            _db.Tags.Add(tag);
            await _db.SaveChangesAsync(cancellationToken);

            return _mapper.Map<TagDto>(tag);
        }

        private static string Slugify(string text)
        {
            if (string.IsNullOrWhiteSpace(text)) return string.Empty;
            var slug = text.Trim().ToLowerInvariant();
            slug = Regex.Replace(slug, @"[^\w\u0600-\u06FF\s-]", string.Empty);
            slug = Regex.Replace(slug, @"[\s_]+", "-");
            slug = Regex.Replace(slug, @"-+", "-");
            return slug.Trim('-');
        }
    }

    public class UpdateTagCommandHandler : ICommandHandler<UpdateTagCommand, TagDto>
    {
        private readonly IApplicationDbContext _db;
        private readonly IMapper _mapper;

        public UpdateTagCommandHandler(IApplicationDbContext db, IMapper mapper)
        {
            _db = db;
            _mapper = mapper;
        }

        public async Task<TagDto> Handle(UpdateTagCommand command, CancellationToken cancellationToken = default)
        {
            var tag = await _db.Tags
                .FirstOrDefaultAsync(t => t.Id == command.Id, cancellationToken);
            if (tag == null)
                throw new DomainException("Tag not found");

            var oldName = tag.Name; // capture before overwriting

            var name = command.Name.Trim();
            var slug = !string.IsNullOrWhiteSpace(command.Slug)
                ? Slugify(command.Slug)
                : Slugify(name);

            if (string.IsNullOrWhiteSpace(slug))
            {
                slug = tag.Slug;
            }

            var conflict = await _db.Tags
                .FirstOrDefaultAsync(t => t.Slug == slug && t.Id != command.Id, cancellationToken);
            if (conflict != null)
                throw new DomainException($"Tag with slug '{slug}' already exists");

            tag.Name = name;
            tag.Slug = slug;

            // Cascade: update SeoKeywords on all products that reference the old tag name.
            // SeoKeywords stores comma-separated tag names, so we find-and-replace in-memory.
            if (!string.Equals(oldName, name, StringComparison.Ordinal))
            {
                var affectedProducts = await _db.Products
                    .Where(p => p.SeoKeywords.Contains(oldName))
                    .ToListAsync(cancellationToken);

                foreach (var product in affectedProducts)
                {
                    // Replace whole-word occurrences by splitting, replacing, and rejoining
                    var parts = product.SeoKeywords
                        .Split(new[] { ',', ';' }, StringSplitOptions.RemoveEmptyEntries);
                    var updated = System.Array.ConvertAll(parts, p => p.Trim() == oldName ? name : p.Trim());
                    product.SeoKeywords = string.Join(", ", updated);
                }
            }

            await _db.SaveChangesAsync(cancellationToken);

            return _mapper.Map<TagDto>(tag);
        }


        private static string Slugify(string text)
        {
            if (string.IsNullOrWhiteSpace(text)) return string.Empty;
            var slug = text.Trim().ToLowerInvariant();
            slug = Regex.Replace(slug, @"[^\w\u0600-\u06FF\s-]", string.Empty);
            slug = Regex.Replace(slug, @"[\s_]+", "-");
            slug = Regex.Replace(slug, @"-+", "-");
            return slug.Trim('-');
        }
    }

    public class DeleteTagCommandHandler : ICommandHandler<DeleteTagCommand, Unit>
    {
        private readonly IApplicationDbContext _db;

        public DeleteTagCommandHandler(IApplicationDbContext db)
        {
            _db = db;
        }

        public async Task<Unit> Handle(DeleteTagCommand command, CancellationToken cancellationToken = default)
        {
            var tag = await _db.Tags
                .FirstOrDefaultAsync(t => t.Id == command.Id, cancellationToken);
            if (tag == null)
                throw new DomainException("Tag not found");

            var tagName = tag.Name;

            // Cascade: remove this tag name from SeoKeywords of every product that has it.
            var affectedProducts = await _db.Products
                .Where(p => p.SeoKeywords.Contains(tagName))
                .ToListAsync(cancellationToken);

            foreach (var product in affectedProducts)
            {
                var parts = product.SeoKeywords
                    .Split(new[] { ',', ';' }, StringSplitOptions.RemoveEmptyEntries)
                    .Select(p => p.Trim())
                    .Where(p => p != tagName)
                    .ToArray();
                product.SeoKeywords = string.Join(", ", parts);
            }

            _db.Tags.Remove(tag);
            await _db.SaveChangesAsync(cancellationToken);

            return Unit.Value;
        }
    }
}