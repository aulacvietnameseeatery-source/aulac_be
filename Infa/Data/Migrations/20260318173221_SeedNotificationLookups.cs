using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Infa.Data.Migrations
{
    /// <inheritdoc />
    public partial class SeedNotificationLookups : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            // ================================================================
            // Seed LookupType 25 = NOTIFICATION_TYPE, 26 = NOTIFICATION_PRIORITY
            // with full I18n support (en source + vi/fr translations)
            // Uses INSERT IGNORE to be safe for re-runs
            // ================================================================

            migrationBuilder.Sql(@"
-- ========================================
-- I18nText: LookupType names & descriptions
-- ========================================

-- NOTIFICATION_TYPE type name
INSERT INTO i18n_text (text_key, source_lang_code, source_text, context, created_at, updated_at)
VALUES ('lookup_type.type_name.notification_type', 'en', 'Notification Type', 'Enum Type Name', NOW(), NOW());
SET @ntTypeNameTextId = LAST_INSERT_ID();

INSERT INTO i18n_translation (text_id, lang_code, translated_text, updated_at)
VALUES (@ntTypeNameTextId, 'vi', 'Loại Thông Báo', NOW()),
       (@ntTypeNameTextId, 'fr', 'Type de Notification', NOW());

-- NOTIFICATION_TYPE type desc
INSERT INTO i18n_text (text_key, source_lang_code, source_text, context, created_at, updated_at)
VALUES ('lookup_type.type_desc.notification_type', 'en', 'Type of notification event', 'Enum Type Description', NOW(), NOW());
SET @ntTypeDescTextId = LAST_INSERT_ID();

INSERT INTO i18n_translation (text_id, lang_code, translated_text, updated_at)
VALUES (@ntTypeDescTextId, 'vi', 'Loại sự kiện thông báo', NOW()),
       (@ntTypeDescTextId, 'fr', 'Type d''événement de notification', NOW());

-- NOTIFICATION_PRIORITY type name
INSERT INTO i18n_text (text_key, source_lang_code, source_text, context, created_at, updated_at)
VALUES ('lookup_type.type_name.notification_priority', 'en', 'Notification Priority', 'Enum Type Name', NOW(), NOW());
SET @npTypeNameTextId = LAST_INSERT_ID();

INSERT INTO i18n_translation (text_id, lang_code, translated_text, updated_at)
VALUES (@npTypeNameTextId, 'vi', 'Mức Ưu Tiên Thông Báo', NOW()),
       (@npTypeNameTextId, 'fr', 'Priorité de Notification', NOW());

-- NOTIFICATION_PRIORITY type desc
INSERT INTO i18n_text (text_key, source_lang_code, source_text, context, created_at, updated_at)
VALUES ('lookup_type.type_desc.notification_priority', 'en', 'Priority level for notifications', 'Enum Type Description', NOW(), NOW());
SET @npTypeDescTextId = LAST_INSERT_ID();

INSERT INTO i18n_translation (text_id, lang_code, translated_text, updated_at)
VALUES (@npTypeDescTextId, 'vi', 'Mức độ ưu tiên cho thông báo', NOW()),
       (@npTypeDescTextId, 'fr', 'Niveau de priorité des notifications', NOW());

-- ========================================
-- LookupType rows
-- ========================================

INSERT IGNORE INTO lookup_type (type_id, type_code, type_name, description, is_configurable, is_system, type_name_text_id, type_desc_text_id)
VALUES
  (25, 'NOTIFICATION_TYPE', 'Notification Type', 'notifications.type', 0, 1, @ntTypeNameTextId, @ntTypeDescTextId),
  (26, 'NOTIFICATION_PRIORITY', 'Notification Priority', 'notifications.priority', 0, 1, @npTypeNameTextId, @npTypeDescTextId);

-- ========================================
-- I18nText: LookupValue names for NotificationType (15 values)
-- ========================================

INSERT INTO i18n_text (text_key, source_lang_code, source_text, context, created_at, updated_at) VALUES
('lookup_value.name.notification_type.new_order', 'en', 'New Order', 'Enum Value Name', NOW(), NOW());
SET @v1 = LAST_INSERT_ID();
INSERT INTO i18n_translation (text_id, lang_code, translated_text, updated_at) VALUES (@v1, 'vi', 'Đơn hàng mới', NOW()), (@v1, 'fr', 'Nouvelle commande', NOW());

INSERT INTO i18n_text (text_key, source_lang_code, source_text, context, created_at, updated_at) VALUES
('lookup_value.name.notification_type.order_cancelled', 'en', 'Order Cancelled', 'Enum Value Name', NOW(), NOW());
SET @v2 = LAST_INSERT_ID();
INSERT INTO i18n_translation (text_id, lang_code, translated_text, updated_at) VALUES (@v2, 'vi', 'Đơn hàng bị hủy', NOW()), (@v2, 'fr', 'Commande annulée', NOW());

INSERT INTO i18n_text (text_key, source_lang_code, source_text, context, created_at, updated_at) VALUES
('lookup_value.name.notification_type.order_item_ready', 'en', 'Item Ready', 'Enum Value Name', NOW(), NOW());
SET @v3 = LAST_INSERT_ID();
INSERT INTO i18n_translation (text_id, lang_code, translated_text, updated_at) VALUES (@v3, 'vi', 'Món ăn đã sẵn sàng', NOW()), (@v3, 'fr', 'Plat prêt', NOW());

INSERT INTO i18n_text (text_key, source_lang_code, source_text, context, created_at, updated_at) VALUES
('lookup_value.name.notification_type.order_item_rejected', 'en', 'Item Rejected', 'Enum Value Name', NOW(), NOW());
SET @v4 = LAST_INSERT_ID();
INSERT INTO i18n_translation (text_id, lang_code, translated_text, updated_at) VALUES (@v4, 'vi', 'Món ăn bị từ chối', NOW()), (@v4, 'fr', 'Plat rejeté', NOW());

INSERT INTO i18n_text (text_key, source_lang_code, source_text, context, created_at, updated_at) VALUES
('lookup_value.name.notification_type.all_items_ready', 'en', 'All Items Ready', 'Enum Value Name', NOW(), NOW());
SET @v5 = LAST_INSERT_ID();
INSERT INTO i18n_translation (text_id, lang_code, translated_text, updated_at) VALUES (@v5, 'vi', 'Tất cả món đã sẵn sàng', NOW()), (@v5, 'fr', 'Tous les plats prêts', NOW());

INSERT INTO i18n_text (text_key, source_lang_code, source_text, context, created_at, updated_at) VALUES
('lookup_value.name.notification_type.payment_completed', 'en', 'Payment Completed', 'Enum Value Name', NOW(), NOW());
SET @v6 = LAST_INSERT_ID();
INSERT INTO i18n_translation (text_id, lang_code, translated_text, updated_at) VALUES (@v6, 'vi', 'Thanh toán hoàn tất', NOW()), (@v6, 'fr', 'Paiement effectué', NOW());

INSERT INTO i18n_text (text_key, source_lang_code, source_text, context, created_at, updated_at) VALUES
('lookup_value.name.notification_type.reservation_created', 'en', 'Reservation Created', 'Enum Value Name', NOW(), NOW());
SET @v7 = LAST_INSERT_ID();
INSERT INTO i18n_translation (text_id, lang_code, translated_text, updated_at) VALUES (@v7, 'vi', 'Đặt bàn mới', NOW()), (@v7, 'fr', 'Réservation créée', NOW());

INSERT INTO i18n_text (text_key, source_lang_code, source_text, context, created_at, updated_at) VALUES
('lookup_value.name.notification_type.reservation_status_changed', 'en', 'Reservation Updated', 'Enum Value Name', NOW(), NOW());
SET @v8 = LAST_INSERT_ID();
INSERT INTO i18n_translation (text_id, lang_code, translated_text, updated_at) VALUES (@v8, 'vi', 'Đặt bàn cập nhật', NOW()), (@v8, 'fr', 'Réservation mise à jour', NOW());

INSERT INTO i18n_text (text_key, source_lang_code, source_text, context, created_at, updated_at) VALUES
('lookup_value.name.notification_type.reservation_reminder', 'en', 'Reservation Reminder', 'Enum Value Name', NOW(), NOW());
SET @v9 = LAST_INSERT_ID();
INSERT INTO i18n_translation (text_id, lang_code, translated_text, updated_at) VALUES (@v9, 'vi', 'Nhắc nhở đặt bàn', NOW()), (@v9, 'fr', 'Rappel de réservation', NOW());

INSERT INTO i18n_text (text_key, source_lang_code, source_text, context, created_at, updated_at) VALUES
('lookup_value.name.notification_type.table_status_changed', 'en', 'Table Status Changed', 'Enum Value Name', NOW(), NOW());
SET @v10 = LAST_INSERT_ID();
INSERT INTO i18n_translation (text_id, lang_code, translated_text, updated_at) VALUES (@v10, 'vi', 'Trạng thái bàn thay đổi', NOW()), (@v10, 'fr', 'Statut de table modifié', NOW());

INSERT INTO i18n_text (text_key, source_lang_code, source_text, context, created_at, updated_at) VALUES
('lookup_value.name.notification_type.low_stock_alert', 'en', 'Low Stock Alert', 'Enum Value Name', NOW(), NOW());
SET @v11 = LAST_INSERT_ID();
INSERT INTO i18n_translation (text_id, lang_code, translated_text, updated_at) VALUES (@v11, 'vi', 'Cảnh báo tồn kho thấp', NOW()), (@v11, 'fr', 'Alerte stock bas', NOW());

INSERT INTO i18n_text (text_key, source_lang_code, source_text, context, created_at, updated_at) VALUES
('lookup_value.name.notification_type.dish_out_of_stock', 'en', 'Dish Out of Stock', 'Enum Value Name', NOW(), NOW());
SET @v12 = LAST_INSERT_ID();
INSERT INTO i18n_translation (text_id, lang_code, translated_text, updated_at) VALUES (@v12, 'vi', 'Món ăn hết hàng', NOW()), (@v12, 'fr', 'Plat en rupture', NOW());

INSERT INTO i18n_text (text_key, source_lang_code, source_text, context, created_at, updated_at) VALUES
('lookup_value.name.notification_type.shift_assigned', 'en', 'Shift Assigned', 'Enum Value Name', NOW(), NOW());
SET @v13 = LAST_INSERT_ID();
INSERT INTO i18n_translation (text_id, lang_code, translated_text, updated_at) VALUES (@v13, 'vi', 'Phân ca làm việc', NOW()), (@v13, 'fr', 'Quart assigné', NOW());

INSERT INTO i18n_text (text_key, source_lang_code, source_text, context, created_at, updated_at) VALUES
('lookup_value.name.notification_type.attendance_alert', 'en', 'Attendance Alert', 'Enum Value Name', NOW(), NOW());
SET @v14 = LAST_INSERT_ID();
INSERT INTO i18n_translation (text_id, lang_code, translated_text, updated_at) VALUES (@v14, 'vi', 'Cảnh báo chấm công', NOW()), (@v14, 'fr', 'Alerte de présence', NOW());

INSERT INTO i18n_text (text_key, source_lang_code, source_text, context, created_at, updated_at) VALUES
('lookup_value.name.notification_type.system_alert', 'en', 'System Alert', 'Enum Value Name', NOW(), NOW());
SET @v15 = LAST_INSERT_ID();
INSERT INTO i18n_translation (text_id, lang_code, translated_text, updated_at) VALUES (@v15, 'vi', 'Cảnh báo hệ thống', NOW()), (@v15, 'fr', 'Alerte système', NOW());

-- ========================================
-- LookupValue rows for NOTIFICATION_TYPE (type_id = 25)
-- ========================================

INSERT IGNORE INTO lookup_value (type_id, value_code, value_name, sort_order, is_active, is_system, locked, value_name_text_id) VALUES
(25, 'NEW_ORDER',                    'New Order',              1,  1, 1, 1, @v1),
(25, 'ORDER_CANCELLED',              'Order Cancelled',        2,  1, 1, 1, @v2),
(25, 'ORDER_ITEM_READY',             'Item Ready',             3,  1, 1, 1, @v3),
(25, 'ORDER_ITEM_REJECTED',          'Item Rejected',          4,  1, 1, 1, @v4),
(25, 'ALL_ITEMS_READY',              'All Items Ready',        5,  1, 1, 1, @v5),
(25, 'PAYMENT_COMPLETED',            'Payment Completed',      6,  1, 1, 1, @v6),
(25, 'RESERVATION_CREATED',          'Reservation Created',    7,  1, 1, 1, @v7),
(25, 'RESERVATION_STATUS_CHANGED',   'Reservation Updated',    8,  1, 1, 1, @v8),
(25, 'RESERVATION_REMINDER',         'Reservation Reminder',   9,  1, 1, 1, @v9),
(25, 'TABLE_STATUS_CHANGED',         'Table Status Changed',   10, 1, 1, 1, @v10),
(25, 'LOW_STOCK_ALERT',              'Low Stock Alert',        11, 1, 1, 1, @v11),
(25, 'DISH_OUT_OF_STOCK',            'Dish Out of Stock',      12, 1, 1, 1, @v12),
(25, 'SHIFT_ASSIGNED',               'Shift Assigned',         13, 1, 1, 1, @v13),
(25, 'ATTENDANCE_ALERT',             'Attendance Alert',       14, 1, 1, 1, @v14),
(25, 'SYSTEM_ALERT',                 'System Alert',           15, 1, 1, 1, @v15);

-- ========================================
-- I18nText: LookupValue names for NotificationPriority (4 values)
-- ========================================

INSERT INTO i18n_text (text_key, source_lang_code, source_text, context, created_at, updated_at) VALUES
('lookup_value.name.notification_priority.low', 'en', 'Low', 'Enum Value Name', NOW(), NOW());
SET @p1 = LAST_INSERT_ID();
INSERT INTO i18n_translation (text_id, lang_code, translated_text, updated_at) VALUES (@p1, 'vi', 'Thấp', NOW()), (@p1, 'fr', 'Basse', NOW());

INSERT INTO i18n_text (text_key, source_lang_code, source_text, context, created_at, updated_at) VALUES
('lookup_value.name.notification_priority.normal', 'en', 'Normal', 'Enum Value Name', NOW(), NOW());
SET @p2 = LAST_INSERT_ID();
INSERT INTO i18n_translation (text_id, lang_code, translated_text, updated_at) VALUES (@p2, 'vi', 'Bình thường', NOW()), (@p2, 'fr', 'Normale', NOW());

INSERT INTO i18n_text (text_key, source_lang_code, source_text, context, created_at, updated_at) VALUES
('lookup_value.name.notification_priority.high', 'en', 'High', 'Enum Value Name', NOW(), NOW());
SET @p3 = LAST_INSERT_ID();
INSERT INTO i18n_translation (text_id, lang_code, translated_text, updated_at) VALUES (@p3, 'vi', 'Cao', NOW()), (@p3, 'fr', 'Haute', NOW());

INSERT INTO i18n_text (text_key, source_lang_code, source_text, context, created_at, updated_at) VALUES
('lookup_value.name.notification_priority.critical', 'en', 'Critical', 'Enum Value Name', NOW(), NOW());
SET @p4 = LAST_INSERT_ID();
INSERT INTO i18n_translation (text_id, lang_code, translated_text, updated_at) VALUES (@p4, 'vi', 'Nghiêm trọng', NOW()), (@p4, 'fr', 'Critique', NOW());

-- ========================================
-- LookupValue rows for NOTIFICATION_PRIORITY (type_id = 26)
-- ========================================

INSERT IGNORE INTO lookup_value (type_id, value_code, value_name, sort_order, is_active, is_system, locked, value_name_text_id) VALUES
(26, 'LOW',      'Low',      1, 1, 1, 1, @p1),
(26, 'NORMAL',   'Normal',   2, 1, 1, 1, @p2),
(26, 'HIGH',     'High',     3, 1, 1, 1, @p3),
(26, 'CRITICAL', 'Critical', 4, 1, 1, 1, @p4);
");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.Sql(@"
-- Remove lookup values
DELETE FROM lookup_value WHERE type_id IN (25, 26);

-- Remove lookup types
DELETE FROM lookup_type WHERE type_id IN (25, 26);

-- Remove i18n translations and texts (by text_key prefix)
DELETE t FROM i18n_translation t
INNER JOIN i18n_text txt ON t.text_id = txt.text_id
WHERE txt.text_key LIKE 'lookup_type.%notification%'
   OR txt.text_key LIKE 'lookup_value.name.notification_%';

DELETE FROM i18n_text
WHERE text_key LIKE 'lookup_type.%notification%'
   OR text_key LIKE 'lookup_value.name.notification_%';
");
        }
    }
}
