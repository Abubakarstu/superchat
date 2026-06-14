using Application.Interfaces;
using Domain.Interfaces;
using MediatR;
using Microsoft.Extensions.Logging;

namespace Application.Commands.Campaigns;

public class SendCampaignHandler : IRequestHandler<SendCampaignCommand, bool>
{
    private readonly ICampaignRepository _campaignRepo;
    private readonly IContactRepository _contactRepo;
    private readonly IUnitOfWork _uow;
    private readonly IWhatsAppService _whatsApp;
    private readonly ILogger<SendCampaignHandler> _logger;

    public SendCampaignHandler(
        ICampaignRepository campaignRepo,
        IContactRepository contactRepo,
        IUnitOfWork uow,
        IWhatsAppService whatsApp,
        ILogger<SendCampaignHandler> logger)
    {
        _campaignRepo = campaignRepo;
        _contactRepo = contactRepo;
        _uow = uow;
        _whatsApp = whatsApp;
        _logger = logger;
    }

    public async Task<bool> Handle(SendCampaignCommand request, CancellationToken ct)
    {
        var campaign = await _campaignRepo.GetByIdAsync(request.CampaignId, ct);
        if (campaign == null) return false;

        campaign.Status = "SENDING";
        _campaignRepo.Update(campaign);
        await _uow.SaveChangesAsync(ct);

        var contacts = await _contactRepo.GetAllAsync(ct);
        var targetContacts = contacts.Where(c => c.IsSubscribed).ToList();
        campaign.TotalRecipients = targetContacts.Count;

        foreach (var contact in targetContacts)
        {
            try
            {
                if (!string.IsNullOrEmpty(contact.Phone))
                {
                    await _whatsApp.SendMessageAsync(contact.Phone, campaign.Name);
                    campaign.DeliveredCount++;
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Campaign send failed for {Phone}", contact.Phone);
            }
        }

        campaign.Status = "SENT";
        campaign.SentAt = DateTime.UtcNow;
        _campaignRepo.Update(campaign);
        await _uow.SaveChangesAsync(ct);
        return true;
    }
}
