-- Users

GRANT ALL ON *.* TO lbuser@localhost IDENTIFIED BY 'lbpass';

REVOKE SUPER ON *.* FROM lbuser@localhost;
