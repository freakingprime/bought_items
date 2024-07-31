using BoughtItems.MVVMBase;
using Dapper;
using HtmlAgilityPack;
using Microsoft.Data.Sqlite;
using Microsoft.Win32;
using Newtonsoft.Json;
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data.SqlTypes;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Runtime.CompilerServices;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading;
using System.Threading.Tasks;
using System.Web.UI;
using System.Windows;
using System.Windows.Media.Animation;

namespace BoughtItems.UI_Merge
{
    public class MergeVm : ViewModelBase
    {
        private static readonly log4net.ILog log = log4net.LogManager.GetLogger(System.Reflection.MethodBase.GetCurrentMethod().DeclaringType.Name);
        private static readonly LogController oldLog = LogController.Instance;

        public MergeVm()
        {
            IsTaskIdle = true;
            DownloadButtonEnabled = true;
            IsUseDatabase = true;
            IsExportDefaultFilename = true;
        }

        #region Bind properties

        private bool _isUseDatabase;

        public bool IsUseDatabase
        {
            get { return _isUseDatabase; }
            set { SetValue(ref _isUseDatabase, value); }
        }

        private int _progressValue;

        public int ProgressValue
        {
            get { return _progressValue; }
            set { SetValue(ref _progressValue, value); }
        }

        private bool _isTaskIdle;

        public bool IsTaskIdle
        {
            get { return _isTaskIdle; }
            set { SetValue(ref _isTaskIdle, value); }
        }

        private bool _downloadButtonEnabled;

        public bool DownloadButtonEnabled
        {
            get { return _downloadButtonEnabled; }
            set { SetValue(ref _downloadButtonEnabled, value); }
        }

        private string _txtDatabasePath;

        public string TxtDatabasePath { get => _txtDatabasePath; set => SetValue(ref _txtDatabasePath, value); }

        private bool _isExportDefaultFilename;

        public bool IsExportDefaultFilename { get => _isExportDefaultFilename; set => SetValue(ref _isExportDefaultFilename, value); }

        #endregion

        #region Normal properties

        private static HttpClient _httpSingleton;

        public static HttpClient HttpSingleton
        {
            get
            {
                if (_httpSingleton == null)
                {
                    _httpSingleton = new HttpClient();
                }
                return _httpSingleton;
            }
        }

        private const string IMAGE_FOLDER_NAME = "images";
        private const string NONE_TEXT = "None";
        private readonly Regex regexOrderID = new Regex(@"\/order\/(\d+)");
        private readonly Regex regexShopID = new Regex(@"\/shop\/(\d+)");

        //<div class="shopee-image__content" style="background-image: url(https://cf.shopee.vn/file/e40478ec9ce88362d0c479fff1cf6e70_tn);"><div class="shopee-image__content--blur"> </div></div>
        //<div class="shopee-image__content" style="background-image: /*savepage-url=https://cf.shopee.vn/file/90796d245838a4ceb821252801ea3b4c_tn*/ var(--savepage-url-14);"><div class="shopee-image__content--blur"> </div></div>

        //2021.04.21: Add ) to ending character
        private readonly Regex regexItemImageURL = new Regex(@"http[^*)""]+");

        public const char FILENAME_SEPERATOR = '\n';

        #endregion

        public void Loaded()
        {
            //LoadDataFromFile(@"D:\DOWNLOAD\Shopee.html", workerMerge, 0, 1);
            //CreateHTML(@"D:\DOWNLOAD\test.html");
        }

        private string GetNode(string type, string nameClass)
        {
            return ".//" + type + "[contains(@class, '" + nameClass + "')]";
        }

        private long GetNumberFromString(string s)
        {
            int i = 0;
            while (i < s.Length)
            {
                if (s[i] < '0' || s[i] > '9')
                {
                    s = s.Remove(i, 1);
                }
                else
                {
                    ++i;
                }
            }
            if (!long.TryParse(s, out long result))
            {
                result = 0;
            }
            return result;
        }

        #region Async function

        private CancellationTokenSource _cts;
        private CancellationToken _cancelToken;
        private IProgress<KeyValuePair<int, string>> _progress = new Progress<KeyValuePair<int, string>>(p =>
        {
            oldLog.SetValueProgress(p.Key, p.Value);
        });

        #endregion

