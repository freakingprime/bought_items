﻿using BoughtItems.MVVMBase;
using BoughtItems.UI_Lazada.DetailInfo;
using BoughtItems.UI_Lazada.Shipping;
using BoughtItems.UI_Lazada.ShopInfo;
using BoughtItems.UI_Merge;
using Dapper;
using Microsoft.Data.Sqlite;
using Microsoft.VisualBasic;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Net.Quic;
using System.Reflection;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading;
using System.Threading.Tasks;
using System.Windows.Controls;
using System.Windows.Documents;
using System.Windows.Input;
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

        private int id = 1000;
        private Regex regexField = new Regex(@"""fields"".+?},", RegexOptions.Singleline | RegexOptions.IgnoreCase);
        private Regex regexShopGroupKey = new Regex(@"""shopGroupKey""[^""]+""([^""]+)", RegexOptions.Singleline | RegexOptions.IgnoreCase);
        private Regex regexTradeOrderId = new Regex(@"ORDERLOGIC_(\d+)", RegexOptions.IgnoreCase);
        private const string ORDER_URL_PATH = @"D:\DOWNLOADED\lazada\order_url.txt";
        private CancellationTokenSource cts;

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
                //keep as it is
                //result = Regex.Unescape(result);
            }
            else
            {
                oldLog.Error("Cannot get order info: " + response.StatusCode);
            }
            return result;
        }

        private string GetUrl(string group, string order)
        {
            return @"https://my.lazada.vn/customer/order/view/?shopGroupKey=" + group + "&tradeOrderId=" + order + "&spm=a2o42.order_list.list_manage.1";
        }

        private static long GetNumberFromString(string s)
        {
            string temp = "0";
            foreach (char c in s)
            {
                if (c >= '0' && c <= '9')
                {
                    temp += c;
                }
                else if (c == ' ')
                {
                    //break;
                }
            }
            _ = long.TryParse(temp, out long ret);
            return ret;
        }

        public async void ButtonFetchOrderInfo()
        {
            cts = new CancellationTokenSource();
            List<KeyValuePair<string, string>> listOrder = new List<KeyValuePair<string, string>>();
            oldLog.Debug("Read order list from database...");
            using (var connection = new SqliteConnection("Data Source=\"" + MergeVm.GetDatabasePath() + "\""))
            {
                var listPair = await connection.QueryAsync(@"select ShopGroupKey,TradeOrderID from lazada_json where JsonData IS NULL OR JsonData NOT LIKE ""%tradeOrder%""");
                if (cts.Token.IsCancellationRequested)
                {
                    oldLog.Debug("Task is cancelled");
                    return;
                }
                //var listPair = await connection.QueryAsync(@"select ShopGroupKey,TradeOrderID from lazada_json");
                foreach (var item in listPair)
                {
                    listOrder.Add(new KeyValuePair<string, string>(item.ShopGroupKey, item.TradeOrderID));
                }
            }
            listOrder.Sort((x, y) =>
            {
                if (y.Key.Equals(x.Key))
                {
                    return y.Value.CompareTo(x.Value);
                }
                return y.Key.CompareTo(x.Key);
            });
            oldLog.Debug("Number of order to fetch: " + listOrder.Count);
            try
            {
                File.WriteAllText(ORDER_URL_PATH, string.Join(Environment.NewLine, listOrder.Select(i => GetUrl(i.Key, i.Value))));
            }
            catch { }
            await Task.Run(async () =>
            {
                for (int i = 1, size = listOrder.Count; i <= size; ++i)
                {
                    if (cts.Token.IsCancellationRequested)
                    {
                        oldLog.Debug("Task is cancelled");
                        return;
                    }
                    var pair = listOrder[i - 1];
                    oldLog.SetValueProgress(i * 100 / size, "Fetch: " + i + "/" + size);
                    string json = MakeRequest(pair.Key, pair.Value).Result;
                    await Task.Delay(200);
                    if (json.Length > 0 && json.Contains("tradeOrderId", StringComparison.OrdinalIgnoreCase))
                    {
                        WriteJSONToDB(pair.Key, pair.Value, json);
                    }
                    else
                    {
                        oldLog.Error("Fail data: " + pair.Key + " " + pair.Value);
                        try
                        {
                            File.WriteAllText(Path.Combine(@"D:\DOWNLOADED\lazada", "laz_order_fail_" + (id++) + ".txt"), json);
                        }
                        catch { }
                    }
                }
            });
            oldLog.SetValueProgress(0);
            oldLog.Debug("Fetch completed");
        }

        private string ExtractJsonKey(string json, string strRegex)
        {
            string ret = "";
            Regex regexKey = new Regex(strRegex, RegexOptions.IgnoreCase);
            MatchCollection matches = regexKey.Matches(json);
            if (matches.Count == 1)
            {
                int indexKey = matches[0].Index;
                if (indexKey >= 0)
                {
                    int indexFirstBrace = json.IndexOf('{', indexKey);
                    if (indexFirstBrace >= 0)
                    {
                        int countBrace = 1;
                        int cur = indexFirstBrace + 1;
                        while (cur < json.Length && countBrace > 0)
                        {
                            switch (json[cur])
                            {
                                case '}':
                                    --countBrace;
                                    break;
                                case '{':
                                    ++countBrace;
                                    break;
                            }
                            ++cur;
                        }
                        ret = json.Substring(indexFirstBrace, cur - indexFirstBrace);
                    }
                }
            }
            return ret;
        }

        private class DbModelItem
        {
            public string Name;
            public string Detail;
            public string ImageURL;
        }

        private void ParseLazadaInfo()
        {
            oldLog.Debug("Begin parse Lazada info...");
            Regex regexRemoveBizParams = new Regex(@"""bizParams""\s*?:[^,]+,");
            List<UI_Merge.OrderInfo> listOrder = new List<UI_Merge.OrderInfo>();
            using (var connection = new SqliteConnection("Data Source=\"" + MergeVm.GetDatabasePath() + "\""))
            {
                var listPair = connection.Query(@"select * from lazada_json where JsonData LIKE ""%tradeOrder%""");
                List<DbModelItem> listItemNoImageData = connection.Query<DbModelItem>(@"SELECT Name,Detail,ImageURL FROM item WHERE ImageData IS NOT NULL").ToList();
                oldLog.Debug("Number of JSON: " + listPair.Count());
                if (cts.Token.IsCancellationRequested)
                {
                    oldLog.Debug("Task is cancelled");
                    return;
                }
                int count = 0;
                int size = listPair.Count();
                foreach (var item in listPair)
                {
                    if (cts.Token.IsCancellationRequested)
                    {
                        oldLog.Debug("Task is cancelled");
                        return;
                    }
                    ++count;
                    oldLog.SetValueProgress(count * 100 / size, "Parsing: " + count + "/" + size);
                    string shopGroupKey = item.ShopGroupKey;
                    string tradeOrderId = item.TradeOrderID;
                    string json = item.JsonData;
                    json = regexRemoveBizParams.Replace(json, "");
                    string sectionPackageShipping = ExtractJsonKey(json, @"""packageShippingInfo[^""]+""\s*:\s*{");
                    string sectionDetail = ExtractJsonKey(json, @"""detailInfo[^""]+""\s*:\s*{");
                    string sectionShop = ExtractJsonKey(json, @"""orderShop[^""]+""\s*:\s*{");
                    if (sectionDetail.Length > 0 && sectionPackageShipping.Length > 0 && sectionShop.Length > 0)
                    {
                        try
                        {
                            ShippingInfoRoot shipping = JsonConvert.DeserializeObject<ShippingInfoRoot>(sectionPackageShipping);
                            DetailInfoRoot detail = JsonConvert.DeserializeObject<DetailInfoRoot>(sectionDetail);
                            ShopInfoRoot shop = JsonConvert.DeserializeObject<ShopInfoRoot>(sectionShop);
                            log.Debug("Get correct JSON: " + shopGroupKey);
                            UI_Merge.OrderInfo order = new UI_Merge.OrderInfo();
                            order.TotalPrice = GetNumberFromString(detail.fields.total);
                            order.ID = tradeOrderId; //use number only
                            order.OrderURL = GetUrl(shopGroupKey, tradeOrderId);
                            order.UserName = "lazada";
                            order.ShopName = shop.fields.name ?? "";
                            order.ShopURL = shop.fields.link ?? "";
                            if (order.ShopURL.StartsWith("//"))
                            {
                                order.ShopURL = "https://" + order.ShopURL.Substring(2);
                            }
                            foreach (var singleItem in detail.fields.extraParam.orderSnapshot.summaries)
                            {
                                ItemInfo info = new ItemInfo();
                                info.ItemName = singleItem.itemTitle ?? "";
                                info.ItemDetails = singleItem.skuInfo ?? "";
                                info.OriginalPrice = GetNumberFromString(singleItem.itemPrice);
                                info.ActualPrice = info.OriginalPrice;
                                info.NumberOfItem = singleItem.quantity;
                                info.ImageURL = singleItem.picUrl ?? "";
                                int countNoImageData = listItemNoImageData.Count(i => i.Name.Equals(info.ItemName) && i.Detail.Equals(info.ItemDetails) && i.ImageURL.Equals(info.ImageURL));
                                if (countNoImageData == 0 && info.ImageURL.StartsWith("http", StringComparison.OrdinalIgnoreCase))
                                {
                                    var data = HttpSingleton.Client.GetByteArrayAsync(new Uri(info.ImageURL)).Result;
                                    Thread.Sleep(200);
                                    if (data.Length > 0)
                                    {
                                        info.LocalImageName = Convert.ToBase64String(data);
                                    }
                                }
                                order.ListItems.Add(info);
                            }
                            listOrder.Add(order);
                        }
                        catch (Exception ex)
                        {
                            oldLog.Error("Cannot get package shipping section: " + shopGroupKey, ex);
                        }
                    }
                    else
                    {
                        log.Debug("Not a completed order: " + GetUrl(shopGroupKey, tradeOrderId));
                    }
                }
            }
            oldLog.Debug("Number of order: " + listOrder.Count);
            MergeVm.InsertOrderInfoToDatabase(listOrder, true);
            oldLog.SetValueProgress(0);
            oldLog.Debug("Parse finished");
        }

        public async void ButtonInsertToDatabase()
        {
            cts = new CancellationTokenSource();
            await Task.Run(() =>
            {
                ParseLazadaInfo();
            });
        }

        internal void ButtonStop()
        {
            if (cts != null)
            {
                cts.Cancel();
                oldLog.Debug("Stop all tasks");
            }
        }
    }
}
