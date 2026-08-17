using System;
using System.Collections.Generic;
using Ecommerce.Application.Common;
using Ecommerce.Application.Common.Queries;
using Ecommerce.Application.DTOs;

namespace Ecommerce.Application.Queries.Admin
{
    public class GetMyAddressesQuery : IQuery<List<AddressDto>>
    {
    }

    public class GetAddressByIdQuery : IQuery<AddressDto>
    {
        public Guid Id { get; set; }
    }
}