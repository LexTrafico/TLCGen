﻿using System.Collections.Generic;
using System.Linq;
using System.Text;
using TLCGen.Generators.CCOL.Settings;
using TLCGen.Models;
using TLCGen.Models.Enumerations;

namespace TLCGen.Generators.CCOL.CodeGeneration.Functionality
{
    [CCOLCodePieceGenerator]
    public class RateltikkersCodeGenerator : CCOLCodePieceGeneratorBase
    {
        #region Fields
        
#pragma warning disable 0649
        private CCOLGeneratorCodeStringSettingModel _usrt;
        private CCOLGeneratorCodeStringSettingModel _usrtdim;
        private CCOLGeneratorCodeStringSettingModel _hrt;
        private CCOLGeneratorCodeStringSettingModel _hdrt;
        private CCOLGeneratorCodeStringSettingModel _tnlrt;
        private CCOLGeneratorCodeStringSettingModel _prmnivongedimd;
        private CCOLGeneratorCodeStringSettingModel _prmnivgedimd;
#pragma warning restore 0649
        private string _hperiod;
        private string _prmperrt;
        private string _prmperrta;
        private string _prmperrtdim;

        #endregion // Fields

        public override void CollectCCOLElements(ControllerModel c)
        {
            _myElements = new List<CCOLElement>();

            foreach (var rt in c.Signalen.Rateltikkers)
            {
                _myElements.Add(CCOLGeneratorSettingsProvider.Default.CreateElement($"{_usrt}{rt.FaseCyclus}", _usrt, rt.BitmapData, rt.FaseCyclus));
                _myElements.Add(CCOLGeneratorSettingsProvider.Default.CreateElement($"{_hrt}{rt.FaseCyclus}", _hrt, rt.FaseCyclus));
                
                if (c.Signalen.DimUitgangPerTikker)
                {
                    _myElements.Add(CCOLGeneratorSettingsProvider.Default.CreateElement($"{_usrtdim}{rt.FaseCyclus}", _usrtdim, rt.DimUitgangBitmapData, rt.FaseCyclus));
                }

                if (rt.Type == RateltikkerTypeEnum.HoeflakeBewaakt && c.Signalen.DimmingNiveauVanuitApplicatie)
                {
                    _myElements.Add(CCOLGeneratorSettingsProvider.Default.CreateElement($"{_prmnivongedimd}{rt.FaseCyclus}", rt.DimmingNiveauPeriodeNietDimmen, CCOLElementTimeTypeEnum.None, _prmnivongedimd, rt.FaseCyclus));
                    _myElements.Add(CCOLGeneratorSettingsProvider.Default.CreateElement($"{_prmnivgedimd}{rt.FaseCyclus}", rt.DimmingNiveauPeriodeDimmen, CCOLElementTimeTypeEnum.None, _prmnivgedimd, rt.FaseCyclus));
                }

                if (rt.Type == RateltikkerTypeEnum.Hoeflake || rt.Type == RateltikkerTypeEnum.HoeflakeBewaakt)
                {
                    _myElements.Add(CCOLGeneratorSettingsProvider.Default.CreateElement($"{_tnlrt}{rt.FaseCyclus}", rt.NaloopTijd, CCOLElementTimeTypeEnum.TE_type, _tnlrt, rt.FaseCyclus));
                }
            }
            foreach (var d in c.Signalen.Rateltikkers.SelectMany(x => x.Detectoren).Select(x => x.Detector).Distinct())
            {
                _myElements.Add(CCOLGeneratorSettingsProvider.Default.CreateElement($"{_hdrt}{d}", _hdrt, d));
            }
        }

        public override bool HasCCOLElements() => true;

        public override int[] HasCode(CCOLCodeTypeEnum type)
        {
            return type switch
            {
                CCOLCodeTypeEnum.RegCSystemApplication => new []{60},
                _ => null
            };
        }

