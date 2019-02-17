# Traffic_Award

Designed to award and incentivize Burst transactions across the network, the Traffic_Award program utilizes the executer's Burst database to query for transactions in a given timeframe and add qualifying sender addresses to a raffle. Winners are drawn from a list at random and displayed in the console window.

This application is written in .NET Core.

You will need to be utilizing MariaDB as your database type. The application is not currently compatible with H2.

How it works:

The app first tries to identify any wallets belonging to or created by exchanges. Currently, only Bittrex and Poloniex wallets are identified. (See the "allexchangewallets" query located in the "appsettings.json" file for more details on how those wallets are identified)  Once found, these wallet ids are stored in the "exchange_wallets" table.

Next, all Burst transactions for the specified time period are queried from the database and stored in the "all_weekly_trans" table. Note, this app was originally intended to be run once a week, but the user can specify a start and end time (startdayinterval/enddayinterval) for the "getalltransactions" query which allows it to find transactions across any timeframe.

The "getqualifyingtransactions" query is then executed to retrieve qualifying accounts. A qualifying account is an address that is sending Burst across the network but NOT to a known exchange wallet.

An optional filter can also be applied via the "getpoolwallets" query, which removes any known pool wallet ids from the list of qualifying transactions.

Finally, N wallets are randomly selected from the list of qualifying accounts and the sender_id from the database is sent to the "transaction" API to determine the BURST Account ID which can be used to create a multi-out transaction.
