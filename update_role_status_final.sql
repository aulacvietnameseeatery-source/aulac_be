USE restaurant_mgmt;

-- Transaction to ensure atomicity
START TRANSACTION;

-- 1. Insert i18n_text for Type and Values
-- Type Name
INSERT INTO i18n_text (text_key, source_lang_code, source_text, context) 
VALUES ('lookup_type.type_name.role_status', 'en', 'Role Status', 'Enum Type Name');
SET @typeNameTextId = LAST_INSERT_ID();

-- Type Description
INSERT INTO i18n_text (text_key, source_lang_code, source_text, context) 
VALUES ('lookup_type.type_desc.role_status', 'en', 'Status of the role', 'Enum Type Description');
SET @typeDescTextId = LAST_INSERT_ID();

-- Value Name: Active
INSERT INTO i18n_text (text_key, source_lang_code, source_text, context) 
VALUES ('lookup_value.value_name.role_status.active', 'en', 'Active', 'Enum Value Name');
SET @valActiveTextId = LAST_INSERT_ID();

-- Value Name: Inactive
INSERT INTO i18n_text (text_key, source_lang_code, source_text, context) 
VALUES ('lookup_value.value_name.role_status.inactive', 'en', 'Inactive', 'Enum Value Name');
SET @valInactiveTextId = LAST_INSERT_ID();

-- 2. Insert Lookup Type (ROLE_STATUS)
-- Use ID 20 if available, or just auto-increment. 
-- However, existing C# code expects IDs. We'll use 20 for consistency with the plan, 
-- but we could also use auto-increment if we updated C# to lookup by Code.
-- To stay aligned with existing C# patterns (LookupTypeInfo), we try to set a fixed ID if possible, 
-- but given the user request "id + 1", we will let it AUTO INCREMENT and capture it.

INSERT INTO lookup_type(type_code, type_name, description, is_configurable, is_system, type_name_text_id, type_desc_text_id) 
VALUES ('ROLE_STATUS', 'Role Status', 'role.role_status', 0, 1, @typeNameTextId, @typeDescTextId);

SET @typeId = LAST_INSERT_ID();

-- 3. Insert Lookup Values
-- Active
INSERT INTO lookup_value(type_id, value_code, value_name, sort_order, is_active, is_system, locked, value_name_text_id)
VALUES (@typeId, 'ACTIVE', 'Active', 1, 1, 1, 1, @valActiveTextId);
SET @activeValueId = LAST_INSERT_ID();

-- Inactive
INSERT INTO lookup_value(type_id, value_code, value_name, sort_order, is_active, is_system, locked, value_name_text_id)
VALUES (@typeId, 'INACTIVE', 'Inactive', 2, 1, 1, 1, @valInactiveTextId);
SET @inactiveValueId = LAST_INSERT_ID();

-- 4. Update Role Table
ALTER TABLE role
ADD COLUMN role_status_lv_id INT UNSIGNED NOT NULL DEFAULT 0;

-- Update existing roles to 'ACTIVE'
UPDATE role SET role_status_lv_id = @activeValueId WHERE role_id > 0;

-- Add index and foreign key
ALTER TABLE role
ADD INDEX idx_role_status_lv (role_status_lv_id);

ALTER TABLE role
ADD CONSTRAINT fk_role_status_lv FOREIGN KEY (role_status_lv_id)
REFERENCES lookup_value (value_id);

COMMIT;

-- Verify
SELECT * FROM lookup_type WHERE type_id = @typeId;
SELECT * FROM lookup_value WHERE type_id = @typeId;