        public override string GetCode(ControllerModel c, CCOLCodeTypeEnum type, string ts, int order)
        {
            var sb = new StringBuilder();

            switch (type)
            {
                case CCOLCodeTypeEnum.RegCSystemApplication:
                    if(c.Signalen.Rateltikkers.Count == 0)
                    {
                        return "";
                    }
                    if (c.Signalen.Rateltikkers.SelectMany(x => x.Detectoren).Any())
                    {
                        sb.AppendLine($"{ts}/* Onthouden drukknop meldingen */");
                        foreach (var d in c.Signalen.Rateltikkers.SelectMany(x => x.Detectoren).Select(x => x.Detector).Distinct())
                        {
                            var fc = c.Fasen.Where(x => x.Detectoren.FirstOrDefault(x2 => x2.Naam == d) != null).FirstOrDefault();
                            if (fc != null)
                            {
                                sb.AppendLine($"{ts}IH[{_hpf}{_hdrt}{d}] = SG[{_fcpf}{fc.Naam}] ? FALSE : IH[{_hpf}{_hdrt}{d}] || (D[{_dpf}{d}] && !SD[{_dpf}{d}] || ED[{_dpf}{d}]) && !G[{_fcpf}{fc.Naam}] && A[{_fcpf}{fc.Naam}];");
                            }
                        }
                        sb.AppendLine();
                    }
                    sb.AppendLine($"{ts}/* Uitsturing aanvraag rateltikkers */");
                    foreach (var rt in c.Signalen.Rateltikkers)
                    {
                        var rta = c.PeriodenData.Perioden.Any(x => x.Type == PeriodeTypeEnum.RateltikkersAanvraag) ? $"{_hpf}{_hperiod}{_prmperrta}" : "NG";
                        var rtr = c.PeriodenData.Perioden.Any(x => x.Type == PeriodeTypeEnum.RateltikkersAltijd) ? $"{_hpf}{_hperiod}{_prmperrt}" : "NG";
                        switch (rt.Type)
                        {
                            case RateltikkerTypeEnum.Accross:
                                sb.Append($"{ts}GUS[{_uspf}{_usrt}{rt.FaseCyclus}] = Rateltikkers_Accross({_fcpf}{rt.FaseCyclus}, {_hpf}{_hrt}{rt.FaseCyclus}, {rta}, {rtr}, ");
                                foreach (var d in rt.Detectoren)
                                {
                                    sb.Append($"{_hpf}{_hdrt}{d.Detector}, ");
                                }
                                sb.AppendLine("END);");
                                break;
                            case RateltikkerTypeEnum.Hoeflake:
                                sb.Append($"{ts}GUS[{_uspf}{_usrt}{rt.FaseCyclus}] = Rateltikkers({_fcpf}{rt.FaseCyclus}, {_hpf}{_hrt}{rt.FaseCyclus}, {rta}, {rtr}, {_tpf}{_tnlrt}{rt.FaseCyclus}, NG, ");
                                foreach (var d in rt.Detectoren)
                                {
                                    sb.Append($"{_hpf}{_hdrt}{d.Detector}, ");
                                }
                                sb.AppendLine("END);");
                                break;
                            case RateltikkerTypeEnum.HoeflakeBewaakt:
                                sb.Append($"{ts}GUS[{_uspf}{_usrt}{rt.FaseCyclus}] = Rateltikkers({_fcpf}{rt.FaseCyclus}, {_hpf}{_hrt}{rt.FaseCyclus}, {rta}, {rtr}, {_tpf}{_tnlrt}{rt.FaseCyclus}, TRUE, ");
                                foreach (var d in rt.Detectoren)
                                {
                                    sb.Append($"{_hpf}{_hdrt}{d.Detector}, ");
                                }
                                sb.AppendLine("END);");
                                break;

                        }
                    }

                    if (c.Signalen.DimUitgangPerTikker)
                    {
                        sb.AppendLine();
                        sb.AppendLine($"{ts}/* uitsturing dimming signaal per rateltikker */");
                        foreach (var rt in c.Signalen.Rateltikkers)
                        {
                            switch (rt.Type)
                            {
                                case RateltikkerTypeEnum.Hoeflake:
                                case RateltikkerTypeEnum.Accross:
                                    sb.AppendLine($"{ts}GUS[{_uspf}{_usrtdim}{rt.FaseCyclus}] = H[{_hpf}{_hperiod}{_prmperrtdim}];");
                                    break;
                                case RateltikkerTypeEnum.HoeflakeBewaakt:
                                    if (c.Signalen.DimmingNiveauVanuitApplicatie)
                                    {
                                        sb.AppendLine($"{ts}GUS[{_uspf}{_usrtdim}{rt.FaseCyclus}] = Rateltikkers_HoeflakeDimming({_hpf}{_hperiod}{_prmperrtdim}, {_prmpf}{_prmnivongedimd}{rt.FaseCyclus}, {_prmpf}{_prmnivgedimd}{rt.FaseCyclus});");
                                    }
                                    else
                                    {
                                        sb.AppendLine($"{ts}GUS[{_uspf}{_usrtdim}{rt.FaseCyclus}] = Rateltikkers_HoeflakeDimming({_hpf}{_hperiod}{_prmperrtdim}, NG, NG);");
                                    }
                                    break;
                            }
                        }
                    }

                    return sb.ToString();

                default:
                    return null;
            }
        }

        public override bool SetSettings(CCOLGeneratorClassWithSettingsModel settings)
        {
            _hperiod = CCOLGeneratorSettingsProvider.Default.GetElementName("hperiod");
            _prmperrt = CCOLGeneratorSettingsProvider.Default.GetElementName("prmperrt");
            _prmperrta = CCOLGeneratorSettingsProvider.Default.GetElementName("prmperrta");
            _prmperrtdim = CCOLGeneratorSettingsProvider.Default.GetElementName("prmperrtdim");

            return base.SetSettings(settings);
        }
    }
}
