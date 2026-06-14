using Domain.Entities;
using Domain.Entities.Analytics;
using Domain.Entities.Automation;
using Domain.Entities.Channels;
using Domain.Entities.Collaboration;
using Domain.Entities.Integrations;
using Domain.Entities.WebWidget;
using Domain.Entities.WhatsApp;
using Domain.Interfaces;
using Microsoft.EntityFrameworkCore;

namespace Infrastructure.Persistence;

public class AppDbContext : DbContext, IUnitOfWork
{
    public AppDbContext(DbContextOptions<AppDbContext> options) : base(options) { }

    public DbSet<Conversation> Conversations => Set<Conversation>();
    public DbSet<Message> Messages => Set<Message>();
    public DbSet<User> Users => Set<User>();
    public DbSet<AiConfig> AiConfigs => Set<AiConfig>();
    public DbSet<Contact> Contacts => Set<Contact>();
    public DbSet<Tag> Tags => Set<Tag>();
    public DbSet<ContactTag> ContactTags => Set<ContactTag>();
    public DbSet<Campaign> Campaigns => Set<Campaign>();
    public DbSet<CampaignRecipient> CampaignRecipients => Set<CampaignRecipient>();
    public DbSet<WhatsAppAccount> WhatsAppAccounts => Set<WhatsAppAccount>();
    public DbSet<WhatsAppTemplate> WhatsAppTemplates => Set<WhatsAppTemplate>();
    public DbSet<Agent> Agents => Set<Agent>();
    public DbSet<AgentGroup> AgentGroups => Set<AgentGroup>();
    public DbSet<ConversationAssignment> ConversationAssignments => Set<ConversationAssignment>();
    public DbSet<InternalNote> InternalNotes => Set<InternalNote>();
    public DbSet<Workflow> Workflows => Set<Workflow>();
    public DbSet<WorkflowTrigger> WorkflowTriggers => Set<WorkflowTrigger>();
    public DbSet<WorkflowAction> WorkflowActions => Set<WorkflowAction>();
    public DbSet<AnalyticsEvent> AnalyticsEvents => Set<AnalyticsEvent>();
    public DbSet<ConversationMetric> ConversationMetrics => Set<ConversationMetric>();
    public DbSet<AgentPerformance> AgentPerformances => Set<AgentPerformance>();
    public DbSet<ChannelAccount> ChannelAccounts => Set<ChannelAccount>();
    public DbSet<Integration> Integrations => Set<Integration>();
    public DbSet<WebWidget> WebWidgets => Set<WebWidget>();
    public DbSet<MessageReaction> MessageReactions => Set<MessageReaction>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<Conversation>(entity =>
        {
            entity.HasKey(e => e.Id);
            entity.HasIndex(e => e.RemoteJid);
            entity.Property(e => e.RemoteJid).HasMaxLength(100).IsRequired();
            entity.Property(e => e.ContactName).HasMaxLength(200).IsRequired();
            entity.Property(e => e.ChannelType).HasMaxLength(50).HasDefaultValue("whatsapp");
            entity.Property(e => e.Status).HasMaxLength(50).HasDefaultValue("open");
            entity.Property(e => e.Priority).HasMaxLength(20).HasDefaultValue("normal");
            entity.HasMany(e => e.Messages).WithOne(m => m.Conversation).HasForeignKey(m => m.ConversationId).OnDelete(DeleteBehavior.Cascade);
            entity.HasMany(e => e.Assignments).WithOne(a => a.Conversation).HasForeignKey(a => a.ConversationId).OnDelete(DeleteBehavior.Cascade);
            entity.HasMany(e => e.Notes).WithOne(n => n.Conversation).HasForeignKey(n => n.ConversationId).OnDelete(DeleteBehavior.Cascade);
            entity.HasOne(e => e.Contact).WithMany(c => c.Conversations).HasForeignKey(e => e.ContactId).OnDelete(DeleteBehavior.SetNull);
            entity.HasOne(e => e.ChannelAccount).WithMany(c => c.Conversations).HasForeignKey(e => e.ChannelAccountId).OnDelete(DeleteBehavior.SetNull);
        });

