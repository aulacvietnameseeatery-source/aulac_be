-- ============================================================
-- Insert SYSTEM_SETTING permissions into the permission table
-- and assign them to the ADMIN role
-- ============================================================

-- 1. Insert permissions (skip if already exist)
INSERT IGNORE INTO permission (screen_code, action_code)
VALUES
    ('SYSTEM_SETTING', 'READ'),
    ('SYSTEM_SETTING', 'EDIT');

-- 2. Get ADMIN role id
SET @admin_role_id = (
    SELECT role_id FROM role WHERE role_code = 'ADMIN' LIMIT 1
);

-- 3. Get permission ids
SET @read_perm_id = (
    SELECT permission_id FROM permission
    WHERE screen_code = 'SYSTEM_SETTING' AND action_code = 'READ'
    LIMIT 1
);

SET @edit_perm_id = (
    SELECT permission_id FROM permission
    WHERE screen_code = 'SYSTEM_SETTING' AND action_code = 'EDIT'
    LIMIT 1
);

-- 4. Assign to ADMIN role
INSERT IGNORE INTO role_permission (role_id, permission_id)
VALUES
    (@admin_role_id, @read_perm_id),
    (@admin_role_id, @edit_perm_id);

-- 5. Verify
SELECT
    p.permission_id,
    p.screen_code,
    p.action_code,
    CONCAT(p.screen_code, ':', p.action_code) AS permission_constant
FROM permission p
WHERE p.screen_code = 'SYSTEM_SETTING'
ORDER BY p.action_code;
