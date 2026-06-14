using Application.Interfaces;
using Domain.Interfaces;
using Infrastructure.Persistence;
using Infrastructure.Persistence.Repositories;
using Infrastructure.Services;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace Infrastructure;

public static class DependencyInjection
{
    public static IServiceCollection AddInfrastructure(this IServiceCollection services, IConfiguration configuration)
    {
        services.AddDbContext<AppDbContext>(options =>
            options.UseSqlite(configuration.GetConnectionString("DefaultConnection")));

        services.AddScoped<IConversationRepository, ConversationRepository>();
        services.AddScoped<IMessageRepository, MessageRepository>();
        services.AddScoped<IAiConfigRepository, AiConfigRepository>();
        services.AddScoped<IContactRepository, ContactRepository>();
        services.AddScoped<ITagRepository, TagRepository>();
        services.AddScoped<ICampaignRepository, CampaignRepository>();
        services.AddScoped<IWhatsAppAccountRepository, WhatsAppAccountRepository>();
        services.AddScoped<IWhatsAppTemplateRepository, WhatsAppTemplateRepository>();
        services.AddScoped<IAgentRepository, AgentRepository>();
        services.AddScoped<IAgentGroupRepository, AgentGroupRepository>();
        services.AddScoped<IInternalNoteRepository, InternalNoteRepository>();
        services.AddScoped<IWorkflowRepository, WorkflowRepository>();
        services.AddScoped<IAnalyticsEventRepository, AnalyticsEventRepository>();
        services.AddScoped<IConversationMetricRepository, ConversationMetricRepository>();
        services.AddScoped<IAgentPerformanceRepository, AgentPerformanceRepository>();
        services.AddScoped<IChannelAccountRepository, ChannelAccountRepository>();
        services.AddScoped<IIntegrationRepository, IntegrationRepository>();
        services.AddScoped<IWebWidgetRepository, WebWidgetRepository>();
        services.AddScoped<IMessageReactionRepository, MessageReactionRepository>();
        services.AddScoped<IUnitOfWork>(sp => sp.GetRequiredService<AppDbContext>());

        services.Configure<BaileysOptions>(configuration.GetSection("BaileysService"));
        services.AddHttpClient<IWhatsAppService, WhatsAppService>();

        services.Configure<AiOptions>(configuration.GetSection("Ai"));
        services.AddHttpClient<IAiService, AiService>();

        services.AddScoped<IAnalyticsService, AnalyticsService>();
        services.AddScoped<IWorkflowEngine, WorkflowEngineService>();

        services.AddScoped<TelegramChannelService>();
        services.AddScoped<EmailChannelService>();
        services.AddHttpClient<FacebookChannelService>();
        services.AddScoped<IChannelService>(sp => sp.GetRequiredService<TelegramChannelService>());
        services.AddScoped<IChannelService>(sp => sp.GetRequiredService<EmailChannelService>());
        services.AddScoped<IChannelService>(sp => sp.GetRequiredService<FacebookChannelService>());
        services.AddScoped<IChannelServiceFactory, ChannelServiceFactory>();

        return services;
    }
}
