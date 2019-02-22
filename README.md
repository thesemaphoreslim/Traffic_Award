# Traffic_Award

Designed to incentivize Burst transactions and exchange purchases, the Traffic_Award program utilizes the user's local Burst node to query for transactions in a given timeframe and add qualifying addresses to a raffle. Winners are drawn from a list at random and displayed in the console window.

A qualifying address is one that is not:
 - An exchange wallet
 - A Burst pool
 - A BMF wallet
 - A Mortimer wallet
 - My wallet

When a valid sender/transaction is identified, a recursive function is called to collect all addresses to which this address has sent Burst over the current raffle period. If any of the addresses or any of their fellows (again, recursive) have sold to an exchange then the current entry(s) will be voided.

Currently, I am only monitoring the Poloniex, Bittrex, and Livecoin addresses for purchases. If you can provide the Burst wallet address of any other exchanges supporting Burst, I would be happy to integrate them into the Raffle.

This application is written in .NET Core so though each package is large, it is completely self-contained and requires no additional libraries or references.

You will need to be utilizing MariaDB as your database type. The application is not currently compatible with H2.

You will need to have a functioning, updated, and running node in order to execute the raffle.
