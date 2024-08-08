using BoughtItems.MVVMBase;
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

        private void WriteToFile(string path, Dictionary<string, string> dict)
        {
            var temp = dict.ToList();
            temp.Sort((x, y) =>
            {
                if (y.Key.Equals(x.Key))
                {
                    return y.Value.CompareTo(x.Value);
                }
                return y.Key.CompareTo(x.Key);
            });
            string s = string.Join(Environment.NewLine, temp.Select(i => i.Key + "|" + i.Value));
            File.WriteAllText(path, s);
        }

        public void ParseOrderList(string json)
        {
            string unicode = Regex.Unescape(json);
            //log.Debug(s);
            //File.WriteAllText(Path.Combine(@"D:\DOWNLOADED\lazada", "laz_" + (id++) + ".txt"), unicode);
            Dictionary<string, string> dictOrder = ReadFromFile(ORDER_DATA_PATH);
            MatchCollection listGroupKey = regexShopGroupKey.Matches(unicode);
            foreach (Match matchGroup in listGroupKey)
            {
                string groupKey = matchGroup.Groups[1].Value;
                Match matchTradeOrderId = regexTradeOrderId.Match(groupKey);
                if (matchTradeOrderId.Success)
                {
                    string tradeOrder = matchTradeOrderId.Groups[1].Value;
                    oldLog.Debug("Group key: " + groupKey + " Trade order: " + tradeOrder);
                    dictOrder[groupKey] = tradeOrder;
                }
            }
            WriteToFile(ORDER_DATA_PATH, dictOrder);
        }

        public void MakeRequest()
        {
            //var request = new HttpRequestMessage(HttpMethod.Post, @"https://my.lazada.vn/customer/api/sync/order-detail");
            //request.Headers.TryAddWithoutValidation("Cookie", Properties.Settings.Default.LazadaCookie);
            //request.Headers.TryAddWithoutValidation("User-Agent", Properties.Settings.Default.UserAgent);
            //var requestContent = new StringContent("{\"ultronVersion\":\"4.5\",\"tradeOrderId\":\"" + tradeOrder + "\",\"shopGroupKey\":\"" + groupKey + "\"}", Encoding.UTF8, "application/json");
            //log.Debug("Request content: " + requestContent.ToString());
            //request.Content = requestContent;
            //var response = await HttpSingleton.Client.SendAsync(request);
            //if (response.IsSuccessStatusCode)
            //{
            //    string result = await response.Content.ReadAsStringAsync();
            //    result = Regex.Unescape(result);
            //    File.WriteAllText(Path.Combine(@"D:\DOWNLOADED\lazada", "laz_order_" + (id++) + ".txt"), result);
            //}
            //else
            //{
            //    oldLog.Error("Cannot get order info: " + response.StatusCode);
            //}
        }
    }
}
