-- ============================================
-- PC BUILDER - ПОЛНЫЙ СКРИПТ БАЗЫ ДАННЫХ
-- PostgreSQL
-- Запуск: psql -U builder -d pc_builder -f schema.sql
-- ============================================

BEGIN;

-- ============================================
-- ОЧИСТКА (если перезапускаем)
-- ============================================
DROP TABLE IF EXISTS build_items CASCADE;
DROP TABLE IF EXISTS builds CASCADE;
DROP TABLE IF EXISTS compatibility_rules CASCADE;
DROP TABLE IF EXISTS cooler_socket_support CASCADE;
DROP TABLE IF EXISTS cooler_specs CASCADE;
DROP TABLE IF EXISTS case_specs CASCADE;
DROP TABLE IF EXISTS storage_specs CASCADE;
DROP TABLE IF EXISTS psu_specs CASCADE;
DROP TABLE IF EXISTS gpu_specs CASCADE;
DROP TABLE IF EXISTS ram_specs CASCADE;
DROP TABLE IF EXISTS motherboard_specs CASCADE;
DROP TABLE IF EXISTS cpu_specs CASCADE;
DROP TABLE IF EXISTS components CASCADE;
DROP TABLE IF EXISTS component_categories CASCADE;
DROP TABLE IF EXISTS users CASCADE;

-- ============================================
-- ТАБЛИЦЫ
-- ============================================

CREATE TABLE users (
    id SERIAL PRIMARY KEY,
    username VARCHAR(50) UNIQUE NOT NULL,
    password_hash VARCHAR(255) NOT NULL,
    created_at TIMESTAMP DEFAULT NOW(),
    last_login_at TIMESTAMP
);

CREATE TABLE component_categories (
    id SERIAL PRIMARY KEY,
    code VARCHAR(20) UNIQUE NOT NULL,
    display_name VARCHAR(100) NOT NULL
);

CREATE TABLE components (
    id SERIAL PRIMARY KEY,
    category_id INTEGER NOT NULL REFERENCES component_categories(id),
    name VARCHAR(100) NOT NULL,
    vendor VARCHAR(50) NOT NULL,
    model VARCHAR(100) NOT NULL,
    description TEXT,
    price DECIMAL(10,2) NOT NULL,
    power_watts INTEGER DEFAULT 0,
    is_active BOOLEAN DEFAULT TRUE,
    model_tier INTEGER NOT NULL CHECK (model_tier IN (1, 2, 3)),
    icon_path VARCHAR(255),
    created_at TIMESTAMP DEFAULT NOW()
);

CREATE TABLE cpu_specs (
    component_id INTEGER PRIMARY KEY REFERENCES components(id) ON DELETE CASCADE,
    socket VARCHAR(20) NOT NULL,
    cores INTEGER NOT NULL,
    threads INTEGER NOT NULL,
    base_ghz DECIMAL(3,1) NOT NULL,
    boost_ghz DECIMAL(3,1),
    tdp_w INTEGER NOT NULL,
    has_integrated_gpu BOOLEAN DEFAULT FALSE,
    ram_type VARCHAR(10) NOT NULL
);

CREATE TABLE motherboard_specs (
    component_id INTEGER PRIMARY KEY REFERENCES components(id) ON DELETE CASCADE,
    socket VARCHAR(20) NOT NULL,
    chipset VARCHAR(20) NOT NULL,
    ram_type VARCHAR(10) NOT NULL,
    ram_slots INTEGER NOT NULL,
    max_ram_gb INTEGER NOT NULL,
    form_factor VARCHAR(10) NOT NULL,
    mb_power_w INTEGER NOT NULL DEFAULT 50,
    pcie_version VARCHAR(5) DEFAULT '4.0'
);

CREATE TABLE ram_specs (
    component_id INTEGER PRIMARY KEY REFERENCES components(id) ON DELETE CASCADE,
    ram_type VARCHAR(10) NOT NULL,
    size_gb INTEGER NOT NULL,
    speed_mhz INTEGER NOT NULL,
    modules_count INTEGER NOT NULL DEFAULT 2,
    cas_latency INTEGER,
    rgb BOOLEAN DEFAULT FALSE
);

CREATE TABLE gpu_specs (
    component_id INTEGER PRIMARY KEY REFERENCES components(id) ON DELETE CASCADE,
    vram_gb INTEGER NOT NULL,
    tdp_w INTEGER NOT NULL,
    required_psu_w INTEGER NOT NULL,
    pcie_version VARCHAR(5) DEFAULT '4.0',
    length_mm INTEGER,
    rgb BOOLEAN DEFAULT FALSE
);

CREATE TABLE psu_specs (
    component_id INTEGER PRIMARY KEY REFERENCES components(id) ON DELETE CASCADE,
    wattage INTEGER NOT NULL,
    efficiency_rating VARCHAR(20) NOT NULL,
    modular_type VARCHAR(20) DEFAULT 'non-modular',
    length_mm INTEGER DEFAULT 140
);

CREATE TABLE storage_specs (
    component_id INTEGER PRIMARY KEY REFERENCES components(id) ON DELETE CASCADE,
    storage_type VARCHAR(10) NOT NULL,
    capacity_gb INTEGER NOT NULL,
    interface_type VARCHAR(20) NOT NULL,
    read_speed_mbs INTEGER,
    write_speed_mbs INTEGER
);

CREATE TABLE cooler_specs (
    component_id INTEGER PRIMARY KEY REFERENCES components(id) ON DELETE CASCADE,
    tdp_support_w INTEGER NOT NULL,
    cooler_type VARCHAR(20) DEFAULT 'air',
    height_mm INTEGER,
    rgb BOOLEAN DEFAULT FALSE
);

-- Нормализованная таблица поддержки сокетов кулерами
CREATE TABLE cooler_socket_support (
    id SERIAL PRIMARY KEY,
    cooler_id INTEGER NOT NULL REFERENCES components(id) ON DELETE CASCADE,
    socket VARCHAR(20) NOT NULL,
    UNIQUE(cooler_id, socket)
);

CREATE TABLE case_specs (
    component_id INTEGER PRIMARY KEY REFERENCES components(id) ON DELETE CASCADE,
    form_factor_support TEXT NOT NULL,
    max_gpu_mm INTEGER NOT NULL,
    max_cooler_height_mm INTEGER,
    fan_slots_count INTEGER DEFAULT 2,
    psu_max_length_mm INTEGER DEFAULT 200,
    has_glass_side BOOLEAN DEFAULT FALSE
);

