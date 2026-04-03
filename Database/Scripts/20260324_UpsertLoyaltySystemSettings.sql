-- Upsert loyalty settings used by PaymentService
-- - loyalty.enabled: BOOL (0/1)
-- - loyalty.point_base_amount: DECIMAL (amount per 1 point)
-- - loyalty.redemption_enabled: BOOL (0/1)
-- - loyalty.points_to_currency_ratio: DECIMAL (amount per 1 redeemed point)
-- - loyalty.min_redemption_points: INT

START TRANSACTION;

INSERT INTO system_setting
(
    setting_key,
    value_type,
    value_string,
    value_int,
    value_decimal,
    value_bool,
    value_json,
    description,
    is_sensitive,
    updated_at,
    updated_by
)
VALUES
(
    'loyalty.enabled',
    'BOOL',
    NULL,
    NULL,
    NULL,
    0,
    NULL,
    'Enable or disable loyalty point accrual.',
    0,
    UTC_TIMESTAMP(),
    NULL
)
ON DUPLICATE KEY UPDATE
    value_type = 'BOOL',
    value_string = NULL,
    value_int = NULL,
    value_decimal = NULL,
    value_bool = VALUES(value_bool),
    value_json = NULL,
    description = VALUES(description),
    is_sensitive = VALUES(is_sensitive),
    updated_at = UTC_TIMESTAMP(),
    updated_by = NULL;

INSERT INTO system_setting
(
    setting_key,
    value_type,
    value_string,
    value_int,
    value_decimal,
    value_bool,
    value_json,
    description,
    is_sensitive,
    updated_at,
    updated_by
)
VALUES
(
    'loyalty.point_base_amount',
    'DECIMAL',
    NULL,
    NULL,
    10.00,
    NULL,
    NULL,
    'Base amount to earn 1 loyalty point.',
    0,
    UTC_TIMESTAMP(),
    NULL
)
ON DUPLICATE KEY UPDATE
    value_type = 'DECIMAL',
    value_string = NULL,
    value_int = NULL,
    value_decimal = VALUES(value_decimal),
    value_bool = NULL,
    value_json = NULL,
    description = VALUES(description),
    is_sensitive = VALUES(is_sensitive),
    updated_at = UTC_TIMESTAMP(),
    updated_by = NULL;

COMMIT;

START TRANSACTION;

INSERT INTO system_setting
(
    setting_key,
    value_type,
    value_string,
    value_int,
    value_decimal,
    value_bool,
    value_json,
    description,
    is_sensitive,
    updated_at,
    updated_by
)
VALUES
(
    'loyalty.redemption_enabled',
    'BOOL',
    NULL,
    NULL,
    NULL,
    1,
    NULL,
    'Enable or disable loyalty point redemption.',
    0,
    UTC_TIMESTAMP(),
    NULL
)
ON DUPLICATE KEY UPDATE
    value_type = 'BOOL',
    value_string = NULL,
    value_int = NULL,
    value_decimal = NULL,
    value_bool = VALUES(value_bool),
    value_json = NULL,
    description = VALUES(description),
    is_sensitive = VALUES(is_sensitive),
    updated_at = UTC_TIMESTAMP(),
    updated_by = NULL;

INSERT INTO system_setting
(
    setting_key,
    value_type,
    value_string,
    value_int,
    value_decimal,
    value_bool,
    value_json,
    description,
    is_sensitive,
    updated_at,
    updated_by
)
VALUES
(
    'loyalty.points_to_currency_ratio',
    'DECIMAL',
    NULL,
    NULL,
    100.00,
    NULL,
    NULL,
    'Amount earned per 1 loyalty point redeemed.',
    0,
    UTC_TIMESTAMP(),
    NULL
)
ON DUPLICATE KEY UPDATE
    value_type = 'DECIMAL',
    value_string = NULL,
    value_int = NULL,
    value_decimal = VALUES(value_decimal),
    value_bool = NULL,
    value_json = NULL,
    description = VALUES(description),
    is_sensitive = VALUES(is_sensitive),
    updated_at = UTC_TIMESTAMP(),
    updated_by = NULL;

INSERT INTO system_setting
(
    setting_key,
    value_type,
    value_string,
    value_int,
    value_decimal,
    value_bool,
    value_json,
    description,
    is_sensitive,
    updated_at,
    updated_by
)
VALUES
(
    'loyalty.min_redemption_points',
    'INT',
    NULL,
    100,
    NULL,
    NULL,
    NULL,
    'Minimum loyalty points required to redeem a coupon.',
    0,
    UTC_TIMESTAMP(),
    NULL
)
ON DUPLICATE KEY UPDATE
    value_type = 'INT',
    value_string = NULL,
    value_int = VALUES(value_int),
    value_decimal = NULL,
    value_bool = NULL,
    value_json = NULL,
    description = VALUES(description),
    is_sensitive = VALUES(is_sensitive),
    updated_at = UTC_TIMESTAMP(),
    updated_by = NULL;

COMMIT;
