using BoughtItems.MVVMBase;
using BoughtItems.UI_Merge.Model;
using HtmlAgilityPack;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;

namespace BoughtItems.UI_Merge.ViewModel
{
    public class MergeVm : ViewModelBase
    {
        private static readonly log4net.ILog log = log4net.LogManager.GetLogger(System.Reflection.MethodBase.GetCurrentMethod().DeclaringType.Name);

        public MergeVm()
        {

        }

        #region Normal properties

        private List<ItemInfo> ListItems = new List<ItemInfo>();
        private List<OrderInfo> ListOrders = new List<OrderInfo>();

        private const string NONE_TEXT = "None";
        private readonly Regex regexOrderID = new Regex(@"\/order\/(\d+)\/");

        #endregion

        public void Loaded()
        {
            var doc = new HtmlDocument();
            doc.Load(@"D:\DOWNLOAD\Shopee.html");
            log.Info("HTML file is loaded");
            try
            {
                log.Info("Find wrapper div that contain all orders");
                HtmlNode wrapperDiv = doc.DocumentNode.SelectSingleNode(GetNode("div", "purchase-list-page__checkout-list-card-container"));
                log.Info("Found. Number of child node div: " + wrapperDiv.ChildNodes.Count);

                List<ItemInfo> tempListItem = new List<ItemInfo>();
                List<OrderInfo> tempListOrder = new List<OrderInfo>();
                HtmlNode node = null;

                foreach (HtmlNode orderDiv in wrapperDiv.ChildNodes)
                {
                    log.Info("Find order ID");
                    node = orderDiv.SelectSingleNode(GetNode("a", "order-content__item-wrapper").Substring(1));
                    string url = node.GetAttributeValue<string>("href", NONE_TEXT).Trim();
                    OrderInfo order = new OrderInfo();
                    long.TryParse(regexOrderID.Match(url).Groups[1].Value, out order.ID);
                    log.Info("Found. Order ID: " + order.ID);

                    log.Info("Find order total price");
                    node = orderDiv.SelectSingleNode(GetNode("span", "purchase-card-buttons__total-price"));
                    string strTotalPrice = node.InnerText;
                    int i = 0;
                    while (i < strTotalPrice.Length)
                    {
                        if (strTotalPrice[i] < '0' || strTotalPrice[i] > '9')
                        {
                            strTotalPrice = strTotalPrice.Remove(i, 1);
                        }
                        else
                        {
                            ++i;
                        }
                    }
                    long.TryParse(strTotalPrice, out order.TotalPrice);
                    log.Info("Found. Order total price: " + order.TotalPrice);

                    log.Info("Find shop name");
                    node = orderDiv.SelectSingleNode(GetNode("span", "order-content__header__seller__name"));
                    string shopName = node.InnerText.Trim();
                    log.Info("Found. Shop name: " + shopName);

                    log.Info("Find shop URL");
                    node = orderDiv.SelectSingleNode(GetNode("a", "order-content__header__seller__view-shop-btn-wrapper"));
                    string shopURL = node.GetAttributeValue<string>("href", NONE_TEXT);
                    log.Info("Found. Shop URL: " + shopURL);

                    log.Info("Find shop image");
                    node = orderDiv.SelectSingleNode(GetNode("img", "shopee-avatar__img"));
                    string shopImageURL = node.GetAttributeValue<string>("data-savepage-src", NONE_TEXT);
                    log.Info("Found. Shop image URL: " + shopImageURL);


                }
            }
            catch (Exception e1)
            {
                log.Error(e1);
            }

        }

        private string GetNode(string type, string name)
        {
            return ".//" + type + "[@class='" + name + "']";
        }
    }
}
