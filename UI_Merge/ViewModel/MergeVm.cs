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
using System.Net;
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
        private static readonly LogController oldLog = LogController.Instance;

        public MergeVm()
        {
            IsTaskIdle = true;
            DownloadButtonEnabled = true;
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

        private bool _downloadButtonEnabled;

        public bool DownloadButtonEnabled
        {
            get { return _downloadButtonEnabled; }
            set { SetValue(ref _downloadButtonEnabled, value); }
        }


        #endregion

        #region Normal properties

        private const string IMAGE_FOLDER_NAME = "images";

        private readonly List<OrderInfo> ListOrders = new List<OrderInfo>();
        private readonly HashSet<long> HashOrderID = new HashSet<long>();

        private const string NONE_TEXT = "None";
        private readonly Regex regexOrderID = new Regex(@"\/order\/(\d+)");
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

        #region Button commands

        public void ButtonBrowseHTMLFiles()
        {
            OpenFileDialog dialog = new OpenFileDialog
            {
                Multiselect = true,
                InitialDirectory = Utils.GetValidFolderPath(Properties.Settings.Default.LastHTMLDirectory),
                Filter = "HTML Files|*.html;*.htm"
            };
            if ((bool)dialog.ShowDialog())
            {
                string names = string.Join(FILENAME_SEPERATOR + "", dialog.FileNames);
                string directory = Utils.GetValidFolderPath(dialog.FileNames[0]);
                log.Info("Directory: " + directory + " | Selected: " + names);
                TxtHTMLFiles = names;
                Properties.Settings.Default.LastHTMLDirectory = directory;
                Properties.Settings.Default.Save();
            }
        }

        public void ButtonBrowseDatabaseFile()
        {
            OpenFileDialog dialog = new OpenFileDialog
            {
                InitialDirectory = Utils.GetValidFolderPath(Properties.Settings.Default.LastDatabaseDirectory),
                Filter = "JSON File|*.json"
            };
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
                log.Info("Current task is running");
                if (cts != null)
                {
                    cts.Cancel();
                    oldLog.Debug("Cancelling merge task");
                }
                else
                {
                    oldLog.Error("Cannot request to cancel task");
                }
            }
            else
            {
                //task is not running

                //declare cancellation token
                cts = new CancellationTokenSource();
                cancelToken = cts.Token;

                //handle progress report
                progressObject = new Progress<int>(value =>
                {
                    ProgressValue = value;
                });

                try
                {
                    IsTaskIdle = false;
                    oldLog.Debug("Start merging");
                    int beforeCount = 0;
                    taskMerge = Task.Run(() =>
                    {
                        if (IsUseDatabase)
                        {
                            ImportDatabase(TxtDatabaseFile);
                            beforeCount = ListOrders.Count;
                        }
                        string[] files = TxtHTMLFiles.Split(FILENAME_SEPERATOR);
                        for (int i = 0; i < files.Length; ++i)
                        {
                            files[i] = files[i].Trim();
                            if (File.Exists(files[i]))
                            {
                                try
                                {
                                    LoadDataFromFile(files[i], i, files.Length);
                                }
                                catch (Exception e1)
                                {
                                    oldLog.Error("Cannot load data from file: " + files[i]);
                                    oldLog.Error(e1.ToString());
                                }
                            }
                        }
                        ListOrders.Sort();
                        ListOrders.Reverse();
                        ButtonMoveImages();
                        return 0;
                    });
                    _ = await taskMerge;
                    oldLog.Debug("Task is completed. Number of orders: " + beforeCount + " -> " + ListOrders.Count + ", change: " + (ListOrders.Count - beforeCount));
                }
                catch (OperationCanceledException)
                {
                    oldLog.Debug("Operation is canceled");
                }
                catch (Exception e1)
                {
                    string s = e1.GetType().Name + ": " + (e1.Message ?? "No message");
                    oldLog.Error("Exception " + s);
                }
                finally
                {
                    ProgressValue = 0;
                    IsTaskIdle = true;
                }
            }
        }

        public void ButtonMoveImages()
        {
            const int MAX_FILE = 200;
            DirectoryInfo di = new DirectoryInfo(Path.Combine(Path.GetDirectoryName(TxtDatabaseFile), IMAGE_FOLDER_NAME));
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

        public async void ButtonDownloadImages()
        {
            ImportDatabase(TxtDatabaseFile);
            DirectoryInfo di = new DirectoryInfo(Path.Combine(Path.GetDirectoryName(TxtDatabaseFile), IMAGE_FOLDER_NAME));
            if (!di.Exists)
            {
                di.Create();
            }

            const int NUMBER_OF_THREAD = 8;
            int globalIndex = -1;
            int orderCount = ListOrders.Count;
            List<Task<List<ImageInfo>>> listTasks = new List<Task<List<ImageInfo>>>();
            DownloadButtonEnabled = false;
            HashSet<string> hashNames = new HashSet<string>(di.GetFiles("*", SearchOption.AllDirectories).Select(i => i.Name).ToList());
            for (int i = 0; i < NUMBER_OF_THREAD; ++i)
            {
                int threadIndex = i;
                listTasks.Add(Task.Run(() =>
                {
                    WebClient wc = new WebClient();
                    int k = Interlocked.Increment(ref globalIndex);
                    List<ImageInfo> result = new List<ImageInfo>();
                    while (k < orderCount)
                    {
                        foreach (ItemInfo item in ListOrders[k].ListItems)
                        {
                            if (hashNames.Contains(item.LocalImageName))
                            {
                                continue;
                            }
                            ImageInfo info = new ImageInfo
                            {
                                URL = item.ImageURL
                            };
                            string localPath = DownloadImage(wc, info.URL, Path.Combine(di.FullName, item.LocalImageName));
                            if (localPath.Length > 0)
                            {
                                info.LocalImagePath = localPath;
                                result.Add(info);
                            }
                        }
                        k = Interlocked.Increment(ref globalIndex);
                    }
                    return result;
                }));
            }
            var allResults = await Task.WhenAll(listTasks.ToArray());
            List<ImageInfo> finalResult = new List<ImageInfo>();
            foreach (var ret in allResults)
            {
                finalResult.AddRange(ret);
            }
            DownloadButtonEnabled = true;
            log.Info("Number of newly downloaded image files: " + finalResult.Count);
            ButtonMoveImages();
        }

        private string DownloadImage(WebClient wc, string url, string path)
        {
            string result = string.Empty;
            if (!File.Exists(path))
            {
                try
                {
                    wc.DownloadFile(url, path);
                }
                catch (Exception e1)
                {
                    log.Error("Cannot download image: " + url, e1);
                }
                if (File.Exists(path))
                {
                    log.Info("Downloaded: " + url + " to " + path);
                    result = path;
                }
            }
            return result;
        }

        public void ButtonAutoLoad()
        {
            string dirPath = Properties.Settings.Default.LastHTMLDirectory;
            if (Directory.Exists(dirPath))
            {
                TxtHTMLFiles = string.Empty;
                foreach (var item in Directory.GetFiles(dirPath))
                {
                    if (item.EndsWith("html"))
                    {
                        TxtHTMLFiles += item + Environment.NewLine;
                    }
                }
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

                const string BACKUP_FOLDER = "backup";
                _ = Directory.CreateDirectory(Path.Combine(directory, BACKUP_FOLDER));

                //backup HTML file
                string backupName = Path.GetFileNameWithoutExtension(name);
                backupName = backupName + "_" + DateTime.Now.ToString("yyyyMMdd_HHmmss") + Path.GetExtension(name);
                backupName = Path.Combine(Path.GetDirectoryName(name), BACKUP_FOLDER, backupName);
                if (File.Exists(name))
                {
                    File.Copy(name, backupName, true);
                }

                CreateHTML(name);
                string offlineHTMLName = name.Replace(".html", "_offline.html");
                CreateOfflineHTML(offlineHTMLName);
                oldLog.Debug("Exported to HTML file: " + name + " and " + offlineHTMLName);

                //Save to database file
                if (IsUseDatabase && File.Exists(TxtDatabaseFile))
                {
                    //backup JSON file
                    backupName = Path.GetFileNameWithoutExtension(TxtDatabaseFile);
                    backupName = backupName + "_" + DateTime.Now.ToString("yyyyMMdd_HHmmss") + Path.GetExtension(TxtDatabaseFile);
                    backupName = Path.Combine(Path.GetDirectoryName(TxtDatabaseFile), BACKUP_FOLDER, backupName);
                    File.Copy(TxtDatabaseFile, backupName);

                    ClearImageURLFromIllegalCharacters();
                    File.WriteAllText(TxtDatabaseFile, JsonConvert.SerializeObject(ListOrders, Formatting.Indented));
                }
            }
        }

        private void ClearImageURLFromIllegalCharacters()
        {
            foreach (OrderInfo order in ListOrders)
            {
                foreach (ItemInfo item in order.ListItems)
                {
                    if (item.ImageURL.Contains(")"))
                    {
                        item.ImageURL = item.ImageURL.Remove(item.ImageURL.IndexOf(")"));
                    }
                    if (item.LocalImageName.Length < 2)
                    {
                        item.LocalImageName = item.ImageURL.Substring(item.ImageURL.LastIndexOf("/") + 1) + ".jpg";
                    }
                }
            }
        }

        #endregion

        private void CreateOfflineHTML(string outputFile)
        {
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
                DirectoryInfo di = new DirectoryInfo(Path.Combine(Path.GetDirectoryName(TxtDatabaseFile), IMAGE_FOLDER_NAME));
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
                DirectoryInfo di = new DirectoryInfo(Path.Combine(Path.GetDirectoryName(TxtDatabaseFile), IMAGE_FOLDER_NAME));
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
            HtmlNodeCollection orderDivs = doc.DocumentNode.SelectNodes(GetNode("div", "hiXKxx"));
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
            WebClient wc = new WebClient();
            string imageFolderPath = Path.Combine(Path.GetDirectoryName(TxtDatabaseFile), IMAGE_FOLDER_NAME);
            var di = Directory.CreateDirectory(imageFolderPath);
            var listImageFileInfo = di.GetFiles("*.jpg", SearchOption.AllDirectories);
            Dictionary<string, string> dictImages = new Dictionary<string, string>();
            foreach (var fi in listImageFileInfo)
            {
                dictImages[fi.Name] = fi.FullName;
            }

            int currentIndex = 0;
            foreach (HtmlNode orderDiv in orderDivs)
            {
                if (cancelToken != null)
                {
                    cancelToken.ThrowIfCancellationRequested();
                }
                progressObject.Report(startProgress + (currentIndex * 100 / numberOfFile / orderCount));
                ++currentIndex;

                OrderInfo order = new OrderInfo
                {
                    UserName = username
                };

                // ".//" + type + "[@class='" + nameClass + "']"
                log.Info("Find order URL and ID");
                node = orderDiv.SelectSingleNode(".//div[@class='" + "qP6Mvo" + "']/a");
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
                node = orderDiv.SelectSingleNode(GetNode("div", "DeWpya"));
                if (node != null)
                {
                    order.TotalPrice = GetNumberFromString(node.InnerText);
                    log.Info("Found order total price: " + order.TotalPrice);
                }

                log.Info("Find shop name");
                node = orderDiv.SelectSingleNode(GetNode("div", "_9Ro5mP"));
                if (node != null)
                {
                    order.ShopName = node.InnerText.Trim();
                    log.Info("Found shop name: " + order.ShopName);
                }

                log.Info("Find shop URL");
                node = orderDiv.SelectSingleNode(GetNode("a", "_7wKGws"));
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
                HtmlNodeCollection itemNodes = orderDiv.SelectNodes(GetNode("span", "x7nENX"));
                if (itemNodes == null)
                {
                    log.Error("Cannot get item node list");
                }
                else
                {
                    log.Info("Item nodes count: " + itemNodes.Count);
                    foreach (var itemNode in itemNodes)
                    {
                        if (cancelToken != null)
                        {
                            cancelToken.ThrowIfCancellationRequested();
                        }
                        try
                        {
                            ItemInfo item = new ItemInfo();

                            log.Info("Find item name");
                            node = itemNode.SelectSingleNode(GetNode("span", "x5GTyN"));
                            if (node != null)
                            {
                                item.ItemName = node.InnerText.Trim();
                                log.Info("Found item name: " + item.ItemName);
                            }

                            log.Info("Find item details");
                            node = itemNode.SelectSingleNode(GetNode("div", "vb0b-P"));
                            if (node != null)
                            {
                                item.ItemDetails = node.InnerText.Substring(node.InnerText.IndexOf(':') + 1).Trim();
                                log.Info("Found item details: " + item.ItemDetails);
                            }

                            log.Info("Find item quantity");
                            node = itemNode.SelectSingleNode(GetNode("div", "_3F1-5M"));
                            if (node != null && long.TryParse(node.InnerText.Substring(node.InnerText.IndexOf('x') + 1).Trim(), out item.NumberOfItem))
                            {
                                log.Info("Found item quantily: " + item.NumberOfItem);
                            }

                            log.Info("Find actual price");
                            node = itemNode.SelectSingleNode(GetNode("div", "_9UJGhr"));
                            if (node != null)
                            {
                                HtmlNode subNode = node.SelectSingleNode(GetNode("span", "-x3Dqh OkfGBc"));
                                if (subNode != null)
                                {
                                    item.ActualPrice = GetNumberFromString(subNode.InnerText);
                                    log.Info("Found actual price: " + item.ActualPrice);

                                    log.Info("Find original price");
                                    subNode = node.SelectSingleNode(GetNode("span", "j2En5+"));
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
                            node = itemNode.SelectSingleNode(GetNode("div", "shopee-image__content"));
                            if (node != null)
                            {
                                item.ImageURL = regexItemImageURL.Match(node.OuterHtml).Value.Trim();

                                //find local image file before downloading
                                item.LocalImageName = item.ImageURL.Substring(item.ImageURL.LastIndexOf("/") + 1) + ".jpg";
                                if (!dictImages.TryGetValue(item.LocalImageName, out string localPath))
                                {
                                    _ = DownloadImage(wc, item.ImageURL, Path.Combine(imageFolderPath, item.LocalImageName));
                                }

                                log.Info("Found item image URL: " + item.ImageURL);
                            }

                            log.Info("Add item to list");
                            order.ListItems.Add(item);
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

                lock (ListOrders)
                {
                    lock (HashOrderID)
                    {
                        if (HashOrderID.Add(order.ID))
                        {
                            //new order
                            string mess = "Add new: " + order.ID + " " + order.UserName;
                            if (order.ListItems.Count > 0)
                            {
                                mess += " (" + order.ListItems[0].ItemName + ", ...)";
                            }
                            oldLog.Debug(mess);
                        }
                        else
                        {
                            //old order
                            //2021.09.27: Override old order with new order from HTML
                            log.Info("Override order: " + order.ID);
                            int oldIndex = ListOrders.FindIndex(i => i.ID == order.ID);
                            if (oldIndex >= 0)
                            {
                                ListOrders.RemoveAt(oldIndex);
                            }
                        }
                    }
                    ListOrders.Add(order);
                }
            }
            ButtonMoveImages();
            log.Info("Complete parsing: " + path + " | Item count: " + ListOrders.Count);
        }
    }
}
