namespace Application.Interfaces;

public interface IWhatsAppService
{
    Task SendMessageAsync(string remoteJid, string message);
    Task<string> GetQrCodeAsync();
    Task<bool> CheckConnectionAsync();
}
