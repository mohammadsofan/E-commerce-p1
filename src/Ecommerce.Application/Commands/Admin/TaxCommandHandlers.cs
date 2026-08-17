using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Ecommerce.Application.Common;
using Ecommerce.Application.Common.Commands;
using Ecommerce.Application.DTOs;
using Ecommerce.Application.Interfaces;
using Ecommerce.Domain.Entities;
using AutoMapper;
using Microsoft.EntityFrameworkCore;

namespace Ecommerce.Application.Commands.Admin
{
    public class CreateTaxCategoryCommandHandler : ICommandHandler<CreateTaxCategoryCommand, AdminTaxCategoryDto>
    {
        private readonly IApplicationDbContext _db;
        private readonly IMapper _mapper;

        public CreateTaxCategoryCommandHandler(IApplicationDbContext db, IMapper mapper)
        {
            _db = db;
            _mapper = mapper;
        }

        public async Task<AdminTaxCategoryDto> Handle(CreateTaxCategoryCommand command, CancellationToken cancellationToken = default)
        {
            var existing = await _db.TaxCategories
                .FirstOrDefaultAsync(c => c.Name == command.Name, cancellationToken);
            if (existing != null)
                throw new Domain.Exceptions.DomainException("Tax category with this name already exists");

            var category = new TaxCategory
            {
                Name = command.Name,
                Description = command.Description,
                IsActive = command.IsActive,
                CreatedAt = DateTimeOffset.UtcNow,
                UpdatedAt = DateTimeOffset.UtcNow
            };

            _db.TaxCategories.Add(category);

            foreach (var rateCmd in command.Rates)
            {
                category.Rates.Add(new TaxRate
                {
                    TaxCategoryId = category.Id,
                    CountryCode = rateCmd.CountryCode,
                    RegionCode = rateCmd.RegionCode,
                    PostalCodePattern = rateCmd.PostalCodePattern,
                    Rate = rateCmd.Rate,
                    IsActive = rateCmd.IsActive,
                    CreatedAt = DateTimeOffset.UtcNow,
                    UpdatedAt = DateTimeOffset.UtcNow
                });
            }

            await _db.SaveChangesAsync(cancellationToken);

            return await GetCategoryDtoAsync(category.Id, cancellationToken);
        }

        private async Task<AdminTaxCategoryDto> GetCategoryDtoAsync(Guid categoryId, CancellationToken cancellationToken)
        {
            var category = await _db.TaxCategories
                .Include(c => c.Rates)
                .FirstOrDefaultAsync(c => c.Id == categoryId, cancellationToken);

            if (category == null)
                throw new Domain.Exceptions.NotFoundException("TaxCategory", categoryId);

            var dto = _mapper.Map<AdminTaxCategoryDto>(category);
            dto.Rates = category.Rates.Select(_mapper.Map<AdminTaxRateDto>).ToList();
            return dto;
        }
    }

    public class UpdateTaxCategoryCommandHandler : ICommandHandler<UpdateTaxCategoryCommand, AdminTaxCategoryDto>
    {
        private readonly IApplicationDbContext _db;
        private readonly IMapper _mapper;

        public UpdateTaxCategoryCommandHandler(IApplicationDbContext db, IMapper mapper)
        {
            _db = db;
            _mapper = mapper;
        }

