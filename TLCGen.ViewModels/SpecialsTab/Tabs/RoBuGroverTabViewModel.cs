﻿using GalaSoft.MvvmLight.Messaging;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Input;
using TLCGen.Extensions;
using TLCGen.Helpers;
using TLCGen.Messaging.Messages;
using TLCGen.Models;
using TLCGen.Models.Enumerations;

namespace TLCGen.ViewModels
{
    [TLCGenTabItem(index: 4, type: TabItemTypeEnum.SpecialsTab)]
    public class RoBuGroverTabViewModel : TLCGenTabItemViewModel
    {
        #region Fields

        private RoBuGroverConflictGroepViewModel _SelectedConflictGroep;
        private RoBuGroverSignaalGroepInstellingenViewModel _SelectedSignaalGroepInstelling;

        private ObservableCollection<RoBuGroverTabFaseViewModel> _Fasen;
        private RoBuGroverTabFaseViewModel _SelectedFaseCyclus;

        private Dictionary<string, string> _ControllerRGVFileDetectoren;
        private Dictionary<string, string> _ControllerRGVHiaatDetectoren;

        #endregion // Fields

        #region Properties

        public int MaximaleCyclustijd
        {
            get { return _Controller.RoBuGrover.MaximaleCyclustijd; }
            set
            {
                _Controller.RoBuGrover.MaximaleCyclustijd = value;
                OnMonitoredPropertyChanged("MaximaleCyclustijd");
            }
        }

        public RoBuGroverMethodeEnum MethodeRobugrover
        {
            get { return _Controller.RoBuGrover.MethodeRoBuGrover; }
            set
            {
                _Controller.RoBuGrover.MethodeRoBuGrover = value;
                OnMonitoredPropertyChanged("MethodeRobugrover");
            }
        }

        public bool OphogenTijdensGroen
        {
            get { return _Controller.RoBuGrover.OphogenTijdensGroen; }
            set
            {
                _Controller.RoBuGrover.OphogenTijdensGroen = value;
                OnMonitoredPropertyChanged("OphogenTijdensGroen");
            }
        }

        public RoBuGroverConflictGroepViewModel SelectedConflictGroep
        {
            get { return _SelectedConflictGroep; }
            set
            {
                var oldval = _SelectedConflictGroep;
                _SelectedConflictGroep = value;
                OnMonitoredPropertyChanged("SelectedConflictGroep");
                Messenger.Default.Send(new SelectedConflictGroepChangedMessage(_SelectedConflictGroep?.ConflictGroep, oldval?.ConflictGroep));
            }
        }

        public RoBuGroverSignaalGroepInstellingenViewModel SelectedSignaalGroepInstelling
        {
            get { return _SelectedSignaalGroepInstelling; }
            set
            {
                _SelectedSignaalGroepInstelling = value;
                if(value != null)
                {
                    OnSelectedSignaalGroepInstellingSelected();
                }
                OnMonitoredPropertyChanged("SelectedSignaalGroepInstelling");
            }
        }

        public RoBuGroverTabFaseViewModel SelectedFaseCyclus
        {
            get { return _SelectedFaseCyclus; }
            set
            {
                _SelectedFaseCyclus = value;
                OnPropertyChanged("SelectedFaseCyclus");
            }
        }

        public ObservableCollectionAroundList<RoBuGroverConflictGroepViewModel, RoBuGroverConflictGroepModel> ConflictGroepen
        {
            get;
            private set;
        }

        public ObservableCollectionAroundList<RoBuGroverSignaalGroepInstellingenViewModel, RoBuGroverSignaalGroepInstellingenModel> SignaalGroepInstellingen
        {
            get;
            private set;
        }

        public ObservableCollection<RoBuGroverTabFaseViewModel> Fasen
        {
            get
            {
                if (_Fasen == null)
                {
                    _Fasen = new ObservableCollection<RoBuGroverTabFaseViewModel>();
                }
                return _Fasen;
            }
        }

        #endregion // Properties

        #region Commands

        RelayCommand _AddConflictGroepCommand;
        public ICommand AddConflictGroepCommand
        {
            get
            {
                if (_AddConflictGroepCommand == null)
                {
                    _AddConflictGroepCommand = new RelayCommand(AddConflictGroepCommand_Executed, AddConflictGroepCommand_CanExecute);
                }
                return _AddConflictGroepCommand;
            }
        }

        RelayCommand _RemoveConflictGroepCommand;
        public ICommand RemoveConflictGroepCommand
        {
            get
            {
                if (_RemoveConflictGroepCommand == null)
                {
                    _RemoveConflictGroepCommand = new RelayCommand(RemoveConflictGroepCommand_Executed, RemoveConflictGroepCommand_CanExecute);
                }
                return _RemoveConflictGroepCommand;
            }
        }

