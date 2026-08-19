using System;
using System.Threading;
using System.Threading.Tasks;
using AutoMapper;
using Ecommerce.Application.Common.Commands;
using Ecommerce.Application.DTOs;
using Ecommerce.Application.Interfaces;
using Ecommerce.Domain.Entities;
using Ecommerce.Domain.Exceptions;
using Microsoft.EntityFrameworkCore;

namespace Ecommerce.Application.Commands.Admin
{
    public class SetInventoryStockCommandHandler : ICommandHandler<SetInventoryStockCommand, AdminInventoryDto>
    {
        private readonly IApplicationDbContext _db;
        private readonly IMapper _mapper;

        public SetInventoryStockCommandHandler(IApplicationDbContext db, IMapper mapper)
        {
            _db = db;
            _mapper = mapper;
        }

        public async Task<AdminInventoryDto> Handle(SetInventoryStockCommand command, CancellationToken cancellationToken = default)
        {
            var inventoryItem = await _db.InventoryItems
                .FirstOrDefaultAsync(i => i.Id == command.InventoryItemId, cancellationToken);

            if (inventoryItem == null)
                throw new NotFoundException("InventoryItem", command.InventoryItemId);

            inventoryItem.SetStock(command.QuantityOnHand);

            await _db.SaveChangesAsync(cancellationToken);

            return _mapper.Map<AdminInventoryDto>(inventoryItem);
        }
    }
}