        public async void ButtonMerge()
        {
            if (!IsTaskIdle)
            {
                //task is running
                oldLog.Debug("Current task is running");
                return;
            }
            //task is not running
            //declare cancellation token
            _cts = new CancellationTokenSource();
            _cancelToken = _cts.Token;
            IsTaskIdle = false;
            ConcurrentBag<OrderInfo> bagOrder = new ConcurrentBag<OrderInfo>();
            string[] files = Properties.Settings.Default.HtmlFiles.Split(FILENAME_SEPERATOR).Select(i => i.Trim()).Where(i => File.Exists(i)).ToArray();
            int count = 0;
            int size = files.Length;

            //2024.07.30: We don't need to download images, just get content directly from internet
            //string imageFolderPath = Path.Combine(Properties.Settings.Default.DatabaseDirectory, Properties.Resources.TEMP_IMAGE_FOLDER);
            //var di = new DirectoryInfo(imageFolderPath);
            //try
            //{
            //    if (di.Exists)
            //    {
            //        di.Delete(true);
            //    }
            //    di.Create();
            //}
            //catch (Exception e1)
            //{
            //    oldLog.Error("Cannot create temporary folder at: " + di.FullName, e1);
            //}

            await Parallel.ForEachAsync(files, (path, _) =>
            {
                try
                {
                    var ret = LoadDataFromFile(path);
                    foreach (var item in ret)
                    {
                        bagOrder.Add(item);
                    }
                }
                catch (Exception e1)
                {
                    oldLog.Error("Cannot load data from file: " + path, e1);
                }
                ++count;
                _progress.Report(new KeyValuePair<int, string>(count * 100 / size, "Parsed " + count + "/" + size));
                return new ValueTask();
            });
            oldLog.Debug("Number of orders: " + bagOrder.Count);
            oldLog.SetValueProgress(0);

            await Task.Run(() =>
            {
                InsertToDatabase(bagOrder);
            });

            IsTaskIdle = true;
        }

        public void ButtonMoveImages()
        {
            const int MAX_FILE = 200;
            DirectoryInfo di = new DirectoryInfo(Path.Combine(Properties.Settings.Default.DatabaseDirectory, IMAGE_FOLDER_NAME));
            if (!di.Exists)
            {
                di.Create();
            }
            DirectoryInfo[] arrSubDir = di.GetDirectories("*", SearchOption.TopDirectoryOnly);
            FileInfo[] tempArr;

            //move file out of sub folder if there's too many file in sub folder
            for (int i = 0; i < arrSubDir.Length; ++i)
            {
                tempArr = arrSubDir[i].GetFiles("*");
                if (tempArr.Length > MAX_FILE)
                {
                    for (int j = MAX_FILE; j < tempArr.Length; ++j)
                    {
                        File.Move(tempArr[j].FullName, Path.Combine(di.FullName, tempArr[j].Name));
                    }
                }
            }

            //move file into current sub folders
            tempArr = di.GetFiles("*", SearchOption.TopDirectoryOnly);
            int index = 0;
            for (int i = 0; i < arrSubDir.Length; ++i)
            {
                int count = arrSubDir[i].GetFiles("*").Length;
                while (count < MAX_FILE && index < tempArr.Length)
                {
                    File.Move(tempArr[index].FullName, Path.Combine(arrSubDir[i].FullName, tempArr[index].Name));
                    ++index;
                    ++count;
                }
            }

            //create more sub folders and copy file into it
            tempArr = di.GetFiles("*", SearchOption.TopDirectoryOnly);
            index = 0;
            int newFolderSuffix = arrSubDir.Length;
            while (index < tempArr.Length)
            {
                DirectoryInfo newDirInfo = new DirectoryInfo(Path.Combine(di.FullName, IMAGE_FOLDER_NAME + newFolderSuffix++));
                if (!newDirInfo.Exists)
                {
                    newDirInfo.Create();
                }
                int count = 0;
                while (count < MAX_FILE && index < tempArr.Length)
                {
                    File.Move(tempArr[index].FullName, Path.Combine(newDirInfo.FullName, tempArr[index].Name));
                    ++index;
                    ++count;
                }
            }
        }

        public async void ButtonExportToHTML()
        {
            string name = "";
            string directory = "";
            if (IsExportDefaultFilename)
            {
                directory = Properties.Settings.Default.ExportedHTMLDirectory;
                if (!Directory.Exists(directory))
                {
                    directory = Properties.Settings.Default.DatabaseDirectory;
                }
                name = Path.Combine(directory, "Summary.html");
                if (MessageBox.Show("Do you want to export to: " + name, "Confirm", MessageBoxButton.YesNo, MessageBoxImage.Question, MessageBoxResult.No) != MessageBoxResult.Yes)
                {
                    return;
                }
            }
            else
            {
                //user select exported file
                SaveFileDialog dialog = new SaveFileDialog
                {
                    InitialDirectory = Utils.GetValidFolderPath(Properties.Settings.Default.ExportedHTMLDirectory),
                    Filter = "HTML File|*.html",
                    CheckFileExists = false,
                };
                if ((bool)dialog.ShowDialog())
                {
                    name = dialog.FileName;
                    directory = Path.GetDirectoryName(name);
                }
            }
            Properties.Settings.Default.ExportedHTMLDirectory = directory;
            Properties.Settings.Default.Save();
            oldLog.Debug("Will export to: " + name);

            IsTaskIdle = false;
            await Task.Run(() =>
            {
                //backup HTML file
                const string BACKUP_FOLDER = "backup";
                _ = Directory.CreateDirectory(Path.Combine(directory, BACKUP_FOLDER));
                string backupName = Path.GetFileNameWithoutExtension(name);
                backupName = backupName + "_" + DateTime.Now.ToString("yyyyMMdd_HHmmss") + Path.GetExtension(name);
                backupName = Path.Combine(Path.GetDirectoryName(name), BACKUP_FOLDER, backupName);
                if (File.Exists(name))
                {
                    File.Copy(name, backupName, true);
                }

                //create HTML
                string offlineHTMLName = name.Replace(".html", "_offline.html");
                CreateHTML(name);
                CreateOfflineHTML(offlineHTMLName);
                oldLog.Debug("Exported to HTML file: " + name + " and " + offlineHTMLName);
            });
            oldLog.SetValueProgress(0);
            IsTaskIdle = true;
        }

