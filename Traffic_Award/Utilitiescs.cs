using MySql.Data.MySqlClient;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Data;
using System.Linq;
using System.Net.Http;
using System.Threading.Tasks;

namespace Traffic_Award
{
    class Utilities
    {
        public static DataTable DataTableQuery(string query, Dictionary<string, object> parameters, bool showerrors)
        {
            using (DataTable dt = new DataTable())
            {
                try
                {
                    using (var mariadbconnection = new MySqlConnection(Program.db_connstring))
                    {
                        mariadbconnection.Open();
                        using (MySqlDataAdapter adapter = new MySqlDataAdapter(query, mariadbconnection))
                        {
                            adapter.SelectCommand.CommandTimeout = 300;
                            adapter.SelectCommand.CommandType = CommandType.Text;
                            foreach (KeyValuePair<string, object> item in parameters)
                            {
                                adapter.SelectCommand.Parameters.AddWithValue(item.Key, item.Value);
                            }
                            adapter.Fill(dt);
                        }
                    }
                }
                catch (Exception ex)
                {
                    if (showerrors)
                    {
                        Console.WriteLine("Error in BlockChainQuery - " + ex);
                        Console.ReadKey(true);
                    }
                }
                finally
                {
                    Program.queryParameters.Clear();
                }
                return dt;
            }
        }

