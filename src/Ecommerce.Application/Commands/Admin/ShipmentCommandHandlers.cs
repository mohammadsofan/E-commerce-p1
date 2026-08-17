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
    public class CreateShipmentCommandHandler : ICommandHandler<CreateShipmentCommand, ShipmentDto>
    {
        private readonly IApplicationDbContext _db;
        private readonly IMapper _mapper;

        public CreateShipmentCommandHandler(IApplicationDbContext db, IMapper mapper)
        {
            _db = db;
            _mapper = mapper;
        }

        public async Task<ShipmentDto> Handle(CreateShipmentCommand command, CancellationToken cancellationToken = default)
        {
            var orderExists = await _db.Orders.AnyAsync(o => o.Id == command.OrderId, cancellationToken);
            if (!orderExists)
                throw new DomainException("Order not found");

            var shipmentId = Guid.NewGuid();
            var shipment = new Shipment
            {
                Id = shipmentId,
                OrderId = command.OrderId,
                WarehouseId = command.WarehouseId,
                Carrier = command.Carrier,
                Status = "Preparing",
                CreatedAt = DateTimeOffset.UtcNow,
                Items = command.Items.Select(i => new ShipmentItem
                {
                    Id = Guid.NewGuid(),
                    ShipmentId = shipmentId,
                    OrderItemId = i.OrderItemId,
                    InventoryItemId = i.InventoryItemId,
                    Quantity = i.Quantity
                }).ToList()
            };

            _db.Shipments.Add(shipment);
            await _db.SaveChangesAsync(cancellationToken);

            var dto = _mapper.Map<ShipmentDto>(shipment);
            dto.WarehouseName = (await _db.Warehouses.AsNoTracking().FirstOrDefaultAsync(w => w.Id == shipment.WarehouseId, cancellationToken))?.Name ?? string.Empty;
            return dto;
        }
    }

    public class UpdateShipmentStatusCommandHandler : ICommandHandler<UpdateShipmentStatusCommand, Unit>
    {
        private readonly IApplicationDbContext _db;

        public UpdateShipmentStatusCommandHandler(IApplicationDbContext db)
        {
            _db = db;
        }

        public async Task<Unit> Handle(UpdateShipmentStatusCommand command, CancellationToken cancellationToken = default)
        {
            var shipment = await _db.Shipments
                .FirstOrDefaultAsync(s => s.Id == command.Id, cancellationToken);
            if (shipment == null)
                throw new DomainException("Shipment not found");

            shipment.Status = command.Status;
            if (command.Status == "Shipped")
                shipment.ShippedAt = DateTimeOffset.UtcNow;
            if (command.Status == "Delivered")
                shipment.DeliveredAt = DateTimeOffset.UtcNow;

            await _db.SaveChangesAsync(cancellationToken);

            return Unit.Value;
        }
    }

    public class UpdateShipmentTrackingCommandHandler : ICommandHandler<UpdateShipmentTrackingCommand, Unit>
    {
        private readonly IApplicationDbContext _db;

        public UpdateShipmentTrackingCommandHandler(IApplicationDbContext db)
        {
            _db = db;
        }

        public async Task<Unit> Handle(UpdateShipmentTrackingCommand command, CancellationToken cancellationToken = default)
        {
            var shipment = await _db.Shipments
                .FirstOrDefaultAsync(s => s.Id == command.Id, cancellationToken);
            if (shipment == null)
                throw new DomainException("Shipment not found");

            shipment.Carrier = command.Carrier;
            shipment.TrackingNumber = command.TrackingNumber;

            await _db.SaveChangesAsync(cancellationToken);

            return Unit.Value;
        }
    }
}