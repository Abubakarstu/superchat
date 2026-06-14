using Application.DTOs;
using MediatR;

namespace Application.Queries;

public class GetMessagesQuery : IRequest<IEnumerable<MessageDto>>
{
    public Guid ConversationId { get; set; }
}
