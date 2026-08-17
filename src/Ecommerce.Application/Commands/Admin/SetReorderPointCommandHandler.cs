using System;
using System.Threading;
using System.Threading.Tasks;
using Ecommerce.Application.Common;
using Ecommerce.Application.Common.Commands;
using Ecommerce.Application.Interfaces;
using Ecommerce.Domain.Entities;
using Ecommerce.Domain.Exceptions;
using Microsoft.EntityFrameworkCore;

namespace Ecommerce.Application.Commands.Admin
{
    public class SetReorderPointCommandHandler : ICommandHandler<SetReorderPointCommand, Unit>
    {
        private readonly IApplicationDbContext _db;

        public SetReorderPointCommandHandler(IApplicationDbContext db)
        {
            _db = db;
        }

        public async Task<Unit> Handle(SetReorderPointCommand command, CancellationToken cancellationToken = default)
        {
            var item = await _db.InventoryItems
                .FirstOrDefaultAsync(i => i.Id == command.InventoryItemId, cancellationToken);

            if (item == null)
                throw new NotFoundException("InventoryItem", command.InventoryItemId);

            item.SetReorderPoint(command.ReorderLevel, command.ReorderQuantity);

            await _db.SaveChangesAsync(cancellationToken);
            return new Unit();
        }
    }
}