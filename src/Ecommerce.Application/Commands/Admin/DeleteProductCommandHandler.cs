using Ecommerce.Application.Common;
using Ecommerce.Application.Common.Commands;
using Ecommerce.Application.Interfaces;
using Ecommerce.Domain.Entities;
using Ecommerce.Domain.Exceptions;
using Microsoft.EntityFrameworkCore;
using System;
using System.Threading;
using System.Threading.Tasks;

namespace Ecommerce.Application.Commands.Admin
{
    public class DeleteProductCommandHandler : ICommandHandler<DeleteProductCommand, Unit>
    {
        private readonly IApplicationDbContext _db;

        public DeleteProductCommandHandler(IApplicationDbContext db)
        {
            _db = db;
        }

        public async Task<Unit> Handle(DeleteProductCommand command, CancellationToken cancellationToken = default)
        {
            var product = await _db.Products
                .FirstOrDefaultAsync(p => p.Id == command.Id, cancellationToken);

            if (product == null)
                throw new NotFoundException("Product", command.Id);

            if (command.HardDelete)
            {
                _db.Products.Remove(product);
            }
            else
            {
                product.IsDeleted = true;
                product.UpdatedAt = DateTimeOffset.UtcNow;
            }

            await _db.SaveChangesAsync(cancellationToken);
            return new Unit();
        }
    }
}