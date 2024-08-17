using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace BoughtItems.UI_Merge
{
    internal class DbModelItem
    {
        public DbModelItem()
        {
        }

        public long ID = 0;
        public string Name = "";
        public string Detail = "";
        public string ImageURL = "";
        public string ImageData = "";
        public string CompressImageData = "";
    }
}
