using Application.DTOs;
using MediatR;

namespace Application.Queries.WebWidget;

public class GetWidgetConfigQuery : IRequest<WebWidgetDto?>
{
}
