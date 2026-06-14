using System.Text.Json.Serialization;
using System.Text.Json;

namespace Application.Models;

// ─── twilio/text ───
public class TwilioTextContent
{
    [JsonPropertyName("body")] public string Body { get; set; } = "";
}

// ─── twilio/media ───
public class TwilioMediaContent
{
    [JsonPropertyName("body")] public string? Body { get; set; }
    [JsonPropertyName("media")] public List<string> Media { get; set; } = new();
}

// ─── twilio/location ───
public class TwilioLocationContent
{
    [JsonPropertyName("longitude")] public double Longitude { get; set; }
    [JsonPropertyName("latitude")] public double Latitude { get; set; }
    [JsonPropertyName("label")] public string? Label { get; set; }
}

// ─── twilio/quick-reply ───
public class QuickReplyAction
{
    [JsonPropertyName("type")] public string Type { get; set; } = "QUICK_REPLY";
    [JsonPropertyName("title")] public string Title { get; set; } = "";
    [JsonPropertyName("id")] public string? Id { get; set; }
}

public class TwilioQuickReplyContent
{
    [JsonPropertyName("body")] public string Body { get; set; } = "";
    [JsonPropertyName("actions")] public List<QuickReplyAction> Actions { get; set; } = new();
}

// ─── twilio/call-to-action ───
public class CallToActionAction
{
    [JsonPropertyName("type")] public string Type { get; set; } = "URL";
    [JsonPropertyName("title")] public string Title { get; set; } = "";
    [JsonPropertyName("url")] public string? Url { get; set; }
    [JsonPropertyName("phone")] public string? Phone { get; set; }
}

public class TwilioCallToActionContent
{
    [JsonPropertyName("body")] public string Body { get; set; } = "";
    [JsonPropertyName("actions")] public List<CallToActionAction> Actions { get; set; } = new();
}

// ─── twilio/list-picker ───
public class ListPickerItem
{
    [JsonPropertyName("item")] public string Item { get; set; } = "";
    [JsonPropertyName("id")] public string Id { get; set; } = "";
    [JsonPropertyName("description")] public string Description { get; set; } = "";
}

public class TwilioListPickerContent
{
    [JsonPropertyName("body")] public string Body { get; set; } = "";
    [JsonPropertyName("button")] public string Button { get; set; } = "";
    [JsonPropertyName("items")] public List<ListPickerItem> Items { get; set; } = new();
}

// ─── twilio/card ───
public class CardAction
{
    [JsonPropertyName("type")] public string Type { get; set; } = "URL";
    [JsonPropertyName("title")] public string Title { get; set; } = "";
    [JsonPropertyName("url")] public string? Url { get; set; }
    [JsonPropertyName("phone")] public string? Phone { get; set; }
    [JsonPropertyName("id")] public string? Id { get; set; }
}

public class TwilioCardContent
{
    [JsonPropertyName("title")] public string? Title { get; set; }
    [JsonPropertyName("body")] public string? Body { get; set; }
    [JsonPropertyName("subtitle")] public string? Subtitle { get; set; }
    [JsonPropertyName("media")] public List<string>? Media { get; set; }
    [JsonPropertyName("actions")] public List<CardAction>? Actions { get; set; }
}

// ─── whatsapp/card ───
public class WhatsAppCardAction
{
    [JsonPropertyName("type")] public string Type { get; set; } = "URL";
    [JsonPropertyName("title")] public string Title { get; set; } = "";
    [JsonPropertyName("url")] public string? Url { get; set; }
    [JsonPropertyName("phone")] public string? Phone { get; set; }
}

public class WhatsAppCardContent
{
    [JsonPropertyName("body")] public string Body { get; set; } = "";
    [JsonPropertyName("footer")] public string? Footer { get; set; }
    [JsonPropertyName("header_text")] public string? HeaderText { get; set; }
    [JsonPropertyName("media")] public List<string>? Media { get; set; }
    [JsonPropertyName("actions")] public List<WhatsAppCardAction> Actions { get; set; } = new();
}