        modelBuilder.Entity<Message>(entity =>
        {
            entity.HasKey(e => e.Id);
            entity.Property(e => e.Content).HasMaxLength(4096).IsRequired();
            entity.Property(e => e.MessageType).HasMaxLength(50);
            entity.Property(e => e.MediaUrl).HasMaxLength(1000);
            entity.Property(e => e.FileName).HasMaxLength(500);
            entity.Property(e => e.MimeType).HasMaxLength(100);
            entity.HasMany(e => e.Reactions).WithOne(r => r.Message).HasForeignKey(r => r.MessageId).OnDelete(DeleteBehavior.Cascade);
            entity.HasOne(e => e.ReplyTo).WithMany(e => e.Replies).HasForeignKey(e => e.ReplyToId).OnDelete(DeleteBehavior.SetNull);
        });

        modelBuilder.Entity<MessageReaction>(entity =>
        {
            entity.HasKey(e => e.Id);
            entity.Property(e => e.Emoji).HasMaxLength(10).IsRequired();
            entity.Property(e => e.SenderJid).HasMaxLength(100);
            entity.Property(e => e.SenderName).HasMaxLength(200);
        });

        modelBuilder.Entity<User>(entity =>
        {
            entity.HasKey(e => e.Id);
            entity.Property(e => e.Name).HasMaxLength(200).IsRequired();
            entity.Property(e => e.PhoneNumber).HasMaxLength(50).IsRequired();
        });

        modelBuilder.Entity<AiConfig>(entity =>
        {
            entity.HasKey(e => e.Id);
            entity.Property(e => e.Name).HasMaxLength(100).IsRequired();
            entity.Property(e => e.SystemPrompt).HasMaxLength(4000).IsRequired();
            entity.Property(e => e.Provider).HasMaxLength(50).IsRequired();
            entity.Property(e => e.Model).HasMaxLength(100);
        });

        modelBuilder.Entity<Contact>(entity =>
        {
            entity.HasKey(e => e.Id);
            entity.HasIndex(e => e.Phone);
            entity.Property(e => e.Name).HasMaxLength(200).IsRequired();
            entity.Property(e => e.Phone).HasMaxLength(50);
            entity.Property(e => e.Email).HasMaxLength(200);
            entity.Property(e => e.Company).HasMaxLength(200);
            entity.Property(e => e.Source).HasMaxLength(50);
            entity.Property(e => e.LifecycleStage).HasMaxLength(50).HasDefaultValue("lead");
            entity.HasMany(e => e.ContactTags).WithOne(ct => ct.Contact).HasForeignKey(ct => ct.ContactId).OnDelete(DeleteBehavior.Cascade);
        });

        modelBuilder.Entity<Tag>(entity =>
        {
            entity.HasKey(e => e.Id);
            entity.Property(e => e.Name).HasMaxLength(100).IsRequired();
            entity.Property(e => e.Color).HasMaxLength(20).HasDefaultValue("#6c757d");
        });

        modelBuilder.Entity<ContactTag>(entity =>
        {
            entity.HasKey(ct => new { ct.ContactId, ct.TagId });
            entity.HasOne(ct => ct.Tag).WithMany(t => t.ContactTags).HasForeignKey(ct => ct.TagId);
        });

        modelBuilder.Entity<Campaign>(entity =>
        {
            entity.HasKey(e => e.Id);
            entity.Property(e => e.Name).HasMaxLength(200).IsRequired();
            entity.Property(e => e.Status).HasMaxLength(50).HasDefaultValue("DRAFT");
            entity.Property(e => e.ChannelType).HasMaxLength(50);
            entity.HasOne(e => e.Template).WithMany(t => t.Campaigns).HasForeignKey(e => e.TemplateId).OnDelete(DeleteBehavior.SetNull);
            entity.HasMany(e => e.Recipients).WithOne(r => r.Campaign).HasForeignKey(r => r.CampaignId).OnDelete(DeleteBehavior.Cascade);
        });

        modelBuilder.Entity<CampaignRecipient>(entity =>
        {
            entity.HasKey(e => e.Id);
            entity.Property(e => e.Status).HasMaxLength(50).HasDefaultValue("PENDING");
            entity.HasOne(e => e.Contact).WithMany(c => c.CampaignRecipients).HasForeignKey(e => e.ContactId).OnDelete(DeleteBehavior.Cascade);
        });

        modelBuilder.Entity<WhatsAppAccount>(entity =>
        {
            entity.HasKey(e => e.Id);
            entity.Property(e => e.PhoneNumberId).HasMaxLength(100).IsRequired();
            entity.Property(e => e.DisplayName).HasMaxLength(200);
        });

