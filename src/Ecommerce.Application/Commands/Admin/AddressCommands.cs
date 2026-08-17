using System;
using Ecommerce.Application.Common;
using Ecommerce.Application.Common.Commands;
using Ecommerce.Application.DTOs;

namespace Ecommerce.Application.Commands.Admin
{
    public class CreateAddressCommand : ICommand<AddressDto>
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
        public string PhoneNumber { get; set; } = string.Empty;
        public bool IsDefaultShipping { get; set; }
        public bool IsDefaultBilling { get; set; }
    }

    public class UpdateAddressCommand : ICommand<AddressDto>
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
        public string PhoneNumber { get; set; } = string.Empty;
        public bool IsDefaultShipping { get; set; }
        public bool IsDefaultBilling { get; set; }
    }

    public class DeleteAddressCommand : ICommand<Unit>
    {
        public Guid Id { get; set; }
    }
}