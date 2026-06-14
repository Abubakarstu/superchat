using Application.DTOs;
using MediatR;

namespace Application.Queries;

public class GetConversationsQuery : IRequest<IEnumerable<ConversationDto>>
{
    public bool? ActiveOnly { get; set; }
}
