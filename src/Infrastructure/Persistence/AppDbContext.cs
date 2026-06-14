using Domain.Entities;
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

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<Conversation>(entity =>
        {
            entity.HasKey(e => e.Id);
            entity.HasIndex(e => e.RemoteJid).IsUnique();
            entity.Property(e => e.RemoteJid).HasMaxLength(100).IsRequired();
            entity.Property(e => e.ContactName).HasMaxLength(200).IsRequired();
            entity.HasMany(e => e.Messages).WithOne(m => m.Conversation).HasForeignKey(m => m.ConversationId).OnDelete(DeleteBehavior.Cascade);
        });

        modelBuilder.Entity<Message>(entity =>
        {
            entity.HasKey(e => e.Id);
            entity.Property(e => e.Content).HasMaxLength(4096).IsRequired();
            entity.Property(e => e.MessageType).HasMaxLength(50);
            entity.Property(e => e.MediaUrl).HasMaxLength(1000);
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

        modelBuilder.Entity<AiConfig>().HasData(new AiConfig
        {
            Id = Guid.Parse("A1B2C3D4-E5F6-7890-ABCD-EF1234567890"),
            Name = "Default",
            SystemPrompt = "You are a helpful WhatsApp assistant. Respond concisely and naturally.",
            Provider = "claude",
            Model = "claude-sonnet-4-20250514",
            Temperature = 0.7,
            MaxTokens = 1024,
            IsActive = true,
            CreatedAt = new DateTime(2025, 1, 1, 0, 0, 0, DateTimeKind.Utc)
        });
    }
}
