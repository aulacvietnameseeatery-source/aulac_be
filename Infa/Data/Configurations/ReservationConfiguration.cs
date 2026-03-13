using Core.Entity;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Infa.Data.Configurations;

public sealed class ReservationConfiguration : IEntityTypeConfiguration<Reservation>
{
    public void Configure(EntityTypeBuilder<Reservation> entity)
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

        entity.Property(e => e.Notes)
            .HasMaxLength(500)
            .HasColumnName("notes");

        entity.Property(e => e.ReservationStatusLvId)
            .HasColumnName("reservation_status_lv_id");
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
    }
}