        RelayCommand _AddRemoveFaseCommand;
        public ICommand AddRemoveFaseCommand
        {
            get
            {
                if (_AddRemoveFaseCommand == null)
                {
                    _AddRemoveFaseCommand = new RelayCommand(AddRemoveFaseCommand_Executed, AddRemoveFaseCommand_CanExecute);
                }
                return _AddRemoveFaseCommand;
            }
        }

        #endregion // Commands

        #region Command functionality

        private bool AddConflictGroepCommand_CanExecute(object obj)
        {
            return true;
        }

        private void AddConflictGroepCommand_Executed(object obj)
        {
            RoBuGroverConflictGroepModel cgm = new RoBuGroverConflictGroepModel();
            ConflictGroepen.Add(new RoBuGroverConflictGroepViewModel(cgm));
        }

        private bool RemoveConflictGroepCommand_CanExecute(object obj)
        {
            return SelectedConflictGroep != null;
        }

        private void RemoveConflictGroepCommand_Executed(object obj)
        {
            var _SelectedConflictGroep = SelectedConflictGroep;
            ConflictGroepen.Remove(SelectedConflictGroep);

            if (ConflictGroepen.Count == 0)
            {
                SignaalGroepInstellingen.RemoveAll();
            }
            else if(SignaalGroepInstellingen.Count > 0)
            {
                foreach(var fc in _SelectedConflictGroep.Fasen)
                {

                    if ((!ConflictGroepen.SelectMany(x => x.Fasen).Any() ||
                         !ConflictGroepen.SelectMany(x => x.Fasen).Where(y => y.FaseCyclus == fc.FaseCyclus).Any()) &&
                          SignaalGroepInstellingen.Where(x => x.FaseCyclus == fc.FaseCyclus).Any())
                    {
                        var instvm = SignaalGroepInstellingen.Where(x => x.FaseCyclus == fc.FaseCyclus).First();
                        try
                        {
                            var addfc = _Controller.Fasen.Where(x => x.Define == instvm.FaseCyclus).First();
                            foreach(DetectorModel dm in addfc.Detectoren)
                            {
                                if(dm.Type == DetectorTypeEnum.Lang)
                                {
                                    var hd = new RoBuGroverHiaatDetectorModel();
                                    hd.Detector = dm.Naam;
                                    instvm.HiaatDetectoren.Add(new RoBuGroverHiaatDetectorViewModel(hd));
                                }
                                if(dm.Type == DetectorTypeEnum.Kop)
                                {
                                    var fd = new RoBuGroverFileDetectorModel();
                                    fd.Detector = dm.Naam;
                                    instvm.FileDetectoren.Add(new RoBuGroverFileDetectorViewModel(fd));
                                }
                            }
                        }
                        catch
                        {

                        }
                        SignaalGroepInstellingen.Remove(instvm);
                        SignaalGroepInstellingen.BubbleSort();
                    }
                }
            }

            SelectedConflictGroep = null;
        }

