-- ============================================================
-- Seed Script: Order History Test Data (gọn)
-- Database: restaurant_mgmt (MySQL 8)
--
-- Yêu cầu đã có sẵn trong DB:
--   Staff:        account_id = 1, 2
--   order_status: 28=PENDING, 29=IN_PROGRESS, 30=COMPLETED, 31=CANCELLED
--   source:       58=DINE_IN, 59=TAKEAWAY, 60=DELIVERY
--   item_status:  35=CREATED, 36=IN_PROGRESS, 37=READY, 38=SERVED, 39=REJECTED
--   Dish/Table/Permission: đã có sẵn
--
-- TRƯỚC KHI CHẠY: thay @d1..@d5 và @t1..@t4 bằng dish_id/table_id thực
-- HOẶC chạy block SET bên dưới nếu DB có dữ liệu dish/table chuẩn
-- ============================================================

SET FOREIGN_KEY_CHECKS = 0;

-- ----------------------------------------------------------------
-- Lấy 5 dish_id và 4 table_id đầu tiên từ DB thực tế
-- (Đổi lại nếu bạn muốn dùng dish/table cụ thể)
-- ----------------------------------------------------------------
SET @d1 = (SELECT dish_id FROM dish WHERE dish_status_lv_id = 42 ORDER BY dish_id LIMIT 1 OFFSET 0);
SET @d2 = (SELECT dish_id FROM dish WHERE dish_status_lv_id = 42 ORDER BY dish_id LIMIT 1 OFFSET 1);
SET @d3 = (SELECT dish_id FROM dish WHERE dish_status_lv_id = 42 ORDER BY dish_id LIMIT 1 OFFSET 2);
SET @d4 = (SELECT dish_id FROM dish WHERE dish_status_lv_id = 42 ORDER BY dish_id LIMIT 1 OFFSET 3);
SET @d5 = (SELECT dish_id FROM dish WHERE dish_status_lv_id = 42 ORDER BY dish_id LIMIT 1 OFFSET 4);

SET @p1 = (SELECT price FROM dish WHERE dish_id = @d1);
SET @p2 = (SELECT price FROM dish WHERE dish_id = @d2);
SET @p3 = (SELECT price FROM dish WHERE dish_id = @d3);
SET @p4 = (SELECT price FROM dish WHERE dish_id = @d4);
SET @p5 = (SELECT price FROM dish WHERE dish_id = @d5);

SET @t1 = (SELECT table_id FROM restaurant_table ORDER BY table_id LIMIT 1 OFFSET 0);
SET @t2 = (SELECT table_id FROM restaurant_table ORDER BY table_id LIMIT 1 OFFSET 1);
SET @t3 = (SELECT table_id FROM restaurant_table ORDER BY table_id LIMIT 1 OFFSET 2);
SET @t4 = (SELECT table_id FROM restaurant_table ORDER BY table_id LIMIT 1 OFFSET 3);

-- ----------------------------------------------------------------
-- 1. CUSTOMER
-- ----------------------------------------------------------------
INSERT IGNORE INTO customer (full_name, phone, email, created_at) VALUES
('Nguyễn Văn An',  '0901111111', 'an.nguyen@gmail.com',  NOW()),
('Trần Thị Bình',  '0902222222', 'binh.tran@gmail.com',  NOW()),
('Lê Hoàng Cát',   '0903333333', 'cat.le@gmail.com',     NOW()),
('Phạm Minh Đức',  '0904444444', 'duc.pham@gmail.com',   NOW()),
('Đỗ Thanh Em',    '0905555555', NULL,                    NOW()),
('Vũ Kiều Fam',    '0906666666', NULL,                    NOW());

-- Lấy customer_id vừa insert
SET @c1 = (SELECT customer_id FROM customer WHERE phone = '0901111111');
SET @c2 = (SELECT customer_id FROM customer WHERE phone = '0902222222');
SET @c3 = (SELECT customer_id FROM customer WHERE phone = '0903333333');
SET @c4 = (SELECT customer_id FROM customer WHERE phone = '0904444444');
SET @c5 = (SELECT customer_id FROM customer WHERE phone = '0905555555');
SET @c6 = (SELECT customer_id FROM customer WHERE phone = '0906666666');

