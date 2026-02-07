USE restaurant_mgmt;

-- =============================================
-- 1. ADD LOOKUP TYPE 'ZONE'
-- =============================================
-- Insert Text for Lookup Type (Optional but good for consistency)
INSERT INTO i18n_text (text_key, source_lang_code, source_text, context) VALUES
('lookup_type.type_name.19', 'en', 'Zone', 'lookup type name'),
('lookup_type.description.19', 'en', 'restaurant_table.zone', 'lookup type description');

-- Get inserted IDs (Assuming AUTO_INCREMENT)
SET @zone_type_name_id = LAST_INSERT_ID(); 
SET @zone_desc_text_id = @zone_type_name_id + 1; -- Roughly assuming consecutive if inserted together

-- Insert Type
INSERT INTO lookup_type (type_id, type_code, type_name, description, is_configurable, is_system, type_name_text_id, type_desc_text_id) VALUES
(19, 'ZONE', 'Zone', 'restaurant_table.zone', 1, 1, @zone_type_name_id, @zone_desc_text_id);


-- =============================================
-- 2. ADD LOOKUP VALUES FOR ZONES
-- =============================================
-- Values: Indoor, Outdoor, Rooftop
-- IDs: starting from 124 (based on dump max 123)

INSERT INTO i18n_text (text_key, source_lang_code, source_text, context) VALUES
('lookup_value.value_name.124', 'en', 'Indoor', 'lookup value name'),
('lookup_value.value_name.125', 'en', 'Outdoor', 'lookup value name'),
('lookup_value.value_name.126', 'en', 'Rooftop', 'lookup value name');

SET @indoor_text_id = LAST_INSERT_ID();
SET @outdoor_text_id = @indoor_text_id + 1; 
SET @rooftop_text_id = @outdoor_text_id + 1;

INSERT INTO lookup_value (value_id, type_id, value_code, value_name, sort_order, is_active, is_system, locked, value_name_text_id) VALUES
(124, 19, 'INDOOR', 'Indoor', 1, 1, 1, 0, @indoor_text_id),
(125, 19, 'OUTDOOR', 'Outdoor', 2, 1, 1, 0, @outdoor_text_id),
(126, 19, 'ROOFTOP', 'Rooftop', 3, 1, 1, 0, @rooftop_text_id);


-- =============================================
-- 3. ALTER RESTAURANT_TABLE
-- =============================================
-- Add column with default value (Indoor = 124) to support existing rows
ALTER TABLE restaurant_table
ADD COLUMN zone_lv_id int UNSIGNED NOT NULL DEFAULT 124;

-- Add Index for performance
ALTER TABLE restaurant_table
ADD INDEX idx_restaurant_table_zone_lv (zone_lv_id);

-- Add Foreign Key Constraint
ALTER TABLE restaurant_table
ADD CONSTRAINT fk_restaurant_table_zone_lv 
FOREIGN KEY (zone_lv_id) REFERENCES lookup_value (value_id);


-- =============================================
-- 4. INSERT SAMPLE TABLES (Optional, remove if not needed)
-- =============================================
-- Status: 14=Available
-- Type: 64=Normal, 67=Booth, 65=VIP
-- Zone: 124=Indoor, 125=Outdoor, 126=Rooftop

-- Remove existing tables to avoid duplicate key errors if re-running on clean DB, 
-- but this is an ALTER script, so usually we just insert new ones.
INSERT IGNORE INTO restaurant_table (table_code, capacity, table_status_lv_id, table_type_lv_id, zone_lv_id, isOnline) VALUES
('TB-001', 2, 14, 64, 124, 1),
('TB-002', 4, 14, 64, 124, 1),
('TB-003', 4, 14, 67, 124, 1),
('TB-004', 6, 14, 64, 124, 1),
('TB-O01', 4, 14, 64, 125, 1), -- Outdoor
('TB-R01', 2, 14, 64, 126, 1); -- Rooftop
