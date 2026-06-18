<div align="center">

# 🖥️ PC Assembly Simulator

### Обучающий Unity-симулятор для изучения комплектующих компьютера и виртуальной сборки ПК

<p>
  <img alt="Unity" src="https://img.shields.io/badge/Unity-3D%20Simulator-111827?style=for-the-badge&logo=unity&logoColor=white">
  <img alt="C#" src="https://img.shields.io/badge/C%23-Application%20Logic-7C3AED?style=for-the-badge&logo=csharp&logoColor=white">
  <img alt="PostgreSQL" src="https://img.shields.io/badge/PostgreSQL-Data%20Storage-2563EB?style=for-the-badge&logo=postgresql&logoColor=white">
  <img alt="Npgsql" src="https://img.shields.io/badge/Npgsql-DB%20Connection-0F766E?style=for-the-badge">
  <img alt="Diploma Project" src="https://img.shields.io/badge/Diploma%20Project-4--22%20%D0%98%D0%A1%D0%9F--8-F59E0B?style=for-the-badge">
</p>

<p>
  <a href="#-about-the-project">
    <img src="https://img.shields.io/badge/ABOUT-PROJECT-374151?style=for-the-badge">
  </a>
  <a href="#-screenshots">
    <img src="https://img.shields.io/badge/SCREENSHOTS-GALLERY-1D4ED8?style=for-the-badge">
  </a>
  <a href="#-quick-start">
    <img src="https://img.shields.io/badge/RUN-LOCALLY-16A34A?style=for-the-badge">
  </a>
</p>

<p>
  <a href="#-about-the-project">About</a> •
  <a href="#-features">Features</a> •
  <a href="#-architecture">Architecture</a> •
  <a href="#-database">Database</a> •
  <a href="#-testing">Testing</a> •
  <a href="#-quick-start">Quick Start</a>
</p>

</div>

---

## 📌

**PC Assembly Simulator** — это интерактивное обучающее приложение, разработанное на **Unity** для демонстрации процесса сборки персонального компьютера в виртуальной 3D-среде.

Проект предназначен для студентов, школьников и начинающих пользователей, которым необходимо изучить устройство системного блока, назначение комплектующих и правила их совместной установки. Вместо изучения сборки только по текстам или видеороликам пользователь работает с виртуальными моделями, выбирает компоненты, размещает их в корпусе и получает обратную связь от системы.

Приложение помогает изучать:

- основные комплектующие персонального компьютера;
- назначение процессора, материнской платы, видеокарты, оперативной памяти, блока питания и накопителей;
- порядок установки компонентов в корпус;
- базовые правила совместимости оборудования;
- сохранение пользовательских сборок и результатов обучения.

---

## 🎯 Цели

Цель проекта — разработать обучающий симулятор сборки персонального компьютера, позволяющий пользователю изучать комплектующие ПК, проверять их совместимость и выполнять виртуальную сборку компьютера в интерактивном режиме.

Основные задачи проекта:

| № | Задача |
|---|---|
| 1 | Разработать пользовательский интерфейс приложения |
| 2 | Реализовать регистрацию и авторизацию пользователей |
| 3 | Создать интерактивную 3D-сцену сборки компьютера |
| 4 | Реализовать механизм перемещения комплектующих Drag-and-Drop |
| 5 | Добавить проверку правильности установки компонентов |
| 6 | Реализовать проверку совместимости комплектующих |
| 7 | Настроить хранение данных в PostgreSQL |
| 8 | Реализовать сохранение и загрузку пользовательских сборок |
| 9 | Провести тестирование основных функций приложения |

---

## 🧩 Особенности

