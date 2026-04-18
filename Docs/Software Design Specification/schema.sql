--
-- Create table `i18n_language`
--
CREATE TABLE i18n_language (
  lang_code varchar(10) NOT NULL,
  lang_name varchar(50) NOT NULL,
  is_active tinyint(1) NOT NULL DEFAULT 1,
  PRIMARY KEY (lang_code)
)
ENGINE = INNODB,
AVG_ROW_LENGTH = 5461,
CHARACTER SET utf8mb4,
COLLATE utf8mb4_unicode_ci,
ROW_FORMAT = DYNAMIC;

--
-- Create table `i18n_text`
--
CREATE TABLE i18n_text (
  text_id bigint NOT NULL AUTO_INCREMENT,
  text_key varchar(200) NOT NULL,
  source_lang_code varchar(10) NOT NULL DEFAULT 'en',
  source_text text NOT NULL,
  context varchar(255) DEFAULT NULL,
  created_at datetime DEFAULT CURRENT_TIMESTAMP,
  updated_at datetime DEFAULT CURRENT_TIMESTAMP ON UPDATE CURRENT_TIMESTAMP,
  PRIMARY KEY (text_id)
)
ENGINE = INNODB,
AUTO_INCREMENT = 291,
AVG_ROW_LENGTH = 265,
CHARACTER SET utf8mb4,
COLLATE utf8mb4_unicode_ci,
ROW_FORMAT = DYNAMIC;

ALTER TABLE i18n_text
ADD CONSTRAINT fk_i18n_text_lang FOREIGN KEY (source_lang_code)
REFERENCES i18n_language (lang_code);

--
-- Create table `service_error_category`
--
CREATE TABLE service_error_category (
  category_id bigint NOT NULL AUTO_INCREMENT,
  category_code varchar(50) NOT NULL,
  category_name varchar(150) NOT NULL,
  description varchar(255) DEFAULT NULL,
  category_name_text_id bigint DEFAULT NULL,
  category_desc_text_id bigint DEFAULT NULL,
  PRIMARY KEY (category_id)
)
ENGINE = INNODB,
CHARACTER SET utf8mb4,
COLLATE utf8mb4_unicode_ci,
ROW_FORMAT = DYNAMIC;

ALTER TABLE service_error_category
ADD CONSTRAINT fk_sec_desc_text FOREIGN KEY (category_desc_text_id)
REFERENCES i18n_text (text_id);

ALTER TABLE service_error_category
ADD CONSTRAINT fk_sec_name_text FOREIGN KEY (category_name_text_id)
REFERENCES i18n_text (text_id);

--
-- Create table `lookup_type`
--
CREATE TABLE lookup_type (
  type_id smallint UNSIGNED NOT NULL AUTO_INCREMENT,
  type_code varchar(50) NOT NULL,
  type_name varchar(150) NOT NULL,
  description varchar(255) DEFAULT NULL,
  is_configurable tinyint(1) NOT NULL COMMENT '1 = admin can add/remove values,0 = controlled enum (statuses, workflows)',
  is_system tinyint(1) NOT NULL DEFAULT 1 COMMENT '1 = system-defined enum type,0 = user-defined/custom type',
  type_name_text_id bigint DEFAULT NULL,
  type_desc_text_id bigint DEFAULT NULL,
  PRIMARY KEY (type_id)
)
ENGINE = INNODB,
AUTO_INCREMENT = 34,
AVG_ROW_LENGTH = 496,
CHARACTER SET utf8mb4,
COLLATE utf8mb4_unicode_ci,
ROW_FORMAT = DYNAMIC;

ALTER TABLE lookup_type
ADD CONSTRAINT fk_lookup_type_desc_text FOREIGN KEY (type_desc_text_id)
REFERENCES i18n_text (text_id);

ALTER TABLE lookup_type
ADD CONSTRAINT fk_lookup_type_name_text FOREIGN KEY (type_name_text_id)
REFERENCES i18n_text (text_id);

--
-- Create table `lookup_value`
--
CREATE TABLE lookup_value (
  value_id int UNSIGNED NOT NULL AUTO_INCREMENT,
  type_id smallint UNSIGNED NOT NULL,
  value_code varchar(50) NOT NULL,
  value_name varchar(150) NOT NULL,
  sort_order smallint NOT NULL,
  is_active tinyint(1) NOT NULL DEFAULT 1,
  meta json DEFAULT NULL,
  is_system tinyint(1) NOT NULL DEFAULT 1 COMMENT '1 = system/seeded value,0 = user-added value',
  locked tinyint(1) NOT NULL DEFAULT 1 COMMENT '1 = value_code cannot be changed and value cannot be deleted',
  deleted_at datetime DEFAULT NULL COMMENT 'Soft delete timestamp; never hard delete lookup values',
  description text DEFAULT NULL,
  update_at datetime DEFAULT NULL,
  value_name_text_id bigint DEFAULT NULL,
  value_desc_text_id bigint DEFAULT NULL,
  PRIMARY KEY (value_id)
)
ENGINE = INNODB,
AUTO_INCREMENT = 1896,
AVG_ROW_LENGTH = 101,
CHARACTER SET utf8mb4,
COLLATE utf8mb4_unicode_ci,
ROW_FORMAT = DYNAMIC;

ALTER TABLE lookup_value
ADD CONSTRAINT fk_lookup_value_desc_text FOREIGN KEY (value_desc_text_id)
REFERENCES i18n_text (text_id);

ALTER TABLE lookup_value
ADD CONSTRAINT fk_lookup_value_name_text FOREIGN KEY (value_name_text_id)
REFERENCES i18n_text (text_id);

ALTER TABLE lookup_value
ADD CONSTRAINT fk_lookup_value_type FOREIGN KEY (type_id)
REFERENCES lookup_type (type_id);

--
-- Create table `role`
--
CREATE TABLE role (
  role_id bigint NOT NULL AUTO_INCREMENT,
  role_code varchar(50) NOT NULL,
  role_name varchar(100) NOT NULL,
  role_status_lv_id int UNSIGNED NOT NULL,
  PRIMARY KEY (role_id)
)
ENGINE = INNODB,
AUTO_INCREMENT = 7,
AVG_ROW_LENGTH = 3276,
CHARACTER SET utf8mb4,
COLLATE utf8mb4_unicode_ci,
ROW_FORMAT = DYNAMIC;

ALTER TABLE role
ADD CONSTRAINT FK_role_lookup_value_role_status_lv_id FOREIGN KEY (role_status_lv_id)
REFERENCES lookup_value (value_id) ON DELETE CASCADE;

--
-- Create table `staff_account`
--
CREATE TABLE staff_account (
  account_id bigint NOT NULL AUTO_INCREMENT,
  full_name varchar(150) NOT NULL,
  phone varchar(30) DEFAULT NULL,
  email varchar(150) DEFAULT NULL,
  role_id bigint NOT NULL,
  created_at datetime NOT NULL DEFAULT CURRENT_TIMESTAMP,
  username varchar(100) NOT NULL,
  password_hash varchar(255) NOT NULL,
  is_locked tinyint(1) NOT NULL,
  last_login_at datetime DEFAULT NULL,
  RegisteredDeviceId longtext DEFAULT NULL,
  account_status_lv_id int UNSIGNED NOT NULL,
  PRIMARY KEY (account_id)
)
ENGINE = INNODB,
AUTO_INCREMENT = 12,
AVG_ROW_LENGTH = 1489,
CHARACTER SET utf8mb4,
COLLATE utf8mb4_unicode_ci,
ROW_FORMAT = DYNAMIC;

ALTER TABLE staff_account
ADD CONSTRAINT staff_account_ibfk_1 FOREIGN KEY (role_id)
REFERENCES role (role_id);