// ─── twilio/carousel ───
public class CarouselCardAction
{
    [JsonPropertyName("type")] public string Type { get; set; } = "QUICK_REPLY";
    [JsonPropertyName("title")] public string Title { get; set; } = "";
    [JsonPropertyName("url")] public string? Url { get; set; }
    [JsonPropertyName("id")] public string? Id { get; set; }
}

public class CarouselCard
{
    [JsonPropertyName("title")] public string? Title { get; set; }
    [JsonPropertyName("body")] public string Body { get; set; } = "";
    [JsonPropertyName("media")] public string Media { get; set; } = "";
    [JsonPropertyName("actions")] public List<CarouselCardAction> Actions { get; set; } = new();
}

public class TwilioCarouselContent
{
    [JsonPropertyName("body")] public string Body { get; set; } = "";
    [JsonPropertyName("cards")] public List<CarouselCard> Cards { get; set; } = new();
}

// ─── Types wrapper (mirrors Twilio's types property) ───
public class TwilioTypesWrapper
{
    [JsonPropertyName("twilio/text")] public TwilioTextContent? TwilioText { get; set; }
    [JsonPropertyName("twilio/media")] public TwilioMediaContent? TwilioMedia { get; set; }
    [JsonPropertyName("twilio/location")] public TwilioLocationContent? TwilioLocation { get; set; }
    [JsonPropertyName("twilio/quick-reply")] public TwilioQuickReplyContent? TwilioQuickReply { get; set; }
    [JsonPropertyName("twilio/call-to-action")] public TwilioCallToActionContent? TwilioCallToAction { get; set; }
    [JsonPropertyName("twilio/list-picker")] public TwilioListPickerContent? TwilioListPicker { get; set; }
    [JsonPropertyName("twilio/card")] public TwilioCardContent? TwilioCard { get; set; }
    [JsonPropertyName("whatsapp/card")] public WhatsAppCardContent? WhatsAppCard { get; set; }
    [JsonPropertyName("twilio/carousel")] public TwilioCarouselContent? TwilioCarousel { get; set; }
}

// ─── Builder (used in the frontend to assemble the JSON) ───
public class TwilioTemplateBuilder
{
    public string ContentType { get; set; } = "twilio/text";
    public Dictionary<string, string> Variables { get; set; } = new();
    public TwilioTypesWrapper Types { get; set; } = new();

    public string ToTypesJson() =>
        JsonSerializer.Serialize(Types, new JsonSerializerOptions { DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull });

    public static TwilioTemplateBuilder FromTypesJson(string? json)
    {
        var builder = new TwilioTemplateBuilder();
        if (!string.IsNullOrEmpty(json))
        {
            try
            {
                var types = JsonSerializer.Deserialize<TwilioTypesWrapper>(json, new JsonSerializerOptions { PropertyNameCaseInsensitive = true });
                if (types != null) builder.Types = types;
            }
            catch { }
        }
        return builder;
    }

    public string ComposeFallbackBody()
    {
        if (Types.TwilioText?.Body != null) return Types.TwilioText.Body;
        if (Types.TwilioQuickReply?.Body != null) return Types.TwilioQuickReply.Body;
        if (Types.TwilioCallToAction?.Body != null) return Types.TwilioCallToAction.Body;
        if (Types.TwilioListPicker?.Body != null) return Types.TwilioListPicker.Body;
        if (Types.TwilioCard?.Body != null) return Types.TwilioCard.Body ?? Types.TwilioCard.Title ?? "";
        if (Types.WhatsAppCard?.Body != null) return Types.WhatsAppCard.Body;
        if (Types.TwilioCarousel?.Body != null) return Types.TwilioCarousel.Body;
        if (Types.TwilioMedia?.Body != null) return Types.TwilioMedia.Body ?? "";
        return "";
    }
}