CREATE TABLE compatibility_rules (
    id SERIAL PRIMARY KEY,
    rule_type VARCHAR(30) NOT NULL,
    component_a_id INTEGER NOT NULL REFERENCES components(id) ON DELETE CASCADE,
    component_b_id INTEGER NOT NULL REFERENCES components(id) ON DELETE CASCADE,
    is_compatible BOOLEAN NOT NULL,
    reason VARCHAR(255)
);

CREATE TABLE builds (
    id SERIAL PRIMARY KEY,
    user_id INTEGER NOT NULL REFERENCES users(id),
    name VARCHAR(100) NOT NULL,
    created_at TIMESTAMP DEFAULT NOW(),
    updated_at TIMESTAMP DEFAULT NOW(),
    is_validated BOOLEAN DEFAULT FALSE,
    score INTEGER DEFAULT 0
);

CREATE TABLE build_items (
    id SERIAL PRIMARY KEY,
    build_id INTEGER NOT NULL REFERENCES builds(id) ON DELETE CASCADE,
    slot_code VARCHAR(20) NOT NULL,
    component_id INTEGER NOT NULL REFERENCES components(id),
    position_x REAL DEFAULT 0,
    position_y REAL DEFAULT 0,
    position_z REAL DEFAULT 0,
    rotation_x REAL DEFAULT 0,
    rotation_y REAL DEFAULT 0,
    rotation_z REAL DEFAULT 0
);

-- ============================================
-- ИНДЕКСЫ ДЛЯ ПРОИЗВОДИТЕЛЬНОСТИ
-- ============================================
CREATE INDEX idx_components_category ON components(category_id);
CREATE INDEX idx_components_vendor ON components(vendor);
CREATE INDEX idx_components_tier ON components(model_tier);
CREATE INDEX idx_build_items_build ON build_items(build_id);
CREATE INDEX idx_build_items_component ON build_items(component_id);
CREATE INDEX idx_comp_rules_a ON compatibility_rules(component_a_id);
CREATE INDEX idx_comp_rules_b ON compatibility_rules(component_b_id);
CREATE INDEX idx_comp_rules_type ON compatibility_rules(rule_type);
CREATE INDEX idx_cooler_socket ON cooler_socket_support(socket);
CREATE INDEX idx_builds_user ON builds(user_id);

-- ============================================
-- КАТЕГОРИИ
-- ============================================
INSERT INTO component_categories (id, code, display_name) VALUES
(1, 'CASE', 'Корпус'),
(2, 'CPU', 'Процессор'),
(3, 'GPU', 'Видеокарта'),
(4, 'RAM', 'Оперативная память'),
(5, 'PSU', 'Блок питания'),
(6, 'MOTHERBOARD', 'Материнская плата'),
(7, 'STORAGE', 'Накопитель'),
(8, 'CPU_COOLER', 'Кулер процессора');

-- Сброс последовательности после вставки категорий
SELECT setval('component_categories_id_seq', 8);

-- ============================================
-- КОМПОНЕНТЫ + СПЕЦИФИКАЦИИ
-- ============================================

-- ==================== CPU (16) ====================
-- ID 1-16
INSERT INTO components (id, category_id, name, vendor, model, description, price, power_watts, model_tier, icon_path) VALUES
(1, 2, 'Intel Core i3-10100F', 'Intel', 'Core i3-10100F', 'Бюджетный 4-ядерный процессор без встроенной графики', 8500, 65, 1, 'icons/cpu_intel_i3_10100f'),
(2, 2, 'Intel Core i5-10400F', 'Intel', 'Core i5-10400F', 'Сбалансированный 6-ядерный процессор для игр', 13000, 65, 2, 'icons/cpu_intel_i5_10400f'),
(3, 2, 'Intel Core i7-10700K', 'Intel', 'Core i7-10700K', 'Мощный 8-ядерный процессор с разблокированным множителем', 28000, 125, 3, 'icons/cpu_intel_i7_10700k'),
(4, 2, 'Intel Core i9-11900K', 'Intel', 'Core i9-11900K', 'Флагманский процессор 11-го поколения', 42000, 125, 3, 'icons/cpu_intel_i9_11900k'),
(5, 2, 'Intel Core i3-12100F', 'Intel', 'Core i3-12100F', 'Бюджетный процессор 12-го поколения с высокой производительностью на ядро', 9500, 60, 1, 'icons/cpu_intel_i3_12100f'),
(6, 2, 'Intel Core i5-13400F', 'Intel', 'Core i5-13400F', '10-ядерный процессор с гибридной архитектурой', 20000, 65, 2, 'icons/cpu_intel_i5_13400f'),
(7, 2, 'Intel Core i7-13700K', 'Intel', 'Core i7-13700K', '16-ядерный процессор для энтузиастов', 39000, 125, 3, 'icons/cpu_intel_i7_13700k'),
(8, 2, 'Intel Core i9-13900K', 'Intel', 'Core i9-13900K', 'Максимальная производительность для любых задач', 55000, 125, 3, 'icons/cpu_intel_i9_13900k'),
(9, 2, 'AMD Ryzen 3 4100', 'AMD', 'Ryzen 3 4100', 'Бюджетный процессор AMD на архитектуре Zen 2', 7000, 65, 1, 'icons/cpu_amd_r3_4100'),
(10, 2, 'AMD Ryzen 5 5600X', 'AMD', 'Ryzen 5 5600X', '6-ядерный процессор с отличным соотношением цена/производительность', 16000, 65, 2, 'icons/cpu_amd_r5_5600x'),
(11, 2, 'AMD Ryzen 7 5800X3D', 'AMD', 'Ryzen 7 5800X3D', 'Игровой процессор с технологией 3D V-Cache', 32000, 105, 3, 'icons/cpu_amd_r7_5800x3d'),
(12, 2, 'AMD Ryzen 9 5950X', 'AMD', 'Ryzen 9 5950X', '16-ядерный процессор для рабочих станций', 48000, 105, 3, 'icons/cpu_amd_r9_5950x'),
(13, 2, 'AMD Ryzen 5 7600', 'AMD', 'Ryzen 5 7600', 'Процессор нового поколения AM5 с DDR5', 21000, 65, 2, 'icons/cpu_amd_r5_7600'),
(14, 2, 'AMD Ryzen 7 7800X3D', 'AMD', 'Ryzen 7 7800X3D', 'Лучший игровой процессор на AM5', 38000, 120, 3, 'icons/cpu_amd_r7_7800x3d'),
(15, 2, 'AMD Ryzen 9 7900X', 'AMD', 'Ryzen 9 7900X', '12-ядерный процессор для профессионалов', 52000, 170, 3, 'icons/cpu_amd_r9_7900x'),
(16, 2, 'AMD Ryzen 5 7500F', 'AMD', 'Ryzen 5 7500F', 'Бюджетный процессор AM5 без встроенной графики', 14000, 65, 1, 'icons/cpu_amd_r5_7500f');

