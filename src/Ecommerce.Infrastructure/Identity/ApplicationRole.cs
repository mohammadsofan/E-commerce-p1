using System;
using Microsoft.AspNetCore.Identity;

namespace Ecommerce.Infrastructure.Identity
{
    public class ApplicationRole : IdentityRole<Guid>
    {
        public string Description { get; set; }
        public DateTimeOffset CreatedAt { get; set; }
    }
}
