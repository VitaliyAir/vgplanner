-- Переносит все данные тестового пользователя (11111111-...) на реального пользователя Keycloak.
-- Запускать вручную (psql / DBeaver) ПОСЛЕ первого входа нового пользователя в приложение:
-- при первом запросе API сам создаёт строку VUsers с его `sub`.
--
-- 1. Подставьте свой sub (Keycloak -> Users -> ваш пользователь -> ID) в new_sub ниже.
-- 2. Скрипт выполняется в одной транзакции: при любой ошибке ничего не меняется.
-- 3. Если у нового пользователя уже есть VDays на те же даты, что и у тестового,
--    сработает уникальный индекс IX_VDays_UserId_Date — тогда сначала удалите/объедините эти дни.
-- 4. Строка VUsers тестового пользователя намеренно остаётся (её удаление каскадно удалило бы данные).

DO $$
DECLARE
    old_id uuid := '11111111-1111-1111-1111-111111111111';
    new_sub uuid := '00000000-0000-0000-0000-000000000000'; -- <-- ЗАМЕНИТЬ на sub из Keycloak
BEGIN
    IF new_sub = '00000000-0000-0000-0000-000000000000' THEN
        RAISE EXCEPTION 'Укажите реальный sub в new_sub';
    END IF;

    IF NOT EXISTS (SELECT 1 FROM "VUsers" WHERE "UserId" = new_sub) THEN
        RAISE EXCEPTION 'Пользователь % ещё не создан в VUsers: сначала войдите в приложение под ним', new_sub;
    END IF;

    UPDATE "VGoals"         SET "UserId" = new_sub          WHERE "UserId" = old_id;
    UPDATE "VTasks"         SET "UserId" = new_sub          WHERE "UserId" = old_id;
    UPDATE "VDays"          SET "UserId" = new_sub          WHERE "UserId" = old_id;
    UPDATE "VNotifications" SET "UserId" = new_sub          WHERE "UserId" = old_id;
    UPDATE "VTaskLogs"      SET "CreatedByUserId" = new_sub WHERE "CreatedByUserId" = old_id;
    UPDATE "AuditLogs"      SET "UserId" = new_sub          WHERE "UserId" = old_id;
END $$;
