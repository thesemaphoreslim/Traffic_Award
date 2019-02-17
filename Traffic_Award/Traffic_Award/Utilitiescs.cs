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
            return retval;
        }

        public static async Task<Tuple<bool, string>> GetBurstAddress(string api, string transid)
        {
            string retval = null;
            bool success = true;
            //string strresponse = null;
            try
            {
                HttpRequestMessage requestMessage = new HttpRequestMessage(HttpMethod.Get, api + transid);
                using (HttpClient httpClient = new HttpClient())
                {
                    HttpResponseMessage response = await httpClient.SendAsync(requestMessage);
                    //strresponse = await response.Content.ReadAsStringAsync();
                    //Console.WriteLine(strresponse);
                    retval = ((Transaction)JsonConvert.DeserializeObject(await response.Content.ReadAsStringAsync(), (typeof(Transaction)))).data.sender;
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