        private void CreateOfflineHTML(string outputFile)
        {
            List<OrderInfo> ListOrders = new List<OrderInfo>();
            StringWriter stringWriter = new StringWriter();
            const int IMAGE_SIZE = 150;
            int count = 0;
            using (HtmlTextWriter writer = new HtmlTextWriter(stringWriter))
            {
                writer.RenderBeginTag(HtmlTextWriterTag.Html);

                writer.RenderBeginTag(HtmlTextWriterTag.Head);

                writer.AddAttribute("charset", "UTF-8");
                writer.RenderBeginTag(HtmlTextWriterTag.Meta);
                writer.RenderEndTag();

                writer.RenderBeginTag(HtmlTextWriterTag.Style);
                writer.WriteLine("img.item_image {width: " + IMAGE_SIZE + "px;height: " + IMAGE_SIZE + "px}");
                writer.WriteLine("table {border-collapse: collapse; table-layout: fixed}");
                writer.WriteLine("th, td {border: 1px solid black; padding: 5px; word-break: break-all; word-wrap: break-word; white-space: normal}");
                writer.RenderEndTag();
                writer.RenderEndTag();

                writer.RenderBeginTag(HtmlTextWriterTag.Body);
                writer.AddAttribute(HtmlTextWriterAttribute.Width, "1250");
                writer.RenderBeginTag(HtmlTextWriterTag.Table);

                writer.RenderBeginTag(HtmlTextWriterTag.Thead);

                //check new git

                writer.RenderBeginTag(HtmlTextWriterTag.Tr);
                writer.WriteLine("<th width=\"40\">No</th>");
                writer.WriteLine("<th width=\"150\">Local Image</th>");
                writer.WriteLine("<th>Item (" + ListOrders.Sum(i => i.ListItems.Count) + ") </th>");

                long superTotalActualPrice = ListOrders.Sum(i => i.ListItems.Sum(j => j.ActualPrice * j.NumberOfItem));
                writer.WriteLine("<th width=\"70\">Price (" + superTotalActualPrice.ToString("N0") + ") </th>");

                //2021.11.14: Add recuded money
                long totalPaid = ListOrders.Sum(i => i.TotalPrice);
                writer.WriteLine("<th width=\"70\">Paid (Saved " + (superTotalActualPrice - totalPaid).ToString("N0") + ") </th>");

                writer.WriteLine("<th width=\"125\">Order (" + ListOrders.GroupBy(i => i.OrderURL).Select(group => group.First()).Count() + ") </th>");
                writer.WriteLine("<th width=\"120\">Shop (" + ListOrders.GroupBy(i => i.ShopURL).Select(g => g.First()).Count() + ") </th>");
                writer.WriteLine("<th width=\"120\">User</th>");
                writer.RenderEndTag(); //end tr

                writer.RenderEndTag(); //end thead

                writer.RenderBeginTag(HtmlTextWriterTag.Tbody);
                count = 0;

                //create local file name database
                Dictionary<string, string> dict = new Dictionary<string, string>();
                DirectoryInfo di = new DirectoryInfo(Path.Combine(Properties.Settings.Default.DatabaseDirectory, IMAGE_FOLDER_NAME));
                if (di.Exists)
                {
                    var subDirs = di.GetDirectories("*");
                    foreach (var imageDir in subDirs)
                    {
                        var files = imageDir.GetFiles("*");
                        foreach (var file in files)
                        {
                            dict[file.Name] = imageDir.Name;
                        }
                    }
                }

                foreach (OrderInfo order in ListOrders)
                {
                    long totalActualPrice = order.ListItems.Sum(i => i.ActualPrice * i.NumberOfItem);
                    foreach (ItemInfo item in order.ListItems)
                    {
                        writer.RenderBeginTag(HtmlTextWriterTag.Tr);

                        ++count;
                        writer.WriteLine("<td>" + count + "</td>");
                        if (!dict.TryGetValue(item.LocalImageName, out string subFolderName))
                        {
                            subFolderName = "";
                        }
                        string imageLocalPath = IMAGE_FOLDER_NAME + "/" + subFolderName + (subFolderName.Length > 0 ? "/" : "") + item.LocalImageName;
                        imageLocalPath = Path.Combine(Path.GetDirectoryName(outputFile), imageLocalPath);
                        writer.WriteLine(string.Format("<td><img class=\"item_image\" src=\"data:image/jpeg;base64,{0}\"/></td>", File.Exists(imageLocalPath) ? Convert.ToBase64String(File.ReadAllBytes(imageLocalPath)) : ""));

                        //item content
                        writer.RenderBeginTag(HtmlTextWriterTag.Td);
                        writer.RenderBeginTag(HtmlTextWriterTag.Div);
                        writer.WriteLine("<div class=\"item_name\">" + item.ItemName + "</div>");
                        if (item.ItemDetails.Length > 0)
                        {
                            writer.WriteLine("<div class=\"item_details\">Loại: " + item.ItemDetails + "</div>");
                        }
                        if (item.NumberOfItem >= 2)
                        {
                            writer.WriteLine("<div class=\"item_details\">Số lượng: " + item.NumberOfItem + "</div>");
                        }
                        writer.RenderEndTag(); //end div
                        writer.RenderEndTag(); //end td

                        writer.WriteLine("<td>" + string.Format("{0:n0}", item.ActualPrice) + "</td>");
                        writer.WriteLine("<td>" + string.Format("{0:n0}", totalActualPrice == 0 ? item.ActualPrice : item.ActualPrice * order.TotalPrice / totalActualPrice) + "</td>");
                        writer.WriteLine(string.Format("<td><a href=\"{0}\" target=\"_blank\">{1}</a></td>", order.OrderURL, order.ID));
                        writer.WriteLine(string.Format("<td><a href=\"{0}\" target=\"_blank\">{1}</a></td>", order.ShopURL, order.ShopName));
                        writer.WriteLine("<td>" + order.UserName + "</td>");

                        writer.RenderEndTag(); //end tr
                    }
                }
                writer.RenderEndTag(); //end tbody
                writer.RenderEndTag(); //end table
                writer.RenderEndTag(); //end body
                writer.RenderEndTag(); //end HTML
            }
            File.WriteAllText(outputFile, stringWriter.ToString());

            List<string> list = new List<string>();
            const string TAB = "\t";

            list.Add(string.Join(TAB, "No", "Item", "Quantity", "Actual Price", "Total Price", "Order", "Shop", "User"));
            count = 0;
            foreach (OrderInfo order in ListOrders)
            {
                foreach (ItemInfo item in order.ListItems)
                {
                    ++count;
                    list.Add(string.Join(TAB, count, item.ItemName.Replace("\r", "").Replace("\n", "") + (item.ItemDetails.Length > 0 ? (" | " + item.ItemDetails) : ""), item.NumberOfItem, item.ActualPrice, item.ActualPrice * item.NumberOfItem, order.ID, order.ShopName, order.UserName));
                }
            }
            string newPath = outputFile.Replace(Path.GetExtension(outputFile), "") + "_excel.txt";
            File.WriteAllText(newPath, string.Join(Environment.NewLine, list));
        }

