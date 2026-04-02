using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Senda.Core.Entities;
using Senda.Core.Enums;

namespace Senda.Infrastructure.Persistence;

public class SendaDbContext : DbContext
{
    public SendaDbContext(DbContextOptions<SendaDbContext> options) : base(options) { }

    public DbSet<Tenant> Tenants => Set<Tenant>();
    public DbSet<KnowledgeDocument> KnowledgeDocuments => Set<KnowledgeDocument>();
    public DbSet<KnowledgeChunk> KnowledgeChunks => Set<KnowledgeChunk>();
    public DbSet<ChatSession> ChatSessions => Set<ChatSession>();
    public DbSet<ChatMessage> ChatMessages => Set<ChatMessage>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.HasPostgresExtension("vector");

        base.OnModelCreating(modelBuilder);

        ConfigureTenant(modelBuilder.Entity<Tenant>());
        ConfigureKnowledgeDocument(modelBuilder.Entity<KnowledgeDocument>());
        ConfigureKnowledgeChunk(modelBuilder.Entity<KnowledgeChunk>());
        ConfigureChatSession(modelBuilder.Entity<ChatSession>());
        ConfigureChatMessage(modelBuilder.Entity<ChatMessage>());
    }

    private static void ConfigureTenant(EntityTypeBuilder<Tenant> entity)
    {
        entity.ToTable("Tenants");
        entity.HasKey(e => e.Id);
        entity.Property(e => e.Id).HasColumnName("id");
        entity.Property(e => e.Name).HasColumnName("name").HasMaxLength(255).IsRequired();
        entity.Property(e => e.SystemPrompt).HasColumnName("system_prompt");
        entity.Property(e => e.IsActive).HasColumnName("is_active");
        entity.Property(e => e.CreatedAt).HasColumnName("created_at");
    }

    private static void ConfigureKnowledgeDocument(EntityTypeBuilder<KnowledgeDocument> entity)
    {
        entity.ToTable("KnowledgeDocuments");
        entity.HasKey(e => e.Id);
        entity.Property(e => e.Id).HasColumnName("id");
        entity.Property(e => e.TenantId).HasColumnName("tenant_id");
        entity.Property(e => e.FileName).HasColumnName("file_name").HasMaxLength(500).IsRequired();
        entity.Property(e => e.SourceType).HasColumnName("source_type").HasConversion<string>().HasMaxLength(20);
        entity.Property(e => e.Status).HasColumnName("status").HasConversion<string>().HasMaxLength(20);
        entity.Property(e => e.UploadedAt).HasColumnName("uploaded_at");

        entity.HasIndex(e => e.TenantId).HasDatabaseName("ix_knowledge_documents_tenant_id");
    }

    private static void ConfigureKnowledgeChunk(EntityTypeBuilder<KnowledgeChunk> entity)
    {
        entity.ToTable("KnowledgeChunks");
        entity.HasKey(e => e.Id);
        entity.Property(e => e.Id).HasColumnName("id");
        entity.Property(e => e.DocumentId).HasColumnName("document_id");
        entity.Property(e => e.TenantId).HasColumnName("tenant_id");
        entity.Property(e => e.Content).HasColumnName("content").HasColumnType("text").IsRequired();
        entity.Property(e => e.Embedding).HasColumnName("embedding").HasColumnType("vector(1536)");
        entity.Property(e => e.TokenCount).HasColumnName("token_count");

        entity.HasIndex(e => e.TenantId).HasDatabaseName("ix_knowledge_chunks_tenant_id");
        entity.HasIndex(e => e.DocumentId).HasDatabaseName("ix_knowledge_chunks_document_id");
    }

    private static void ConfigureChatSession(EntityTypeBuilder<ChatSession> entity)
    {
        entity.ToTable("ChatSessions");
        entity.HasKey(e => e.Id);
        entity.Property(e => e.Id).HasColumnName("id");
        entity.Property(e => e.TenantId).HasColumnName("tenant_id");
        entity.Property(e => e.CustomerIdentifier).HasColumnName("customer_identifier").HasMaxLength(255).IsRequired();
        entity.Property(e => e.StartedAt).HasColumnName("started_at");
        entity.Property(e => e.LastActivityAt).HasColumnName("last_activity_at");

        entity.HasIndex(e => e.TenantId).HasDatabaseName("ix_chat_sessions_tenant_id");
        entity.HasIndex(e => new { e.TenantId, e.CustomerIdentifier }).HasDatabaseName("ix_chat_sessions_tenant_customer");
    }

    private static void ConfigureChatMessage(EntityTypeBuilder<ChatMessage> entity)
    {
        entity.ToTable("ChatMessages");
        entity.HasKey(e => e.Id);
        entity.Property(e => e.Id).HasColumnName("id");
        entity.Property(e => e.SessionId).HasColumnName("session_id");
        entity.Property(e => e.Role).HasColumnName("role").HasConversion<string>().HasMaxLength(20);
        entity.Property(e => e.Content).HasColumnName("content").HasColumnType("text").IsRequired();
        entity.Property(e => e.CreatedAt).HasColumnName("created_at");

        entity.HasIndex(e => e.SessionId).HasDatabaseName("ix_chat_messages_session_id");
    }
}