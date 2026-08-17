using System;
using System.Linq;
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
    public class CreateWarehouseCommandHandler : ICommandHandler<CreateWarehouseCommand, WarehouseDto>
    {
        private readonly IApplicationDbContext _db;
        private readonly IMapper _mapper;

        public CreateWarehouseCommandHandler(IApplicationDbContext db, IMapper mapper)
        {
            _db = db;
            _mapper = mapper;
        }

        public async Task<WarehouseDto> Handle(CreateWarehouseCommand command, CancellationToken cancellationToken = default)
        {
            var code = command.Code.Trim().ToUpperInvariant();
            var existing = await _db.Warehouses
                .FirstOrDefaultAsync(w => w.Code == code, cancellationToken);
            if (existing != null)
                throw new DomainException($"Warehouse with code {code} already exists");

            var warehouse = new Warehouse
            {
                Id = Guid.NewGuid(),
                Name = command.Name,
                Code = code,
                IsActive = command.IsActive,
                CreatedAt = DateTimeOffset.UtcNow,
                UpdatedAt = DateTimeOffset.UtcNow
            };

            _db.Warehouses.Add(warehouse);
            await _db.SaveChangesAsync(cancellationToken);

            return _mapper.Map<WarehouseDto>(warehouse);
        }
    }

    public class UpdateWarehouseCommandHandler : ICommandHandler<UpdateWarehouseCommand, WarehouseDto>
    {
        private readonly IApplicationDbContext _db;
        private readonly IMapper _mapper;

        public UpdateWarehouseCommandHandler(IApplicationDbContext db, IMapper mapper)
        {
            _db = db;
            _mapper = mapper;
        }

        public async Task<WarehouseDto> Handle(UpdateWarehouseCommand command, CancellationToken cancellationToken = default)
        {
            var warehouse = await _db.Warehouses
                .FirstOrDefaultAsync(w => w.Id == command.Id, cancellationToken);
            if (warehouse == null)
                throw new DomainException("Warehouse not found");

            var code = command.Code.Trim().ToUpperInvariant();
            var conflict = await _db.Warehouses
                .FirstOrDefaultAsync(w => w.Code == code && w.Id != command.Id, cancellationToken);
            if (conflict != null)
                throw new DomainException($"Warehouse with code {code} already exists");

            warehouse.Name = command.Name;
            warehouse.Code = code;
            warehouse.IsActive = command.IsActive;
            warehouse.UpdatedAt = DateTimeOffset.UtcNow;

            await _db.SaveChangesAsync(cancellationToken);

            return _mapper.Map<WarehouseDto>(warehouse);
        }
    }

    public class DeleteWarehouseCommandHandler : ICommandHandler<DeleteWarehouseCommand, Unit>
    {
        private readonly IApplicationDbContext _db;

        public DeleteWarehouseCommandHandler(IApplicationDbContext db)
        {
            _db = db;
        }

        public async Task<Unit> Handle(DeleteWarehouseCommand command, CancellationToken cancellationToken = default)
        {
            var warehouse = await _db.Warehouses
                .FirstOrDefaultAsync(w => w.Id == command.Id, cancellationToken);
            if (warehouse == null)
                throw new DomainException("Warehouse not found");

            _db.Warehouses.Remove(warehouse);
            await _db.SaveChangesAsync(cancellationToken);

            return Unit.Value;
        }
    }
}
