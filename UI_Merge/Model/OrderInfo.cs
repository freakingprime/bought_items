using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace BoughtItems.UI_Merge.Model
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

        public int CompareTo(OrderInfo other)
        {
            if (this.UserName.Equals(other.UserName))
            {
                return this.ID.CompareTo(other.ID);
            }
            return this.UserName.CompareTo(other.UserName);
        }

        #endregion
    }
}
