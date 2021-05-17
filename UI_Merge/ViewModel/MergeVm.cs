using BoughtItems.MVVMBase;
using BoughtItems.UI_Merge.Model;
using HtmlAgilityPack;
using Microsoft.Win32;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.IO;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading;
using System.Threading.Tasks;
using System.Web.UI;

namespace BoughtItems.UI_Merge.ViewModel
{
    public class MergeVm : ViewModelBase
    {
        private static readonly log4net.ILog log = log4net.LogManager.GetLogger(System.Reflection.MethodBase.GetCurrentMethod().DeclaringType.Name);

        public MergeVm()
        {
            IsTaskIdle = true;
        }

        #region Bind properties

        private string _txtHTMLFiles;

        public string TxtHTMLFiles
        {
            get { return _txtHTMLFiles; }
            set { SetValue(ref _txtHTMLFiles, value); }
        }

        private string _txtDatabaseFile;

        public string TxtDatabaseFile
        {
            get { return _txtDatabaseFile; }
            set { SetValue(ref _txtDatabaseFile, value); }
        }

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

        private bool _isWorkerIdle;

        public bool IsTaskIdle
        {
            get { return _isWorkerIdle; }
            set { SetValue(ref _isWorkerIdle, value); }
        }


        #endregion

        #region Normal properties

        private List<OrderInfo> ListOrders = new List<OrderInfo>();
        private HashSet<long> HashOrderID = new HashSet<long>();

        private const string NONE_TEXT = "None";
        private readonly Regex regexOrderID = new Regex(@"\/order\/(\d+)\/");
        private readonly Regex regexShopID = new Regex(@"\/shop\/(\d+)");

        //<div class="shopee-image__content" style="background-image: url(https://cf.shopee.vn/file/e40478ec9ce88362d0c479fff1cf6e70_tn);"><div class="shopee-image__content--blur"> </div></div>
        //<div class="shopee-image__content" style="background-image: /*savepage-url=https://cf.shopee.vn/file/90796d245838a4ceb821252801ea3b4c_tn*/ var(--savepage-url-14);"><div class="shopee-image__content--blur"> </div></div>

        //2021.04.21: Add ) to ending character
        private readonly Regex regexItemImageURL = new Regex(@"http[^*)]+");

        public const char FILENAME_SEPERATOR = '\n';

        #endregion

        public void Loaded()
        {
            //LoadDataFromFile(@"D:\DOWNLOAD\Shopee.html", workerMerge, 0, 1);
            //CreateHTML(@"D:\DOWNLOAD\test.html");
        }

        private string GetNode(string type, string nameClass)
        {
            return ".//" + type + "[@class='" + nameClass + "']";
        }

        private long GetNumberFromString(string s)
        {
            long result = 0;
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
            long.TryParse(s, out result);
            return result;
        }

        #region Button commands

        public void ButtonBrowseHTMLFiles()
        {
            OpenFileDialog dialog = new OpenFileDialog();
            dialog.Multiselect = true;
            dialog.InitialDirectory = Utils.GetValidFolderPath(Properties.Settings.Default.LastHTMLDirectory);
            dialog.Filter = "HTML Files|*.html;*.htm";
            if ((bool)dialog.ShowDialog())
            {
                string names = String.Join(FILENAME_SEPERATOR + "", dialog.FileNames);
                string directory = Utils.GetValidFolderPath(dialog.FileNames[0]);
                log.Info("Directory: " + directory + " | Selected: " + names);
                TxtHTMLFiles = names;
                Properties.Settings.Default.LastHTMLDirectory = directory;
                Properties.Settings.Default.Save();
            }
        }

        public void ButtonBrowseDatabaseFile()
        {
            OpenFileDialog dialog = new OpenFileDialog();
            dialog.InitialDirectory = Utils.GetValidFolderPath(Properties.Settings.Default.LastDatabaseDirectory);
            dialog.Filter = "JSON File|*.json";
            if ((bool)dialog.ShowDialog())
            {
                string directory = Utils.GetValidFolderPath(dialog.FileName);
                log.Info("Directory: " + directory + " | Selected: " + dialog.FileName);
                TxtDatabaseFile = dialog.FileName;
                Properties.Settings.Default.LastDatabaseDirectory = directory;
                Properties.Settings.Default.Save();
            }
        }

