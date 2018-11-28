﻿using GalaSoft.MvvmLight.Messaging;
using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Text.RegularExpressions;
using System.Windows;
using TLCGen.Extensions;
using TLCGen.Integrity;
using TLCGen.Messaging;
using TLCGen.Messaging.Messages;
using TLCGen.Models;
using TLCGen.Models.Enumerations;

namespace TLCGen.ModelManagement
{
    public class TLCGenModelManager : ITLCGenModelManager
    {
        #region Fields

        private static readonly object _Locker = new object();
        private static ITLCGenModelManager _Default;

        private IMessenger _MessengerInstance;
	    private Action<object, string> _setDefaultsAction;

	    #endregion // Fields

        #region Properties

        public static ITLCGenModelManager Default
        {
            get
            {
                if (_Default == null)
                {
                    lock (_Locker)
                    {
                        if (_Default == null)
                        {
                            _Default = new TLCGenModelManager();
                        }
                    }
                }
                return _Default;
            }
        }

        private IMessenger MessengerInstance
        {
            get { return _MessengerInstance; }
            set { _MessengerInstance = value; }
        }

        public ControllerModel Controller
        {
            get; set;
        }

        #endregion // Properties

        #region Public Methods

        public static void OverrideDefault(ITLCGenModelManager provider)
        {
            _Default = provider;
        }

	    public void InjectDefaultAction(Action<object, string> setDefaultsAction)
	    {
		    _setDefaultsAction = setDefaultsAction;
	    }

        public bool CheckVersionOrder(ControllerModel controller)
        {
            var vc = Version.Parse(string.IsNullOrWhiteSpace(controller.Data.TLCGenVersie) ? "0.0.0.0" : controller.Data.TLCGenVersie);
            var vp = Assembly.GetEntryAssembly().GetName().Version;
            if(vc > vp)
            {
                MessageBox.Show($"Dit bestand is gemaakt met een nieuwere versie van TLCGen,\n" +
                                $"en kan met deze versie niet worden geopend.\n\n" +
                                $"Versie TLCGen: {vp.ToString()}\n" +
                                $"Versie bestand: {vc.ToString()}", "Versies komen niet overeen");
                return false;
            }
            return true;
        }

        public void CorrectModelByVersion(ControllerModel controller, string filename)
        {
            // correct segment items
            foreach(var s in controller.Data.SegmentenDisplayBitmapData)
            {
                if (s.Naam.StartsWith("segm"))
                {
                    s.Naam = s.Naam.Replace("segm", "");
                }
            }

            // check PostAfhandelingOV_Add in ov.add
            var ovAddFile = Path.Combine(Path.GetDirectoryName(filename), controller.Data.Naam + "ov.add");
            if (File.Exists(ovAddFile))
            {
                var ovaddtext = File.ReadAllLines(ovAddFile);
                if(ovaddtext.All(x => !Regex.IsMatch(x, @"^\s*void\s+PostAfhandelingOV_Add.*")))
                {
                    MessageBox.Show($"Let op! Deze versie van TLCGen maakt een functie\n" +
                                $"'PostAfhandelingOV' aan in bestand {controller.Data.Naam}ov.c. Hierin wordt\n" +
                                $"de functie 'PostAfhandelingOV_Add' aangeroepen, die echter\n" +
                                $"ontbreekt in bestand {controller.Data.Naam}ov.add.", "Functie PostAfhandelingOV_Add ontbreekt.\n\n" +
                                "Voeg deze dus toe, waarschijnlijk in plaats van 'void post_AfhandelingOV'," +
                                "want die wordt niet aangeroepen.");
                }
            }

            var v = Version.Parse(string.IsNullOrWhiteSpace(controller.Data.TLCGenVersie) ? "0.0.0.0" : controller.Data.TLCGenVersie);
            
            // In version 0.2.2.0, the OVIngreepModel object was changed
            var checkVer = Version.Parse("0.2.2.0");
            if(v < checkVer)
            {
                bool vecom = false;
                foreach (var ov in controller.OVData.OVIngrepen)
                {
                    if (ov.KAR)
                    {
                        ov.MeldingenData.Inmeldingen.Add(new OVIngreepInUitMeldingModel
                        {
                            AlleenIndienGeenInmelding = false,
                            AntiJutterTijd = 15,
                            AntiJutterTijdToepassen = true,
                            InUit = OVIngreepInUitMeldingTypeEnum.Inmelding,
                            KijkNaarWisselStand = false,
                            OpvangStoring = false,
                            Type = OVIngreepInUitMeldingVoorwaardeTypeEnum.KARMelding
                        });
                        ov.MeldingenData.Uitmeldingen.Add(new OVIngreepInUitMeldingModel
                        {
                            AlleenIndienGeenInmelding = false,
                            AntiJutterTijd = 15,
                            AntiJutterTijdToepassen = true,
                            InUit = OVIngreepInUitMeldingTypeEnum.Uitmelding,
                            KijkNaarWisselStand = false,
                            OpvangStoring = false,
                            Type = OVIngreepInUitMeldingVoorwaardeTypeEnum.KARMelding
                        });
                    }
                    if(ov.Vecom && !vecom)
                    {
                        vecom = true;
                        MessageBox.Show("Dit is oud type TLCGen bestand. OV via VECOM moet opnieuw worden opgegeven.", "VECOM opnieuw invoeren.", MessageBoxButton.OK);
                    }
                }
            }

            // In version 0.2.3.0, handling of segments was altered.
            checkVer = Version.Parse("0.2.3.0");
            if(v < checkVer)
            {
                foreach (var s in controller.Data.SegmentenDisplayBitmapData)
                {
                    if (s.Naam.StartsWith("segm"))
                    {
                        s.Naam = s.Naam.Replace("segm", "");
                    }
                }
            }

            // In version 0.3.5.0, voetgangers got lanes.
            checkVer = Version.Parse("0.3.5.0");
            if (v < checkVer)
            {
                foreach (var s in controller.Fasen)
                {
                    if (!s.AantalRijstroken.HasValue || s.Type == FaseTypeEnum.Voetganger && s.AantalRijstroken.Value == 1)
                    {
                        switch (s.Type)
                        {
                            case FaseTypeEnum.Voetganger:
                                s.AantalRijstroken = 2;
                                break;
                            default:
                                s.AantalRijstroken = 1;
                                break;
                        }
                    }
                }
            }
        }

