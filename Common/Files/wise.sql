----------------------------------------------------------------------
-- This SQL script creates the schemas and tables needed for the 
--  Wise40 project.
--
-- Note:
--   Existing schemas and tables are first DROPPED
----------------------------------------------------------------------


--
-- The activities schema and table
--
DROP SCHEMA IF EXISTS `wise40`;
CREATE SCHEMA `wise40`;
USE `wise40`;

DROP TABLE IF EXISTS `activities`;
CREATE TABLE `activities` (
  `id` int(11) NOT NULL AUTO_INCREMENT,
  `time` timestamp(3) NOT NULL,
  `text` varchar(256) DEFAULT NULL,
  `tags` varchar(128) DEFAULT NULL,
  `code` int(11) DEFAULT NULL,
  `line` varchar(45) DEFAULT NULL,
  PRIMARY KEY (`id`)
) ENGINE=InnoDB DEFAULT CHARSET=utf8mb4 COLLATE=utf8mb4_0900_ai_ci COMMENT='Records the Wise40 activities';



--
-- The wise schema and table
--
DROP SCHEMA IF EXISTS `wise`;
CREATE SCHEMA `wise`;
USE `wise`;

DROP TABLE IF EXISTS `weather`;
CREATE TABLE `weather`.`weather` (
  `Time` TIMESTAMP(3) NOT NULL,
  `Station` VARCHAR(45) NOT NULL,
  `Temperature` DOUBLE NULL,
  `Humidity` DOUBLE NULL,
  `WindSpeed` DOUBLE NULL,
  `WindDir` DECIMAL NULL,
  `StarFWHM` DOUBLE NULL,
  `RainRate` DOUBLE NULL,
  `SkyAmbientTemp` DOUBLE NULL,
  `SensorTemp` DOUBLE NULL,
  `DewPoint` DOUBLE NULL,
  `CloudCover` DOUBLE NULL,
  `Pressure` DOUBLE NULL,
  PRIMARY KEY (`Time`, `Station`)
) ENGINE=InnoDB DEFAULT CHARSET=utf8mb4 COLLATE=utf8mb4_0900_ai_ci COMMENT = 'Keeps record of weather readings at the Wise Observatory';
