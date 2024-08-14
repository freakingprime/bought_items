using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace BoughtItems.UI_Merge
{
    public class OrderInfo : IComparable<OrderInfo>
    {
        public OrderInfo()
        {

        }

        #region Normal properties

        public string ID = "0";
        public string OrderURL = string.Empty;
        public long TotalPrice = 0;

        public string UserName = string.Empty;
        public string ShopName = string.Empty;
        public string ShopImageURL = string.Empty;
        public string ShopLocalImageName = string.Empty;
        public string ShopURL = string.Empty;

        public List<ItemInfo> ListItems = new List<ItemInfo>(10);
        #endregion

        public bool IsValid
        {
            get
            {
                return ID.Length > 0 && OrderURL.Length > 0 && UserName.Length > 0 && ShopName.Length > 0 && ShopURL.Length > 0;
            }
        }

        public int CompareTo(OrderInfo other)
        {
            return other.ID.CompareTo(ID);
        }

        public override string ToString()
        {
            return ID + " " + UserName + " " + TotalPrice;
        }
    }
}
