CREATE DATABASE IF NOT EXISTS `MultexBot`;

USE `MultexBot`;

-- BASE

-- Tokens
CREATE TABLE Tokens
  (
    Id VARCHAR(128) NOT NULL,
    `Data` VARCHAR (2048),
    `CreatedTime` DATETIME NOT NULL DEFAULT NOW(),
    INDEX (`Id`)
  )
ENGINE = InnoDB;

-- ADMIN

-- AdminRoles
CREATE TABLE AdminRoles
  (
    `Name` VARCHAR(32) NOT NULL UNIQUE,
    `Scopes` VARCHAR(1024) NOT NULL,
    PRIMARY KEY (`Name`)
  )
ENGINE = MyISAM;
INSERT INTO AdminRoles
  (`Name`,`Scopes`)
VALUES
  ('MASTER','ADMIN_FULL;ROLE_FULL;COIN_FULL;USER_FULL;USER_ACCESS;USER_READ;FUND_FULL;FUND_READ;NFT_FULL');


-- Admins
CREATE TABLE Admins
  (
    `Id` BIGINT NOT NULL AUTO_INCREMENT,
    `Email` VARCHAR(128) NOT NULL UNIQUE,
    `Password` VARCHAR(128) NOT NULL,
    `Role` VARCHAR(32) NOT NULL,
    `CreatedTime` BIGINT NOT NULL,
    `UpdatedTime` BIGINT,
    `GaSecret` VARCHAR (128),
    `GaEnable` BIT DEFAULT 0,
    PRIMARY KEY (`Id`),
    INDEX (`Email`)
  )
ENGINE = InnoDB;

INSERT INTO Admins
  (`Email`,`Password`,`Role`,`CreatedTime`)
VALUES
  ('nnam2404@gmail.com',UPPER(SHA2('12345678',512)),'MASTER',UNIX_TIMESTAMP() * 1000);

-- USER

-- Users
CREATE TABLE Users
  (
    `Id` BIGINT NOT NULL AUTO_INCREMENT,
    `Email` VARCHAR(128) NOT NULL UNIQUE,
    `Username` VARCHAR(32) NOT NULL UNIQUE,
    `Account` VARCHAR(128),
    `AvatarImage` VARCHAR(256),
    `CoverImage` VARCHAR(256),
    `VerifyLevel` TINYINT NOT NULL DEFAULT 0,
    `Status` TINYINT NOT NULL DEFAULT 1,
    `Rank` TINYINT NOT NULL DEFAULT 0,
    `MemberCount` INT NOT NULL DEFAULT 0,
    `Password` VARCHAR(128) NOT NULL,
    `GaSecret` VARCHAR(128),
    `GaEnable` BIT DEFAULT 0,
    `SponsorId` BIGINT,
    `SponsorUsername` VARCHAR(32),
    `CreatedTime` BIGINT NOT NULL,
    `UpdatedTime` BIGINT,
    `PasswordUpdatedTime` BIGINT,
    `BlockWithdraw` BIT DEFAULT 0,
    `PostCount` INT NOT NULL DEFAULT 0,
    `DonateCount` INT NOT NULL DEFAULT 0,
    `TotalDonate` DECIMAL(20,8) NOT NULL DEFAULT 0,
    `FollowCount` INT NOT NULL DEFAULT 0,
    PRIMARY KEY(Id),
    INDEX(Username)
  )
ENGINE = InnoDB;
ALTER TABLE Users AUTO_INCREMENT = 6789;

INSERT INTO Users
    (Username,Email,Password,SponsorId,SponsorUsername,VerifyLevel,Status,Rank,CreatedTime)
VALUES
    ('multexbot','multexbot@gmail.com',UPPER(SHA2('12345678',512)),0,"system",1,1,0,UNIX_TIMESTAMP() * 1000);

-- DirectReferrals
CREATE TABLE DirectReferrals
  (
    `UserId` BIGINT NOT NULL,
    `SponsorId` BIGINT NOT NULL,
    `Level` TINYINT NOT NULL,
    `CreatedTime` BIGINT NOT NULL,
    PRIMARY KEY(`UserId`,`SponsorId`)
  )
ENGINE = InnoDB;

CREATE TABLE Bots
  (
    Id BIGINT NOT NULL AUTO_INCREMENT,
    `Guid` VARCHAR(32) NOT NULL UNIQUE,
    UserId BIGINT NOT NULL,
    Email VARCHAR(128) NOT NULL,
    `Name` VARCHAR(128) NOT NULL,
    RootId BIGINT,
    ExchangeType TINYINT NOT NULL,
    ApiKey VARCHAR(128) NOT NULL,
    SecretKey VARCHAR(128) NOT NULL,
    Symbol VARCHAR(32) NOT NULL,
    Base VARCHAR(32) NOT NULL,
    Quote VARCHAR(32) NOT NULL,
    Side TINYINT NOT NULL,
    IsActive BIT NOT NULL DEFAULT 0,
    LastExecute BIGINT NOT NULL,
    NextTime BIGINT NOT NULL,
    LastPrice DECIMAL(20,8) UNSIGNED NOT NULL,
    LastPriceUsd DECIMAL(20,8) UNSIGNED NOT NULL,
    Options TEXT NOT NULL,
    Log TEXT,
    PRIMARY KEY(Id),
    INDEX(Guid),
    INDEX(UserId)
  )
ENGINE = MyISAM,
DEFAULT CHARSET utf8
COLLATE utf8_unicode_ci;
ALTER TABLE Bots AUTO_INCREMENT = 123;

CREATE TABLE BotOrders
  (
    Id BIGINT NOT NULL AUTO_INCREMENT,
    `Guid` VARCHAR(32) NOT NULL UNIQUE,
    BotId BIGINT NOT NULL,
    UserId BIGINT NOT NULL,
    ExternalId BIGINT NOT NULL,
    Symbol VARCHAR(32) NOT NULL,
    Base VARCHAR(32) NOT NULL,
    Quote VARCHAR(32) NOT NULL,
    Side TINYINT NOT NULL,
    Price DECIMAL(20,8) UNSIGNED NOT NULL,
    Qty DECIMAL(20,8) UNSIGNED NOT NULL,
    Total DECIMAL(20,8) UNSIGNED NOT NULL,
    ExpiredTime BIGINT NOT NULL,
    IsExpired BIT NOT NULL,
    `Time` BIGINT NOT NULL,
    PRIMARY KEY(Id),
    INDEX(Guid),
    INDEX(BotId),
    INDEX(UserId)
  )
ENGINE = MyISAM;
ALTER TABLE BotOrders AUTO_INCREMENT = 123;