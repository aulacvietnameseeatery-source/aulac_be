-- Add CANCELLED status to order_item_status lookup values
-- This allows customers to cancel items that haven't been prepared yet (CREATED status)

USE restaurant_mgmt;

-- Insert CANCELLED status for OrderItemStatus (type_id = 13)
INSERT INTO lookup_value (type_id, value_code, value_name, sort_order, is_active, meta, is_system, locked, deleted_at, description, update_at)
VALUES (
    13,                     -- type_id for OrderItemStatus
    'CANCELLED',            -- value_code
    'Cancelled',            -- value_name
    6,                      -- sort_order (after REJECTED which is 5)
    1,                      -- is_active
    '{"legacy_num": 6}',    -- meta
    1,                      -- is_system
    1,                      -- locked
    NULL,                   -- deleted_at
    NULL,                   -- description
    NULL                    -- update_at
);

-- Verify the insertion
SELECT value_id, type_id, value_code, value_name, sort_order, is_active
FROM lookup_value
WHERE type_id = 13
ORDER BY sort_order;
