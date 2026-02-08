using CSSistemas.Domain.Entities;
using CSSistemas.Domain.Enums;
using Microsoft.EntityFrameworkCore;

namespace CSSistemas.Infrastructure.Data;

public class AppDbContext : DbContext
{
    public AppDbContext(DbContextOptions<AppDbContext> options) : base(options) { }

    public DbSet<User> Users => Set<User>();
    public DbSet<Business> Businesses => Set<Business>();
    public DbSet<Service> Services => Set<Service>();
    public DbSet<Appointment> Appointments => Set<Appointment>();
    public DbSet<Client> Clients => Set<Client>();
    public DbSet<SystemMessage> SystemMessages => Set<SystemMessage>();
    public DbSet<BusinessHours> BusinessHours => Set<BusinessHours>();
    public DbSet<Subscription> Subscriptions => Set<Subscription>();
    public DbSet<Plan> Plans => Set<Plan>();
    public DbSet<Notification> Notifications => Set<Notification>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        // Soft delete: todas as consultas excluem IsDeleted = true por padrão
        modelBuilder.Entity<User>(entity =>
        {
            entity.ToTable("Users");
            entity.HasKey(e => e.Id);
            entity.Property(e => e.Email).HasMaxLength(256).IsRequired();
            entity.Property(e => e.PasswordHash).HasMaxLength(500).IsRequired();
            entity.Property(e => e.Name).HasMaxLength(200).IsRequired();
            entity.Property(e => e.ProfilePhotoUrl).HasMaxLength(500);
            entity.Property(e => e.DocumentType);
            entity.Property(e => e.DocumentNumber).HasMaxLength(20);
            entity.Property(e => e.ResetToken).HasMaxLength(500);
            entity.Property(e => e.ResetTokenExpiresAt);
            entity.Property(e => e.IsAdmin);
            entity.Property(e => e.FailedLoginAttempts);
            entity.Property(e => e.LockoutEnd);
            entity.HasQueryFilter(e => !e.IsDeleted);
            entity.HasIndex(e => e.Email).IsUnique().HasFilter("\"IsDeleted\" = false");
            entity.HasIndex(e => new { e.DocumentType, e.DocumentNumber }).IsUnique().HasFilter("\"DocumentNumber\" IS NOT NULL AND \"IsDeleted\" = false");
        });

        modelBuilder.Entity<Business>(entity =>
        {
            entity.ToTable("Businesses");
            entity.HasKey(e => e.Id);
            entity.Property(e => e.Name).HasMaxLength(200).IsRequired();
            entity.Property(e => e.PublicSlug).HasMaxLength(100);
            entity.Property(e => e.WhatsAppPhone).HasMaxLength(20);
            entity.HasQueryFilter(e => !e.IsDeleted);
            entity.HasIndex(e => e.PublicSlug).IsUnique().HasFilter("\"PublicSlug\" IS NOT NULL AND \"IsDeleted\" = false");
            entity.HasOne(e => e.User).WithMany().HasForeignKey(e => e.UserId).OnDelete(DeleteBehavior.Restrict);
        });

        modelBuilder.Entity<Service>(entity =>
        {
            entity.ToTable("Services");
            entity.HasKey(e => e.Id);
            entity.Property(e => e.Name).HasMaxLength(200).IsRequired();
            entity.Property(e => e.Price).HasPrecision(18, 2);
            entity.HasQueryFilter(e => !e.IsDeleted);
            entity.HasOne(e => e.Business).WithMany(b => b.Services).HasForeignKey(e => e.BusinessId).OnDelete(DeleteBehavior.Restrict);
        });

