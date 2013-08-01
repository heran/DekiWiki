--
-- MindTouch: 
-- bugfix #849: cur_restrictions field type of cur table was changed from tinyblob to mediumblob. 
-- see check-in: r1942
-- Nov 2005
--
ALTER TABLE cur MODIFY cur_restrictions MEDIUMBLOB NOT NULL;