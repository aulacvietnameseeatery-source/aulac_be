-- ============================================================
-- Seed Inventory Permissions + Lookup Types + Lookup Values
-- Safe to re-run (idempotent)
-- Target DB: MySQL (restaurant_mgmt)
-- ============================================================

USE restaurant_mgmt;

START TRANSACTION;

-- ============================================================
-- 1) PERMISSIONS (INVENTORY:*)
-- ============================================================
INSERT IGNORE INTO permission (screen_code, action_code)
VALUES
    ('INVENTORY', 'READ'),
    ('INVENTORY', 'CREATE'),
    ('INVENTORY', 'APPROVE'),
    ('INVENTORY', 'STOCK_CHECK'),
    ('INVENTORY', 'REPORT_READ');

-- Assign to ADMIN and MANAGER if those roles exist
INSERT IGNORE INTO role_permission (role_id, permission_id)
SELECT
    r.role_id,
    p.permission_id
FROM role r
JOIN permission p
    ON p.screen_code = 'INVENTORY'
   AND p.action_code IN ('READ', 'CREATE', 'APPROVE', 'STOCK_CHECK', 'REPORT_READ')
WHERE r.role_code IN ('ADMIN', 'MANAGER');

-- ============================================================
-- 2) LOOKUP TYPES (fixed IDs aligned with FE LOOKUP_TYPE)
-- ============================================================
-- NOTE:
-- If your DB already has the same type_code with a different type_id,
-- INSERT IGNORE will skip insertion due to unique constraints.
-- FE currently references IDs 28-33, so verify mapping after seed.

INSERT IGNORE INTO lookup_type (type_id, type_code, type_name, is_configurable, is_system, description)
VALUES
    (28, 'INVENTORY_CATEGORY', 'Inventory Category', 1, 1, 'Inventory item category'),
    (29, 'EXPORT_REASON',      'Export Reason',      1, 1, 'Reason for stock export'),
    (30, 'VARIANCE_REASON',    'Variance Reason',    1, 1, 'Reason for inventory variance'),
    (31, 'KITCHEN_TOOL_TYPE',  'Kitchen Tool Type',  1, 1, 'Type of kitchen tools'),
    (32, 'CONSUMABLE_TYPE',    'Consumable Type',    1, 1, 'Type of consumable supplies'),
    (33, 'EQUIPMENT_TYPE',     'Equipment Type',     1, 1, 'Type of kitchen/store equipment');

-- ============================================================
-- 3) LOOKUP VALUES
-- Insert by type_code lookup so script works even if type_id differs
-- ============================================================

INSERT INTO lookup_value (type_id, value_code, value_name, sort_order, is_active, is_system, locked)
SELECT
    lt.type_id,
    seed.value_code,
    seed.value_name,
    seed.sort_order,
    1 AS is_active,
    1 AS is_system,
    0 AS locked
