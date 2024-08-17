using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace BoughtItems.UI_Lazada.ShopInfo
{
    class ShopInfoJSON
    {
    }
    // Root myDeserializedClass = JsonConvert.DeserializeObject<Root>(myJsonResponse);
    public class Fields
    {
        public string statusColor { get; set; }
        public long tradeOrderId { get; set; }
        public string link { get; set; }
        public bool isRedMart { get; set; }
        public SellerInfo sellerInfo { get; set; }
        public long promotionId { get; set; }
        public List<object> badges { get; set; }
        public long sequence { get; set; }
        public long sellerId { get; set; }
        public string name { get; set; }
        public OrderInfo orderInfo { get; set; }
        public string logo { get; set; }
        public long shopId { get; set; }
        public string status { get; set; }
    }

    public class OrderInfo
    {
        public object combinedOrder { get; set; }
        public bool submit { get; set; }
        public object signature { get; set; }
        public object statusColor { get; set; }
        public object tradeOrderId { get; set; }
        public Validator validator { get; set; }
        public object payUrl { get; set; }
        public string type { get; set; }
        public object paymentDes { get; set; }
        public object tips { get; set; }
        public object guaranteeTip { get; set; }
        public object createdAt { get; set; }
        public object showDialog { get; set; }
        public object bizName { get; set; }
        public object linkAsButton { get; set; }
        public object id { get; set; }
        public object linkText { get; set; }
        public object linkColor { get; set; }
        public object parentId { get; set; }
        public bool async { get; set; }
        public bool input { get; set; }
        public object tipsColor { get; set; }
        public object checkoutIds { get; set; }
        public object bannerText { get; set; }
        public object linkDisable { get; set; }
        public object bannerIcon { get; set; }
        public object paidAt { get; set; }
        public object checkoutId { get; set; }
        public object oldTradeOrderId { get; set; }
        public object paymentDesColor { get; set; }
        public object status { get; set; }
    }

    public class ShopInfoRoot
    {
        public string id { get; set; }
        public string tag { get; set; }
        public Fields fields { get; set; }
        public string type { get; set; }
    }

    public class SellerInfo
    {
        public string imChatName { get; set; }
        public string IMUrl { get; set; }
        public object shopTags { get; set; }
        public bool submit { get; set; }
        public object signature { get; set; }
        public Validator validator { get; set; }
        public object shopName { get; set; }
        public object sellerStoreInfos { get; set; }
        public object shopUrl { get; set; }
        public object title { get; set; }
        public string type { get; set; }
        public object parentId { get; set; }
        public string accountId { get; set; }
        public bool async { get; set; }
        public bool input { get; set; }
        public string itemId { get; set; }
        public object sellerId { get; set; }
        public object bizName { get; set; }
        public object id { get; set; }
        public string imSwitch { get; set; }
        public string IMHost { get; set; }
        public string skuId { get; set; }
    }

    public class Validator
    {
    }


}
