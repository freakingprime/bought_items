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

        public MainWindow()
        {
            InitializeComponent();
            AssignDataContext();
            this.Title = Properties.Resources.TITLE + " " + Properties.Resources.VERSION + "." + Properties.Resources.BuildTime;
        }

        private MainWindowVm context;

        private void AssignDataContext()
        {
            this.context = this.DataContext as MainWindowVm;
            if (context == null)
            {
                log.Error("Cannot get data context. Value is null.");
            }
        }

        private void Window_DataContextChanged(object sender, DependencyPropertyChangedEventArgs e)
        {
            log.Info("Main Window data context is changed");
            AssignDataContext();
        }

        private void Window_Loaded(object sender, RoutedEventArgs e)
        {
            
        }
    }
}
