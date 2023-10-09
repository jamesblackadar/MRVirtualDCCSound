using Microsoft.Extensions.Logging;
using System;
using System.Collections;
using System.Collections.Specialized;
using System.Text.Json;

namespace MRVirtualDCCSound.Core
{
    public class RosterCollection : IList<MobileSFXBase>, INotifyCollectionChanged
    {
        IMobileEventProvider mobileEventProvider;
        ILogger<RosterCollection> _il;
        IStorageRepository<List<MobileSFXBase>> _sr;
        ILoggerFactory _lf;
        public RosterCollection(IMobileEventProvider eventProvider, IStorageRepository<List<MobileSFXBase>> storageRepository, ILoggerFactory lf)
        {
            _il = lf.CreateLogger<RosterCollection>();
            _lf = lf;
            _sr = storageRepository;
            eventProvider.MobileEventArgEmit += OnMobileEventArg;
        }
        void OnMobileEventArg(object? sender, MobileEventArgs e)
        {
            if (_dictionary.TryGetValue(e.Address, out int i))
            {
                this[i].OnNext(e);
            }
        }

        public string StorageFilePath => _sr.FilePath;

        private List<MobileSFXBase> _d = new List<MobileSFXBase>();

        private Dictionary<uint, int> _dictionary = new Dictionary<uint, int>();
        public MobileSFXBase this[int index]
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
        public void Add(MobileSFXBase item)
        {
            item.InitializeSound();//todo lazy intitialize
            Insert(_d.Count, item);
        }

        public void Clear()
        {
            _d.Clear();
            BuildNewDictionary();
            //OnCollectionChanged(new NotifyCollectionChangedEventArgs(NotifyCollectionChangedAction.Reset));
        }

        public bool Contains(MobileSFXBase item)
        {
            return _d.Contains(item);
        }

        public void CopyTo(MobileSFXBase[] array, int arrayIndex)
        {
            _d.CopyTo(array, arrayIndex);
        }

        public IEnumerator<MobileSFXBase> GetEnumerator()
        {
            return _d.GetEnumerator();
        }

        public int IndexOf(MobileSFXBase item)
        {
            return _d.IndexOf(item);
        }

        public void Insert(int index, MobileSFXBase item)
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
                //OnCollectionChanged(new NotifyCollectionChangedEventArgs(NotifyCollectionChangedAction.Add, item));
            }
            else
            {
                throw new ArgumentException($"Decoder with address of '{item.Id}' already exists!");
            }
        }

        public bool Remove(MobileSFXBase item)
        {
            var removed = _d.Remove(item);
            if (removed)
            {
                //item.PropertyChanged -= Item_PropertyChanged;
                BuildNewDictionary();
                // OnCollectionChanged(new NotifyCollectionChangedEventArgs(NotifyCollectionChangedAction.Remove, item));
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
        //protected virtual void OnCollectionChanged(NotifyCollectionChangedEventArgs e)
        //{
        //    if (CollectionChanged != null)
        //    {
        //        CollectionChanged(this, e);
        //    }
        //}
        private void BuildNewDictionary()
        {
            _dictionary.Clear();
            for (int i = 0; i < _d.Count; i++)
            {
                _dictionary.Add(_d[i].Id, i);//Note: we aren't preventing duplicates.                                                
            }
        }

        public void Load()
        {
            foreach (var item in _sr.Read())
            {
                //Add(new MobileSFXBeep(item));
                //continue;
                var locologger = _lf.CreateLogger("MRVirtualDCCSound.Loco" + item.Id);
                _il.LogInformation($"Attempting to load{item.Id} with sound folder {item.SoundsFolder}");
                try
                {
                    var di = new DirectoryInfo(item.SoundsFolder ?? ".\\");
                    var jsonConfigFile = di.EnumerateFiles("*.json").FirstOrDefault();
                    if (jsonConfigFile == null)
                    {
                        Add(new MobileSFXSteam(item, locologger));//default
                    }
                    else if (jsonConfigFile.Name.Equals("_beep.json", StringComparison.InvariantCultureIgnoreCase))
                    {
                        Add(new MobileSFXBeep(item,locologger));//add testing/beep sound decoder
                    }
                    else if (jsonConfigFile.Name.Equals("mobiledecoderdm.json", StringComparison.InvariantCultureIgnoreCase))
                    {
                        try
                        {
                            var config = JsonSerializer.Deserialize<MobileSFXDieselMech.Config>(File.ReadAllText(jsonConfigFile.FullName));
                        }
                        catch (Exception ex)
                        {
                            //we can make a method to robustly deserialize.
                            _il.LogError(ex, "Error loading DM config file");
                        }
                        finally
                        {
                            Add(new MobileSFXDieselMech(item,locologger));
                        }
                    }
                    else if (jsonConfigFile.Name.Equals("mobiledecodersteamgeared.json", StringComparison.InvariantCultureIgnoreCase))
                    {
                        Add(new MobileDecoderSteamGeared(item,locologger));
                    }
                    else
                    {
                        Add(new MobileSFXSteam(item,locologger));
                    }
                }
                catch (Exception ex)
                {
                    //adding loco failed.
                    _il.LogError("Error loading locomotive", ex);
                }
            }
        }
        public void Save()
        {
            _sr.Write(this);
        }
    }
}