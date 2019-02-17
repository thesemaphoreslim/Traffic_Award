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
                string db_server = configuration["db_server"];
                string db_port = configuration["db_port"];
                string db_uid = configuration["db_uid"];
                string db_pwd = configuration["db_pwd"];
                string db_name = configuration["db_name"];
                string db_connstring = "Server=" + db_server + ";Port=" + db_port + ";Uid=" + db_uid + ";Pwd=" + db_pwd + ";Database=" + db_name;
                string addexchanges = configuration["addexchanges"];
                string getalltransactions = configuration["getalltransactions"];
                string getqualifyingtransactions = configuration["getqualifyingtransactions"];
                string getwinnerdata = configuration["getwinnerdata"];
                string getpoolwallets = configuration["getpoolwallets"];
                string geteligibility = configuration["geteligibility"];
                string checkfortable = configuration["checkfortable"];
                string createexchangetable = configuration["createexchangetable"];
                string createtranstable = configuration["createtranstable"];
                double startdayinterval = double.Parse(configuration["startdayinterval"]);
                double enddayinterval = double.Parse(configuration["enddayinterval"]);
                double burstamount = double.Parse(configuration["burstamount"]);
                double feeamount = double.Parse(configuration["feeamount"]);
                double minfeeamount = double.Parse(configuration["minfeeamount"]);
                double numofwinners = double.Parse(configuration["numofwinners"]);
                string burstAccountAPI = configuration["BurstAccountAPI"];
                string burstTransactionsAPI = configuration["BurstTransactionsAPI"];
                bool removePoolAddresses = bool.Parse(configuration["removepooladdresses"]);
                bool dodouble = bool.Parse(configuration["dodouble"]);
                bool firstrun = true;
                bool keeprunning = true;
                List<string> rafflemembers = new List<string>();
                List<string> rafflewinners = new List<string>();
                List<string> exwallets = new List<string>();
                List<string> alltrans = new List<string>();
                List<string> winnerAddresses = new List<string>();
                Dictionary<string, string> winnerdata = new Dictionary<string, string>();
                #endregion


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
                        #region Check for schema
                        Utilities.queryParameters.Add("@db_name", db_name);
                        Utilities.queryParameters.Add("@table_name", "exchange_wallets");
                        if (string.IsNullOrEmpty(Utilities.GetWinnerData(db_connstring, checkfortable, Utilities.queryParameters, true)))
                        {
                            Utilities.TestDBUpdate(db_connstring, createexchangetable, Utilities.queryParameters, true);
                        }
                        Utilities.queryParameters.Add("@db_name", db_name);
                        Utilities.queryParameters.Add("@table_name", "all_weekly_trans");
                        if (string.IsNullOrEmpty(Utilities.GetWinnerData(db_connstring, checkfortable, Utilities.queryParameters, true)))
                        {
                            Utilities.TestDBUpdate(db_connstring, createtranstable, Utilities.queryParameters, true);
                        }
                        #endregion

                        #region Clearing testdb tables
                        Console.WriteLine("Clearing exchange wallets from test db" + Environment.NewLine);
                        Utilities.TestDBUpdate(db_connstring, "TRUNCATE TABLE exchange_wallets;", Utilities.queryParameters, true);


                        Console.WriteLine("Clearing transaction table from test db" + Environment.NewLine);
                        Utilities.TestDBUpdate(db_connstring, "TRUNCATE TABLE all_weekly_trans;", Utilities.queryParameters, true);
                        #endregion

                        #region Capturing exchange wallets
                        Console.WriteLine("Querying Burst blockchain for exchange wallets based on known exchange addresses (this may take a moment)" + Environment.NewLine);
                        using (DataTable dt = Utilities.DataTableQuery(db_connstring, allexchangewallets, Utilities.queryParameters, true))
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
                                Utilities.TestDBUpdate(db_connstring, sb.ToString(), Utilities.queryParameters, true);
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
                                Utilities.TestDBUpdate(db_connstring, exchange, Utilities.queryParameters, false);
                            }
                        }
                        Console.WriteLine("Exchange wallets captured" + Environment.NewLine);
                        #endregion


                        #region Capturing Transactions
                        Utilities.queryParameters.Add("@starttime", starttimestamp);
                        Utilities.queryParameters.Add("@endtime", endtimestamp);
                        Utilities.queryParameters.Add("@burstamount", burstamount);
                        Utilities.queryParameters.Add("@feeamount", feeamount);
                        Utilities.queryParameters.Add("@minfeeamount", minfeeamount);
                        Console.WriteLine("Querying Burst blockchain for all transactions over " + burstamount + " planck between " + starttimestamp + " and " + endtimestamp + Environment.NewLine);
                        using (DataTable dt = Utilities.DataTableQuery(db_connstring, getalltransactions, Utilities.queryParameters, true))
                        {
                            StringBuilder sb = new StringBuilder();
                            sb.Append("INSERT INTO all_weekly_trans (amount, fee, recipient_id, sender_id, timestamp, trans_id) VALUES ");
                            foreach (DataRow row in dt.Rows)
                            {
                                if (dodouble)
                                {
                                    if (double.Parse(row[1].ToString()) >= feeamount)
                                    {
                                        alltrans.Add(string.Format("({0},{1},{2},{3},{4},{5})", Utilities.SqlEscapeString(row[0]), Utilities.SqlEscapeString(row[1]), Utilities.SqlEscapeString(row[2]), Utilities.SqlEscapeString(row[3]), Utilities.SqlEscapeString(row[4]), Utilities.SqlEscapeString(row[5])));
                                    }
                                    if (double.Parse(row[0].ToString()) >= burstamount)
                                    {
                                        alltrans.Add(string.Format("({0},{1},{2},{3},{4},{5})", Utilities.SqlEscapeString(row[0]), Utilities.SqlEscapeString(row[1]), Utilities.SqlEscapeString(row[2]), Utilities.SqlEscapeString(row[3]), Utilities.SqlEscapeString(row[4]), Utilities.SqlEscapeString(row[5])));
                                        alltrans.Add(string.Format("({0},{1},{2},{3},{4},{5})", Utilities.SqlEscapeString(row[0]), Utilities.SqlEscapeString(row[1]), Utilities.SqlEscapeString(row[2]), Utilities.SqlEscapeString(row[3]), Utilities.SqlEscapeString(row[4]), Utilities.SqlEscapeString(row[5])));
                                    }
                                }
                                else
                                {
                                    alltrans.Add(string.Format("({0},{1},{2},{3},{4},{5})", Utilities.SqlEscapeString(row[0]), Utilities.SqlEscapeString(row[1]), Utilities.SqlEscapeString(row[2]), Utilities.SqlEscapeString(row[3]), Utilities.SqlEscapeString(row[4]), Utilities.SqlEscapeString(row[5])));
                                }
                            }
                            sb.Append(string.Join(",", alltrans)).Append(";");

                            if (alltrans.Count > 0)
                            {
                                Console.WriteLine("Adding transactions to testdb" + Environment.NewLine);
                                Utilities.TestDBUpdate(db_connstring, sb.ToString(), Utilities.queryParameters, true);
                            }
                            else
                            {
                                Console.WriteLine("No transactions found for the given timeframe...Pretty sure your database is blank or out of sync - or maybe you selected an invalid time range. Try again.");
                                return;
                            }
                        }
                        Console.WriteLine("Transactions captured");
                        #endregion
                    }

                    #region Capturing qualifying transactions
                    rafflemembers.Clear();
                    Console.WriteLine("Querying testdb to find qualifying transactions for the raffle" + Environment.NewLine);
                    using (DataTable dt = Utilities.DataTableQuery(db_connstring, getqualifyingtransactions, Utilities.queryParameters, true))
                    {
                        foreach (DataRow row in dt.Rows)
                        {
                            rafflemembers.Add(row[0].ToString());
                        }
                    }
                    Console.WriteLine("Qualifying transactions captured" + Environment.NewLine);
                    #endregion


                    #region Remove Pool wallets
                    Console.WriteLine("There are a total of " + rafflemembers.Count + " raffle entries." + Environment.NewLine);
                    //Console.WriteLine("Do you wish to remove pool wallets from the raffle? (Y/n)" + Environment.NewLine);
                    //if (Console.ReadKey(true).Key == ConsoleKey.Y)
                    if (removePoolAddresses)
                    {
                        Console.WriteLine("Removing pool wallets..." + Environment.NewLine);
                        using (DataTable dt = Utilities.DataTableQuery(db_connstring, getpoolwallets, Utilities.queryParameters, true))
                        {
                            foreach (DataRow row in dt.Rows)
                            {
                                rafflemembers.RemoveAll(item => item == row[0].ToString());
                            }
                        }
                        Console.WriteLine("There are now a total of " + rafflemembers.Count + " raffle entries." + Environment.NewLine);
                    }
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
                                Utilities.queryParameters.Add("@transid", burstaddress.Item2);
                                string eligibleId = Utilities.GetWinnerData(db_connstring, geteligibility, Utilities.queryParameters, true);
                                if (rafflemembers.Contains(eligibleId))
                                {
                                    Console.WriteLine("Congratulations, you are eligible for the raffle and have been entered " + rafflemembers.FindAll(item => item == eligibleId).Count + " time(s)!");
                                }
                                else
                                {
                                    Console.WriteLine("Sorry, you are not eligible for the raffle...");
                                    Console.WriteLine("Send Burst to friends, make purchases from the marketplace, or buy Burst from an exchange to become eligible!" + Environment.NewLine + Environment.NewLine);
                                }
                            }
                            else
                            {
                                Console.WriteLine("Sorry, you are not eligible for the raffle. We could not find any transactions for this BURST address.");
                            }
                            #endregion
                            break;
                        case "runraffle":
                            #region Retrieving raffle winners
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
                                Utilities.queryParameters.Add("@winner", winner);
                                winnerdata.Add(winner, Utilities.GetWinnerData(db_connstring, getwinnerdata, Utilities.queryParameters, true));
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
                            Console.WriteLine(Environment.NewLine + Environment.NewLine);
                            foreach (string address in winnerAddresses)
                            {
                                Console.WriteLine("Congratulations to " + address);
                            }
                            Console.WriteLine(Environment.NewLine + Environment.NewLine);
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
