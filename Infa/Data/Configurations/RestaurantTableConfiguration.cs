using Core.Entity;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Infa.Data.Configurations;

public sealed class RestaurantTableConfiguration : IEntityTypeConfiguration<RestaurantTable>
{
    public void Configure(EntityTypeBuilder<RestaurantTable> entity)
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
        entity.Property(e => e.ZoneLvId).HasColumnName("zone_lv_id");

        entity.Property(e => e.QrToken)
        .HasMaxLength(255)
        .HasColumnName("qr_token");

        entity.Property(e => e.CreatedAt)
        .HasDefaultValueSql("CURRENT_TIMESTAMP")
        .HasColumnType("datetime")
        .HasColumnName("created_at");

        entity.Property(e => e.UpdatedAt)
        .ValueGeneratedOnAddOrUpdate()
        .HasDefaultValueSql("CURRENT_TIMESTAMP")
        .HasColumnType("datetime")
        .HasColumnName("updated_at");

        entity.Property(e => e.UpdatedByStaffId)
        .HasColumnName("updated_by_staff_id");

        entity.HasOne(d => d.UpdatedByStaff)
        .WithMany()
        .HasForeignKey(d => d.UpdatedByStaffId)
        .HasConstraintName("fk_restaurant_table_updated_by_staff");

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

        entity.HasOne(d => d.ZoneLv).WithMany(p => p.RestaurantTableZoneLvs)
        .HasForeignKey(d => d.ZoneLvId)
        .OnDelete(DeleteBehavior.ClientSetNull)
        .HasConstraintName("fk_restaurant_table_zone_lv");
    }
}