        public static List<string> GetRecipients(string recipient, List<string> allrecipients)
        {
            List<string> recipients = new List<string>();
            Program.queryParameters.Add("@starttime", Program.starttimestamp);
            Program.queryParameters.Add("@endtime", Program.endtimestamp);
            Program.queryParameters.Add("@recipient", recipient);
            try
            {
                using (DataTable dt = DataTableQuery(Program.getrecipients, Program.queryParameters, true))
                {
                    foreach (DataRow row in dt.Rows)
                    {

                        if (Program.exchangewallets.Contains(row[0].ToString()) || Program.poolwallets.Contains(row[0].ToString()))
                        {
                            allrecipients.Add(row[0].ToString());
                            continue;
                        }
                        else if (!allrecipients.Contains(row[0].ToString()))
                        {
                            allrecipients.Add(row[0].ToString());
                            GetRecipients(row[0].ToString(), allrecipients);
                        }
                        else
                        {
                            allrecipients.Add(row[0].ToString());
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine("Error getting recipients for " + recipient + ": " + ex);
            }
            finally
            {
                Program.queryParameters.Clear();
            }
            return allrecipients;
        }

        public static void AddRaffleEntries(int reward)
        {
            List<string> allrecipients = new List<string>();
            for (int n = 0; n < Program.excheck.Count; n++)
            {
                ExCheck recipient = Program.excheck[n];
                int i = 0;
                allrecipients = Program.excheck.FirstOrDefault(y => y.waschecked == true && y.recipientid == recipient.recipientid)?.receiverlist;
                if (allrecipients == null)
                {
                    allrecipients = Utilities.GetRecipients(recipient.recipientid, new List<string>());
                }
                recipient.receiverlist = allrecipients;
                foreach (string exchange in allrecipients)
                {
                    if (Program.exchangewallets.Contains(exchange))
                    {
                        if (Program.excheck.Select(x => x).Where(y => y.recipientid == recipient.recipientid && y.penalize == true && y.penalid == i).Count() > 0)
                        {
                            recipient.penalize = false;
                            recipient.penalid = i;
                        }
                        else
                        {
                            recipient.penalize = true;
                            recipient.penalid = i;
                            break;
                        }
                    }
                    i++;
                }
                recipient.waschecked = true;
            }
            int awards = 0;
            int reductions = 0;
            int loopcount = 0;
            foreach (ExCheck recipient in Program.excheck)
            {
                loopcount = 0;
                awards = 0;
                reductions = 0;
                if (!recipient.penalize)
                {
                    loopcount = reward;
                    awards += reward;
                }
                else
                {
                    reductions += reward;
                }
                Program.queryParameters.Add("@wallet_id", recipient.recipientid);
                if (Program.exchangeaward == reward)
                {
                    Program.queryParameters.Add("@exchange_entries", reward);
                    Program.queryParameters.Add("@trans_entries", 0);
                }
                else
                {
                    Program.queryParameters.Add("@exchange_entries", 0);
                    Program.queryParameters.Add("@trans_entries", reward);
                }
                Program.queryParameters.Add("@reductions", reductions);
                TestDBUpdate(Program.summarytableupsert, Program.queryParameters, true);
                for (int i = 0; i < loopcount; i++)
                {
                    Program.rafflemembers.Add(recipient.recipientid);
                }
            }

        }

        public static void TestDBUpdate(string query, Dictionary<string, object> parameters, bool showerrors)
        {
            try
            {
                using (var mariadbconnection = new MySqlConnection(Program.db_connstring))
                {
                    mariadbconnection.Open();
                    using (MySqlCommand cmd = new MySqlCommand(query, mariadbconnection))
                    {
                        cmd.CommandType = CommandType.Text;
                        cmd.CommandTimeout = 300;
                        foreach (KeyValuePair<string, object> item in parameters)
                        {
                            cmd.Parameters.AddWithValue(item.Key, item.Value);
                        }
                        cmd.ExecuteNonQuery();
                    }
                }
            }
            catch (Exception ex)
            {
                if (showerrors)
                {
                    Console.WriteLine("Error in TestDBUpdate - " + ex);
                    Console.ReadKey(true);
                }
            }
            finally
            {
                Program.queryParameters.Clear();
            }
        }

        public static string GetWinnerData(string query, Dictionary<string, object> parameters, bool showerrors)
        {
            string retval = null;
            try
            {
                using (var mariadbconnection = new MySqlConnection(Program.db_connstring))
                {
                    mariadbconnection.Open();
                    using (MySqlCommand cmd = new MySqlCommand(query, mariadbconnection))
                    {
                        cmd.CommandType = CommandType.Text;
                        cmd.CommandTimeout = 300;
                        foreach (KeyValuePair<string, object> item in parameters)
                        {
                            cmd.Parameters.AddWithValue(item.Key, item.Value);
                        }
                        object test = cmd.ExecuteScalar();
                        if (test != null && !(string.IsNullOrEmpty(test.ToString())))
                        {
                            retval = test.ToString();
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                if (showerrors)
                {
                    Console.WriteLine("Error in TestDBUpdate - " + ex);
                    Console.ReadKey(true);
                }
            }
            finally
            {
                Program.queryParameters.Clear();
            }
            return retval;
        }

        public static async Task<Tuple<bool, string>> GetBurstAccountID(string api, string address)
        {
            string retval = null;
            bool success = true;
            try
            {
                HttpRequestMessage requestMessage = new HttpRequestMessage(HttpMethod.Get, api + address);
                using (HttpClient httpClient = new HttpClient())
                {
                    HttpResponseMessage response = await httpClient.SendAsync(requestMessage);
                    retval = ((AccountRS)JsonConvert.DeserializeObject(await response.Content.ReadAsStringAsync(), (typeof(AccountRS)))).account;
                    //Console.WriteLine(retval);
                }
            }
            catch (Exception ex)
            {
                success = false;
                retval = "" + ex;
            }
            return Tuple.Create(success, retval);

        }

        public static async Task<Tuple<bool, string>> GetTransactionIds(string api, string burstaddress)
        {
            string retval = null;
            bool success = true;
            //string strresponse = null;
            List<string> transids = new List<string>();
            try
            {
                HttpRequestMessage requestMessage = new HttpRequestMessage(HttpMethod.Get, api + burstaddress);
                using (HttpClient httpClient = new HttpClient())
                {
                    HttpResponseMessage response = await httpClient.SendAsync(requestMessage);
                    //strresponse = await response.Content.ReadAsStringAsync();
                    //Console.WriteLine(strresponse);
                    transids = ((TransactionIds)JsonConvert.DeserializeObject(await response.Content.ReadAsStringAsync(), (typeof(TransactionIds)))).transactionIds;
                }
            }
            catch (Exception ex)
            {
                success = false;
                retval = "" + ex;
            }
            foreach (string id in transids)
            {
                retval = id;
                break;
            }
            return Tuple.Create(success, retval);
        }

        public static async Task<Tuple<bool, string>> GetBurstAddress(string api, string accountid)
        {
            string retval = null;
            bool success = true;
            try
            {
                HttpRequestMessage requestMessage = new HttpRequestMessage(HttpMethod.Get, api + accountid);
                using (HttpClient httpClient = new HttpClient())
                {
                    HttpResponseMessage response = await httpClient.SendAsync(requestMessage);
                    retval = ((AccountRS)JsonConvert.DeserializeObject(await response.Content.ReadAsStringAsync(), (typeof(AccountRS)))).accountRS;
                    //Console.WriteLine(retval);
                }
            }
            catch (Exception ex)
            {
                success = false;
                retval = "" + ex;
            }
            return Tuple.Create(success, retval);
        }

        public static async Task<Tuple<bool, string>> GetStringID(string api, string accountid)
        {
            string retval = null;
            bool success = true;
            try
            {
                HttpRequestMessage requestMessage = new HttpRequestMessage(HttpMethod.Get, api + accountid);
                using (HttpClient httpClient = new HttpClient())
                {
                    HttpResponseMessage response = await httpClient.SendAsync(requestMessage);
                    retval = ((StringID)JsonConvert.DeserializeObject(await response.Content.ReadAsStringAsync(), (typeof(StringID)))).stringId;
                    //Console.WriteLine(retval);
                }
            }
            catch (Exception ex)
            {
                success = false;
                retval = "" + ex;
            }
            return Tuple.Create(success, retval);
        }

        public static string SqlEscapeString(object value)
        {
            if (string.IsNullOrEmpty(value.ToString()))
            {
                return "NULL";
            }
            else
            {
                return MySqlHelper.EscapeString(value.ToString());
            }
        }
    }
}
