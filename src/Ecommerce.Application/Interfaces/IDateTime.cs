using System;

namespace Ecommerce.Application.Interfaces
{
    public interface IDateTime
    {
        DateTimeOffset Now { get; }
    }
}
