﻿using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Collections.Specialized;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace TLCGen.Helpers
{
    /// <summary>
    /// Represents an ObservableCollection of T1, that holds ViewModel items that
    /// wrap around Models items of type T2. The items of type T2 are in a list
    /// that is parsed to the constructor.
    /// </summary>
    /// <typeparam name="T1">ViewModel type this is collection of</typeparam>
    /// <typeparam name="T2">Model type the ViewModel items wrap</typeparam>
    public class ObservableCollectionAroundList<T1, T2> : ObservableCollection<T1> where T1 : IViewModelWithItem
    {
        #region Fields

        private List<T2> _ModelItems;

        #endregion // Fields

        #region Properties

        #endregion // Properties

        #region Collection Changed

        void Items_CollectionChanged(object sender, NotifyCollectionChangedEventArgs e)
        {
            if (e.NewItems != null && e.NewItems.Count > 0)
            {
                foreach (T1 i in e.NewItems)
                {
                    _ModelItems.Add((T2)(i as IViewModelWithItem).GetItem());
                }
            }
            if (e.OldItems != null && e.OldItems.Count > 0)
            {
                foreach (T1 i in e.OldItems)
                {
                    _ModelItems.Remove((T2)(i as IViewModelWithItem).GetItem());
                }
            }
        }

        #endregion // Collection Changed

        #region Constructor

        public ObservableCollectionAroundList(List<T2> modelitems)
        {
            _ModelItems = modelitems;

            foreach (T2 im in _ModelItems)
            {
                object[] args = { im };
                this.Add((T1)Activator.CreateInstance(typeof(T1), args));
            }

            this.CollectionChanged += new NotifyCollectionChangedEventHandler(Items_CollectionChanged);
        }

        #endregion // Constructor
    }
}
