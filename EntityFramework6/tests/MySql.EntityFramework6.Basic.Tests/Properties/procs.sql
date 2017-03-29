
DROP PROCEDURE IF EXISTS AddAuthor$$
DROP PROCEDURE IF EXISTS DeleteAuthor$$
DROP PROCEDURE IF EXISTS UpdateAuthor$$

CREATE PROCEDURE AddAuthor(theid INT, thename VARCHAR(20), theage INT) 
BEGIN
	IF theid = 66 THEN
		SELECT SLEEP(30);
	ELSE
		INSERT INTO authors VALUES (theid, thename, theage);
	END IF;
END $$

CREATE PROCEDURE DeleteAuthor(theid int)
BEGIN
	DELETE FROM authors WHERE id=theid;
END $$

CREATE PROCEDURE UpdateAuthor(theid int, thename varchar(20), theage int) 
BEGIN
	UPDATE authors SET `name`=thename, age=theage WHERE id=theid;
END $$

CREATE FUNCTION spFunc(id INT, name VARCHAR(20)) RETURNS INT
BEGIN
	RETURN id;
END $$

