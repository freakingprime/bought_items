using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Collections.Specialized;
using System.ComponentModel;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace BoughtItems.MVVMBase
{
    public class RangeObservableCollection<T> : ObservableCollection<T>
    {
        private static log4net.ILog log;

        public RangeObservableCollection() : base()
        {
            log = log4net.LogManager.GetLogger("RangeCollection");
        }

        public RangeObservableCollection(string name) : base()
        {
            log = log4net.LogManager.GetLogger(name);
        }

        private bool _suppressNotification = false;

        public bool SuppressNotification
        {
            get { return _suppressNotification; }
            set
            {
                _suppressNotification = value;
                if (!value)
                {
                    //supress is set to False
                    OnCollectionChanged(new NotifyCollectionChangedEventArgs(NotifyCollectionChangedAction.Reset));
                }
            }
        }

        protected override void OnCollectionChanged(NotifyCollectionChangedEventArgs e)
        {
            //log.Debug("OnCollectionChanged is suppressed: " + SuppressNotification);
            if (!SuppressNotification) base.OnCollectionChanged(e);
        }

        public void AddRange(IEnumerable<T> list)
        {
            if (list == null) throw new ArgumentNullException("list");

            SuppressNotification = true;

            foreach (T item in list)
            {
                Add(item);
            }

            SuppressNotification = false;
        }
    }
}
