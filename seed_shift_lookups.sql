-- ============================================================
--  Shift Management Lookup Seed
--  Adds lookup_type rows (21-24) and all lookup_value rows
--  Safe to re-run: uses INSERT IGNORE throughout
-- ============================================================

-- ?? lookup_type ??????????????????????????????????????????????

INSERT IGNORE INTO `lookup_type` (`type_id`, `type_code`, `type_name`, `is_configurable`, `is_system`, `description`)
VALUES
  (21, 'SHIFT_TYPE',     'Shift Type',   1, 1, 'Type of shift: MORNING, LUNCH, EVENING'),
  (22, 'SHIFT_STATUS',             'Shift Status',        0, 1, 'Lifecycle status of a shift schedule'),
  (23, 'SHIFT_ASSIGNMENT_STATUS',  'Shift Assignment Status',  0, 1, 'Status of a staff assignment to a shift'),
  (24, 'ATTENDANCE_STATUS',        'Attendance Status',        0, 1, 'Attendance outcome for a shift assignment');

-- ?? lookup_value: SHIFT_TYPE (type_id = 21) ??????????????????

INSERT IGNORE INTO `lookup_value` (`type_id`, `value_code`, `value_name`, `sort_order`, `is_active`, `is_system`, `locked`)
VALUES
  (21, 'MORNING', 'Morning', 1, 1, 1, 1),
  (21, 'LUNCH',   'Lunch',   2, 1, 1, 1),
  (21, 'EVENING', 'Evening', 3, 1, 1, 1);

-- ?? lookup_value: SHIFT_STATUS (type_id = 22) ????????????????

INSERT IGNORE INTO `lookup_value` (`type_id`, `value_code`, `value_name`, `sort_order`, `is_active`, `is_system`, `locked`)
VALUES
  (22, 'DRAFT',     'Draft',   1, 1, 1, 1),
  (22, 'PUBLISHED', 'Published', 2, 1, 1, 1),
  (22, 'CLOSED',    'Closed',    3, 1, 1, 1),
  (22, 'CANCELLED', 'Cancelled', 4, 1, 1, 1);

-- ?? lookup_value: SHIFT_ASSIGNMENT_STATUS (type_id = 23) ?????

INSERT IGNORE INTO `lookup_value` (`type_id`, `value_code`, `value_name`, `sort_order`, `is_active`, `is_system`, `locked`)
VALUES
  (23, 'ASSIGNED',  'Assigned',  1, 1, 1, 1),
  (23, 'CONFIRMED', 'Confirmed', 2, 1, 1, 1),
  (23, 'CANCELLED', 'Cancelled', 3, 1, 1, 1);

-- ?? lookup_value: ATTENDANCE_STATUS (type_id = 24) ???????????

INSERT IGNORE INTO `lookup_value` (`type_id`, `value_code`, `value_name`, `sort_order`, `is_active`, `is_system`, `locked`)
VALUES
  (24, 'SCHEDULED',   'Scheduled',   1, 1, 1, 1),
(24, 'ACTIVE',      'Active',      2, 1, 1, 1),
  (24, 'COMPLETED',   'Completed',   3, 1, 1, 1),
  (24, 'LATE',      'Late',        4, 1, 1, 1),
  (24, 'ABSENT',      'Absent',      5, 1, 1, 1),
  (24, 'EARLY_LEAVE', 'Early Leave', 6, 1, 1, 1),
  (24, 'EXCUSED',     'Excused', 7, 1, 1, 1);

-- ?? permission rows for shift module ?????????????????????????
-- screen_code = 'SHIFT', action_code = right-hand side of the colon

INSERT IGNORE INTO `permission` (`screen_code`, `action_code`)
VALUES
  ('SHIFT', 'READ'),
  ('SHIFT', 'SCHEDULE'),
  ('SHIFT', 'ASSIGN'),
  ('SHIFT', 'CHECK_IN'),
  ('SHIFT', 'CHECK_OUT'),
  ('SHIFT', 'ADJUST_ATTENDANCE'),
  ('SHIFT', 'REPORT_READ'),
  ('SHIFT', 'CLOSE');