INSERT INTO cpu_specs (component_id, socket, cores, threads, base_ghz, boost_ghz, tdp_w, has_integrated_gpu, ram_type) VALUES
(1, 'LGA1200', 4, 8, 3.6, 4.3, 65, FALSE, 'DDR4'),
(2, 'LGA1200', 6, 12, 2.9, 4.3, 65, FALSE, 'DDR4'),
(3, 'LGA1200', 8, 16, 3.8, 5.1, 125, TRUE, 'DDR4'),
(4, 'LGA1200', 8, 16, 3.5, 5.3, 125, TRUE, 'DDR4'),
(5, 'LGA1700', 4, 8, 3.3, 4.3, 60, FALSE, 'DDR5'),
(6, 'LGA1700', 10, 16, 2.5, 4.6, 65, FALSE, 'DDR5'),
(7, 'LGA1700', 16, 24, 3.4, 5.4, 125, TRUE, 'DDR5'),
(8, 'LGA1700', 24, 32, 3.0, 5.8, 125, TRUE, 'DDR5'),
(9, 'AM4', 4, 8, 3.8, 4.0, 65, FALSE, 'DDR4'),
(10, 'AM4', 6, 12, 3.7, 4.6, 65, FALSE, 'DDR4'),
(11, 'AM4', 8, 16, 3.4, 4.5, 105, FALSE, 'DDR4'),
(12, 'AM4', 16, 32, 3.4, 4.9, 105, FALSE, 'DDR4'),
(13, 'AM5', 6, 12, 3.8, 5.1, 65, TRUE, 'DDR5'),
(14, 'AM5', 8, 16, 4.2, 5.0, 120, TRUE, 'DDR5'),
(15, 'AM5', 12, 24, 4.7, 5.6, 170, TRUE, 'DDR5'),
(16, 'AM5', 6, 12, 3.7, 5.0, 65, FALSE, 'DDR5');

-- Сброс последовательности
SELECT setval('components_id_seq', 16);

-- ==================== МАТЕРИНСКИЕ ПЛАТЫ (12) ====================
-- ID 17-28
INSERT INTO components (id, category_id, name, vendor, model, description, price, power_watts, model_tier, icon_path) VALUES
(17, 6, 'ASUS PRIME H410M-K', 'ASUS', 'PRIME H410M-K', 'Базовая плата для офисных ПК', 6500, 40, 1, 'icons/mb_asus_h410mk'),
(18, 6, 'MSI MAG B560M BAZOOKA', 'MSI', 'MAG B560M BAZOOKA', 'Средняя плата с поддержкой разгона памяти', 9500, 50, 2, 'icons/mb_msi_b560m'),
(19, 6, 'ASUS ROG STRIX Z590-A', 'ASUS', 'ROG STRIX Z590-A', 'Премиальная плата для разгона', 22000, 65, 3, 'icons/mb_asus_z590a'),
(20, 6, 'Gigabyte B760M DS3H', 'Gigabyte', 'B760M DS3H', 'Современная плата с DDR5', 11000, 50, 2, 'icons/mb_gigabyte_b760m'),
(21, 6, 'MSI PRO Z790-P WIFI', 'MSI', 'PRO Z790-P WIFI', 'Флагманская плата с Wi-Fi', 24000, 65, 3, 'icons/mb_msi_z790p'),
(22, 6, 'ASRock H610M-HDV', 'ASRock', 'H610M-HDV', 'Бюджетная плата для LGA1700', 7000, 40, 1, 'icons/mb_asrock_h610m'),
(23, 6, 'Gigabyte A520M S2H', 'Gigabyte', 'A520M S2H', 'Базовая плата AM4 для Ryzen', 5500, 40, 1, 'icons/mb_gigabyte_a520m'),
(24, 6, 'MSI B550-A PRO', 'MSI', 'B550-A PRO', 'Надёжная ATX плата для игр', 12000, 55, 2, 'icons/mb_msi_b550a'),
(25, 6, 'ASUS ROG STRIX X570-E', 'ASUS', 'ROG STRIX X570-E', 'Топовая плата AM4 с PCIe 4.0', 28000, 70, 3, 'icons/mb_asus_x570e'),
(26, 6, 'ASRock B650M Pro RS', 'ASRock', 'B650M Pro RS', 'Средняя плата AM5 с DDR5', 14000, 55, 2, 'icons/mb_asrock_b650m'),
(27, 6, 'MSI MAG X670E TOMAHAWK', 'MSI', 'MAG X670E TOMAHAWK', 'Мощная плата для Ryzen 7000', 32000, 70, 3, 'icons/mb_msi_x670e'),
(28, 6, 'Gigabyte A620M H', 'Gigabyte', 'A620M H', 'Бюджетная плата AM5', 8000, 40, 1, 'icons/mb_gigabyte_a620m');

INSERT INTO motherboard_specs (component_id, socket, chipset, ram_type, ram_slots, max_ram_gb, form_factor, mb_power_w, pcie_version) VALUES
(17, 'LGA1200', 'H410', 'DDR4', 2, 64, 'mATX', 40, '3.0'),
(18, 'LGA1200', 'B560', 'DDR4', 4, 128, 'mATX', 50, '4.0'),
(19, 'LGA1200', 'Z590', 'DDR4', 4, 128, 'ATX', 65, '4.0'),
(20, 'LGA1700', 'B760', 'DDR5', 4, 192, 'mATX', 50, '5.0'),
(21, 'LGA1700', 'Z790', 'DDR5', 4, 192, 'ATX', 65, '5.0'),
(22, 'LGA1700', 'H610', 'DDR5', 2, 96, 'mATX', 40, '4.0'),
(23, 'AM4', 'A520', 'DDR4', 2, 64, 'mATX', 40, '3.0'),
(24, 'AM4', 'B550', 'DDR4', 4, 128, 'ATX', 55, '4.0'),
(25, 'AM4', 'X570', 'DDR4', 4, 128, 'ATX', 70, '4.0'),
(26, 'AM5', 'B650', 'DDR5', 4, 192, 'mATX', 55, '5.0'),
(27, 'AM5', 'X670E', 'DDR5', 4, 192, 'ATX', 70, '5.0'),
(28, 'AM5', 'A620', 'DDR5', 2, 96, 'mATX', 40, '4.0');

-- Сброс последовательности
SELECT setval('components_id_seq', 28);

