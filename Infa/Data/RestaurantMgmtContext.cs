using System;
using System.Collections.Generic;
using Core.Entity;
using Core.Domain.Enums;
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

    public virtual DbSet<Ingredient> Ingredients { get; set; }

    public virtual DbSet<InventoryTransaction> InventoryTransactions { get; set; }

    public virtual DbSet<InventoryTransactionItem> InventoryTransactionItems { get; set; }

    public virtual DbSet<InventoryTransactionMedium> InventoryTransactionMedia { get; set; }

    public virtual DbSet<MediaAsset> MediaAssets { get; set; }

    public virtual DbSet<Order> Orders { get; set; }

    public virtual DbSet<OrderItem> OrderItems { get; set; }

    public virtual DbSet<OrderPromotion> OrderPromotions { get; set; }

    public virtual DbSet<Payment> Payments { get; set; }

    public virtual DbSet<Permission> Permissions { get; set; }

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

    public virtual DbSet<TableMedium> TableMedia { get; set; }

    protected override void OnConfiguring(DbContextOptionsBuilder optionsBuilder) { }

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder
            .UseCollation("utf8mb4_unicode_ci")
            .HasCharSet("utf8mb4");

        modelBuilder.Entity<AuditLog>(entity =>
        {
            entity.HasKey(e => e.LogId).HasName("PRIMARY");

            entity.ToTable("audit_log");

            entity.HasIndex(e => e.StaffId, "audit_log_ibfk_1");

            entity.HasIndex(e => e.CreatedAt, "idx_audit_time");

            entity.Property(e => e.LogId).HasColumnName("log_id");
            entity.Property(e => e.ActionCode)
                .HasMaxLength(100)
                .HasColumnName("action_code");
            entity.Property(e => e.CreatedAt)
                .HasDefaultValueSql("CURRENT_TIMESTAMP")
                .HasColumnType("datetime")
                .HasColumnName("created_at");
            entity.Property(e => e.StaffId).HasColumnName("staff_id");
            entity.Property(e => e.TargetId).HasColumnName("target_id");
            entity.Property(e => e.TargetTable)
                .HasMaxLength(100)
                .HasColumnName("target_table");

            entity.HasOne(d => d.Staff).WithMany(p => p.AuditLogs)
                .HasForeignKey(d => d.StaffId)
                .HasConstraintName("audit_log_ibfk_1");
        });

        modelBuilder.Entity<AuthSession>(entity =>
        {
            entity.HasKey(e => e.SessionId).HasName("PRIMARY");

            entity.ToTable("auth_session");

            entity.HasIndex(e => new { e.UserId, e.ExpiresAt }, "idx_session_user");

            entity.Property(e => e.SessionId).HasColumnName("session_id");
            entity.Property(e => e.CreatedAt)
                .HasDefaultValueSql("CURRENT_TIMESTAMP")
                .HasColumnType("datetime")
                .HasColumnName("created_at");
            entity.Property(e => e.DeviceInfo)
                .HasMaxLength(500)
                .HasColumnName("device_info");
            entity.Property(e => e.ExpiresAt)
                .HasColumnType("datetime")
                .HasColumnName("expires_at");
            entity.Property(e => e.IpAddress)
                .HasMaxLength(45)
                .HasColumnName("ip_address");
            entity.Property(e => e.Revoked)
                .HasDefaultValueSql("'0'")
                .HasColumnName("revoked");
            entity.Property(e => e.TokenHash)
                .HasMaxLength(255)
                .HasColumnName("token_hash");
            entity.Property(e => e.UserId).HasColumnName("user_id");

            entity.HasOne(d => d.User).WithMany(p => p.AuthSessions)
                .HasForeignKey(d => d.UserId)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("auth_session_ibfk_1");
        });

        modelBuilder.Entity<CurrentStock>(entity =>
        {
            entity.HasKey(e => e.IngredientId).HasName("PRIMARY");

            entity.ToTable("current_stock");

            entity.Property(e => e.IngredientId)
                .ValueGeneratedNever()
                .HasColumnName("ingredient_id");
            entity.Property(e => e.LastUpdatedAt)
                .ValueGeneratedOnAddOrUpdate()
                .HasDefaultValueSql("CURRENT_TIMESTAMP")
                .HasColumnType("datetime")
                .HasColumnName("last_updated_at");
            entity.Property(e => e.QuantityOnHand)
                .HasPrecision(14, 3)
                .HasColumnName("quantity_on_hand");

            entity.HasOne(d => d.Ingredient).WithOne(p => p.CurrentStock)
                .HasForeignKey<CurrentStock>(d => d.IngredientId)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("fk_current_stock_ingredient");
        });

        modelBuilder.Entity<Customer>(entity =>
        {
            entity.HasKey(e => e.CustomerId).HasName("PRIMARY");

            entity.ToTable("customer");

            entity.HasIndex(e => e.Phone, "phone").IsUnique();

            entity.Property(e => e.CustomerId).HasColumnName("customer_id");
            entity.Property(e => e.CreatedAt)
                .HasDefaultValueSql("CURRENT_TIMESTAMP")
                .HasColumnType("datetime")
                .HasColumnName("created_at");
            entity.Property(e => e.Email)
                .HasMaxLength(150)
                .HasColumnName("email");
            entity.Property(e => e.FullName)
                .HasMaxLength(150)
                .HasColumnName("full_name");
            entity.Property(e => e.IsMember)
                .HasDefaultValueSql("'0'")
                .HasColumnName("is_member");
            entity.Property(e => e.LoyaltyPoints)
                .HasDefaultValueSql("'0'")
                .HasColumnName("loyalty_points");
            entity.Property(e => e.Phone)
                .HasMaxLength(30)
                .HasColumnName("phone");
        });

        modelBuilder.Entity<Dish>(entity =>
        {
            entity.HasKey(e => e.DishId).HasName("PRIMARY");

            entity.ToTable("dish");

            entity.HasIndex(e => e.CategoryId, "category_id");

            entity.Property(e => e.DishId).HasColumnName("dish_id");
            entity.Property(e => e.CategoryId).HasColumnName("category_id");
            entity.Property(e => e.CreatedAt)
                .HasDefaultValueSql("CURRENT_TIMESTAMP")
                .HasColumnType("datetime")
                .HasColumnName("created_at");
            entity.Property(e => e.DishName)
                .HasMaxLength(200)
                .HasColumnName("dish_name");
            entity.Property(e => e.DishStatus)
                .HasConversion<byte>()
                .HasDefaultValueSql("'1'")
                .HasComment("DishStatus: 1=AVAILABLE,2=OUT_OF_STOCK,3=HIDDEN")
                .HasColumnName("dish_status")
                .HasColumnType("tinyint unsigned");
            entity.Property(e => e.Price)
                .HasPrecision(12, 2)
                .HasColumnName("price");

            entity.HasOne(d => d.Category).WithMany(p => p.Dishes)
                .HasForeignKey(d => d.CategoryId)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("dish_ibfk_1");
        });

        modelBuilder.Entity<DishCategory>(entity =>
        {
            entity.HasKey(e => e.CategoryId).HasName("PRIMARY");

            entity.ToTable("dish_category");

            entity.Property(e => e.CategoryId).HasColumnName("category_id");
            entity.Property(e => e.CategoryName)
                .HasMaxLength(100)
                .HasColumnName("category_name");
        });

        modelBuilder.Entity<DishMedium>(entity =>
        {
            entity.HasKey(e => new { e.DishId, e.MediaId })
                .HasName("PRIMARY")
                .HasAnnotation("MySql:IndexPrefixLength", new[] { 0, 0 });

            entity.ToTable("dish_media");

            entity.HasIndex(e => e.MediaId, "media_id");

            entity.Property(e => e.DishId).HasColumnName("dish_id");
            entity.Property(e => e.MediaId).HasColumnName("media_id");
            entity.Property(e => e.IsPrimary)
                .HasDefaultValueSql("'0'")
                .HasColumnName("is_primary");

            entity.HasOne(d => d.Dish).WithMany(p => p.DishMedia)
                .HasForeignKey(d => d.DishId)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("dish_media_ibfk_1");

            entity.HasOne(d => d.Media).WithMany(p => p.DishMedia)
                .HasForeignKey(d => d.MediaId)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("dish_media_ibfk_2");
        });

        modelBuilder.Entity<Ingredient>(entity =>
        {
            entity.HasKey(e => e.IngredientId).HasName("PRIMARY");

            entity.ToTable("ingredient");

            entity.HasIndex(e => e.SupplierId, "FK_ingredient_supplier_id");

            entity.Property(e => e.IngredientId).HasColumnName("ingredient_id");
            entity.Property(e => e.IngredientName)
                .HasMaxLength(200)
                .HasColumnName("ingredient_name");
            entity.Property(e => e.SupplierId).HasColumnName("supplier_id");
            entity.Property(e => e.Unit)
                .HasMaxLength(20)
                .HasColumnName("unit");

            entity.HasOne(d => d.Supplier).WithMany(p => p.Ingredients)
                .HasForeignKey(d => d.SupplierId)
                .HasConstraintName("FK_ingredient_supplier_id");
        });

        modelBuilder.Entity<InventoryTransaction>(entity =>
        {
            entity.HasKey(e => e.TransactionId).HasName("PRIMARY");

            entity.ToTable("inventory_transaction");

            entity.HasIndex(e => e.CreatedBy, "fk_inventory_transaction_staff");

            entity.HasIndex(e => new { e.Type, e.CreatedAt }, "idx_inventory_transaction_dir_time");

            entity.HasIndex(e => e.CreatedAt, "idx_inventory_transaction_time");

            entity.Property(e => e.TransactionId).HasColumnName("transaction_id");
            entity.Property(e => e.CreatedAt)
                .HasDefaultValueSql("CURRENT_TIMESTAMP")
                .HasColumnType("datetime")
                .HasColumnName("created_at");
            entity.Property(e => e.CreatedBy).HasColumnName("created_by");
            entity.Property(e => e.Note)
                .HasMaxLength(255)
                .HasColumnName("note");
            entity.Property(e => e.Status)
                .HasConversion<byte>()
                .HasComment("1=DRAFT,2=PENDING_APPROVAL,3=COMPLETED,4=CANCELLED")
                .HasColumnName("status")
                .HasColumnType("tinyint unsigned");
            entity.Property(e => e.Type)
                .HasConversion<byte>()
                .HasComment("1=IN, 2=OUT, 3=ADJUST")
                .HasColumnName("type")
                .HasColumnType("tinyint unsigned");

            entity.HasOne(d => d.CreatedByNavigation).WithMany(p => p.InventoryTransactions)
                .HasForeignKey(d => d.CreatedBy)
                .HasConstraintName("fk_inventory_transaction_staff");
        });

        modelBuilder.Entity<InventoryTransactionItem>(entity =>
        {
            entity.HasKey(e => e.TransactionItemId).HasName("PRIMARY");

            entity.ToTable("inventory_transaction_item");

            entity.HasIndex(e => e.IngredientId, "idx_inventory_transaction_item_ingredient");

            entity.HasIndex(e => e.TransactionId, "idx_inventory_transaction_item_transaction");

            entity.HasIndex(e => new { e.TransactionId, e.IngredientId }, "uq_inventory_transaction_item").IsUnique();

            entity.Property(e => e.TransactionItemId).HasColumnName("transaction_item_id");
            entity.Property(e => e.IngredientId).HasColumnName("ingredient_id");
            entity.Property(e => e.Note)
                .HasMaxLength(255)
                .HasColumnName("note");
            entity.Property(e => e.Quantity)
                .HasPrecision(14, 3)
                .HasColumnName("quantity");
            entity.Property(e => e.TransactionId).HasColumnName("transaction_id");
            entity.Property(e => e.Unit)
                .HasMaxLength(20)
                .HasColumnName("unit");

            entity.HasOne(d => d.Ingredient).WithMany(p => p.InventoryTransactionItems)
                .HasForeignKey(d => d.IngredientId)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("fk_inventory_transaction_item_ingredient");

            entity.HasOne(d => d.Transaction).WithMany(p => p.InventoryTransactionItems)
                .HasForeignKey(d => d.TransactionId)
                .HasConstraintName("fk_inventory_transaction_item_transaction");
        });

        modelBuilder.Entity<InventoryTransactionMedium>(entity =>
        {
            entity.HasKey(e => new { e.TransactionId, e.MediaId })
                .HasName("PRIMARY")
                .HasAnnotation("MySql:IndexPrefixLength", new[] { 0, 0 });

            entity.ToTable("inventory_transaction_media");

            entity.HasIndex(e => e.MediaId, "idx_inventory_transaction_media_media");

            entity.HasIndex(e => new { e.TransactionId, e.IsPrimary }, "idx_inventory_transaction_media_primary");

            entity.Property(e => e.TransactionId).HasColumnName("transaction_id");
            entity.Property(e => e.MediaId).HasColumnName("media_id");
            entity.Property(e => e.IsPrimary)
                .HasDefaultValueSql("'0'")
                .HasColumnName("is_primary");

            entity.HasOne(d => d.Media).WithMany(p => p.InventoryTransactionMedia)
                .HasForeignKey(d => d.MediaId)
                .HasConstraintName("fk_inventory_transaction_media_asset");

            entity.HasOne(d => d.Transaction).WithMany(p => p.InventoryTransactionMedia)
                .HasForeignKey(d => d.TransactionId)
                .HasConstraintName("fk_inventory_transaction_media_transaction");
        });

        modelBuilder.Entity<MediaAsset>(entity =>
        {
            entity.HasKey(e => e.MediaId).HasName("PRIMARY");

            entity.ToTable("media_asset");

            entity.HasIndex(e => e.MediaTypeId, "media_type_id");

            entity.Property(e => e.MediaId).HasColumnName("media_id");
            entity.Property(e => e.CreatedAt)
                .HasDefaultValueSql("CURRENT_TIMESTAMP")
                .HasColumnType("datetime")
                .HasColumnName("created_at");
            entity.Property(e => e.DurationSec).HasColumnName("duration_sec");
            entity.Property(e => e.Height).HasColumnName("height");
            entity.Property(e => e.MediaTypeId)
                .HasConversion<byte>()
                .HasComment("MediaType (numeric enum in app)")
                .HasColumnName("media_type_id")
                .HasColumnType("tinyint unsigned");
            entity.Property(e => e.MimeType)
                .HasMaxLength(100)
                .HasColumnName("mime_type");
            entity.Property(e => e.Url)
                .HasMaxLength(500)
                .HasColumnName("url");
            entity.Property(e => e.Width).HasColumnName("width");
        });

        modelBuilder.Entity<Order>(entity =>
        {
            entity.HasKey(e => e.OrderId).HasName("PRIMARY");

            entity.ToTable("orders");

            entity.HasIndex(e => e.CustomerId, "FK_orders_customer_id");

            entity.HasIndex(e => e.CreatedAt, "idx_order_status");

            entity.HasIndex(e => e.StaffId, "orders_ibfk_2");

            entity.HasIndex(e => e.SourceId, "source_id");

            entity.HasIndex(e => e.TableId, "table_id");

            entity.Property(e => e.OrderId).HasColumnName("order_id");
            entity.Property(e => e.CreatedAt)
                .HasDefaultValueSql("CURRENT_TIMESTAMP")
                .HasColumnType("datetime")
                .HasColumnName("created_at");
            entity.Property(e => e.CustomerId).HasColumnName("customer_id");
            entity.Property(e => e.OrderStatus)
                .HasConversion<byte>()
                .HasDefaultValueSql("'1'")
                .HasComment("OrderStatus: 1=PENDING,2=IN_PROGRESS,3=COMPLETED,4=CANCELLED")
                .HasColumnName("order_status")
                .HasColumnType("tinyint unsigned");
            entity.Property(e => e.SourceId)
                .HasConversion<byte>()
                .HasComment("OrderSource (numeric enum in app)")
                .HasColumnName("source_id")
                .HasColumnType("tinyint unsigned");
            entity.Property(e => e.StaffId).HasColumnName("staff_id");
            entity.Property(e => e.TableId).HasColumnName("table_id");
            entity.Property(e => e.TipAmount)
                .HasPrecision(14, 2)
                .HasColumnName("tip_amount");
            entity.Property(e => e.TotalAmount)
                .HasPrecision(14, 2)
                .HasColumnName("total_amount");
            entity.Property(e => e.UpdatedAt)
                .HasColumnType("datetime")
                .HasColumnName("updated_at");

            entity.HasOne(d => d.Customer).WithMany(p => p.Orders)
                .HasForeignKey(d => d.CustomerId)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("FK_orders_customer_id");

            entity.HasOne(d => d.Staff).WithMany(p => p.Orders)
                .HasForeignKey(d => d.StaffId)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("orders_ibfk_2");

            entity.HasOne(d => d.Table).WithMany(p => p.Orders)
                .HasForeignKey(d => d.TableId)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("orders_ibfk_1");
        });

        modelBuilder.Entity<OrderItem>(entity =>
        {
            entity.HasKey(e => e.OrderItemId).HasName("PRIMARY");

            entity.ToTable("order_item");

            entity.HasIndex(e => e.DishId, "dish_id");

            entity.HasIndex(e => e.OrderId, "order_id");

            entity.Property(e => e.OrderItemId).HasColumnName("order_item_id");
            entity.Property(e => e.DishId).HasColumnName("dish_id");
            entity.Property(e => e.ItemStatus)
                .HasConversion<byte>()
                .HasDefaultValueSql("'1'")
                .HasComment("OrderItemStatus: 1=CREATED,2=IN_PROGRESS,3=READY,4=SERVED,5=REJECTED")
                .HasColumnName("item_status")
                .HasColumnType("tinyint unsigned");
            entity.Property(e => e.OrderId).HasColumnName("order_id");
            entity.Property(e => e.Price)
                .HasPrecision(12, 2)
                .HasColumnName("price");
            entity.Property(e => e.Quantity).HasColumnName("quantity");
            entity.Property(e => e.RejectReason)
                .HasMaxLength(255)
                .HasColumnName("reject_reason");

            entity.HasOne(d => d.Dish).WithMany(p => p.OrderItems)
                .HasForeignKey(d => d.DishId)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("order_item_ibfk_2");

            entity.HasOne(d => d.Order).WithMany(p => p.OrderItems)
                .HasForeignKey(d => d.OrderId)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("order_item_ibfk_1");
        });

        modelBuilder.Entity<OrderPromotion>(entity =>
        {
            entity.HasKey(e => e.OrderPromotionId).HasName("PRIMARY");

            entity.ToTable("order_promotion");

            entity.HasIndex(e => e.PromotionId, "idx_invoice_promo_promo");

            entity.HasIndex(e => new { e.OrderId, e.PromotionId }, "uq_invoice_promo").IsUnique();

            entity.Property(e => e.OrderPromotionId).HasColumnName("order_promotion_id");
            entity.Property(e => e.AppliedAt)
                .HasDefaultValueSql("CURRENT_TIMESTAMP")
                .HasColumnType("datetime")
                .HasColumnName("applied_at");
            entity.Property(e => e.DiscountAmount)
                .HasPrecision(14, 2)
                .HasColumnName("discount_amount");
            entity.Property(e => e.OrderId).HasColumnName("order_id");
            entity.Property(e => e.PromotionId).HasColumnName("promotion_id");

            entity.HasOne(d => d.Order).WithMany(p => p.OrderPromotions)
                .HasForeignKey(d => d.OrderId)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("FK_invoice_promotion_order_id");

            entity.HasOne(d => d.Promotion).WithMany(p => p.OrderPromotions)
                .HasForeignKey(d => d.PromotionId)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("order_promotion_ibfk_2");
        });

        modelBuilder.Entity<Payment>(entity =>
        {
            entity.HasKey(e => e.PaymentId).HasName("PRIMARY");

            entity.ToTable("payment");

            entity.HasIndex(e => e.MethodId, "method_id");

            entity.HasIndex(e => e.OrderId, "payment_ibfk_1");

            entity.Property(e => e.PaymentId).HasColumnName("payment_id");
            entity.Property(e => e.ChangeAmount)
                .HasPrecision(14, 2)
                .HasColumnName("change_amount");
            entity.Property(e => e.MethodId)
                .HasConversion<byte>()
                .HasComment("PaymentMethod (numeric enum in app)")
                .HasColumnName("method_id")
                .HasColumnType("tinyint unsigned");
            entity.Property(e => e.OrderId).HasColumnName("order_id");
            entity.Property(e => e.PaidAt)
                .HasDefaultValueSql("CURRENT_TIMESTAMP")
                .HasColumnType("datetime")
                .HasColumnName("paid_at");
            entity.Property(e => e.ReceivedAmount)
                .HasPrecision(14, 2)
                .HasColumnName("received_amount");

            entity.HasOne(d => d.Order).WithMany(p => p.Payments)
                .HasForeignKey(d => d.OrderId)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("payment_ibfk_1");
        });

        modelBuilder.Entity<Permission>(entity =>
        {
            entity.HasKey(e => e.PermissionId).HasName("PRIMARY");

            entity.ToTable("permission");

            entity.Property(e => e.PermissionId).HasColumnName("permission_id");
            entity.Property(e => e.ActionCode)
                .HasMaxLength(20)
                .HasColumnName("action_code");
            entity.Property(e => e.ScreenCode)
                .HasMaxLength(100)
                .HasColumnName("screen_code");
        });

        modelBuilder.Entity<Promotion>(entity =>
        {
            entity.HasKey(e => e.PromotionId).HasName("PRIMARY");

            entity.ToTable("promotion");

            entity.HasIndex(e => e.PromoCode, "promo_code").IsUnique();

            entity.HasIndex(e => e.TypeId, "type_id");

            entity.Property(e => e.PromotionId).HasColumnName("promotion_id");
            entity.Property(e => e.CreatedAt)
                .HasDefaultValueSql("CURRENT_TIMESTAMP")
                .HasColumnType("datetime")
                .HasColumnName("created_at");
            entity.Property(e => e.Description)
                .HasMaxLength(255)
                .HasColumnName("description");
            entity.Property(e => e.EndTime)
                .HasColumnType("datetime")
                .HasColumnName("end_time");
            entity.Property(e => e.MaxUsage).HasColumnName("max_usage");
            entity.Property(e => e.PromoCode)
                .HasMaxLength(50)
                .HasColumnName("promo_code");
            entity.Property(e => e.PromoName)
                .HasMaxLength(200)
                .HasColumnName("promo_name");
            entity.Property(e => e.PromotionStatus)
                .HasConversion<byte>()
                .HasDefaultValueSql("'1'")
                .HasComment("PromotionStatus: 1=SCHEDULED,2=ACTIVE,3=EXPIRED,4=DISABLED")
                .HasColumnName("promotion_status")
                .HasColumnType("tinyint unsigned");
            entity.Property(e => e.StartTime)
                .HasColumnType("datetime")
                .HasColumnName("start_time");
            entity.Property(e => e.TypeId)
                .HasConversion<byte>()
                .HasComment("PromotionType (numeric enum in app)")
                .HasColumnName("type_id")
                .HasColumnType("tinyint unsigned");
            entity.Property(e => e.UsedCount)
                .HasDefaultValueSql("'0'")
                .HasColumnName("used_count");
        });

        modelBuilder.Entity<PromotionRule>(entity =>
        {
            entity.HasKey(e => e.RuleId).HasName("PRIMARY");

            entity.ToTable("promotion_rule");

            entity.HasIndex(e => e.PromotionId, "promotion_id");

            entity.HasIndex(e => e.RequiredCategoryId, "required_category_id");

            entity.HasIndex(e => e.RequiredDishId, "required_dish_id");

            entity.Property(e => e.RuleId).HasColumnName("rule_id");
            entity.Property(e => e.MinOrderValue)
                .HasPrecision(14, 2)
                .HasColumnName("min_order_value");
            entity.Property(e => e.MinQuantity).HasColumnName("min_quantity");
            entity.Property(e => e.PromotionId).HasColumnName("promotion_id");
            entity.Property(e => e.RequiredCategoryId).HasColumnName("required_category_id");
            entity.Property(e => e.RequiredDishId).HasColumnName("required_dish_id");

            entity.HasOne(d => d.Promotion).WithMany(p => p.PromotionRules)
                .HasForeignKey(d => d.PromotionId)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("promotion_rule_ibfk_1");

            entity.HasOne(d => d.RequiredCategory).WithMany(p => p.PromotionRules)
                .HasForeignKey(d => d.RequiredCategoryId)
                .HasConstraintName("promotion_rule_ibfk_3");

            entity.HasOne(d => d.RequiredDish).WithMany(p => p.PromotionRules)
                .HasForeignKey(d => d.RequiredDishId)
                .HasConstraintName("promotion_rule_ibfk_2");
        });

        modelBuilder.Entity<PromotionTarget>(entity =>
        {
            entity.HasKey(e => e.TargetId).HasName("PRIMARY");

            entity.ToTable("promotion_target");

            entity.HasIndex(e => e.CategoryId, "category_id");

            entity.HasIndex(e => e.DishId, "dish_id");

            entity.HasIndex(e => e.PromotionId, "promotion_id");

            entity.Property(e => e.TargetId).HasColumnName("target_id");
            entity.Property(e => e.CategoryId).HasColumnName("category_id");
            entity.Property(e => e.DishId).HasColumnName("dish_id");
            entity.Property(e => e.PromotionId).HasColumnName("promotion_id");

            entity.HasOne(d => d.Category).WithMany(p => p.PromotionTargets)
                .HasForeignKey(d => d.CategoryId)
                .HasConstraintName("promotion_target_ibfk_3");

            entity.HasOne(d => d.Dish).WithMany(p => p.PromotionTargets)
                .HasForeignKey(d => d.DishId)
                .HasConstraintName("promotion_target_ibfk_2");

            entity.HasOne(d => d.Promotion).WithMany(p => p.PromotionTargets)
                .HasForeignKey(d => d.PromotionId)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("promotion_target_ibfk_1");
        });

        modelBuilder.Entity<Recipe>(entity =>
        {
            entity.HasKey(e => new { e.DishId, e.IngredientId })
                .HasName("PRIMARY")
                .HasAnnotation("MySql:IndexPrefixLength", new[] { 0, 0 });

            entity.ToTable("recipe");

            entity.HasIndex(e => e.IngredientId, "idx_recipe_ingredient");

            entity.Property(e => e.DishId).HasColumnName("dish_id");
            entity.Property(e => e.IngredientId).HasColumnName("ingredient_id");
            entity.Property(e => e.Note)
                .HasMaxLength(255)
                .HasColumnName("note");
            entity.Property(e => e.Quantity)
                .HasPrecision(12, 3)
                .HasColumnName("quantity");
            entity.Property(e => e.Unit)
                .HasMaxLength(20)
                .HasColumnName("unit");

            entity.HasOne(d => d.Dish).WithMany(p => p.Recipes)
                .HasForeignKey(d => d.DishId)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("fk_recipe_dish");

            entity.HasOne(d => d.Ingredient).WithMany(p => p.Recipes)
                .HasForeignKey(d => d.IngredientId)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("fk_recipe_ingredient");
        });

        modelBuilder.Entity<Reservation>(entity =>
        {
            entity.HasKey(e => e.ReservationId).HasName("PRIMARY");

            entity.ToTable("reservation");

            entity.HasIndex(e => e.CustomerId, "customer_id");

            entity.HasIndex(e => e.ReservedTime, "idx_reservation_time");

            entity.HasIndex(e => e.SourceId, "source_id");

            entity.Property(e => e.ReservationId).HasColumnName("reservation_id");
            entity.Property(e => e.CreatedAt)
                .HasDefaultValueSql("CURRENT_TIMESTAMP")
                .HasColumnType("datetime")
                .HasColumnName("created_at");
            entity.Property(e => e.CustomerId).HasColumnName("customer_id");
            entity.Property(e => e.CustomerName)
                .HasMaxLength(150)
                .HasColumnName("customer_name");
            entity.Property(e => e.Email)
                .HasMaxLength(150)
                .HasColumnName("email");
            entity.Property(e => e.PartySize).HasColumnName("party_size");
            entity.Property(e => e.Phone)
                .HasMaxLength(30)
                .HasColumnName("phone");
            entity.Property(e => e.ReservationStatus)
                .HasConversion<byte>()
                .HasDefaultValueSql("'1'")
                .HasComment("ReservationStatus: 1=PENDING,2=CONFIRMED,3=CHECKED_IN,4=CANCELLED,5=NO_SHOW")
                .HasColumnName("reservation_status")
                .HasColumnType("tinyint unsigned");
            entity.Property(e => e.ReservedTime)
                .HasColumnType("datetime")
                .HasColumnName("reserved_time");
            entity.Property(e => e.SourceId)
                .HasConversion<byte>()
                .HasComment("ReservationSource (numeric enum in app)")
                .HasColumnName("source_id")
                .HasColumnType("tinyint unsigned");

            entity.HasOne(d => d.Customer).WithMany(p => p.Reservations)
                .HasForeignKey(d => d.CustomerId)
                .HasConstraintName("reservation_ibfk_1");

            entity.HasMany(d => d.Tables).WithMany(p => p.Reservations)
                .UsingEntity<Dictionary<string, object>>(
                    "ReservationTable",
                    r => r.HasOne<RestaurantTable>().WithMany()
                        .HasForeignKey("TableId")
                        .OnDelete(DeleteBehavior.ClientSetNull)
                        .HasConstraintName("reservation_table_ibfk_2"),
                    l => l.HasOne<Reservation>().WithMany()
                        .HasForeignKey("ReservationId")
                        .OnDelete(DeleteBehavior.ClientSetNull)
                        .HasConstraintName("reservation_table_ibfk_1"),
                    j =>
                    {
                        j.HasKey("ReservationId", "TableId")
                            .HasName("PRIMARY")
                            .HasAnnotation("MySql:IndexPrefixLength", new[] { 0, 0 });
                        j.ToTable("reservation_table");
                        j.HasIndex(new[] { "TableId" }, "table_id");
                        j.IndexerProperty<long>("ReservationId").HasColumnName("reservation_id");
                        j.IndexerProperty<long>("TableId").HasColumnName("table_id");
                    });
        });

        modelBuilder.Entity<RestaurantTable>(entity =>
        {
            entity.HasKey(e => e.TableId).HasName("PRIMARY");

            entity.ToTable("restaurant_table");

            entity.HasIndex(e => e.TableQrImg, "FK_restaurant_table_table_qr_img");

            entity.HasIndex(e => e.TableType, "FK_restaurant_table_table_type");

            entity.HasIndex(e => e.TableCode, "table_code").IsUnique();

            entity.Property(e => e.TableId).HasColumnName("table_id");
            entity.Property(e => e.Capacity).HasColumnName("capacity");
            entity.Property(e => e.TableCode)
                .HasMaxLength(50)
                .HasColumnName("table_code");
            entity.Property(e => e.TableQrImg).HasColumnName("table_qr_img");
            entity.Property(e => e.TableStatus)
                .HasConversion<byte>()
                .HasDefaultValueSql("'1'")
                .HasComment("TableStatus: 1=AVAILABLE,2=OCCUPIED,3=RESERVED,4=LOCKED")
                .HasColumnName("table_status")
                .HasColumnType("tinyint unsigned");
            entity.Property(e => e.TableType)
                .HasConversion<byte>()
                .HasComment("TableType (numeric enum in app)")
                .HasColumnName("table_type")
                .HasColumnType("tinyint unsigned");

            entity.HasOne(d => d.TableQrImgNavigation).WithMany(p => p.RestaurantTables)
                .HasForeignKey(d => d.TableQrImg)
                .OnDelete(DeleteBehavior.Cascade)
                .HasConstraintName("FK_restaurant_table_table_qr_img");
        });

        modelBuilder.Entity<Role>(entity =>
        {
            entity.HasKey(e => e.RoleId).HasName("PRIMARY");

            entity.ToTable("role");

            entity.HasIndex(e => e.RoleCode, "role_code").IsUnique();

            entity.Property(e => e.RoleId).HasColumnName("role_id");
            entity.Property(e => e.RoleCode)
                .HasMaxLength(50)
                .HasColumnName("role_code");
            entity.Property(e => e.RoleName)
                .HasMaxLength(100)
                .HasColumnName("role_name");

            entity.HasMany(d => d.Permissions).WithMany(p => p.Roles)
                .UsingEntity<Dictionary<string, object>>(
                    "RolePermission",
                    r => r.HasOne<Permission>().WithMany()
                        .HasForeignKey("PermissionId")
                        .OnDelete(DeleteBehavior.ClientSetNull)
                        .HasConstraintName("role_permission_ibfk_2"),
                    l => l.HasOne<Role>().WithMany()
                        .HasForeignKey("RoleId")
                        .OnDelete(DeleteBehavior.ClientSetNull)
                        .HasConstraintName("role_permission_ibfk_1"),
                    j =>
                    {
                        j.HasKey("RoleId", "PermissionId")
                            .HasName("PRIMARY")
                            .HasAnnotation("MySql:IndexPrefixLength", new[] { 0, 0 });
                        j.ToTable("role_permission");
                        j.HasIndex(new[] { "PermissionId" }, "permission_id");
                        j.IndexerProperty<long>("RoleId").HasColumnName("role_id");
                        j.IndexerProperty<long>("PermissionId").HasColumnName("permission_id");
                    });
        });

        modelBuilder.Entity<ServiceError>(entity =>
        {
            entity.HasKey(e => e.ErrorId).HasName("PRIMARY");

            entity.ToTable("service_error");

            entity.HasIndex(e => e.CategoryId, "category_id");

            entity.HasIndex(e => e.OrderId, "idx_service_error_order");

            entity.HasIndex(e => new { e.StaffId, e.CreatedAt }, "idx_service_error_staff");

            entity.HasIndex(e => e.OrderItemId, "order_item_id");

            entity.HasIndex(e => e.ResolvedBy, "service_error_ibfk_7");

            entity.HasIndex(e => e.SeverityId, "severity_id");

            entity.HasIndex(e => e.TableId, "table_id");

            entity.Property(e => e.ErrorId).HasColumnName("error_id");
            entity.Property(e => e.CategoryId).HasColumnName("category_id");
            entity.Property(e => e.CreatedAt)
                .HasDefaultValueSql("CURRENT_TIMESTAMP")
                .HasColumnType("datetime")
                .HasColumnName("created_at");
            entity.Property(e => e.Description)
                .HasMaxLength(500)
                .HasColumnName("description");
            entity.Property(e => e.IsResolved)
                .HasDefaultValueSql("'0'")
                .HasColumnName("is_resolved");
            entity.Property(e => e.OrderId).HasColumnName("order_id");
            entity.Property(e => e.OrderItemId).HasColumnName("order_item_id");
            entity.Property(e => e.PenaltyAmount)
                .HasPrecision(12, 2)
                .HasDefaultValueSql("'0.00'")
                .HasColumnName("penalty_amount");
            entity.Property(e => e.ResolvedAt)
                .HasColumnType("datetime")
                .HasColumnName("resolved_at");
            entity.Property(e => e.ResolvedBy).HasColumnName("resolved_by");
            entity.Property(e => e.SeverityId)
                .HasConversion<byte>()
                .HasComment("Severity (numeric enum in app)")
                .HasColumnName("severity_id")
                .HasColumnType("tinyint unsigned");
            entity.Property(e => e.StaffId).HasColumnName("staff_id");
            entity.Property(e => e.TableId).HasColumnName("table_id");

            entity.HasOne(d => d.Category).WithMany(p => p.ServiceErrors)
                .HasForeignKey(d => d.CategoryId)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("service_error_ibfk_5");

            entity.HasOne(d => d.Order).WithMany(p => p.ServiceErrors)
                .HasForeignKey(d => d.OrderId)
                .HasConstraintName("service_error_ibfk_2");

            entity.HasOne(d => d.OrderItem).WithMany(p => p.ServiceErrors)
                .HasForeignKey(d => d.OrderItemId)
                .HasConstraintName("service_error_ibfk_3");

            entity.HasOne(d => d.ResolvedByNavigation).WithMany(p => p.ServiceErrorResolvedByNavigations)
                .HasForeignKey(d => d.ResolvedBy)
                .HasConstraintName("service_error_ibfk_7");

            entity.HasOne(d => d.Staff).WithMany(p => p.ServiceErrorStaffs)
                .HasForeignKey(d => d.StaffId)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("service_error_ibfk_1");

            entity.HasOne(d => d.Table).WithMany(p => p.ServiceErrors)
                .HasForeignKey(d => d.TableId)
                .HasConstraintName("service_error_ibfk_4");
        });

        modelBuilder.Entity<ServiceErrorCategory>(entity =>
        {
            entity.HasKey(e => e.CategoryId).HasName("PRIMARY");

            entity.ToTable("service_error_category");

            entity.HasIndex(e => e.CategoryCode, "category_code").IsUnique();

            entity.Property(e => e.CategoryId).HasColumnName("category_id");
            entity.Property(e => e.CategoryCode)
                .HasMaxLength(50)
                .HasColumnName("category_code");
            entity.Property(e => e.CategoryName)
                .HasMaxLength(150)
                .HasColumnName("category_name");
            entity.Property(e => e.Description)
                .HasMaxLength(255)
                .HasColumnName("description");
        });

        modelBuilder.Entity<StaffAccount>(entity =>
        {
            entity.HasKey(e => e.AccountId).HasName("PRIMARY");

            entity.ToTable("staff_account");

            entity.HasIndex(e => e.Email, "email").IsUnique();

            entity.HasIndex(e => e.RoleId, "role_id");

            entity.HasIndex(e => e.Username, "username").IsUnique();

            entity.Property(e => e.AccountId).HasColumnName("account_id");
            entity.Property(e => e.AccountStatus)
                .HasConversion<byte>()
                .HasDefaultValueSql("'1'")
                .HasComment("AccountStatus: 1=ACTIVE,2=INACTIVE,3=LOCKED")
                .HasColumnName("account_status")
                .HasColumnType("tinyint unsigned");
            entity.Property(e => e.CreatedAt)
                .HasDefaultValueSql("CURRENT_TIMESTAMP")
                .HasColumnType("datetime")
                .HasColumnName("created_at");
            entity.Property(e => e.Email)
                .HasMaxLength(150)
                .HasColumnName("email");
            entity.Property(e => e.FullName)
                .HasMaxLength(150)
                .HasColumnName("full_name");
            entity.Property(e => e.IsLocked).HasColumnName("is_locked");
            entity.Property(e => e.LastLoginAt)
                .HasColumnType("datetime")
                .HasColumnName("last_login_at");
            entity.Property(e => e.PasswordHash)
                .HasMaxLength(255)
                .HasColumnName("password_hash");
            entity.Property(e => e.Phone)
                .HasMaxLength(30)
                .HasColumnName("phone");
            entity.Property(e => e.RoleId).HasColumnName("role_id");
            entity.Property(e => e.Username)
                .HasMaxLength(100)
                .HasColumnName("username");

            entity.HasOne(d => d.Role).WithMany(p => p.StaffAccounts)
                .HasForeignKey(d => d.RoleId)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("staff_account_ibfk_1");
        });

        modelBuilder.Entity<Supplier>(entity =>
        {
            entity.HasKey(e => e.SupplierId).HasName("PRIMARY");

            entity.ToTable("supplier");

            entity.Property(e => e.SupplierId).HasColumnName("supplier_id");
            entity.Property(e => e.Email)
                .HasMaxLength(150)
                .HasColumnName("email");
            entity.Property(e => e.Phone)
                .HasMaxLength(50)
                .HasColumnName("phone");
            entity.Property(e => e.SupplierName)
                .HasMaxLength(200)
                .HasColumnName("supplier_name");
        });

        modelBuilder.Entity<TableMedium>(entity =>
        {
            entity.HasKey(e => new { e.TableId, e.MediaId })
                .HasName("PRIMARY")
                .HasAnnotation("MySql:IndexPrefixLength", new[] { 0, 0 });

            entity.ToTable("table_media");

            entity.HasIndex(e => e.MediaId, "media_id");

            entity.Property(e => e.TableId).HasColumnName("table_id");
            entity.Property(e => e.MediaId).HasColumnName("media_id");
            entity.Property(e => e.IsPrimary)
                .HasDefaultValueSql("'0'")
                .HasColumnName("is_primary");

            entity.HasOne(d => d.Media).WithMany(p => p.TableMedia)
                .HasForeignKey(d => d.MediaId)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("table_media_ibfk_2");

            entity.HasOne(d => d.Table).WithMany(p => p.TableMedia)
                .HasForeignKey(d => d.TableId)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("table_media_ibfk_1");
        });

        OnModelCreatingPartial(modelBuilder);
    }

    partial void OnModelCreatingPartial(ModelBuilder modelBuilder);
}
