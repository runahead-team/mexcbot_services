CREATE DATABASE IF NOT EXISTS `MexcBot`;

USE `MexcBot`;

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

CREATE TABLE Bots
  (
    Id BIGINT NOT NULL AUTO_INCREMENT,
    UserId BIGINT NOT NULL,
    Base VARCHAR (16) NOT NULL,
    Quote VARCHAR (16) NOT NULL,
    `Type` TINYINT NOT NULL,
    VolumeOption TEXT,
    MakerOption TEXT,
    ApiKey VARCHAR(128) NOT NULL,
    ApiSecret VARCHAR(128) NOT NULL,
    Logs TEXT,
    `Status` TINYINT NOT NULL,
    LastRunTime BIGINT NULL,
    NextRunVolTime BIGINT NULL,
    NextRunMakerTime BIGINT NULL,
    ExchangeInfo TEXT,
    AccountInfo TEXT,
    CreatedTime BIGINT NOT NULL,
    PRIMARY KEY(Id),
    INDEX(UserId)
  )
ENGINE = MyISAM,
DEFAULT CHARSET utf8
COLLATE utf8_unicode_ci;
ALTER TABLE Bots AUTO_INCREMENT = 1;

CREATE TABLE BotOrders
  (
    Id BIGINT NOT NULL AUTO_INCREMENT,
    BotId BIGINT NOT NULL,
    BotType TINYINT NOT NULL,
    UserId BIGINT NOT NULL,
    OrderId VARCHAR(64) NOT NULL,
    OrderListId VARCHAR(64) NOT NULL,
    Symbol VARCHAR(32) NOT NULL,
    Side VARCHAR(32) NOT NULL,
    Price VARCHAR(32) NOT NULL,
    OrigQty VARCHAR(32) NOT NULL,
    IsRunCancellation TINYINT NOT NULL DEFAULT 0,
    `Status` TINYINT NOT NULL,
    ExpiredTime BIGINT NULL,
    `Type` VARCHAR(32) NOT NULL,
    `TransactTime` BIGINT NOT NULL,
    PRIMARY KEY(Id),
    INDEX(BotId),
    INDEX(UserId)
  )
ENGINE = MyISAM;
ALTER TABLE BotOrders AUTO_INCREMENT = 1;