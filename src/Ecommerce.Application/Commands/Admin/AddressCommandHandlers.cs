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
    public class CreateAddressCommandHandler : ICommandHandler<CreateAddressCommand, AddressDto>
    {
        private readonly IApplicationDbContext _db;
        private readonly IMapper _mapper;
        private readonly ICurrentUserService _currentUser;

        public CreateAddressCommandHandler(IApplicationDbContext db, IMapper mapper, ICurrentUserService currentUser)
        {
            _db = db;
            _mapper = mapper;
            _currentUser = currentUser;
        }

        public async Task<AddressDto> Handle(CreateAddressCommand command, CancellationToken cancellationToken = default)
        {
            var userId = _currentUser.UserId ?? throw new DomainException("User is not authenticated");
            var now = DateTimeOffset.UtcNow;

            var address = new Address
            {
                Id = Guid.NewGuid(),
                UserId = userId,
                Type = command.Type,
                FirstName = command.FirstName,
                LastName = command.LastName,
                CompanyName = command.CompanyName,
                AddressLine1 = command.AddressLine1,
                AddressLine2 = command.AddressLine2,
                City = command.City,
                State = command.State,
                PostalCode = command.PostalCode,
                CountryCode = command.EffectiveCountryCode,
                PhoneNumber = command.PhoneNumber,
                IsDefaultShipping = command.EffectiveIsDefaultShipping,
                IsDefaultBilling = command.EffectiveIsDefaultBilling,
                CreatedAt = now,
                UpdatedAt = now
            };

            // The very first address a customer saves becomes their default, otherwise the
            // checkout address picker would have nothing pre-selected.
            var hasExistingAddress = await _db.Addresses
                .AnyAsync(a => a.UserId == userId && !a.IsDeleted, cancellationToken);
            if (!hasExistingAddress)
            {
                address.IsDefaultShipping = true;
                address.IsDefaultBilling = true;
            }

            if (address.IsDefaultShipping)
                await ClearDefaultShippingAsync(userId, address.Id, cancellationToken);
            if (address.IsDefaultBilling)
                await ClearDefaultBillingAsync(userId, address.Id, cancellationToken);

            _db.Addresses.Add(address);
            await _db.SaveChangesAsync(cancellationToken);

            return _mapper.Map<AddressDto>(address);
        }

        protected async Task ClearDefaultShippingAsync(Guid userId, Guid excludeId, CancellationToken cancellationToken)
        {
            var defaults = await _db.Addresses
                .Where(a => a.UserId == userId && a.Id != excludeId && !a.IsDeleted && a.IsDefaultShipping)
                .ToListAsync(cancellationToken);
            foreach (var a in defaults)
                a.IsDefaultShipping = false;
        }

        protected async Task ClearDefaultBillingAsync(Guid userId, Guid excludeId, CancellationToken cancellationToken)
        {
            var defaults = await _db.Addresses
                .Where(a => a.UserId == userId && a.Id != excludeId && !a.IsDeleted && a.IsDefaultBilling)
                .ToListAsync(cancellationToken);
            foreach (var a in defaults)
                a.IsDefaultBilling = false;
        }
    }

    public class UpdateAddressCommandHandler : ICommandHandler<UpdateAddressCommand, AddressDto>
    {
        private readonly IApplicationDbContext _db;
        private readonly IMapper _mapper;
        private readonly ICurrentUserService _currentUser;

        public UpdateAddressCommandHandler(IApplicationDbContext db, IMapper mapper, ICurrentUserService currentUser)
        {
            _db = db;
            _mapper = mapper;
            _currentUser = currentUser;
        }

        public async Task<AddressDto> Handle(UpdateAddressCommand command, CancellationToken cancellationToken = default)
        {
            var userId = _currentUser.UserId ?? throw new DomainException("User is not authenticated");

            var address = await _db.Addresses
                .FirstOrDefaultAsync(a => a.Id == command.Id && a.UserId == userId && !a.IsDeleted, cancellationToken);
            if (address == null)
                throw new NotFoundException("Address", command.Id);

            if (command.EffectiveIsDefaultShipping)
                await ClearDefaultShippingAsync(userId, address.Id, cancellationToken);
            if (command.EffectiveIsDefaultBilling)
                await ClearDefaultBillingAsync(userId, address.Id, cancellationToken);

            address.Type = command.Type;
            address.FirstName = command.FirstName;
            address.LastName = command.LastName;
            address.CompanyName = command.CompanyName;
            address.AddressLine1 = command.AddressLine1;
            address.AddressLine2 = command.AddressLine2;
            address.City = command.City;
            address.State = command.State;
            address.PostalCode = command.PostalCode;
            address.CountryCode = command.EffectiveCountryCode;
            address.PhoneNumber = command.PhoneNumber;
            address.IsDefaultShipping = command.EffectiveIsDefaultShipping;
            address.IsDefaultBilling = command.EffectiveIsDefaultBilling;
            address.UpdatedAt = DateTimeOffset.UtcNow;

            await _db.SaveChangesAsync(cancellationToken);

            return _mapper.Map<AddressDto>(address);
        }

        protected async Task ClearDefaultShippingAsync(Guid userId, Guid excludeId, CancellationToken cancellationToken)
        {
            var defaults = await _db.Addresses
                .Where(a => a.UserId == userId && a.Id != excludeId && !a.IsDeleted && a.IsDefaultShipping)
                .ToListAsync(cancellationToken);
            foreach (var a in defaults)
                a.IsDefaultShipping = false;
        }

        protected async Task ClearDefaultBillingAsync(Guid userId, Guid excludeId, CancellationToken cancellationToken)
        {
            var defaults = await _db.Addresses
                .Where(a => a.UserId == userId && a.Id != excludeId && !a.IsDeleted && a.IsDefaultBilling)
                .ToListAsync(cancellationToken);
            foreach (var a in defaults)
                a.IsDefaultBilling = false;
        }
    }

    public class DeleteAddressCommandHandler : ICommandHandler<DeleteAddressCommand, Unit>
    {
        private readonly IApplicationDbContext _db;
        private readonly ICurrentUserService _currentUser;

        public DeleteAddressCommandHandler(IApplicationDbContext db, ICurrentUserService currentUser)
        {
            _db = db;
            _currentUser = currentUser;
        }

        public async Task<Unit> Handle(DeleteAddressCommand command, CancellationToken cancellationToken = default)
        {
            var userId = _currentUser.UserId ?? throw new DomainException("User is not authenticated");

            var address = await _db.Addresses
                .FirstOrDefaultAsync(a => a.Id == command.Id && a.UserId == userId && !a.IsDeleted, cancellationToken);
            if (address == null)
                throw new NotFoundException("Address", command.Id);

            address.IsDeleted = true;
            address.UpdatedAt = DateTimeOffset.UtcNow;

            await _db.SaveChangesAsync(cancellationToken);

            return Unit.Value;
        }
    }
}
