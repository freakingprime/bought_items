using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace BoughtItems.UI_Merge.Model
{
    public class OrderInfo
    {
        public OrderInfo()
        {

        }

        #region Normal properties

        public long ID = 0;
        public long TotalPrice = 0;
        public List<long> ListItemID = new List<long>(10);

        #endregion
    }
}
