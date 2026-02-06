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

    public virtual DbSet<SystemSetting> SystemSettings { get; set; }

    public virtual DbSet<TableMedium> TableMedia { get; set; }

    protected override void OnConfiguring(DbContextOptionsBuilder optionsBuilder)
#warning To protect potentially sensitive information in your connection string, you should move it out of source code. You can avoid scaffolding the connection string by using the Name= syntax to read it from configuration - see https://go.microsoft.com/fwlink/?linkid=2131148. For more guidance on storing connection strings, see https://go.microsoft.com/fwlink/?LinkId=723263.
        => optionsBuilder.UseMySql("server=localhost;database=restaurant_mgmt;user=root;password=1234", ServerVersion.Parse("8.0.44-mysql"));

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

            entity.HasIndex(e => e.DescriptionTextId, "fk_dish_desc_text");

            entity.HasIndex(e => e.DishNameTextId, "fk_dish_name_text");

            entity.HasIndex(e => e.NoteTextId, "fk_dish_note_text");

            entity.HasIndex(e => e.ShortDescriptionTextId, "fk_dish_short_desc_text");

            entity.HasIndex(e => e.SloganTextId, "fk_dish_slogan_text");

            entity.HasIndex(e => e.DishStatusLvId, "idx_dish_status_lv");

            entity.Property(e => e.DishId).HasColumnName("dish_id");
            entity.Property(e => e.Calories).HasColumnName("calories");
            entity.Property(e => e.CategoryId).HasColumnName("category_id");
            entity.Property(e => e.ChefRecommended).HasColumnName("chef_recommended");
            entity.Property(e => e.CookTimeMinutes).HasColumnName("cook_time_minutes");
            entity.Property(e => e.CreatedAt)
                .HasDefaultValueSql("CURRENT_TIMESTAMP")
                .HasColumnType("datetime")
                .HasColumnName("created_at");
            entity.Property(e => e.Description)
                .HasColumnType("text")
                .HasColumnName("description");
            entity.Property(e => e.DescriptionTextId).HasColumnName("description_text_id");
            entity.Property(e => e.DishName)
                .HasMaxLength(200)
                .HasColumnName("dish_name");
            entity.Property(e => e.DishNameTextId).HasColumnName("dish_name_text_id");
            entity.Property(e => e.DishStatusLvId).HasColumnName("dish_status_lv_id");
            entity.Property(e => e.DisplayOrder).HasColumnName("display_order");
            entity.Property(e => e.IsOnline)
                .IsRequired()
                .HasDefaultValueSql("'1'")
                .HasColumnName("isOnline");
            entity.Property(e => e.Note)
                .HasMaxLength(500)
                .HasColumnName("note")
                .UseCollation("utf8mb3_general_ci")
                .HasCharSet("utf8mb3");
            entity.Property(e => e.NoteTextId).HasColumnName("note_text_id");
            entity.Property(e => e.PrepTimeMinutes).HasColumnName("prep_time_minutes");
            entity.Property(e => e.Price)
                .HasPrecision(12, 2)
                .HasColumnName("price");
            entity.Property(e => e.ShortDescription)
                .HasMaxLength(255)
                .HasColumnName("short_description")
                .UseCollation("utf8mb3_general_ci")
                .HasCharSet("utf8mb3");
            entity.Property(e => e.ShortDescriptionTextId).HasColumnName("short_description_text_id");
            entity.Property(e => e.Slogan)
                .HasMaxLength(250)
                .HasColumnName("slogan")
                .UseCollation("utf8mb3_general_ci")
                .HasCharSet("utf8mb3");
            entity.Property(e => e.SloganTextId).HasColumnName("slogan_text_id");

            entity.HasOne(d => d.Category).WithMany(p => p.Dishes)
                .HasForeignKey(d => d.CategoryId)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("dish_ibfk_1");

            entity.HasOne(d => d.DescriptionText).WithMany(p => p.DishDescriptionTexts)
                .HasForeignKey(d => d.DescriptionTextId)
                .HasConstraintName("fk_dish_desc_text");

            entity.HasOne(d => d.DishNameText).WithMany(p => p.DishDishNameTexts)
                .HasForeignKey(d => d.DishNameTextId)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("fk_dish_name_text");

            entity.HasOne(d => d.DishStatusLv).WithMany(p => p.Dishes)
                .HasForeignKey(d => d.DishStatusLvId)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("fk_dish_status_lv");

            entity.HasOne(d => d.NoteText).WithMany(p => p.DishNoteTexts)
                .HasForeignKey(d => d.NoteTextId)
                .HasConstraintName("fk_dish_note_text");

            entity.HasOne(d => d.ShortDescriptionText).WithMany(p => p.DishShortDescriptionTexts)
                .HasForeignKey(d => d.ShortDescriptionTextId)
                .HasConstraintName("fk_dish_short_desc_text");

            entity.HasOne(d => d.SloganText).WithMany(p => p.DishSloganTexts)
                .HasForeignKey(d => d.SloganTextId)
                .HasConstraintName("fk_dish_slogan_text");
        });

        modelBuilder.Entity<DishCategory>(entity =>
        {
            entity.HasKey(e => e.CategoryId).HasName("PRIMARY");

            entity.ToTable("dish_category");

            entity.HasIndex(e => e.DescriptionTextId, "FK_dish_category_i18n_text_text_id");

            entity.HasIndex(e => e.CategoryNameTextId, "fk_cat_name_text");

            entity.Property(e => e.CategoryId).HasColumnName("category_id");
            entity.Property(e => e.CategoryName)
                .HasMaxLength(100)
                .HasColumnName("category_name");
            entity.Property(e => e.CategoryNameTextId).HasColumnName("category_name_text_id");
            entity.Property(e => e.Description)
                .HasMaxLength(100)
                .HasColumnName("description");
            entity.Property(e => e.DescriptionTextId).HasColumnName("description_text_id");
            entity.Property(e => e.IsDisabled).HasColumnName("isDisabled");

            entity.HasOne(d => d.CategoryNameText).WithMany(p => p.DishCategoryCategoryNameTexts)
                .HasForeignKey(d => d.CategoryNameTextId)
                .HasConstraintName("fk_cat_name_text");

            entity.HasOne(d => d.DescriptionText).WithMany(p => p.DishCategoryDescriptionTexts)
                .HasForeignKey(d => d.DescriptionTextId)
                .HasConstraintName("FK_dish_category_i18n_text_text_id");
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

        modelBuilder.Entity<DishTag>(entity =>
        {
            entity.HasKey(e => e.DishTagId).HasName("PRIMARY");
            entity.ToTable("dish_tag");

            entity.HasIndex(e => e.DishId, "FK_dish_tag_dish_dish_id");

            entity.HasIndex(e => e.TagId, "FK_dish_tag_lookup_value_value_id");

            entity.Property(e => e.DishId).HasColumnName("dish_id");
            entity.Property(e => e.DishTagId).HasColumnName("dish_tag_id");
            entity.Property(e => e.TagId).HasColumnName("tag_id");

            entity.HasOne(d => d.Dish).WithMany()
                .HasForeignKey(d => d.DishId)
                .OnDelete(DeleteBehavior.ClientSetNull);

            entity.HasOne(d => d.Tag).WithMany()
                .HasForeignKey(d => d.TagId)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("FK_dish_tag_lookup_value_value_id");
        });

        modelBuilder.Entity<I18nLanguage>(entity =>
        {
            entity.HasKey(e => e.LangCode).HasName("PRIMARY");

            entity.ToTable("i18n_language");

            entity.Property(e => e.LangCode)
                .HasMaxLength(10)
                .HasColumnName("lang_code");
            entity.Property(e => e.IsActive)
                .IsRequired()
                .HasDefaultValueSql("'1'")
                .HasColumnName("is_active");
            entity.Property(e => e.LangName)
                .HasMaxLength(50)
                .HasColumnName("lang_name");
        });

        modelBuilder.Entity<I18nText>(entity =>
        {
            entity.HasKey(e => e.TextId).HasName("PRIMARY");

            entity.ToTable("i18n_text");

            entity.HasIndex(e => e.SourceLangCode, "idx_i18n_text_source_lang");

            entity.HasIndex(e => e.TextKey, "uq_i18n_text_key").IsUnique();

            entity.Property(e => e.TextId).HasColumnName("text_id");
            entity.Property(e => e.Context)
                .HasMaxLength(255)
                .HasColumnName("context");
            entity.Property(e => e.CreatedAt)
                .HasDefaultValueSql("CURRENT_TIMESTAMP")
                .HasColumnType("datetime")
                .HasColumnName("created_at");
            entity.Property(e => e.SourceLangCode)
                .HasMaxLength(10)
                .HasDefaultValueSql("'en'")
                .HasColumnName("source_lang_code");
            entity.Property(e => e.SourceText)
                .HasColumnType("text")
                .HasColumnName("source_text");
            entity.Property(e => e.TextKey)
                .HasMaxLength(200)
                .HasColumnName("text_key");
            entity.Property(e => e.UpdatedAt)
                .ValueGeneratedOnAddOrUpdate()
                .HasDefaultValueSql("CURRENT_TIMESTAMP")
                .HasColumnType("datetime")
                .HasColumnName("updated_at");

            entity.HasOne(d => d.SourceLangCodeNavigation).WithMany(p => p.I18nTexts)
                .HasForeignKey(d => d.SourceLangCode)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("fk_i18n_text_lang");
        });

        modelBuilder.Entity<I18nTranslation>(entity =>
        {
            entity.HasKey(e => new { e.TextId, e.LangCode })
                .HasName("PRIMARY")
                .HasAnnotation("MySql:IndexPrefixLength", new[] { 0, 0 });

            entity.ToTable("i18n_translation");

            entity.HasIndex(e => e.LangCode, "fk_i18n_tr_lang");

            entity.Property(e => e.TextId).HasColumnName("text_id");
            entity.Property(e => e.LangCode)
                .HasMaxLength(10)
                .HasColumnName("lang_code");
            entity.Property(e => e.TranslatedText)
                .HasColumnType("text")
                .HasColumnName("translated_text");
            entity.Property(e => e.UpdatedAt)
                .ValueGeneratedOnAddOrUpdate()
                .HasDefaultValueSql("CURRENT_TIMESTAMP")
                .HasColumnType("datetime")
                .HasColumnName("updated_at");

            entity.HasOne(d => d.LangCodeNavigation).WithMany(p => p.I18nTranslations)
                .HasForeignKey(d => d.LangCode)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("fk_i18n_tr_lang");

            entity.HasOne(d => d.Text).WithMany(p => p.I18nTranslations)
                .HasForeignKey(d => d.TextId)
                .HasConstraintName("fk_i18n_tr_text");
        });

        modelBuilder.Entity<Ingredient>(entity =>
        {
            entity.HasKey(e => e.IngredientId).HasName("PRIMARY");

            entity.ToTable("ingredient");

            entity.HasIndex(e => e.ImageId, "FK_ingredient_image_id");

            entity.HasIndex(e => e.TypeLvId, "FK_ingredient_type_lv_id");

            entity.HasIndex(e => e.IngredientNameTextId, "fk_ingredient_name_text");

            entity.Property(e => e.IngredientId).HasColumnName("ingredient_id");
            entity.Property(e => e.ImageId).HasColumnName("image_id");
            entity.Property(e => e.IngredientName)
                .HasMaxLength(200)
                .HasColumnName("ingredient_name");
            entity.Property(e => e.IngredientNameTextId).HasColumnName("ingredient_name_text_id");
            entity.Property(e => e.TypeLvId).HasColumnName("type_lv_id");
            entity.Property(e => e.Unit)
                .HasMaxLength(20)
                .HasColumnName("unit");

            entity.HasOne(d => d.Image).WithMany(p => p.Ingredients)
                .HasForeignKey(d => d.ImageId)
                .HasConstraintName("FK_ingredient_image_id");

            entity.HasOne(d => d.IngredientNameText).WithMany(p => p.Ingredients)
                .HasForeignKey(d => d.IngredientNameTextId)
                .HasConstraintName("fk_ingredient_name_text");

            entity.HasOne(d => d.TypeLv).WithMany(p => p.Ingredients)
                .HasForeignKey(d => d.TypeLvId)
                .HasConstraintName("FK_ingredient_type_lv_id");
        });

        modelBuilder.Entity<IngredientSupplier>(entity =>
        {
            entity.HasKey(e => e.IngredientSupplierId).HasName("PRIMARY");

            entity.ToTable("ingredient_supplier");

            entity.HasIndex(e => e.IngredientId, "FK_ingredient_supplier_ingredient_ingredient_id");

            entity.HasIndex(e => e.SupplierId, "FK_ingredient_supplier_supplier_supplier_id");

            entity.Property(e => e.IngredientSupplierId)
                .ValueGeneratedNever()
                .HasColumnName("ingredient_supplier_id");
            entity.Property(e => e.CreatedAt)
                .HasColumnType("datetime")
                .HasColumnName("created_at");
            entity.Property(e => e.IngredientId).HasColumnName("ingredient_id");
            entity.Property(e => e.SupplierId).HasColumnName("supplier_id");

            entity.HasOne(d => d.Ingredient).WithMany(p => p.IngredientSuppliers).HasForeignKey(d => d.IngredientId);

            entity.HasOne(d => d.Supplier).WithMany(p => p.IngredientSuppliers).HasForeignKey(d => d.SupplierId);
        });

        modelBuilder.Entity<InventoryTransaction>(entity =>
        {
            entity.HasKey(e => e.TransactionId).HasName("PRIMARY");

            entity.ToTable("inventory_transaction");

            entity.HasIndex(e => e.SupplierId, "FK_inventory_transaction_supplier_supplier_id");

            entity.HasIndex(e => e.CreatedBy, "fk_inventory_transaction_staff");

            entity.HasIndex(e => e.CreatedAt, "idx_inventory_transaction_dir_time");

            entity.HasIndex(e => e.StatusLvId, "idx_inventory_tx_status_lv");

            entity.HasIndex(e => e.TypeLvId, "idx_inventory_tx_type_lv");

            entity.Property(e => e.TransactionId).HasColumnName("transaction_id");
            entity.Property(e => e.CreatedAt)
                .HasDefaultValueSql("CURRENT_TIMESTAMP")
                .HasColumnType("datetime")
                .HasColumnName("created_at");
            entity.Property(e => e.CreatedBy).HasColumnName("created_by");
            entity.Property(e => e.Note)
                .HasMaxLength(255)
                .HasColumnName("note");
            entity.Property(e => e.StatusLvId).HasColumnName("status_lv_id");
            entity.Property(e => e.SupplierId).HasColumnName("supplier_id");
            entity.Property(e => e.TypeLvId).HasColumnName("type_lv_id");

            entity.HasOne(d => d.CreatedByNavigation).WithMany(p => p.InventoryTransactions)
                .HasForeignKey(d => d.CreatedBy)
                .HasConstraintName("fk_inventory_transaction_staff");

            entity.HasOne(d => d.StatusLv).WithMany(p => p.InventoryTransactionStatusLvs)
                .HasForeignKey(d => d.StatusLvId)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("fk_inventory_tx_status_lv");

            entity.HasOne(d => d.Supplier).WithMany(p => p.InventoryTransactions).HasForeignKey(d => d.SupplierId);

            entity.HasOne(d => d.TypeLv).WithMany(p => p.InventoryTransactionTypeLvs)
                .HasForeignKey(d => d.TypeLvId)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("fk_inventory_tx_type_lv");
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

        modelBuilder.Entity<LookupType>(entity =>
        {
            entity.HasKey(e => e.TypeId).HasName("PRIMARY");

            entity.ToTable("lookup_type");

            entity.HasIndex(e => e.TypeDescTextId, "fk_lookup_type_desc_text");

            entity.HasIndex(e => e.TypeNameTextId, "fk_lookup_type_name_text");

            entity.HasIndex(e => e.TypeCode, "uq_lookup_type_code").IsUnique();

            entity.Property(e => e.TypeId).HasColumnName("type_id");
            entity.Property(e => e.Description)
                .HasMaxLength(255)
                .HasColumnName("description");
            entity.Property(e => e.IsConfigurable)
                .HasComment("1 = admin can add/remove values, 0 = controlled enum (statuses, workflows)")
                .HasColumnName("is_configurable");
            entity.Property(e => e.IsSystem)
                .IsRequired()
                .HasDefaultValueSql("'1'")
                .HasComment("1 = system-defined enum type, 0 = user-defined/custom type")
                .HasColumnName("is_system");
            entity.Property(e => e.TypeCode)
                .HasMaxLength(50)
                .HasColumnName("type_code");
            entity.Property(e => e.TypeDescTextId).HasColumnName("type_desc_text_id");
            entity.Property(e => e.TypeName)
                .HasMaxLength(150)
                .HasColumnName("type_name");
            entity.Property(e => e.TypeNameTextId).HasColumnName("type_name_text_id");

            entity.HasOne(d => d.TypeDescText).WithMany(p => p.LookupTypeTypeDescTexts)
                .HasForeignKey(d => d.TypeDescTextId)
                .HasConstraintName("fk_lookup_type_desc_text");

            entity.HasOne(d => d.TypeNameText).WithMany(p => p.LookupTypeTypeNameTexts)
                .HasForeignKey(d => d.TypeNameTextId)
                .HasConstraintName("fk_lookup_type_name_text");
        });

        modelBuilder.Entity<LookupValue>(entity =>
        {
            entity.HasKey(e => e.ValueId).HasName("PRIMARY");

            entity.ToTable("lookup_value");

            entity.HasIndex(e => e.ValueDescTextId, "fk_lookup_value_desc_text");

            entity.HasIndex(e => e.ValueNameTextId, "fk_lookup_value_name_text");

            entity.HasIndex(e => new { e.TypeId, e.IsActive, e.SortOrder }, "idx_lookup_value_type_active");

            entity.HasIndex(e => new { e.TypeId, e.ValueCode }, "uq_lookup_value").IsUnique();

            entity.Property(e => e.ValueId).HasColumnName("value_id");
            entity.Property(e => e.DeletedAt)
                .HasComment("Soft delete timestamp; never hard delete lookup values")
                .HasColumnType("datetime")
                .HasColumnName("deleted_at");
            entity.Property(e => e.Description)
                .HasColumnType("text")
                .HasColumnName("description");
            entity.Property(e => e.IsActive)
                .IsRequired()
                .HasDefaultValueSql("'1'")
                .HasColumnName("is_active");
            entity.Property(e => e.IsSystem)
                .IsRequired()
                .HasDefaultValueSql("'1'")
                .HasComment("1 = system/seeded value, 0 = user-added value")
                .HasColumnName("is_system");
            entity.Property(e => e.Locked)
                .IsRequired()
                .HasDefaultValueSql("'1'")
                .HasComment("1 = value_code cannot be changed and value cannot be deleted")
                .HasColumnName("locked");
            entity.Property(e => e.Meta)
                .HasColumnType("json")
                .HasColumnName("meta");
            entity.Property(e => e.SortOrder).HasColumnName("sort_order");
            entity.Property(e => e.TypeId).HasColumnName("type_id");
            entity.Property(e => e.UpdateAt)
                .HasColumnType("datetime")
                .HasColumnName("update_at");
            entity.Property(e => e.ValueCode)
                .HasMaxLength(50)
                .HasColumnName("value_code");
            entity.Property(e => e.ValueDescTextId).HasColumnName("value_desc_text_id");
            entity.Property(e => e.ValueName)
                .HasMaxLength(150)
                .HasColumnName("value_name");
            entity.Property(e => e.ValueNameTextId).HasColumnName("value_name_text_id");

            entity.HasOne(d => d.Type).WithMany(p => p.LookupValues)
                .HasForeignKey(d => d.TypeId)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("fk_lookup_value_type");

            entity.HasOne(d => d.ValueDescText).WithMany(p => p.LookupValueValueDescTexts)
                .HasForeignKey(d => d.ValueDescTextId)
                .HasConstraintName("fk_lookup_value_desc_text");

            entity.HasOne(d => d.ValueNameText).WithMany(p => p.LookupValueValueNameTexts)
                .HasForeignKey(d => d.ValueNameTextId)
                .HasConstraintName("fk_lookup_value_name_text");
        });

        modelBuilder.Entity<MediaAsset>(entity =>
        {
            entity.HasKey(e => e.MediaId).HasName("PRIMARY");

            entity.ToTable("media_asset");

            entity.HasIndex(e => e.MediaTypeLvId, "idx_media_asset_type_lv");

            entity.Property(e => e.MediaId).HasColumnName("media_id");
            entity.Property(e => e.CreatedAt)
                .HasDefaultValueSql("CURRENT_TIMESTAMP")
                .HasColumnType("datetime")
                .HasColumnName("created_at");
            entity.Property(e => e.DurationSec).HasColumnName("duration_sec");
            entity.Property(e => e.Height).HasColumnName("height");
            entity.Property(e => e.MediaTypeLvId).HasColumnName("media_type_lv_id");
            entity.Property(e => e.MimeType)
                .HasMaxLength(100)
                .HasColumnName("mime_type");
            entity.Property(e => e.Url)
                .HasMaxLength(500)
                .HasColumnName("url");
            entity.Property(e => e.Width).HasColumnName("width");

            entity.HasOne(d => d.MediaTypeLv).WithMany(p => p.MediaAssets)
                .HasForeignKey(d => d.MediaTypeLvId)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("fk_media_asset_type_lv");
        });

        modelBuilder.Entity<Order>(entity =>
        {
            entity.HasKey(e => e.OrderId).HasName("PRIMARY");

            entity.ToTable("orders");

            entity.HasIndex(e => e.CustomerId, "FK_orders_customer_id");

            entity.HasIndex(e => e.CreatedAt, "idx_order_status");

            entity.HasIndex(e => e.SourceLvId, "idx_orders_source_lv");

            entity.HasIndex(e => e.OrderStatusLvId, "idx_orders_status_lv");

            entity.HasIndex(e => e.StaffId, "orders_ibfk_2");

            entity.HasIndex(e => e.TableId, "table_id");

            entity.Property(e => e.OrderId).HasColumnName("order_id");
            entity.Property(e => e.CreatedAt)
                .HasDefaultValueSql("CURRENT_TIMESTAMP")
                .HasColumnType("datetime")
                .HasColumnName("created_at");
            entity.Property(e => e.CustomerId).HasColumnName("customer_id");
            entity.Property(e => e.OrderStatusLvId).HasColumnName("order_status_lv_id");
            entity.Property(e => e.SourceLvId).HasColumnName("source_lv_id");
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

            entity.HasOne(d => d.OrderStatusLv).WithMany(p => p.OrderOrderStatusLvs)
                .HasForeignKey(d => d.OrderStatusLvId)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("fk_orders_status_lv");

            entity.HasOne(d => d.SourceLv).WithMany(p => p.OrderSourceLvs)
                .HasForeignKey(d => d.SourceLvId)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("fk_orders_source_lv");

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

            entity.HasIndex(e => e.ItemStatusLvId, "idx_order_item_status_lv");

            entity.HasIndex(e => e.OrderId, "order_id");

            entity.Property(e => e.OrderItemId).HasColumnName("order_item_id");
            entity.Property(e => e.DishId).HasColumnName("dish_id");
            entity.Property(e => e.ItemStatus)
                .HasDefaultValueSql("'1'")
                .HasComment("OrderItemStatus: 1=CREATED,2=IN_PROGRESS,3=READY,4=SERVED,5=REJECTED")
                .HasColumnName("item_status");
            entity.Property(e => e.ItemStatusLvId).HasColumnName("item_status_lv_id");
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

            entity.HasOne(d => d.ItemStatusLv).WithMany(p => p.OrderItems)
                .HasForeignKey(d => d.ItemStatusLvId)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("fk_order_item_status_lv");

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

            entity.HasIndex(e => e.MethodLvId, "idx_payment_method_lv");

            entity.HasIndex(e => e.OrderId, "payment_ibfk_1");

            entity.Property(e => e.PaymentId).HasColumnName("payment_id");
            entity.Property(e => e.ChangeAmount)
                .HasPrecision(14, 2)
                .HasColumnName("change_amount");
            entity.Property(e => e.MethodLvId).HasColumnName("method_lv_id");
            entity.Property(e => e.OrderId).HasColumnName("order_id");
            entity.Property(e => e.PaidAt)
                .HasDefaultValueSql("CURRENT_TIMESTAMP")
                .HasColumnType("datetime")
                .HasColumnName("paid_at");
            entity.Property(e => e.ReceivedAmount)
                .HasPrecision(14, 2)
                .HasColumnName("received_amount");

            entity.HasOne(d => d.MethodLv).WithMany(p => p.Payments)
                .HasForeignKey(d => d.MethodLvId)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("fk_payment_method_lv");

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

            entity.HasIndex(e => e.PromoDescTextId, "fk_promo_desc_text");

            entity.HasIndex(e => e.PromoNameTextId, "fk_promo_name_text");

            entity.HasIndex(e => e.PromotionStatusLvId, "idx_promotion_status_lv");

            entity.HasIndex(e => e.TypeLvId, "idx_promotion_type_lv");

            entity.HasIndex(e => e.PromoCode, "promo_code").IsUnique();

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
            entity.Property(e => e.PromoDescTextId).HasColumnName("promo_desc_text_id");
            entity.Property(e => e.PromoName)
                .HasMaxLength(200)
                .HasColumnName("promo_name");
            entity.Property(e => e.PromoNameTextId).HasColumnName("promo_name_text_id");
            entity.Property(e => e.PromotionStatusLvId).HasColumnName("promotion_status_lv_id");
            entity.Property(e => e.StartTime)
                .HasColumnType("datetime")
                .HasColumnName("start_time");
            entity.Property(e => e.TypeLvId).HasColumnName("type_lv_id");
            entity.Property(e => e.UsedCount)
                .HasDefaultValueSql("'0'")
                .HasColumnName("used_count");

            entity.HasOne(d => d.PromoDescText).WithMany(p => p.PromotionPromoDescTexts)
                .HasForeignKey(d => d.PromoDescTextId)
                .HasConstraintName("fk_promo_desc_text");

            entity.HasOne(d => d.PromoNameText).WithMany(p => p.PromotionPromoNameTexts)
                .HasForeignKey(d => d.PromoNameTextId)
                .HasConstraintName("fk_promo_name_text");

            entity.HasOne(d => d.PromotionStatusLv).WithMany(p => p.PromotionPromotionStatusLvs)
                .HasForeignKey(d => d.PromotionStatusLvId)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("fk_promotion_status_lv");

            entity.HasOne(d => d.TypeLv).WithMany(p => p.PromotionTypeLvs)
                .HasForeignKey(d => d.TypeLvId)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("fk_promotion_type_lv");
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

            entity.HasIndex(e => e.SourceLvId, "idx_reservation_source_lv");

            entity.HasIndex(e => e.ReservationStatusLvId, "idx_reservation_status_lv");

            entity.HasIndex(e => e.ReservedTime, "idx_reservation_time");

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
            entity.Property(e => e.ReservationStatusLvId).HasColumnName("reservation_status_lv_id");
            entity.Property(e => e.ReservedTime)
                .HasColumnType("datetime")
                .HasColumnName("reserved_time");
            entity.Property(e => e.SourceLvId).HasColumnName("source_lv_id");

            entity.HasOne(d => d.Customer).WithMany(p => p.Reservations)
                .HasForeignKey(d => d.CustomerId)
                .HasConstraintName("reservation_ibfk_1");

            entity.HasOne(d => d.ReservationStatusLv).WithMany(p => p.ReservationReservationStatusLvs)
                .HasForeignKey(d => d.ReservationStatusLvId)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("fk_reservation_status_lv");

            entity.HasOne(d => d.SourceLv).WithMany(p => p.ReservationSourceLvs)
                .HasForeignKey(d => d.SourceLvId)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("fk_reservation_source_lv");

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

            entity.HasIndex(e => e.TableStatusLvId, "idx_restaurant_table_status_lv");

            entity.HasIndex(e => e.TableTypeLvId, "idx_restaurant_table_type_lv");

            entity.HasIndex(e => e.TableCode, "table_code").IsUnique();

            entity.Property(e => e.TableId).HasColumnName("table_id");
            entity.Property(e => e.Capacity).HasColumnName("capacity");
            entity.Property(e => e.IsOnline)
                .IsRequired()
                .HasDefaultValueSql("'1'")
                .HasColumnName("isOnline");
            entity.Property(e => e.TableCode)
                .HasMaxLength(50)
                .HasColumnName("table_code");
            entity.Property(e => e.TableQrImg).HasColumnName("table_qr_img");
            entity.Property(e => e.TableStatusLvId).HasColumnName("table_status_lv_id");
            entity.Property(e => e.TableTypeLvId).HasColumnName("table_type_lv_id");

            entity.HasOne(d => d.TableQrImgNavigation).WithMany(p => p.RestaurantTables)
                .HasForeignKey(d => d.TableQrImg)
                .OnDelete(DeleteBehavior.Cascade)
                .HasConstraintName("FK_restaurant_table_table_qr_img");

            entity.HasOne(d => d.TableStatusLv).WithMany(p => p.RestaurantTableTableStatusLvs)
                .HasForeignKey(d => d.TableStatusLvId)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("fk_restaurant_table_status_lv");

            entity.HasOne(d => d.TableTypeLv).WithMany(p => p.RestaurantTableTableTypeLvs)
                .HasForeignKey(d => d.TableTypeLvId)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("fk_restaurant_table_type_lv");
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

            entity.HasIndex(e => e.SeverityLvId, "idx_service_error_severity_lv");

            entity.HasIndex(e => new { e.StaffId, e.CreatedAt }, "idx_service_error_staff");

            entity.HasIndex(e => e.OrderItemId, "order_item_id");

            entity.HasIndex(e => e.ResolvedBy, "service_error_ibfk_7");

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
            entity.Property(e => e.SeverityLvId).HasColumnName("severity_lv_id");
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

            entity.HasOne(d => d.SeverityLv).WithMany(p => p.ServiceErrors)
                .HasForeignKey(d => d.SeverityLvId)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("fk_service_error_severity_lv");

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

            entity.HasIndex(e => e.CategoryDescTextId, "fk_sec_desc_text");

            entity.HasIndex(e => e.CategoryNameTextId, "fk_sec_name_text");

            entity.Property(e => e.CategoryId).HasColumnName("category_id");
            entity.Property(e => e.CategoryCode)
                .HasMaxLength(50)
                .HasColumnName("category_code");
            entity.Property(e => e.CategoryDescTextId).HasColumnName("category_desc_text_id");
            entity.Property(e => e.CategoryName)
                .HasMaxLength(150)
                .HasColumnName("category_name");
            entity.Property(e => e.CategoryNameTextId).HasColumnName("category_name_text_id");
            entity.Property(e => e.Description)
                .HasMaxLength(255)
                .HasColumnName("description");

            entity.HasOne(d => d.CategoryDescText).WithMany(p => p.ServiceErrorCategoryCategoryDescTexts)
                .HasForeignKey(d => d.CategoryDescTextId)
                .HasConstraintName("fk_sec_desc_text");

            entity.HasOne(d => d.CategoryNameText).WithMany(p => p.ServiceErrorCategoryCategoryNameTexts)
                .HasForeignKey(d => d.CategoryNameTextId)
                .HasConstraintName("fk_sec_name_text");
        });

        modelBuilder.Entity<StaffAccount>(entity =>
        {
            entity.HasKey(e => e.AccountId).HasName("PRIMARY");

            entity.ToTable("staff_account");

            entity.HasIndex(e => e.Email, "email").IsUnique();

            entity.HasIndex(e => e.AccountStatusLvId, "idx_staff_account_status_lv");

            entity.HasIndex(e => e.RoleId, "role_id");

            entity.HasIndex(e => e.Username, "username").IsUnique();

            entity.Property(e => e.AccountId).HasColumnName("account_id");
            entity.Property(e => e.AccountStatusLvId).HasColumnName("account_status_lv_id");
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

        modelBuilder.Entity<SystemSetting>(entity =>
        {
            entity.HasKey(e => e.SettingId).HasName("PRIMARY");

            entity.ToTable("system_setting");

            entity.HasIndex(e => e.UpdatedBy, "fk_setting_updated_by");

            entity.HasIndex(e => e.SettingKey, "uq_setting_key").IsUnique();

            entity.Property(e => e.SettingId).HasColumnName("setting_id");
            entity.Property(e => e.Description)
                .HasMaxLength(255)
                .HasColumnName("description");
            entity.Property(e => e.IsSensitive).HasColumnName("is_sensitive");
            entity.Property(e => e.SettingKey)
                .HasMaxLength(100)
                .HasColumnName("setting_key");
            entity.Property(e => e.UpdatedAt)
                .ValueGeneratedOnAddOrUpdate()
                .HasDefaultValueSql("CURRENT_TIMESTAMP")
                .HasColumnType("datetime")
                .HasColumnName("updated_at");
            entity.Property(e => e.UpdatedBy).HasColumnName("updated_by");
            entity.Property(e => e.ValueBool).HasColumnName("value_bool");
            entity.Property(e => e.ValueDecimal)
                .HasPrecision(18, 6)
                .HasColumnName("value_decimal");
            entity.Property(e => e.ValueInt).HasColumnName("value_int");
            entity.Property(e => e.ValueJson)
                .HasColumnType("json")
                .HasColumnName("value_json");
            entity.Property(e => e.ValueString)
                .HasMaxLength(500)
                .HasColumnName("value_string");
            entity.Property(e => e.ValueType)
                .HasColumnType("enum('STRING','INT','DECIMAL','BOOL','JSON','DATETIME')")
                .HasColumnName("value_type");

            entity.HasOne(d => d.UpdatedByNavigation).WithMany(p => p.SystemSettings)
                .HasForeignKey(d => d.UpdatedBy)
                .HasConstraintName("fk_setting_updated_by");
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
