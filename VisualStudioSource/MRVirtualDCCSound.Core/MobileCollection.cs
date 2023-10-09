using System.Collections;
using System.Collections.Specialized;
using System.ComponentModel;

namespace MRVirtualDCCSound.Core
{
    public class MobileCollection<T> : IList<T>, INotifyCollectionChanged where T : MobileSFXBase
    {
        private List<T> _d = new List<T>();

        private Dictionary<uint, int> _dictionary = new Dictionary<uint, int>();
        public T this[int index]
        {
            get => _d[index];
            set => _d[index] = value;
        }

        public int Count => _d.Count;
        public bool IsReadOnly => false;

        public event NotifyCollectionChangedEventHandler? CollectionChanged;

        public int GetIndexFor(uint index)
        {
            return _dictionary[index];
        }
        public void Add(T item)
        {
            Insert(_d.Count, item);
        }

        public void Clear()
        {
            _d.Clear();
            BuildNewDictionary();
            OnCollectionChanged(new NotifyCollectionChangedEventArgs(NotifyCollectionChangedAction.Reset));
        }

        public bool Contains(T item)
        {
            return _d.Contains(item);
        }

        public void CopyTo(T[] array, int arrayIndex)
        {
            _d.CopyTo(array, arrayIndex);
        }

        public IEnumerator<T> GetEnumerator()
        {
            return _d.GetEnumerator();
        }

        public int IndexOf(T item)
        {
            return _d.IndexOf(item);
        }

        public void Insert(int index, T item)
        {
            if (!_dictionary.ContainsKey(item.Id))
            {
                //item.PropertyChanged += Item_PropertyChanged;
                if (index == _d.Count)
                {
                    _d.Add(item);
                    _dictionary.Add(item.Id, _d.Count - 1);
                }
                else
                {
                    //inserting before end.
                    _d.Insert(index, item);
                    BuildNewDictionary();
                }
                OnCollectionChanged(new NotifyCollectionChangedEventArgs(NotifyCollectionChangedAction.Add, item));
            }
            else
            {
                throw new ArgumentException($"Decoder with address of '{item.Id}' already exists!");
            }
        }

        public bool Remove(T item)
        {
            var removed = _d.Remove(item);
            if (removed)
            {
                //item.PropertyChanged -= Item_PropertyChanged;
                BuildNewDictionary();
                OnCollectionChanged(new NotifyCollectionChangedEventArgs(NotifyCollectionChangedAction.Remove, item));
            }
            return removed;
        }

        public void RemoveAt(int index)
        {
            var item = _d[index];
            this.Remove(item);
        }

        IEnumerator IEnumerable.GetEnumerator()
        {
            return _d.GetEnumerator();
        }
        protected virtual void OnCollectionChanged(NotifyCollectionChangedEventArgs e)
        {
            if (CollectionChanged != null)
            {
                CollectionChanged(this, e);
            }
        }
        private void BuildNewDictionary()
        {
            _dictionary.Clear();
            for (int i = 0; i < _d.Count; i++)
            {
                _dictionary.Add(_d[i].Id, i);//Note: we aren't preventing duplicates.                                                
            }
        }
        protected virtual void Item_PropertyChanged(object? sender, PropertyChangedEventArgs e)
        {
            OnCollectionChanged(new NotifyCollectionChangedEventArgs(NotifyCollectionChangedAction.Reset));
            if (e.PropertyName == nameof(MobileSFXBase.Id))
            {
                BuildNewDictionary();
            }
            else if (e.PropertyName == nameof(MobileSFXBase.SoundsFolder) || e.PropertyName == nameof(MobileSFXBase.MaxRPM))
            {
                ((MobileSFXBase)sender).InitializeSound();
            }
        }
    }
}