--
-- Create table `system_setting`
--
CREATE TABLE system_setting (
  setting_id int UNSIGNED NOT NULL AUTO_INCREMENT,
  setting_key varchar(100) NOT NULL,
  setting_name varchar(255) DEFAULT NULL,
  value_type enum ('STRING', 'INT', 'DECIMAL', 'BOOL', 'JSON', 'DATETIME') NOT NULL,
  value_string varchar(500) DEFAULT NULL,
  value_int bigint DEFAULT NULL,
  value_decimal decimal(18, 6) DEFAULT NULL,
  value_bool tinyint(1) DEFAULT NULL,
  value_json json DEFAULT NULL,
  description varchar(255) DEFAULT NULL,
  is_sensitive tinyint(1) NOT NULL,
  updated_at datetime NOT NULL DEFAULT CURRENT_TIMESTAMP ON UPDATE CURRENT_TIMESTAMP,
  updated_by bigint DEFAULT NULL,
  PRIMARY KEY (setting_id)
)
ENGINE = INNODB,
AUTO_INCREMENT = 204,
AVG_ROW_LENGTH = 426,
CHARACTER SET utf8mb4,
COLLATE utf8mb4_unicode_ci,
ROW_FORMAT = DYNAMIC;

ALTER TABLE system_setting
ADD CONSTRAINT fk_setting_updated_by FOREIGN KEY (updated_by)
REFERENCES staff_account (account_id);

--
-- Create table `shift_template`
--
CREATE TABLE shift_template (
  shift_template_id bigint NOT NULL AUTO_INCREMENT,
  template_name varchar(100) NOT NULL,
  default_start_time time NOT NULL,
  default_end_time time NOT NULL,
  description varchar(500) DEFAULT NULL,
  buffer_before_minutes int DEFAULT NULL,
  buffer_after_minutes int DEFAULT NULL,
  is_active tinyint(1) NOT NULL DEFAULT 1,
  created_by bigint NOT NULL,
  created_at datetime NOT NULL DEFAULT CURRENT_TIMESTAMP,
  updated_by bigint DEFAULT NULL,
  updated_at datetime NOT NULL DEFAULT CURRENT_TIMESTAMP ON UPDATE CURRENT_TIMESTAMP,
  PRIMARY KEY (shift_template_id)
)
ENGINE = INNODB,
AUTO_INCREMENT = 4,
AVG_ROW_LENGTH = 5461,
CHARACTER SET utf8mb4,
COLLATE utf8mb4_unicode_ci,
ROW_FORMAT = DYNAMIC;

ALTER TABLE shift_template
ADD CONSTRAINT fk_shift_template_created_by FOREIGN KEY (created_by)
REFERENCES staff_account (account_id);

ALTER TABLE shift_template
ADD CONSTRAINT fk_shift_template_updated_by FOREIGN KEY (updated_by)
REFERENCES staff_account (account_id);

--
-- Create table `shift_assignment`
--
CREATE TABLE shift_assignment (
  shift_assignment_id bigint NOT NULL AUTO_INCREMENT,
  shift_template_id bigint NOT NULL,
  staff_id bigint NOT NULL,
  work_date date NOT NULL,
  planned_start_at datetime NOT NULL,
  planned_end_at datetime NOT NULL,
  assignment_status_lv_id int UNSIGNED NOT NULL,
  is_active tinyint(1) NOT NULL DEFAULT 1,
  tags varchar(200) DEFAULT NULL,
  notes varchar(500) DEFAULT NULL,
  assigned_by bigint NOT NULL,
  assigned_at datetime NOT NULL DEFAULT CURRENT_TIMESTAMP,
  created_at datetime NOT NULL DEFAULT CURRENT_TIMESTAMP,
  updated_at datetime NOT NULL DEFAULT CURRENT_TIMESTAMP ON UPDATE CURRENT_TIMESTAMP,
  PRIMARY KEY (shift_assignment_id)
)
ENGINE = INNODB,
AUTO_INCREMENT = 221,
AVG_ROW_LENGTH = 245,
CHARACTER SET utf8mb4,
COLLATE utf8mb4_unicode_ci,
ROW_FORMAT = DYNAMIC;

ALTER TABLE shift_assignment
ADD CONSTRAINT fk_shift_assignment_assigned_by FOREIGN KEY (assigned_by)
REFERENCES staff_account (account_id);

ALTER TABLE shift_assignment
ADD CONSTRAINT fk_shift_assignment_staff FOREIGN KEY (staff_id)
REFERENCES staff_account (account_id);

ALTER TABLE shift_assignment
ADD CONSTRAINT fk_shift_assignment_status_lv FOREIGN KEY (assignment_status_lv_id)
REFERENCES lookup_value (value_id);

ALTER TABLE shift_assignment
ADD CONSTRAINT fk_shift_assignment_template FOREIGN KEY (shift_template_id)
REFERENCES shift_template (shift_template_id) ON DELETE CASCADE;

--
-- Create table `attendance_record`
--
CREATE TABLE attendance_record (
  attendance_id bigint NOT NULL AUTO_INCREMENT,
  shift_assignment_id bigint NOT NULL,
  attendance_status_lv_id int UNSIGNED NOT NULL,
  actual_check_in_at datetime DEFAULT NULL,
  actual_check_out_at datetime DEFAULT NULL,
  late_minutes int NOT NULL DEFAULT 0,
  early_leave_minutes int NOT NULL DEFAULT 0,
  worked_minutes int NOT NULL DEFAULT 0,
  is_manual_adjustment tinyint(1) NOT NULL DEFAULT 0,
  adjustment_reason varchar(500) DEFAULT NULL,
  reviewed_by bigint DEFAULT NULL,
  reviewed_at datetime DEFAULT NULL,
  created_at datetime NOT NULL DEFAULT CURRENT_TIMESTAMP,
  updated_at datetime NOT NULL DEFAULT CURRENT_TIMESTAMP ON UPDATE CURRENT_TIMESTAMP,
  PRIMARY KEY (attendance_id)
)
ENGINE = INNODB,
AUTO_INCREMENT = 2,
AVG_ROW_LENGTH = 16384,
CHARACTER SET utf8mb4,
COLLATE utf8mb4_unicode_ci,
ROW_FORMAT = DYNAMIC;

ALTER TABLE attendance_record
ADD CONSTRAINT fk_attendance_assignment FOREIGN KEY (shift_assignment_id)
REFERENCES shift_assignment (shift_assignment_id) ON DELETE CASCADE;

ALTER TABLE attendance_record
ADD CONSTRAINT fk_attendance_reviewed_by FOREIGN KEY (reviewed_by)
REFERENCES staff_account (account_id);

ALTER TABLE attendance_record
ADD CONSTRAINT fk_attendance_status_lv FOREIGN KEY (attendance_status_lv_id)
REFERENCES lookup_value (value_id);

--
-- Create table `time_log`
--
CREATE TABLE time_log (
  time_log_id bigint NOT NULL AUTO_INCREMENT,
  attendance_record_id bigint NOT NULL,
  punch_in_time datetime NOT NULL,
  punch_out_time datetime DEFAULT NULL,
  gps_location_in varchar(50) DEFAULT NULL,
  gps_location_out varchar(50) DEFAULT NULL,
  device_id_in varchar(255) DEFAULT NULL,
  device_id_out varchar(255) DEFAULT NULL,
  validation_status varchar(30) NOT NULL DEFAULT 'Valid',
  punch_duration_minutes int NOT NULL DEFAULT 0,
  created_at datetime NOT NULL DEFAULT CURRENT_TIMESTAMP,
  PRIMARY KEY (time_log_id)
)
ENGINE = INNODB,
CHARACTER SET utf8mb4,
COLLATE utf8mb4_unicode_ci,
ROW_FORMAT = DYNAMIC;

ALTER TABLE time_log
ADD CONSTRAINT fk_time_log_attendance FOREIGN KEY (attendance_record_id)
REFERENCES attendance_record (attendance_id) ON DELETE CASCADE;

