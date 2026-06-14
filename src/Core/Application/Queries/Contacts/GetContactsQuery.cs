using Application.DTOs;
using MediatR;

namespace Application.Queries.Contacts;

public class GetContactsQuery : IRequest<IEnumerable<ContactDto>>
{
    public string? Tag { get; set; }
    public string? Search { get; set; }
}
