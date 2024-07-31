using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace BoughtItems.UI_Merge
{
    public class ItemInfo : IComparable<ItemInfo>
    {
        public ItemInfo()
        {

        }

        #region Normal properties

        public string ItemName = string.Empty;
        public string ItemDetails = string.Empty;
        public long ActualPrice = 0;
        public long OriginalPrice = 0;
        public long NumberOfItem = 0;
        public string ImageURL = string.Empty;
        public string LocalImageName = string.Empty;

        #endregion

        public bool IsValid
        {
            get
            {
                return (ItemName.Length > 0 || ItemDetails.Length > 0 || ImageURL.Length > 0) && LocalImageName.Length > 0;
            }
        }

        public override string ToString()
        {
            return ItemName + " " + ActualPrice + " x" + NumberOfItem;
        }

        public int CompareTo(ItemInfo other)
        {
            return ItemName.CompareTo(other.ItemName);
        }
    }
}
