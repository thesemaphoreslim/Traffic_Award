using Microsoft.Extensions.Configuration;
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Data;
using System.IO;
using System.Text;
using System.Threading;
using System.Linq;
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

    public class AccountRS
    {
        public string accountRS { get; set; }
        public string account { get; set; }
    }

    public class StringID
    {
        public string stringId { get; set; }
    }

    class Program
    {
        #region Capturing app configs from json
        static IConfigurationBuilder builder = new ConfigurationBuilder().SetBasePath(Directory.GetCurrentDirectory()).AddJsonFile("appsettings.json");
        static IConfigurationRoot configuration = builder.Build();
        static string allexchangewallets = configuration["allexchangewallets"];
        static string db_server = configuration["db_server"];
        static string db_port = configuration["db_port"];
        static string db_uid = configuration["db_uid"];
        static string db_pwd = configuration["db_pwd"];
        static string db_name = configuration["db_name"];
        static string db_connstring = "Server=" + db_server + ";Port=" + db_port + ";Uid=" + db_uid + ";Pwd=" + db_pwd + ";Database=" + db_name;
        static string addexchanges = configuration["addexchanges"];
        static string getalltransactions = configuration["getalltransactions"];
        static string getqualifyingtransactions = configuration["getqualifyingtransactions"];
        static string getwinnerdata = configuration["getwinnerdata"];
        static string getpoolwallets = configuration["getpoolwallets"];
        static string geteligibility = configuration["geteligibility"];
        static string checkfortable = configuration["checkfortable"];
        static string createexchangetable = configuration["createexchangetable"];
        static string createtranstable = configuration["createtranstable"];
        static double startdayinterval = double.Parse(configuration["startdayinterval"]);
        static double enddayinterval = double.Parse(configuration["enddayinterval"]);
        static double burstamount = double.Parse(configuration["burstamount"]);
        static double feeamount = double.Parse(configuration["feeamount"]);
        static double minfeeamount = double.Parse(configuration["minfeeamount"]);
        static double numofwinners = double.Parse(configuration["numofwinners"]);
        static string burstTransactionsAPI = configuration["BurstTransactionsAPI"];
        static string burstAddressAPI = configuration["BurstAddressAPI"];
        static string burstStringID = configuration["BurstStringID"];
        static bool removePoolAddresses = bool.Parse(configuration["removepooladdresses"]);
        static bool dodouble = bool.Parse(configuration["dodouble"]);
        static int maxraffleentries = int.Parse(configuration["maxraffleentries"]);
        static string contestantsfile = configuration["contestantsfile"];
        static string winnersfile = configuration["winnersfile"];
        static bool keeprunning = true;
        //static ConcurrentBag<string> rafflemembers = new ConcurrentBag<string>();
        static ConcurrentDictionary<string, string> addresslist = new ConcurrentDictionary<string, string>();
        //static Dictionary<string, string> addresslist = new Dictionary<string, string>();
        static List<string> rafflemembers = new List<string>();
        static List<string> rafflewinners = new List<string>();
        static List<string> exwallets = new List<string>();
        static List<string> alltrans = new List<string>();
        static List<string> winnerAddresses = new List<string>();
        static List<string> raffleaddresses = new List<string>();
        //static List<string> distinctmembers = new List<string>();
        static List<Task> tasks = new List<Task>();
        static Dictionary<string, string> winnerdata = new Dictionary<string, string>();
        static StringBuilder sb = new StringBuilder();
        #endregion

        #region Calculating timestamp
        static DateTime burstepoch = new DateTime(2014, 08, 11, 2, 0, 0);
        static DateTime startdate = DateTime.Now.AddDays(startdayinterval);
        static DateTime enddate = DateTime.Now.AddDays(enddayinterval);
        static double starttimestamp = (startdate - burstepoch).TotalSeconds;
        static double endtimestamp = (enddate - burstepoch).TotalSeconds;
        #endregion

        static void Main(string[] args)
        {
            RunSetup();
            RunAsync().GetAwaiter().GetResult();
        }
        
        static void RunSetup()
        {
            try
            {
                Console.Clear();
                Console.WriteLine(Environment.NewLine + Environment.NewLine + "Running initial setup...this may take a moment." + Environment.NewLine + Environment.NewLine);
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
                Utilities.TestDBUpdate(db_connstring, "TRUNCATE TABLE exchange_wallets;", Utilities.queryParameters, true);
                Utilities.TestDBUpdate(db_connstring, "TRUNCATE TABLE all_weekly_trans;", Utilities.queryParameters, true);
                #endregion

                #region Capturing exchange wallets
                Console.WriteLine("Querying Burst blockchain for exchange wallets based on known exchange addresses (this may take a moment)" + Environment.NewLine);
                using (DataTable dt = Utilities.DataTableQuery(db_connstring, allexchangewallets, Utilities.queryParameters, true))
                {
                    
                    sb.Append("INSERT INTO exchange_wallets (wallet_id) VALUES ");
                    foreach (DataRow row in dt.Rows)
                    {
                        exwallets.Add(string.Format("({0})", row[0]));
                    }
                    sb.Append(string.Join(",", exwallets)).Append(";");

                    if (exwallets.Count > 0)
                    {
                        Console.WriteLine(exwallets.Count + " exchange wallets found. Storing exchange wallets for later..." + Environment.NewLine);
                        Utilities.TestDBUpdate(db_connstring, sb.ToString(), Utilities.queryParameters, true);
                    }
                    else
                    {
                        Console.WriteLine("No exchange wallets found...Pretty sure your database is blank or out of sync.  Resync and try again.");
                        return;
                    }
                    foreach (string exchange in addexchanges.Split(','))
                    {
                        Utilities.TestDBUpdate(db_connstring, exchange, Utilities.queryParameters, false);
                    }
                    sb.Clear();
                }
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
                        Utilities.TestDBUpdate(db_connstring, sb.ToString(), Utilities.queryParameters, true);
                    }
                    else
                    {
                        Console.WriteLine("No transactions found for the given timeframe...Pretty sure your database is blank or out of sync - or maybe you selected an invalid time range. Try again.");
                        return;
                    }
                    sb.Clear();
                }
                #endregion
            }
            catch (Exception ex)
            {
                Console.WriteLine("Error executing raffle setup: " + ex);
            }
        }

        static async Task RunAsync()
        {
            try
            {
                #region Make your selection
                do
                {
                    rafflemembers.Clear();
                    rafflewinners.Clear();
                    exwallets.Clear();
                    alltrans.Clear();
                    winnerAddresses.Clear();
                    raffleaddresses.Clear();
                    //distinctmembers.Clear();
                    winnerdata.Clear();
                    addresslist.Clear();
                    tasks.Clear();
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


                    #region Capturing qualifying transactions
                    Tuple<bool, string> burstaddress = null;
                    rafflemembers.Clear();
                    Console.WriteLine("Querying blockchain to find qualifying transactions for the raffle" + Environment.NewLine);
                    using (DataTable dt = Utilities.DataTableQuery(db_connstring, getqualifyingtransactions, Utilities.queryParameters, true))
                    {
                        foreach (DataRow row in dt.Rows)
                        {
                            rafflemembers.Add(row[0].ToString());
                        }
                    }
                    #endregion


                    #region Remove Pool wallets
                    Console.WriteLine("There are a total of " + rafflemembers.Count + " raffle entries." + Environment.NewLine);
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


                    #region Converting Database IDs to Account IDs
                    //foreach (string member in rafflemembers)
                    //{
                    //    if (!distinctmembers.Contains(member))
                    //    {
                    //        distinctmembers.Add(member);
                    //    }
                    //}
                    //foreach (string member in distinctmembers)
                    foreach (string member in rafflemembers.Distinct())
                    {
                        tasks.Add(Task.Run(async () =>
                        {
                            Tuple<bool, string> account = await Utilities.GetStringID(burstStringID, member);
                            if (account.Item1)
                            {
                                account = await Utilities.GetBurstAddress(burstAddressAPI, account.Item2);
                                if (account.Item1)
                                {
                                    addresslist.TryAdd(member, account.Item2);
                                }
                            }
                        }));
                    }
                    Task.WaitAll(tasks.ToArray());
                    int index = 0;
                    foreach (KeyValuePair<string, string> address in addresslist)
                    {
                        if (address.Key != address.Value)
                        {
                            do
                            {
                                index = rafflemembers.IndexOf(address.Key);
                                if (index >= 0) rafflemembers[index] = address.Value;
                            } while (index >= 0);
                        }
                    }
                    int reducecount = 0;
                    //Console.WriteLine("Here are your finalists...");
                    var distinctlist = from item in rafflemembers group item by item into grp select new { member = grp.Key, count = grp.Count() };
                    foreach (var member in distinctlist)
                    {
                        reducecount = 0;
                        if (member.count > maxraffleentries)
                        {
                            //Console.WriteLine("Removing " + (member.count - maxraffleentries) + " entries for " + member.member);
                            do
                            {
                                index = rafflemembers.IndexOf(member.member);
                                if (index >= 0)
                                {
                                    rafflemembers.RemoveAt(index);
                                    reducecount++;
                                }
                                else
                                {
                                    break;
                                }
                            } while (reducecount < (member.count - maxraffleentries));
                        }
                    }
                    sb.Clear();
                    distinctlist = from item in rafflemembers group item by item into grp select new { member = grp.Key, count = grp.Count() };
                    foreach (var member in distinctlist)
                    {
                        sb.Append(member.member).Append(",").Append(member.count).Append(Environment.NewLine);
                        //Console.WriteLine(member.member + " recieved " + member.count + " raffle entries.");
                    }
                    File.WriteAllText(Environment.CurrentDirectory + @"\" + contestantsfile, sb.ToString());
                    //Console.WriteLine(Environment.NewLine + Environment.NewLine + "Press any key to continue...");
                    //Console.ReadKey(true);
                    #endregion


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
                            if (rafflemembers.Contains(yourburstaddress))
                            {
                                Console.WriteLine(Environment.NewLine + Environment.NewLine + "Congratulations, you are eligible for the raffle and have been entered " + rafflemembers.FindAll(item => item == yourburstaddress).Count + " time(s)!" + Environment.NewLine + Environment.NewLine);
                                Console.WriteLine("Press any key to continue");
                                Console.ReadKey(true);
                                break;
                            }
                            else
                            {
                                Console.WriteLine(Environment.NewLine + Environment.NewLine + "Sorry, you are not eligible for the raffle...");
                                Console.WriteLine("Send Burst to friends, make purchases from the marketplace,");
                                Console.WriteLine("or donate to our marketing/development efforts to become eligible!" + Environment.NewLine + Environment.NewLine);
                                Console.WriteLine("Press any key to continue");
                                Console.ReadKey(true);
                                break;
                            }
                            #endregion
                            //break;
                        case "runraffle":
                            #region Retrieving raffle winners
                            Console.WriteLine("Retrieving " + numofwinners + " raffle winners at random" + Environment.NewLine);
                            
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


                            #region Retrieving BURST address for each winner
                            sb.Clear();
                            //Console.WriteLine("Retrieving BURST address for each raffle winner" + Environment.NewLine);
                            foreach (string winner in rafflewinners)
                            {
                                burstaddress = await Utilities.GetBurstAddress(burstAddressAPI, winner);
                                if (burstaddress.Item1)
                                {
                                    sb.Append(burstaddress.Item2).Append(Environment.NewLine);
                                    Console.WriteLine(burstaddress.Item2);
                                }
                                else
                                {
                                    sb.Append("Failed to get BURST address for account ID ").Append(winner).Append(Environment.NewLine);
                                    Console.WriteLine("Failed to get BURST address for account ID " + winner);
                                }
                            }
                            File.WriteAllText(Environment.CurrentDirectory + @"\" + winnersfile, sb.ToString());
                            Console.WriteLine(Environment.NewLine + "All winners and contestants have been written to your current directory as .csv files." + Environment.NewLine + "Press any key to continue...");
                            Console.ReadKey(true);
                            #endregion
                            break;
                    }
                    
                } while (keeprunning);
            }
            catch (Exception ex)
            {
                Console.WriteLine("Error: " + ex);
            }
        }
    }
}
