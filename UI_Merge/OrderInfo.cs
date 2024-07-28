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

        public long ID = 0;
        public string OrderURL = string.Empty;
        public long TotalPrice = 0;

        public string UserName = string.Empty;
        public long ShopID = 0;
        public string ShopName = string.Empty;
        public string ShopImageURL = string.Empty;
        public string ShopLocalImageName = string.Empty;
        public string ShopURL = string.Empty;

        public List<ItemInfo> ListItems = new List<ItemInfo>(10);
        #endregion

        public int CompareTo(OrderInfo other)
        {
            return ID.CompareTo(other.ID);
        }

        public override string ToString()
        {
            return ID + " " + UserName + " " + TotalPrice;
        }
    }
}
