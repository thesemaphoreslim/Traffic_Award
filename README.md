# Traffic_Award

Designed to award and incentivize Burst transactions across the network, the Traffic_Award program utilizes the executer's Burst database to query for transactions in a given timeframe and add qualifying sender addresses to a raffle. Winners are drawn from a list at random and displayed in the console window.

This application is written in .NET Core.

You will need to be utilizing MariaDB as your database type. The application is not currently compatible with H2.

How it works:

First, exchange and Mortimer wallet ids are stored in the "exchange_wallets" table.

Next, all Burst transactions for the specified time period with amounts greater than or equal to the "burstamount" (1000 Burst) OR fees greater than or equal to the "feeamount" are queried from the database and stored in the "all_weekly_trans" table.

There is an optional parameter called "dodouble" that, when set to true, will weight sender transactions differently. For example, a sender creating a transaction with a fee >= "feeamount" will earn an entry and if the same transaction is for an amount >= "burstamount" the sender will earn 2 additional entries. This is done to incentivize both larger transaction fees and regular transactions, while ensuring regular transactions are weighted more heavily.  If "dodouble" is set to false, each qualifying sender earns only 1 entry.

The "getqualifyingtransactions" query is then executed to retrieve qualifying accounts. A qualifying account is one that is NOT an exchange, pool, Mortimer, or BMF wallet that is sending Burst across the network to another qualifying account.

When all the qualifying transactions/senders are collected, raffle entries for a single wallet exceeding the "maxraffleentries" value (50) are reduced to the max value. This is done to reduce the potential for "gaming" the system.

Finally, N wallets are randomly selected from the list of qualifying accounts and displayed in the console window. Exports of the raffle contestants and their entries are exported to "contestants.csv" and the winners are exported to "winners.csv".

You can check if your BURST address is eligible for the raffle and how many entries it has earned by selecting the appropriate menu option.

COMING SOON: Identifying purchases of BURST from exchanges. Buyers get 10x raffle entries! HODL only, no SODL.

Exception sender_ids listed below:
5810532812037266198 = Poloniex Wallet
-3382445822566252642 = brs.hpool.com Wallet
-5063553784103844629 = Bittrex Wallet
-1151854202306370986 = Burst Marketing Fund Wallet
-2687430935354532896 = BMF Pool Wallet
-6170125764990584993 = Mortimer
8909519353220579349 = Dev Wallet