-- ----------------------------------------------------------------
-- 2. ORDERS
-- ----------------------------------------------------------------
INSERT INTO orders (table_id, staff_id, customer_id, total_amount, tip_amount, source_lv_id, order_status_lv_id, created_at, updated_at) VALUES
-- 5 COMPLETED
(@t1, 1, @c1, @p1 + @p2*2 + @p3,  20000, 58, 30, DATE_SUB(NOW(), INTERVAL 3 DAY),    DATE_SUB(NOW(), INTERVAL 3 DAY)),
(@t2, 1, @c2, @p3 + @p4 + @p5,    NULL,  58, 30, DATE_SUB(NOW(), INTERVAL 2 DAY),    DATE_SUB(NOW(), INTERVAL 2 DAY)),
(@t3, 2, @c3, @p1*2 + @p5,        30000, 58, 30, DATE_SUB(NOW(), INTERVAL 2 DAY),    DATE_SUB(NOW(), INTERVAL 2 DAY)),
(@t1, 1, @c4, @p1 + @p3,          NULL,  59, 30, DATE_SUB(NOW(), INTERVAL 1 DAY),    DATE_SUB(NOW(), INTERVAL 1 DAY)),
(@t4, 2, @c5, @p2 + @p4 + @p5,    25000, 60, 30, DATE_SUB(NOW(), INTERVAL 1 DAY),    DATE_SUB(NOW(), INTERVAL 1 DAY)),
-- 2 CANCELLED
(@t2, 1, @c6, @p1,                 NULL,  58, 31, DATE_SUB(NOW(), INTERVAL 4 HOUR), DATE_SUB(NOW(), INTERVAL 3 HOUR)),
(@t4, 2, @c1, @p4,                 NULL,  59, 31, DATE_SUB(NOW(), INTERVAL 5 HOUR), DATE_SUB(NOW(), INTERVAL 4 HOUR)),
-- 3 IN_PROGRESS
(@t1, 1, @c2, @p5*2 + @p1 + @p2,  NULL,  58, 29, DATE_SUB(NOW(), INTERVAL 1 HOUR),   NULL),
(@t2, 2, @c3, @p3 + @p2,           NULL,  58, 29, DATE_SUB(NOW(), INTERVAL 45 MINUTE), NULL),
(@t3, 1, @c4, @p1 + @p4,           NULL,  60, 29, DATE_SUB(NOW(), INTERVAL 30 MINUTE), NULL),
-- 2 PENDING
(@t4, 1, @c5, @p5 + @p4,           NULL,  58, 28, DATE_SUB(NOW(), INTERVAL 10 MINUTE), NULL),
(@t1, 2, @c6, @p5*2 + @p1 + @p3,  NULL,  59, 28, DATE_SUB(NOW(), INTERVAL 5 MINUTE),  NULL);

-- ----------------------------------------------------------------
-- 3. ORDER_ITEM  (dùng order_id tự tăng vừa tạo)
-- ----------------------------------------------------------------
-- Lấy 12 order_id mới nhất (theo thứ tự insert)
SET @o1  = (SELECT order_id FROM orders ORDER BY order_id DESC LIMIT 1 OFFSET 11);
SET @o2  = (SELECT order_id FROM orders ORDER BY order_id DESC LIMIT 1 OFFSET 10);
SET @o3  = (SELECT order_id FROM orders ORDER BY order_id DESC LIMIT 1 OFFSET 9);
SET @o4  = (SELECT order_id FROM orders ORDER BY order_id DESC LIMIT 1 OFFSET 8);
SET @o5  = (SELECT order_id FROM orders ORDER BY order_id DESC LIMIT 1 OFFSET 7);
SET @o6  = (SELECT order_id FROM orders ORDER BY order_id DESC LIMIT 1 OFFSET 6);
SET @o7  = (SELECT order_id FROM orders ORDER BY order_id DESC LIMIT 1 OFFSET 5);
SET @o8  = (SELECT order_id FROM orders ORDER BY order_id DESC LIMIT 1 OFFSET 4);
SET @o9  = (SELECT order_id FROM orders ORDER BY order_id DESC LIMIT 1 OFFSET 3);
SET @o10 = (SELECT order_id FROM orders ORDER BY order_id DESC LIMIT 1 OFFSET 2);
SET @o11 = (SELECT order_id FROM orders ORDER BY order_id DESC LIMIT 1 OFFSET 1);
SET @o12 = (SELECT order_id FROM orders ORDER BY order_id DESC LIMIT 1 OFFSET 0);

