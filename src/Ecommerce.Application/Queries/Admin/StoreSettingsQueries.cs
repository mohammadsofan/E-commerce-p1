using System.Threading;
using System.Threading.Tasks;
using Ecommerce.Application.Common.Queries;
using Ecommerce.Application.DTOs;
using Ecommerce.Application.Interfaces;
using Microsoft.EntityFrameworkCore;

namespace Ecommerce.Application.Queries.Admin
{
    public class GetStoreSettingsQuery : IQuery<StoreSettingsDto>
    {
    }

    public class GetShippingSettingsQuery : IQuery<ShippingSettingsDto>
    {
    }

    public class GetStoreSettingsQueryHandler : IQueryHandler<GetStoreSettingsQuery, StoreSettingsDto>
    {
        private readonly IApplicationDbContext _db;

        public GetStoreSettingsQueryHandler(IApplicationDbContext db)
        {
            _db = db;
        }

        public async Task<StoreSettingsDto> Handle(GetStoreSettingsQuery query, CancellationToken cancellationToken = default)
        {
            var setting = await _db.StoreSettings.FirstOrDefaultAsync(cancellationToken);
            if (setting == null)
            {
                setting = new Domain.Entities.StoreSetting();
                await _db.StoreSettings.AddAsync(setting, cancellationToken);
                await _db.SaveChangesAsync(cancellationToken);
            }

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

    public class GetShippingSettingsQueryHandler : IQueryHandler<GetShippingSettingsQuery, ShippingSettingsDto>
    {
        private readonly IApplicationDbContext _db;

        public GetShippingSettingsQueryHandler(IApplicationDbContext db)
        {
            _db = db;
        }

        public async Task<ShippingSettingsDto> Handle(GetShippingSettingsQuery query, CancellationToken cancellationToken = default)
        {
            var setting = await _db.StoreSettings.FirstOrDefaultAsync(cancellationToken);
            return new ShippingSettingsDto
            {
                StandardShippingCost = setting?.StandardShippingCost ?? 15m,
                FreeShippingThreshold = setting?.FreeShippingThreshold ?? 50m,
                CurrencyCode = setting?.CurrencyCode ?? "ILS"
            };
        }
    }
}
