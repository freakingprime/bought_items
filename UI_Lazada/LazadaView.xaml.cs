using Microsoft.Web.WebView2.Core;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Navigation;
using System.Windows.Shapes;

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
        }

        private async void CoreWebView2_WebResourceResponseReceived(object sender, CoreWebView2WebResourceResponseReceivedEventArgs e)
        {
            if (e.Request.Uri.Contains("order-list"))
            {
                log.Debug("Web resource received");
                var response = await e.Response.GetContentAsync();
            }
        }
    }
}