        public async Task<AdminTaxCategoryDto> Handle(UpdateTaxCategoryCommand command, CancellationToken cancellationToken = default)
        {
            var category = await _db.TaxCategories
                .Include(c => c.Rates)
                .FirstOrDefaultAsync(c => c.Id == command.Id, cancellationToken);

            if (category == null)
                throw new Domain.Exceptions.NotFoundException("TaxCategory", command.Id);

            if (command.RowVersion.Length > 0)
            {
                var entry = _db.GetEntry(category);
                entry.OriginalValues["RowVersion"] = command.RowVersion;
            }

            var existing = await _db.TaxCategories
                .FirstOrDefaultAsync(c => c.Name == command.Name && c.Id != command.Id, cancellationToken);
            if (existing != null)
                throw new Domain.Exceptions.DomainException("Tax category with this name already exists");

            category.Name = command.Name;
            category.Description = command.Description;
            category.IsActive = command.IsActive;
            category.UpdatedAt = DateTimeOffset.UtcNow;

            foreach (var rateCmd in command.Rates)
            {
                if (rateCmd.IsDeleted && rateCmd.Id.HasValue)
                {
                    var rate = category.Rates.FirstOrDefault(r => r.Id == rateCmd.Id.Value);
                    if (rate != null)
                        _db.TaxRates.Remove(rate);
                }
                else if (rateCmd.Id.HasValue)
                {
                    var rate = category.Rates.FirstOrDefault(r => r.Id == rateCmd.Id.Value);
                    if (rate != null)
                    {
                        rate.CountryCode = rateCmd.CountryCode;
                        rate.RegionCode = rateCmd.RegionCode;
                        rate.PostalCodePattern = rateCmd.PostalCodePattern;
                        rate.Rate = rateCmd.Rate;
                        rate.IsActive = rateCmd.IsActive;
                        rate.UpdatedAt = DateTimeOffset.UtcNow;
                    }
                }
                else
                {
                    category.Rates.Add(new TaxRate
                    {
                        TaxCategoryId = category.Id,
                        CountryCode = rateCmd.CountryCode,
                        RegionCode = rateCmd.RegionCode,
                        PostalCodePattern = rateCmd.PostalCodePattern,
                        Rate = rateCmd.Rate,
                        IsActive = rateCmd.IsActive,
                        CreatedAt = DateTimeOffset.UtcNow,
                        UpdatedAt = DateTimeOffset.UtcNow
                    });
                }
            }

            await _db.SaveChangesAsync(cancellationToken);

            return await GetCategoryDtoAsync(category.Id, cancellationToken);
        }

        private async Task<AdminTaxCategoryDto> GetCategoryDtoAsync(Guid categoryId, CancellationToken cancellationToken)
        {
            var category = await _db.TaxCategories
                .Include(c => c.Rates)
                .FirstOrDefaultAsync(c => c.Id == categoryId, cancellationToken);

            if (category == null)
                throw new Domain.Exceptions.NotFoundException("TaxCategory", categoryId);

            var dto = _mapper.Map<AdminTaxCategoryDto>(category);
            dto.Rates = category.Rates.Select(_mapper.Map<AdminTaxRateDto>).ToList();
            return dto;
        }
    }

    public class DeleteTaxCategoryCommandHandler : ICommandHandler<DeleteTaxCategoryCommand, Unit>
    {
        private readonly IApplicationDbContext _db;

        public DeleteTaxCategoryCommandHandler(IApplicationDbContext db)
        {
            _db = db;
        }

        public async Task<Unit> Handle(DeleteTaxCategoryCommand command, CancellationToken cancellationToken = default)
        {
            var category = await _db.TaxCategories
                .Include(c => c.Rates)
                .FirstOrDefaultAsync(c => c.Id == command.Id, cancellationToken);

            if (category == null)
                throw new Domain.Exceptions.NotFoundException("TaxCategory", command.Id);

            _db.TaxRates.RemoveRange(category.Rates);
            _db.TaxCategories.Remove(category);

            await _db.SaveChangesAsync(cancellationToken);

            return Unit.Value;
        }
    }

    public class CreateTaxRateOnlyCommandHandler : ICommandHandler<CreateTaxRateOnlyCommand, AdminTaxRateDto>
    {
        private readonly IApplicationDbContext _db;
        private readonly IMapper _mapper;

        public CreateTaxRateOnlyCommandHandler(IApplicationDbContext db, IMapper mapper)
        {
            _db = db;
            _mapper = mapper;
        }

