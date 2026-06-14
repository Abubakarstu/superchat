using Application.Interfaces;
using MediatR;

namespace Application.Queries;

public class GetConnectionStatusQuery : IRequest<bool>
{
}

public class GetConnectionStatusHandler : IRequestHandler<GetConnectionStatusQuery, bool>
{
    private readonly IWhatsAppService _whatsAppService;

    public GetConnectionStatusHandler(IWhatsAppService whatsAppService)
    {
        _whatsAppService = whatsAppService;
    }

    public async Task<bool> Handle(GetConnectionStatusQuery request, CancellationToken cancellationToken)
    {
        return await _whatsAppService.CheckConnectionAsync();
    }
}