--
-- Create table `auth_session`
--
CREATE TABLE auth_session (
  session_id bigint NOT NULL AUTO_INCREMENT,
  user_id bigint NOT NULL,
  token_hash varchar(255) NOT NULL,
  expires_at datetime NOT NULL,
  created_at datetime DEFAULT CURRENT_TIMESTAMP,
  revoked tinyint(1) DEFAULT 0,
  device_info varchar(500) DEFAULT NULL,
  ip_address varchar(45) DEFAULT NULL,
  PRIMARY KEY (session_id)
)
ENGINE = INNODB,
AUTO_INCREMENT = 1034,
AVG_ROW_LENGTH = 299,
CHARACTER SET utf8mb4,
COLLATE utf8mb4_unicode_ci,
ROW_FORMAT = DYNAMIC;

ALTER TABLE auth_session
ADD CONSTRAINT auth_session_ibfk_1 FOREIGN KEY (user_id)
REFERENCES staff_account (account_id);

--
-- Create table `login_activity`
--
CREATE TABLE login_activity (
  login_activity_id bigint NOT NULL AUTO_INCREMENT,
  staff_id bigint NOT NULL,
  session_id bigint DEFAULT NULL,
  event_type varchar(50) NOT NULL,
  device_info varchar(255) DEFAULT NULL,
  ip_address varchar(64) DEFAULT NULL,
  occurred_at datetime NOT NULL DEFAULT CURRENT_TIMESTAMP,
  PRIMARY KEY (login_activity_id)
)
ENGINE = INNODB,
CHARACTER SET utf8mb4,
COLLATE utf8mb4_unicode_ci,
ROW_FORMAT = DYNAMIC;

ALTER TABLE login_activity
ADD CONSTRAINT fk_login_activity_session FOREIGN KEY (session_id)
REFERENCES auth_session (session_id) ON DELETE SET NULL;

ALTER TABLE login_activity
ADD CONSTRAINT fk_login_activity_staff FOREIGN KEY (staff_id)
REFERENCES staff_account (account_id);

--
-- Create table `audit_log`
--
CREATE TABLE audit_log (
  log_id bigint NOT NULL AUTO_INCREMENT,
  staff_id bigint DEFAULT NULL,
  action_code varchar(100) DEFAULT NULL,
  target_table varchar(100) DEFAULT NULL,
  target_id bigint DEFAULT NULL,
  created_at datetime DEFAULT CURRENT_TIMESTAMP,
  PRIMARY KEY (log_id)
)
ENGINE = INNODB,
CHARACTER SET utf8mb4,
COLLATE utf8mb4_unicode_ci,
ROW_FORMAT = DYNAMIC;

ALTER TABLE audit_log
ADD CONSTRAINT audit_log_ibfk_1 FOREIGN KEY (staff_id)
REFERENCES staff_account (account_id);

--
-- Create table `promotion`
--
CREATE TABLE promotion (
  promotion_id bigint NOT NULL AUTO_INCREMENT,
  promo_code varchar(50) DEFAULT NULL,
  promo_name varchar(200) NOT NULL,
  description varchar(255) DEFAULT NULL,
  start_time datetime NOT NULL,
  end_time datetime NOT NULL,
  discount_value decimal(10, 2) NOT NULL,
  max_usage int DEFAULT NULL,
  used_count int DEFAULT 0,
  created_at datetime DEFAULT CURRENT_TIMESTAMP,
  type_lv_id int UNSIGNED NOT NULL,
  promotion_status_lv_id int UNSIGNED NOT NULL,
  promo_name_text_id bigint DEFAULT NULL,
  promo_desc_text_id bigint DEFAULT NULL,
  PRIMARY KEY (promotion_id)
)
ENGINE = INNODB,
AUTO_INCREMENT = 9,
AVG_ROW_LENGTH = 2340,
CHARACTER SET utf8mb4,
COLLATE utf8mb4_unicode_ci,
ROW_FORMAT = DYNAMIC;

ALTER TABLE promotion
ADD CONSTRAINT fk_promo_desc_text FOREIGN KEY (promo_desc_text_id)
REFERENCES i18n_text (text_id);

ALTER TABLE promotion
ADD CONSTRAINT fk_promo_name_text FOREIGN KEY (promo_name_text_id)
REFERENCES i18n_text (text_id);

ALTER TABLE promotion
ADD CONSTRAINT fk_promotion_status_lv FOREIGN KEY (promotion_status_lv_id)
REFERENCES lookup_value (value_id);

ALTER TABLE promotion
ADD CONSTRAINT fk_promotion_type_lv FOREIGN KEY (type_lv_id)
REFERENCES lookup_value (value_id);

--
-- Create table `media_asset`
--
CREATE TABLE media_asset (
  media_id bigint NOT NULL AUTO_INCREMENT,
  url varchar(500) NOT NULL,
  mime_type varchar(100) DEFAULT NULL,
  width int DEFAULT NULL,
  height int DEFAULT NULL,
  duration_sec int DEFAULT NULL,
  created_at datetime DEFAULT CURRENT_TIMESTAMP,
  media_type_lv_id int UNSIGNED NOT NULL,
  PRIMARY KEY (media_id)
)
ENGINE = INNODB,
AUTO_INCREMENT = 88,
AVG_ROW_LENGTH = 744,
CHARACTER SET utf8mb4,
COLLATE utf8mb4_unicode_ci,
ROW_FORMAT = DYNAMIC;

ALTER TABLE media_asset
ADD CONSTRAINT fk_media_asset_type_lv FOREIGN KEY (media_type_lv_id)
REFERENCES lookup_value (value_id);

--
-- Create table `restaurant_table`
--
CREATE TABLE restaurant_table (
  table_id bigint NOT NULL AUTO_INCREMENT,
  table_code varchar(50) NOT NULL,
  capacity int NOT NULL,
  table_qr_img bigint DEFAULT NULL,
  table_status_lv_id int UNSIGNED NOT NULL,
  table_type_lv_id int UNSIGNED NOT NULL,
  zone_lv_id int UNSIGNED NOT NULL,
  isOnline tinyint(1) NOT NULL DEFAULT 1,
  qr_token varchar(255) DEFAULT NULL,
  created_at datetime NOT NULL DEFAULT CURRENT_TIMESTAMP,
  updated_at datetime NOT NULL DEFAULT CURRENT_TIMESTAMP ON UPDATE CURRENT_TIMESTAMP,
  updated_by_staff_id bigint DEFAULT NULL,
  is_deleted tinyint(1) NOT NULL DEFAULT 0,
  PRIMARY KEY (table_id)
)
ENGINE = INNODB,
AUTO_INCREMENT = 14,
AVG_ROW_LENGTH = 2340,
CHARACTER SET utf8mb4,
COLLATE utf8mb4_unicode_ci,
ROW_FORMAT = DYNAMIC;

ALTER TABLE restaurant_table
ADD CONSTRAINT fk_restaurant_table_status_lv FOREIGN KEY (table_status_lv_id)
REFERENCES lookup_value (value_id);

ALTER TABLE restaurant_table
ADD CONSTRAINT FK_restaurant_table_table_qr_img FOREIGN KEY (table_qr_img)
REFERENCES media_asset (media_id) ON DELETE SET NULL;

ALTER TABLE restaurant_table
ADD CONSTRAINT fk_restaurant_table_type_lv FOREIGN KEY (table_type_lv_id)
REFERENCES lookup_value (value_id);

ALTER TABLE restaurant_table
ADD CONSTRAINT fk_restaurant_table_updated_by_staff FOREIGN KEY (updated_by_staff_id)
REFERENCES staff_account (account_id);

ALTER TABLE restaurant_table
ADD CONSTRAINT fk_restaurant_table_zone_lv FOREIGN KEY (zone_lv_id)
REFERENCES lookup_value (value_id);

--
-- Create table `table_media`
--
CREATE TABLE table_media (
  table_id bigint NOT NULL,
  media_id bigint NOT NULL,
  is_primary tinyint(1) DEFAULT 0,
  PRIMARY KEY (table_id, media_id)
)
ENGINE = INNODB,
AVG_ROW_LENGTH = 1820,
CHARACTER SET utf8mb4,
COLLATE utf8mb4_unicode_ci,
ROW_FORMAT = DYNAMIC;

