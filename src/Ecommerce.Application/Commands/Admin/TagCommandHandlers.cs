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
            var slug = Slugify(name);
            var existing = await _db.Tags
                .FirstOrDefaultAsync(t => t.Slug == slug, cancellationToken);
            if (existing != null)
                throw new DomainException($"Tag '{name}' already exists");

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

        private static string Slugify(string name)
        {
            var slug = name.Trim().ToLower();
            slug = Regex.Replace(slug, @"[^a-z0-9\s-]", string.Empty);
            slug = Regex.Replace(slug, @"[\s-]+", "-");
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

            var name = command.Name.Trim();
            var slug = Slugify(name);
            var conflict = await _db.Tags
                .FirstOrDefaultAsync(t => t.Slug == slug && t.Id != command.Id, cancellationToken);
            if (conflict != null)
                throw new DomainException($"Tag '{name}' already exists");

            tag.Name = name;
            tag.Slug = slug;

            await _db.SaveChangesAsync(cancellationToken);

            return _mapper.Map<TagDto>(tag);
        }

        private static string Slugify(string name)
        {
            var slug = name.Trim().ToLower();
            slug = Regex.Replace(slug, @"[^a-z0-9\s-]", string.Empty);
            slug = Regex.Replace(slug, @"[\s-]+", "-");
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

            _db.Tags.Remove(tag);
            await _db.SaveChangesAsync(cancellationToken);

            return Unit.Value;
        }
    }
}