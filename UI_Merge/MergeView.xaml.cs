using Microsoft.Win32;
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

namespace BoughtItems.UI_Merge
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

            string[] split = Properties.Settings.Default.HtmlFiles.Split(MergeVm.FILENAME_SEPERATOR);
            string names = "";
            foreach (var item in split)
            {
                if (File.Exists(item))
                {
                    names += item + MergeVm.FILENAME_SEPERATOR;
                }
            }
            names = names.Trim();
            TxtHtmlFiles.Text = names;

            if (File.Exists(Properties.Settings.Default.DatabasePath))
            {
                TxtDatabaseFile.Text = Properties.Settings.Default.DatabasePath;
            }

            CheckboxUseDatabase.IsChecked = true;
            BtnAutoLoad_Click(null, null);
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
            OpenFileDialog dialog = new OpenFileDialog
            {
                Multiselect = true,
                InitialDirectory = Utils.GetValidFolderPath(Properties.Settings.Default.LastHTMLDirectory),
                Filter = "HTML Files|*.html;*.htm"
            };
            if ((bool)dialog.ShowDialog())
            {
                string names = string.Join("\n", dialog.FileNames);
                string directory = Utils.GetValidFolderPath(dialog.FileNames[0]);
                log.Info("Directory: " + directory + " | Selected: " + names);
                TxtHtmlFiles.Text = names;
                Properties.Settings.Default.LastHTMLDirectory = directory;
                Properties.Settings.Default.Save();
            }
        }

        private void BtnBrowseDatabaseFile_Click(object sender, RoutedEventArgs e)
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
                TxtDatabaseFile.Text = dialog.FileName;
                Properties.Settings.Default.LastDatabaseDirectory = directory;
                Properties.Settings.Default.Save();
            }
        }

        private void TxtSelectedHTML_TextChanged(object sender, TextChangedEventArgs e)
        {
            Properties.Settings.Default.HtmlFiles = ((TextBox)sender).Text.Trim();
            Properties.Settings.Default.Save();
        }

        private void TxtDatabaseFile_TextChanged(object sender, TextChangedEventArgs e)
        {
            Properties.Settings.Default.DatabasePath = ((TextBox)sender).Text.Trim();
            Properties.Settings.Default.LastDatabaseDirectory = Utils.GetValidFolderPath(((TextBox)sender).Text.Trim());
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
            string dirPath = Properties.Settings.Default.LastHTMLDirectory;
            if (Directory.Exists(dirPath))
            {
                string paths = string.Empty;
                foreach (var item in Directory.GetFiles(dirPath))
                {
                    if (item.EndsWith("html"))
                    {
                        paths += item + Environment.NewLine;
                    }
                }
                TxtHtmlFiles.Text = paths.Trim();
            }
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