ALTER TABLE table_media
ADD CONSTRAINT table_media_ibfk_1 FOREIGN KEY (table_id)
REFERENCES restaurant_table (table_id);

ALTER TABLE table_media
ADD CONSTRAINT table_media_ibfk_2 FOREIGN KEY (media_id)
REFERENCES media_asset (media_id);

--
-- Create table `ingredient`
--
CREATE TABLE ingredient (
  ingredient_id bigint NOT NULL AUTO_INCREMENT,
  ingredient_name varchar(200) NOT NULL,
  type_lv_id int UNSIGNED DEFAULT NULL,
  ingredient_name_text_id bigint DEFAULT NULL,
  image_id bigint DEFAULT NULL,
  unit_lv_id int UNSIGNED NOT NULL DEFAULT 0,
  category_lv_id int UNSIGNED DEFAULT NULL,
  PRIMARY KEY (ingredient_id)
)
ENGINE = INNODB,
AUTO_INCREMENT = 13,
AVG_ROW_LENGTH = 1365,
CHARACTER SET utf8mb4,
COLLATE utf8mb4_unicode_ci,
ROW_FORMAT = DYNAMIC;

ALTER TABLE ingredient
ADD CONSTRAINT FK_ingredient_category_lv_id FOREIGN KEY (category_lv_id)
REFERENCES lookup_value (value_id);

ALTER TABLE ingredient
ADD CONSTRAINT FK_ingredient_image_id FOREIGN KEY (image_id)
REFERENCES media_asset (media_id);

ALTER TABLE ingredient
ADD CONSTRAINT fk_ingredient_name_text FOREIGN KEY (ingredient_name_text_id)
REFERENCES i18n_text (text_id);

ALTER TABLE ingredient
ADD CONSTRAINT FK_ingredient_type_lv_id FOREIGN KEY (type_lv_id)
REFERENCES lookup_value (value_id);

ALTER TABLE ingredient
ADD CONSTRAINT FK_ingredient_unit_lv_id FOREIGN KEY (unit_lv_id)
REFERENCES lookup_value (value_id) ON DELETE CASCADE;

--
-- Create table `current_stock`
--
CREATE TABLE current_stock (
  ingredient_id bigint NOT NULL,
  quantity_on_hand decimal(14, 3) NOT NULL,
  last_updated_at datetime DEFAULT CURRENT_TIMESTAMP ON UPDATE CURRENT_TIMESTAMP,
  min_stock_level decimal(14, 3) NOT NULL DEFAULT 0.000,
  PRIMARY KEY (ingredient_id)
)
ENGINE = INNODB,
AVG_ROW_LENGTH = 1365,
CHARACTER SET utf8mb4,
COLLATE utf8mb4_unicode_ci,
ROW_FORMAT = DYNAMIC;

ALTER TABLE current_stock
ADD CONSTRAINT fk_current_stock_ingredient FOREIGN KEY (ingredient_id)
REFERENCES ingredient (ingredient_id);

--
-- Create table `i18n_translation`
--
CREATE TABLE i18n_translation (
  text_id bigint NOT NULL,
  lang_code varchar(10) NOT NULL,
  translated_text text NOT NULL,
  updated_at datetime DEFAULT CURRENT_TIMESTAMP ON UPDATE CURRENT_TIMESTAMP,
  PRIMARY KEY (text_id, lang_code)
)
ENGINE = INNODB,
AVG_ROW_LENGTH = 172,
CHARACTER SET utf8mb4,
COLLATE utf8mb4_unicode_ci,
ROW_FORMAT = DYNAMIC;

ALTER TABLE i18n_translation
ADD CONSTRAINT fk_i18n_tr_lang FOREIGN KEY (lang_code)
REFERENCES i18n_language (lang_code);

ALTER TABLE i18n_translation
ADD CONSTRAINT fk_i18n_tr_text FOREIGN KEY (text_id)
REFERENCES i18n_text (text_id) ON DELETE CASCADE;

--
-- Create table `dish_category`
--
CREATE TABLE dish_category (
  category_id bigint NOT NULL AUTO_INCREMENT,
  category_name varchar(100) NOT NULL,
  category_name_text_id bigint DEFAULT NULL,
  description varchar(100) DEFAULT NULL,
  description_text_id bigint DEFAULT NULL,
  display_order int NOT NULL,
  is_disable tinyint(1) NOT NULL,
  PRIMARY KEY (category_id)
)
ENGINE = INNODB,
AUTO_INCREMENT = 11,
AVG_ROW_LENGTH = 1820,
CHARACTER SET utf8mb4,
COLLATE utf8mb4_unicode_ci,
ROW_FORMAT = DYNAMIC;

ALTER TABLE dish_category
ADD CONSTRAINT fk_cat_name_text FOREIGN KEY (category_name_text_id)
REFERENCES i18n_text (text_id);

ALTER TABLE dish_category
ADD CONSTRAINT FK_dish_category_i18n_text_text_id FOREIGN KEY (description_text_id)
REFERENCES i18n_text (text_id);

--
-- Create table `dish`
--
CREATE TABLE dish (
  dish_id bigint NOT NULL AUTO_INCREMENT,
  category_id bigint NOT NULL,
  dish_name varchar(200) NOT NULL,
  price decimal(12, 2) NOT NULL,
  created_at datetime DEFAULT CURRENT_TIMESTAMP,
  dish_status_lv_id int UNSIGNED NOT NULL,
  description text DEFAULT NULL,
  slogan varchar(250) CHARACTER SET utf8mb3 COLLATE utf8mb3_general_ci DEFAULT NULL,
  note varchar(500) CHARACTER SET utf8mb3 COLLATE utf8mb3_general_ci DEFAULT NULL,
  calories int DEFAULT NULL,
  short_description varchar(255) CHARACTER SET utf8mb3 COLLATE utf8mb3_general_ci DEFAULT NULL,
  display_order tinyint DEFAULT NULL,
  chef_recommended tinyint(1) DEFAULT NULL,
  prep_time_minutes int DEFAULT NULL,
  cook_time_minutes int DEFAULT NULL,
  isOnline tinyint(1) NOT NULL DEFAULT 1,
  description_text_id bigint DEFAULT NULL,
  slogan_text_id bigint DEFAULT NULL,
  note_text_id bigint DEFAULT NULL,
  short_description_text_id bigint DEFAULT NULL,
  dish_name_text_id bigint NOT NULL,
  PRIMARY KEY (dish_id)
)
ENGINE = INNODB,
AUTO_INCREMENT = 59,
AVG_ROW_LENGTH = 282,
CHARACTER SET utf8mb4,
COLLATE utf8mb4_unicode_ci,
ROW_FORMAT = DYNAMIC;

ALTER TABLE dish
ADD CONSTRAINT dish_ibfk_1 FOREIGN KEY (category_id)
REFERENCES dish_category (category_id);

ALTER TABLE dish
ADD CONSTRAINT fk_dish_desc_text FOREIGN KEY (description_text_id)
REFERENCES i18n_text (text_id);

ALTER TABLE dish
ADD CONSTRAINT fk_dish_name_text FOREIGN KEY (dish_name_text_id)
REFERENCES i18n_text (text_id);

ALTER TABLE dish
ADD CONSTRAINT fk_dish_note_text FOREIGN KEY (note_text_id)
REFERENCES i18n_text (text_id);

ALTER TABLE dish
ADD CONSTRAINT fk_dish_short_desc_text FOREIGN KEY (short_description_text_id)
REFERENCES i18n_text (text_id);

ALTER TABLE dish
ADD CONSTRAINT fk_dish_slogan_text FOREIGN KEY (slogan_text_id)
REFERENCES i18n_text (text_id);