-- ==================== RAM (12) ====================
-- ID 29-40
INSERT INTO components (id, category_id, name, vendor, model, description, price, power_watts, model_tier, icon_path) VALUES
(29, 4, 'Kingston FURY Beast 16GB DDR4', 'Kingston', 'FURY Beast 16GB DDR4', 'Бюджетная DDR4 память', 4500, 0, 1, 'icons/ram_kingston_fury_ddr4'),
(30, 4, 'Patriot Viper Steel 16GB DDR4', 'Patriot', 'Viper Steel 16GB DDR4', 'Надёжная память с радиаторами', 5500, 0, 2, 'icons/ram_patriot_viper_ddr4'),
(31, 4, 'G.Skill Trident Z RGB 16GB DDR4', 'G.Skill', 'Trident Z RGB 16GB DDR4', 'Яркая RGB память для геймеров', 7000, 0, 2, 'icons/ram_gskill_trident_ddr4'),
(32, 4, 'Corsair Vengeance LPX 32GB DDR4', 'Corsair', 'Vengeance LPX 32GB DDR4', '32 ГБ для тяжёлых задач', 9500, 0, 2, 'icons/ram_corsair_vengeance_ddr4'),
(33, 4, 'G.Skill Trident Z Neo 32GB DDR4', 'G.Skill', 'Trident Z Neo 32GB DDR4', 'Высокоскоростная память для AMD', 13000, 0, 3, 'icons/ram_gskill_neo_ddr4'),
(34, 4, 'Corsair Dominator Platinum 64GB DDR4', 'Corsair', 'Dominator Platinum 64GB DDR4', 'Премиальная память большого объёма', 22000, 0, 3, 'icons/ram_corsair_dominator_ddr4'),
(35, 4, 'Crucial Basics 16GB DDR5', 'Crucial', 'Basics 16GB DDR5', 'Базовая DDR5 память', 5500, 0, 1, 'icons/ram_crucial_ddr5'),
(36, 4, 'Kingston FURY Beast 32GB DDR5', 'Kingston', 'FURY Beast 32GB DDR5', 'Оптимальный набор DDR5', 10500, 0, 2, 'icons/ram_kingston_fury_ddr5'),
(37, 4, 'G.Skill Trident Z5 RGB 32GB DDR5', 'G.Skill', 'Trident Z5 RGB 32GB DDR5', 'Высокоскоростная DDR5 с RGB', 14000, 0, 3, 'icons/ram_gskill_z5_ddr5'),
(38, 4, 'Corsair Vengeance 64GB DDR5', 'Corsair', 'Vengeance 64GB DDR5', '64 ГБ для профессионалов', 21000, 0, 3, 'icons/ram_corsair_vengeance_ddr5'),
(39, 4, 'ADATA XPG Lancer 16GB DDR5', 'ADATA', 'XPG Lancer 16GB DDR5', 'Бюджетный набор DDR5', 5000, 0, 1, 'icons/ram_adata_lancer_ddr5'),
(40, 4, 'TeamGroup T-Force Delta 32GB DDR5', 'TeamGroup', 'T-Force Delta 32GB DDR5', 'Быстрая DDR5 с агрессивным дизайном', 15500, 0, 3, 'icons/ram_teamgroup_delta_ddr5');

INSERT INTO ram_specs (component_id, ram_type, size_gb, speed_mhz, modules_count, cas_latency, rgb) VALUES
(29, 'DDR4', 16, 3200, 2, 16, FALSE),
(30, 'DDR4', 16, 3600, 2, 17, FALSE),
(31, 'DDR4', 16, 3600, 2, 16, TRUE),
(32, 'DDR4', 32, 3600, 2, 18, FALSE),
(33, 'DDR4', 32, 4000, 2, 18, TRUE),
(34, 'DDR4', 64, 3600, 2, 18, TRUE),
(35, 'DDR5', 16, 4800, 2, 40, FALSE),
(36, 'DDR5', 32, 5600, 2, 36, FALSE),
(37, 'DDR5', 32, 6000, 2, 30, TRUE),
(38, 'DDR5', 64, 5600, 2, 40, FALSE),
(39, 'DDR5', 16, 5200, 2, 38, FALSE),
(40, 'DDR5', 32, 6400, 2, 32, TRUE);

-- Сброс последовательности
SELECT setval('components_id_seq', 40);

-- ==================== GPU (12) ====================
-- ID 41-52
INSERT INTO components (id, category_id, name, vendor, model, description, price, power_watts, model_tier, icon_path) VALUES
(41, 3, 'NVIDIA GeForce GTX 1650', 'NVIDIA', 'GeForce GTX 1650', 'Базовая видеокарта для нетребовательных игр', 14000, 75, 1, 'icons/gpu_gtx1650'),
(42, 3, 'AMD Radeon RX 6500 XT', 'AMD', 'Radeon RX 6500 XT', 'Бюджетная видеокарта AMD', 13500, 107, 1, 'icons/gpu_rx6500xt'),
(43, 3, 'NVIDIA GeForce RTX 3050', 'NVIDIA', 'GeForce RTX 3050', 'Видеокарта с трассировкой лучей начального уровня', 23000, 130, 2, 'icons/gpu_rtx3050'),
(44, 3, 'AMD Radeon RX 6600', 'AMD', 'Radeon RX 6600', 'Отличная карта для Full HD гейминга', 21000, 132, 2, 'icons/gpu_rx6600'),
(45, 3, 'NVIDIA GeForce RTX 3060 Ti', 'NVIDIA', 'GeForce RTX 3060 Ti', 'Золотая середина для 1440p', 32000, 200, 2, 'icons/gpu_rtx3060ti'),
(46, 3, 'AMD Radeon RX 6700 XT', 'AMD', 'Radeon RX 6700 XT', '12 ГБ видеопамяти для высоких настроек', 38000, 230, 3, 'icons/gpu_rx6700xt'),
(47, 3, 'NVIDIA GeForce RTX 4070', 'NVIDIA', 'GeForce RTX 4070', 'Эффективная карта с DLSS 3', 55000, 200, 3, 'icons/gpu_rtx4070'),
(48, 3, 'AMD Radeon RX 7800 XT', 'AMD', 'Radeon RX 7800 XT', '16 ГБ для 4K гейминга', 58000, 263, 3, 'icons/gpu_rx7800xt'),
(49, 3, 'NVIDIA GeForce RTX 4080', 'NVIDIA', 'GeForce RTX 4080', 'Мощная карта для 4K с запасом', 95000, 320, 3, 'icons/gpu_rtx4080'),
(50, 3, 'AMD Radeon RX 7900 XTX', 'AMD', 'Radeon RX 7900 XTX', 'Флагман AMD с 24 ГБ видеопамяти', 105000, 355, 3, 'icons/gpu_rx7900xtx'),
(51, 3, 'NVIDIA GeForce RTX 4060', 'NVIDIA', 'GeForce RTX 4060', 'Энергоэффективная карта с DLSS 3', 28000, 115, 2, 'icons/gpu_rtx4060'),
(52, 3, 'Intel Arc A750', 'Intel', 'Arc A750', 'Видеокарта Intel с аппаратным RT', 25000, 225, 2, 'icons/gpu_arca750');

