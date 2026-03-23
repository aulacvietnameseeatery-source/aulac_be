using System;
using System.Collections.Generic;
using Core.Entity;
using Microsoft.EntityFrameworkCore;
using Pomelo.EntityFrameworkCore.MySql.Scaffolding.Internal;

namespace Infa.Data;

public partial class RestaurantMgmtContext : DbContext
{
    public RestaurantMgmtContext()
    {
    }

    public RestaurantMgmtContext(DbContextOptions<RestaurantMgmtContext> options)
        : base(options)
    {
    }

    public virtual DbSet<AuditLog> AuditLogs { get; set; }

    public virtual DbSet<AuthSession> AuthSessions { get; set; }

    public virtual DbSet<CurrentStock> CurrentStocks { get; set; }

    public virtual DbSet<Customer> Customers { get; set; }

    public virtual DbSet<Dish> Dishes { get; set; }

    public virtual DbSet<DishCategory> DishCategories { get; set; }

    public virtual DbSet<DishMedium> DishMedia { get; set; }

    public virtual DbSet<DishTag> DishTags { get; set; }

    public virtual DbSet<I18nLanguage> I18nLanguages { get; set; }

    public virtual DbSet<I18nText> I18nTexts { get; set; }

    public virtual DbSet<I18nTranslation> I18nTranslations { get; set; }

    public virtual DbSet<Ingredient> Ingredients { get; set; }

    public virtual DbSet<IngredientSupplier> IngredientSuppliers { get; set; }

    public virtual DbSet<InventoryTransaction> InventoryTransactions { get; set; }

    public virtual DbSet<InventoryTransactionItem> InventoryTransactionItems { get; set; }

    public virtual DbSet<InventoryTransactionMedium> InventoryTransactionMedia { get; set; }

    public virtual DbSet<LookupType> LookupTypes { get; set; }

    public virtual DbSet<LookupValue> LookupValues { get; set; }

    public virtual DbSet<MediaAsset> MediaAssets { get; set; }

    public virtual DbSet<Order> Orders { get; set; }

    public virtual DbSet<OrderCoupon> OrderCoupons { get; set; }

    public virtual DbSet<OrderItem> OrderItems { get; set; }

    public virtual DbSet<OrderPromotion> OrderPromotions { get; set; }

    public virtual DbSet<Payment> Payments { get; set; }

    public virtual DbSet<Permission> Permissions { get; set; }

    public virtual DbSet<Coupon> Coupons { get; set; }

    public virtual DbSet<Promotion> Promotions { get; set; }

    public virtual DbSet<PromotionRule> PromotionRules { get; set; }

    public virtual DbSet<PromotionTarget> PromotionTargets { get; set; }

    public virtual DbSet<Recipe> Recipes { get; set; }

    public virtual DbSet<Reservation> Reservations { get; set; }

    public virtual DbSet<RestaurantTable> RestaurantTables { get; set; }

    public virtual DbSet<Role> Roles { get; set; }

    public virtual DbSet<ServiceError> ServiceErrors { get; set; }

    public virtual DbSet<ServiceErrorCategory> ServiceErrorCategories { get; set; }

    public virtual DbSet<StaffAccount> StaffAccounts { get; set; }

    public virtual DbSet<Supplier> Suppliers { get; set; }

    public virtual DbSet<SystemSetting> SystemSettings { get; set; }

    public virtual DbSet<TableMedium> TableMedia { get; set; }

    public virtual DbSet<ShiftTemplate> ShiftTemplates { get; set; }

    public virtual DbSet<ShiftAssignment> ShiftAssignments { get; set; }

    public virtual DbSet<AttendanceRecord> AttendanceRecords { get; set; }

    public virtual DbSet<TimeLog> TimeLogs { get; set; }

    public virtual DbSet<LoginActivity> LoginActivities { get; set; }
    public virtual DbSet<EmailTemplate> EmailTemplates { get; set; }

    public virtual DbSet<Notification> Notifications { get; set; }

    public virtual DbSet<NotificationReadState> NotificationReadStates { get; set; }

    public virtual DbSet<NotificationPreference> NotificationPreferences { get; set; }

    public virtual DbSet<Tax> Taxes { get; set; }

    protected override void OnConfiguring(DbContextOptionsBuilder optionsBuilder)

    {
        
    }

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder
            .UseCollation("utf8mb4_unicode_ci")
            .HasCharSet("utf8mb4");

        modelBuilder.ApplyConfigurationsFromAssembly(typeof(RestaurantMgmtContext).Assembly);

        OnModelCreatingPartial(modelBuilder);
    }

    partial void OnModelCreatingPartial(ModelBuilder modelBuilder);
}
