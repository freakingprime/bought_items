using BoughtItems.UI_Merge.ViewModel;
using System;
using System.Collections.Generic;
using System.IO;
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

namespace BoughtItems.UI_Merge.View
{
    /// <summary>
    /// Interaction logic for MergeView.xaml
    /// </summary>
    public partial class MergeView : UserControl
    {
        private static readonly log4net.ILog log = log4net.LogManager.GetLogger(System.Reflection.MethodBase.GetCurrentMethod().DeclaringType.Name);

        public MergeView()
        {
            InitializeComponent();
            AssignDataContext();

            string[] split = Properties.Settings.Default.TxtHTMLFiles.Split(MergeVm.FILENAME_SEPERATOR);
            string names = "";
            foreach (var item in split)
            {
                if (File.Exists(item))
                {
                    names += item + MergeVm.FILENAME_SEPERATOR;
                }
            }
            names = names.Trim();
            TxtSelectedHTML.Text = names;

            if (File.Exists(Properties.Settings.Default.TxtDatabaseFile))
            {
                TxtDatabaseFile.Text = Properties.Settings.Default.TxtDatabaseFile;
            }

            CheckboxUseDatabase.IsChecked = true;
        }

        private MergeVm context;

        private void UserControl_DataContextChanged(object sender, DependencyPropertyChangedEventArgs e)
        {
            AssignDataContext();
        }

        private void AssignDataContext()
        {
            this.context = this.DataContext as MergeVm;
            if (context == null)
            {
                log.Error("Cannot get data context. Value is null.");
            }
        }

        private void UserControl_Loaded(object sender, RoutedEventArgs e)
        {
            AssignDataContext();
            if (context != null)
            {
                context.Loaded();
            }
        }

        private void BtnBrowseHTMLFiles_Click(object sender, RoutedEventArgs e)
        {
            context.ButtonBrowseHTMLFiles();
        }

        private void BtnBrowseDatabaseFile_Click(object sender, RoutedEventArgs e)
        {
            context.ButtonBrowseDatabaseFile();
        }

        private void TxtSelectedHTML_TextChanged(object sender, TextChangedEventArgs e)
        {
            Properties.Settings.Default.TxtHTMLFiles = TxtSelectedHTML.Text.Trim();
            Properties.Settings.Default.Save();
        }

        private void TxtDatabaseFile_TextChanged(object sender, TextChangedEventArgs e)
        {
            Properties.Settings.Default.TxtDatabaseFile = TxtDatabaseFile.Text.Trim();
            Properties.Settings.Default.Save();
        }

        private void BtnMerge_Click(object sender, RoutedEventArgs e)
        {
            context.ButtonMerge();
        }

        private void BtnExportToHTML_Click(object sender, RoutedEventArgs e)
        {
            context.ButtonExportToHTML();
        }

        private void BtnAutoLoad_Click(object sender, RoutedEventArgs e)
        {
            context.ButtonAutoLoad();
        }

        private void BtnDownloadImage_Click(object sender, RoutedEventArgs e)
        {
            context.ButtonDownloadImages();
        }

        private void BtnMoveImages_Click(object sender, RoutedEventArgs e)
        {
            context.ButtonMoveImages();
        }
    }
}