ALTER TABLE dish
ADD CONSTRAINT fk_dish_status_lv FOREIGN KEY (dish_status_lv_id)
REFERENCES lookup_value (value_id);

--
-- Create table `recipe`
--
CREATE TABLE recipe (
  dish_id bigint NOT NULL,
  ingredient_id bigint NOT NULL,
  quantity decimal(12, 3) NOT NULL,
  unit varchar(20) NOT NULL,
  note varchar(255) DEFAULT NULL,
  PRIMARY KEY (dish_id, ingredient_id)
)
ENGINE = INNODB,
CHARACTER SET utf8mb4,
COLLATE utf8mb4_unicode_ci,
ROW_FORMAT = DYNAMIC;

ALTER TABLE recipe
ADD CONSTRAINT fk_recipe_dish FOREIGN KEY (dish_id)
REFERENCES dish (dish_id);

ALTER TABLE recipe
ADD CONSTRAINT fk_recipe_ingredient FOREIGN KEY (ingredient_id)
REFERENCES ingredient (ingredient_id);

--
-- Create table `promotion_target`
--
CREATE TABLE promotion_target (
  target_id bigint NOT NULL AUTO_INCREMENT,
  promotion_id bigint NOT NULL,
  dish_id bigint DEFAULT NULL,
  category_id bigint DEFAULT NULL,
  PRIMARY KEY (target_id)
)
ENGINE = INNODB,
AUTO_INCREMENT = 3,
AVG_ROW_LENGTH = 8192,
CHARACTER SET utf8mb4,
COLLATE utf8mb4_unicode_ci,
ROW_FORMAT = DYNAMIC;

ALTER TABLE promotion_target
ADD CONSTRAINT promotion_target_ibfk_1 FOREIGN KEY (promotion_id)
REFERENCES promotion (promotion_id);

ALTER TABLE promotion_target
ADD CONSTRAINT promotion_target_ibfk_2 FOREIGN KEY (dish_id)
REFERENCES dish (dish_id);

ALTER TABLE promotion_target
ADD CONSTRAINT promotion_target_ibfk_3 FOREIGN KEY (category_id)
REFERENCES dish_category (category_id);

--
-- Create table `promotion_rule`
--
CREATE TABLE promotion_rule (
  rule_id bigint NOT NULL AUTO_INCREMENT,
  promotion_id bigint NOT NULL,
  min_order_value decimal(14, 2) DEFAULT NULL,
  min_quantity int DEFAULT NULL,
  required_dish_id bigint DEFAULT NULL,
  required_category_id bigint DEFAULT NULL,
  PRIMARY KEY (rule_id)
)
ENGINE = INNODB,
AUTO_INCREMENT = 13,
AVG_ROW_LENGTH = 2048,
CHARACTER SET utf8mb4,
COLLATE utf8mb4_unicode_ci,
ROW_FORMAT = DYNAMIC;

ALTER TABLE promotion_rule
ADD CONSTRAINT promotion_rule_ibfk_1 FOREIGN KEY (promotion_id)
REFERENCES promotion (promotion_id);

ALTER TABLE promotion_rule
ADD CONSTRAINT promotion_rule_ibfk_2 FOREIGN KEY (required_dish_id)
REFERENCES dish (dish_id);

ALTER TABLE promotion_rule
ADD CONSTRAINT promotion_rule_ibfk_3 FOREIGN KEY (required_category_id)
REFERENCES dish_category (category_id);

--
-- Create table `dish_tag`
--
CREATE TABLE dish_tag (
  dish_tag_id bigint NOT NULL AUTO_INCREMENT,
  dish_id bigint NOT NULL,
  tag_id int UNSIGNED NOT NULL,
  PRIMARY KEY (dish_tag_id)
)
ENGINE = INNODB,
AUTO_INCREMENT = 126,
AVG_ROW_LENGTH = 131,
CHARACTER SET utf8mb4,
COLLATE utf8mb4_unicode_ci,
ROW_FORMAT = DYNAMIC;

ALTER TABLE dish_tag
ADD CONSTRAINT FK_dish_tag_dish_dish_id FOREIGN KEY (dish_id)
REFERENCES dish (dish_id);

ALTER TABLE dish_tag
ADD CONSTRAINT FK_dish_tag_lookup_value_value_id FOREIGN KEY (tag_id)
REFERENCES lookup_value (value_id);

--
-- Create table `dish_media`
--
CREATE TABLE dish_media (
  dish_id bigint NOT NULL,
  media_id bigint NOT NULL,
  is_primary tinyint(1) DEFAULT 0,
  PRIMARY KEY (dish_id, media_id)
)
ENGINE = INNODB,
AVG_ROW_LENGTH = 2048,
CHARACTER SET utf8mb4,
COLLATE utf8mb4_unicode_ci,
ROW_FORMAT = DYNAMIC;

ALTER TABLE dish_media
ADD CONSTRAINT dish_media_ibfk_1 FOREIGN KEY (dish_id)
REFERENCES dish (dish_id);

ALTER TABLE dish_media
ADD CONSTRAINT dish_media_ibfk_2 FOREIGN KEY (media_id)
REFERENCES media_asset (media_id);

--
-- Create table `tax`
--
CREATE TABLE tax (
  tax_id bigint NOT NULL AUTO_INCREMENT,
  tax_name varchar(100) NOT NULL,
  tax_rate decimal(5, 2) NOT NULL COMMENT 'Percentage value, e.g., 8.00 for 8%',
  tax_type enum ('INCLUSIVE', 'EXCLUSIVE') NOT NULL DEFAULT 'EXCLUSIVE',
  is_active tinyint(1) NOT NULL DEFAULT 1,
  is_default tinyint(1) NOT NULL DEFAULT 0,
  created_at datetime DEFAULT CURRENT_TIMESTAMP,
  updated_at datetime DEFAULT CURRENT_TIMESTAMP ON UPDATE CURRENT_TIMESTAMP,
  PRIMARY KEY (tax_id)
)
ENGINE = INNODB,
AUTO_INCREMENT = 4,
AVG_ROW_LENGTH = 8192,
CHARACTER SET utf8mb4,
COLLATE utf8mb4_unicode_ci,
ROW_FORMAT = DYNAMIC;

--
-- Create table `customer`
--
CREATE TABLE customer (
  customer_id bigint NOT NULL AUTO_INCREMENT,
  full_name varchar(150) DEFAULT NULL,
  phone varchar(30) NOT NULL,
  email varchar(150) DEFAULT NULL,
  is_member tinyint(1) DEFAULT 0,
  loyalty_points int DEFAULT 0,
  created_at datetime DEFAULT CURRENT_TIMESTAMP,
  PRIMARY KEY (customer_id)
)
ENGINE = INNODB,
AUTO_INCREMENT = 93,
AVG_ROW_LENGTH = 528,
CHARACTER SET utf8mb4,
COLLATE utf8mb4_unicode_ci,
ROW_FORMAT = DYNAMIC;

--
-- Create table `reservation`
--
CREATE TABLE reservation (
  reservation_id bigint NOT NULL AUTO_INCREMENT,
  customer_id bigint DEFAULT NULL,
  customer_name varchar(150) NOT NULL,
  phone varchar(30) NOT NULL,
  email varchar(150) DEFAULT NULL,
  party_size int NOT NULL,
  reserved_time datetime NOT NULL,
  created_at datetime DEFAULT CURRENT_TIMESTAMP,
  source_lv_id int UNSIGNED NOT NULL,
  notes varchar(500) DEFAULT NULL,
  reservation_status_lv_id int UNSIGNED NOT NULL,
  PRIMARY KEY (reservation_id)
)
ENGINE = INNODB,
AUTO_INCREMENT = 117,
AVG_ROW_LENGTH = 142,
CHARACTER SET utf8mb4,
COLLATE utf8mb4_unicode_ci,
ROW_FORMAT = DYNAMIC;

ALTER TABLE reservation
ADD CONSTRAINT fk_reservation_source_lv FOREIGN KEY (source_lv_id)
REFERENCES lookup_value (value_id);

