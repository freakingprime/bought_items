using BoughtItems.UI_MainWindow.ViewModel;
using HtmlAgilityPack;
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

namespace BoughtItems
{
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindow : Window
    {
        private static readonly log4net.ILog log = log4net.LogManager.GetLogger(System.Reflection.MethodBase.GetCurrentMethod().DeclaringType.Name);
        private LogController oldLog = LogController.Instance;

        public MainWindow()
        {
            InitializeComponent();
            Title = Properties.Resources.TITLE + " " + Properties.Resources.VERSION + "." + Properties.Resources.BuildTime;
            oldLog.SetTextBox(TxtLog);
        }

        private MainWindowVm context = new MainWindowVm();

        private void Window_DataContextChanged(object sender, DependencyPropertyChangedEventArgs e)
        {
            log.Info("Main Window data context is changed");
            if (e.NewValue is MainWindowVm vm)
            {
                context = vm;
            }
        }

        private void TxtLog_TextChanged(object sender, TextChangedEventArgs e)
        {
            TxtLog.ScrollToEnd();
        }
    }
}