        private void CreateHTML(string outputFile)
        {
            Stopwatch sw = Stopwatch.StartNew();
            IEnumerable<DbModelOrder> list = null;
            using (var connection = new SqliteConnection("Data Source=\"" + GetDatabasePath() + "\""))
            {
                list = connection.Query<DbModelOrder>(@"select * from orderpee,item,order_item where orderpee.ID=order_item.OrderID and item.ID=order_item.ItemID");
                sw.Stop();
                oldLog.Debug("Query all data in: " + sw.ElapsedMilliseconds + " ms");
            }
            if (list == null)
            {
                oldLog.Error("Cannot query database");
                return;
            }
            var arrOrder = list.ToArray();
            Array.Sort(arrOrder, (x, y) =>
            {
                if (y.OrderID == x.OrderID)
                {
                    return x.Name.CompareTo(y.Name);
                }
                return y.OrderID.CompareTo(x.OrderID);
            });
            StringWriter stringWriter = new StringWriter();
            const int IMAGE_SIZE = 150;
            int count = 0;

            using (HtmlTextWriter writer = new HtmlTextWriter(stringWriter))
            {
                writer.RenderBeginTag(HtmlTextWriterTag.Html);

                writer.RenderBeginTag(HtmlTextWriterTag.Head);

                writer.AddAttribute("charset", "UTF-8");
                writer.RenderBeginTag(HtmlTextWriterTag.Meta);
                writer.RenderEndTag();

                writer.RenderBeginTag(HtmlTextWriterTag.Style);
                writer.WriteLine("img.item_image {width: " + IMAGE_SIZE + "px;height: " + IMAGE_SIZE + "px}");
                writer.WriteLine("table {border-collapse: collapse; table-layout: fixed}");
                writer.WriteLine("th, td {border: 1px solid black; padding: 5px; word-break: break-all; word-wrap: break-word; white-space: normal}");
                writer.RenderEndTag();
                writer.RenderEndTag();

                writer.RenderBeginTag(HtmlTextWriterTag.Body);
                writer.AddAttribute(HtmlTextWriterAttribute.Width, "1250");
                writer.RenderBeginTag(HtmlTextWriterTag.Table);

                writer.RenderBeginTag(HtmlTextWriterTag.Thead);

                //check new git

                writer.RenderBeginTag(HtmlTextWriterTag.Tr);
                writer.WriteLine("<th width=\"40\">No</th>");
                writer.WriteLine("<th width=\"150\">Image</th>");
                writer.WriteLine("<th width=\"150\">Local Image</th>");
                writer.WriteLine("<th>Item (" + arrOrder.Count() + ") </th>");

                long superTotalActualPrice = arrOrder.Sum(i => i.ActualPrice * i.Quantity);
                writer.WriteLine("<th width=\"70\">Price (" + superTotalActualPrice.ToString("N0") + ") </th>");

                //2021.11.14: Add recuded money
                long totalPaid = arrOrder.GroupBy(order => order.OrderID).Sum(group => group.First().TotalPrice);
                writer.WriteLine("<th width=\"70\">Paid (Saved " + (superTotalActualPrice - totalPaid).ToString("N0") + ") </th>");

                writer.WriteLine("<th width=\"125\">Order (" + arrOrder.GroupBy(order => order.OrderID).Count() + ") </th>");
                writer.WriteLine("<th width=\"120\">Shop (" + arrOrder.GroupBy(order => order.ShopURL).Count() + ") </th>");
                writer.WriteLine("<th width=\"120\">User</th>");
                writer.RenderEndTag(); //end tr

                writer.RenderEndTag(); //end thead

                writer.RenderBeginTag(HtmlTextWriterTag.Tbody);
                count = 0;

                foreach (var order in arrOrder)
                {
                    long totalActualPrice = arrOrder.Where(i => i.OrderID == order.OrderID).Sum(i => i.ActualPrice * i.Quantity);
                    foreach (ItemInfo item in order.ListItems)
                    {
                        writer.RenderBeginTag(HtmlTextWriterTag.Tr);

                        ++count;
                        writer.WriteLine("<td>" + count + "</td>");
                        writer.WriteLine(string.Format("<td><img class=\"item_image\" src=\"{0}\"/></td>", item.ImageURL));
                        if (!dict.TryGetValue(item.LocalImageName, out string subFolderName))
                        {
                            subFolderName = "";
                        }
                        writer.WriteLine(string.Format("<td><img class=\"item_image\" src=\"{0}\"/></td>", IMAGE_FOLDER_NAME + "/" + subFolderName + (subFolderName.Length > 0 ? "/" : "") + item.LocalImageName));

                        //item content
                        writer.RenderBeginTag(HtmlTextWriterTag.Td);
                        writer.RenderBeginTag(HtmlTextWriterTag.Div);
                        writer.WriteLine("<div class=\"item_name\">" + item.ItemName + "</div>");
                        if (item.ItemDetails.Length > 0)
                        {
                            writer.WriteLine("<div class=\"item_details\">Loại: " + item.ItemDetails + "</div>");
                        }
                        if (item.NumberOfItem >= 2)
                        {
                            writer.WriteLine("<div class=\"item_details\">Số lượng: " + item.NumberOfItem + "</div>");
                        }
                        writer.RenderEndTag(); //end div
                        writer.RenderEndTag(); //end td

                        writer.WriteLine("<td>" + string.Format("{0:n0}", item.ActualPrice) + "</td>");
                        writer.WriteLine("<td>" + string.Format("{0:n0}", totalActualPrice == 0 ? item.ActualPrice : item.ActualPrice * order.TotalPrice / totalActualPrice) + "</td>");
                        writer.WriteLine(string.Format("<td><a href=\"{0}\" target=\"_blank\">{1}</a></td>", order.OrderURL, order.ID));
                        writer.WriteLine(string.Format("<td><a href=\"{0}\" target=\"_blank\">{1}</a></td>", order.ShopURL, order.ShopName));
                        writer.WriteLine("<td>" + order.UserName + "</td>");

                        writer.RenderEndTag(); //end tr
                    }
                }
                writer.RenderEndTag(); //end tbody
                writer.RenderEndTag(); //end table
                writer.RenderEndTag(); //end body
                writer.RenderEndTag(); //end HTML
            }
            File.WriteAllText(outputFile, stringWriter.ToString());

            List<string> list = new List<string>();
            const string TAB = "\t";

            list.Add(string.Join(TAB, "No", "Item", "Quantity", "Actual Price", "Total Price", "Order", "Shop", "User"));
            count = 0;
            foreach (OrderInfo order in ListOrders)
            {
                foreach (ItemInfo item in order.ListItems)
                {
                    ++count;
                    list.Add(string.Join(TAB, count, item.ItemName.Replace("\r", "").Replace("\n", "") + (item.ItemDetails.Length > 0 ? (" | " + item.ItemDetails) : ""), item.NumberOfItem, item.ActualPrice, item.ActualPrice * item.NumberOfItem, order.ID, order.ShopName, order.UserName));
                }
            }
            string newPath = outputFile.Replace(Path.GetExtension(outputFile), "") + "_excel.txt";
            File.WriteAllText(newPath, string.Join(Environment.NewLine, list));
        }