INSERT INTO gpu_specs (component_id, vram_gb, tdp_w, required_psu_w, pcie_version, length_mm, rgb) VALUES
(41, 4, 75, 300, '3.0', 190, FALSE),
(42, 4, 107, 400, '4.0', 194, FALSE),
(43, 8, 130, 550, '4.0', 242, FALSE),
(44, 8, 132, 450, '4.0', 225, FALSE),
(45, 8, 200, 600, '4.0', 242, FALSE),
(46, 12, 230, 650, '4.0', 267, FALSE),
(47, 12, 200, 650, '4.0', 244, FALSE),
(48, 16, 263, 700, '4.0', 267, FALSE),
(49, 16, 320, 750, '4.0', 304, FALSE),
(50, 24, 355, 800, '4.0', 287, FALSE),
(51, 8, 115, 550, '4.0', 244, FALSE),
(52, 8, 225, 600, '4.0', 267, FALSE);

-- Сброс последовательности
SELECT setval('components_id_seq', 52);

-- ==================== PSU (10) ====================
-- ID 53-62
INSERT INTO components (id, category_id, name, vendor, model, description, price, power_watts, model_tier, icon_path) VALUES
(53, 5, 'Deepcool PF500', 'Deepcool', 'PF500', 'Бюджетный блок питания 500 Вт', 4000, 0, 1, 'icons/psu_deepcool_pf500'),
(54, 5, 'Cougar VTE500', 'Cougar', 'VTE500', 'Начальный БП с бронзовым сертификатом', 4500, 0, 1, 'icons/psu_cougar_vte500'),
(55, 5, 'be quiet! System Power 10 650W', 'be quiet!', 'System Power 10 650W', 'Тихий и надёжный БП', 6500, 0, 2, 'icons/psu_bequiet_sp10'),
(56, 5, 'Cooler Master MWE 650 Bronze', 'Cooler Master', 'MWE 650 Bronze', 'Полумодульный БП для игровых сборок', 7000, 0, 2, 'icons/psu_coolermaster_mwe650'),
(57, 5, 'Corsair RM750e', 'Corsair', 'RM750e', 'Полностью модульный золотой БП', 10500, 0, 3, 'icons/psu_corsair_rm750e'),
(58, 5, 'Deepcool PQ850M', 'Deepcool', 'PQ850M', 'Мощный БП с золотым сертификатом', 12000, 0, 3, 'icons/psu_deepcool_pq850m'),
(59, 5, 'Thermaltake Toughpower GF3 1000W', 'Thermaltake', 'Toughpower GF3 1000W', 'Килловатник с поддержкой ATX 3.0', 17000, 0, 3, 'icons/psu_thermaltake_gf3'),
(60, 5, 'Seasonic FOCUS GX-850', 'Seasonic', 'FOCUS GX-850', 'Эталонный БП от Seasonic', 13500, 0, 3, 'icons/psu_seasonic_gx850'),
(61, 5, 'Gigabyte P550B', 'Gigabyte', 'P550B', 'Бюджетный БП 550 Вт', 4800, 0, 1, 'icons/psu_gigabyte_p550b'),
(62, 5, 'ASUS ROG Thor 1200P', 'ASUS', 'ROG Thor 1200P', 'Премиальный платиновый БП с OLED-дисплеем', 32000, 0, 3, 'icons/psu_asus_thor1200p');

INSERT INTO psu_specs (component_id, wattage, efficiency_rating, modular_type, length_mm) VALUES
(53, 500, '80+', 'non-modular', 140),
(54, 500, '80+ Bronze', 'non-modular', 140),
(55, 650, '80+ Bronze', 'non-modular', 140),
(56, 650, '80+ Bronze', 'semi-modular', 140),
(57, 750, '80+ Gold', 'full-modular', 140),
(58, 850, '80+ Gold', 'full-modular', 160),
(59, 1000, '80+ Gold', 'full-modular', 160),
(60, 850, '80+ Gold', 'full-modular', 140),
(61, 550, '80+ Bronze', 'non-modular', 140),
(62, 1200, '80+ Platinum', 'full-modular', 190);

-- Сброс последовательности
SELECT setval('components_id_seq', 62);

-- ==================== STORAGE (10) ====================
-- ID 63-72
INSERT INTO components (id, category_id, name, vendor, model, description, price, power_watts, model_tier, icon_path) VALUES
(63, 7, 'WD Blue 1TB HDD', 'Western Digital', 'Blue 1TB', 'Классический жёсткий диск на 1 ТБ', 4500, 0, 1, 'icons/storage_wd_blue_1tb'),
(64, 7, 'Seagate Barracuda 2TB HDD', 'Seagate', 'Barracuda 2TB', '2 ТБ для хранения данных', 6000, 0, 1, 'icons/storage_seagate_barracuda'),
(65, 7, 'Kingston A400 480GB SSD', 'Kingston', 'A400 480GB', 'Бюджетный SATA SSD', 3500, 0, 1, 'icons/storage_kingston_a400'),
(66, 7, 'Samsung 870 EVO 1TB SSD', 'Samsung', '870 EVO 1TB', 'Надёжный SATA SSD на 1 ТБ', 8000, 0, 2, 'icons/storage_samsung_870evo'),
(67, 7, 'WD Blue SN570 1TB NVMe', 'Western Digital', 'Blue SN570 1TB', 'Быстрый NVMe начального уровня', 7000, 0, 2, 'icons/storage_wd_sn570'),
(68, 7, 'Samsung 980 1TB NVMe', 'Samsung', '980 1TB', 'Популярный NVMe без DRAM', 8000, 0, 2, 'icons/storage_samsung_980'),
(69, 7, 'Samsung 990 Pro 1TB NVMe', 'Samsung', '990 Pro 1TB', 'Самый быстрый PCIe 4.0 накопитель', 12000, 0, 3, 'icons/storage_samsung_990pro'),
(70, 7, 'WD Black SN850X 2TB NVMe', 'Western Digital', 'Black SN850X 2TB', 'Топовый игровой NVMe 2 ТБ', 18500, 0, 3, 'icons/storage_wd_sn850x'),
(71, 7, 'Kingston NV2 1TB NVMe', 'Kingston', 'NV2 1TB', 'Доступный NVMe PCIe 4.0', 6000, 0, 2, 'icons/storage_kingston_nv2'),
(72, 7, 'Seagate FireCuda 530 2TB NVMe', 'Seagate', 'FireCuda 530 2TB', 'Высокоскоростной NVMe с большим ресурсом', 21000, 0, 3, 'icons/storage_seagate_firecuda');