        #region Async function

        private CancellationTokenSource cts;
        private CancellationToken cancelToken;
        private IProgress<int> progressObject;
        private Task<int> taskMerge;

        #endregion

        public async void ButtonMerge()
        {
            if (taskMerge != null && taskMerge.Status.Equals(TaskStatus.Running))
            {
                //task is running
                log.Error("Current task is running.");
                if (cts != null)
                {
                    cts.Cancel();
                    log.Debug("Request to cancel task");
                }
                else
                {
                    log.Error("Cannot request to cancel task");
                }
            }
            else
            {
                //task is not running

                //declare cancellation token
                cts = new CancellationTokenSource();
                cancelToken = cts.Token;

                //handle progress report
                var progressHander = new Progress<int>(value =>
                {
                    ProgressValue = value;
                });
                progressObject = progressHander as IProgress<int>;

                //create task
                try
                {
                    IsTaskIdle = false;
                    taskMerge = Task.Run(() =>
                    {
                        if (IsUseDatabase)
                        {
                            ImportDatabase(TxtDatabaseFile);
                        }

                        string[] files = TxtHTMLFiles.Split(FILENAME_SEPERATOR);
                        for (int i = 0; i < files.Length; ++i)
                        {
                            if (File.Exists(files[i]))
                            {
                                try
                                {
                                    LoadDataFromFile(files[i], i, files.Length);
                                }
                                catch (Exception e1)
                                {
                                    log.Error("Cannot load data from file: " + files[i], e1);
                                }
                            }
                        }
                        ListOrders.Sort();
                        ListOrders.Reverse();
                        return 13;
                    });
                    await taskMerge;
                    log.Info("Task is completed. Number of orders: " + ListOrders.Count);
                }
                catch (OperationCanceledException)
                {
                    log.Debug("Operation is canceled");
                }
                catch (Exception e1)
                {
                    string s = e1.GetType().Name + ": " + (e1.Message ?? "No message");
                    log.Debug("Exception " + s);
                }
                finally
                {
                    ProgressValue = 0;
                    IsTaskIdle = true;
                }
            }
        }

        public void ButtonExportToJSON()
        {
            SaveFileDialog dialog = new SaveFileDialog();
            dialog.InitialDirectory = Utils.GetValidFolderPath(Properties.Settings.Default.LastDatabaseDirectory);
            dialog.Filter = "JSON File|*.json";
            if ((bool)dialog.ShowDialog())
            {
                string name = dialog.FileName;
                string directory = Utils.GetValidFolderPath(name);
                log.Info("Directory: " + directory + " | Exported to: " + name);
                Properties.Settings.Default.LastDatabaseDirectory = directory;
                Properties.Settings.Default.Save();
                File.WriteAllText(name, JsonConvert.SerializeObject(ListOrders, Formatting.Indented));
            }
        }

        public void ButtonExportToHTML()
        {
            SaveFileDialog dialog = new SaveFileDialog();
            dialog.InitialDirectory = Utils.GetValidFolderPath(Properties.Settings.Default.ExportedHTMLDirectory);
            dialog.Filter = "HTML File|*.html";
            if ((bool)dialog.ShowDialog())
            {
                string name = dialog.FileName;
                string directory = Utils.GetValidFolderPath(name);
                log.Info("Directory: " + directory + " | Export to: " + name);
                Properties.Settings.Default.ExportedHTMLDirectory = directory;
                Properties.Settings.Default.Save();

                //TODO: Export to HTML
                CreateHTML(name);

                //Save to database file
                if (IsUseDatabase && File.Exists(TxtDatabaseFile))
                {
                    string backupName = Path.Combine(Path.GetDirectoryName(TxtDatabaseFile), "Backup_" + DateTime.Now.ToString("yyyyMMdd_HHmmss") + ".json");
                    File.Copy(TxtDatabaseFile, backupName);
                    File.WriteAllText(TxtDatabaseFile, JsonConvert.SerializeObject(ListOrders, Formatting.Indented));
                }
            }
        }

        #endregion