        private List<OrderInfo> ImportDatabase(string path)
        {
            List<OrderInfo> ret = new List<OrderInfo>();
            HashSet<long> HashOrderID = new HashSet<long>();
            try
            {
                List<OrderInfo> temp = JsonConvert.DeserializeObject<List<OrderInfo>>(File.ReadAllText(path));
                ret.Clear();
                HashOrderID.Clear();
                foreach (OrderInfo order in temp)
                {
                    if (HashOrderID.Add(order.ID))
                    {
                        ret.Add(order);
                    }
                }
            }
            catch (Exception e1)
            {
                log.Error("Cannot import database. " + (e1.Message ?? "No message"));
            }
            return ret;
        }

        private const string ORDER_DIV = "YL_VlX"; //contain whole order
        private const string ORDER_URL_DIV = "LY5oll"; //contain link to order
        private const string ORDER_TOTAL_PRICE_DIV = "NWUSQP";
        private const string SHOP_NAME_DIV = "UDaMW3";
        private const string SHOP_URL_A = "Mr26O7";
        private const string SINGLE_ITEM_DIV = "mZ1OWk";
        private const string ITEM_NAME_SPAN = "DWVWOJ";
        private const string ITEM_DETAIL_DIV = "rsautk";
        private const string ITEM_QUANTITY_DIV = "j3I_Nh";
        private const string ACTUAL_PRICE_DIV = "YRp1mm";
        private const string ACTUAL_PRICE_SPAN = "nW_6Oi PNlXhK";
        private const string ORIGINAL_PRICE_SPAN = "q6Gzj5";
        private const string IMAGE_DIV = "dJaa92";