INSERT INTO order_item (order_id, dish_id, quantity, price, item_status, item_status_lv_id, reject_reason) VALUES
-- Order 1 COMPLETED
(@o1, @d1, 1, @p1, 4, 38, NULL),
(@o1, @d2, 2, @p2, 4, 38, NULL),
(@o1, @d3, 1, @p3, 4, 38, NULL),
-- Order 2 COMPLETED
(@o2, @d3, 1, @p3, 4, 38, NULL),
(@o2, @d4, 1, @p4, 4, 38, NULL),
(@o2, @d5, 1, @p5, 4, 38, NULL),
-- Order 3 COMPLETED
(@o3, @d1, 2, @p1, 4, 38, NULL),
(@o3, @d5, 1, @p5, 4, 38, NULL),
-- Order 4 COMPLETED
(@o4, @d1, 1, @p1, 4, 38, NULL),
(@o4, @d3, 1, @p3, 4, 38, NULL),
-- Order 5 COMPLETED
(@o5, @d2, 1, @p2, 4, 38, NULL),
(@o5, @d4, 1, @p4, 4, 38, NULL),
(@o5, @d5, 1, @p5, 4, 38, NULL),
-- Order 6 CANCELLED
(@o6, @d1, 1, @p1, 1, 35, NULL),
-- Order 7 CANCELLED
(@o7, @d4, 1, @p4, 5, 39, 'Hết nguyên liệu'),
-- Order 8 IN_PROGRESS
(@o8, @d5, 2, @p5, 2, 36, NULL),
(@o8, @d1, 1, @p1, 3, 37, NULL),
(@o8, @d2, 1, @p2, 3, 37, NULL),
-- Order 9 IN_PROGRESS
(@o9, @d3, 1, @p3, 2, 36, NULL),
(@o9, @d2, 1, @p2, 1, 35, NULL),
-- Order 10 IN_PROGRESS
(@o10, @d1, 1, @p1, 2, 36, NULL),
(@o10, @d4, 1, @p4, 1, 35, NULL),
-- Order 11 PENDING
(@o11, @d5, 1, @p5, 1, 35, NULL),
(@o11, @d4, 1, @p4, 1, 35, NULL),
-- Order 12 PENDING
(@o12, @d5, 2, @p5, 1, 35, NULL),
(@o12, @d1, 1, @p1, 1, 35, NULL),
(@o12, @d3, 1, @p3, 1, 35, NULL);

SET FOREIGN_KEY_CHECKS = 1;

-- ----------------------------------------------------------------
-- Verify
-- ----------------------------------------------------------------
SELECT
    o.order_id,
    t.table_code,
    sa.full_name        AS staff_name,
    c.full_name         AS customer_name,
    lv_src.value_code   AS source,
    lv_st.value_code    AS status,
    o.total_amount,
    o.created_at,
    COUNT(oi.order_item_id) AS item_count
FROM orders o
JOIN restaurant_table t ON o.table_id = t.table_id
JOIN staff_account sa   ON o.staff_id = sa.account_id
JOIN customer c         ON o.customer_id = c.customer_id
JOIN lookup_value lv_src ON o.source_lv_id = lv_src.value_id
JOIN lookup_value lv_st  ON o.order_status_lv_id = lv_st.value_id
LEFT JOIN order_item oi  ON o.order_id = oi.order_id
GROUP BY o.order_id
ORDER BY o.created_at DESC;
