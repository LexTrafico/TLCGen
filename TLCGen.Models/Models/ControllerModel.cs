﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Xml.Serialization;

namespace TLCGen.Models
{
    [Serializable]
    public class ControllerModel
    {
        #region Fields

        #endregion // Fields

        #region Properties

        [XmlIgnore]
        public long NextID { get; set; }

        [XmlElement(ElementName = "Data")]
        public ControllerDataModel Data { get; set; }

        [XmlArrayItem(ElementName = "FaseCyclus")]
        public List<FaseCyclusModel> Fasen { get; set; }

        [XmlArrayItem(ElementName = "Detector")]
        public List<DetectorModel> Detectoren { get; set; }

        [XmlArrayItem(ElementName = "RichtingGevoeligeAanvraag")]
        public List<RichtingGevoeligeAanvraagModel> RichtingGevoeligeAanvragen { get; set; }

        [XmlArrayItem(ElementName = "RichtingGevoeligVerleng")]
        public List<RichtingGevoeligVerlengModel> RichtingGevoeligVerlengen { get; set; }

        public InterSignaalGroepModel InterSignaalGroep { get; set; } 

        [XmlArrayItem(ElementName = "GroentijdenSet")]
        public List<GroentijdenSetModel> GroentijdenSets { get; set; }

        [XmlArrayItem(ElementName = "Periode")]
        public List<PeriodeModel> Perioden { get; set; }

        public PTPDataModel PTPData { get; set; }

        public OVDataModel OVData { get; set; }

        public ModuleMolenModel ModuleMolen { get; set; }

        public CustomDataModel CustomData { get; set; }

        #endregion // Properties

        #region Constructor

        public ControllerModel() : base()
        {
            Data = new ControllerDataModel();
            Fasen = new List<FaseCyclusModel>();
            Detectoren = new List<DetectorModel>();
            GroentijdenSets = new List<GroentijdenSetModel>();
            ModuleMolen = new ModuleMolenModel();
            Perioden = new List<PeriodeModel>();
            CustomData = new CustomDataModel();
            InterSignaalGroep = new InterSignaalGroepModel();
            RichtingGevoeligeAanvragen = new List<RichtingGevoeligeAanvraagModel>();
            RichtingGevoeligVerlengen = new List<RichtingGevoeligVerlengModel>();
            PTPData = new PTPDataModel();
            OVData = new OVDataModel();
        }

        #endregion // Constructor
    }
}
