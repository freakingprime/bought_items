using Microsoft.VisualBasic;
using Microsoft.Web.WebView2.Core;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net.Http;
using System.Net;
using System.Net.Sockets;
using System.Security.Policy;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Navigation;

namespace BoughtItems.UI_Lazada
{
    /// <summary>
    /// Interaction logic for LazadaView.xaml
    /// </summary>
    public partial class LazadaView : UserControl
    {
        private static readonly log4net.ILog log = log4net.LogManager.GetLogger(System.Reflection.MethodBase.GetCurrentMethod().DeclaringType.Name);
        private static readonly LogController oldLog = LogController.Instance;
        public LazadaView()
        {
            InitializeComponent();
        }

        private LazadaVm _context = null;

        private void UserControl_DataContextChanged(object sender, DependencyPropertyChangedEventArgs e)
        {
            if (this.DataContext is LazadaVm vm)
            {
                _context = vm;
            }
        }

        private void BtnLazada_Click(object sender, RoutedEventArgs e)
        {
            _context?.ButtonLazada();
        }

        private async void MainWebView_Initialized(object sender, EventArgs e)
        {
            log.Debug("Main is initalized");
            await MainWebView.EnsureCoreWebView2Async();
            MainWebView.Source = new Uri(@"https://my.lazada.vn/customer/order/index/?spm=a2o42.order_details.0.0.13e5705bOajZVb");
        }

        private void MainWebView_CoreWebView2InitializationCompleted(object sender, Microsoft.Web.WebView2.Core.CoreWebView2InitializationCompletedEventArgs e)
        {
            log.Debug("Core is initialized");
            string filter = "*order-list*";   // or "*" for all requests
            MainWebView.CoreWebView2.AddWebResourceRequestedFilter(filter, CoreWebView2WebResourceContext.All);
            MainWebView.CoreWebView2.WebResourceResponseReceived += CoreWebView2_WebResourceResponseReceived;
            MainWebView.CoreWebView2.NavigationCompleted += CoreWebView2_NavigationCompleted;
        }

        private async void CoreWebView2_NavigationCompleted(object sender, CoreWebView2NavigationCompletedEventArgs e)
        {
            var core = (CoreWebView2)sender;

            //cookie
            List<CoreWebView2Cookie> listCookie = await core.CookieManager.GetCookiesAsync(@"lazada.vn");
            string temp = string.Join(";", listCookie.Select(i => i.Name + "=" + i.Value));
            if (!temp.Equals(Properties.Settings.Default.LazadaCookie))
            {
                Properties.Settings.Default.LazadaCookie = temp;
                Properties.Settings.Default.Save();
                oldLog.Debug("New cookie is updated");
            }
            else
            {
                oldLog.Debug("Keep old cookies");
            }

            //user agent
            const string USER_AGENT_DOMAIN = "whatismybrowser.com";
            if (core.Source.Contains(USER_AGENT_DOMAIN, StringComparison.OrdinalIgnoreCase))
            {
                string useragent = await core.ExecuteScriptAsync("document.querySelector(\"a.user_agent\").innerHTML");
                if (!"null".Equals(useragent))
                {
                    useragent = useragent.Trim().Substring(1);
                    useragent = useragent.Remove(useragent.Length - 1);
                    if (!useragent.Equals(Properties.Settings.Default.UserAgent))
                    {
                        Properties.Settings.Default.UserAgent = useragent;
                        Properties.Settings.Default.Save();
                        oldLog.Debug("New user agent: " + useragent);
                    }
                    else
                    {
                        oldLog.Debug("Keep old user agent");
                    }
                }
            }
        }

        private async void CoreWebView2_WebResourceResponseReceived(object sender, CoreWebView2WebResourceResponseReceivedEventArgs e)
        {
            if (e.Request.Uri.Contains("order-list"))
            {
                log.Debug("Web resource received");
                var sr = new StreamReader(await e.Response.GetContentAsync());
                _context?.ParseOrderList(sr.ReadToEnd());
            }
        }

        private void BtnGoLazada_Click(object sender, RoutedEventArgs e)
        {
            MainWebView.Source = new Uri(@"https://my.lazada.vn/customer/order/index/?spm=a2o42.order_details.0.0.13e5705bOajZVb");
        }

        private void BtnGoUserAgent_Click(object sender, RoutedEventArgs e)
        {
            MainWebView.Source = new Uri(@"https://www.whatismybrowser.com/");
        }
    }
}