        public async Task<AdminTaxRateDto> Handle(CreateTaxRateOnlyCommand command, CancellationToken cancellationToken = default)
        {
            var category = await _db.TaxCategories.FindAsync(new object[] { command.TaxCategoryId }, cancellationToken);
            if (category == null)
                throw new Domain.Exceptions.NotFoundException("TaxCategory", command.TaxCategoryId);

            var existing = await _db.TaxRates
                .FirstOrDefaultAsync(r => r.TaxCategoryId == command.TaxCategoryId
                    && r.CountryCode == command.CountryCode
                    && r.RegionCode == command.RegionCode, cancellationToken);
            if (existing != null)
                throw new Domain.Exceptions.DomainException("Tax rate already exists for this region");

            var rate = new TaxRate
            {
                TaxCategoryId = command.TaxCategoryId,
                CountryCode = command.CountryCode,
                RegionCode = command.RegionCode,
                PostalCodePattern = command.PostalCodePattern,
                Rate = command.Rate,
                IsActive = command.IsActive,
                CreatedAt = DateTimeOffset.UtcNow,
                UpdatedAt = DateTimeOffset.UtcNow
            };

            _db.TaxRates.Add(rate);
            await _db.SaveChangesAsync(cancellationToken);

            return _mapper.Map<AdminTaxRateDto>(rate);
        }
    }

    public class UpdateTaxRateOnlyCommandHandler : ICommandHandler<UpdateTaxRateOnlyCommand, AdminTaxRateDto>
    {
        private readonly IApplicationDbContext _db;
        private readonly IMapper _mapper;

        public UpdateTaxRateOnlyCommandHandler(IApplicationDbContext db, IMapper mapper)
        {
            _db = db;
            _mapper = mapper;
        }

        public async Task<AdminTaxRateDto> Handle(UpdateTaxRateOnlyCommand command, CancellationToken cancellationToken = default)
        {
            var rate = await _db.TaxRates.FindAsync(new object[] { command.Id }, cancellationToken);
            if (rate == null)
                throw new Domain.Exceptions.NotFoundException("TaxRate", command.Id);

            if (command.RowVersion.Length > 0)
            {
                var entry = _db.GetEntry(rate);
                entry.OriginalValues["RowVersion"] = command.RowVersion;
            }

            var existing = await _db.TaxRates
                .FirstOrDefaultAsync(r => r.TaxCategoryId == rate.TaxCategoryId
                    && r.CountryCode == command.CountryCode
                    && r.RegionCode == command.RegionCode
                    && r.Id != command.Id, cancellationToken);
            if (existing != null)
                throw new Domain.Exceptions.DomainException("Tax rate already exists for this region");

            rate.CountryCode = command.CountryCode;
            rate.RegionCode = command.RegionCode;
            rate.PostalCodePattern = command.PostalCodePattern;
            rate.Rate = command.Rate;
            rate.IsActive = command.IsActive;
            rate.UpdatedAt = DateTimeOffset.UtcNow;

            await _db.SaveChangesAsync(cancellationToken);

            return _mapper.Map<AdminTaxRateDto>(rate);
        }
    }

    public class DeleteTaxRateCommandHandler : ICommandHandler<DeleteTaxRateCommand, Unit>
    {
        private readonly IApplicationDbContext _db;

        public DeleteTaxRateCommandHandler(IApplicationDbContext db)
        {
            _db = db;
        }

        public async Task<Unit> Handle(DeleteTaxRateCommand command, CancellationToken cancellationToken = default)
        {
            var rate = await _db.TaxRates.FindAsync(new object[] { command.Id }, cancellationToken);
            if (rate == null)
                throw new Domain.Exceptions.NotFoundException("TaxRate", command.Id);

            _db.TaxRates.Remove(rate);
            await _db.SaveChangesAsync(cancellationToken);

            return Unit.Value;
        }
    }
}