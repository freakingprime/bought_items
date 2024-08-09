using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace BoughtItems.UI_Lazada.OrderItem
{
    internal class OrderItemJSON
    {

    }

    // Root myDeserializedClass = JsonConvert.DeserializeObject<Root>(myJsonResponse);
    public class BadgeList
    {
        public object bgColor { get; set; }
        public string color { get; set; }
        public object icon { get; set; }
        public string text { get; set; }
        public string type { get; set; }
    }

    public class Delivery
    {
        public object isShowIcon { get; set; }
        public object createdAt { get; set; }
        public object orderDetailPrice { get; set; }
        public string method { get; set; }
        public object deliverySummary { get; set; }
        public object price { get; set; }
        public object pckNum { get; set; }
        public string email { get; set; }
        public object desc { get; set; }
        public string status { get; set; }
    }

    public class DialogInfo
    {
        public string link { get; set; }
        public string text { get; set; }
        public string type { get; set; }
    }

    public class External
    {
    }

    public class Fields
    {
        public string itemType { get; set; }
        public List<object> bizIcons { get; set; }
        public bool paymentPendingCancel { get; set; }
        public string actualPrice { get; set; }
        public string statusColor { get; set; }
        public string bizCode { get; set; }
        public string buyerEmail { get; set; }
        public bool isFreeGift { get; set; }
        public string groupId { get; set; }
        public string tradeOrderId { get; set; }
        public bool isNonPdp { get; set; }
        public List<BadgeList> badgeList { get; set; }
        public string title { get; set; }
        public string scene { get; set; }
        public string picUrl { get; set; }
        public bool queryReviewApiSuccess { get; set; }
        public bool indentation { get; set; }
        public string sellerId { get; set; }
        public string price { get; set; }
        public Reversible reversible { get; set; }
        public string warranty { get; set; }
        public Sku sku { get; set; }
        public string skuId { get; set; }
        public Delivery delivery { get; set; }
        public int quantity { get; set; }
        public bool disableLink { get; set; }
        public string itemId { get; set; }
        public int sequence { get; set; }
        public External external { get; set; }
        public bool reviewable { get; set; }
        public string quantityPrefix { get; set; }
        public string itemUrl { get; set; }
        public bool isFreeSample { get; set; }
        public string status { get; set; }
    }

    public class Reversible
    {
        public object useNewFunction { get; set; }
        public object combinedOrder { get; set; }
        public object needRequest { get; set; }
        public object paymentPendingCancel { get; set; }
        public string reverseOrderId { get; set; }
        public string tradeOrderId { get; set; }
        public string link { get; set; }
        public object requestParams { get; set; }
        public object closingAt { get; set; }
        public object returnDialog { get; set; }
        public bool button { get; set; }
        public object disableDialog { get; set; }
        public object returnOld { get; set; }
        public bool action { get; set; }
        public List<DialogInfo> dialogInfo { get; set; }
        public string step { get; set; }
        public string tradeOrderLineId { get; set; }
        public string desc { get; set; }
        public string status { get; set; }
    }

    public class Root
    {
        public string id { get; set; }
        public string tag { get; set; }
        public Fields fields { get; set; }
        public string type { get; set; }
    }

    public class Sku
    {
        public string skuText { get; set; }
        public object featureText { get; set; }
        public object productVariant { get; set; }
        public object brandId { get; set; }
        public object fromPage { get; set; }
        public object brand { get; set; }
        public object alertDialog { get; set; }
        public string skuId { get; set; }
    }
}
