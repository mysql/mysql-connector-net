CREATE TABLE IF NOT EXISTS webpages_membership (
    UserId int NOT NULL primary key,
    CreateDate datetime NULL,
    ConfirmationToken nvarchar(128) NULL,
    IsConfirmed bit NULL DEFAULT 0,
    LastPasswordFailureDate datetime NULL,
    PasswordFailuresSinceLastSuccess int NOT NULL DEFAULT 0,
    Password nvarchar(128) NOT NULL,
    PasswordChangedDate datetime NULL,
    PasswordSalt nvarchar(128) NOT NULL,
    PasswordVerificationToken nvarchar(128) NULL,
    PasswordVerificationTokenExpirationDate datetime NULL
);

CREATE TABLE IF NOT EXISTS webpages_OAuthMembership(
	Provider nvarchar(30) NOT NULL,
	ProviderUserId nvarchar(100) NOT NULL,
	UserId int NOT NULL, primary key (Provider, ProviderUserId));
	
CREATE TABLE IF NOT EXISTS webpages_OAuthtoken (
	Token nvarchar(100) not null primary key, 
	Secret nvarchar(100) not null);

CREATE TABLE IF NOT EXISTS webpages_Roles(
	RoleId int auto_increment NOT NULL primary key,
	RoleName varchar(256) NOT NULL);

CREATE TABLE IF NOT EXISTS webpages_UsersInRoles(
	UserId int NOT NULL,
	RoleId int NOT NULL,
    primary key(UserId, RoleId),
    CONSTRAINT fk_RoleId FOREIGN KEY(RoleId) REFERENCES webpages_Roles (RoleId));