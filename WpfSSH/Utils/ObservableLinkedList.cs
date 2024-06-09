using System;
using System.Collections;
using System.Collections.Generic;
using System.Collections.Specialized;
using System.ComponentModel;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace WpfSSH
{
    internal class ObservableLinkedList<T> : INotifyPropertyChanged, IEnumerable<T>
    {
        public LinkedList<T> list = new LinkedList<T>();
        public event NotifyCollectionChangedEventHandler CollectionChanged;
        public event PropertyChangedEventHandler? PropertyChanged;

        public void OnNotifyCollectionChanged()
        {
            if (CollectionChanged != null)
            {
                CollectionChanged(this, new NotifyCollectionChangedEventArgs(NotifyCollectionChangedAction.Reset));
            }
        }

        public IEnumerator<T> GetEnumerator()
        {
            return list.GetEnumerator();
        }

        IEnumerator IEnumerable.GetEnumerator()
        {
            return list.GetEnumerator();
        }
    }
}
