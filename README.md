# TgBooking

Telegram-бот для бронирования услуг на C# с PostgreSQL.

## Возможности

- Регистрация клиента: имя и телефон
- Запись на услугу: выбор услуги, дня текущего месяца и времени (09:00–20:00)
- Уведомление администратора и обработка заявки (подтвердить / отменить / перенести)
- Управление услугами администратором (добавить / удалить)
- Просмотр списка заявок из базы данных

## Локальный запуск

1. Скопируйте `.env.example` в `.env` и заполните значения
2. Запустите контейнеры:

```bash
docker compose up -d
docker compose up -d --build
```

3. Откройте бота в Telegram и отправьте `/start`

## GitHub Secrets для деплоя


 `BOT_TOKEN`  Токен Telegram-бота 
 `ADMIN_TELEGRAM_ID`  ID администратора 
 `POSTGRES_PASSWORD`  Пароль PostgreSQL 
 `SERVER_HOST`  IP или домен VPS 
 `SERVER_USER`  SSH-логин 
 `SERVER_PASSWORD`  SSH-пароль 


## Подготовка VPS

1.  Docker и Docker Compose при первичном деплое авто установка. 

## Структура

- `src/TgBooking` — приложение бота
- `db/init.sql` — схема PostgreSQL


