-- 1. Create tax table
CREATE TABLE tax (
  tax_id bigint NOT NULL AUTO_INCREMENT,
  tax_name varchar(100) NOT NULL,
  tax_rate decimal(5, 2) NOT NULL COMMENT 'Percentage value, e.g., 8.00 for 8%',
  tax_type enum('INCLUSIVE', 'EXCLUSIVE') NOT NULL DEFAULT 'EXCLUSIVE',
  is_active tinyint(1) NOT NULL DEFAULT 1,
  is_default tinyint(1) NOT NULL DEFAULT 0,
  created_at datetime DEFAULT CURRENT_TIMESTAMP,
  updated_at datetime DEFAULT CURRENT_TIMESTAMP ON UPDATE CURRENT_TIMESTAMP,
  PRIMARY KEY (tax_id)
) ENGINE=InnoDB CHARACTER SET utf8mb4 COLLATE utf8mb4_unicode_ci;

-- 2. Add tax_id and tax_amount to orders table
ALTER TABLE orders 
ADD COLUMN tax_id bigint NULL,
ADD COLUMN tax_amount decimal(14, 2) NOT NULL DEFAULT 0;

-- 3. Add foreign key constraint
ALTER TABLE orders
ADD CONSTRAINT fk_orders_tax FOREIGN KEY (tax_id) REFERENCES tax(tax_id);

-- 4. Initial Seed (Optional but helpful)
INSERT INTO tax (tax_name, tax_rate, tax_type, is_active, is_default) 
VALUES ('VAT', 10.00, 'EXCLUSIVE', 1, 1);
