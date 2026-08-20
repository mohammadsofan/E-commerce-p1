using System;
using Ecommerce.Application.Common.Commands;
using Ecommerce.Application.DTOs;

namespace Ecommerce.Application.Commands.StoreFeatures
{
    public class CreateStoreFeatureCommand : ICommand<StoreFeatureDto>
    {
        public string Title { get; set; } = string.Empty;
        public string Description { get; set; } = string.Empty;
        public string IconName { get; set; } = "Truck";
        public int DisplayOrder { get; set; } = 0;
        public bool IsActive { get; set; } = true;
    }

    public class UpdateStoreFeatureCommand : ICommand<StoreFeatureDto>
    {
        public Guid Id { get; set; }
        public string Title { get; set; } = string.Empty;
        public string Description { get; set; } = string.Empty;
        public string IconName { get; set; } = "Truck";
        public int DisplayOrder { get; set; }
        public bool IsActive { get; set; }
    }

    public class DeleteStoreFeatureCommand : ICommand<Ecommerce.Application.Common.Unit>
    {
        public Guid Id { get; set; }
    }
}
