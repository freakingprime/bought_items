using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace BoughtItems.UI_Lazada.DetailInfo
{
    public class DetailInfoJSON
    {
    }
    // Root myDeserializedClass = JsonConvert.DeserializeObject<Root>(myJsonResponse);
    public class CreatedAt
    {
        public string value { get; set; }
        public string key { get; set; }
    }

    public class ExtraParam
    {
        public CreatedAt createdAt { get; set; }
        public string transactionSnapshotText { get; set; }
        public string viewText { get; set; }
        public List<InfoGroup> infoGroups { get; set; }
        public PaidAt paidAt { get; set; }
        public OrderSnapshot orderSnapshot { get; set; }
        public object orderMemo { get; set; }
        public List<PreSaleExtraParam> preSaleExtraParams { get; set; }
    }

    public class Fields
    {
        public string createdAt { get; set; }
        public List<object> orderLineIds { get; set; }
        public string total { get; set; }
        public List<Operation> operations { get; set; }
        public long tradeOrderId { get; set; }
        public string paidAt { get; set; }
        public ExtraParam extraParam { get; set; }
        public string linkText { get; set; }
        public string copy { get; set; }
        public string linkColor { get; set; }
        public string orderIdTitle { get; set; }
    }

    public class InfoGroup
    {
        public string value { get; set; }
        public string key { get; set; }
    }

    public class Operation
    {
        public object toastContent { get; set; }
        public object combinedOrder { get; set; }
        public object trackInfo { get; set; }
        public object deleteDialog { get; set; }
        public object shape { get; set; }
        public object selectTime { get; set; }
        public bool available { get; set; }
        public object link { get; set; }
        public object guiderTips { get; set; }
        public string type { get; set; }
        public object hasIncentive { get; set; }
        public object selectPackage { get; set; }
        public string bizParams { get; set; }
        public string sellerId { get; set; }
        public object showDialog { get; set; }
        public object sortFirst { get; set; }
        public object action { get; set; }
        public object pickUpInStoreField { get; set; }
        public object invoice { get; set; }
        public object text { get; set; }
        public string btnType { get; set; }
        public string btn { get; set; }
        public object confirmDialog { get; set; }
    }

    public class OrderSnapshot
    {
        public bool submit { get; set; }
        public object signature { get; set; }
        public long tradeOrderId { get; set; }
        public Validator validator { get; set; }
        public string title { get; set; }
        public string type { get; set; }
        public object parentId { get; set; }
        public bool async { get; set; }
        public bool input { get; set; }
        public object bizName { get; set; }
        public object id { get; set; }
        public string text { get; set; }
        public List<Summary> summaries { get; set; }
    }

    public class PaidAt
    {
        public string value { get; set; }
        public string key { get; set; }
    }

    public class PreSaleExtraParam
    {
        public string value { get; set; }
        public string key { get; set; }
    }

    public class DetailInfoRoot
    {
        public string id { get; set; }
        public string tag { get; set; }
        public Fields fields { get; set; }
        public string type { get; set; }
    }

    public class Summary
    {
        public string buttonText { get; set; }
        public string picUrl { get; set; }
        public long quantity { get; set; }
        public string quantityPrefix { get; set; }
        public string itemTitle { get; set; }
        public string itemPrice { get; set; }
        public string buttonUrl { get; set; }
        public string skuInfo { get; set; }
        public object tradeOrderLineId { get; set; }
    }

    public class Validator
    {
    }


}