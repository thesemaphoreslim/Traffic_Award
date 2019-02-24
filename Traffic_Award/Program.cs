using Microsoft.Extensions.Configuration;
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Data;
using System.IO;
using System.Text;
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

    public class ExCheck
    {
        public string recipientid { get; set; }
        public bool penalize { get; set; }
        public long penalid { get; set; }
        public bool waschecked { get; set; }
        public List<string> receiverlist { get; set; }
    }

    public class Summary
    {
        public string walletid { get; set; }
        public List<DetailSummary> details { get; set; }
    }

    public class DetailSummary
    {
        public int exchangentries { get; set; }
        public int transentries { get; set; }
        public int reductions { get; set; }
    }

    class Program
    {
        #region Capturing app configs from json
        public static IConfigurationBuilder builder = new ConfigurationBuilder().SetBasePath(Directory.GetCurrentDirectory()).AddJsonFile("appsettings.json");
        public static IConfigurationRoot configuration = builder.Build();
        public static string allexchangewallets = configuration["allexchangewallets"];
        public static string db_server = configuration["db_server"];
        public static string db_port = configuration["db_port"];
        public static string db_uid = configuration["db_uid"];
        public static string db_pwd = configuration["db_pwd"];
        public static string db_name = configuration["db_name"];
        public static string db_connstring = "Server=" + db_server + ";Port=" + db_port + ";Uid=" + db_uid + ";Pwd=" + db_pwd + ";Database=" + db_name;
        public static string poloid = configuration["poloid"];
        public static string bittid = configuration["bittid"];
        public static string brsid = configuration["brsid"];
        public static string bmfid = configuration["bmfid"];
        public static string bmfpid = configuration["bmfpid"];
        public static string mortid = configuration["mortid"];
        public static string devid = configuration["devid"];
        public static string liveid = configuration["liveid"];
        static string getalltransactions = configuration["getalltransactions"];
        static string getqualifyingtransactions = configuration["getqualifyingtransactions"];
        static string getwinnerdata = configuration["getwinnerdata"];
        static string getpoolwallets = configuration["getpoolwallets"];
        static string geteligibility = configuration["geteligibility"];
        static string getexchangetrans = configuration["getexchangetrans"];
        static string getexchangetransamt = configuration["getexchangetransamt"];
        public static string getrecipients = configuration["getrecipients"];
        static string checkfortable = configuration["checkfortable"];
        static string createexchangetable = configuration["createexchangetable"];
        static string createtranstable = configuration["createtranstable"];
        static string createsummarytable = configuration["createsummarytable"];
        static string getsummarytable = configuration["getsummarydata"];
        static string getnewbmfmembers = configuration["getnewbmfmembers"];
        static string getbmfdonations = configuration["getbmfdonations"];
        public static string summarytableupsert = configuration["summarytableupsert"];
        public static double startdayinterval = double.Parse(configuration["startdayinterval"]);
        public static double enddayinterval = double.Parse(configuration["enddayinterval"]);
        static double exchangemin = double.Parse(configuration["exchangemin"]);
        static double burstamount = double.Parse(configuration["burstamount"]);
        static double feeamount = double.Parse(configuration["feeamount"]);
        static double minfeeamount = double.Parse(configuration["minfeeamount"]);
        static double numofwinners = double.Parse(configuration["numofwinners"]);
        static string burstTransactionsAPI = configuration["BurstTransactionsAPI"];
        static string burstAddressAPI = configuration["BurstAddressAPI"];
        static string burstStringID = configuration["BurstStringID"];
        static bool removePoolAddresses = bool.Parse(configuration["removepooladdresses"]);
        static int maxraffleentries = int.Parse(configuration["maxraffleentries"]);
        static string contestantsfile = configuration["contestantsfile"];
        static string winnersfile = configuration["winnersfile"];
        static bool keeprunning = true;
        static ConcurrentDictionary<string, string> addresslist = new ConcurrentDictionary<string, string>();
        public static List<string> rafflemembers = new List<string>();
        static List<string> rafflewinners = new List<string>();
        static List<string> alltrans = new List<string>();
        static List<Task> tasks = new List<Task>();
        public static List<string> poolwallets = new List<string>();
        static StringBuilder sb = new StringBuilder();
        static Tuple<bool, string> burstaddress = Tuple.Create<bool, string>(false, null);
        public static Dictionary<string, object> queryParameters = new Dictionary<string, object>();
        public static List<ExCheck> excheck = new List<ExCheck>();
        public static List<string> exchangewallets = new List<string>{ poloid, bittid, liveid };
        public static int transactionaward = int.Parse(configuration["transactionaward"]);
        public static int exchangeaward = int.Parse(configuration["exchangeaward"]);
        public static int bmfaward = int.Parse(configuration["bmfaward"]);
        public static List<Summary> summary = new List<Summary>();
        static List<string> examt = new List<string>();
        static string displaystr = null;
        #endregion


        #region Calculating timestamp
        public static DateTime burstepoch = new DateTime(2014, 08, 11, 2, 0, 0, DateTimeKind.Utc);
        public static DateTime startdate = DateTime.Now.AddDays(startdayinterval);
        public static DateTime enddate = DateTime.Now.AddDays(enddayinterval);
        public static string startdatetime = configuration["startdatetime"];
        public static string enddatetime = configuration["enddatetime"];
        public static double starttimestamp = (startdate - burstepoch).TotalSeconds;
        public static double endtimestamp = (enddate - burstepoch).TotalSeconds;
        #endregion

        static void Main(string[] args)
        {
            RunAsync().GetAwaiter().GetResult();
        }
        
        static void RunSetup()
        {
            try
            {
                Console.Clear();
                if (DateTime.TryParse(startdatetime, out startdate))
                {
                    starttimestamp = (startdate - burstepoch).TotalSeconds;
                    Console.WriteLine("Using static start date/time of " + startdatetime);
                    Console.WriteLine("Press any key to continue");
                    Console.ReadKey(true);
                }
                if (DateTime.TryParse(enddatetime, out enddate))
                {
                    endtimestamp = (enddate - burstepoch).TotalSeconds;
                    Console.WriteLine("Using static end date/time of " + enddatetime);
                    Console.WriteLine("Press any key to continue");
                    Console.ReadKey(true);
                }


                #region Check for schema
                queryParameters.Add("@table_name", "exchange_wallets");
                queryParameters.Add("@db_name", db_name);
                if (string.IsNullOrEmpty(Utilities.GetWinnerData(checkfortable, queryParameters, true)))
                {
                    Utilities.DBUpdate(createexchangetable, queryParameters, true);
                }
                queryParameters.Add("@table_name", "all_weekly_trans");
                queryParameters.Add("@db_name", db_name);
                if (string.IsNullOrEmpty(Utilities.GetWinnerData(checkfortable, queryParameters, true)))
                {
                    Utilities.DBUpdate(createtranstable, queryParameters, true);
                }
                queryParameters.Add("@table_name", "raffle_summary");
                queryParameters.Add("@db_name", db_name);
                if (string.IsNullOrEmpty(Utilities.GetWinnerData(checkfortable, queryParameters, true)))
                {
                    Utilities.DBUpdate(createsummarytable, queryParameters, true);
                }
                #endregion


                #region Clearing testdb tables
                Utilities.DBUpdate("TRUNCATE TABLE exchange_wallets;", queryParameters, true);
                Utilities.DBUpdate("TRUNCATE TABLE all_weekly_trans;", queryParameters, true);
                Utilities.DBUpdate("TRUNCATE TABLE raffle_summary;", queryParameters, true);
                #endregion


                #region Capturing exchange wallets
                queryParameters.Add("@poloid", poloid);
                queryParameters.Add("@bittid", bittid);
                queryParameters.Add("@brsid", brsid);
                queryParameters.Add("@mortid", mortid);
                queryParameters.Add("@liveid", liveid);
                Utilities.DBUpdate(allexchangewallets, queryParameters, false);
                #endregion


                #region Capturing Transactions
                queryParameters.Add("@starttime", starttimestamp);
                queryParameters.Add("@endtime", endtimestamp);
                queryParameters.Add("@burstamount", burstamount);
                queryParameters.Add("@feeamount", feeamount);
                queryParameters.Add("@minfeeamount", minfeeamount);
                using (DataTable dt = Utilities.DataTableQuery(getalltransactions, queryParameters, true))
                {
                    sb.Append("INSERT INTO all_weekly_trans (amount, fee, recipient_id, sender_id, timestamp, trans_id) VALUES ");
                    foreach (DataRow row in dt.Rows)
                    {
                        alltrans.Add(string.Format("({0},{1},{2},{3},{4},{5})", Utilities.SqlEscapeString(row[0]), Utilities.SqlEscapeString(row[1]), Utilities.SqlEscapeString(row[2]), Utilities.SqlEscapeString(row[3]), Utilities.SqlEscapeString(row[4]), Utilities.SqlEscapeString(row[5])));
                    }
                    sb.Append(string.Join(",", alltrans)).Append(";");
                    if (alltrans.Count > 0)
                    {
                        Utilities.DBUpdate(sb.ToString(), queryParameters, true);
                    }
                    else
                    {
                        Console.WriteLine("No transactions found for the given timeframe...Pretty sure your database is blank or out of sync - or maybe you selected an invalid time range. Try again.");
                        Console.ReadKey(true);
                        return;
                    }
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
                    alltrans.Clear();
                    addresslist.Clear();
                    tasks.Clear();
                    poolwallets.Clear();
                    excheck.Clear();
                    sb.Clear();
                    burstaddress = Tuple.Create<bool, string>(false, null);
                    displaystr = null;
                    string mySelection = null;
                    do
                    {
                        Console.Clear();
                        Console.WriteLine("********************************************");
                        Console.WriteLine("*   Burst Marketing Fund - Traffic Award   *");
                        Console.WriteLine("********************************************" + Environment.NewLine + Environment.NewLine);
                        Console.WriteLine("Make your selection:" + Environment.NewLine + Environment.NewLine);
                        Console.WriteLine("A) Execute Raffle" + Environment.NewLine + Environment.NewLine);
                        Console.WriteLine("B) Check Address Eligibility" + Environment.NewLine + Environment.NewLine);
                        Console.WriteLine("C) Export Leaderboard" + Environment.NewLine + Environment.NewLine);
                        Console.WriteLine("X) Exit" + Environment.NewLine + Environment.NewLine);
                        switch (Console.ReadKey(true).Key)
                        {
                            case ConsoleKey.A:
                                Console.Clear();
                                Console.WriteLine("Executing Raffle..." + Environment.NewLine);
                                displaystr = "Executing Raffle...";
                                mySelection = "runraffle";
                                break;
                            case ConsoleKey.B:
                                Console.Clear();
                                Console.WriteLine("Checking Eligibility..." + Environment.NewLine);
                                displaystr = "Checking Eligibility...";
                                mySelection = "checkeligibility";
                                break;
                            case ConsoleKey.C:
                                Console.Clear();
                                Console.WriteLine("Exporting Leaderboard as csv" + Environment.NewLine);
                                displaystr = "Exporting Leaderboard as csv...";
                                mySelection = "leaderboard";
                                break;
                            case ConsoleKey.X:
                                Console.WriteLine("Exiting..." + Environment.NewLine);
                                return;
                        }
                    } while (string.IsNullOrEmpty(mySelection));
                    RunSetup();
                    rafflemembers.Clear();
                    #endregion


                    Console.WriteLine("If you hang here, there may be a problem with your database connection.");
                    Console.WriteLine("Close any open connections or restart the application.");


                    #region Collect Pool wallets
                    if (removePoolAddresses)
                    {
                        using (DataTable dt = Utilities.DataTableQuery(getpoolwallets, queryParameters, true))
                        {
                            foreach (DataRow row in dt.Rows)
                            {
                                poolwallets.Add(row[0].ToString());
                            }
                        }
                    }
                    Console.Clear();
                    Console.WriteLine(displaystr + Environment.NewLine);
                    #endregion


                    #region Capturing qualifying BMF donations
                    queryParameters.Add("@starttime", starttimestamp);
                    queryParameters.Add("@endtime", endtimestamp);
                    queryParameters.Add("@bmfid", bmfid);
                    queryParameters.Add("@bmfaward", bmfaward);
                    queryParameters.Add("@burstamount", burstamount);
                    queryParameters.Add("@bmfpid", bmfpid);
                    queryParameters.Add("@devid", devid);
                    Utilities.DBUpdate(getbmfdonations, queryParameters, true);
                    using (DataTable dt = Utilities.DataTableQuery("SELECT * from raffle_summary where bmf_entries>0;", queryParameters, true))
                    {
                        foreach (DataRow row in dt.Rows)
                        {
                            for (int i = 0; i < bmfaward; i++)
                            {
                                rafflemembers.Add(row[0].ToString());
                            }
                        }
                    }
                    #endregion


                    #region Capturing qualifying BMF pool members
                    //queryParameters.Add("@starttime", starttimestamp);
                    //queryParameters.Add("@endtime", endtimestamp);
                    //queryParameters.Add("@bmfpid", bmfpid);
                    //queryParameters.Add("@bmfaward", bmfaward);
                    //Utilities.DBUpdate(getnewbmfmembers, queryParameters, true);
                    //using (DataTable dt = Utilities.DataTableQuery("SELECT * FROM raffle_summary WHERE bmf_entries>0;", queryParameters, true))
                    //{
                    //    foreach (DataRow row in dt.Rows)
                    //    {
                    //        for (int i = 0; i < bmfaward; i++)
                    //        {
                    //            rafflemembers.Add(row[0].ToString());
                    //        }
                    //    }
                    //}
                    #endregion


                    #region Capturing qualifying transactions
                    excheck.Clear();
                    queryParameters.Add("@poloid", poloid);
                    queryParameters.Add("@bittid", bittid);
                    queryParameters.Add("@brsid", brsid);
                    queryParameters.Add("@mortid", mortid);
                    queryParameters.Add("@bmfid", bmfid);
                    queryParameters.Add("@bmfpid", bmfpid);
                    queryParameters.Add("@devid", devid);
                    queryParameters.Add("@liveid", liveid);
                    using (DataTable dt = Utilities.DataTableQuery(getqualifyingtransactions, queryParameters, true))
                    {
                        foreach (DataRow row in dt.Rows)
                        {
                            excheck.Add(new ExCheck { recipientid = row[0].ToString(), penalize = false, penalid = -1, receiverlist = new List<string>(), waschecked = false });
                        }
                    }
                    Console.WriteLine("Checking all " + excheck.Count + " qualifying transactions for sell activity. This may take a moment." + Environment.NewLine);
                    Utilities.AddRaffleEntries(transactionaward);
                    #endregion


                    #region Capture qualifying exchange purchases
                    excheck.Clear();
                    queryParameters.Add("@starttime", starttimestamp);
                    queryParameters.Add("@endtime", endtimestamp);
                    queryParameters.Add("@exchangemin", exchangemin);
                    queryParameters.Add("@poloid", poloid);
                    queryParameters.Add("@bittid", bittid);
                    queryParameters.Add("@devid", devid);
                    queryParameters.Add("@liveid", liveid);
                    using (DataTable dt = Utilities.DataTableQuery(getexchangetransamt, queryParameters, true))
                    {
                        foreach (DataRow row in dt.Rows)
                        {
                            examt.Add(row[0].ToString());
                        }
                    }
                    queryParameters.Add("@starttime", starttimestamp);
                    queryParameters.Add("@endtime", endtimestamp);
                    queryParameters.Add("@exchangemin", exchangemin);
                    queryParameters.Add("@poloid", poloid);
                    queryParameters.Add("@bittid", bittid);
                    queryParameters.Add("@devid", devid);
                    queryParameters.Add("@liveid", liveid);
                    using (DataTable dt = Utilities.DataTableQuery(getexchangetrans, queryParameters, true))
                    {
                        foreach (DataRow row in dt.Rows)
                        {
                            if (examt.Contains(row[0].ToString()))
                            {
                                excheck.Add(new ExCheck { recipientid = row[0].ToString(), penalize = false, penalid = -1 });
                            }
                        }
                    }
                    Console.WriteLine("Checking all " + excheck.Count + " exchange purchases for sell activity. This may take a moment." + Environment.NewLine);
                    Utilities.AddRaffleEntries(exchangeaward);
                    #endregion


                    Console.WriteLine("There are a total of " + rafflemembers.Count + " raffle entries." + Environment.NewLine);


                    #region Converting Database IDs to Account IDs
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
                    tasks.Clear();
                    int index = 0;
                    foreach (KeyValuePair<string, string> address in addresslist)
                    {
                        queryParameters.Add("@wallet_address", address.Value);
                        queryParameters.Add("@wallet_id", address.Key);
                        Utilities.DBUpdate("UPDATE raffle_summary SET wallet_address=@wallet_address WHERE wallet_id=@wallet_id;", Program.queryParameters, true);
                        do
                        {
                            index = rafflemembers.IndexOf(address.Key);
                            if (index >= 0)
                            {
                                rafflemembers[index] = address.Value;
                            }
                        } while (index >= 0);
                    }
                    addresslist.Clear();
                    using (DataTable dt = Utilities.DataTableQuery("SELECT wallet_id from raffle_summary where wallet_address IS NULL", queryParameters, true))
                    {
                        foreach (DataRow row in dt.Rows)
                        {
                            tasks.Add(Task.Run(async () =>
                            {
                                Tuple<bool, string> account = await Utilities.GetStringID(burstStringID, row[0].ToString());
                                if (account.Item1)
                                {
                                    account = await Utilities.GetBurstAddress(burstAddressAPI, account.Item2);
                                    if (account.Item1)
                                    {
                                        addresslist.TryAdd(row[0].ToString(), account.Item2);
                                    }
                                }
                            }));
                        }
                    }
                    Task.WaitAll(tasks.ToArray());
                    tasks.Clear();
                    foreach (KeyValuePair<string, string> address in addresslist)
                    {
                        queryParameters.Add("@wallet_address", address.Value);
                        queryParameters.Add("@wallet_id", address.Key);
                        Utilities.DBUpdate("UPDATE raffle_summary SET wallet_address=@wallet_address WHERE wallet_id=@wallet_id;", Program.queryParameters, true);
                    }
                    int reducecount = -1;
                    var distinctlist = from item in rafflemembers group item by item into grp select new { member = grp.Key, count = grp.Count() };
                    foreach (var member in distinctlist)
                    {
                        if (member.count > maxraffleentries)
                        {
                            queryParameters.Add("@total_entries", maxraffleentries);
                            if (reducecount < 0)
                            {
                                Console.WriteLine("Reducing entries to meet the maximum allowed (" + maxraffleentries + ")" + Environment.NewLine);
                            }
                            reducecount = 0;
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
                            Console.WriteLine("Reduced " + member.member + " entries from " + member.count + " to " + maxraffleentries);
                        }
                        else
                        {
                            queryParameters.Add("@total_entries", member.count);
                        }
                        queryParameters.Add("@wallet_address", member.member);
                        Utilities.DBUpdate("UPDATE raffle_summary SET total_entries=@total_entries WHERE wallet_address=@wallet_address;", Program.queryParameters, true);
                    }
                    if (reducecount >= 0)
                    {
                        Console.WriteLine(Environment.NewLine + "There are now a total of " + rafflemembers.Count + " raffle entries." + Environment.NewLine);
                    }
                    sb.Clear();
                    using (DataTable dt = Utilities.DataTableQuery("SELECT wallet_address, total_entries, bmf_entries, exchange_entries, trans_entries, reductions FROM raffle_summary order by total_entries desc;", queryParameters, true))
                    //using (DataTable dt = Utilities.DataTableQuery("SELECT wallet_address, total_entries, exchange_entries, trans_entries, reductions FROM raffle_summary order by total_entries desc, wallet_address;", queryParameters, true))
                    {
                        sb.Append("Burst Address,Total Entries,BMF Entries,Exchange Entries,Transaction Entries,Reductions").Append(Environment.NewLine);
                        //sb.Append("Burst Address,Total Entries,Exchange Entries,Transaction Entries,Reductions").Append(Environment.NewLine);
                        foreach (DataRow row in dt.Rows)
                        {
                            sb.Append(row[0].ToString()).Append(",").Append(row[1].ToString()).Append(",").Append(row[2].ToString()).Append(",").Append(row[3].ToString()).Append(",").Append(row[4].ToString()).Append(",").Append(row[5].ToString()).Append(Environment.NewLine);
                            //sb.Append(row[0].ToString()).Append(",").Append(row[1].ToString()).Append(",").Append(row[2].ToString()).Append(",").Append(row[3].ToString()).Append(",").Append(row[4].ToString()).Append(Environment.NewLine);
                        }
                    }
                    try
                    {
                        File.WriteAllText(Environment.CurrentDirectory + @"\" + contestantsfile, sb.ToString());
                    }
                    catch (Exception ex)
                    {
                        Console.WriteLine("Unable to write contestants file. You probably have it open...");
                    }
                    #endregion


                    switch (mySelection)
                    {
                        case "leaderboard":
                            Console.WriteLine("Leaderboard exported as 'contestant.csv'" + Environment.NewLine);
                            Console.WriteLine("Press any key to continue");
                            Console.ReadKey(true);
                            break;
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
                                Console.WriteLine(Environment.NewLine + Environment.NewLine + "No raffle entries found for this address." + Environment.NewLine);
                                Console.WriteLine("Press any key to continue");
                                Console.ReadKey(true);
                                break;
                            }
                            #endregion
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
                            try
                            {
                                File.WriteAllText(Environment.CurrentDirectory + @"\" + winnersfile, sb.ToString());
                            }
                            catch (Exception ex)
                            {
                                Console.WriteLine("Unable to write contestants file. You probably have it open...");
                            }
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
                Console.ReadKey(true);
            }
        }
    }
}