INSERT INTO storage_specs (component_id, storage_type, capacity_gb, interface_type, read_speed_mbs, write_speed_mbs) VALUES
(63, 'HDD', 1000, 'SATA 3', 150, 150),
(64, 'HDD', 2000, 'SATA 3', 180, 180),
(65, 'SSD', 480, 'SATA 3', 500, 450),
(66, 'SSD', 1000, 'SATA 3', 560, 530),
(67, 'NVMe', 1000, 'PCIe 3.0', 3500, 3000),
(68, 'NVMe', 1000, 'PCIe 3.0', 3500, 3000),
(69, 'NVMe', 1000, 'PCIe 4.0', 7450, 6900),
(70, 'NVMe', 2000, 'PCIe 4.0', 7300, 6600),
(71, 'NVMe', 1000, 'PCIe 4.0', 3500, 2100),
(72, 'NVMe', 2000, 'PCIe 4.0', 7300, 6900);

-- Сброс последовательности
SELECT setval('components_id_seq', 72);

-- ==================== CPU_COOLER (10) ====================
-- ID 73-82
INSERT INTO components (id, category_id, name, vendor, model, description, price, power_watts, model_tier, icon_path) VALUES
(73, 8, 'Deepcool ICE BLADE 100', 'Deepcool', 'ICE BLADE 100', 'Простой бюджетный кулер', 1200, 0, 1, 'icons/cooler_deepcool_iceblade'),
(74, 8, 'ID-COOLING SE-214-XT', 'ID-COOLING', 'SE-214-XT', 'Бюджетный кулер с хорошей эффективностью', 1800, 0, 1, 'icons/cooler_idcooling_se214'),
(75, 8, 'Cooler Master Hyper 212', 'Cooler Master', 'Hyper 212', 'Народный кулер, проверенный временем', 3000, 0, 2, 'icons/cooler_cm_hyper212'),
(76, 8, 'be quiet! Pure Rock 2', 'be quiet!', 'Pure Rock 2', 'Тихий кулер для средних процессоров', 3500, 0, 2, 'icons/cooler_bequiet_purerock2'),
(77, 8, 'Deepcool AK400', 'Deepcool', 'AK400', 'Отличный кулер за свою цену', 2800, 0, 2, 'icons/cooler_deepcool_ak400'),
(78, 8, 'Noctua NH-D15', 'Noctua', 'NH-D15', 'Лучший воздушный кулер в мире', 9500, 0, 3, 'icons/cooler_noctua_nhd15'),
(79, 8, 'Arctic Liquid Freezer II 240', 'Arctic', 'Liquid Freezer II 240', 'СЖО с толстым радиатором', 8500, 0, 3, 'icons/cooler_arctic_lf2_240'),
(80, 8, 'Corsair H100i Elite Capellix', 'Corsair', 'H100i Elite Capellix', 'Стильная СЖО с RGB подсветкой', 12500, 0, 3, 'icons/cooler_corsair_h100i'),
(81, 8, 'Deepcool LS520', 'Deepcool', 'LS520', 'СЖО с яркой RGB помпой', 9500, 0, 3, 'icons/cooler_deepcool_ls520'),
(82, 8, 'NZXT Kraken X73', 'NZXT', 'Kraken X73', '360 мм СЖО с зеркальной помпой', 16000, 0, 3, 'icons/cooler_nzxt_kraken_x73');

INSERT INTO cooler_specs (component_id, tdp_support_w, cooler_type, height_mm, rgb) VALUES
(73, 100, 'air', 130, FALSE),
(74, 180, 'air', 150, FALSE),
(75, 150, 'air', 158, FALSE),
(76, 150, 'air', 155, FALSE),
(77, 220, 'air', 155, FALSE),
(78, 250, 'air', 165, FALSE),
(79, 250, 'aio', 53, FALSE),
(80, 250, 'aio', 52, TRUE),
(81, 280, 'aio', 55, TRUE),
(82, 300, 'aio', 52, TRUE);

-- Поддержка сокетов кулерами (нормализованные данные)
INSERT INTO cooler_socket_support (cooler_id, socket) VALUES
-- Deepcool ICE BLADE 100 (73) - все современные сокеты
(73, 'LGA1200'), (73, 'LGA1700'), (73, 'AM4'), (73, 'AM5'),
-- ID-COOLING SE-214-XT (74)
(74, 'LGA1200'), (74, 'LGA1700'), (74, 'AM4'), (74, 'AM5'),
-- Cooler Master Hyper 212 (75)
(75, 'LGA1200'), (75, 'LGA1700'), (75, 'AM4'), (75, 'AM5'),
-- be quiet! Pure Rock 2 (76)
(76, 'LGA1200'), (76, 'LGA1700'), (76, 'AM4'), (76, 'AM5'),
-- Deepcool AK400 (77)
(77, 'LGA1200'), (77, 'LGA1700'), (77, 'AM4'), (77, 'AM5'),
-- Noctua NH-D15 (78)
(78, 'LGA1200'), (78, 'LGA1700'), (78, 'AM4'), (78, 'AM5'),
-- Arctic Liquid Freezer II 240 (79)
(79, 'LGA1200'), (79, 'LGA1700'), (79, 'AM4'), (79, 'AM5'),
-- Corsair H100i Elite Capellix (80)
(80, 'LGA1200'), (80, 'LGA1700'), (80, 'AM4'), (80, 'AM5'),
-- Deepcool LS520 (81)
(81, 'LGA1200'), (81, 'LGA1700'), (81, 'AM4'), (81, 'AM5'),
-- NZXT Kraken X73 (82)
(82, 'LGA1200'), (82, 'LGA1700'), (82, 'AM4'), (82, 'AM5');

-- Сброс последовательности
SELECT setval('components_id_seq', 82);

