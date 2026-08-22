using System;
using System.Threading;
using System.Threading.Tasks;
using Ecommerce.Application.Common.Commands;
using Ecommerce.Application.DTOs;
using Ecommerce.Application.Interfaces;
using Ecommerce.Domain.Exceptions;
using Microsoft.EntityFrameworkCore;

namespace Ecommerce.Application.Commands.Admin
{
    public class UpdateStoreSettingsCommand : ICommand<StoreSettingsDto>
    {
        public decimal StandardShippingCost { get; set; }
        public decimal? FreeShippingThreshold { get; set; }
        public string StoreName { get; set; } = "Sofan Store";
        public string? ContactEmail { get; set; }
        public string? ContactPhone { get; set; }
        public string CurrencyCode { get; set; } = "ILS";
    }

    public class UpdateStoreSettingsCommandHandler : ICommandHandler<UpdateStoreSettingsCommand, StoreSettingsDto>
    {
        private readonly IApplicationDbContext _db;

        public UpdateStoreSettingsCommandHandler(IApplicationDbContext db)
        {
            _db = db;
        }

        public async Task<StoreSettingsDto> Handle(UpdateStoreSettingsCommand command, CancellationToken cancellationToken = default)
        {
            if (command.StandardShippingCost < 0)
                throw new DomainException("Standard shipping cost cannot be negative");

            if (command.FreeShippingThreshold.HasValue && command.FreeShippingThreshold.Value < 0)
                throw new DomainException("Free shipping threshold cannot be negative");

            var setting = await _db.StoreSettings.FirstOrDefaultAsync(cancellationToken);
            if (setting == null)
            {
                setting = new Domain.Entities.StoreSetting();
                await _db.StoreSettings.AddAsync(setting, cancellationToken);
            }

            setting.StandardShippingCost = command.StandardShippingCost;
            setting.FreeShippingThreshold = command.FreeShippingThreshold;
            if (!string.IsNullOrWhiteSpace(command.StoreName))
                setting.StoreName = command.StoreName.Trim();
            setting.ContactEmail = command.ContactEmail?.Trim();
            setting.ContactPhone = command.ContactPhone?.Trim();
            if (!string.IsNullOrWhiteSpace(command.CurrencyCode))
                setting.CurrencyCode = command.CurrencyCode.Trim().ToUpperInvariant();
            setting.UpdatedAt = DateTimeOffset.UtcNow;

            await _db.SaveChangesAsync(cancellationToken);

            return new StoreSettingsDto
            {
                Id = setting.Id,
                StandardShippingCost = setting.StandardShippingCost,
                FreeShippingThreshold = setting.FreeShippingThreshold,
                StoreName = setting.StoreName,
                ContactEmail = setting.ContactEmail,
                ContactPhone = setting.ContactPhone,
                CurrencyCode = setting.CurrencyCode,
                UpdatedAt = setting.UpdatedAt
            };
        }
    }
}
