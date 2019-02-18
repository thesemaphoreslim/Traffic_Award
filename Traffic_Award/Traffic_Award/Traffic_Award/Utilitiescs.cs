using MySql.Data.MySqlClient;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Data;
using System.Net.Http;
using System.Threading.Tasks;

namespace Traffic_Award
{
    class Utilities
    {
        public static Dictionary<string, object> queryParameters = new Dictionary<string, object>();

        public static DataTable DataTableQuery(string connectionstring, string query, Dictionary<string, object> parameters, bool showerrors)
        {
            using (DataTable dt = new DataTable())
            {
                try
                {
                    using (var mariadbconnection = new MySqlConnection(connectionstring))
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
                    if (showerrors) Console.WriteLine("Error in BlockChainQuery - " + ex);
                }
                finally
                {
                    Utilities.queryParameters.Clear();
                }
                return dt;
            }
        }

        public static DataTable TestDBUpdate(string connectionstring, string query, Dictionary<string, object> parameters, bool showerrors)
        {
            using (DataTable dt = new DataTable())
            {
                try
                {
                    using (var mariadbconnection = new MySqlConnection(connectionstring))
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
                    if (showerrors) Console.WriteLine("Error in TestDBUpdate - " + ex);
                }
                finally
                {
                    Utilities.queryParameters.Clear();
                }
                return dt;
            }
        }

        public static string GetWinnerData(string connectionstring, string query, Dictionary<string, object> parameters, bool showerrors)
        {
            string retval = null;
            try
            {
                using (var mariadbconnection = new MySqlConnection(connectionstring))
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
                if (showerrors) Console.WriteLine("Error in TestDBUpdate - " + ex);
            }
            finally
            {
                Utilities.queryParameters.Clear();
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
