using Ecommerce.Application.Common;
using Ecommerce.Application.Common.Commands;

namespace Ecommerce.Application.Commands.Admin
{
    public class DeleteProductVariantCommand : ICommand<Unit>
    {
        public Guid Id { get; set; }
    }
}