        public bool IsElementIdentifierUnique(TLCGenObjectTypeEnum objectType, string identifier, bool vissim = false)
        {
            if(!vissim) return TLCGenIntegrityChecker.IsElementNaamUnique(Controller, identifier);
            return TLCGenIntegrityChecker.IsElementVissimNaamUnique(Controller, identifier);
        }

        #endregion // Public Methods

        #region TLCGen Messaging

        public void OnFasenChanging(FasenChangingMessage message)
        {
            if (message.AddedFasen != null)
            {
                foreach (var fcm in message.AddedFasen)
                {
                    // PT Conflict prms
                    if (Controller.OVData.OVIngreepType != Models.Enumerations.OVIngreepTypeEnum.Geen)
                    {
                        var prms = new OVIngreepSignaalGroepParametersModel();
                        _setDefaultsAction?.Invoke(prms, null);
                        prms.FaseCyclus = fcm.Naam;
                        Controller.OVData.OVIngreepSignaalGroepParameters.Add(prms);
                    }

                    // Module settings
                    var fcmlm = new FaseCyclusModuleDataModel() { FaseCyclus = fcm.Naam };
	                _setDefaultsAction?.Invoke(fcmlm, null);
                    Controller.ModuleMolen.FasenModuleData.Add(fcmlm);

                    // Green times
                    foreach (var set in Controller.GroentijdenSets)
                    {
                        var mgm = new GroentijdModel { FaseCyclus = fcm.Naam };
                        _setDefaultsAction(mgm, fcm.Type.ToString());
                        set.Groentijden.Add(mgm);
                    }
                }
            }
            if (message.RemovedFasen != null)
            {
                foreach (var fcm in message.RemovedFasen)
                {
                    // PT Conflict prms
                    if (Controller.OVData.OVIngreepType != Models.Enumerations.OVIngreepTypeEnum.Geen)
                    {
                        OVIngreepSignaalGroepParametersModel _prms = null;
                        foreach (var prms in Controller.OVData.OVIngreepSignaalGroepParameters)
                        {
                            if (prms.FaseCyclus == fcm.Naam)
                            {
                                _prms = prms;
                            }
                        }
                        if (_prms != null)
                        {
                            Controller.OVData.OVIngreepSignaalGroepParameters.Remove(_prms);
                        }
                    }

                    // Module settings
                    FaseCyclusModuleDataModel fcvm = null;
                    foreach(var f in Controller.ModuleMolen.FasenModuleData)
                    {
                        if(fcm.Naam == f.FaseCyclus)
                        {
                            fcvm = f;
                        }
                    }
                    if (fcvm != null)
                    {
                        Controller.ModuleMolen.FasenModuleData.Remove(fcvm);
                    }

                    // Green times
                    foreach (var set in Controller.GroentijdenSets)
                    {
                        GroentijdModel mgm = null;
                        foreach (var mgvm in set.Groentijden)
                        {
                            if (mgvm.FaseCyclus == fcm.Naam)
                            {
                                mgm = mgvm;
                            }
                        }
                        if (mgm != null)
                        {
                            set.Groentijden.Remove(mgm);
                        }
                    }
                }
            }

            // Sorting
            Controller.OVData.OVIngreepSignaalGroepParameters.BubbleSort();
            foreach (var set in Controller.GroentijdenSets)
            {
                set.Groentijden.BubbleSort();
            }
            Controller.ModuleMolen.FasenModuleData.BubbleSort();

            // Messaging
            MessengerInstance.Send(new FasenChangedMessage(message.AddedFasen, message.RemovedFasen));
        }

