using Application.DTOs;
using MediatR;

namespace Application.Queries.Workflows;

public class GetWorkflowsQuery : IRequest<IEnumerable<WorkflowDto>>
{
}
