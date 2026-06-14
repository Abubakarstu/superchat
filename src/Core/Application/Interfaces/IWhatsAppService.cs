namespace Application.Interfaces;

public class SendMediaRequest
{
    public string RemoteJid { get; set; } = string.Empty;
    public string MediaUrl { get; set; } = string.Empty;
    public string MediaType { get; set; } = string.Empty;
    public string? Caption { get; set; }
    public string? FileName { get; set; }
    public string? MimeType { get; set; }
}

public class SendTemplateRequest
{
    public string RemoteJid { get; set; } = string.Empty;
    public string Body { get; set; } = string.Empty;
    public string? TemplateName { get; set; }
    public string? Language { get; set; }
    public string? Header { get; set; }
    public string? Footer { get; set; }
    public string? Buttons { get; set; }
    public string? ContentType { get; set; }
    public string? TypesJson { get; set; }
}

public class SendReactionRequest
{
    public string RemoteJid { get; set; } = string.Empty;
    public string MessageId { get; set; } = string.Empty;
    public string Emoji { get; set; } = string.Empty;
    public bool Remove { get; set; }
}

public class ReadReceiptsRequest
{
    public string RemoteJid { get; set; } = string.Empty;
    public List<string> MessageIds { get; set; } = new();
}

public class EditMessageRequest
{
    public string RemoteJid { get; set; } = string.Empty;
    public string MessageId { get; set; } = string.Empty;
    public string NewText { get; set; } = string.Empty;
}

public class DeleteMessageRequest
{
    public string RemoteJid { get; set; } = string.Empty;
    public string MessageId { get; set; } = string.Empty;
    public bool ForEveryone { get; set; }
}

public class SendContactRequest
{
    public string RemoteJid { get; set; } = string.Empty;
    public string ContactName { get; set; } = string.Empty;
    public string ContactPhone { get; set; } = string.Empty;
}

public class SendPollRequest
{
    public string RemoteJid { get; set; } = string.Empty;
    public string PollName { get; set; } = string.Empty;
    public List<string> Options { get; set; } = new();
    public int SelectableCount { get; set; } = 1;
}

public class SendStatusRequest
{
    public string? Text { get; set; }
    public string? MediaUrl { get; set; }
    public string? MediaType { get; set; }
}

public class GroupCreateRequest
{
    public string Subject { get; set; } = string.Empty;
    public List<string> Participants { get; set; } = new();
}

public class GroupParticipantsRequest
{
    public string GroupJid { get; set; } = string.Empty;
    public List<string> Participants { get; set; } = new();
    public string Action { get; set; } = "add";
}

public class GroupUpdateRequest
{
    public string GroupJid { get; set; } = string.Empty;
    public string? Subject { get; set; }
    public string? Description { get; set; }
    public string? Setting { get; set; }
    public int? EphemeralDuration { get; set; }
}

public class BlockContactRequest
{
    public string RemoteJid { get; set; } = string.Empty;
    public bool Block { get; set; }
}

public class UpdateProfileRequest
{
    public string? Name { get; set; }
    public string? Status { get; set; }
    public string? ProfilePictureUrl { get; set; }
}

public class GroupMetadataResult
{
    public string? Id { get; set; }
    public string? Subject { get; set; }
    public string? Desc { get; set; }
    public int Size { get; set; }
    public int? EphemeralDuration { get; set; }
    public List<GroupParticipantInfo> Participants { get; set; } = new();
}

public class GroupParticipantInfo
{
    public string? Jid { get; set; }
    public string? Admin { get; set; }
    public string? Name { get; set; }
}

public interface IWhatsAppService
{
    Task SendMessageAsync(string remoteJid, string message);
    Task SendMediaAsync(SendMediaRequest request);
    Task SendTemplateAsync(SendTemplateRequest request);
    Task<string> GetQrCodeAsync();
    Task<bool> CheckConnectionAsync();
    Task<byte[]?> GetProfilePictureAsync(string jid);
    Task SendReactionAsync(SendReactionRequest request);
    Task ReadReceiptsAsync(ReadReceiptsRequest request);
    Task EditMessageAsync(EditMessageRequest request);
    Task DeleteMessageAsync(DeleteMessageRequest request);
    Task SendContactAsync(SendContactRequest request);
    Task SendPollAsync(SendPollRequest request);
    Task SendStatusAsync(SendStatusRequest request);
    Task<GroupMetadataResult?> GetGroupMetadataAsync(string groupJid);
    Task CreateGroupAsync(GroupCreateRequest request);
    Task GroupParticipantsUpdateAsync(GroupParticipantsRequest request);
    Task GroupUpdateAsync(GroupUpdateRequest request);
    Task GroupLeaveAsync(string groupJid);
    Task BlockContactAsync(BlockContactRequest request);
    Task UpdateProfileAsync(UpdateProfileRequest request);
}