| Модуль | Возможность |
|---|---|
| 🔐 User Account | Регистрация, авторизация и работа с учётной записью пользователя |
| 🖥️ 3D Assembly Room | Интерактивная сцена для виртуальной сборки системного блока |
| 🧲 Drag-and-Drop | Перемещение комплектующих и установка в подходящие слоты |
| 🧠 Compatibility Check | Проверка совместимости компонентов ПК |
| 💡 Hint System | Подсказки при выборе или неправильной установке комплектующих |
| 📦 Component Catalog | Список комплектующих с характеристиками и фильтрацией |
| 💾 Save / Load Builds | Сохранение и загрузка созданных конфигураций компьютера |
| 📊 Learning Results | Формирование результата прохождения обучающего сценария |
| 🗄️ PostgreSQL Storage | Хранение пользователей, комплектующих, сборок и статистики |

---

## 🖼 Скриншоты


| Главное меню | Регистрация / вход |
|---|---|
| <img src="docs/screenshots/main-menu.png" width="390" alt="Main Menu"> | <img src="docs/screenshots/registration.png" width="390" alt="Registration or Login Window"> |

| Комната сборки | Каталог комплектующих |
|---|---|
| <img src="docs/screenshots/assembly-room.png" width="390" alt="Assembly Room"> | <img src="docs/screenshots/component-list.png" width="390" alt="Component Catalog"> |

| Установка комплектующей | Финальный собранный ПК |
|---|---|
| <img src="docs/screenshots/component-installation.png" width="390" alt="Component Installation"> | <img src="docs/screenshots/final-pc-build.png" width="390" alt="Final PC Build"> |

Минимальный набор скриншотов закрывает основные пользовательские сценарии: запуск приложения, вход в систему, выбор комплектующих, процесс сборки и итоговый результат.

---

## 🏗 Архитектура

Приложение построено как Unity-клиент с локальным взаимодействием с базой данных PostgreSQL через библиотеку **Npgsql**.

```mermaid
flowchart LR
    User[👤 Пользователь] --> UI[🧭 Unity UI]
    UI --> Auth[🔐 Авторизация]
    UI --> Scene[🖥️ 3D-сцена сборки]

    Scene --> Catalog[📦 Каталог комплектующих]
    Scene --> DragDrop[🧲 Drag-and-Drop]
    DragDrop --> Slots[📍 Слоты установки]
    Slots --> Checker[🧠 Проверка совместимости]
    Checker --> Result[📊 Результат сборки]

    Auth --> DataLayer[🔌 Npgsql Data Layer]
    Catalog --> DataLayer
    Result --> DataLayer
    DataLayer --> DB[(🗄️ PostgreSQL)]

    Tests[✅ Тестирование] --> UI
    Tests --> Scene
    Tests --> DB
```

### Слои

| Уровень | Технологии | Назначение |
|---|---|---|
| Client | Unity, C# | Интерфейс, 3D-сцена, логика сборки, взаимодействие пользователя с объектами |
| Data Access | Npgsql | Подключение Unity-приложения к PostgreSQL |
| Database | PostgreSQL | Хранение пользователей, комплектующих, сборок, совместимости и статистики |
| Assets | 3D-модели, Unity UI | Визуальное представление компонентов и интерфейса |
| Testing | Ручное и функциональное тестирование | Проверка корректности основных сценариев работы |

---

## 🔄 Application Flow

```mermaid
sequenceDiagram
    participant U as Пользователь
    participant C as Unity App
    participant S as 3D-сцена
    participant L as Логика проверки
    participant D as PostgreSQL

    U->>C: Регистрация / вход
    C->>D: Проверка данных пользователя
    D-->>C: Результат авторизации
    U->>S: Открытие комнаты сборки
    S->>D: Загрузка комплектующих
    D-->>S: Список компонентов
    U->>S: Выбор и перенос комплектующего
    S->>L: Проверка слота установки
    L->>L: Проверка совместимости
    L-->>S: Подсказка / успешная установка
    U->>C: Сохранение сборки
    C->>D: Запись конфигурации и статистики
```

---

## 🗄 БД

