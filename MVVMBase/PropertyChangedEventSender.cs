using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace BoughtItems.MVVMBase
{
    public class PropertyChangedEventSender
    {
        public PropertyChangedEventSender(INotifyPropertyChanged sender)
        {
            Sender = sender;
        }

        /// <summary>
        /// 보낸이
        /// Source에 대해서 Reference에 대한 작업이 있다면 IDisposable를 구현 해야 한다.
        /// </summary>
        public INotifyPropertyChanged Sender { get; protected set; }

        public void Send(string propertyName, PropertyChangedEventHandler evt)
        {
            Send(Sender, propertyName, evt);
        }

        public void Send(string[] propertyNames, PropertyChangedEventHandler evt)
        {
            Send(Sender, propertyNames, evt);
        }

        public static void Send(INotifyPropertyChanged sender, string propertyName, PropertyChangedEventHandler evt)
        {
            if (sender == null || evt == null)
            {
                return;
            }

            evt(sender, new PropertyChangedEventArgs(propertyName));
        }

        public static void Send(INotifyPropertyChanged sender, PropertyChangedEventArgs e, PropertyChangedEventHandler evt)
        {
            if (sender == null || e == null || evt == null)
            {
                return;
            }

            evt(sender, e);
        }

        public static void Send(INotifyPropertyChanged sender, string[] propertyNames, PropertyChangedEventHandler evt)
        {
            if (sender == null || evt == null || propertyNames == null || propertyNames.Length == 0)
            {
                return;
            }
            foreach (string propertyName in propertyNames)
            {
                evt(sender, new PropertyChangedEventArgs(propertyName));
            }
        }
    }
}
