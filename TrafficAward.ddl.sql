-- ----------------------------------------------------------------
--  TABLE exchange_wallets
-- ----------------------------------------------------------------

CREATE TABLE burstwallet.exchange_wallets
(
   wallet_id    BIGINT(19) NOT NULL,
   PRIMARY KEY(wallet_id)
)
ENGINE INNODB
COLLATE 'utf8_bin'
ROW_FORMAT DEFAULT;

-- ----------------------------------------------------------------
--  TABLE all_weekly_trans
-- ----------------------------------------------------------------

CREATE TABLE burstwallet.all_weekly_trans
(
   db_id           BIGINT(20) NOT NULL AUTO_INCREMENT,
   trans_id        BIGINT(20) NOT NULL,
   amount          BIGINT(20) NOT NULL,
   fee             BIGINT(20) NOT NULL,
   `timestamp`     INT(11) NOT NULL,
   recipient_id    BIGINT(20) NULL,
   sender_id       BIGINT(20) NOT NULL,
   PRIMARY KEY(db_id)
)
ENGINE INNODB
COLLATE 'utf8_bin'
ROW_FORMAT DEFAULT;


