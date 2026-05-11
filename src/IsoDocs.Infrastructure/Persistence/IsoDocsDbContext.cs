using IsoDocs.Domain.Attachments;
using IsoDocs.Domain.Audit;
using IsoDocs.Domain.Cases;
using IsoDocs.Domain.Communications;
using IsoDocs.Domain.Customers;
using IsoDocs.Domain.Identity;
using IsoDocs.Domain.Workflows;
using Microsoft.EntityFrameworkCore;

namespace IsoDocs.Infrastructure.Persistence;

/// <summary>
/// IsoDocs 主資料庫上下文（SQL Server 2022 / Azure SQL Database）。
/// 所有資料表以 IEntityTypeConfiguration 在 Configurations/ 下集中管理。
/// </summary>
public class IsoDocsDbContext : DbContext
{
    public IsoDocsDbContext(DbContextOptions<IsoDocsDbContext> options) : base(options)
    {
    }

    // Identity
    public DbSet<User> Users => Set<User>();
    public DbSet<Role> Roles => Set<Role>();
    public DbSet<UserRole> UserRoles => Set<UserRole>();
    public DbSet<Delegation> Delegations => Set<Delegation>();

    // Workflows
    public DbSet<WorkflowTemplate> WorkflowTemplates => Set<WorkflowTemplate>();
    public DbSet<WorkflowNode> WorkflowNodes => Set<WorkflowNode>();
    public DbSet<FieldDefinition> FieldDefinitions => Set<FieldDefinition>();
    public DbSet<DocumentType> DocumentTypes => Set<DocumentType>();

    // Customers
    public DbSet<Customer> Customers => Set<Customer>();

    // Cases
    public DbSet<Case> Cases => Set<Case>();
    public DbSet<CaseField> CaseFields => Set<CaseField>();
    public DbSet<CaseRelation> CaseRelations => Set<CaseRelation>();
    public DbSet<CaseNode> CaseNodes => Set<CaseNode>();
    public DbSet<CaseAction> CaseActions => Set<CaseAction>();

    // Communications
    public DbSet<Comment> Comments => Set<Comment>();
    public DbSet<Notification> Notifications => Set<Notification>();

    // Attachments
    public DbSet<Attachment> Attachments => Set<Attachment>();

    // Audit
    public DbSet<AuditTrail> AuditTrails => Set<AuditTrail>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);
        // 自動插拔 IsoDocs.Infrastructure assembly 內的所有 IEntityTypeConfiguration<>
        modelBuilder.ApplyConfigurationsFromAssembly(typeof(IsoDocsDbContext).Assembly);
    }
}
