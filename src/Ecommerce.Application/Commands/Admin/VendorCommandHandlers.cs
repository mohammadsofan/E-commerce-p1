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
    public class CreateVendorCommandHandler : ICommandHandler<CreateVendorCommand, VendorDto>
    {
        private readonly IApplicationDbContext _db;
        private readonly IMapper _mapper;

        public CreateVendorCommandHandler(IApplicationDbContext db, IMapper mapper)
        {
            _db = db;
            _mapper = mapper;
        }

        public async Task<VendorDto> Handle(CreateVendorCommand command, CancellationToken cancellationToken = default)
        {
            var code = command.Code.Trim().ToUpperInvariant();
            var existing = await _db.Vendors
                .FirstOrDefaultAsync(v => v.Code == code, cancellationToken);
            if (existing != null)
                throw new DomainException($"Vendor with code {code} already exists");

            var vendor = new Vendor
            {
                Id = Guid.NewGuid(),
                Name = command.Name,
                Code = code,
                IsActive = command.IsActive,
                CreatedAt = DateTimeOffset.UtcNow
            };

            _db.Vendors.Add(vendor);
            await _db.SaveChangesAsync(cancellationToken);

            return _mapper.Map<VendorDto>(vendor);
        }
    }

    public class UpdateVendorCommandHandler : ICommandHandler<UpdateVendorCommand, VendorDto>
    {
        private readonly IApplicationDbContext _db;
        private readonly IMapper _mapper;

        public UpdateVendorCommandHandler(IApplicationDbContext db, IMapper mapper)
        {
            _db = db;
            _mapper = mapper;
        }

        public async Task<VendorDto> Handle(UpdateVendorCommand command, CancellationToken cancellationToken = default)
        {
            var vendor = await _db.Vendors
                .FirstOrDefaultAsync(v => v.Id == command.Id, cancellationToken);
            if (vendor == null)
                throw new DomainException("Vendor not found");

            var code = command.Code.Trim().ToUpperInvariant();
            var conflict = await _db.Vendors
                .FirstOrDefaultAsync(v => v.Code == code && v.Id != command.Id, cancellationToken);
            if (conflict != null)
                throw new DomainException($"Vendor with code {code} already exists");

            vendor.Name = command.Name;
            vendor.Code = code;
            vendor.IsActive = command.IsActive;

            await _db.SaveChangesAsync(cancellationToken);

            return _mapper.Map<VendorDto>(vendor);
        }
    }

    public class DeleteVendorCommandHandler : ICommandHandler<DeleteVendorCommand, Unit>
    {
        private readonly IApplicationDbContext _db;

        public DeleteVendorCommandHandler(IApplicationDbContext db)
        {
            _db = db;
        }

        public async Task<Unit> Handle(DeleteVendorCommand command, CancellationToken cancellationToken = default)
        {
            var vendor = await _db.Vendors
                .FirstOrDefaultAsync(v => v.Id == command.Id, cancellationToken);
            if (vendor == null)
                throw new DomainException("Vendor not found");

            _db.Vendors.Remove(vendor);
            await _db.SaveChangesAsync(cancellationToken);

            return Unit.Value;
        }
    }

    public class CreateVendorProductCommandHandler : ICommandHandler<CreateVendorProductCommand, VendorProductDto>
    {
        private readonly IApplicationDbContext _db;
        private readonly IMapper _mapper;

        public CreateVendorProductCommandHandler(IApplicationDbContext db, IMapper mapper)
        {
            _db = db;
            _mapper = mapper;
        }

        public async Task<VendorProductDto> Handle(CreateVendorProductCommand command, CancellationToken cancellationToken = default)
        {
            var vendor = await _db.Vendors
                .AsNoTracking()
                .FirstOrDefaultAsync(v => v.Id == command.VendorId, cancellationToken);
            if (vendor == null)
                throw new DomainException("Vendor not found");

            var product = await _db.Products
                .AsNoTracking()
                .FirstOrDefaultAsync(p => p.Id == command.ProductId, cancellationToken);
            if (product == null)
                throw new DomainException("Product not found");

            var vendorProduct = new VendorProduct
            {
                Id = Guid.NewGuid(),
                VendorId = command.VendorId,
                ProductId = command.ProductId,
                VendorSku = command.VendorSku,
                Price = command.Price,
                IsActive = true
            };

            _db.VendorProducts.Add(vendorProduct);
            await _db.SaveChangesAsync(cancellationToken);

            var dto = _mapper.Map<VendorProductDto>(vendorProduct);
            dto.VendorName = vendor.Name;
            dto.ProductName = product.Name;

            return dto;
        }
    }

    public class DeleteVendorProductCommandHandler : ICommandHandler<DeleteVendorProductCommand, Unit>
    {
        private readonly IApplicationDbContext _db;

        public DeleteVendorProductCommandHandler(IApplicationDbContext db)
        {
            _db = db;
        }

        public async Task<Unit> Handle(DeleteVendorProductCommand command, CancellationToken cancellationToken = default)
        {
            var vendorProduct = await _db.VendorProducts
                .FirstOrDefaultAsync(vp => vp.Id == command.Id, cancellationToken);
            if (vendorProduct == null)
                throw new DomainException("Vendor product not found");

            _db.VendorProducts.Remove(vendorProduct);
            await _db.SaveChangesAsync(cancellationToken);

            return Unit.Value;
        }
    }
}