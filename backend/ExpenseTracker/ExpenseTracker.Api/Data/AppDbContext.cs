using ExpenseTracker.Api.Data.Entities;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Identity.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore;

namespace ExpenseTracker.Api.Data
{
    public class AppDbContext : IdentityDbContext<User, IdentityRole<Guid>, Guid>
    {
        public AppDbContext(DbContextOptions<AppDbContext> options)
            : base(options)
        {
        }

        public DbSet<Category> Categories { get; set; } = default!;
        public DbSet<Transaction> Transactions { get; set; } = default!;

        // ✅ NEW
        public DbSet<Workspace> Workspaces { get; set; } = default!;
        public DbSet<ExpenseTracker.Api.Data.Entities.WorkspaceMember> WorkspaceMembers { get; set; } = default!;
        public DbSet<WorkspaceInvite> WorkspaceInvites { get; set; } = default!;

        protected override void OnModelCreating(ModelBuilder builder)
        {
            base.OnModelCreating(builder);

            builder.Entity<WorkspaceInvite>()
                .HasIndex(i => i.Token)
                .IsUnique();

            builder.Entity<WorkspaceMember>()
                .HasKey(m => m.Id);

            builder.Entity<WorkspaceMember>()
                .HasIndex(m => new { m.WorkspaceId, m.UserId })
                .IsUnique();

            builder.Entity<WorkspaceMember>()
                .HasOne(m => m.Workspace)
                .WithMany(w => w.Members)
                .HasForeignKey(m => m.WorkspaceId);

            builder.Entity<WorkspaceMember>()
                .HasOne(m => m.User)
                .WithMany()
                .HasForeignKey(m => m.UserId);

            builder.Entity<WorkspaceInvite>()
                .HasOne(i => i.Workspace)
                .WithMany()
                .HasForeignKey(i => i.WorkspaceId);

            builder.Entity<WorkspaceInvite>()
                .HasOne(i => i.InvitedByUser)
                .WithMany()
                .HasForeignKey(i => i.InvitedByUserId);

            builder.Entity<Transaction>()
                .HasOne(t => t.Workspace)
                .WithMany()
                .HasForeignKey(t => t.WorkspaceId);

            builder.Entity<Transaction>()
                .HasOne(t => t.CreatedByUser)
                .WithMany()
                .HasForeignKey(t => t.CreatedByUserId);

            builder.Entity<Category>()
                .HasOne(c => c.Workspace)
                .WithMany()
                .HasForeignKey(c => c.WorkspaceId);

            builder.Entity<Category>()
                .HasOne(c => c.CreatedByUser)
                .WithMany()
                .HasForeignKey(c => c.CreatedByUserId);
        }
    }
}