База данных используется для хранения информации о пользователях, комплектующих, совместимости компонентов и результатах прохождения обучающих сценариев.

```mermaid
erDiagram
    USERS ||--o{ BUILDS : creates
    CASES ||--o{ BUILDS : selected_for
    BUILDS ||--o{ BUILD_COMPONENTS : contains
    COMPONENTS ||--o{ BUILD_COMPONENTS : included_in
    COMPONENTS ||--o{ COMPATIBILITY : component_a
    COMPONENTS ||--o{ COMPATIBILITY : component_b
    USERS ||--o{ STATISTICS : has

    USERS {
        int user_id PK
        string login
        string password_hash
        datetime created_at
    }

    COMPONENTS {
        int component_id PK
        string name
        string category
        string socket
        string form_factor
        string characteristics
    }

    CASES {
        int case_id PK
        string name
        string form_factor
        string description
    }

    COMPATIBILITY {
        int compatibility_id PK
        int component_a_id FK
        int component_b_id FK
        boolean is_compatible
        string comment
    }

    BUILDS {
        int build_id PK
        int user_id FK
        int case_id FK
        string build_name
        datetime created_at
    }

    BUILD_COMPONENTS {
        int id PK
        int build_id FK
        int component_id FK
        string slot_name
    }

    STATISTICS {
        int statistic_id PK
        int user_id FK
        int score
        int mistakes_count
        datetime finished_at
    }
```

### Database Tables

| Таблица | Назначение |
|---|---|
| `users` | Данные зарегистрированных пользователей |
| `components` | Сведения о комплектующих персонального компьютера |
| `cases` | Информация о корпусах ПК |
| `compatibility` | Правила совместимости комплектующих |
| `builds` | Сохранённые пользовательские сборки |
| `build_components` | Состав конкретной сборки |
| `statistics` | Результаты прохождения обучающих сценариев |

---

## 🧪 Тесты

Тестирование проекта направлено на проверку корректности пользовательских сценариев, стабильности интерфейса, работы Drag-and-Drop и сохранения данных.

| Test Case | What is checked | Expected Result |
|---|---|---|
| Registration Test | Регистрация нового пользователя | Пользователь успешно создаётся в системе |
| Authorization Test | Вход по корректным данным | Пользователь попадает в главное меню |
| Wrong Login Test | Обработка неправильных данных | Система выводит сообщение об ошибке |
| Scene Loading Test | Загрузка комнаты сборки | 3D-сцена открывается без ошибок |
| Drag-and-Drop Test | Перемещение комплектующих | Компонент корректно перемещается пользователем |
| Installation Test | Установка детали в подходящий слот | Компонент фиксируется в нужной области |
| Wrong Slot Test | Попытка неправильной установки | Система запрещает установку и показывает подсказку |
| Compatibility Test | Проверка совместимости компонентов | Несовместимые детали не проходят проверку |
| Save Build Test | Сохранение конфигурации ПК | Сборка записывается в базу данных |
| Load Build Test | Загрузка ранее сохранённой сборки | Конфигурация восстанавливается в приложении |
| Result Test | Формирование результата обучения | Отображается оценка действий пользователя |

---

## 🧠 Образовательная ценность

Симулятор делает процесс изучения сборки ПК более наглядным и практико-ориентированным. Пользователь не просто читает описание комплектующих, а взаимодействует с ними в виртуальной среде: выбирает детали, размещает их в корпусе, анализирует ошибки и получает результат выполнения задания.

Проект может использоваться как учебный тренажёр для изучения:

- устройства персонального компьютера;
- назначения комплектующих;
- последовательности сборки системного блока;
- совместимости аппаратных компонентов;
- базовых принципов работы с базами данных;
- разработки интерактивных приложений на Unity.

---

<div align="center">

### 🖥️ PC Assembly Simulator

**Unity • C# • PostgreSQL • Npgsql • 3D Simulation • Drag-and-Drop**

</div>
