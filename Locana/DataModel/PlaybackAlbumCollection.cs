﻿using Locana.Utility;
using System;
using System.Collections.Generic;
using System.Collections.Specialized;
using System.ComponentModel;
using System.Linq;

namespace Locana.DataModel
{
    public class AlbumGroupCollection : List<Album>, INotifyPropertyChanged, INotifyCollectionChanged
    {
        private readonly bool SortAlbum;

        public Album.SortOrder ContentSortOrder { set; get; }

        public AlbumGroupCollection(bool sortAlbum = true)
        {
            SortAlbum = sortAlbum;
            ContentSortOrder = Album.SortOrder.AddToTail;
        }

        private SelectivityFactor _SelectivityFactor = SelectivityFactor.None;
        public SelectivityFactor SelectivityFactor
        {
            get { return _SelectivityFactor; }
            set
            {
                _SelectivityFactor = value;
                lock (this)
                {
                    foreach (var group in this)
                    {
                        group.SelectivityFactor = value;
                    }
                }
            }
        }

        new public void Clear()
        {
            base.Clear();
            OnCollectionChanged(new NotifyCollectionChangedEventArgs(NotifyCollectionChangedAction.Reset));
        }

        public bool Remove(Thumbnail content, bool deleteGroupIfEmpty = true)
        {
            lock (this)
            {
                var group = GetGroup(content.GroupTitle);
                if (group == null)
                {
                    DebugUtil.Log(() => "Remove: group does not exist");
                    return false;
                }
                var res = group.Remove(content);
                if (deleteGroupIfEmpty && group.Count == 0)
                {
                    DebugUtil.LogSensitive(() => "Remove no item group: {0}", group.Key);
                    var index = IndexOf(group);
                    var removed = Remove(group);
                    if (removed)
                    {
                        OnCollectionChanged(new NotifyCollectionChangedEventArgs(NotifyCollectionChangedAction.Remove, group, index));
                    }
                }
                return res;
            }
        }

        public void Add(Thumbnail content)
        {
            lock (this)
            {
                var group = GetGroup(content.GroupTitle);
                if (group == null)
                {
                    group = new Album(content.GroupTitle, ContentSortOrder);
                    AddAlbum(group);
                }
                group.Add(content);
            }
        }

        private void AddAlbum(Album item)
        {
            int insertAt = Count;
            if (item.Key == Album.LOCANA_DIRECTORY)
            {
                // Display Locana group at the top of the list
                insertAt = 0;
            }
            else if (SortAlbum)
            {
                for (int i = 0; i < Count; i++)
                {
                    if (string.CompareOrdinal(this[i].Key, item.Key) < 0)
                    {
                        insertAt = i;
                        break;
                    }
                }
            }
            Insert(insertAt, item);
            OnCollectionChanged(new NotifyCollectionChangedEventArgs(NotifyCollectionChangedAction.Add, item, insertAt));
        }

        private Album GetGroup(string key)
        {
            return this.SingleOrDefault(item => item.Key == key);
        }

        public event PropertyChangedEventHandler PropertyChanged;
        private void OnPropertyChanged(string name)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(name));
        }

        public event NotifyCollectionChangedEventHandler CollectionChanged;
        private void OnCollectionChanged(NotifyCollectionChangedEventArgs e)
        {
            OnPropertyChanged(nameof(Count));
            OnPropertyChanged("Item[]");
            try
            {
                CollectionChanged?.Invoke(this, e);
            }
            catch (NotSupportedException)
            {
                OnCollectionChanged(new NotifyCollectionChangedEventArgs(NotifyCollectionChangedAction.Reset));
            }
        }
    }
}
