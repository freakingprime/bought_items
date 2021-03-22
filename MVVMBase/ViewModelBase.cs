using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Text;
using System.Threading.Tasks;

namespace BoughtItems.MVVMBase
{
    [Serializable]
    public abstract class ViewModelBase : ModelBase, IDisposable
    {
        public ViewModelBase()
        {
        }

        #region IDisposable Members

        bool _isDisposed = false;
        public bool IsDisposed
        {
            get { return _isDisposed; }
        }

        ~ViewModelBase()
        {
            Dispose(false);
        }

        public virtual void Dispose()
        {
            Dispose(true);

            //GC.SuppressFinalize(this);
        }

        protected virtual void OnPreviewDispose()
        {
        }

        protected virtual void OnDisposeManaged()
        {
        }

        protected virtual void OnDisposeUnmanaged()
        {
        }

        protected void Dispose(bool isDisposing)
        {
            if (_isDisposed)
            {
                return;
            }

            if (isDisposing)
            {
                OnPreviewDispose();
                OnDisposeManaged();
            }

            OnDisposeUnmanaged();

            _isDisposed = true;
        }

        #endregion
    }
}
