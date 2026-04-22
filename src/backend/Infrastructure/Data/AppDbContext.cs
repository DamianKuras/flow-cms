using Domain;
using Domain.ContentItems;
using Domain.Fields;
using Domain.Users;
using Infrastructure.Fields.Entities;
using Infrastructure.Persistence.Permissions;
using Infrastructure.Users;
using Microsoft.AspNetCore.Identity.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore;
using ContentType = Domain.ContentTypes.ContentType;

namespace Infrastructure.Data;

/// <summary>
/// EF Core database context. Extends <see cref="IdentityDbContext{TUser,TRole,TKey}"/> to
/// include CMS entities alongside ASP.NET Core Identity tables.
/// </summary>
public class AppDbContext(DbContextOptions<AppDbContext> options)
    : IdentityDbContext<AppUser, AppRole, Guid>(options)
{
    /// <summary>Content items stored in the CMS.</summary>
    public DbSet<ContentItem> ContentItems { get; set; }

    /// <summary>Content type definitions that describe the schema of content items.</summary>
    public DbSet<ContentType> ContentTypes { get; set; }

    /// <summary>Field definitions belonging to content types.</summary>
    public DbSet<Field> Fields { get; set; }

    /// <summary>Validation rule entries attached to fields.</summary>
    public DbSet<ValidationRuleEntity> ValidationRulesEntries { get; set; }

    /// <summary>Parameters for validation rules.</summary>
    public DbSet<ValidationRuleParameterEntity> ValidationRuleParameters { get; set; }

    /// <summary>Transformation rule entries attached to fields.</summary>
    public DbSet<TransformationRuleEntity> TransformationRuleEntities { get; set; }

    /// <summary>Parameters for transformation rules.</summary>
    public DbSet<TransformationRuleParameterEntity> TransformationRuleParameterEntities { get; set; }

    /// <summary>Domain user projections, kept in sync with Identity users.</summary>
    public DbSet<User> DomainUsers { get; set; }

    /// <summary>Refresh tokens issued during authentication.</summary>
    public DbSet<RefreshToken> RefreshTokens { get; set; }

    /// <summary>Schema migration jobs between published content type versions.</summary>
    public DbSet<MigrationJob> MigrationJobs { get; set; }

    /// <summary>Permission rules assigned to roles.</summary>
    public DbSet<RolePermissionEntity> RolePermissions => Set<RolePermissionEntity>();

    /// <inheritdoc/>
    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);
        modelBuilder.ApplyConfigurationsFromAssembly(typeof(AppDbContext).Assembly);
    }
}
