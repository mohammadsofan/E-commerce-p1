using System.Threading;
using System.Threading.Tasks;
using AutoMapper;
using Ecommerce.Application.Common.Queries;
using Ecommerce.Application.DTOs;
using Ecommerce.Application.Interfaces;
using Ecommerce.Application.Queries.Admin;
using Ecommerce.Domain.Exceptions;
using Microsoft.EntityFrameworkCore;

namespace Ecommerce.Application.Queries.Admin
{
    public class GetAdminProductByIdQueryHandler : IQueryHandler<GetAdminProductByIdQuery, AdminProductDto>
    {
        private readonly IApplicationDbContext _db;
        private readonly IMapper _mapper;

        public GetAdminProductByIdQueryHandler(IApplicationDbContext db, IMapper mapper)
        {
            _db = db;
            _mapper = mapper;
        }

        public async Task<AdminProductDto> Handle(GetAdminProductByIdQuery query, CancellationToken cancellationToken = default)
        {
            var product = await _db.Products
                .Include(p => p.Variants)
                .Include(p => p.Images)
                .Include(p => p.InventoryItems)
                .FirstOrDefaultAsync(p => p.Id == query.Id, cancellationToken);

            if (product == null)
                throw new NotFoundException("Product", query.Id);

            return _mapper.Map<AdminProductDto>(product);
        }
    }
}