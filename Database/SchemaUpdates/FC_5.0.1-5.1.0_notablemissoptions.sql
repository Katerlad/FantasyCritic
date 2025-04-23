CREATE TABLE `tbl_discord_notablemissoptions` (
	`Name` VARCHAR(50) NOT NULL,
	PRIMARY KEY (`Name`) USING BTREE
)
ENGINE=InnoDB
;

INSERT INTO `tbl_discord_notablemissoptions` (`Name`) VALUES ('InitialScore');
INSERT INTO `tbl_discord_notablemissoptions` (`Name`) VALUES ('ScoreUpdates');
INSERT INTO `tbl_discord_notablemissoptions` (`Name`) VALUES ('None');

ALTER TABLE `tbl_discord_leaguechannel`
	ADD COLUMN `NotableMissSetting` VARCHAR(50) NULL DEFAULT NULL AFTER `BidAlertRoleID`,
	ADD CONSTRAINT `FK_tbl_discord_leaguechannel_tbl_discord_notablemissoptions` FOREIGN KEY (`NotableMissSetting`) REFERENCES `tbl_discord_notablemissoptions` (`Name`) ON UPDATE NO ACTION ON DELETE NO ACTION;

UPDATE `tbl_discord_leaguechannel` SET `NotableMissSetting` = 'ScoreUpdates' WHERE `SendNotableMisses` = 1;
UPDATE `tbl_discord_leaguechannel` SET `NotableMissSetting` = 'None' WHERE `SendNotableMisses` = 0;

ALTER TABLE `tbl_discord_leaguechannel`
	CHANGE COLUMN `NotableMissSetting` `NotableMissSetting` VARCHAR(50) NOT NULL AFTER `BidAlertRoleID`;