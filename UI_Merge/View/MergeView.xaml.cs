using BoughtItems.UI_Merge.ViewModel;
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
    }
}
