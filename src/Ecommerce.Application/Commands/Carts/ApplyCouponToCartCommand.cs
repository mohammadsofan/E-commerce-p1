using System;
using Ecommerce.Application.Common.Commands;
using Ecommerce.Application.DTOs;

namespace Ecommerce.Application.Commands.Carts
{
    public class ApplyCouponToCartCommand : ICommand<CartDto>
    {
        public string Code { get; set; } = string.Empty;
    }
}