ALTER TABLE reservation
ADD CONSTRAINT fk_reservation_status_lv FOREIGN KEY (reservation_status_lv_id)
REFERENCES lookup_value (value_id);

ALTER TABLE reservation
ADD CONSTRAINT reservation_ibfk_1 FOREIGN KEY (customer_id)
REFERENCES customer (customer_id);

--
-- Create table `reservation_table`
--
CREATE TABLE reservation_table (
  reservation_id bigint NOT NULL,
  table_id bigint NOT NULL,
  PRIMARY KEY (reservation_id, table_id)
)
ENGINE = INNODB,
AVG_ROW_LENGTH = 104,
CHARACTER SET utf8mb4,
COLLATE utf8mb4_unicode_ci,
ROW_FORMAT = DYNAMIC;

ALTER TABLE reservation_table
ADD CONSTRAINT reservation_table_ibfk_1 FOREIGN KEY (reservation_id)
REFERENCES reservation (reservation_id);

ALTER TABLE reservation_table
ADD CONSTRAINT reservation_table_ibfk_2 FOREIGN KEY (table_id)
REFERENCES restaurant_table (table_id);

--
-- Create table `orders`
--
CREATE TABLE orders (
  order_id bigint NOT NULL AUTO_INCREMENT,
  table_id bigint DEFAULT NULL,
  staff_id bigint DEFAULT NULL,
  created_at datetime DEFAULT CURRENT_TIMESTAMP,
  customer_id bigint NOT NULL,
  updated_at datetime DEFAULT NULL,
  total_amount decimal(14, 2) NOT NULL,
  tip_amount decimal(14, 2) DEFAULT NULL,
  source_lv_id int UNSIGNED NOT NULL,
  order_status_lv_id int UNSIGNED NOT NULL,
  tax_id bigint DEFAULT NULL,
  tax_amount decimal(14, 2) NOT NULL DEFAULT 0.00,
  sub_total_amount decimal(14, 2) NOT NULL DEFAULT 0.00,
  PRIMARY KEY (order_id)
)
ENGINE = INNODB,
AUTO_INCREMENT = 111,
AVG_ROW_LENGTH = 150,
CHARACTER SET utf8mb4,
COLLATE utf8mb4_unicode_ci,
ROW_FORMAT = DYNAMIC;

ALTER TABLE orders
ADD CONSTRAINT FK_orders_customer_id FOREIGN KEY (customer_id)
REFERENCES customer (customer_id);

ALTER TABLE orders
ADD CONSTRAINT fk_orders_source_lv FOREIGN KEY (source_lv_id)
REFERENCES lookup_value (value_id);

ALTER TABLE orders
ADD CONSTRAINT fk_orders_status_lv FOREIGN KEY (order_status_lv_id)
REFERENCES lookup_value (value_id);

ALTER TABLE orders
ADD CONSTRAINT fk_orders_tax FOREIGN KEY (tax_id)
REFERENCES tax (tax_id);

ALTER TABLE orders
ADD CONSTRAINT orders_ibfk_1 FOREIGN KEY (table_id)
REFERENCES restaurant_table (table_id);

ALTER TABLE orders
ADD CONSTRAINT orders_ibfk_2 FOREIGN KEY (staff_id)
REFERENCES staff_account (account_id);

--
-- Create table `payment`
--
CREATE TABLE payment (
  payment_id bigint NOT NULL AUTO_INCREMENT,
  order_id bigint NOT NULL,
  received_amount decimal(14, 2) NOT NULL,
  change_amount decimal(14, 2) NOT NULL,
  paid_at datetime DEFAULT CURRENT_TIMESTAMP,
  method_lv_id int UNSIGNED NOT NULL,
  PRIMARY KEY (payment_id)
)
ENGINE = INNODB,
AUTO_INCREMENT = 65,
AVG_ROW_LENGTH = 264,
CHARACTER SET utf8mb4,
COLLATE utf8mb4_unicode_ci,
ROW_FORMAT = DYNAMIC;

ALTER TABLE payment
ADD CONSTRAINT fk_payment_method_lv FOREIGN KEY (method_lv_id)
REFERENCES lookup_value (value_id);

ALTER TABLE payment
ADD CONSTRAINT payment_ibfk_1 FOREIGN KEY (order_id)
REFERENCES orders (order_id);

--
-- Create table `order_promotion`
--
CREATE TABLE order_promotion (
  order_promotion_id bigint NOT NULL AUTO_INCREMENT,
  order_id bigint NOT NULL,
  promotion_id bigint NOT NULL,
  discount_amount decimal(14, 2) NOT NULL,
  applied_at datetime DEFAULT CURRENT_TIMESTAMP,
  PRIMARY KEY (order_promotion_id)
)
ENGINE = INNODB,
AUTO_INCREMENT = 32,
AVG_ROW_LENGTH = 655,
CHARACTER SET utf8mb4,
COLLATE utf8mb4_unicode_ci,
ROW_FORMAT = DYNAMIC;

ALTER TABLE order_promotion
ADD CONSTRAINT FK_invoice_promotion_order_id FOREIGN KEY (order_id)
REFERENCES orders (order_id);

ALTER TABLE order_promotion
ADD CONSTRAINT order_promotion_ibfk_2 FOREIGN KEY (promotion_id)
REFERENCES promotion (promotion_id);

--
-- Create table `order_item`
--
CREATE TABLE order_item (
  order_item_id bigint NOT NULL AUTO_INCREMENT,
  order_id bigint NOT NULL,
  dish_id bigint NOT NULL,
  quantity int NOT NULL,
  price decimal(12, 2) NOT NULL,
  item_status tinyint UNSIGNED NOT NULL DEFAULT 1 COMMENT 'OrderItemStatus:1=CREATED,2=IN_PROGRESS,3=READY,4=SERVED,5=REJECTED',
  reject_reason varchar(255) DEFAULT NULL,
  Note longtext DEFAULT NULL,
  item_status_lv_id int UNSIGNED NOT NULL,
  PRIMARY KEY (order_item_id)
)
ENGINE = INNODB,
AUTO_INCREMENT = 259,
AVG_ROW_LENGTH = 64,
CHARACTER SET utf8mb4,
COLLATE utf8mb4_unicode_ci,
ROW_FORMAT = DYNAMIC;

ALTER TABLE order_item
ADD CONSTRAINT fk_order_item_status_lv FOREIGN KEY (item_status_lv_id)
REFERENCES lookup_value (value_id);

ALTER TABLE order_item
ADD CONSTRAINT order_item_ibfk_1 FOREIGN KEY (order_id)
REFERENCES orders (order_id);

ALTER TABLE order_item
ADD CONSTRAINT order_item_ibfk_2 FOREIGN KEY (dish_id)
REFERENCES dish (dish_id);

--
-- Create table `service_error`
--
CREATE TABLE service_error (
  error_id bigint NOT NULL AUTO_INCREMENT,
  staff_id bigint NOT NULL,
  order_id bigint DEFAULT NULL,
  order_item_id bigint DEFAULT NULL,
  table_id bigint DEFAULT NULL,
  category_id bigint NOT NULL,
  description varchar(500) NOT NULL,
  penalty_amount decimal(12, 2) DEFAULT 0.00,
  is_resolved tinyint(1) DEFAULT 0,
  resolved_by bigint DEFAULT NULL,
  resolved_at datetime DEFAULT NULL,
  created_at datetime DEFAULT CURRENT_TIMESTAMP,
  severity_lv_id int UNSIGNED NOT NULL,
  PRIMARY KEY (error_id)
)
ENGINE = INNODB,
CHARACTER SET utf8mb4,
COLLATE utf8mb4_unicode_ci,
ROW_FORMAT = DYNAMIC;

ALTER TABLE service_error
ADD CONSTRAINT fk_service_error_severity_lv FOREIGN KEY (severity_lv_id)
REFERENCES lookup_value (value_id);

