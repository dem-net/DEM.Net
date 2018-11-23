// ==========================================================================
// Copyright (c) 2011-2016, dlg.krakow.pl
// All Rights Reserved
//
// NOTICE: dlg.krakow.pl permits you to use, modify, and distribute this file
// in accordance with the terms of the license agreement accompanying it.
// ==========================================================================

using System;
using System.Collections.Generic;
using System.Linq;

namespace Gpx
{
    public class GpxProperties
    {
        private class GpxListWrapper<T> : IList<T>
        {
            GpxProperties Properties_;
            string Name_;
            IList<T> Items_;

            public GpxListWrapper(GpxProperties properties, string name)
            {
                this.Properties_ = properties;
                this.Name_ = name;
                this.Items_ = properties.GetObjectProperty<IList<T>>(name);
            }

            public int IndexOf(T item)
            {
                return (Items_ != null) ? Items_.IndexOf(item) : -1;
            }

            public void Insert(int index, T item)
            {
                if (Items_ == null && index != 0) throw new ArgumentOutOfRangeException();

                if (Items_ == null)
                {
                    Items_ = new List<T>();
                    Properties_.SetObjectProperty(Name_, Items_);
                }

                Items_.Insert(index, item);
            }

            public void RemoveAt(int index)
            {
                if (Items_ == null) throw new ArgumentOutOfRangeException();
                Items_.RemoveAt(index);
            }

            public T this[int index]
            {
                get
                {
                    if (Items_ == null) throw new ArgumentOutOfRangeException();
                    return Items_[index];
                }
                set
                {
                    if (Items_ == null) throw new ArgumentOutOfRangeException();
                    Items_[index] = value;
                }
            }

            public void Add(T item)
            {
                if (Items_ == null)
                {
                    Items_ = new List<T>();
                    Properties_.SetObjectProperty(Name_, Items_);
                }

                Items_.Add(item);
            }

            public void Clear()
            {
                if (Items_ != null)
                {
                    Items_.Clear();
                    Items_ = null;
                }
            }

            public bool Contains(T item)
            {
                return Items_ != null ? Items_.Contains(item) : false;
            }

            public void CopyTo(T[] array, int arrayIndex)
            {
                if (Items_ == null) return;
                Items_.CopyTo(array, arrayIndex);
            }

            public int Count
            {
                get { return Items_ != null ? Items_.Count : 0; }
            }

            public bool IsReadOnly
            {
                get { return false; }
            }

            public bool Remove(T item)
            {
                return Items_ != null ? Items_.Remove(item) : false;
            }

            public IEnumerator<T> GetEnumerator()
            {
                return (Items_ != null ? Items_ : Enumerable.Empty<T>()).GetEnumerator();
            }

            System.Collections.IEnumerator System.Collections.IEnumerable.GetEnumerator()
            {
                return this.GetEnumerator();
            }
        }

        Dictionary<string, object> Properties_ = null;

        public Nullable<T> GetValueProperty<T>(string name) where T : struct
        {
            if (Properties_ == null) return null;

            object value;
            if (!Properties_.TryGetValue(name, out value)) return null;

            return (T)value;
        }

        public T GetObjectProperty<T>(string name) where T : class
        {
            if (Properties_ == null) return null;

            object value;
            if (!Properties_.TryGetValue(name, out value)) return null;

            return (T)value;
        }

        public IList<T> GetListProperty<T>(string name)
        {
            return new GpxListWrapper<T>(this, name);
        }

        public void SetValueProperty<T>(string name, Nullable<T> value) where T : struct
        {
            if (value != null)
            {
                if (Properties_ == null) Properties_ = new Dictionary<string, object>();
                Properties_[name] = value.Value;
            }
            else if (Properties_ != null)
            {
                Properties_.Remove(name);
            }
        }

        public void SetObjectProperty<T>(string name, T value) where T : class
        {
            if (value != null)
            {
                if (Properties_ == null) Properties_ = new Dictionary<string, object>();
                Properties_[name] = value;
            }
            else if (Properties_ != null)
            {
                Properties_.Remove(name);
            }
        }
    }
}
