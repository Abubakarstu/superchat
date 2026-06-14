using Application.Commands;
using Application.Commands.Templates;
using Application.DTOs;
using Application.Models;
using Application.Queries.Templates;
using Domain.Entities.WhatsApp;
using Domain.Interfaces;
using MediatR;
using Microsoft.AspNetCore.Mvc;

namespace API.Controllers;

[ApiController]
[Route("api/templates")]
public class TemplatesController : ControllerBase
{
    private readonly IMediator _mediator;
    private readonly IWhatsAppTemplateRepository _templateRepo;
    private readonly IWhatsAppAccountRepository _accountRepo;
    private readonly IUnitOfWork _uow;
    public TemplatesController(IMediator mediator, IWhatsAppTemplateRepository templateRepo, IWhatsAppAccountRepository accountRepo, IUnitOfWork uow)
    {
        _mediator = mediator;
        _templateRepo = templateRepo;
        _accountRepo = accountRepo;
        _uow = uow;
    }

    [HttpGet]
    public async Task<ActionResult<IEnumerable<TemplateDto>>> GetAll([FromQuery] Guid? accountId)
    {
        return Ok(await _mediator.Send(new GetTemplatesQuery { AccountId = accountId }));
    }

    [HttpGet("{id:guid}")]
    public async Task<ActionResult<TemplateDto>> GetById(Guid id)
    {
        var templates = await _mediator.Send(new GetTemplatesQuery());
        var template = templates.FirstOrDefault(t => t.Id == id);
        if (template == null) return NotFound();
        return Ok(template);
    }

    [HttpPost]
    public async Task<ActionResult<TemplateDto>> Create([FromBody] CreateTemplateCommand command)
    {
        return Ok(await _mediator.Send(command));
    }

    [HttpPut("{id:guid}")]
    public async Task<ActionResult<TemplateDto>> Update(Guid id, [FromBody] UpdateTemplateCommand command)
    {
        if (id != command.Id) return BadRequest("ID mismatch");
        return Ok(await _mediator.Send(command));
    }

    [HttpDelete("{id:guid}")]
    public async Task<IActionResult> Delete(Guid id)
    {
        await _mediator.Send(new DeleteTemplateCommand { Id = id });
        return NoContent();
    }

    [HttpPost("{id:guid}/send")]
    public async Task<ActionResult<MessageDto>> SendToConversation(Guid id, [FromBody] SendTemplateToConversationRequest request)
    {
        var templates = await _mediator.Send(new GetTemplatesQuery());
        var template = templates.FirstOrDefault(t => t.Id == id);
        if (template == null) return NotFound("Template not found");

        var typesJson = template.TypesJson;
        if (!string.IsNullOrEmpty(typesJson) && request.Variables != null && request.Variables.Count > 0)
        {
            typesJson = InterpolateJson(typesJson, request.Variables);
        }

        var body = InterpolateTemplate(template.Body, request.Variables);
        var header = InterpolateTemplate(template.Header, request.Variables);

        var command = new SendTemplateCommand
        {
            RemoteJid = request.RemoteJid,
            Body = body,
            TemplateName = template.Name,
            Header = header,
            Footer = template.Footer,
            ContactName = request.ContactName,
            ContactPhone = request.ContactPhone,
            ContentType = template.ContentType,
            TypesJson = typesJson
        };

        return Ok(await _mediator.Send(command));
    }