        private void CreateHTML(string outputFile)
        {
            StringWriter stringWriter = new StringWriter();
            const int IMAGE_SIZE = 150;
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
                writer.WriteLine("<th>Item</th>");
                writer.WriteLine("<th width=\"70\">Price</th>");
                writer.WriteLine("<th width=\"125\">Order</th>");
                writer.WriteLine("<th width=\"120\">Shop</th>");
                writer.WriteLine("<th width=\"120\">User</th>");
                writer.RenderEndTag(); //end tr

                writer.RenderEndTag(); //end thead

                writer.RenderBeginTag(HtmlTextWriterTag.Tbody);
                int count = 0;
                foreach (var order in ListOrders)
                {
                    foreach (var item in order.ListItems)
                    {
                        writer.RenderBeginTag(HtmlTextWriterTag.Tr);

                        ++count;
                        writer.WriteLine("<td>" + count + "</td>");
                        writer.WriteLine(string.Format("<td><img class=\"item_image\" src=\"{0}\"/></td>", item.ImageURL));

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
        }

        private void ImportDatabase(string path)
        {
            try
            {
                List<OrderInfo> temp = JsonConvert.DeserializeObject<List<OrderInfo>>(File.ReadAllText(path));
                ListOrders.Clear();
                HashOrderID.Clear();
                foreach (OrderInfo order in temp)
                {
                    if (!HashOrderID.Contains(order.ID))
                    {
                        ListOrders.Add(order);
                        HashOrderID.Add(order.ID);
                    }
                }
            }
            catch (Exception e1)
            {
                log.Error("Cannot import database. " + (e1.Message ?? "No message"));
            }
        }

        private void LoadDataFromFile(string path, int currentFile, int numberOfFile)
        {
            int startProgress = currentFile * 100 / numberOfFile;
            var doc = new HtmlDocument();
            doc.Load(path);
            log.Info("HTML file is loaded: " + path);

            log.Info("Find wrapper div that contain all orders");
            HtmlNode wrapperDiv = doc.DocumentNode.SelectSingleNode(GetNode("div", "purchase-list-page__checkout-list-card-container"));
            int orderCount = wrapperDiv.ChildNodes.Count;
            log.Info("Found number of child node div: " + orderCount);

            List<OrderInfo> tempListOrder = new List<OrderInfo>();
            HtmlNode node = null;
            HtmlNode subNode = null;
            string username = string.Empty;

            log.Info("Find username");
            node = doc.DocumentNode.SelectSingleNode(GetNode("div", "navbar__username"));
            if (node != null)
            {
                username = node.InnerText.Trim();
                log.Info("Found username: " + username);
            }

            int currentIndex = 0;
            foreach (HtmlNode orderDiv in wrapperDiv.ChildNodes)
            {
                if (cancelToken != null)
                {
                    cancelToken.ThrowIfCancellationRequested();
                }

                OrderInfo order = new OrderInfo();
                order.UserName = username;
                progressObject.Report(startProgress + (currentIndex * 100 / numberOfFile / orderCount));
                ++currentIndex;

                log.Info("Find order URL and ID");
                node = orderDiv.SelectSingleNode(GetNode("a", "order-content__item-wrapper"));
                if (node != null)
                {
                    order.OrderURL = node.GetAttributeValue<string>("href", NONE_TEXT).Trim();
                    log.Info("Found order URL: " + order.OrderURL);
                    long.TryParse(regexOrderID.Match(order.OrderURL).Groups[1].Value, out order.ID);
                    log.Info("Found order ID: " + order.ID);
                }

                if (HashOrderID.Contains(order.ID))
                {
                    log.Info("This order is duplicated: " + order.ID);
                    continue;
                }

                log.Info("Find order total price");
                node = orderDiv.SelectSingleNode(GetNode("span", "purchase-card-buttons__total-price"));
                if (node != null)
                {
                    order.TotalPrice = GetNumberFromString(node.InnerText);
                    log.Info("Found order total price: " + order.TotalPrice);
                }

                log.Info("Find shop name");
                node = orderDiv.SelectSingleNode(GetNode("span", "order-content__header__seller__name"));
                if (node != null)
                {
                    order.ShopName = node.InnerText.Trim();
                    log.Info("Found shop name: " + order.ShopName);
                }

                log.Info("Find shop URL");
                node = orderDiv.SelectSingleNode(GetNode("a", "order-content__header__seller__view-shop-btn-wrapper"));
                if (node != null)
                {
                    order.ShopURL = node.GetAttributeValue<string>("href", NONE_TEXT);
                    log.Info("Found shop URL: " + order.ShopURL);
                    long.TryParse(regexShopID.Match(order.ShopURL).Groups[1].Value, out order.ShopID);
                    log.Info("Found shop ID: " + order.ShopID);
                }

                log.Info("Find shop image");
                node = orderDiv.SelectSingleNode(GetNode("img", "shopee-avatar__img"));
                if (node != null)
                {
                    order.ShopImageURL = node.GetAttributeValue<string>("data-savepage-src", NONE_TEXT);
                    log.Info("Found shop image URL: " + order.ShopImageURL);
                }

                log.Info("Get item nodes");
                HtmlNodeCollection itemNodes = orderDiv.SelectNodes(GetNode("a", "order-content__item-wrapper"));
                log.Info("Item nodes count: " + itemNodes.Count);
                foreach (var itemNode in itemNodes)
                {
                    if (cancelToken != null)
                    {
                        cancelToken.ThrowIfCancellationRequested();
                    }

                    ItemInfo item = new ItemInfo();

                    log.Info("Find item name");
                    node = itemNode.SelectSingleNode(GetNode("div", "order-content__item__name"));
                    if (node != null)
                    {
                        item.ItemName = node.InnerText.Trim();
                        log.Info("Found item name: " + item.ItemName);
                    }

                    log.Info("Find item details");
                    node = itemNode.SelectSingleNode(GetNode("div", "order-content__item__variation"));
                    if (node != null)
                    {
                        item.ItemDetails = node.InnerText.Substring(node.InnerText.IndexOf(':') + 1).Trim();
                        log.Info("Found item details: " + item.ItemDetails);
                    }

                    log.Info("Find item quantity");
                    node = itemNode.SelectSingleNode(GetNode("div", "order-content__item__quantity"));
                    if (long.TryParse(node.InnerText.Substring(node.InnerText.IndexOf('x') + 1).Trim(), out item.NumberOfItem))
                    {
                        log.Info("Found item quantily: " + item.NumberOfItem);
                    }

                    log.Info("Find actual price");
                    node = itemNode.SelectSingleNode(GetNode("div", "order-content__item__price-text"));
                    if (node != null)
                    {
                        subNode = node.SelectSingleNode(GetNode("div", "shopee-price--primary"));
                        if (subNode != null)
                        {
                            item.ActualPrice = GetNumberFromString(subNode.InnerText);
                            log.Info("Found actual price: " + item.ActualPrice);

                            log.Info("Find original price");
                            subNode = node.SelectSingleNode(GetNode("div", "shopee-price--original"));
                            if (subNode != null)
                            {
                                item.OriginalPrice = GetNumberFromString(subNode.InnerText);
                                log.Info("Found original price: " + item.OriginalPrice);
                            }
                        }
                        else
                        {
                            subNode = node.SelectSingleNode(".//div");
                            if (subNode != null)
                            {
                                item.ActualPrice = GetNumberFromString(subNode.InnerText);
                                item.OriginalPrice = item.ActualPrice;
                                log.Info("There's only 1 price: " + item.ActualPrice);
                            }
                        }
                    }

                    log.Info("Find item image");
                    node = itemNode.SelectSingleNode(GetNode("div", "shopee-image__content"));
                    if (node != null)
                    {
                        item.ImageURL = regexItemImageURL.Match(node.OuterHtml).Value.Trim();
                        log.Info("Found item image URL: " + item.ImageURL);
                    }

                    log.Info("Add item to list");
                    order.ListItems.Add(item);
                }
                tempListOrder.Add(order);
                HashOrderID.Add(order.ID);
            }
            ListOrders.AddRange(tempListOrder);
            log.Info("Complete parsing: " + path + " | Item count: " + ListOrders.Count);
        }
    }
}
