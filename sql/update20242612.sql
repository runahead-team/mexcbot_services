create table BotHistory(
    `Id` bigint(20) NOT NULL AUTO_INCREMENT,
    `Date` bigint(20) NOT NULL,
    `BotId` bigint(20) NOT NULL,
    `BalanceBase` varchar(32),
    `BalanceQuote` varchar(32),
    `Spread` varchar(32),
    PRIMARY KEY (`Id`),
    KEY `BotIdDate` (`BotId`, `Date`)
) ENGINE = InnoDB;

alter table
    Bots
add
    `BasePrecision` int;

alter table
    Bots
add
    `QuotePrecision` int;

alter table
    Bots
add
    IsRunBlinking bit default 0;