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
        try
        {
            var widgets = await _widgetRepo.GetAllAsync(ct);
            var w = widgets.FirstOrDefault(w => w.IsActive);
            if (w != null)
                return new WebWidgetDto
                {
                    Id = w.Id, Name = w.Name, GreetingText = w.GreetingText,
                    PrimaryColor = w.PrimaryColor, Position = w.Position,
                    IsActive = w.IsActive, WhatsAppNumber = w.WhatsAppNumber, EnableBot = w.EnableBot
                };
        }
        catch { }

        return new WebWidgetDto
        {
            Id = Guid.NewGuid(), Name = "Default Widget",
            GreetingText = "Hi! How can we help you?",
            PrimaryColor = "#075e54", Position = "right",
            IsActive = true, EnableBot = true
        };
    }
}