        private void OnNameChanging(NameChangingMessage msg)
        {
            ChangeNameOnObject(Controller, msg.OldName, msg.NewName);
            MessengerInstance.Send(new NameChangedMessage(msg.ObjectType, msg.OldName, msg.NewName));
        }

        public void ChangeNameOnObject(object obj, string oldName, string newName)
        {
            if (obj == null) return;
            Type objType = obj.GetType();

            // class refers to?
            var refToAttr = objType.GetCustomAttribute<RefersToAttribute>();

            PropertyInfo[] properties = objType.GetProperties();
            foreach (PropertyInfo property in properties)
            {
                object propValue = property.GetValue(obj);

                // for strings
                if (property.PropertyType == typeof(string))
                {
                    // if this is the referent string, set it if needed
                    if (refToAttr != null &&
                        (property.Name == refToAttr.ReferProperty1 || 
                         property.Name == refToAttr.ReferProperty2 ||
                         property.Name == refToAttr.ReferProperty3))
                    {
                        if ((string)propValue == oldName)
                        {
                            property.SetValue(obj, newName);
                        }
                    }
                    // otherwise, check if the string has RefersTo itself, and set if needed
                    else
                    {
                        var strRefToAttr = property.GetCustomAttribute<RefersToAttribute>();
                        if (strRefToAttr != null)
                        {
                            if ((string)propValue == oldName)
                            {
                                property.SetValue(obj, newName);
                            }
                        }
                    }
                }
                // for lists
                else if (propValue is IList elems)
                {
                    foreach (var item in elems)
                    {
                        ChangeNameOnObject(item, oldName, newName);
                    }
                }
                // for objects
                else if(!property.PropertyType.IsValueType)
                {
                    ChangeNameOnObject(propValue, oldName, newName);
                }
            }
        }

        public void OnModelManagerMessage(ModelManagerMessageBase msg)
        {
            switch (msg)
            {
                case OVIngreepMeldingChangedMessage meldingMsg:
                    var ovi = Controller.OVData.OVIngrepen.FirstOrDefault(x => x.FaseCyclus == meldingMsg.FaseCyclus);
                    if (ovi != null && meldingMsg.MeldingType == OVIngreepInUitMeldingVoorwaardeTypeEnum.KARMelding)
                    {
                        var karMelding = ovi.MeldingenData.Inmeldingen.FirstOrDefault(x => x.Type == OVIngreepInUitMeldingVoorwaardeTypeEnum.KARMelding);

                        if (karMelding != null && ovi.DummyKARInmelding == null)
                        {
                            ovi.DummyKARInmelding = new DetectorModel()
                            {
                                Dummy = true,
                                Naam = "dummykarin" + ovi.FaseCyclus
                            };
                        }
                        else if (karMelding == null && ovi.DummyKARInmelding != null)
                        {
                            ovi.DummyKARInmelding = null;
                        }

                        karMelding = ovi.MeldingenData.Uitmeldingen.FirstOrDefault(x => x.Type == OVIngreepInUitMeldingVoorwaardeTypeEnum.KARMelding);

                        if (karMelding != null && ovi.DummyKARUitmelding == null)
                        {
                            ovi.DummyKARUitmelding = new DetectorModel()
                            {
                                Dummy = true,
                                Naam = "dummykaruit" + ovi.FaseCyclus
                            };
                        }
                        else if (karMelding == null && ovi.DummyKARUitmelding != null)
                        {
                            ovi.DummyKARUitmelding = null;
                        }
                    }
                    break;
                case ModulesChangedMessage modulesMessage:
                    if (Controller.Data.UitgangPerModule)
                    {
                        foreach (var m in Controller.ModuleMolen.Modules)
                        {
                            if (!Controller.Data.ModulenDisplayBitmapData.Any(x => x.Naam == m.Naam))
                            {
                                Controller.Data.ModulenDisplayBitmapData.Add(new ModuleDisplayElementModel
                                {
                                    Naam = m.Naam
                                });
                                Controller.Data.ModulenDisplayBitmapData.BubbleSort();
                            }
                        }
                        var rd = new List<ModuleDisplayElementModel>();
                        foreach (var md in Controller.Data.ModulenDisplayBitmapData)
                        {
                            if (!Controller.ModuleMolen.Modules.Any(x => x.Naam == md.Naam))
                            {
                                rd.Add(md);
                            }
                        }
                        foreach (var r in rd)
                        {
                            Controller.Data.ModulenDisplayBitmapData.Remove(r);
                        }
                    }
                    break;

            }
        }

        #endregion // TLCGen Messaging

        #region Constructor

        public TLCGenModelManager(IMessenger messengerinstance = null)
        {
            if(messengerinstance == null)
            {
                MessengerInstance = Messenger.Default;
            }
            MessengerInstance.Register(this, new Action<FasenChangingMessage>(OnFasenChanging));
            MessengerInstance.Register(this, new Action<NameChangingMessage>(OnNameChanging));
            MessengerInstance.Register(this, true, new Action<ModelManagerMessageBase>(OnModelManagerMessage));
        }

        #endregion // Constructor
    }
}