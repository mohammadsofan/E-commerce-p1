using System;
using System.Text.Json.Serialization;
using Ecommerce.Application.Common;
using Ecommerce.Application.Common.Commands;
using Ecommerce.Application.DTOs;

namespace Ecommerce.Application.Commands.Admin
{
    /// <summary>
    /// Shared surface for address create/update so validation rules are written once.
    /// </summary>
    public interface IAddressCommand
    {
        string FirstName { get; }
        string LastName { get; }
        string AddressLine1 { get; }
        string AddressLine2 { get; }
        string City { get; }
        string State { get; }
        string PostalCode { get; }
        string PhoneNumber { get; }

        /// <summary>Country code after taking the legacy <c>country</c> alias into account.</summary>
        string EffectiveCountryCode { get; }
    }

    public class CreateAddressCommand : ICommand<AddressDto>, IAddressCommand
    {
        public string Type { get; set; } = "Shipping";
        public string FirstName { get; set; } = string.Empty;
        public string LastName { get; set; } = string.Empty;
        public string CompanyName { get; set; } = string.Empty;
        public string AddressLine1 { get; set; } = string.Empty;
        public string AddressLine2 { get; set; } = string.Empty;
        public string City { get; set; } = string.Empty;
        public string State { get; set; } = string.Empty;
        public string PostalCode { get; set; } = string.Empty;
        public string CountryCode { get; set; } = string.Empty;

        /// <summary>
        /// Alias accepted from clients that post <c>country</c> instead of <c>countryCode</c>.
        /// Kept so the storefront contract and the command cannot silently disagree.
        /// </summary>
        public string? Country { get; set; }

        public string PhoneNumber { get; set; } = string.Empty;
        public bool IsDefaultShipping { get; set; }
        public bool IsDefaultBilling { get; set; }

        /// <summary>
        /// Alias accepted from clients that post a single <c>isDefault</c> flag. When set it marks
        /// the address as the default for both shipping and billing.
        /// </summary>
        public bool? IsDefault { get; set; }

        [JsonIgnore]
        public string EffectiveCountryCode =>
            !string.IsNullOrWhiteSpace(CountryCode) ? CountryCode.Trim() : (Country ?? string.Empty).Trim();

        [JsonIgnore]
        public bool EffectiveIsDefaultShipping => IsDefaultShipping || (IsDefault ?? false);

        [JsonIgnore]
        public bool EffectiveIsDefaultBilling => IsDefaultBilling || (IsDefault ?? false);
    }

    public class UpdateAddressCommand : ICommand<AddressDto>, IAddressCommand
    {
        public Guid Id { get; set; }
        public string Type { get; set; } = "Shipping";
        public string FirstName { get; set; } = string.Empty;
        public string LastName { get; set; } = string.Empty;
        public string CompanyName { get; set; } = string.Empty;
        public string AddressLine1 { get; set; } = string.Empty;
        public string AddressLine2 { get; set; } = string.Empty;
        public string City { get; set; } = string.Empty;
        public string State { get; set; } = string.Empty;
        public string PostalCode { get; set; } = string.Empty;
        public string CountryCode { get; set; } = string.Empty;
        public string? Country { get; set; }
        public string PhoneNumber { get; set; } = string.Empty;
        public bool IsDefaultShipping { get; set; }
        public bool IsDefaultBilling { get; set; }
        public bool? IsDefault { get; set; }

        [JsonIgnore]
        public string EffectiveCountryCode =>
            !string.IsNullOrWhiteSpace(CountryCode) ? CountryCode.Trim() : (Country ?? string.Empty).Trim();

        [JsonIgnore]
        public bool EffectiveIsDefaultShipping => IsDefaultShipping || (IsDefault ?? false);

        [JsonIgnore]
        public bool EffectiveIsDefaultBilling => IsDefaultBilling || (IsDefault ?? false);
    }

    public class DeleteAddressCommand : ICommand<Unit>
    {
        public Guid Id { get; set; }
    }
}
