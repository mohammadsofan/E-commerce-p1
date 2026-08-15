using System;

namespace Ecommerce.Application.Interfaces
{
    public interface ICurrentUserService
    {
        Guid? UserId { get; }
        string UserName { get; }
    }
}