        private List<OrderInfo> LoadDataFromFile(string path)
        {
            List<OrderInfo> ret = new List<OrderInfo>();
            var doc = new HtmlDocument();
            doc.Load(path);
            log.Info("HTML file is loaded: " + path);

            log.Info("Find wrapper div that contain all orders");
            HtmlNodeCollection orderDivs = doc.DocumentNode.SelectNodes(GetNode("div", ORDER_DIV));
            int orderCount = orderDivs.Count;
            log.Info("Found number of order node div: " + orderCount);

            string username = string.Empty;

            log.Info("Find username");
            HtmlNode node = doc.DocumentNode.SelectSingleNode(GetNode("div", "navbar__username"));
            if (node != null)
            {
                username = node.InnerText.Trim();
                log.Info("Found username: " + username);
            }

            //2021.08.18: Download images too
            int currentIndex = 0;
            foreach (HtmlNode orderDiv in orderDivs)
            {
                ++currentIndex;

                OrderInfo order = new OrderInfo
                {
                    UserName = username
                };

                // ".//" + type + "[@class='" + nameClass + "']"
                log.Info("Find order URL and ID");
                node = orderDiv.SelectSingleNode(".//div[@class='" + ORDER_URL_DIV + "']/a");
                if (node != null)
                {
                    order.OrderURL = node.GetAttributeValue<string>("href", NONE_TEXT).Trim();
                    log.Info("Found order URL: " + order.OrderURL);
                    if (long.TryParse(regexOrderID.Match(order.OrderURL).Groups[1].Value, out order.ID))
                    {
                        log.Info("Found order ID: " + order.ID);
                    }
                }

                log.Info("Find order total price");
                node = orderDiv.SelectSingleNode(GetNode("div", ORDER_TOTAL_PRICE_DIV));
                if (node != null)
                {
                    order.TotalPrice = GetNumberFromString(node.InnerText);
                    log.Info("Found order total price: " + order.TotalPrice);
                }

                log.Info("Find shop name");
                node = orderDiv.SelectSingleNode(GetNode("div", SHOP_NAME_DIV));
                if (node != null)
                {
                    order.ShopName = node.InnerText.Trim();
                    log.Info("Found shop name: " + order.ShopName);
                }

                log.Info("Find shop URL");
                node = orderDiv.SelectSingleNode(GetNode("a", SHOP_URL_A));
                if (node != null)
                {
                    order.ShopURL = node.GetAttributeValue<string>("href", NONE_TEXT);
                    log.Info("Found shop URL: " + order.ShopURL);
                    if (long.TryParse(regexShopID.Match(order.ShopURL).Groups[1].Value, out order.ShopID))
                    {
                        log.Info("Found shop ID: " + order.ShopID);
                    }
                    else
                    {
                        log.Info("Cannot find shop ID as a number: " + order.ShopURL);
                    }
                }

                log.Info("Get item nodes");
                HtmlNodeCollection itemNodes = orderDiv.SelectNodes(GetNode("div", SINGLE_ITEM_DIV));
                if (itemNodes == null)
                {
                    log.Error("Cannot get item node list");
                }
                else
                {
                    log.Info("Item nodes count: " + itemNodes.Count);
                    foreach (var itemNode in itemNodes)
                    {
                        try
                        {
                            ItemInfo item = new ItemInfo();

                            log.Info("Find item name");
                            node = itemNode.SelectSingleNode(GetNode("span", ITEM_NAME_SPAN));
                            if (node != null)
                            {
                                {
                                    item.ItemName = node.InnerText.Trim();
                                    log.Info("Found item name: " + item.ItemName);
                                }

                                log.Info("Find item details");
                                node = itemNode.SelectSingleNode(GetNode("div", ITEM_DETAIL_DIV));
                                if (node != null)
                                {
                                    item.ItemDetails = node.InnerText.Substring(node.InnerText.IndexOf(':') + 1).Trim();
                                    log.Info("Found item details: " + item.ItemDetails);
                                }

                                log.Info("Find item quantity");
                                node = itemNode.SelectSingleNode(GetNode("div", ITEM_QUANTITY_DIV));
                                if (node != null && long.TryParse(node.InnerText.Substring(node.InnerText.IndexOf('x') + 1).Trim(), out item.NumberOfItem))
                                {
                                    log.Info("Found item quantily: " + item.NumberOfItem);
                                }

                                log.Info("Find actual price");
                                node = itemNode.SelectSingleNode(GetNode("div", ACTUAL_PRICE_DIV));
                                if (node != null)
                                {
                                    HtmlNode subNode = node.SelectSingleNode(GetNode("span", ACTUAL_PRICE_SPAN));
                                    if (subNode != null)
                                    {
                                        item.ActualPrice = GetNumberFromString(subNode.InnerText);
                                        log.Info("Found actual price: " + item.ActualPrice);

                                        log.Info("Find original price");
                                        subNode = node.SelectSingleNode(GetNode("span", ORIGINAL_PRICE_SPAN));
                                        if (subNode != null)
                                        {
                                            item.OriginalPrice = GetNumberFromString(subNode.InnerText);
                                            log.Info("Found original price: " + item.OriginalPrice);
                                        }
                                    }
                                    else
                                    {
                                        subNode = node.SelectSingleNode(".//span");
                                        if (subNode != null)
                                        {
                                            item.ActualPrice = GetNumberFromString(subNode.InnerText);
                                            item.OriginalPrice = item.ActualPrice;
                                            log.Info("There's only 1 price: " + item.ActualPrice);
                                        }
                                    }
                                }

                                log.Info("Find item image");
                                node = itemNode.SelectSingleNode(GetNode("div", IMAGE_DIV));
                                if (node != null && regexItemImageURL.IsMatch(node.OuterHtml))
                                {
                                    item.ImageURL = regexItemImageURL.Match(node.OuterHtml).Value.Trim();
                                    if (item.ImageURL.Contains(")"))
                                    {
                                        item.ImageURL = item.ImageURL.Remove(item.ImageURL.IndexOf(")"));
                                    }
                                    string imageName = item.ImageURL.Substring(item.ImageURL.LastIndexOf("/") + 1) + ".jpg";
                                    var data = HttpSingleton.GetByteArrayAsync(item.ImageURL).Result;
                                    if (data.Length > 0)
                                    {
                                        item.LocalImageName = Convert.ToBase64String(data);
                                    }
                                    log.Info("Found item image URL: " + item.ImageURL);
                                }

                                log.Info("Add item to list");
                                order.ListItems.Add(item);
                            }
                        }
                        catch (NullReferenceException e1)
                        {
                            log.Error("Cannot get item node because of null pointer", e1);
                        }
                        catch (Exception e2)
                        {
                            log.Error("Cannot get item node", e2);
                        }
                    }
                }
                ret.Add(order);
            }
            log.Info("Complete parsing: " + path + " | Item count: " + ret.Count);
            return ret;
        }

