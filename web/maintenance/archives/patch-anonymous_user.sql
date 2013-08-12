select user_id, user_name into @id, @name from users where user_name = "Anonymous" and user_service_id = 1;
update `attachments` set at_user = @id, at_user_text = @name where at_user = 0;
update `pages` set page_user_id = @id where page_user_id = 0 AND page_namespace IN (0, 2, 10);