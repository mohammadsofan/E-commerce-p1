using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using AutoMapper;
using Ecommerce.Application.Common.Queries;
using Ecommerce.Application.DTOs;
using Ecommerce.Application.Interfaces;
using Ecommerce.Domain.Exceptions;
using Microsoft.EntityFrameworkCore;

namespace Ecommerce.Application.Queries.Products
{
    public class GetProductBySlugQueryHandler : IQueryHandler<GetProductBySlugQuery, ProductDto>
    {
        private readonly IApplicationDbContext _db;
        private readonly IMapper _mapper;

        public GetProductBySlugQueryHandler(IApplicationDbContext db, IMapper mapper)
        {
            _db = db;
            _mapper = mapper;
        }

        public async Task<ProductDto> Handle(GetProductBySlugQuery query, CancellationToken cancellationToken = default)
        {
            var product = await _db.Products
                .AsNoTracking()
                .FirstOrDefaultAsync(p => p.Slug == query.Slug, cancellationToken);

            if (product == null) throw new NotFoundException("Product", query.Slug);
            return _mapper.Map<ProductDto>(product);
        }
    }
}