        void AddRemoveFaseCommand_Executed(object prm)
        {
            var fc = prm as RoBuGroverTabFaseViewModel;
            SelectedFaseCyclus = fc;
            if (fc.CanBeAddedToConflictGroep && !fc.IsInConflictGroep)
            {
                var fcm = new RoBuGroverConflictGroepFaseModel();
                fcm.FaseCyclus = fc.FaseCyclusDefine;
                var fcvm = new RoBuGroverConflictGroepFaseViewModel(fcm);
                SelectedConflictGroep.Fasen.Add(fcvm);
                if(!SignaalGroepInstellingen.Where(x => x.FaseCyclus == fc.FaseCyclusDefine).Any())
                {
                    var inst = new RoBuGroverSignaalGroepInstellingenModel();
                    inst.FaseCyclus = fc.FaseCyclusDefine;
                    var instvm = new RoBuGroverSignaalGroepInstellingenViewModel(inst);
                    try
                    {
                        var addfc = _Controller.Fasen.Where(x => x.Define == instvm.FaseCyclus).First();
                        foreach (DetectorModel dm in addfc.Detectoren)
                        {
                            if (dm.Type == DetectorTypeEnum.Lang)
                            {
                                var hd = new RoBuGroverHiaatDetectorModel();
                                hd.Detector = dm.Naam;
                                instvm.HiaatDetectoren.Add(new RoBuGroverHiaatDetectorViewModel(hd));
                            }
                            if (dm.Type == DetectorTypeEnum.Kop)
                            {
                                var fd = new RoBuGroverFileDetectorModel();
                                fd.Detector = dm.Naam;
                                instvm.FileDetectoren.Add(new RoBuGroverFileDetectorViewModel(fd));
                            }
                        }
                    }
                    catch
                    {

                    }
                    SignaalGroepInstellingen.Add(instvm);
                    SignaalGroepInstellingen.BubbleSort();
                }
            }
            else if (fc.IsInConflictGroep)
            {
                // Use custom method instead of Remove method:
                // it removes based on Define instead of reference
                RoBuGroverConflictGroepFaseViewModel removevm = null;
                removevm = SelectedConflictGroep.Fasen.Where(x => x.FaseCyclus == fc.FaseCyclusNaam).First();
                SelectedConflictGroep.Fasen.Remove(removevm);
                if (!ConflictGroepen.SelectMany(x => x.Fasen).Where(y => y.FaseCyclus == fc.FaseCyclusDefine).Any() &&
                    SignaalGroepInstellingen.Where(x => x.FaseCyclus == fc.FaseCyclusDefine).Any())
                {
                    var instvm = SignaalGroepInstellingen.Where(x => x.FaseCyclus == fc.FaseCyclusDefine).First();
                    SignaalGroepInstellingen.Remove(instvm);
                    SignaalGroepInstellingen.BubbleSort();
                }
            }
            foreach (RoBuGroverTabFaseViewModel tfc in Fasen)
            {
                tfc.UpdateConflictGroepInfo();
            }
        }

        bool AddRemoveFaseCommand_CanExecute(object prm)
        {
            return SelectedConflictGroep != null;
        }

        #endregion // Command functionality

        #region Private methods

        private void OnSelectedSignaalGroepInstellingSelected()
        {
            List<string> selectedfiledet = new List<string>();
            if (_ControllerRGVFileDetectoren.Count > 0)
            {
                foreach (var d in _ControllerRGVFileDetectoren)
                {
                    if (d.Value == SelectedSignaalGroepInstelling.FaseCyclus)
                    {
                        selectedfiledet.Add(d.Key);
                    }
                }
            }
            List<string> selectedhiaatdet = new List<string>();
            if (_ControllerRGVHiaatDetectoren.Count > 0)
            {
                foreach (var d in _ControllerRGVHiaatDetectoren)
                {
                    if (d.Value == SelectedSignaalGroepInstelling.FaseCyclus)
                    {
                        selectedhiaatdet.Add(d.Key);
                    }
                }
            }
            SelectedSignaalGroepInstelling.OnSelected(selectedfiledet, selectedhiaatdet);
        }

        #endregion // Private methods

        #region Public methods

        #endregion // Public methods

        #region TLCGen TabItem overrides

        public override string DisplayName
        {
            get { return "RoBuGrover"; }
        }

        public override void OnSelected()
        {
            Fasen.Clear();
            foreach(FaseCyclusModel fcm in _Controller.Fasen)
            {
                Fasen.Add(new RoBuGroverTabFaseViewModel(fcm.Define, fcm.Naam));
            }

            _ControllerRGVFileDetectoren = new Dictionary<string, string>();
            _ControllerRGVHiaatDetectoren = new Dictionary<string, string>();
            foreach (FaseCyclusModel fcm in _Controller.Fasen)
            {
                foreach (DetectorModel dm in fcm.Detectoren)
                {
                    if (dm.Type == Models.Enumerations.DetectorTypeEnum.Kop)
                        _ControllerRGVFileDetectoren.Add(dm.Naam, fcm.Define);
                    if (dm.Type == Models.Enumerations.DetectorTypeEnum.Lang)
                        _ControllerRGVHiaatDetectoren.Add(dm.Naam, fcm.Define);
                }
            }

            if (SelectedSignaalGroepInstelling != null)
            {
                OnSelectedSignaalGroepInstellingSelected();
            }
        }

        #endregion // TLCGen TabItem overrides

        #region Constructor

        public RoBuGroverTabViewModel(ControllerModel controller) : base(controller)
        {
            ConflictGroepen = new ObservableCollectionAroundList<RoBuGroverConflictGroepViewModel, RoBuGroverConflictGroepModel>(_Controller.RoBuGrover.ConflictGroepen);
            SignaalGroepInstellingen = new ObservableCollectionAroundList<RoBuGroverSignaalGroepInstellingenViewModel, RoBuGroverSignaalGroepInstellingenModel>(_Controller.RoBuGrover.SignaalGroepInstellingen);
        }

        #endregion // Constructor
    }
}