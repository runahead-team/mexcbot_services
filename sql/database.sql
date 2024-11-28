-- MySQL dump 10.13  Distrib 8.0.38, for macos14 (x86_64)
--
-- Host: 13.215.81.17    Database: MexcBot
-- ------------------------------------------------------
-- Server version	5.7.42

/*!40101 SET @OLD_CHARACTER_SET_CLIENT=@@CHARACTER_SET_CLIENT */;
/*!40101 SET @OLD_CHARACTER_SET_RESULTS=@@CHARACTER_SET_RESULTS */;
/*!40101 SET @OLD_COLLATION_CONNECTION=@@COLLATION_CONNECTION */;
/*!50503 SET NAMES utf8 */;
/*!40103 SET @OLD_TIME_ZONE=@@TIME_ZONE */;
/*!40103 SET TIME_ZONE='+00:00' */;
/*!40014 SET @OLD_UNIQUE_CHECKS=@@UNIQUE_CHECKS, UNIQUE_CHECKS=0 */;
/*!40014 SET @OLD_FOREIGN_KEY_CHECKS=@@FOREIGN_KEY_CHECKS, FOREIGN_KEY_CHECKS=0 */;
/*!40101 SET @OLD_SQL_MODE=@@SQL_MODE, SQL_MODE='NO_AUTO_VALUE_ON_ZERO' */;
/*!40111 SET @OLD_SQL_NOTES=@@SQL_NOTES, SQL_NOTES=0 */;

--
-- Table structure for table `BotOrders`
--

DROP TABLE IF EXISTS `BotOrders`;
/*!40101 SET @saved_cs_client     = @@character_set_client */;
/*!50503 SET character_set_client = utf8mb4 */;
CREATE TABLE `BotOrders` (
  `Id` bigint(20) NOT NULL AUTO_INCREMENT,
  `BotId` bigint(20) NOT NULL,
  `BotType` tinyint(4) NOT NULL,
  `UserId` bigint(20) NOT NULL,
  `OrderId` varchar(64) COLLATE utf8mb4_unicode_ci NOT NULL,
  `OrderListId` varchar(64) COLLATE utf8mb4_unicode_ci NOT NULL,
  `Symbol` varchar(32) COLLATE utf8mb4_unicode_ci NOT NULL,
  `Side` varchar(32) COLLATE utf8mb4_unicode_ci NOT NULL,
  `Price` varchar(32) COLLATE utf8mb4_unicode_ci NOT NULL,
  `OrigQty` varchar(32) COLLATE utf8mb4_unicode_ci NOT NULL,
  `IsRunCancellation` tinyint(4) NOT NULL DEFAULT '0',
  `Status` tinyint(4) NOT NULL,
  `ExpiredTime` bigint(20) DEFAULT NULL,
  `Type` varchar(32) COLLATE utf8mb4_unicode_ci NOT NULL,
  `TransactTime` bigint(20) NOT NULL,
  `BotExchangeType` tinyint(4) NOT NULL,
  PRIMARY KEY (`Id`),
  KEY `BotId` (`BotId`),
  KEY `UserId` (`UserId`)
) ENGINE=MyISAM AUTO_INCREMENT=3033750 DEFAULT CHARSET=utf8mb4 COLLATE=utf8mb4_unicode_ci;
/*!40101 SET character_set_client = @saved_cs_client */;

--
-- Table structure for table `Bots`
--

DROP TABLE IF EXISTS `Bots`;
/*!40101 SET @saved_cs_client     = @@character_set_client */;
/*!50503 SET character_set_client = utf8mb4 */;
CREATE TABLE `Bots` (
  `Id` bigint(20) NOT NULL AUTO_INCREMENT,
  `UserId` bigint(20) NOT NULL,
  `Base` varchar(16) COLLATE utf8_unicode_ci NOT NULL,
  `Quote` varchar(16) COLLATE utf8_unicode_ci NOT NULL,
  `Type` tinyint(4) NOT NULL,
  `VolumeOption` text COLLATE utf8_unicode_ci,
  `MakerOption` text COLLATE utf8_unicode_ci,
  `ApiKey` varchar(128) COLLATE utf8_unicode_ci NOT NULL,
  `ApiSecret` varchar(128) COLLATE utf8_unicode_ci NOT NULL,
  `Passphrase` varchar(128) COLLATE utf8_unicode_ci NULL,
  `Logs` text COLLATE utf8_unicode_ci,
  `Status` tinyint(4) NOT NULL,
  `LastRunTime` bigint(20) DEFAULT NULL,
  `NextRunVolTime` bigint(20) DEFAULT NULL,
  `NextRunMakerTime` bigint(20) DEFAULT NULL,
  `ExchangeInfo` text COLLATE utf8_unicode_ci,
  `AccountInfo` text COLLATE utf8_unicode_ci,
  `CreatedTime` bigint(20) NOT NULL,
  `ExchangeType` tinyint(4) NOT NULL,
  PRIMARY KEY (`Id`),
  KEY `UserId` (`UserId`)
) ENGINE=MyISAM AUTO_INCREMENT=19 DEFAULT CHARSET=utf8 COLLATE=utf8_unicode_ci;
/*!40101 SET character_set_client = @saved_cs_client */;

--
-- Table structure for table `Tokens`
--

DROP TABLE IF EXISTS `Tokens`;
/*!40101 SET @saved_cs_client     = @@character_set_client */;
/*!50503 SET character_set_client = utf8mb4 */;
CREATE TABLE `Tokens` (
  `Id` varchar(128) COLLATE utf8mb4_unicode_ci NOT NULL,
  `Data` varchar(2048) COLLATE utf8mb4_unicode_ci DEFAULT NULL,
  `CreatedTime` datetime NOT NULL DEFAULT CURRENT_TIMESTAMP,
  KEY `Id` (`Id`)
) ENGINE=InnoDB DEFAULT CHARSET=utf8mb4 COLLATE=utf8mb4_unicode_ci;
/*!40101 SET character_set_client = @saved_cs_client */;
/*!40103 SET TIME_ZONE=@OLD_TIME_ZONE */;

/*!40101 SET SQL_MODE=@OLD_SQL_MODE */;
/*!40014 SET FOREIGN_KEY_CHECKS=@OLD_FOREIGN_KEY_CHECKS */;
/*!40014 SET UNIQUE_CHECKS=@OLD_UNIQUE_CHECKS */;
/*!40101 SET CHARACTER_SET_CLIENT=@OLD_CHARACTER_SET_CLIENT */;
/*!40101 SET CHARACTER_SET_RESULTS=@OLD_CHARACTER_SET_RESULTS */;
/*!40101 SET COLLATION_CONNECTION=@OLD_COLLATION_CONNECTION */;
/*!40111 SET SQL_NOTES=@OLD_SQL_NOTES */;

-- Dump completed on 2024-11-25 21:13:12
