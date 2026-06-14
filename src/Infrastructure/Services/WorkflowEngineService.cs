using Application.Interfaces;
using Domain.Entities.Collaboration;
using Domain.Entities;
using Domain.Interfaces;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using System.Text.Json;

namespace Infrastructure.Services;

public class WorkflowEngineService : IWorkflowEngine
{
    private readonly IServiceProvider _sp;
    private readonly ILogger<WorkflowEngineService> _logger;

    public WorkflowEngineService(IServiceProvider sp, ILogger<WorkflowEngineService> logger)
    {
        _sp = sp;
        _logger = logger;
    }

    public async Task ExecuteAsync(string eventType, Guid? entityId = null, string? metadata = null)
    {
        try
        {
            using var scope = _sp.CreateScope();
            var workflowRepo = scope.ServiceProvider.GetRequiredService<IWorkflowRepository>();
            var uow = scope.ServiceProvider.GetRequiredService<IUnitOfWork>();
            var analytics = scope.ServiceProvider.GetRequiredService<IAnalyticsService>();

            await analytics.TrackEventAsync(eventType, entityId?.GetType().Name, entityId, metadata: metadata);

            var workflows = await workflowRepo.GetActiveByTriggerAsync(eventType);
            foreach (var workflow in workflows)
            {
                foreach (var action in workflow.Actions.OrderBy(a => a.Order))
                {
                    if (action.DelayMinutes > 0)
                        await Task.Delay(TimeSpan.FromMinutes(action.DelayMinutes.Value));

                    await ExecuteAction(action.ActionType, action.Configuration, entityId, scope);
                }
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Workflow execution failed for event {EventType}", eventType);
        }
    }

    private async Task ExecuteAction(string actionType, string config, Guid? entityId, IServiceScope scope)
    {
        switch (actionType)
        {
            case "assign_agent":
                var assignConfig = JsonSerializer.Deserialize<AssignConfig>(config);
                if (assignConfig != null && entityId.HasValue)
                {
                    var agentRepo = scope.ServiceProvider.GetRequiredService<IAgentRepository>();
                    var convRepo = scope.ServiceProvider.GetRequiredService<IConversationRepository>();
                    var uow = scope.ServiceProvider.GetRequiredService<IUnitOfWork>();

                    var agent = await agentRepo.GetByIdAsync(assignConfig.AgentId);
                    var conv = await convRepo.GetByIdAsync(entityId.Value);
                    if (agent != null && conv != null)
                    {
                        conv.Assignments.Add(new ConversationAssignment
                        {
                            ConversationId = conv.Id,
                            AgentId = agent.Id,
                            Type = "auto_assigned"
                        });
                        convRepo.Update(conv);
                        await uow.SaveChangesAsync();
                    }
                }
                break;

            case "add_tag":
                var tagConfig = JsonSerializer.Deserialize<TagConfig>(config);
                if (tagConfig != null && entityId.HasValue)
                {
                    var contactRepo = scope.ServiceProvider.GetRequiredService<IContactRepository>();
                    var tagRepo = scope.ServiceProvider.GetRequiredService<ITagRepository>();
                    var uow = scope.ServiceProvider.GetRequiredService<IUnitOfWork>();

                    var contact = await contactRepo.GetByIdAsync(entityId.Value);
                    var tags = await tagRepo.GetAllAsync();
                    var tag = tags.FirstOrDefault(t => t.Name == tagConfig.TagName);
                    if (tag == null)
                    {
                        tag = new Tag { Name = tagConfig.TagName };
                        tagRepo.Add(tag);
                    }
                    if (contact != null && !contact.ContactTags.Any(ct => ct.TagId == tag.Id))
                    {
                        contact.ContactTags.Add(new ContactTag { ContactId = contact.Id, TagId = tag.Id });
                        contactRepo.Update(contact);
                        await uow.SaveChangesAsync();
                    }
                }
                break;
        }
    }
}

public class AssignConfig { public Guid AgentId { get; set; } }
public class TagConfig { public string TagName { get; set; } = string.Empty; }
