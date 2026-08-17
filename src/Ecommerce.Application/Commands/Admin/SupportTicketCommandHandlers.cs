using System;
using System.Threading;
using System.Threading.Tasks;
using AutoMapper;
using Ecommerce.Application.Common;
using Ecommerce.Application.Common.Commands;
using Ecommerce.Application.DTOs;
using Ecommerce.Application.Interfaces;
using Ecommerce.Domain.Entities;
using Ecommerce.Domain.Exceptions;
using Microsoft.EntityFrameworkCore;

namespace Ecommerce.Application.Commands.Admin
{
    public class CreateSupportTicketCommandHandler : ICommandHandler<CreateSupportTicketCommand, SupportTicketDto>
    {
        private readonly IApplicationDbContext _db;
        private readonly IMapper _mapper;
        private readonly ICurrentUserService _currentUser;

        public CreateSupportTicketCommandHandler(IApplicationDbContext db, IMapper mapper, ICurrentUserService currentUser)
        {
            _db = db;
            _mapper = mapper;
            _currentUser = currentUser;
        }

        public async Task<SupportTicketDto> Handle(CreateSupportTicketCommand command, CancellationToken cancellationToken = default)
        {
            var userId = _currentUser.UserId ?? throw new DomainException("User is not authenticated");
            var now = DateTimeOffset.UtcNow;

            var ticket = new SupportTicket
            {
                Id = Guid.NewGuid(),
                UserId = userId,
                Subject = command.Subject,
                Status = "Open",
                Priority = command.Priority,
                CreatedAt = now,
                UpdatedAt = now
            };

            ticket.Messages.Add(new SupportTicketMessage
            {
                Id = Guid.NewGuid(),
                SupportTicketId = ticket.Id,
                UserId = userId,
                Message = command.Message,
                IsInternal = false,
                CreatedAt = now
            });

            _db.SupportTickets.Add(ticket);
            await _db.SaveChangesAsync(cancellationToken);

            return _mapper.Map<SupportTicketDto>(ticket);
        }
    }

    public class ReplySupportTicketCommandHandler : ICommandHandler<ReplySupportTicketCommand, Unit>
    {
        private readonly IApplicationDbContext _db;
        private readonly ICurrentUserService _currentUser;

        public ReplySupportTicketCommandHandler(IApplicationDbContext db, ICurrentUserService currentUser)
        {
            _db = db;
            _currentUser = currentUser;
        }

        public async Task<Unit> Handle(ReplySupportTicketCommand command, CancellationToken cancellationToken = default)
        {
            var userId = _currentUser.UserId ?? throw new DomainException("User is not authenticated");

            var ticket = await _db.SupportTickets
                .FirstOrDefaultAsync(t => t.Id == command.Id, cancellationToken);
            if (ticket == null)
                throw new DomainException("Support ticket not found");

            ticket.Messages.Add(new SupportTicketMessage
            {
                Id = Guid.NewGuid(),
                SupportTicketId = ticket.Id,
                UserId = userId,
                Message = command.Message,
                IsInternal = command.IsInternal,
                CreatedAt = DateTimeOffset.UtcNow
            });

            ticket.Status = command.IsInternal ? "InProgress" : "WaitingOnCustomer";
            ticket.UpdatedAt = DateTimeOffset.UtcNow;

            await _db.SaveChangesAsync(cancellationToken);

            return Unit.Value;
        }
    }

    public class UpdateSupportTicketCommandHandler : ICommandHandler<UpdateSupportTicketCommand, Unit>
    {
        private readonly IApplicationDbContext _db;

        public UpdateSupportTicketCommandHandler(IApplicationDbContext db)
        {
            _db = db;
        }

        public async Task<Unit> Handle(UpdateSupportTicketCommand command, CancellationToken cancellationToken = default)
        {
            var ticket = await _db.SupportTickets
                .FirstOrDefaultAsync(t => t.Id == command.Id, cancellationToken);
            if (ticket == null)
                throw new DomainException("Support ticket not found");

            if (!string.IsNullOrWhiteSpace(command.Status))
                ticket.Status = command.Status;
            if (!string.IsNullOrWhiteSpace(command.Priority))
                ticket.Priority = command.Priority;
            ticket.AssignedToUserId = command.AssignedToUserId;
            ticket.UpdatedAt = DateTimeOffset.UtcNow;

            await _db.SaveChangesAsync(cancellationToken);

            return Unit.Value;
        }
    }
}