ALTER TABLE service_error
ADD CONSTRAINT service_error_ibfk_1 FOREIGN KEY (staff_id)
REFERENCES staff_account (account_id);

ALTER TABLE service_error
ADD CONSTRAINT service_error_ibfk_2 FOREIGN KEY (order_id)
REFERENCES orders (order_id);

ALTER TABLE service_error
ADD CONSTRAINT service_error_ibfk_3 FOREIGN KEY (order_item_id)
REFERENCES order_item (order_item_id);

ALTER TABLE service_error
ADD CONSTRAINT service_error_ibfk_4 FOREIGN KEY (table_id)
REFERENCES restaurant_table (table_id);

ALTER TABLE service_error
ADD CONSTRAINT service_error_ibfk_5 FOREIGN KEY (category_id)
REFERENCES service_error_category (category_id);

ALTER TABLE service_error
ADD CONSTRAINT service_error_ibfk_7 FOREIGN KEY (resolved_by)
REFERENCES staff_account (account_id);

--
-- Create table `coupon`
--
CREATE TABLE coupon (
  coupon_id bigint NOT NULL AUTO_INCREMENT,
  coupon_code varchar(50) NOT NULL,
  coupon_name varchar(200) NOT NULL,
  description varchar(255) DEFAULT NULL,
  start_time datetime NOT NULL,
  end_time datetime NOT NULL,
  discount_value decimal(10, 2) NOT NULL,
  max_usage int DEFAULT NULL,
  used_count int DEFAULT 0,
  created_at datetime DEFAULT CURRENT_TIMESTAMP,
  type_lv_id int UNSIGNED NOT NULL,
  coupon_status_lv_id int UNSIGNED NOT NULL,
  customer_id bigint DEFAULT NULL,
  PRIMARY KEY (coupon_id)
)
ENGINE = INNODB,
AUTO_INCREMENT = 6,
AVG_ROW_LENGTH = 3276,
CHARACTER SET utf8mb4,
COLLATE utf8mb4_unicode_ci,
ROW_FORMAT = DYNAMIC;

ALTER TABLE coupon
ADD CONSTRAINT fk_coupon_customer FOREIGN KEY (customer_id)
REFERENCES customer (customer_id) ON DELETE SET NULL;

ALTER TABLE coupon
ADD CONSTRAINT fk_coupon_status_lv FOREIGN KEY (coupon_status_lv_id)
REFERENCES lookup_value (value_id);

ALTER TABLE coupon
ADD CONSTRAINT fk_coupon_type_lv FOREIGN KEY (type_lv_id)
REFERENCES lookup_value (value_id);

--
-- Create table `order_coupon`
--
CREATE TABLE order_coupon (
  order_coupon_id bigint NOT NULL AUTO_INCREMENT,
  order_id bigint NOT NULL,
  coupon_id bigint NOT NULL,
  discount_amount decimal(14, 2) NOT NULL,
  applied_at datetime DEFAULT CURRENT_TIMESTAMP,
  PRIMARY KEY (order_coupon_id)
)
ENGINE = INNODB,
AUTO_INCREMENT = 12,
AVG_ROW_LENGTH = 1489,
CHARACTER SET utf8mb4,
COLLATE utf8mb4_unicode_ci,
ROW_FORMAT = DYNAMIC;

ALTER TABLE order_coupon
ADD CONSTRAINT fk_order_coupon_coupon FOREIGN KEY (coupon_id)
REFERENCES coupon (coupon_id);

ALTER TABLE order_coupon
ADD CONSTRAINT fk_order_coupon_order FOREIGN KEY (order_id)
REFERENCES orders (order_id);

--
-- Create table `supplier`
--
CREATE TABLE supplier (
  supplier_id bigint NOT NULL AUTO_INCREMENT,
  supplier_name varchar(200) NOT NULL,
  phone varchar(50) DEFAULT NULL,
  email varchar(150) DEFAULT NULL,
  address varchar(500) CHARACTER SET utf8mb4 COLLATE utf8mb4_0900_ai_ci DEFAULT NULL,
  tax_code varchar(50) CHARACTER SET utf8mb4 COLLATE utf8mb4_0900_ai_ci DEFAULT NULL,
  PRIMARY KEY (supplier_id)
)
ENGINE = INNODB,
AUTO_INCREMENT = 4,
AVG_ROW_LENGTH = 5461,
CHARACTER SET utf8mb4,
COLLATE utf8mb4_unicode_ci,
ROW_FORMAT = DYNAMIC;

--
-- Create table `inventory_transaction`
--
CREATE TABLE inventory_transaction (
  transaction_id bigint NOT NULL AUTO_INCREMENT,
  created_by bigint DEFAULT NULL,
  created_at datetime DEFAULT CURRENT_TIMESTAMP,
  note varchar(500) DEFAULT NULL,
  type_lv_id int UNSIGNED NOT NULL,
  status_lv_id int UNSIGNED NOT NULL,
  supplier_id bigint DEFAULT NULL,
  approved_at datetime DEFAULT NULL,
  approved_by bigint DEFAULT NULL,
  export_reason_lv_id int UNSIGNED DEFAULT NULL,
  stock_check_area_note varchar(500) DEFAULT NULL,
  submitted_at datetime DEFAULT NULL,
  transaction_code varchar(30) DEFAULT NULL,
  PRIMARY KEY (transaction_id)
)
ENGINE = INNODB,
AUTO_INCREMENT = 15,
AVG_ROW_LENGTH = 2048,
CHARACTER SET utf8mb4,
COLLATE utf8mb4_unicode_ci,
ROW_FORMAT = DYNAMIC;

ALTER TABLE inventory_transaction
ADD CONSTRAINT fk_inventory_transaction_approved_by FOREIGN KEY (approved_by)
REFERENCES staff_account (account_id);

ALTER TABLE inventory_transaction
ADD CONSTRAINT fk_inventory_transaction_staff FOREIGN KEY (created_by)
REFERENCES staff_account (account_id);

ALTER TABLE inventory_transaction
ADD CONSTRAINT FK_inventory_transaction_supplier_supplier_id FOREIGN KEY (supplier_id)
REFERENCES supplier (supplier_id);

ALTER TABLE inventory_transaction
ADD CONSTRAINT fk_inventory_tx_export_reason_lv FOREIGN KEY (export_reason_lv_id)
REFERENCES lookup_value (value_id);

ALTER TABLE inventory_transaction
ADD CONSTRAINT fk_inventory_tx_status_lv FOREIGN KEY (status_lv_id)
REFERENCES lookup_value (value_id);

ALTER TABLE inventory_transaction
ADD CONSTRAINT fk_inventory_tx_type_lv FOREIGN KEY (type_lv_id)
REFERENCES lookup_value (value_id);

--
-- Create table `inventory_transaction_media`
--
CREATE TABLE inventory_transaction_media (
  transaction_id bigint NOT NULL,
  media_id bigint NOT NULL,
  is_primary tinyint(1) DEFAULT 0,
  PRIMARY KEY (transaction_id, media_id)
)
ENGINE = INNODB,
CHARACTER SET utf8mb4,
COLLATE utf8mb4_unicode_ci,
ROW_FORMAT = DYNAMIC;

ALTER TABLE inventory_transaction_media
ADD CONSTRAINT fk_inventory_transaction_media_asset FOREIGN KEY (media_id)
REFERENCES media_asset (media_id) ON DELETE CASCADE;

ALTER TABLE inventory_transaction_media
ADD CONSTRAINT fk_inventory_transaction_media_transaction FOREIGN KEY (transaction_id)
REFERENCES inventory_transaction (transaction_id) ON DELETE CASCADE;

