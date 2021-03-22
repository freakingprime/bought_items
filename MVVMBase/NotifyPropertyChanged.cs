using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Diagnostics;
using System.Diagnostics.CodeAnalysis;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace BoughtItems.MVVMBase
{
    /// <summary>
    /// INotifyPropertyChanged 구현부
    /// </summary>
    [Serializable]
    public abstract class NotifyPropertyChanged : INotifyPropertyChanged
    {
        public NotifyPropertyChanged()
        {
        }

        #region INotifyPropertyChanged Members

        public event PropertyChangedEventHandler PropertyChanged
        {
            add
            {
                _propertyChanged += value;
            }
            remove
            {
                _propertyChanged -= value;
            }
        }
        [NonSerialized]
        PropertyChangedEventHandler _propertyChanged;

        [SuppressMessage("Microsoft.Design", "CA1030:UseEventsWhereAppropriate")]
        protected void RaisePropertyChanged(string propertyName)
        {
            CheckPropertyName(propertyName);
            OnPropertyChanged(new PropertyChangedEventArgs(propertyName));
        }

        [SuppressMessage("Microsoft.Design", "CA1030:UseEventsWhereAppropriate")]
        protected void RaisePropertyChanged(params string[] propertyNames)
        {
            foreach (string propertyName in propertyNames)
            {
                RaisePropertyChanged(propertyName);
            }
        }

        protected virtual void OnPropertyChanged(PropertyChangedEventArgs e)
        {
            PropertyChangedEventSender.Send(this, e, _propertyChanged);
        }

        [Conditional("DEBUG")]
        void CheckPropertyName(string propertyName)
        {
            PropertyDescriptor pd = TypeDescriptor.GetProperties(this)[propertyName];
            if (pd == null)
            {
                throw new InvalidOperationException("The property with the propertyName '" + propertyName + "' doesn't exist.");
            }
        }

        #endregion

        #region Helper
        public virtual void RefreshAllProperties()
        {
            PropertyDescriptorCollection properties = TypeDescriptor.GetProperties(this);

            var propertieNames = new System.Collections.Generic.List<string>();
            foreach (PropertyDescriptor prop in properties)
                propertieNames.Add(prop.Name);

            RaisePropertyChanged(propertieNames.ToArray());
        }
        #endregion
    }
}