FROM lookup_type lt
JOIN (
    -- INVENTORY_CATEGORY
    SELECT 'INVENTORY_CATEGORY' AS type_code, 'FOOD_INGREDIENT' AS value_code, 'Food Ingredient' AS value_name, 1 AS sort_order
    UNION ALL SELECT 'INVENTORY_CATEGORY', 'KITCHEN_TOOL',      'Kitchen Tool',      2
    UNION ALL SELECT 'INVENTORY_CATEGORY', 'CONSUMABLE_SUPPLY', 'Consumable Supply', 3
    UNION ALL SELECT 'INVENTORY_CATEGORY', 'EQUIPMENT',         'Equipment',         4

    -- EXPORT_REASON
    UNION ALL SELECT 'EXPORT_REASON', 'COOKING',   'Cooking',          1
    UNION ALL SELECT 'EXPORT_REASON', 'SPOILED',   'Spoiled',          2
    UNION ALL SELECT 'EXPORT_REASON', 'EXPIRED',   'Expired',          3
    UNION ALL SELECT 'EXPORT_REASON', 'BROKEN',    'Broken',           4
    UNION ALL SELECT 'EXPORT_REASON', 'LOST',      'Lost',             5
    UNION ALL SELECT 'EXPORT_REASON', 'DISPOSED',  'Disposed',         6
    UNION ALL SELECT 'EXPORT_REASON', 'WORN_OUT',  'Worn Out',         7

    -- VARIANCE_REASON
    UNION ALL SELECT 'VARIANCE_REASON', 'BREAKAGE',       'Breakage',        1
    UNION ALL SELECT 'VARIANCE_REASON', 'NATURAL_LOSS',   'Natural Loss',    2
    UNION ALL SELECT 'VARIANCE_REASON', 'COUNTING_ERROR', 'Counting Error',  3

    -- KITCHEN_TOOL_TYPE
    UNION ALL SELECT 'KITCHEN_TOOL_TYPE', 'KNIFE',     'Knife',      1
    UNION ALL SELECT 'KITCHEN_TOOL_TYPE', 'PAN',       'Pan',        2
    UNION ALL SELECT 'KITCHEN_TOOL_TYPE', 'POT',       'Pot',        3
    UNION ALL SELECT 'KITCHEN_TOOL_TYPE', 'UTENSIL',   'Utensil',    4
    UNION ALL SELECT 'KITCHEN_TOOL_TYPE', 'CONTAINER', 'Container',  5
    UNION ALL SELECT 'KITCHEN_TOOL_TYPE', 'OTHER',     'Other',      6

    -- CONSUMABLE_TYPE
    UNION ALL SELECT 'CONSUMABLE_TYPE', 'CLEANING_SUPPLY', 'Cleaning Supply', 1
    UNION ALL SELECT 'CONSUMABLE_TYPE', 'PACKAGING',       'Packaging',       2
    UNION ALL SELECT 'CONSUMABLE_TYPE', 'NAPKIN',          'Napkin',          3
    UNION ALL SELECT 'CONSUMABLE_TYPE', 'GLOVE',           'Glove',           4
    UNION ALL SELECT 'CONSUMABLE_TYPE', 'OTHER',           'Other',           5

    -- EQUIPMENT_TYPE
    UNION ALL SELECT 'EQUIPMENT_TYPE', 'COOKER',  'Cooker',  1
    UNION ALL SELECT 'EQUIPMENT_TYPE', 'FRIDGE',  'Fridge',  2
    UNION ALL SELECT 'EQUIPMENT_TYPE', 'FREEZER', 'Freezer', 3
    UNION ALL SELECT 'EQUIPMENT_TYPE', 'OVEN',    'Oven',    4
    UNION ALL SELECT 'EQUIPMENT_TYPE', 'MIXER',   'Mixer',   5
    UNION ALL SELECT 'EQUIPMENT_TYPE', 'OTHER',   'Other',   6
) seed
    ON seed.type_code = lt.type_code
LEFT JOIN lookup_value lv
    ON lv.type_id = lt.type_id
   AND lv.value_code = seed.value_code
WHERE lv.value_id IS NULL;

COMMIT;

-- ============================================================
-- 4) VERIFY
-- ============================================================
SELECT
    p.permission_id,
    CONCAT(p.screen_code, ':', p.action_code) AS permission_constant
FROM permission p
WHERE p.screen_code = 'INVENTORY'
ORDER BY p.action_code;

SELECT
    lt.type_id,
    lt.type_code,
    lt.type_name,
    COUNT(lv.value_id) AS value_count
FROM lookup_type lt
LEFT JOIN lookup_value lv ON lv.type_id = lt.type_id
WHERE lt.type_code IN (
    'INVENTORY_CATEGORY',
    'EXPORT_REASON',
    'VARIANCE_REASON',
    'KITCHEN_TOOL_TYPE',
    'CONSUMABLE_TYPE',
    'EQUIPMENT_TYPE'
)
GROUP BY lt.type_id, lt.type_code, lt.type_name
ORDER BY lt.type_id;
