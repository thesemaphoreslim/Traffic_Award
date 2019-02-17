# Traffic_Award

Designed to award and incentivize Burst transactions across the network, the Traffic_Award program utilizes the executer's Burst database to query for transactions in a given timeframe and add qualifying sender addresses to a raffle. Winners are drawn from a list at random and displayed in the console window.

This application is written in .NET Core.

You will need to be utilizing MariaDB as your database type. The application is not currently compatible with H2.

How it works:

The app first tries to identify any wallets belonging to or created by exchanges. Currently, only Bittrex, Poloniex, and HPool wallets are identified. (See the "allexchangewallets" query located in the "appsettings.json" file for more details on how those wallets are identified)  Once found, these wallet ids are stored in the "exchange_wallets" table.

Next, all Burst transactions for the specified time period with A) amounts greater than or equal to the "burstamount" (1000 Burst) and fees greater than or equal to the "minfeeamount" OR B) fees greater than or equal to the "feeamount" are queried from the database and stored in the "all_weekly_trans" table.

There is an optional parameter called "dodouble" that, when set to true, will weight sender transactions differently. For example, a sender creating a transaction with a fee >= "feeamount" will earn an entry and if the same transaction is for an amount >= "burstamount" AND >= "minfeeamount" the sender will earn 2 additional entries. This is done to incentivize both larger transaction fees and regular transactions, while ensuring regular transactions are weighted more heavily.  If "dodouble" is set to false, each qualifying sender earn only 1 entry.

The "getqualifyingtransactions" query is then executed to retrieve qualifying accounts. A qualifying account is a non-exchange/non-pool address that is sending Burst across the network to another non-exchange/non-pool address. (Note: removing of the pool wallets is an optional filter applied via the "getpoolwallets" query, which removes any known pool wallet ids from the list of qualifying transactions. It is enabled by default.)

Finally, N wallets are randomly selected from the list of qualifying accounts and the sender_id from the database is sent to the "transaction" API to determine the BURST Account ID which can be used to create a multi-out transaction.

You can check if your BURST address is eligible for the raffle and how many entries it has earned by selecting the appropriate menu option.

Exception sender_ids listed below:
5810532812037266198 = Poloniex Wallet
-3382445822566252642 = brs.hpool.com Wallet
-5063553784103844629 = Bittrex Wallet
-1151854202306370986 = Burst Marketing Fund Wallet
-2687430935354532896 = BMF Pool Wallet
