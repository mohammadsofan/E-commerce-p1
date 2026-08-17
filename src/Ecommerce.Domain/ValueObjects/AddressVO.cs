using System;

namespace Ecommerce.Domain.ValueObjects
{
    public sealed class AddressVO
    {
        public string FirstName { get; }
        public string LastName { get; }
        public string AddressLine1 { get; }
        public string AddressLine2 { get; }
        public string City { get; }
        public string State { get; }
        public string PostalCode { get; }
        public string CountryCode { get; }

        public AddressVO(string firstName, string lastName, string addressLine1, string city, string postalCode, string countryCode)
        {
            FirstName = firstName ?? throw new ArgumentNullException(nameof(firstName));
            LastName = lastName ?? throw new ArgumentNullException(nameof(lastName));
            AddressLine1 = addressLine1 ?? throw new ArgumentNullException(nameof(addressLine1));
            AddressLine2 = string.Empty;
            City = city ?? throw new ArgumentNullException(nameof(city));
            State = string.Empty;
            PostalCode = postalCode ?? throw new ArgumentNullException(nameof(postalCode));
            CountryCode = countryCode ?? throw new ArgumentNullException(nameof(countryCode));
        }
    }
}
