using Core.Entity;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Infa.Data.Configurations;

public sealed class DishConfiguration : IEntityTypeConfiguration<Dish>
{
    public void Configure(EntityTypeBuilder<Dish> entity)
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
    }
}
