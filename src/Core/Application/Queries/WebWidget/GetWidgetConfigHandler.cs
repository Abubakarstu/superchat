using Application.DTOs;
using Domain.Interfaces;
using MediatR;

namespace Application.Queries.WebWidget;

public class GetWidgetConfigHandler : IRequestHandler<GetWidgetConfigQuery, WebWidgetDto?>
{
    private readonly IWebWidgetRepository _widgetRepo;

    public GetWidgetConfigHandler(IWebWidgetRepository widgetRepo)
    {
        _widgetRepo = widgetRepo;
    }

    public async Task<WebWidgetDto?> Handle(GetWidgetConfigQuery request, CancellationToken ct)
    {
        var widgets = await _widgetRepo.GetAllAsync(ct);
        var w = widgets.FirstOrDefault(w => w.IsActive);
        if (w == null) return null;

        return new WebWidgetDto
        {
            Id = w.Id, Name = w.Name, GreetingText = w.GreetingText,
            PrimaryColor = w.PrimaryColor, Position = w.Position,
            IsActive = w.IsActive, WhatsAppNumber = w.WhatsAppNumber, EnableBot = w.EnableBot
        };
    }
}
