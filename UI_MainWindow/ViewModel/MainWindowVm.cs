using BoughtItems.MVVMBase;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace BoughtItems.UI_MainWindow.ViewModel
{
    public class MainWindowVm : ViewModelBase
    {
        private static readonly log4net.ILog log = log4net.LogManager.GetLogger(System.Reflection.MethodBase.GetCurrentMethod().DeclaringType.Name);

        public MainWindowVm()
        {
            log.Info("MainWindowVm is created");
        }
    }
}