        private void InsertToDatabase(IEnumerable<OrderInfo> list)
        {
            Stopwatch sw = Stopwatch.StartNew();
            oldLog.Debug("Begin insert " + list.Count() + " order to database...");
            int orderRow = 0;
            int itemRow = 0;
            int orderItemRow = 0;
            int errorOrder = list.Count(i => !i.IsValid);
            int errorItem = list.SelectMany(i => i.ListItems).Count(i => !i.IsValid);
            if (errorOrder > 0 || errorItem > 0)
            {
                string mess = "Found " + errorOrder + " order error and " + errorItem + " item error. Still continue.";
                oldLog.Error(mess);
                _ = MessageBox.Show(mess, "Error", MessageBoxButton.OK, MessageBoxImage.Error);
            }
            using (var connection = new SqliteConnection("Data Source=\"" + GetDatabasePath() + "\""))
            {
                connection.Open();
                var transaction = connection.BeginTransaction();
                foreach (var order in list)
                {
                    if (order.IsValid)
                    {
                        orderRow += connection.Execute(@"INSERT OR IGNORE INTO orderpee (ID,URL,TotalPrice,UserName,ShopName,ShopURL) VALUES (@ID,@URL,@TotalPrice,@UserName,@ShopName,@ShopURL)", new { order.ID, URL = order.OrderURL, TotalPrice = (int)order.TotalPrice, order.UserName, order.ShopName, order.ShopURL });
                    }
                    foreach (var item in order.ListItems)
                    {
                        if (item.IsValid)
                        {
                            itemRow += connection.Execute(@"INSERT OR IGNORE INTO item (Name,Detail,ImageURL,ImageData) VALUES (@ItemName,@ItemDetails,@ImageURL,@ImageData)", new { item.ItemName, item.ItemDetails, item.ImageURL, ImageData = item.LocalImageName });
                        }
                    }
                }
                transaction.Commit();
                transaction = connection.BeginTransaction();
                foreach (var order in list)
                {
                    foreach (var item in order.ListItems)
                    {
                        int ItemID = connection.QuerySingle<int>(@"SELECT ID FROM item WHERE Name=@ItemName AND Detail=@ItemDetails AND ImageURL=@ImageURL", new { item.ItemName, item.ItemDetails, item.ImageURL });
                        orderItemRow += connection.Execute(@"INSERT OR IGNORE INTO order_item (OrderID,ItemID,ActualPrice,OriginalPrice,Quantity) VALUES (@OrderID,@ItemID,@ActualPrice,@OriginalPrice,@NumberOfItem)", new { OrderID = order.ID, ItemID, item.ActualPrice, item.OriginalPrice, item.NumberOfItem });
                    }
                }
                transaction.Commit();
            }
            sw.Stop();
            oldLog.Debug("Insert " + orderRow + " order and " + itemRow + " item and " + orderItemRow + " connection to DB in: " + sw.ElapsedMilliseconds + " ms");
        }

