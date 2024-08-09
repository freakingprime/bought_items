using BoughtItems.MVVMBase;
using BoughtItems.UI_Merge;
using Dapper;
using Microsoft.Data.Sqlite;
using Microsoft.VisualBasic;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using System.Windows.Controls;
using System.Windows.Documents;
using System.Windows.Media.Animation;

namespace BoughtItems.UI_Lazada
{
    public class LazadaVm : ViewModelBase
    {
        private static readonly log4net.ILog log = log4net.LogManager.GetLogger(System.Reflection.MethodBase.GetCurrentMethod().DeclaringType.Name);
        private static readonly LogController oldLog = LogController.Instance;
        public LazadaVm()
        {

        }
        public void ButtonLazada()
        {

        }

        private int id = 1000;
        private Regex regexField = new Regex(@"""fields"".+?},", RegexOptions.Singleline | RegexOptions.IgnoreCase);
        private Regex regexShopGroupKey = new Regex(@"""shopGroupKey""[^""]+""([^""]+)", RegexOptions.Singleline | RegexOptions.IgnoreCase);
        private Regex regexTradeOrderId = new Regex(@"ORDERLOGIC_(\d+)", RegexOptions.IgnoreCase);
        private const string ORDER_DATA_PATH = @"D:\DOWNLOADED\lazada\order_data.txt";
        private const string ORDER_URL_PATH = @"D:\DOWNLOADED\lazada\order_url.txt";

        private Dictionary<string, string> ReadFromFile(string path)
        {
            Dictionary<string, string> ret = new Dictionary<string, string>();
            string[] lines = File.ReadAllLines(path);
            foreach (string line in lines)
            {
                string[] split = line.Split("|", StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries);
                if (split.Length == 2)
                {
                    ret[split[0]] = split[1];
                }
            }
            return ret;
        }

        private void WriteKeyToDB(Dictionary<string, string> dict)
        {
            int orderRow = 0;
            using (var connection = new SqliteConnection("Data Source=\"" + MergeVm.GetDatabasePath() + "\""))
            {
                connection.Open();
                var transaction = connection.BeginTransaction();
                foreach (var item in dict)
                {
                    orderRow += connection.Execute(@"INSERT OR IGNORE INTO lazada_json (ShopGroupKey,TradeOrderID) VALUES (@ShopGroupKey,@TradeOrderID)", new { ShopGroupKey = item.Key, TradeOrderID = item.Value });
                }
                transaction.Commit();
            }
            oldLog.Debug("Write " + orderRow + " rows to DB from " + dict.Count + " pairs");
        }

        private void WriteJSONToDB(string groupKey, string tradeOrderId, string json)
        {
            using (var connection = new SqliteConnection("Data Source=\"" + MergeVm.GetDatabasePath() + "\""))
            {
                connection.Open();
                connection.Execute(@"INSERT OR REPLACE INTO lazada_json (ShopGroupKey,TradeOrderID,JsonData) VALUES (@groupKey,@tradeOrderId,@json)", new { groupKey, tradeOrderId, json });
            }
        }

        public void ParseOrderList(string json)
        {
            Dictionary<string, string> dictOrder = new Dictionary<string, string>();
            string unicode = Regex.Unescape(json);
            MatchCollection listGroupKey = regexShopGroupKey.Matches(unicode);
            foreach (Match matchGroup in listGroupKey)
            {
                string groupKey = matchGroup.Groups[1].Value;
                Match matchTradeOrderId = regexTradeOrderId.Match(groupKey);
                if (matchTradeOrderId.Success)
                {
                    string tradeOrder = matchTradeOrderId.Groups[1].Value;
                    //oldLog.Debug("Group key: " + groupKey + " Trade order: " + tradeOrder);
                    dictOrder[groupKey] = tradeOrder;
                }
            }
            WriteKeyToDB(dictOrder);
        }

        public async Task<string> MakeRequest(string groupKey, string tradeOrderId)
        {
            var request = new HttpRequestMessage(HttpMethod.Post, @"https://my.lazada.vn/customer/api/sync/order-detail");
            string cookie = Properties.Settings.Default.LazadaCookie;
            string userAgent = Properties.Settings.Default.UserAgent;
            request.Headers.TryAddWithoutValidation("Cookie", cookie);
            request.Headers.TryAddWithoutValidation("User-Agent", userAgent);
            var requestContent = new StringContent("{\"ultronVersion\":\"4.5\",\"tradeOrderId\":\"" + tradeOrderId + "\",\"shopGroupKey\":\"" + groupKey + "\"}", Encoding.UTF8, "application/json");
            request.Content = requestContent;
            var response = await HttpSingleton.Client.SendAsync(request);
            //await Task.Delay(500);
            string result = "";
            if (response.IsSuccessStatusCode)
            {
                result = await response.Content.ReadAsStringAsync();
                result = Regex.Unescape(result);
                File.WriteAllText(Path.Combine(@"D:\DOWNLOADED\lazada", "laz_order_" + (id++) + ".txt"), result);
                WriteJSONToDB(groupKey, tradeOrderId, result);
            }
            else
            {
                oldLog.Error("Cannot get order info: " + response.StatusCode);
            }
            return result;
        }

        public async void ButtonFetchOrderInfo()
        {
            var listOrder = ReadFromFile(ORDER_DATA_PATH).ToList();
            listOrder.Sort((x, y) =>
            {
                if (y.Key.Equals(x.Key))
                {
                    return y.Value.CompareTo(x.Value);
                }
                return y.Key.CompareTo(x.Key);
            });
            oldLog.Debug("Number of order: " + listOrder.Count);
            File.WriteAllText(ORDER_URL_PATH, string.Join(Environment.NewLine, listOrder.Select(i => @"https://my.lazada.vn/customer/order/view/?shopGroupKey=" + i.Key + "&tradeOrderId=" + i.Value + "&spm=a2o42.order_list.list_manage.1")));
            await Task.Run(() =>
            {
                for (int i = 1, size = listOrder.Count; i <= size; ++i)
                {
                    var pair = listOrder[i - 1];
                    oldLog.SetValueProgress(i * 100 / size, "Fetch: " + i + "/" + size);
                    string json = MakeRequest(pair.Key, pair.Value).Result;
                }
            });
        }

        private const string ORDER_JSON_FOLDER = @"D:\DOWNLOADED\lazada";
        public void ButtonImportFromFile()
        {
            var files = Directory.GetFiles(ORDER_JSON_FOLDER, "laz_order_*");
            foreach (var path in files)
            {
                string json = File.ReadAllText(path);
                break;
            }
        }
    }
}