-- ==================== CASE (12) ====================
-- ID 83-94
INSERT INTO components (id, category_id, name, vendor, model, description, price, power_watts, model_tier, icon_path) VALUES
(83, 1, 'Zalman ZM-T3', 'Zalman', 'ZM-T3', 'Компактный бюджетный корпус mATX', 2500, 0, 1, 'icons/case_zalman_zmt3'),
(84, 1, 'Aerocool Cylon Mini', 'Aerocool', 'Cylon Mini', 'Минималистичный корпус с RGB полосой', 2200, 0, 1, 'icons/case_aerocool_cylon'),
(85, 1, 'Deepcool MATREXX 30', 'Deepcool', 'MATREXX 30', 'Бюджетный mATX с хорошей вентиляцией', 2000, 0, 1, 'icons/case_deepcool_matrexx30'),
(86, 1, 'Deepcool MATREXX 55 MESH', 'Deepcool', 'MATREXX 55 MESH', 'Просторный корпус с сетчатой передней панелью', 5500, 0, 2, 'icons/case_deepcool_matrexx55'),
(87, 1, 'Zalman Z9 NEO', 'Zalman', 'Z9 NEO', 'Стильный корпус с закалённым стеклом', 6000, 0, 2, 'icons/case_zalman_z9neo'),
(88, 1, 'Corsair 4000D Airflow', 'Corsair', '4000D Airflow', 'Легендарный корпус с отличным продувом', 9500, 0, 3, 'icons/case_corsair_4000d'),
(89, 1, 'NZXT H510', 'NZXT', 'H510', 'Минималистичный корпус премиум-класса', 8500, 0, 3, 'icons/case_nzxt_h510'),
(90, 1, 'Lian Li LANCOOL III', 'Lian Li', 'LANCOOL III', 'Просторный корпус с откидными панелями', 12000, 0, 3, 'icons/case_lianli_lancool3'),
(91, 1, 'Fractal Design Torrent', 'Fractal Design', 'Torrent', 'Лучший корпус по продуваемости', 17000, 0, 3, 'icons/case_fractal_torrent'),
(92, 1, 'Phanteks Eclipse P300A', 'Phanteks', 'Eclipse P300A', 'Бюджетный корпус с mesh фасадом', 5200, 0, 2, 'icons/case_phanteks_p300a'),
(93, 1, 'Cooler Master MasterBox Q300L', 'Cooler Master', 'MasterBox Q300L', 'Универсальный mATX с магнитными фильтрами', 3800, 0, 1, 'icons/case_coolermaster_q300l'),
(94, 1, 'be quiet! Pure Base 500DX', 'be quiet!', 'Pure Base 500DX', 'Тихий корпус с RGB и mesh', 10000, 0, 3, 'icons/case_bequiet_pb500dx');

INSERT INTO case_specs (component_id, form_factor_support, max_gpu_mm, max_cooler_height_mm, fan_slots_count, psu_max_length_mm, has_glass_side) VALUES
(83, 'mATX', 300, 150, 2, 200, FALSE),
(84, 'mATX', 290, 147, 1, 180, FALSE),
(85, 'mATX', 250, 130, 1, 180, FALSE),
(86, 'ATX', 370, 165, 4, 200, TRUE),
(87, 'ATX', 380, 165, 5, 200, TRUE),
(88, 'ATX', 360, 170, 2, 220, FALSE),
(89, 'ATX', 381, 165, 2, 200, TRUE),
(90, 'ATX', 435, 185, 4, 220, TRUE),
(91, 'ATX', 461, 188, 5, 230, TRUE),
(92, 'ATX', 355, 160, 1, 200, TRUE),
(93, 'mATX', 360, 157, 1, 160, FALSE),
(94, 'ATX', 396, 190, 3, 225, TRUE);

-- Сброс последовательности
SELECT setval('components_id_seq', 94);

-- ============================================
-- СОВМЕСТИМОСТЬ CPU ↔ MB по сокету
-- ============================================

-- LGA1200 CPU (1-4) ↔ LGA1200 MB (17-19)
INSERT INTO compatibility_rules (rule_type, component_a_id, component_b_id, is_compatible, reason) VALUES
('CPU_MB_SOCKET', 1, 17, TRUE, 'Сокет LGA1200 совпадает'),
('CPU_MB_SOCKET', 1, 18, TRUE, 'Сокет LGA1200 совпадает'),
('CPU_MB_SOCKET', 1, 19, TRUE, 'Сокет LGA1200 совпадает'),
('CPU_MB_SOCKET', 2, 17, TRUE, 'Сокет LGA1200 совпадает'),
('CPU_MB_SOCKET', 2, 18, TRUE, 'Сокет LGA1200 совпадает'),
('CPU_MB_SOCKET', 2, 19, TRUE, 'Сокет LGA1200 совпадает'),
('CPU_MB_SOCKET', 3, 17, TRUE, 'Сокет LGA1200 совпадает'),
('CPU_MB_SOCKET', 3, 18, TRUE, 'Сокет LGA1200 совпадает'),
('CPU_MB_SOCKET', 3, 19, TRUE, 'Сокет LGA1200 совпадает'),
('CPU_MB_SOCKET', 4, 17, TRUE, 'Сокет LGA1200 совпадает'),
('CPU_MB_SOCKET', 4, 18, TRUE, 'Сокет LGA1200 совпадает'),
('CPU_MB_SOCKET', 4, 19, TRUE, 'Сокет LGA1200 совпадает');

-- LGA1700 CPU (5-8) ↔ LGA1700 MB (20-22)
INSERT INTO compatibility_rules (rule_type, component_a_id, component_b_id, is_compatible, reason) VALUES
('CPU_MB_SOCKET', 5, 20, TRUE, 'Сокет LGA1700 совпадает'),
('CPU_MB_SOCKET', 5, 21, TRUE, 'Сокет LGA1700 совпадает'),
('CPU_MB_SOCKET', 5, 22, TRUE, 'Сокет LGA1700 совпадает'),
('CPU_MB_SOCKET', 6, 20, TRUE, 'Сокет LGA1700 совпадает'),
('CPU_MB_SOCKET', 6, 21, TRUE, 'Сокет LGA1700 совпадает'),
('CPU_MB_SOCKET', 6, 22, TRUE, 'Сокет LGA1700 совпадает'),
('CPU_MB_SOCKET', 7, 20, TRUE, 'Сокет LGA1700 совпадает'),
('CPU_MB_SOCKET', 7, 21, TRUE, 'Сокет LGA1700 совпадает'),
('CPU_MB_SOCKET', 7, 22, TRUE, 'Сокет LGA1700 совпадает'),
('CPU_MB_SOCKET', 8, 20, TRUE, 'Сокет LGA1700 совпадает'),
('CPU_MB_SOCKET', 8, 21, TRUE, 'Сокет LGA1700 совпадает'),
('CPU_MB_SOCKET', 8, 22, TRUE, 'Сокет LGA1700 совпадает');

