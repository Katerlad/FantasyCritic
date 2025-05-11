CREATE TABLE `tbl_discord_notablemissoptions` (
	`Name` VARCHAR(50) NOT NULL,
	PRIMARY KEY (`Name`)
)
;

ALTER TABLE `tbl_discord_leaguechannel`
	ADD COLUMN `ShowPickedGameNews` BIT NULL DEFAULT NULL AFTER `BidAlertRoleID`,
	ADD COLUMN `ShowEligibleGameNews` BIT NULL DEFAULT NULL AFTER `ShowPickedGameNews`,
	ADD COLUMN `ShowCurrentYearGameNewsOnly` BIT NULL DEFAULT NULL AFTER `ShowEligibleGameNews`,
	ADD COLUMN `NotableMissSetting` VARCHAR(50) NULL DEFAULT NULL AFTER `ShowCurrentYearGameNewsOnly`,
	ADD CONSTRAINT `FK_tbl_discord_leaguechannel_tbl_discord_notablemissoptions` FOREIGN KEY (`NotableMissSetting`) REFERENCES `tbl_discord_notablemissoptions` (`Name`) ON UPDATE NO ACTION ON DELETE NO ACTION;

ALTER TABLE `tbl_discord_gamenewschannel`
	ADD COLUMN `ShowMightReleaseInYearNews` BIT NULL AFTER `GameNewsSetting`,
	ADD COLUMN `ShowWillReleaseInYearNews` BIT NULL DEFAULT NULL AFTER `ShowMightReleaseInYearNews`,
	ADD COLUMN `ShowScoreGameNews` BIT NULL DEFAULT NULL AFTER `ShowWillReleaseInYearNews`,
	ADD COLUMN `ShowReleasedGameNews` BIT NULL DEFAULT NULL AFTER `ShowScoreGameNews`,
	ADD COLUMN `ShowNewGameNews` BIT NULL DEFAULT NULL AFTER `ShowReleasedGameNews`,
	ADD COLUMN `ShowEditedGameNews` BIT NULL DEFAULT NULL AFTER `ShowNewGameNews`;
