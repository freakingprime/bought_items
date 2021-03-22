using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace BoughtItems.UI_Merge.Model
{
    public class ItemInfo
    {
        private static readonly log4net.ILog log = log4net.LogManager.GetLogger(System.Reflection.MethodBase.GetCurrentMethod().DeclaringType.Name);

        public ItemInfo()
        {

        }

        #region Normal properties

        public long ID = 0;
        public string ItemName = string.Empty;
        public string ItemDetails = string.Empty;
        public long ActualPrice = 0;
        public long OriginalPrice = 0;
        public long NumberOfItem = 0;
        public string ImageURL = string.Empty;
        public string LocalImageName = string.Empty;

        public string ShopName = string.Empty;
        public string ShopImageURL = string.Empty;
        public string ShopLocalImageName = string.Empty;
        public string ShopURL = string.Empty;
        public string ShopID = string.Empty;
        public string OrderURL = string.Empty;

        #endregion
    }
}