--
-- Create table `inventory_transaction_item`
--
CREATE TABLE inventory_transaction_item (
  transaction_item_id bigint NOT NULL AUTO_INCREMENT,
  transaction_id bigint NOT NULL,
  ingredient_id bigint NOT NULL,
  quantity decimal(14, 3) NOT NULL,
  note varchar(255) DEFAULT NULL,
  actual_quantity decimal(14, 3) DEFAULT NULL,
  system_quantity decimal(14, 3) DEFAULT NULL,
  unit_lv_id int UNSIGNED NOT NULL DEFAULT 0,
  unit_price decimal(14, 2) DEFAULT NULL,
  variance_reason_lv_id int UNSIGNED DEFAULT NULL,
  PRIMARY KEY (transaction_item_id)
)
ENGINE = INNODB,
AUTO_INCREMENT = 11,
AVG_ROW_LENGTH = 2048,
CHARACTER SET utf8mb4,
COLLATE utf8mb4_unicode_ci,
ROW_FORMAT = DYNAMIC;

ALTER TABLE inventory_transaction_item
ADD CONSTRAINT fk_inventory_transaction_item_ingredient FOREIGN KEY (ingredient_id)
REFERENCES ingredient (ingredient_id);

ALTER TABLE inventory_transaction_item
ADD CONSTRAINT fk_inventory_transaction_item_transaction FOREIGN KEY (transaction_id)
REFERENCES inventory_transaction (transaction_id) ON DELETE CASCADE;

ALTER TABLE inventory_transaction_item
ADD CONSTRAINT fk_inventory_tx_item_unit_lv FOREIGN KEY (unit_lv_id)
REFERENCES lookup_value (value_id);

ALTER TABLE inventory_transaction_item
ADD CONSTRAINT fk_inventory_tx_item_variance_reason_lv FOREIGN KEY (variance_reason_lv_id)
REFERENCES lookup_value (value_id);

--
-- Create table `ingredient_supplier`
--
CREATE TABLE ingredient_supplier (
  ingredient_supplier_id bigint NOT NULL AUTO_INCREMENT,
  supplier_id bigint DEFAULT NULL,
  ingredient_id bigint DEFAULT NULL,
  created_at datetime DEFAULT NULL,
  PRIMARY KEY (ingredient_supplier_id)
)
ENGINE = INNODB,
AUTO_INCREMENT = 9,
AVG_ROW_LENGTH = 3276,
CHARACTER SET utf8mb4,
COLLATE utf8mb4_unicode_ci,
ROW_FORMAT = DYNAMIC;

ALTER TABLE ingredient_supplier
ADD CONSTRAINT FK_ingredient_supplier_ingredient_ingredient_id FOREIGN KEY (ingredient_id)
REFERENCES ingredient (ingredient_id);

ALTER TABLE ingredient_supplier
ADD CONSTRAINT FK_ingredient_supplier_supplier_supplier_id FOREIGN KEY (supplier_id)
REFERENCES supplier (supplier_id);

--
-- Create table `permission`
--
CREATE TABLE permission (
  permission_id bigint NOT NULL AUTO_INCREMENT,
  screen_code varchar(100) NOT NULL,
  action_code varchar(20) NOT NULL,
  PRIMARY KEY (permission_id)
)
ENGINE = INNODB,
AUTO_INCREMENT = 73,
AVG_ROW_LENGTH = 227,
CHARACTER SET utf8mb4,
COLLATE utf8mb4_unicode_ci,
ROW_FORMAT = DYNAMIC;

--
-- Create table `role_permission`
--
CREATE TABLE role_permission (
  role_id bigint NOT NULL,
  permission_id bigint NOT NULL,
  PRIMARY KEY (role_id, permission_id)
)
ENGINE = INNODB,
AVG_ROW_LENGTH = 130,
CHARACTER SET utf8mb4,
COLLATE utf8mb4_unicode_ci,
ROW_FORMAT = DYNAMIC;

ALTER TABLE role_permission
ADD CONSTRAINT role_permission_ibfk_1 FOREIGN KEY (role_id)
REFERENCES role (role_id);

ALTER TABLE role_permission
ADD CONSTRAINT role_permission_ibfk_2 FOREIGN KEY (permission_id)
REFERENCES permission (permission_id);

--
-- Create table `notifications`
--
CREATE TABLE notifications (
  notification_id bigint NOT NULL AUTO_INCREMENT,
  type varchar(50) NOT NULL,
  title varchar(200) NOT NULL,
  body varchar(1000) DEFAULT NULL,
  priority varchar(20) NOT NULL,
  require_ack tinyint(1) NOT NULL,
  sound_key varchar(100) DEFAULT NULL,
  action_url varchar(500) DEFAULT NULL,
  entity_type varchar(50) DEFAULT NULL,
  entity_id varchar(50) DEFAULT NULL,
  metadata_json json DEFAULT NULL,
  target_permissions varchar(500) DEFAULT NULL,
  target_user_ids varchar(500) DEFAULT NULL,
  created_at datetime NOT NULL DEFAULT CURRENT_TIMESTAMP,
  PRIMARY KEY (notification_id)
)
ENGINE = INNODB,
AUTO_INCREMENT = 542,
AVG_ROW_LENGTH = 313,
CHARACTER SET utf8mb4,
COLLATE utf8mb4_unicode_ci,
ROW_FORMAT = DYNAMIC;

--
-- Create table `notification_read_states`
--
CREATE TABLE notification_read_states (
  notification_read_state_id bigint NOT NULL AUTO_INCREMENT,
  notification_id bigint NOT NULL,
  user_id bigint NOT NULL,
  is_read tinyint(1) NOT NULL DEFAULT 0,
  is_acknowledged tinyint(1) NOT NULL DEFAULT 0,
  read_at datetime DEFAULT NULL,
  acknowledged_at datetime DEFAULT NULL,
  created_at datetime NOT NULL DEFAULT CURRENT_TIMESTAMP,
  PRIMARY KEY (notification_read_state_id)
)
ENGINE = INNODB,
AUTO_INCREMENT = 449,
AVG_ROW_LENGTH = 146,
CHARACTER SET utf8mb4,
COLLATE utf8mb4_unicode_ci,
ROW_FORMAT = DYNAMIC;

ALTER TABLE notification_read_states
ADD CONSTRAINT fk_nrs_notification FOREIGN KEY (notification_id)
REFERENCES notifications (notification_id) ON DELETE CASCADE;

--
-- Create table `notification_preferences`
--
CREATE TABLE notification_preferences (
  notification_preference_id bigint NOT NULL AUTO_INCREMENT,
  user_id bigint NOT NULL,
  notification_type varchar(50) NOT NULL,
  is_enabled tinyint(1) NOT NULL DEFAULT 1,
  sound_enabled tinyint(1) NOT NULL DEFAULT 1,
  created_at datetime NOT NULL DEFAULT CURRENT_TIMESTAMP,
  updated_at datetime NOT NULL DEFAULT CURRENT_TIMESTAMP,
  PRIMARY KEY (notification_preference_id)
)
ENGINE = INNODB,
AUTO_INCREMENT = 26,
CHARACTER SET utf8mb4,
COLLATE utf8mb4_unicode_ci,
ROW_FORMAT = DYNAMIC;

--
-- Create table `email_template`
--
CREATE TABLE email_template (
  TemplateId bigint NOT NULL AUTO_INCREMENT,
  TemplateCode varchar(100) NOT NULL,
  TemplateName varchar(200) NOT NULL,
  Subject varchar(500) NOT NULL,
  BodyHtml longtext NOT NULL,
  Description varchar(1000) DEFAULT NULL,
  CreatedAt datetime(6) NOT NULL DEFAULT CURRENT_TIMESTAMP(6),
  UpdatedAt datetime(6) DEFAULT CURRENT_TIMESTAMP(6) ON UPDATE CURRENT_TIMESTAMP(6),
  PRIMARY KEY (TemplateId)
)
ENGINE = INNODB,
AUTO_INCREMENT = 6,
AVG_ROW_LENGTH = 4096,
CHARACTER SET utf8mb4,
COLLATE utf8mb4_unicode_ci,
ROW_FORMAT = DYNAMIC;
