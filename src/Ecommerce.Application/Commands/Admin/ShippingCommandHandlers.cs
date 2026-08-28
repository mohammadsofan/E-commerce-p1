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
    public class CreateShippingZoneCommandHandler : ICommandHandler<CreateShippingZoneCommand, AdminShippingZoneDto>
    {
        private readonly IApplicationDbContext _db;
        private readonly IMapper _mapper;

        public CreateShippingZoneCommandHandler(IApplicationDbContext db, IMapper mapper)
        {
            _db = db;
            _mapper = mapper;
        }

        public async Task<AdminShippingZoneDto> Handle(CreateShippingZoneCommand command, CancellationToken cancellationToken = default)
        {
            var zone = new ShippingZone
            {
                Name = command.Name,
                Description = command.Description,
                IsActive = command.IsActive,
                CreatedAt = DateTimeOffset.UtcNow,
                UpdatedAt = DateTimeOffset.UtcNow
            };

            _db.ShippingZones.Add(zone);

            foreach (var locCmd in command.Locations)
            {
                zone.Locations.Add(new ShippingZoneLocation
                {
                    ShippingZoneId = zone.Id,
                    CountryCode = locCmd.CountryCode,
                    RegionCode = locCmd.RegionCode,
                    PostalCodePattern = locCmd.PostalCodePattern
                });
            }

            await _db.SaveChangesAsync(cancellationToken);

            return await GetZoneDtoAsync(zone.Id, cancellationToken);
        }

        private async Task<AdminShippingZoneDto> GetZoneDtoAsync(Guid zoneId, CancellationToken cancellationToken)
        {
            var zone = await _db.ShippingZones
                .Include(z => z.Locations)
                .Include(z => z.Methods)
                    .ThenInclude(m => m.Rates)
                .FirstOrDefaultAsync(z => z.Id == zoneId, cancellationToken);

            if (zone == null)
                throw new Domain.Exceptions.NotFoundException("ShippingZone", zoneId);

            var dto = _mapper.Map<AdminShippingZoneDto>(zone);
            dto.Locations = zone.Locations.Select(_mapper.Map<AdminShippingZoneLocationDto>).ToList();
            dto.Methods = zone.Methods.Select(_mapper.Map<AdminShippingMethodDto>).ToList();
            return dto;
        }
    }

    public class UpdateShippingZoneCommandHandler : ICommandHandler<UpdateShippingZoneCommand, AdminShippingZoneDto>
    {
        private readonly IApplicationDbContext _db;
        private readonly IMapper _mapper;

        public UpdateShippingZoneCommandHandler(IApplicationDbContext db, IMapper mapper)
        {
            _db = db;
            _mapper = mapper;
        }

        public async Task<AdminShippingZoneDto> Handle(UpdateShippingZoneCommand command, CancellationToken cancellationToken = default)
        {
            var zone = await _db.ShippingZones
                .Include(z => z.Locations)
                .FirstOrDefaultAsync(z => z.Id == command.Id, cancellationToken);

            if (zone == null)
                throw new Domain.Exceptions.NotFoundException("ShippingZone", command.Id);

            if (command.RowVersion.Length > 0)
            {
                var entry = _db.GetEntry(zone);
                entry.OriginalValues["RowVersion"] = command.RowVersion;
            }

            zone.Name = command.Name;
            zone.Description = command.Description;
            zone.IsActive = command.IsActive;
            zone.UpdatedAt = DateTimeOffset.UtcNow;

            foreach (var locCmd in command.Locations)
            {
                if (locCmd.IsDeleted && locCmd.Id.HasValue)
                {
                    var loc = zone.Locations.FirstOrDefault(l => l.Id == locCmd.Id.Value);
                    if (loc != null)
                        _db.ShippingZoneLocations.Remove(loc);
                }
                else if (locCmd.Id.HasValue)
                {
                    var loc = zone.Locations.FirstOrDefault(l => l.Id == locCmd.Id.Value);
                    if (loc != null)
                    {
                        loc.CountryCode = locCmd.CountryCode;
                        loc.RegionCode = locCmd.RegionCode;
                        loc.PostalCodePattern = locCmd.PostalCodePattern;
                    }
                }
                else
                {
                    zone.Locations.Add(new ShippingZoneLocation
                    {
                        ShippingZoneId = zone.Id,
                        CountryCode = locCmd.CountryCode,
                        RegionCode = locCmd.RegionCode,
                        PostalCodePattern = locCmd.PostalCodePattern
                    });
                }
            }

            await _db.SaveChangesAsync(cancellationToken);

            return await GetZoneDtoAsync(zone.Id, cancellationToken);
        }

        private async Task<AdminShippingZoneDto> GetZoneDtoAsync(Guid zoneId, CancellationToken cancellationToken)
        {
            var zone = await _db.ShippingZones
                .Include(z => z.Locations)
                .Include(z => z.Methods)
                    .ThenInclude(m => m.Rates)
                .FirstOrDefaultAsync(z => z.Id == zoneId, cancellationToken);

            if (zone == null)
                throw new Domain.Exceptions.NotFoundException("ShippingZone", zoneId);

            var dto = _mapper.Map<AdminShippingZoneDto>(zone);
            dto.Locations = zone.Locations.Select(_mapper.Map<AdminShippingZoneLocationDto>).ToList();
            dto.Methods = zone.Methods.Select(_mapper.Map<AdminShippingMethodDto>).ToList();
            return dto;
        }
    }

    public class DeleteShippingZoneCommandHandler : ICommandHandler<DeleteShippingZoneCommand, Unit>
    {
        private readonly IApplicationDbContext _db;

        public DeleteShippingZoneCommandHandler(IApplicationDbContext db)
        {
            _db = db;
        }

        public async Task<Unit> Handle(DeleteShippingZoneCommand command, CancellationToken cancellationToken = default)
        {
            var zone = await _db.ShippingZones
                .Include(z => z.Locations)
                .Include(z => z.Methods)
                    .ThenInclude(m => m.Rates)
                .FirstOrDefaultAsync(z => z.Id == command.Id, cancellationToken);

            if (zone == null)
                throw new Domain.Exceptions.NotFoundException("ShippingZone", command.Id);

            _db.ShippingZoneLocations.RemoveRange(zone.Locations);
            _db.ShippingRates.RemoveRange(zone.Methods.SelectMany(m => m.Rates));
            _db.ShippingMethods.RemoveRange(zone.Methods);
            _db.ShippingZones.Remove(zone);

            await _db.SaveChangesAsync(cancellationToken);

            return Unit.Value;
        }
    }

    public class CreateShippingMethodCommandHandler : ICommandHandler<CreateShippingMethodCommand, AdminShippingMethodDto>
    {
        private readonly IApplicationDbContext _db;
        private readonly IMapper _mapper;

        public CreateShippingMethodCommandHandler(IApplicationDbContext db, IMapper mapper)
        {
            _db = db;
            _mapper = mapper;
        }

        public async Task<AdminShippingMethodDto> Handle(CreateShippingMethodCommand command, CancellationToken cancellationToken = default)
        {
            var zone = await _db.ShippingZones.FindAsync(new object[] { command.ShippingZoneId }, cancellationToken);
            if (zone == null)
                throw new Domain.Exceptions.NotFoundException("ShippingZone", command.ShippingZoneId);

            if (command.BaseRate < 0)
                throw new Ecommerce.Domain.Exceptions.DomainException("Shipping BaseRate cannot be negative.");

            var method = new ShippingMethod
            {
                ShippingZoneId = command.ShippingZoneId,
                Name = command.Name,
                Description = command.Description,
                Type = command.Type,
                BaseRate = command.BaseRate,
                FreeShippingThreshold = command.FreeShippingThreshold,
                EstimatedDaysMin = command.EstimatedDaysMin,
                EstimatedDaysMax = command.EstimatedDaysMax,
                IsActive = command.IsActive,
                DisplayOrder = command.DisplayOrder,
                CreatedAt = DateTimeOffset.UtcNow,
                UpdatedAt = DateTimeOffset.UtcNow
            };

            _db.ShippingMethods.Add(method);

            foreach (var rateCmd in command.Rates)
            {
                if (rateCmd.Rate < 0)
                    throw new Ecommerce.Domain.Exceptions.DomainException("Shipping Rate cannot be negative.");

                method.Rates.Add(new ShippingRate
                {
                    ShippingMethodId = method.Id,
                    ConditionType = rateCmd.ConditionType,
                    ConditionOperator = rateCmd.ConditionOperator,
                    ConditionValueMin = rateCmd.ConditionValueMin,
                    ConditionValueMax = rateCmd.ConditionValueMax,
                    Rate = rateCmd.Rate,
                    CreatedAt = DateTimeOffset.UtcNow,
                    UpdatedAt = DateTimeOffset.UtcNow
                });
            }

            await _db.SaveChangesAsync(cancellationToken);

            return await GetMethodDtoAsync(method.Id, cancellationToken);
        }

        private async Task<AdminShippingMethodDto> GetMethodDtoAsync(Guid methodId, CancellationToken cancellationToken)
        {
            var method = await _db.ShippingMethods
                .Include(m => m.Rates)
                .FirstOrDefaultAsync(m => m.Id == methodId, cancellationToken);

            if (method == null)
                throw new Domain.Exceptions.NotFoundException("ShippingMethod", methodId);

            var dto = _mapper.Map<AdminShippingMethodDto>(method);
            dto.Rates = method.Rates.Select(_mapper.Map<AdminShippingRateDto>).ToList();
            return dto;
        }
    }

    public class UpdateShippingMethodCommandHandler : ICommandHandler<UpdateShippingMethodCommand, AdminShippingMethodDto>
    {
        private readonly IApplicationDbContext _db;
        private readonly IMapper _mapper;

        public UpdateShippingMethodCommandHandler(IApplicationDbContext db, IMapper mapper)
        {
            _db = db;
            _mapper = mapper;
        }

        public async Task<AdminShippingMethodDto> Handle(UpdateShippingMethodCommand command, CancellationToken cancellationToken = default)
        {
            var method = await _db.ShippingMethods
                .Include(m => m.Rates)
                .FirstOrDefaultAsync(m => m.Id == command.Id, cancellationToken);

            if (method == null)
                throw new Domain.Exceptions.NotFoundException("ShippingMethod", command.Id);

            if (command.BaseRate < 0)
                throw new Ecommerce.Domain.Exceptions.DomainException("Shipping BaseRate cannot be negative.");

            if (command.RowVersion.Length > 0)
            {
                var entry = _db.GetEntry(method);
                entry.OriginalValues["RowVersion"] = command.RowVersion;
            }

            if (command.ShippingZoneId != Guid.Empty)
            {
                method.ShippingZoneId = command.ShippingZoneId;
            }

            if (!string.IsNullOrWhiteSpace(command.Name))
            {
                method.Name = command.Name;
            }

            if (command.Description != null)
            {
                method.Description = command.Description;
            }

            if (!string.IsNullOrWhiteSpace(command.Type))
            {
                method.Type = command.Type;
            }

            method.BaseRate = command.BaseRate;
            method.FreeShippingThreshold = command.FreeShippingThreshold;
            if (command.EstimatedDaysMin.HasValue)
                method.EstimatedDaysMin = command.EstimatedDaysMin;
            if (command.EstimatedDaysMax.HasValue)
                method.EstimatedDaysMax = command.EstimatedDaysMax;
            method.IsActive = command.IsActive;
            method.DisplayOrder = command.DisplayOrder;
            method.UpdatedAt = DateTimeOffset.UtcNow;

            foreach (var rateCmd in command.Rates)
            {
                if (!rateCmd.IsDeleted && rateCmd.Rate < 0)
                    throw new Ecommerce.Domain.Exceptions.DomainException("Shipping Rate cannot be negative.");
                if (rateCmd.IsDeleted && rateCmd.Id.HasValue)
                {
                    var rate = method.Rates.FirstOrDefault(r => r.Id == rateCmd.Id.Value);
                    if (rate != null)
                        _db.ShippingRates.Remove(rate);
                }
                else if (rateCmd.Id.HasValue)
                {
                    var rate = method.Rates.FirstOrDefault(r => r.Id == rateCmd.Id.Value);
                    if (rate != null)
                    {
                        rate.ConditionType = rateCmd.ConditionType;
                        rate.ConditionOperator = rateCmd.ConditionOperator;
                        rate.ConditionValueMin = rateCmd.ConditionValueMin;
                        rate.ConditionValueMax = rateCmd.ConditionValueMax;
                        rate.Rate = rateCmd.Rate;
                        rate.UpdatedAt = DateTimeOffset.UtcNow;
                    }
                }
                else
                {
                    method.Rates.Add(new ShippingRate
                    {
                        ShippingMethodId = method.Id,
                        ConditionType = rateCmd.ConditionType,
                        ConditionOperator = rateCmd.ConditionOperator,
                        ConditionValueMin = rateCmd.ConditionValueMin,
                        ConditionValueMax = rateCmd.ConditionValueMax,
                        Rate = rateCmd.Rate,
                        CreatedAt = DateTimeOffset.UtcNow,
                        UpdatedAt = DateTimeOffset.UtcNow
                    });
                }
            }

            await _db.SaveChangesAsync(cancellationToken);

            return await GetMethodDtoAsync(method.Id, cancellationToken);
        }

        private async Task<AdminShippingMethodDto> GetMethodDtoAsync(Guid methodId, CancellationToken cancellationToken)
        {
            var method = await _db.ShippingMethods
                .Include(m => m.Rates)
                .FirstOrDefaultAsync(m => m.Id == methodId, cancellationToken);

            if (method == null)
                throw new Domain.Exceptions.NotFoundException("ShippingMethod", methodId);

            var dto = _mapper.Map<AdminShippingMethodDto>(method);
            dto.Rates = method.Rates.Select(_mapper.Map<AdminShippingRateDto>).ToList();
            return dto;
        }
    }

    public class DeleteShippingMethodCommandHandler : ICommandHandler<DeleteShippingMethodCommand, Unit>
    {
        private readonly IApplicationDbContext _db;

        public DeleteShippingMethodCommandHandler(IApplicationDbContext db)
        {
            _db = db;
        }

        public async Task<Unit> Handle(DeleteShippingMethodCommand command, CancellationToken cancellationToken = default)
        {
            var method = await _db.ShippingMethods
                .Include(m => m.Rates)
                .FirstOrDefaultAsync(m => m.Id == command.Id, cancellationToken);

            if (method == null)
                throw new Domain.Exceptions.NotFoundException("ShippingMethod", command.Id);

            _db.ShippingRates.RemoveRange(method.Rates);
            _db.ShippingMethods.Remove(method);

            await _db.SaveChangesAsync(cancellationToken);

            return Unit.Value;
        }
    }

    public class CreateShippingRateOnlyCommandHandler : ICommandHandler<CreateShippingRateOnlyCommand, AdminShippingRateDto>
    {
        private readonly IApplicationDbContext _db;
        private readonly IMapper _mapper;

        public CreateShippingRateOnlyCommandHandler(IApplicationDbContext db, IMapper mapper)
        {
            _db = db;
            _mapper = mapper;
        }

        public async Task<AdminShippingRateDto> Handle(CreateShippingRateOnlyCommand command, CancellationToken cancellationToken = default)
        {
            var method = await _db.ShippingMethods.FindAsync(new object[] { command.ShippingMethodId }, cancellationToken);
            if (method == null)
                throw new Domain.Exceptions.NotFoundException("ShippingMethod", command.ShippingMethodId);

            if (command.Rate < 0)
                throw new Ecommerce.Domain.Exceptions.DomainException("Shipping Rate cannot be negative.");

            var rate = new ShippingRate
            {
                ShippingMethodId = command.ShippingMethodId,
                ConditionType = command.ConditionType,
                ConditionOperator = command.ConditionOperator,
                ConditionValueMin = command.ConditionValueMin,
                ConditionValueMax = command.ConditionValueMax,
                Rate = command.Rate,
                CreatedAt = DateTimeOffset.UtcNow,
                UpdatedAt = DateTimeOffset.UtcNow
            };

            _db.ShippingRates.Add(rate);
            await _db.SaveChangesAsync(cancellationToken);

            return _mapper.Map<AdminShippingRateDto>(rate);
        }
    }

    public class UpdateShippingRateOnlyCommandHandler : ICommandHandler<UpdateShippingRateOnlyCommand, AdminShippingRateDto>
    {
        private readonly IApplicationDbContext _db;
        private readonly IMapper _mapper;

        public UpdateShippingRateOnlyCommandHandler(IApplicationDbContext db, IMapper mapper)
        {
            _db = db;
            _mapper = mapper;
        }

        public async Task<AdminShippingRateDto> Handle(UpdateShippingRateOnlyCommand command, CancellationToken cancellationToken = default)
        {
            var rate = await _db.ShippingRates.FindAsync(new object[] { command.Id }, cancellationToken);
            if (rate == null)
                throw new Domain.Exceptions.NotFoundException("ShippingRate", command.Id);

            if (command.Rate < 0)
                throw new Ecommerce.Domain.Exceptions.DomainException("Shipping Rate cannot be negative.");

            rate.ConditionType = command.ConditionType;
            rate.ConditionOperator = command.ConditionOperator;
            rate.ConditionValueMin = command.ConditionValueMin;
            rate.ConditionValueMax = command.ConditionValueMax;
            rate.Rate = command.Rate;
            rate.UpdatedAt = DateTimeOffset.UtcNow;

            await _db.SaveChangesAsync(cancellationToken);

            return _mapper.Map<AdminShippingRateDto>(rate);
        }
    }

    public class DeleteShippingRateCommandHandler : ICommandHandler<DeleteShippingRateCommand, Unit>
    {
        private readonly IApplicationDbContext _db;

        public DeleteShippingRateCommandHandler(IApplicationDbContext db)
        {
            _db = db;
        }

        public async Task<Unit> Handle(DeleteShippingRateCommand command, CancellationToken cancellationToken = default)
        {
            var rate = await _db.ShippingRates.FindAsync(new object[] { command.Id }, cancellationToken);
            if (rate == null)
                throw new Domain.Exceptions.NotFoundException("ShippingRate", command.Id);

            _db.ShippingRates.Remove(rate);
            await _db.SaveChangesAsync(cancellationToken);

            return Unit.Value;
        }
    }
}