using Application.Interfaces;
using MediatR;

namespace Application.Queries;

public class GetQrCodeQuery : IRequest<string>
{
}

public class GetQrCodeHandler : IRequestHandler<GetQrCodeQuery, string>
{
    private readonly IWhatsAppService _whatsAppService;

    public GetQrCodeHandler(IWhatsAppService whatsAppService)
    {
        _whatsAppService = whatsAppService;
    }

    public async Task<string> Handle(GetQrCodeQuery request, CancellationToken cancellationToken)
    {
        try
        {
            return await _whatsAppService.GetQrCodeAsync();
        }
        catch
        {
            return string.Empty;
        }
    }
}
