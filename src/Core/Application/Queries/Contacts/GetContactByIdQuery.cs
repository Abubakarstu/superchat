using Application.DTOs;
using MediatR;

namespace Application.Queries.Contacts;

public class GetContactByIdQuery : IRequest<ContactDto?>
{
    public Guid Id { get; set; }
}
