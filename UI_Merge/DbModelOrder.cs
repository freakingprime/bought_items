using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace BoughtItems.UI_Merge
{
    public class DbModelOrder
    {
        public DbModelOrder() { }

        public string URL;
        public long TotalPrice;
        public string UserName;
        public string ShopName;
        public string ShopURL;
        public string Name;
        public string Detail;
        public string ImageURL;
        public string ImageData;
        public string CompressImageData;
        public long ActualPrice;
        public long OriginalPrice;
        public int Quantity;
        public string OrderID;
    }
}
