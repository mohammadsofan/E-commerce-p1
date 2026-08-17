using System;
using Ecommerce.Application.Common;
using Ecommerce.Application.Common.Commands;
using Ecommerce.Application.DTOs;

namespace Ecommerce.Application.Commands.Admin
{
    public class SubmitProductReviewCommand : ICommand<ProductReviewDto>
    {
        public Guid ProductId { get; set; }
        public int Rating { get; set; }
        public string Title { get; set; } = string.Empty;
        public string Comment { get; set; } = string.Empty;
    }

    public class UpdateReviewStatusCommand : ICommand<Unit>
    {
        public Guid Id { get; set; }
        public bool IsApproved { get; set; }
    }

    public class DeleteReviewCommand : ICommand<Unit>
    {
        public Guid Id { get; set; }
    }
}