        internal void ButtonInitDatabase()
        {
            Stopwatch sw = Stopwatch.StartNew();
            var ListOrders = ImportDatabase(Properties.Settings.Default.DatabasePath);
            int orderRow = 0;
            int itemRow = 0;
            int orderItemRow = 0;

            //create local file name database
            Dictionary<string, string> dict = new Dictionary<string, string>();
            DirectoryInfo di = new DirectoryInfo(Path.Combine(Properties.Settings.Default.DatabaseDirectory, IMAGE_FOLDER_NAME));
            if (di.Exists)
            {
                var subDirs = di.GetDirectories("*");
                foreach (var imageDir in subDirs)
                {
                    var files = imageDir.GetFiles("*");
                    foreach (var file in files)
                    {
                        dict[file.Name] = imageDir.Name;
                    }
                }
            }

            using (var connection = new SqliteConnection("Data Source=\"" + GetDatabasePath() + "\""))
            {
                connection.Open();
                var transaction = connection.BeginTransaction();
                foreach (var order in ListOrders)
                {
                    orderRow += connection.Execute(@"INSERT OR IGNORE INTO orderpee (ID,URL,TotalPrice,UserName,ShopName,ShopURL) VALUES (@ID,@URL,@TotalPrice,@UserName,@ShopName,@ShopURL)", new { order.ID, URL = order.OrderURL, TotalPrice = (int)order.TotalPrice, order.UserName, order.ShopName, order.ShopURL });
                    foreach (var item in order.ListItems)
                    {
                        if (!dict.TryGetValue(item.LocalImageName, out string subFolderName))
                        {
                            subFolderName = "";
                        }
                        string imageLocalPath = IMAGE_FOLDER_NAME + "/" + subFolderName + (subFolderName.Length > 0 ? "/" : "") + item.LocalImageName;
                        imageLocalPath = Path.Combine(Properties.Settings.Default.DatabaseDirectory, imageLocalPath);
                        string ImageData = File.Exists(imageLocalPath) ? Convert.ToBase64String(File.ReadAllBytes(imageLocalPath)) : "";
                        itemRow += connection.Execute(@"INSERT OR IGNORE INTO item (Name,Detail,ImageURL,ImageData) VALUES (@ItemName,@ItemDetails,@ImageURL,@ImageData)", new { item.ItemName, item.ItemDetails, item.ImageURL, ImageData });
                    }
                }
                transaction.Commit();
                transaction = connection.BeginTransaction();
                foreach (var order in ListOrders)
                {
                    foreach (var item in order.ListItems)
                    {
                        int ItemID = connection.QuerySingle<int>(@"SELECT ID FROM item WHERE Name=@ItemName AND Detail=@ItemDetails AND ImageURL=@ImageURL", new { item.ItemName, item.ItemDetails, item.ImageURL });
                        orderItemRow += connection.Execute(@"INSERT OR IGNORE INTO order_item (OrderID,ItemID,ActualPrice,OriginalPrice,Quantity) VALUES (@OrderID,@ItemID,@ActualPrice,@OriginalPrice,@NumberOfItem)", new { OrderID = order.ID, ItemID, item.ActualPrice, item.OriginalPrice, item.NumberOfItem });
                    }
                }
                transaction.Commit();
            }
            sw.Stop();
            oldLog.Debug("Insert " + orderRow + " order and " + itemRow + " item and " + orderItemRow + " connection to DB in: " + sw.ElapsedMilliseconds + " ms");
        }

        private string GetDatabasePath()
        {
            string targetPath = Properties.Settings.Default.DatabasePath;
            if (!File.Exists(targetPath))
            {
                //copy template file to target path
                FileInfo templateInfo = new FileInfo("template.db");
                if (templateInfo.Exists)
                {
                    try
                    {
                        File.Copy(templateInfo.FullName, targetPath, true);
                    }
                    catch (Exception e1)
                    {
                        oldLog.Error("Cannot copy template database to target: " + targetPath, e1);
                    }
                }
                else
                {
                    oldLog.Error("Cannot find template database at: " + templateInfo.FullName);
                }
            }
            if (!File.Exists(targetPath))
            {
                targetPath = "";
            }
            TxtDatabasePath = targetPath;
            return targetPath;
        }

    }
}
