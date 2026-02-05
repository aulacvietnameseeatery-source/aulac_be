-- Insert new permissions for Account Management
-- These permissions are used by the AccountController endpoints

-- Permission: Create Account
INSERT INTO permission (screen_code, action_code)
VALUES ('ACCOUNT', 'CREATE')
ON DUPLICATE KEY UPDATE screen_code = 'ACCOUNT';

-- Permission: Update Account (profile updates)
INSERT INTO permission (screen_code, action_code)
VALUES ('ACCOUNT', 'UPDATE')
ON DUPLICATE KEY UPDATE screen_code = 'ACCOUNT';

-- Verify existing permissions
-- You should already have these from your schema:
-- ('ACCOUNT', 'READ') - ViewAccount
-- ('ACCOUNT', 'EDIT') - EditAccount
-- ('ACCOUNT', 'DELETE') - DeleteAccount
-- ('ACCOUNT', 'RESET_PASSWORD') - ResetPassword

-- Optional: Assign permissions to ADMIN role (adjust role_id as needed)
-- Get the role_id for ADMIN first:
SET @admin_role_id = (SELECT role_id FROM role WHERE role_code = 'ADMIN' LIMIT 1);

-- Get permission_id for new permissions
SET @create_permission_id = (SELECT permission_id FROM permission WHERE screen_code = 'ACCOUNT' AND action_code = 'CREATE' LIMIT 1);
SET @update_permission_id = (SELECT permission_id FROM permission WHERE screen_code = 'ACCOUNT' AND action_code = 'UPDATE' LIMIT 1);

-- Assign to ADMIN role
INSERT IGNORE INTO role_permission (role_id, permission_id)
VALUES 
    (@admin_role_id, @create_permission_id),
    (@admin_role_id, @update_permission_id);

-- Verification query
SELECT 
p.permission_id,
    p.screen_code,
    p.action_code,
    CONCAT(p.screen_code, ':', p.action_code) AS permission_constant
FROM permission p
WHERE p.screen_code = 'ACCOUNT'
ORDER BY p.action_code;