        modelBuilder.Entity<Appointment>(entity =>
        {
            entity.ToTable("Appointments");
            entity.HasKey(e => e.Id);
            entity.Property(e => e.ClientName).HasMaxLength(200).IsRequired();
            entity.Property(e => e.ClientPhone).HasMaxLength(20);
            entity.Property(e => e.ClientEmail).HasMaxLength(256);
            entity.Property(e => e.Notes).HasMaxLength(1000);
            entity.Property(e => e.CancelToken).HasMaxLength(64);
            entity.HasQueryFilter(e => !e.IsDeleted);
            entity.HasOne(e => e.Business).WithMany(b => b.Appointments).HasForeignKey(e => e.BusinessId).OnDelete(DeleteBehavior.Restrict);
            entity.HasOne(e => e.Service).WithMany(s => s.Appointments).HasForeignKey(e => e.ServiceId).OnDelete(DeleteBehavior.Restrict);
        });

        modelBuilder.Entity<Client>(entity =>
        {
            entity.ToTable("Clients");
            entity.HasKey(e => e.Id);
            entity.Property(e => e.Name).HasMaxLength(200).IsRequired();
            entity.Property(e => e.Phone).HasMaxLength(20);
            entity.Property(e => e.Email).HasMaxLength(256);
            entity.Property(e => e.Notes).HasMaxLength(1000);
            entity.HasQueryFilter(e => !e.IsDeleted);
            entity.HasOne(e => e.Business).WithMany(b => b.Clients).HasForeignKey(e => e.BusinessId).OnDelete(DeleteBehavior.Restrict);
        });

        modelBuilder.Entity<SystemMessage>(entity =>
        {
            entity.ToTable("SystemMessages");
            entity.HasKey(e => e.Id);
            entity.Property(e => e.Key).HasMaxLength(100).IsRequired();
            entity.Property(e => e.Title).HasMaxLength(200).IsRequired();
            entity.Property(e => e.Body).HasMaxLength(4000);
            entity.HasQueryFilter(e => !e.IsDeleted);
            entity.HasOne(e => e.Business).WithMany(b => b.SystemMessages).HasForeignKey(e => e.BusinessId).OnDelete(DeleteBehavior.Restrict);
        });

        modelBuilder.Entity<BusinessHours>(entity =>
        {
            entity.ToTable("BusinessHours");
            entity.HasKey(e => e.Id);
            entity.Property(e => e.DayOfWeek);
            entity.Property(e => e.OpenAtMinutes);
            entity.Property(e => e.CloseAtMinutes);
            entity.HasQueryFilter(e => !e.IsDeleted);
            entity.HasOne(e => e.Business).WithMany(b => b.BusinessHours).HasForeignKey(e => e.BusinessId).OnDelete(DeleteBehavior.Restrict);
        });

        modelBuilder.Entity<Subscription>(entity =>
        {
            entity.ToTable("Subscriptions");
            entity.HasKey(e => e.Id);
            entity.Property(e => e.SubscriptionType);
            entity.Property(e => e.StartedAt);
            entity.Property(e => e.EndsAt);
            entity.HasQueryFilter(e => !e.IsDeleted);
            entity.HasOne(e => e.User).WithMany().HasForeignKey(e => e.UserId).OnDelete(DeleteBehavior.Restrict);
        });

        modelBuilder.Entity<Plan>(entity =>
        {
            entity.ToTable("Plans");
            entity.HasKey(e => e.Id);
            entity.Property(e => e.Name).HasMaxLength(100).IsRequired();
            entity.Property(e => e.Price).HasPrecision(18, 2);
            entity.Property(e => e.BillingIntervalMonths);
            entity.Property(e => e.Features).HasMaxLength(500);
            entity.Property(e => e.IsActive);
            entity.Property(e => e.CreatedAt);
        });

        modelBuilder.Entity<Notification>(entity =>
        {
            entity.ToTable("Notifications");
            entity.HasKey(e => e.Id);
            entity.Property(e => e.Type).HasMaxLength(50).IsRequired();
            entity.Property(e => e.ClientName).HasMaxLength(200).IsRequired();
            entity.Property(e => e.ScheduledAt);
            entity.Property(e => e.AppointmentId);
            entity.Property(e => e.ReadAt);
            entity.HasQueryFilter(e => !e.IsDeleted);
            entity.HasOne(e => e.User).WithMany().HasForeignKey(e => e.UserId).OnDelete(DeleteBehavior.Restrict);
        });
    }
}
