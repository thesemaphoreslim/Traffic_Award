using Microsoft.Extensions.Configuration;
using System;
using System.Collections.Generic;
using System.Data;
using System.IO;
using System.Text;
using System.Threading.Tasks;

namespace Traffic_Award
{
    public class Transaction
    {
        public TransactionData data { get; set; }
    }

    public class TransactionData
    {
        public string sender { get; set; }
    }

    public class TransactionIds
    {
        public List<string> transactionIds { get; set; }
    }

    class Program
    {
        static void Main(string[] args)
        {
            RunAsync().GetAwaiter().GetResult();
        }

        static async Task RunAsync()
        {
            try
            {
                #region Capturing app configs from json
                var builder = new ConfigurationBuilder().SetBasePath(Directory.GetCurrentDirectory()).AddJsonFile("appsettings.json");
                var configuration = builder.Build();
                string allexchangewallets = configuration["allexchangewallets"];
                string blockchainconnection = configuration["burstblockchain"];
                string addexchanges = configuration["addexchanges"];
                string getalltransactions = configuration["getalltransactions"];
                string getqualifyingtransactions = configuration["getqualifyingtransactions"];
                string getwinnerdata = configuration["getwinnerdata"];
                string getpoolwallets = configuration["getpoolwallets"];
                string geteligibility = configuration["geteligibility"];
                double startdayinterval = double.Parse(configuration["startdayinterval"]);
                double enddayinterval = double.Parse(configuration["enddayinterval"]);
                double burstamount = double.Parse(configuration["burstamount"]);
                double feeamount = double.Parse(configuration["feeamount"]);
                double numofwinners = double.Parse(configuration["numofwinners"]);
                string burstAccountAPI = configuration["BurstAccountAPI"];
                string burstTransactionsAPI = configuration["BurstTransactionsAPI"];
                bool removePoolAddresses = bool.Parse(configuration["removepooladdresses"]);
                bool firstrun = true;
                bool keeprunning = true;
                List<string> rafflemembers = new List<string>();
                List<string> rafflewinners = new List<string>();
                List<string> exwallets = new List<string>();
                List<string> alltrans = new List<string>();
                List<string> winnerAddresses = new List<string>();
                Dictionary<string, string> winnerdata = new Dictionary<string, string>();
                #endregion

                Dictionary<string, object> queryParameters = new Dictionary<string, object>();

                #region Calculating timestamp
                DateTime burstepoch = new DateTime(2014, 08, 11, 2, 0, 0);
                DateTime startdate = DateTime.Now.AddDays(startdayinterval);
                DateTime enddate = DateTime.Now.AddDays(enddayinterval);
                double starttimestamp = (startdate - burstepoch).TotalSeconds;
                double endtimestamp = (enddate - burstepoch).TotalSeconds;
                #endregion


                #region Make your selection
                do
                {
                    rafflemembers.Clear();
                    rafflewinners.Clear();
                    exwallets.Clear();
                    alltrans.Clear();
                    winnerAddresses.Clear();
                    winnerdata.Clear();
                    string mySelection = null;
                    do
                    {
                        Console.Clear();
                        Console.WriteLine("********************************************");
                        Console.WriteLine("*   Burst Marketing Fund - Traffic Award   *");
                        Console.WriteLine("********************************************" + Environment.NewLine + Environment.NewLine);
                        Console.WriteLine("Make your selection:" + Environment.NewLine + Environment.NewLine);
                        Console.WriteLine("A) Execute Raffle" + Environment.NewLine + Environment.NewLine);
                        Console.WriteLine("B) Check Wallet Eligibility" + Environment.NewLine + Environment.NewLine);
                        Console.WriteLine("X) Exit" + Environment.NewLine + Environment.NewLine);
                        switch (Console.ReadKey(true).Key)
                        {
                            case ConsoleKey.A:
                                Console.Clear();
                                Console.WriteLine("Executing Raffle..." + Environment.NewLine);
                                mySelection = "runraffle";
                                break;
                            case ConsoleKey.B:
                                Console.Clear();
                                Console.WriteLine("Checking Eligibility..." + Environment.NewLine);
                                mySelection = "checkeligibility";
                                break;
                            case ConsoleKey.X:
                                Console.WriteLine("Exiting..." + Environment.NewLine);
                                return;
                        }
                    } while (string.IsNullOrEmpty(mySelection));
                    #endregion

                    if (firstrun)
                    {
                        #region Clearing testdb tables
                        Console.WriteLine("Clearing exchange wallets from test db" + Environment.NewLine);
                        Utilities.TestDBUpdate(blockchainconnection, "TRUNCATE TABLE exchange_wallets;", queryParameters, true);


                        Console.WriteLine("Clearing transaction table from test db" + Environment.NewLine);
                        Utilities.TestDBUpdate(blockchainconnection, "TRUNCATE TABLE all_weekly_trans;", queryParameters, true);
                        queryParameters.Clear();
                        #endregion

                        #region Capturing exchange wallets
                        Console.WriteLine("Querying Burst blockchain for exchange wallets based on known exchange addresses (this may take a moment)" + Environment.NewLine);
                        using (DataTable dt = Utilities.DataTableQuery(blockchainconnection, allexchangewallets, queryParameters, true))
                        {
                            StringBuilder sb = new StringBuilder();
                            sb.Append("INSERT INTO exchange_wallets (wallet_id) VALUES ");
                            foreach (DataRow row in dt.Rows)
                            {
                                exwallets.Add(string.Format("({0})", row[0]));
                            }
                            sb.Append(string.Join(",", exwallets)).Append(";");

                            Console.WriteLine(exwallets.Count + " exchange wallets found. Adding exchange wallets to testdb" + Environment.NewLine);
                            if (exwallets.Count > 0)
                            {
                                Utilities.TestDBUpdate(blockchainconnection, sb.ToString(), queryParameters, true);
                                queryParameters.Clear();
                            }
                            else
                            {
                                Console.WriteLine("No exchange wallets found...Pretty sure your database is blank or out of sync.  Resync and try again.");
                                return;
                            }
                            Console.WriteLine("Exchange wallets added to testdb" + Environment.NewLine);

                            Console.WriteLine("Adding known exchange wallets to testdb" + Environment.NewLine);
                            foreach (string exchange in addexchanges.Split(','))
                            {
                                Utilities.TestDBUpdate(blockchainconnection, exchange, queryParameters, false);
                            }
                        }
                        Console.WriteLine("Exchange wallets captured" + Environment.NewLine);
                        queryParameters.Clear();
                        #endregion


                        #region Capturing Transactions
                        queryParameters.Add("@starttime", starttimestamp);
                        queryParameters.Add("@endtime", endtimestamp);
                        queryParameters.Add("@burstamount", burstamount);
                        queryParameters.Add("@feeamount", feeamount);
                        Console.WriteLine("Querying Burst blockchain for all transactions over " + burstamount + " planck between " + starttimestamp + " and " + endtimestamp + Environment.NewLine);
                        using (DataTable dt = Utilities.DataTableQuery(blockchainconnection, getalltransactions, queryParameters, true))
                        {
                            queryParameters.Clear();
                            StringBuilder sb = new StringBuilder();
                            sb.Append("INSERT INTO all_weekly_trans (amount, fee, recipient_id, sender_id, timestamp, trans_id) VALUES ");
                            foreach (DataRow row in dt.Rows)
                            {
                                alltrans.Add(string.Format("({0},{1},{2},{3},{4},{5})", Utilities.SqlEscapeString(row[0]), Utilities.SqlEscapeString(row[1]), Utilities.SqlEscapeString(row[2]), Utilities.SqlEscapeString(row[3]), Utilities.SqlEscapeString(row[4]), Utilities.SqlEscapeString(row[5])));
                            }
                            sb.Append(string.Join(",", alltrans)).Append(";");

                            if (alltrans.Count > 0)
                            {
                                Console.WriteLine("Adding transactions to testdb" + Environment.NewLine);
                                Utilities.TestDBUpdate(blockchainconnection, sb.ToString(), queryParameters, true);
                            }
                            else
                            {
                                Console.WriteLine("No transactions found for the given timeframe...Pretty sure your database is blank or out of sync - or maybe you selected an invalid time range. Try again.");
                                return;
                            }
                        }
                        Console.WriteLine("Transactions captured");
                        queryParameters.Clear();
                        #endregion
                    }

                    #region Capturing qualifying transactions
                    rafflemembers.Clear();
                    Console.WriteLine("Querying testdb to find qualifying transactions for the raffle" + Environment.NewLine);
                    using (DataTable dt = Utilities.DataTableQuery(blockchainconnection, getqualifyingtransactions, queryParameters, true))
                    {
                        queryParameters.Clear();
                        foreach (DataRow row in dt.Rows)
                        {
                            rafflemembers.Add(row[0].ToString());
                        }
                    }
                    Console.WriteLine("Qualifying transactions captured" + Environment.NewLine);
                    queryParameters.Clear();
                    #endregion


                    #region Remove Pool wallets
                    Console.WriteLine("There are a total of " + rafflemembers.Count + " raffle entries." + Environment.NewLine);
                    //Console.WriteLine("Do you wish to remove pool wallets from the raffle? (Y/n)" + Environment.NewLine);
                    //if (Console.ReadKey(true).Key == ConsoleKey.Y)
                    if (removePoolAddresses)
                    {
                        Console.WriteLine("Removing pool wallets..." + Environment.NewLine);
                        using (DataTable dt = Utilities.DataTableQuery(blockchainconnection, getpoolwallets, queryParameters, true))
                        {
                            foreach (DataRow row in dt.Rows)
                            {
                                rafflemembers.RemoveAll(item => item == row[0].ToString());
                            }
                        }
                    }
                    queryParameters.Clear();
                    #endregion

                    firstrun = false;
                    Tuple<bool, string> burstaddress = null;
                    switch (mySelection)
                    {
                        case "checkeligibility":
                            #region Checking eligibility
                            string yourburstaddress = null;
                            do
                            {
                                Console.WriteLine("Enter your BURST address (Example: BURST-1234-5678-ABCD-EFGH)" + Environment.NewLine);
                                yourburstaddress = Console.ReadLine();
                            } while (yourburstaddress.Split('-').Length < 5);
                            Console.WriteLine("Checking eligibility. This may take a moment" + Environment.NewLine);
                            burstaddress = await Utilities.GetTransactionIds(burstTransactionsAPI, yourburstaddress);
                            if (burstaddress.Item1)
                            {
                                queryParameters.Add("@transid", burstaddress.Item2);
                                string eligibleId = Utilities.GetWinnerData(blockchainconnection, geteligibility, queryParameters, true);
                                if (rafflemembers.Contains(eligibleId))
                                {
                                    Console.WriteLine("Congratulations, you are eligible for the raffle and have been entered " + rafflemembers.FindAll(item => item == eligibleId).Count + " time(s)!");
                                }
                                else
                                {
                                    Console.WriteLine("Sorry, you are not eligible for the raffle...");
                                    Console.WriteLine("Send Burst to friends, make purchases from the marketplace, or buy Burst from an exchange to become eligible!" + Environment.NewLine + Environment.NewLine);
                                }
                                queryParameters.Clear();
                            }
                            else
                            {
                                Console.WriteLine("Sorry, you are not eligible for the raffle. We could not find any transactions for this BURST address.");
                            }
                            #endregion
                            break;
                        case "runraffle":
                            #region Retrieving raffle winners
                            Console.WriteLine("There are a total of " + rafflemembers.Count + " raffle entries." + Environment.NewLine);
                            Console.WriteLine("Retrieving raffle winners at random" + Environment.NewLine);
                            
                            if (rafflemembers.Count > 0)
                            {
                                if (rafflemembers.Count < numofwinners) numofwinners = rafflemembers.Count;
                                Random rnd = new Random();
                                int random = 0;
                                string winner = null;
                                for (int i = 0; i < numofwinners; i++)
                                {
                                    random = rnd.Next(rafflemembers.Count);
                                    winner = rafflemembers[random];
                                    //Console.WriteLine("Winner! " + winner);
                                    rafflemembers.RemoveAll(item => item == winner);
                                    rafflewinners.Add(winner);
                                    if (rafflemembers.Count == 0) break;
                                }
                            }
                            else
                            {
                                Console.WriteLine("No eligible raffle winners...Sad!" + Environment.NewLine);
                            }
                            #endregion


                            #region Retrieving transaction details for each winner to obtain Burst address
                            Console.WriteLine("Retrieving a transaction for each raffle winner" + Environment.NewLine);
                            foreach (string winner in rafflewinners)
                            {
                                queryParameters.Add("@winner", winner);
                                winnerdata.Add(winner, Utilities.GetWinnerData(blockchainconnection, getwinnerdata, queryParameters, true));
                                queryParameters.Clear();
                            }
                            Console.WriteLine("Finished retrieving transaction data for each raffle winner" + Environment.NewLine);
                            #endregion


                            #region Get Burst Account via API
                            Console.WriteLine("Calling Burst API to retrieve winner's BURST account" + Environment.NewLine);
                            foreach (KeyValuePair<string, string> winner in winnerdata)
                            {
                                burstaddress = await Utilities.GetBurstAddress(burstAccountAPI, winner.Value);
                                if (burstaddress.Item1)
                                {
                                    //Console.WriteLine("sender_id = " + winner.Key + " --- trans_id = " + winner.Value + " --- burst acct = " + burstaddress.Item2);
                                    winnerAddresses.Add(burstaddress.Item2);
                                }
                                else
                                {
                                    Console.WriteLine("Error getting Burst account for sender_id " + winner.Key + " and trans_id " + winner.Value + ": " + burstaddress.Item2 + Environment.NewLine);
                                    Console.ReadKey(true);
                                }
                            }
                            #endregion


                            #region Display Winners!
                            foreach (string address in winnerAddresses)
                            {
                                Console.WriteLine("Congratulations to " + address);
                            }
                            #endregion
                            break;
                    }
                    Console.WriteLine("Done! Press any key to continue...");
                    Console.ReadKey(true);
                } while (keeprunning);
            }
            catch (Exception ex)
            {
                Console.WriteLine("Error: " + ex);
            }
        }
    }
}
