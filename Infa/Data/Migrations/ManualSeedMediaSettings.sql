USE restaurant_mgmt_dev;
INSERT INTO system_setting (setting_key, setting_name, value_type, value_bool, description, is_sensitive, updated_at)
VALUES 
('landing_page.show_dish_image', 'Hiển thị ảnh món ăn', 'BOOL', 1, 'Bật/Tắt hiển thị hình ảnh trong chi tiết món ăn trên landing page', 0, NOW()),
('landing_page.show_dish_image360', 'Hiển thị ảnh 360 độ', 'BOOL', 1, 'Bật/Tắt hiển thị ảnh 360 độ trong chi tiết món ăn trên landing page', 0, NOW()),
('landing_page.show_dish_video', 'Hiển thị video món ăn', 'BOOL', 1, 'Bật/Tắt hiển thị video trong chi tiết món ăn trên landing page', 0, NOW());
