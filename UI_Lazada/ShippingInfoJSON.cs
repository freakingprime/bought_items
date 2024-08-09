using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace BoughtItems.UI_Lazada.Shipping
{
    internal class ShippingInfoJSON
    {
    }
    // Root myDeserializedClass = JsonConvert.DeserializeObject<Root>(myJsonResponse);
    public class Delivery
    {
        public bool isShowIcon { get; set; }
        public object createdAt { get; set; }
        public OrderDetailPrice orderDetailPrice { get; set; }
        public string method { get; set; }
        public object deliverySummary { get; set; }
        public string price { get; set; }
        public string pckNum { get; set; }
        public object email { get; set; }
        public string desc { get; set; }
        public string status { get; set; }
    }

    public class Fields
    {
        public Delivery delivery { get; set; }
        public TrackingPackage trackingPackage { get; set; }
    }

    public class OrderDetailPrice
    {
        public bool async { get; set; }
        public string buttonText { get; set; }
        public bool input { get; set; }
        public bool submit { get; set; }
        public object bizName { get; set; }
        public object signature { get; set; }
        public Validator validator { get; set; }
        public object id { get; set; }
        public string title { get; set; }
        public string type { get; set; }
        public object parentId { get; set; }
        public List<Summary> summaries { get; set; }
    }

    public class Root
    {
        public string id { get; set; }
        public string tag { get; set; }
        public Fields fields { get; set; }
        public string type { get; set; }
    }

    public class Summary
    {
        public string value { get; set; }
        public bool isBold { get; set; }
        public string key { get; set; }
        public string valueColor { get; set; }
    }

    public class TrackingPackage
    {
        public string trackingTitle { get; set; }
        public string trackingUrl { get; set; }
        public string lastPackageInfo { get; set; }
        public string lastUpdateTimeDesc { get; set; }
    }

    public class Validator
    {
    }


}