    [HttpPost("seed")]
    public async Task<IActionResult> Seed()
    {
        var existing = await _templateRepo.GetAllAsync();
        if (existing.Any()) return BadRequest("Templates already exist. Delete them first or use the individual create endpoints.");

        var accountId = Guid.Parse("00000000-0000-0000-0000-000000000001");
        var samples = new List<WhatsAppTemplate>();

        // Ensure a WhatsAppAccount exists for the FK
        var account = await _accountRepo.GetByIdAsync(accountId);
        if (account == null)
        {
            _accountRepo.Add(new Domain.Entities.WhatsApp.WhatsAppAccount
            {
                Id = accountId,
                PhoneNumberId = "seed",
                BusinessAccountId = "seed",
                AccessToken = "seed",
                WabaId = "seed",
                WebhookSecret = "seed",
                DisplayName = "Seed Account",
                IsConnected = true
            });
            await _uow.SaveChangesAsync();
        }

        string Types(Dictionary<string, object> content) => System.Text.Json.JsonSerializer.Serialize(content);

        // 1. twilio/text
        samples.Add(new WhatsAppTemplate
        {
            WhatsAppAccountId = accountId,
            Name = "Welcome Text",
            Language = "en",
            Category = "UTILITY",
            Status = "APPROVED",
            Body = "Hi {{1}}, welcome to Superchat! We're excited to have you on board. Reply HELP for assistance.",
            Header = "Superchat",
            ContentType = "twilio/text",
            TypesJson = Types(new Dictionary<string, object> { ["twilio/text"] = new Dictionary<string, object> { ["body"] = "Hi {{1}}, welcome to Superchat! We're excited to have you on board. Reply HELP for assistance." } })
        });

        // 2. twilio/media
        samples.Add(new WhatsAppTemplate
        {
            WhatsAppAccountId = accountId,
            Name = "Promo Image",
            Language = "en",
            Category = "MARKETING",
            Status = "APPROVED",
            Body = "Check out our latest offer {{1}}!",
            Header = "Limited Time Offer",
            Footer = "Offer ends soon!",
            ContentType = "twilio/media",
            TypesJson = Types(new Dictionary<string, object> { ["twilio/media"] = new Dictionary<string, object> { ["body"] = "Check out our latest offer {{1}}!", ["media"] = new[] { "https://picsum.photos/400/300" } } })
        });

        // 3. twilio/quick-reply
        samples.Add(new WhatsAppTemplate
        {
            WhatsAppAccountId = accountId,
            Name = "Support Options",
            Language = "en",
            Category = "UTILITY",
            Status = "APPROVED",
            Body = "How can we help you today?",
            Header = "Customer Support",
            Footer = "Superchat Support Team",
            ContentType = "twilio/quick-reply",
            TypesJson = Types(new Dictionary<string, object> { ["twilio/quick-reply"] = new Dictionary<string, object> { ["body"] = "How can we help you today?", ["actions"] = new[] { new Dictionary<string, object> { ["type"] = "QUICK_REPLY", ["title"] = "Check Order", ["id"] = "check_order" }, new Dictionary<string, object> { ["type"] = "QUICK_REPLY", ["title"] = "Talk to Agent", ["id"] = "talk_agent" }, new Dictionary<string, object> { ["type"] = "QUICK_REPLY", ["title"] = "FAQs", ["id"] = "faqs" } } } })
        });

        // 4. twilio/call-to-action
        samples.Add(new WhatsAppTemplate
        {
            WhatsAppAccountId = accountId,
            Name = "Flash Sale",
            Language = "en",
            Category = "MARKETING",
            Status = "APPROVED",
            Body = "🔥 Flash Sale! Get {{1}}% off everything for the next 24 hours. Don't miss out!",
            Header = "⚡ Flash Sale Alert",
            Footer = "Superchat Deals",
            ContentType = "twilio/call-to-action",
            TypesJson = Types(new Dictionary<string, object> { ["twilio/call-to-action"] = new Dictionary<string, object> { ["body"] = "🔥 Flash Sale! Get {{1}}% off everything for the next 24 hours. Don't miss out!", ["actions"] = new[] { new Dictionary<string, object> { ["type"] = "URL", ["title"] = "Shop Now", ["url"] = "https://example.com/shop/{{2}}" }, new Dictionary<string, object> { ["type"] = "PHONE_NUMBER", ["title"] = "Call Us", ["phone"] = "+15551234567" } } } })
        });

        // 5. twilio/list-picker
        samples.Add(new WhatsAppTemplate
        {
            WhatsAppAccountId = accountId,
            Name = "Destination Selector",
            Language = "en",
            Category = "MARKETING",
            Status = "PENDING",
            Body = "Choose your dream destination! Sale ends {{1}}.",
            Header = "🌍 Travel Deals",
            Footer = "Superchat Travels",
            ContentType = "twilio/list-picker",
            TypesJson = Types(new Dictionary<string, object> { ["twilio/list-picker"] = new Dictionary<string, object> { ["body"] = "Choose your dream destination! Sale ends {{1}}.", ["button"] = "View Destinations", ["items"] = new[] { new Dictionary<string, object> { ["item"] = "Paris - $499", ["description"] = "Flight to CDG, 5-star hotel included", ["id"] = "paris" }, new Dictionary<string, object> { ["item"] = "Tokyo - $699", ["description"] = "Flight to NRT, 4-night stay", ["id"] = "tokyo" }, new Dictionary<string, object> { ["item"] = "Dubai - $399", ["description"] = "Flight to DXB, desert safari included", ["id"] = "dubai" } } } })
        });

        // 6. twilio/card
        samples.Add(new WhatsAppTemplate
        {
            WhatsAppAccountId = accountId,
            Name = "Elite Status Card",
            Language = "en",
            Category = "UTILITY",
            Status = "APPROVED",
            Body = "Congratulations, you've reached Elite status! Use code {{1}} for 10% off.",
            Header = "🌟 Elite Member",
            Footer = "Valid until Dec 2026",
            ContentType = "twilio/card",
            TypesJson = Types(new Dictionary<string, object> { ["twilio/card"] = new Dictionary<string, object> { ["title"] = "Elite Member", ["body"] = "Congratulations, you've reached Elite status! Use code {{1}} for 10% off.", ["subtitle"] = "Valid until Dec 2026", ["media"] = new[] { "https://picsum.photos/400/200" }, ["actions"] = new[] { new Dictionary<string, object> { ["type"] = "URL", ["title"] = "Shop Now", ["url"] = "https://example.com/{{2}}" }, new Dictionary<string, object> { ["type"] = "PHONE_NUMBER", ["title"] = "Call Concierge", ["phone"] = "+15559876543" } } } })
        });

        // 7. whatsapp/card
        samples.Add(new WhatsAppTemplate
        {
            WhatsAppAccountId = accountId,
            Name = "Order Confirmation",
            Language = "en",
            Category = "UTILITY",
            Status = "APPROVED",
            Body = "Your order #{{1}} has been confirmed! Total: ${{2}}. Estimated delivery: {{3}}.",
            Header = "Order Confirmed ✅",
            Footer = "Thank you for shopping with us",
            ContentType = "whatsapp/card",
            TypesJson = Types(new Dictionary<string, object> { ["whatsapp/card"] = new Dictionary<string, object> { ["body"] = "Your order #{{1}} has been confirmed! Total: ${{2}}. Estimated delivery: {{3}}.", ["header_text"] = "Order Confirmed ✅", ["footer"] = "Thank you for shopping with us", ["media"] = new[] { "https://picsum.photos/400/200" }, ["actions"] = new[] { new Dictionary<string, object> { ["type"] = "URL", ["title"] = "Track Order", ["url"] = "https://example.com/orders/{{4}}" }, new Dictionary<string, object> { ["type"] = "PHONE_NUMBER", ["title"] = "Support", ["phone"] = "+15551234567" } } } })
        });

        // 8. twilio/carousel
        samples.Add(new WhatsAppTemplate
        {
            WhatsAppAccountId = accountId,
            Name = "Product Carousel",
            Language = "en",
            Category = "MARKETING",
            Status = "PENDING",
            Body = "Check out our latest collection!",
            Header = "🛍️ New Arrivals",
            Footer = "Superchat Store",
            ContentType = "twilio/carousel",
            TypesJson = Types(new Dictionary<string, object> { ["twilio/carousel"] = new Dictionary<string, object> { ["body"] = "Check out our latest collection!", ["cards"] = new[] { new Dictionary<string, object> { ["title"] = "Classic Tee", ["body"] = "100% cotton, available in 5 colors", ["media"] = "https://picsum.photos/200/200?random=1", ["actions"] = new[] { new Dictionary<string, object> { ["type"] = "QUICK_REPLY", ["title"] = "Buy Now", ["id"] = "buy_tee" }, new Dictionary<string, object> { ["type"] = "URL", ["title"] = "View Details", ["url"] = "https://example.com/tee" } } }, new Dictionary<string, object> { ["title"] = "Denim Jacket", ["body"] = "Premium denim, limited edition", ["media"] = "https://picsum.photos/200/200?random=2", ["actions"] = new[] { new Dictionary<string, object> { ["type"] = "QUICK_REPLY", ["title"] = "Buy Now", ["id"] = "buy_jacket" }, new Dictionary<string, object> { ["type"] = "URL", ["title"] = "View Details", ["url"] = "https://example.com/jacket" } } }, new Dictionary<string, object> { ["title"] = "Running Shoes", ["body"] = "Lightweight, breathable, size 6-12", ["media"] = "https://picsum.photos/200/200?random=3", ["actions"] = new[] { new Dictionary<string, object> { ["type"] = "QUICK_REPLY", ["title"] = "Buy Now", ["id"] = "buy_shoes" }, new Dictionary<string, object> { ["type"] = "URL", ["title"] = "View Details", ["url"] = "https://example.com/shoes" } } } } } })
        });

        // 9. twilio/location
        samples.Add(new WhatsAppTemplate
        {
            WhatsAppAccountId = accountId,
            Name = "Store Location",
            Language = "en",
            Category = "UTILITY",
            Status = "PENDING",
            Body = "Visit our flagship store!",
            Header = "📍 Our Location",
            Footer = "Superchat Inc.",
            ContentType = "twilio/location",
            TypesJson = Types(new Dictionary<string, object> { ["twilio/location"] = new Dictionary<string, object> { ["latitude"] = 37.7749, ["longitude"] = -122.4194, ["label"] = "Superchat HQ - San Francisco" } })
        });

        // 10. baileys/buttons — native Baileys interactive buttons
        samples.Add(new WhatsAppTemplate
        {
            WhatsAppAccountId = accountId,
            Name = "Quick Actions",
            Language = "en",
            Category = "UTILITY",
            Status = "APPROVED",
            Body = "What would you like to do?",
            Header = "⚡ Quick Actions",
            Footer = "Superchat",
            ContentType = "baileys/buttons",
            TypesJson = Types(new Dictionary<string, object> { ["baileys/buttons"] = new Dictionary<string, object> { ["body"] = "What would you like to do?", ["buttons"] = new[] { new Dictionary<string, object> { ["title"] = "View Profile", ["id"] = "view_profile" }, new Dictionary<string, object> { ["title"] = "Send Feedback", ["id"] = "feedback" }, new Dictionary<string, object> { ["title"] = "Help Center", ["id"] = "help" } } } })
        });

        // 11. baileys/list — native Baileys interactive list
        samples.Add(new WhatsAppTemplate
        {
            WhatsAppAccountId = accountId,
            Name = "Account Menu",
            Language = "en",
            Category = "UTILITY",
            Status = "APPROVED",
            Body = "Select an option from the menu below:",
            Header = "📋 Account Menu",
            Footer = "Choose an option",
            ContentType = "baileys/list",
            TypesJson = Types(new Dictionary<string, object> { ["baileys/list"] = new Dictionary<string, object> { ["body"] = "Select an option from the menu below:", ["buttonText"] = "View Options", ["sections"] = new[] { new Dictionary<string, object> { ["title"] = "Account", ["rows"] = new[] { new Dictionary<string, object> { ["title"] = "My Orders", ["id"] = "my_orders", ["description"] = "View your order history" }, new Dictionary<string, object> { ["title"] = "Settings", ["id"] = "settings", ["description"] = "Manage preferences" } } }, new Dictionary<string, object> { ["title"] = "Support", ["rows"] = new[] { new Dictionary<string, object> { ["title"] = "Contact Us", ["id"] = "contact", ["description"] = "Talk to support" }, new Dictionary<string, object> { ["title"] = "FAQs", ["id"] = "faq", ["description"] = "Frequently asked questions" } } } } } })
        });

        // 12. baileys/template — native Baileys template buttons
        samples.Add(new WhatsAppTemplate
        {
            WhatsAppAccountId = accountId,
            Name = "Business Card",
            Language = "en",
            Category = "UTILITY",
            Status = "APPROVED",
            Body = "Connect with us! Visit our website or give us a call.",
            Header = "🏢 Superchat Business",
            Footer = "We're here 24/7",
            ContentType = "baileys/template",
            TypesJson = Types(new Dictionary<string, object> { ["baileys/template"] = new Dictionary<string, object> { ["body"] = "Connect with us! Visit our website or give us a call.", ["buttons"] = new[] { new Dictionary<string, object> { ["type"] = "URL", ["title"] = "Visit Website", ["url"] = "https://example.com" }, new Dictionary<string, object> { ["type"] = "PHONE_NUMBER", ["title"] = "Call Us", ["phone"] = "+15551234567" } } } })
        });

        // 13. baileys/sticker — native Baileys sticker
        samples.Add(new WhatsAppTemplate
        {
            WhatsAppAccountId = accountId,
            Name = "Welcome Sticker",
            Language = "en",
            Category = "UTILITY",
            Status = "APPROVED",
            Body = "Welcome to Superchat! 🎉",
            Header = "Welcome!",
            ContentType = "baileys/sticker",
            TypesJson = Types(new Dictionary<string, object> { ["baileys/sticker"] = new Dictionary<string, object> { ["body"] = "Welcome to Superchat! 🎉", ["url"] = "https://img.icons8.com/color/512/whatsapp.png" } })
        });

        foreach (var s in samples) _templateRepo.Add(s);
        await _uow.SaveChangesAsync();

        return Ok(new { count = samples.Count, message = $"Created {samples.Count} sample templates" });
    }

    [HttpPost("preview")]
    public ActionResult Preview([FromBody] PreviewRequest request)
    {
        var builder = TwilioTemplateBuilder.FromTypesJson(request.TypesJson);
        var fallback = builder.ComposeFallbackBody();
        return Ok(new { body = fallback, typesJson = request.TypesJson });
    }

    private static string InterpolateTemplate(string? text, Dictionary<string, string>? variables)
    {
        if (string.IsNullOrEmpty(text) || variables == null || variables.Count == 0) return text ?? "";
        foreach (var kv in variables)
        {
            text = text.Replace($"{{{{{kv.Key}}}}}", kv.Value);
        }
        return text;
    }

    private static string InterpolateJson(string json, Dictionary<string, string> variables)
    {
        foreach (var kv in variables)
        {
            json = json.Replace($"{{{{{kv.Key}}}}}", kv.Value);
        }
        return json;
    }
}

public class SendTemplateToConversationRequest
{
    public string RemoteJid { get; set; } = string.Empty;
    public string? ContactName { get; set; }
    public string? ContactPhone { get; set; }
    public Dictionary<string, string>? Variables { get; set; }
}

public class PreviewRequest
{
    public string? TypesJson { get; set; }
}