        modelBuilder.Entity<WhatsAppTemplate>(entity =>
        {
            entity.HasKey(e => e.Id);
            entity.Property(e => e.Name).HasMaxLength(100).IsRequired();
            entity.Property(e => e.Language).HasMaxLength(10).HasDefaultValue("en");
            entity.Property(e => e.Category).HasMaxLength(50);
            entity.Property(e => e.Status).HasMaxLength(50);
            entity.HasOne(e => e.Account).WithMany(a => a.Templates).HasForeignKey(e => e.WhatsAppAccountId).OnDelete(DeleteBehavior.Cascade);
        });

        modelBuilder.Entity<Agent>(entity =>
        {
            entity.HasKey(e => e.Id);
            entity.HasIndex(e => e.Email).IsUnique();
            entity.Property(e => e.Name).HasMaxLength(200).IsRequired();
            entity.Property(e => e.Email).HasMaxLength(200).IsRequired();
            entity.Property(e => e.Role).HasMaxLength(50).HasDefaultValue("agent");
            entity.Property(e => e.Status).HasMaxLength(50).HasDefaultValue("offline");
        });

        modelBuilder.Entity<AgentGroup>(entity =>
        {
            entity.HasKey(e => e.Id);
            entity.Property(e => e.Name).HasMaxLength(200).IsRequired();
            entity.HasMany(e => e.Agents).WithMany(a => a.AgentGroups);
        });

        modelBuilder.Entity<ConversationAssignment>(entity =>
        {
            entity.HasKey(e => e.Id);
            entity.Property(e => e.Type).HasMaxLength(50);
            entity.HasOne(e => e.Agent).WithMany(a => a.Assignments).HasForeignKey(e => e.AgentId).OnDelete(DeleteBehavior.Cascade);
        });

        modelBuilder.Entity<InternalNote>(entity =>
        {
            entity.HasKey(e => e.Id);
            entity.Property(e => e.Content).HasMaxLength(4000).IsRequired();
            entity.HasOne(e => e.Agent).WithMany(a => a.Notes).HasForeignKey(e => e.AgentId).OnDelete(DeleteBehavior.Cascade);
        });

        modelBuilder.Entity<Workflow>(entity =>
        {
            entity.HasKey(e => e.Id);
            entity.Property(e => e.Name).HasMaxLength(200).IsRequired();
            entity.HasMany(e => e.Triggers).WithOne(t => t.Workflow).HasForeignKey(t => t.WorkflowId).OnDelete(DeleteBehavior.Cascade);
            entity.HasMany(e => e.Actions).WithOne(a => a.Workflow).HasForeignKey(a => a.WorkflowId).OnDelete(DeleteBehavior.Cascade);
        });

        modelBuilder.Entity<WorkflowTrigger>(entity =>
        {
            entity.HasKey(e => e.Id);
            entity.Property(e => e.EventType).HasMaxLength(100).IsRequired();
        });

        modelBuilder.Entity<WorkflowAction>(entity =>
        {
            entity.HasKey(e => e.Id);
            entity.Property(e => e.ActionType).HasMaxLength(100).IsRequired();
            entity.Property(e => e.Configuration).HasMaxLength(4000);
        });

        modelBuilder.Entity<AnalyticsEvent>(entity =>
        {
            entity.HasKey(e => e.Id);
            entity.Property(e => e.EventType).HasMaxLength(100).IsRequired();
            entity.HasIndex(e => new { e.EventType, e.Timestamp });
        });

        modelBuilder.Entity<ConversationMetric>(entity => { entity.HasKey(e => e.Id); });
        modelBuilder.Entity<AgentPerformance>(entity => { entity.HasKey(e => e.Id); });

        modelBuilder.Entity<ChannelAccount>(entity =>
        {
            entity.HasKey(e => e.Id);
            entity.Property(e => e.ChannelType).HasMaxLength(50).IsRequired();
            entity.Property(e => e.Name).HasMaxLength(200).IsRequired();
        });

        modelBuilder.Entity<Integration>(entity =>
        {
            entity.HasKey(e => e.Id);
            entity.Property(e => e.Provider).HasMaxLength(100).IsRequired();
            entity.Property(e => e.Type).HasMaxLength(100);
        });

        modelBuilder.Entity<WebWidget>(entity =>
        {
            entity.HasKey(e => e.Id);
            entity.Property(e => e.Name).HasMaxLength(200);
            entity.Property(e => e.PrimaryColor).HasMaxLength(20);
            entity.Property(e => e.Position).HasMaxLength(20);
        });

    }
}
