-- =============================================================
-- Notification Module - Database Migration Script
-- Target: MySQL 8.0+
-- =============================================================

CREATE TABLE IF NOT EXISTS `notifications` (
    `notification_id` BIGINT NOT NULL AUTO_INCREMENT,
    `type` VARCHAR(50) NOT NULL,
    `title` VARCHAR(200) NOT NULL,
    `body` VARCHAR(1000) NULL,
    `priority` VARCHAR(20) NOT NULL DEFAULT 'Normal',
    `require_ack` TINYINT(1) NOT NULL DEFAULT 0,
    `sound_key` VARCHAR(100) NULL,
    `action_url` VARCHAR(500) NULL,
    `entity_type` VARCHAR(50) NULL,
    `entity_id` VARCHAR(50) NULL,
    `metadata_json` JSON NULL,
    `target_permissions` VARCHAR(500) NULL COMMENT 'Comma-separated permission codes, e.g. ORDER:READ,ORDER:EDIT',
    `target_user_ids` VARCHAR(500) NULL COMMENT 'Comma-separated user IDs for user-specific targeting',
    `created_at` DATETIME NOT NULL DEFAULT CURRENT_TIMESTAMP,
    PRIMARY KEY (`notification_id`),
    INDEX `idx_notifications_created_at` (`created_at` DESC),
    INDEX `idx_notifications_type` (`type`)
) ENGINE=InnoDB DEFAULT CHARSET=utf8mb4 COLLATE=utf8mb4_unicode_ci;

CREATE TABLE IF NOT EXISTS `notification_read_states` (
    `notification_read_state_id` BIGINT NOT NULL AUTO_INCREMENT,
    `notification_id` BIGINT NOT NULL,
    `user_id` BIGINT NOT NULL,
    `is_read` TINYINT(1) NOT NULL DEFAULT 0,
    `is_acknowledged` TINYINT(1) NOT NULL DEFAULT 0,
    `read_at` DATETIME NULL,
    `acknowledged_at` DATETIME NULL,
    `created_at` DATETIME NOT NULL DEFAULT CURRENT_TIMESTAMP,
    PRIMARY KEY (`notification_read_state_id`),
    UNIQUE INDEX `uq_notification_user` (`notification_id`, `user_id`),
    INDEX `idx_nrs_user_is_read` (`user_id`, `is_read`),
    INDEX `idx_nrs_notification_id` (`notification_id`),
    CONSTRAINT `fk_nrs_notification` FOREIGN KEY (`notification_id`)
        REFERENCES `notifications` (`notification_id`)
        ON DELETE CASCADE
) ENGINE=InnoDB DEFAULT CHARSET=utf8mb4 COLLATE=utf8mb4_unicode_ci;