-- AM4 CPU (9-12) ↔ AM4 MB (23-25)
INSERT INTO compatibility_rules (rule_type, component_a_id, component_b_id, is_compatible, reason) VALUES
('CPU_MB_SOCKET', 9, 23, TRUE, 'Сокет AM4 совпадает'),
('CPU_MB_SOCKET', 9, 24, TRUE, 'Сокет AM4 совпадает'),
('CPU_MB_SOCKET', 9, 25, TRUE, 'Сокет AM4 совпадает'),
('CPU_MB_SOCKET', 10, 23, TRUE, 'Сокет AM4 совпадает'),
('CPU_MB_SOCKET', 10, 24, TRUE, 'Сокет AM4 совпадает'),
('CPU_MB_SOCKET', 10, 25, TRUE, 'Сокет AM4 совпадает'),
('CPU_MB_SOCKET', 11, 23, TRUE, 'Сокет AM4 совпадает'),
('CPU_MB_SOCKET', 11, 24, TRUE, 'Сокет AM4 совпадает'),
('CPU_MB_SOCKET', 11, 25, TRUE, 'Сокет AM4 совпадает'),
('CPU_MB_SOCKET', 12, 23, TRUE, 'Сокет AM4 совпадает'),
('CPU_MB_SOCKET', 12, 24, TRUE, 'Сокет AM4 совпадает'),
('CPU_MB_SOCKET', 12, 25, TRUE, 'Сокет AM4 совпадает');

-- AM5 CPU (13-16) ↔ AM5 MB (26-28)
INSERT INTO compatibility_rules (rule_type, component_a_id, component_b_id, is_compatible, reason) VALUES
('CPU_MB_SOCKET', 13, 26, TRUE, 'Сокет AM5 совпадает'),
('CPU_MB_SOCKET', 13, 27, TRUE, 'Сокет AM5 совпадает'),
('CPU_MB_SOCKET', 13, 28, TRUE, 'Сокет AM5 совпадает'),
('CPU_MB_SOCKET', 14, 26, TRUE, 'Сокет AM5 совпадает'),
('CPU_MB_SOCKET', 14, 27, TRUE, 'Сокет AM5 совпадает'),
('CPU_MB_SOCKET', 14, 28, TRUE, 'Сокет AM5 совпадает'),
('CPU_MB_SOCKET', 15, 26, TRUE, 'Сокет AM5 совпадает'),
('CPU_MB_SOCKET', 15, 27, TRUE, 'Сокет AM5 совпадает'),
('CPU_MB_SOCKET', 15, 28, TRUE, 'Сокет AM5 совпадает'),
('CPU_MB_SOCKET', 16, 26, TRUE, 'Сокет AM5 совпадает'),
('CPU_MB_SOCKET', 16, 27, TRUE, 'Сокет AM5 совпадает'),
('CPU_MB_SOCKET', 16, 28, TRUE, 'Сокет AM5 совпадает');

-- Примеры несовместимости разных сокетов
INSERT INTO compatibility_rules (rule_type, component_a_id, component_b_id, is_compatible, reason) VALUES
('CPU_MB_SOCKET', 1, 23, FALSE, 'Разные сокеты: LGA1200 vs AM4'),
('CPU_MB_SOCKET', 9, 17, FALSE, 'Разные сокеты: AM4 vs LGA1200'),
('CPU_MB_SOCKET', 5, 23, FALSE, 'Разные сокеты: LGA1700 vs AM4'),
('CPU_MB_SOCKET', 13, 20, FALSE, 'Разные сокеты: AM5 vs LGA1700');

-- ============================================
-- СОВМЕСТИМОСТЬ RAM ↔ MB по типу памяти
-- ============================================

-- DDR4 RAM совместима с DDR4 платами
INSERT INTO compatibility_rules (rule_type, component_a_id, component_b_id, is_compatible, reason) VALUES
('RAM_MB_TYPE', 29, 17, TRUE, 'DDR4 совпадает'),
('RAM_MB_TYPE', 29, 18, TRUE, 'DDR4 совпадает'),
('RAM_MB_TYPE', 29, 19, TRUE, 'DDR4 совпадает'),
('RAM_MB_TYPE', 30, 24, TRUE, 'DDR4 совпадает'),
('RAM_MB_TYPE', 31, 25, TRUE, 'DDR4 совпадает'),
('RAM_MB_TYPE', 32, 24, TRUE, 'DDR4 совпадает'),
('RAM_MB_TYPE', 33, 25, TRUE, 'DDR4 совпадает'),
('RAM_MB_TYPE', 34, 25, TRUE, 'DDR4 совпадает');

-- DDR5 RAM совместима с DDR5 платами
INSERT INTO compatibility_rules (rule_type, component_a_id, component_b_id, is_compatible, reason) VALUES
('RAM_MB_TYPE', 35, 20, TRUE, 'DDR5 совпадает'),
('RAM_MB_TYPE', 35, 21, TRUE, 'DDR5 совпадает'),
('RAM_MB_TYPE', 35, 22, TRUE, 'DDR5 совпадает'),
('RAM_MB_TYPE', 36, 26, TRUE, 'DDR5 совпадает'),
('RAM_MB_TYPE', 37, 27, TRUE, 'DDR5 совпадает'),
('RAM_MB_TYPE', 38, 26, TRUE, 'DDR5 совпадает'),
('RAM_MB_TYPE', 39, 28, TRUE, 'DDR5 совпадает'),
('RAM_MB_TYPE', 40, 27, TRUE, 'DDR5 совпадает');

-- Примеры несовместимости DDR4 ↔ DDR5
INSERT INTO compatibility_rules (rule_type, component_a_id, component_b_id, is_compatible, reason) VALUES
('RAM_MB_TYPE', 29, 20, FALSE, 'DDR4 не поддерживается платой DDR5'),
('RAM_MB_TYPE', 30, 21, FALSE, 'DDR4 не поддерживается платой DDR5'),
('RAM_MB_TYPE', 35, 17, FALSE, 'DDR5 не поддерживается платой DDR4'),
('RAM_MB_TYPE', 36, 23, FALSE, 'DDR5 не поддерживается платой DDR4');

-- ============================================
-- ТЕСТОВЫЙ ПОЛЬЗОВАТЕЛЬ
-- ============================================
INSERT INTO users (username, password_hash) VALUES
('test_user', 'hash_placeholder');

-- ============================================
-- ФИНАЛЬНЫЙ СБРОС ПОСЛЕДОВАТЕЛЬНОСТЕЙ
-- ============================================
SELECT setval('components_id_seq', 94);
SELECT setval('compatibility_rules_id_seq', (SELECT MAX(id) FROM compatibility_rules));

COMMIT;