DROP DATABASE IF EXISTS {0};


CREATE DATABASE {0};
USE {0};

CREATE TABLE items (
  item_id int primary key auto_increment,
  description varchar(50) not null,
  brand varchar(50),
  price float not null,
  quantity int not null
);

CREATE TABLE stores(
  store_id int primary key auto_increment,
  name varchar(50) not null,
  address varchar(100)
);

CREATE TABLE employees(
  employee_id int primary key auto_increment,
  name varchar(50) not null,
  store_id int,
  active bool
);

CREATE TABLE orders(
  order_id int primary key auto_increment,
  employee_id int not null,
  client_name varchar(50)
);

CREATE TABLE order_details(
  order_id int not null,
  item_id int not null,
  quantity int,
  price float,
  discount float
);



-- Populating data

INSERT INTO stores VALUES(null, 'Main', 'Superdome Minessota');

INSERT INTO items VALUES(null, 'Wings', 'OwnMark', 12.99, 12);
INSERT INTO items VALUES(null, 'Cookies', 'The Bear', 1.99, 1);
INSERT INTO items VALUES(null, 'Icecream', 'Vanilla', 2.99, 1);

INSERT INTO employees VALUES(null, 'Marcelino', 1, true);
