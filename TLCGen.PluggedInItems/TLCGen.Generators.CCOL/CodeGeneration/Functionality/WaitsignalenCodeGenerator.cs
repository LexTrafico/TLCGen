﻿using System.Collections.Generic;
using System.Linq;
using System.Text;
using TLCGen.Generators.CCOL.Extensions;
using TLCGen.Generators.CCOL.Settings;
using TLCGen.Models;
using TLCGen.Models.Enumerations;

namespace TLCGen.Generators.CCOL.CodeGeneration
{
    [CCOLCodePieceGenerator]
    public class WaitsignalenCodeGenerator : CCOLCodePieceGeneratorBase
    {
#pragma warning disable 0649
        private CCOLGeneratorCodeStringSettingModel _uswt;
#pragma warning restore 0649

        public override void CollectCCOLElements(ControllerModel c)
        {
            _myElements = new List<CCOLElement>();

            // outputs
            var alldets = c.Fasen.SelectMany(x => x.Detectoren).Concat(c.Detectoren);
            foreach (var d in alldets)
            {
                if(d.Wachtlicht)
                {
                    _myElements.Add(CCOLGeneratorSettingsProvider.Default.CreateElement(_uswt + d.Naam, _uswt, d.WachtlichtBitmapData, d.Naam));
                }
            }
        }

        public override bool HasCCOLElements() => true;
    
        public override int HasCode(CCOLCodeTypeEnum type)
        {
            return type switch
            {
                CCOLCodeTypeEnum.RegCSystemApplication => 80,
                _ => 0
            };
        }

        public override string GetCode(ControllerModel c, CCOLCodeTypeEnum type, string ts)
        {
            if (!c.Fasen.SelectMany(x => x.Detectoren).Any(x2 => x2.Wachtlicht))
                return "";

            var sb = new StringBuilder();

            switch (type)
            {
                case CCOLCodeTypeEnum.RegCSystemApplication:
                    sb.AppendLine($"{ts}/* wachtlicht uitsturing */");
                    sb.AppendLine($"{ts}/* --------------------- */");
                    foreach (var fc in c.Fasen)
                    {
                        foreach (var d in fc.Detectoren)
                        {
                            if (d.Wachtlicht)
                            {
                                var wtdef = d.WachtlichtBitmapData.GetBitmapCoordinaatOutputDefine(_uswt + d.Naam);
                                var ddef = d.GetDefine();
                                var fcdef = fc.GetDefine();
                                if (c.Data.AansturingWaitsignalen == AansturingWaitsignalenEnum.DrukknopGebruik)
                                {
                                    sb.AppendLine($"{ts}CIF_GUS[{wtdef}] = (D[{ddef}] && !SD[{ddef}] || ED[{ddef}]) && A[{fcdef}] && !G[{fcdef}] && REG ? TRUE : CIF_GUS[{wtdef}] && !G[{fcdef}] && REG;");
                                }
                                else if (c.Data.AansturingWaitsignalen == AansturingWaitsignalenEnum.AanvraagGezet)
                                {
                                    sb.AppendLine($"{ts}CIF_GUS[{wtdef}] = !G[{fcdef}] && A[{fcdef}] && REG;");
                                }
                            }
                        }
                    }
                    return sb.ToString();
                default:
                    return null;
            }
        }
    }
}
