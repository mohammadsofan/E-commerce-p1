using System;
using System.Collections.Generic;
using Ecommerce.Application.Common.Queries;
using Ecommerce.Application.DTOs;

namespace Ecommerce.Application.Queries.Products
{
    public class GetFrequentlyBoughtTogetherQuery : IQuery<List<ProductDto>>
    {
        public List<Guid> ProductIds { get; set; } = new List<Guid>();
        public int Limit { get; set; } = 4;

        public GetFrequentlyBoughtTogetherQuery() { }

        public GetFrequentlyBoughtTogetherQuery(List<Guid> productIds, int limit = 4)
        {
            ProductIds = productIds ?? new List<Guid>();
            Limit = limit > 0 ? limit : 4;
        }
    }
}
