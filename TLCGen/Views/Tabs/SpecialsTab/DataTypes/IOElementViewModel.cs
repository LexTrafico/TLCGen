﻿using System;
using GalaSoft.MvvmLight;
using TLCGen.Models;

namespace TLCGen.ViewModels
{
    public class IOElementViewModel : ViewModelBase, IComparable
    {
        public IOElementViewModel(IOElementModel element)
        {
            Element = element;
        }

        public int RangeerIndex
        {
            get => !UseSecondaryIndex ? Element.RangeerIndex : Element.RangeerIndex2;
            set
            {
                if (!UseSecondaryIndex) Element.RangeerIndex = value;
                else Element.RangeerIndex2 = value;
                if (SavedData != null)
                {
                    if (!UseSecondaryIndex) SavedData.RangeerIndex = value;
                    else SavedData.RangeerIndex2 = value;
                }
            }
        }
        
        public string ManualNaam
        {
            get => Element.ManualNaam;
            set
            {
                Element.ManualNaam = value;
                if (SavedData != null)
                {
                    SavedData.ManualNaam = value;
                }
            }
        }

        public bool HasManualName
        {
            get => SavedData.HasManualNaam;
            set
            {
                SavedData.HasManualNaam = value;
                RaisePropertyChanged();
            }
        }
        
        public bool UseSecondaryIndex { get; set; }

        public IOElementModel Element { get; }

        public IOElementRangeerDataModel SavedData { get; set; }

        public int CompareTo(object obj)
        {
            return RangeerIndex.CompareTo(((IOElementViewModel) obj).RangeerIndex);
        }
    }
}