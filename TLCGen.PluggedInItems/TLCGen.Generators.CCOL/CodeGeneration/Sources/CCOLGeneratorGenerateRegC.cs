﻿using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using TLCGen.Models;
using TLCGen.Generators.Shared;
using TLCGen.Models.Enumerations;

namespace TLCGen.Generators.CCOL.CodeGeneration
{
    public partial class CCOLGenerator
    {
        private string GenerateRegC(ControllerModel controller, ICCOLGeneratorSettingsProvider settingsProvider)
        {
            var sb = new StringBuilder();
            sb.AppendLine("/* REGELPROGRAMMA */");
            sb.AppendLine("/* -------------- */");
            sb.AppendLine();
            sb.Append(GenerateFileHeader(controller.Data, "reg.c"));
            sb.AppendLine();
            sb.Append(GenerateVersionHeader(controller.Data));
            sb.AppendLine();
            sb.AppendLine("#define REG (CIF_WPS[CIF_PROG_STATUS] == CIF_STAT_REG)");
            if(controller.InterSignaalGroep?.Nalopen?.Count > 0)
            {
                sb.AppendLine("#define NALOPEN");
            }
            if (controller.PrioData.PrioIngrepen.Count > 0 || controller.PrioData.HDIngrepen.Count > 0)
            {
                sb.AppendLine("#define PRIO_ADDFILE");
            }
            sb.AppendLine();
            sb.Append(GenerateRegCBeforeIncludes(controller));
            sb.Append(GenerateRegCIncludes(controller));
            sb.Append(GenerateRegCTop(controller));
            if (controller.Data.KWCType != KWCTypeEnum.Geen && controller.Data.KWCUitgebreid)
            {
                sb.AppendLine();
                sb.Append(GenerateRegCKwcApplication(controller));
            }
            sb.Append(GenerateRegCPreApplication(controller));
            sb.Append(GenerateRegCKlokPerioden(controller));
            sb.Append(GenerateRegCAanvragen(controller));
            if (controller.Data.SynchronisatiesType == SynchronisatiesTypeEnum.RealFunc)
            {
                sb.Append(GenerateRegCRealisatieTijden(controller));
            }
            sb.Append(GenerateRegCMaxOfVerlenggroen(controller, settingsProvider));
            sb.Append(GenerateRegCWachtgroen(controller, settingsProvider));
            sb.Append(GenerateRegCMeetkriterium(controller));
            sb.Append(GenerateRegCMeeverlengen(controller, settingsProvider));
            sb.Append(GenerateRegCSynchronisaties(controller));
            sb.Append(GenerateRegCRealisatieAfhandeling(controller));
            sb.Append(GenerateRegCFileVerwerking(controller));
            sb.Append(GenerateRegCDetectieStoring(controller));
            sb.Append(GenerateRegCInitApplication(controller));
            sb.Append(GenerateRegCPostApplication(controller));
            sb.Append(GenerateRegCApplication(controller, settingsProvider));
            sb.Append(GenerateRegCSystemApplication(controller));
            if(controller.Data.CCOLVersie >= CCOLVersieEnum.CCOL9)
            {
                sb.Append(GenerateRegCSystemApplication2(controller));
            }
            sb.Append(GenerateRegCDumpApplication(controller));
            sb.Append(GenerateRegCSpecialSignals(controller));

            return sb.ToString();
        }

        private string GenerateRegCBeforeIncludes(ControllerModel controller)
        {
            var sb = new StringBuilder();

            CCOLCodeGenerationHelper.AddCodeTypeToStringBuilder(controller, OrderedPieceGenerators, sb, CCOLCodeTypeEnum.RegCBeforeIncludes, false, true, false, true, ts);

            return sb.ToString();
        }

        private string GenerateRegCIncludes(ControllerModel controller)
        {
            var sb = new StringBuilder();

            sb.AppendLine("/* include files */");
            sb.AppendLine("/* ------------- */");
            sb.AppendLine($"{ts}#include \"{controller.Data.Naam}sys.h\"");
            sb.AppendLine($"{ts}#include \"stdfunc.h\"  /* standaard functies                */");
            sb.AppendLine($"{ts}#include \"fcvar.c\"    /* fasecycli                         */");
            sb.AppendLine($"{ts}#include \"kfvar.c\"    /* conflicten                        */");
            sb.AppendLine($"{ts}#include \"usvar.c\"    /* uitgangs elementen                */");
            sb.AppendLine($"{ts}#include \"dpvar.c\"    /* detectie elementen                */");
            if (controller.Data.GarantieOntruimingsTijden)
            {
                if(controller.Data.CCOLVersie >= CCOLVersieEnum.CCOL95 && controller.Data.Intergroen)
                {
                    sb.AppendLine($"{ts}#include \"tig_min.c\"   /* garantie-ontruimingstijden        */");
                }
                else
                {
                    sb.AppendLine($"{ts}#include \"to_min.c\"   /* garantie-ontruimingstijden        */");
                }
            }
            sb.AppendLine($"{ts}#include \"trg_min.c\"  /* garantie-roodtijden               */");
            sb.AppendLine($"{ts}#include \"tgg_min.c\"  /* garantie-groentijden              */");
            sb.AppendLine($"{ts}#include \"tgl_min.c\"  /* garantie-geeltijden               */");
            sb.AppendLine($"{ts}#include \"isvar.c\"    /* ingangs elementen                 */");
            if (controller.HasDSI())
            {
                sb.AppendLine($"{ts}#include \"dsivar.c\"   /* selectieve detectie               */");
            }
            sb.AppendLine($"{ts}#include \"hevar.c\"    /* hulp elementen                    */");
            sb.AppendLine($"{ts}#include \"mevar.c\"    /* geheugen elementen                */");
            sb.AppendLine($"{ts}#include \"tmvar.c\"    /* tijd elementen                    */");
            sb.AppendLine($"{ts}#include \"ctvar.c\"    /* teller elementen                  */");
            sb.AppendLine($"{ts}#include \"schvar.c\"   /* software schakelaars              */");
	        if (controller.HalfstarData.IsHalfstar)
	        {
                if(controller.Data.CCOLVersie >= CCOLVersieEnum.CCOL95)
                {
				    sb.AppendLine($"{ts}#include \"trigvar.c\"   /* uitgebreide signaalplan structuur */");
                }
                else
                {
				    sb.AppendLine($"{ts}#include \"tigvar.c\"   /* uitgebreide signaalplan structuur */");
                }
				sb.AppendLine($"{ts}#include \"plevar.c\"   /* uitgebreide signaalplan structuur */");
	        }
            sb.AppendLine($"{ts}#include \"prmvar.c\"   /* parameters                        */");
            sb.AppendLine($"{ts}#include \"lwmlvar.c\"  /* langstwachtende modulen structuur */");
            if(controller.Data.VLOGType != VLOGTypeEnum.Geen)
            {
                sb.AppendLine($"{ts}#ifndef NO_VLOG");
                sb.AppendLine($"{ts}{ts}#include \"vlogvar.c\"  /* variabelen t.b.v. vlogfuncties                */");
                sb.AppendLine($"{ts}{ts}#include \"logvar.c\"   /* variabelen t.b.v. logging                     */");
                sb.AppendLine($"{ts}{ts}#include \"monvar.c\"   /* variabelen t.b.v. realtime monitoring         */");
                sb.AppendLine($"{ts}{ts}#include \"fbericht.h\"");
                sb.AppendLine($"{ts}#endif");
            }
            if(controller.PrioData.PrioIngrepen.Count > 0 || controller.PrioData.HDIngrepen.Count > 0)
            {
                sb.AppendLine($"{ts}#include \"prio.h\"       /* prio-afhandeling                  */");
            }
            if (controller.RISData.RISToepassen)
            {
                sb.AppendLine($"{ts}#ifndef NO_RIS");
                sb.AppendLine($"{ts}{ts}#include \"risvar.c\" /* ccol ris controller */");
                sb.AppendLine($"{ts}{ts}#include \"risappl.c\" /* RIS applicatiefuncties */");
                if (controller.PrioData.PrioIngrepen.Any(x => x.MeldingenData.Inmeldingen.Any(x2 => x2.Type == PrioIngreepInUitMeldingVoorwaardeTypeEnum.RISVoorwaarde)) ||
                    controller.PrioData.PrioIngrepen.Any(x => x.MeldingenData.Uitmeldingen.Any(x2 => x2.Type == PrioIngreepInUitMeldingVoorwaardeTypeEnum.RISVoorwaarde)))
                {
                    sb.AppendLine($"{ts}{ts}#if (CCOL_V > 100)");
                    sb.AppendLine($"{ts}{ts}#define RIS_SSM  /* Gebruik in/uitmelden via RIS SSM */");
                    sb.AppendLine($"{ts}{ts}#endif");
                }
                sb.AppendLine($"{ts}{ts}#include \"extra_func_ris.c\" /* RIS extra functies */");
                sb.AppendLine($"{ts}{ts}#if (!defined AUTOMAAT && !defined AUTOMAAT_TEST)");
                sb.AppendLine($"{ts}{ts}{ts}#include \"rissimvar.h\" /* ccol ris simulatie functie */");
                sb.AppendLine($"{ts}{ts}#endif");
                sb.AppendLine($"{ts}#endif");
            }
            sb.AppendLine($"{ts}#include \"prsvar.c\"   /* parameters parser                 */");
            sb.AppendLine($"{ts}#include \"control.c\"  /* controller interface              */");
            sb.AppendLine($"{ts}#include \"rtappl.h\"   /* applicatie routines               */");
            if(controller.PrioData.PrioIngrepen.Count > 0 || controller.PrioData.HDIngrepen.Count > 0)
            {
                if(controller.PrioData.PrioIngrepen.Any(x => x.CheckWagenNummer))
                {
                    sb.AppendLine($"{ts}#define PRIO_CHECK_WAGENNMR /* check op wagendienstnummer          */");
                }
                sb.AppendLine($"{ts}#include \"extra_func_prio.c\" /* extra standaard functies OV     */");
            }
            sb.AppendLine($"{ts}#include \"extra_func.c\" /* extra standaard functies        */");
            sb.AppendLine();
            sb.AppendLine("#if (!defined AUTOMAAT && !defined AUTOMAAT_TEST)");
            sb.AppendLine("/*    #include \"ccdump.inc\" */");
            sb.AppendLine($"{ts}#include \"keysdef.c\"     /* Definitie toetsenbord t.b.v. stuffkey  */");
            sb.AppendLine($"{ts}#if !defined (_DEBUG)");
            sb.AppendLine($"{ts}{ts}#include \"xyprintf.h\" /* Printen debuginfo                      */");
            sb.AppendLine($"{ts}#endif");
            sb.AppendLine("#endif");
            sb.AppendLine();
            sb.AppendLine($"{ts}#include \"detectie.c\"");
            sb.AppendLine($"{ts}#include \"ccolfunc.c\"");
            if (controller.Data.FixatieMogelijk)
            {
                sb.AppendLine($"{ts}#include \"fixatie.c\"");
            }
            if (controller.Data.SynchronisatiesType == SynchronisatiesTypeEnum.SyncFunc &&
                (controller.InterSignaalGroep.Voorstarten.Any() || controller.InterSignaalGroep.Gelijkstarten.Any()))
            {
                sb.AppendLine($"{ts}#include \"syncvar.c\"  /* synchronisatie functies           */");
            }

	        if (controller.HalfstarData.IsHalfstar)
	        {
		        sb.AppendLine($"{ts}#include \"{controller.Data.Naam}hst.c\"");
	        }
            foreach (var gen in OrderedPieceGenerators[CCOLCodeTypeEnum.RegCIncludes])
            {
                sb.Append(gen.Value.GetCode(controller, CCOLCodeTypeEnum.RegCIncludes, ts));
            }
            sb.AppendLine($"{ts}#include \"{controller.Data.Naam}reg.add\"");
            sb.AppendLine();

            return sb.ToString();
        }

        private string GenerateRegCTop(ControllerModel c)
        {
            var sb = new StringBuilder();

            sb.AppendLine("mulv TDH_old[DPMAX];");
            sb.AppendLine("mulv DB_old[DPMAX];");
            sb.AppendLine("mulv DVG[DPMAX]; /* T.b.v. veiligheidsgroen */");
            sb.AppendLine();


            if (c.Data.CCOLMulti)
            {
                sb.AppendLine($"s_int16 CCOL_SLAVE = {c.Data.CCOLMultiSlave};");
                sb.AppendLine();
            }

            foreach (var gen in OrderedPieceGenerators[CCOLCodeTypeEnum.RegCTop])
            {
                sb.Append(gen.Value.GetCode(c, CCOLCodeTypeEnum.RegCTop, ts));
            }
            sb.AppendLine();

            return sb.ToString();
        }

        private string GenerateRegCKwcApplication(ControllerModel controller)
        {
            var sb = new StringBuilder();

            sb.AppendLine("void KwcApplication(void)");
            sb.AppendLine("{");

            foreach (var gen in OrderedPieceGenerators[CCOLCodeTypeEnum.RegCKwcApplication])
            {
                sb.Append(gen.Value.GetCode(controller, CCOLCodeTypeEnum.RegCKwcApplication, ts));
            }
            
            sb.AppendLine($"{ts}KwcApplication_Add();");
            sb.AppendLine("}");
            sb.AppendLine();

            return sb.ToString();
        }

        //private void GetVariables(StringBuilder sb, CCOLCodeTypeEnum type, ControllerModel controller)
        //{
        //    if (CCOLElementCollector.FunctionLocalVariables.ContainsKey(type))
        //    {
        //        foreach (var i in CCOLElementCollector.FunctionLocalVariables[type])
        //        {
        //            sb.AppendLine($"{ts}{i.Item1} {i.Item2};");
        //        }
        //        sb.AppendLine();
        //    }
        //}
        //
        //private void GetVariablesAndCode(StringBuilder sb, CCOLCodeTypeEnum type, ControllerModel controller)
        //{
        //    if (CCOLElementCollector.FunctionLocalVariables.ContainsKey(type))
        //    {
        //        foreach (var i in CCOLElementCollector.FunctionLocalVariables[type])
        //        {
        //            sb.AppendLine($"{ts}{i.Item1} {i.Item2};");
        //        }
        //        sb.AppendLine();
        //    }
        //
        //    foreach (var gen in OrderedPieceGenerators[type])
        //    {
        //        sb.Append(gen.Value.GetCode(controller, type, ts));
        //    }
        //}
        //
        //
        //private void GetCode(StringBuilder sb, CCOLCodeTypeEnum type, ControllerModel controller)
        //{
        //    foreach (var gen in OrderedPieceGenerators[type])
        //    {
        //        sb.Append(gen.Value.GetCode(controller, type, ts));
        //    }
        //}

        private string GenerateRegCPreApplication(ControllerModel controller)
        {
            var sb = new StringBuilder();

            sb.AppendLine("void PreApplication(void)");
            sb.AppendLine("{");

            CCOLCodeGenerationHelper.AddCodeTypeToStringBuilder(controller, OrderedPieceGenerators, sb, CCOLCodeTypeEnum.RegCPreApplication, true, true, false, true, ts);

            sb.AppendLine($"{ts}PreApplication_Add();");

	        if (controller.HalfstarData.IsHalfstar)
	        {
		        sb.AppendLine($"{ts}pre_application_halfstar();");
	        }
	        sb.AppendLine();

	        if (controller.HalfstarData.IsHalfstar || controller.Fasen.Any(x => x.WachttijdVoorspeller) ||
                controller.Fasen.Any(x => x.SchoolIngreep != NooitAltijdAanUitEnum.Nooit || x.SeniorenIngreep != NooitAltijdAanUitEnum.Nooit))
	        {
		        sb.AppendLine($"{ts}/* Genereren knippersignalen */");
		        sb.AppendLine($"{ts}UpdateKnipperSignalen();");
	        }

	        sb.AppendLine("}");
	        sb.AppendLine();

            return sb.ToString();
        }

        private string GenerateRegCKlokPerioden(ControllerModel controller)
        {
            var sb = new StringBuilder();

            sb.AppendLine("void KlokPerioden(void)");
            sb.AppendLine("{");

            CCOLCodeGenerationHelper.AddCodeTypeToStringBuilder(controller, OrderedPieceGenerators, sb, CCOLCodeTypeEnum.RegCKlokPerioden, true, true, false, true, ts);

            sb.AppendLine($"{ts}KlokPerioden_Add();");
            
	        if (controller.HalfstarData.IsHalfstar)
	        {
		        sb.AppendLine($"{ts}KlokPerioden_halfstar();");
	        }

	        sb.AppendLine("}");
	        sb.AppendLine();

            return sb.ToString();
        }

        private string GenerateRegCAanvragen(ControllerModel controller)
        {
            var sb = new StringBuilder();

            sb.AppendLine("void Aanvragen(void)");
            sb.AppendLine("{");

            CCOLCodeGenerationHelper.AddCodeTypeToStringBuilder(controller, OrderedPieceGenerators, sb, CCOLCodeTypeEnum.RegCAanvragen, true, true, false, true, ts);

            sb.AppendLine($"{ts}Aanvragen_Add();");
			
	        if (controller.HalfstarData.IsHalfstar)
	        {
		        sb.AppendLine($"{ts}Aanvragen_halfstar();");
	        }

            sb.AppendLine("}");
            sb.AppendLine();

            return sb.ToString();
        }
        
        private string GenerateRegCRealisatieTijden(ControllerModel controller)
        {
            var sb = new StringBuilder();

            sb.AppendLine("void RealisatieTijden(void)");
            sb.AppendLine("{");

            CCOLCodeGenerationHelper.AddCodeTypeToStringBuilder(controller, OrderedPieceGenerators, sb, CCOLCodeTypeEnum.RegCRealisatieTijden, true, true, false, true, ts);

            sb.AppendLine($"{ts}RealisatieTijden_Add();");
			
            // TODO Realisatie tijden tijdens halfstar, relevant als aparte functie?
            //if (controller.HalfstarData.IsHalfstar)
            //{
            //    sb.AppendLine($"{ts}RealisatieTijden_halfstar();");
            //}

            sb.AppendLine("}");
            sb.AppendLine();

            return sb.ToString();
        }

        private string GenerateRegCMaxOfVerlenggroen(ControllerModel controller, ICCOLGeneratorSettingsProvider settingsProvider)
        {
            var sb = new StringBuilder();

            var _hplact = settingsProvider.GetElementName("hplact");

            switch (controller.Data.TypeGroentijden)
            {
                case GroentijdenTypeEnum.MaxGroentijden:
                    sb.AppendLine("void Maxgroen(void)");
                    sb.AppendLine("{");

                    CCOLCodeGenerationHelper.AddCodeTypeToStringBuilder(controller, OrderedPieceGenerators, sb, CCOLCodeTypeEnum.RegCMaxgroen, true, true, false, true, ts);

                    // Add file
                    sb.AppendLine($"{ts}Maxgroen_Add();");

                    CCOLCodeGenerationHelper.AddCodeTypeToStringBuilder(controller, OrderedPieceGenerators, sb, CCOLCodeTypeEnum.RegCMaxgroenNaAdd, true, true, false, false, ts);

                    sb.AppendLine("}");

                    break;

                case GroentijdenTypeEnum.VerlengGroentijden:
                    sb.AppendLine("void Verlenggroen(void)");
                    sb.AppendLine("{");

                    CCOLCodeGenerationHelper.AddCodeTypeToStringBuilder(controller, OrderedPieceGenerators, sb, CCOLCodeTypeEnum.RegCVerlenggroen, true, true, false, true, ts);

                    // Add file
                    sb.AppendLine($"{ts}Maxgroen_Add();");
                    sb.AppendLine("}");

                    sb.AppendLine();
                    break;
            }
            sb.AppendLine();

            return sb.ToString();
        }

        private string GenerateRegCWachtgroen(ControllerModel controller, ICCOLGeneratorSettingsProvider settingsProvider)
        {
            var sb = new StringBuilder();
            var _hplact = settingsProvider.GetElementName("hplact");

            sb.AppendLine("void Wachtgroen(void)");
            sb.AppendLine("{");

            CCOLCodeGenerationHelper.AddCodeTypeToStringBuilder(controller, OrderedPieceGenerators, sb, CCOLCodeTypeEnum.RegCWachtgroen, true, true, false, true, ts);

            sb.AppendLine($"{ts}Wachtgroen_Add();");
	        sb.AppendLine("}");

            return sb.ToString();
        }

        private string GenerateRegCMeetkriterium(ControllerModel controller)
        {
            var sb = new StringBuilder();

            sb.AppendLine("void Meetkriterium(void)");
            sb.AppendLine("{");
            
            CCOLCodeGenerationHelper.AddCodeTypeToStringBuilder(controller, OrderedPieceGenerators, sb, CCOLCodeTypeEnum.RegCMeetkriterium, true, true, false, true, ts);

            sb.AppendLine($"{ts}Meetkriterium_Add();");

            sb.AppendLine("}");
            sb.AppendLine();

            return sb.ToString();
        }

        private string GenerateRegCMeeverlengen(ControllerModel controller, ICCOLGeneratorSettingsProvider settingsProvider)
        {
            var sb = new StringBuilder();
            var _hplact = settingsProvider.GetElementName("hplact");

            sb.AppendLine("void Meeverlengen(void)");
            sb.AppendLine("{");

            CCOLCodeGenerationHelper.AddCodeTypeToStringBuilder(controller, OrderedPieceGenerators, sb, CCOLCodeTypeEnum.RegCMeeverlengen, true, true, false, true, ts);

            sb.AppendLine($"{ts}Meeverlengen_Add();");
            sb.AppendLine("}");

            return sb.ToString();
        }

        private string GenerateRegCSynchronisaties(ControllerModel controller)
        {
            var sb = new StringBuilder();

            sb.AppendLine("void Synchronisaties(void)");
            sb.AppendLine("{");

            CCOLCodeGenerationHelper.AddCodeTypeToStringBuilder(controller, OrderedPieceGenerators, sb, CCOLCodeTypeEnum.RegCSynchronisaties, true, true, false, true, ts);

            sb.AppendLine($"{ts}Synchronisaties_Add();");
	        sb.AppendLine("}");
            sb.AppendLine();

            return sb.ToString();
        }

        private string GenerateRegCRealisatieAfhandeling(ControllerModel c)
        {
            var sb = new StringBuilder();

            sb.AppendLine("void RealisatieAfhandeling(void)");
            sb.AppendLine("{");

            CCOLCodeGenerationHelper.AddCodeTypeToStringBuilder(c, OrderedPieceGenerators, sb, CCOLCodeTypeEnum.RegCRealisatieAfhandeling, true, false, false, true, ts);
            CCOLCodeGenerationHelper.AddCodeTypeToStringBuilder(c, OrderedPieceGenerators, sb, CCOLCodeTypeEnum.RegCRealisatieAfhandelingVoorModules, true, false, false, true, ts);
            CCOLCodeGenerationHelper.AddCodeTypeToStringBuilder(c, OrderedPieceGenerators, sb, CCOLCodeTypeEnum.RegCRealisatieAfhandelingModules, true, false, false, true, ts);
            CCOLCodeGenerationHelper.AddCodeTypeToStringBuilder(c, OrderedPieceGenerators, sb, CCOLCodeTypeEnum.RegCRealisatieAfhandelingNaModules, true, false, false, true, ts);
            CCOLCodeGenerationHelper.AddCodeTypeToStringBuilder(c, OrderedPieceGenerators, sb, CCOLCodeTypeEnum.RegCRealisatieAfhandelingVoorModules, false, true, false, true, ts);
            CCOLCodeGenerationHelper.AddCodeTypeToStringBuilder(c, OrderedPieceGenerators, sb, CCOLCodeTypeEnum.RegCRealisatieAfhandelingModules, false, true, false, true, ts);
            CCOLCodeGenerationHelper.AddCodeTypeToStringBuilder(c, OrderedPieceGenerators, sb, CCOLCodeTypeEnum.RegCRealisatieAfhandelingNaModules, false, true, false, true, ts);

            var molens = new List<ModuleMolenModel> { c.ModuleMolen };
            if (c.Data.MultiModuleReeksen)
            {
                molens = c.MultiModuleMolens.Where(x => x.Modules.Any(x2 => x2.Fasen.Any())).ToList();
            }

            if (c.InterSignaalGroep.Nalopen.Any())
            {
                foreach(var m in molens)
                {
                    sb.AppendLine($"{ts}Y{m.Reeks}[{m.Reeks}] = yml_cv_pr_nl(PR{m.Reeks}, {m.Reeks}, {m.Reeks}_MAX);");
                }
            }
            else
            {
                foreach (var m in molens)
                {
                    sb.AppendLine($"{ts}Y{m.Reeks}[{m.Reeks}] = yml_cv_pr(PR{m.Reeks}, {m.Reeks}, {m.Reeks}_MAX);");
                }
            }
            sb.AppendLine();
            foreach (var m in molens)
            {
                if (!m.Modules.Any()) continue;
                foreach (var mm in m.Modules)
                {
                    var mmNaam = Regex.Replace(mm.Naam, @"ML[A-E]+", "ML");
                    if (mm.Naam == m.WachtModule)
                    {
                        sb.AppendLine($"{ts}Y{m.Reeks}[{mmNaam}] |= yml_wml(PR{m.Reeks}, {m.Reeks}_MAX);");
                    }
                    else
                        sb.AppendLine($"{ts}Y{m.Reeks}[{mmNaam}] |= FALSE;");
                }
            }
            sb.AppendLine();
            sb.AppendLine($"{ts}Modules_Add();");
	        sb.AppendLine();
            foreach (var m in molens)
            {
                sb.AppendLine($"{ts}S{m.Reeks} = modules({m.Reeks}_MAX, PR{m.Reeks}, Y{m.Reeks}, &{m.Reeks});");
            }
            sb.AppendLine();
            if (!c.Data.MultiModuleReeksen)
            {
                sb.AppendLine($"{ts}for (fc = 0; fc < FCMAX; ++fc)");
                sb.AppendLine($"{ts}" + "{");
                sb.AppendLine($"{ts}{ts}YM[fc] &= ~BIT5;");
                sb.AppendLine($"{ts}{ts}YM[fc] |= SML && PG[fc] ? BIT5 : FALSE;");
                sb.AppendLine($"{ts}" + "}");
            }
            else
            {
                sb.AppendLine($"{ts}for (fc = 0; fc < FCMAX; ++fc)");
                sb.AppendLine($"{ts}" + "{");
                sb.AppendLine($"{ts}{ts}YM[fc] &= ~BIT5;");
                sb.AppendLine($"{ts}" + "}");
                foreach (var m in molens)
                {
                    if (!m.Modules.Any()) continue;
                    foreach(var fc in m.Modules.SelectMany(x => x.Fasen).Distinct())
                    {
                        sb.AppendLine($"{ts}YM[{_fcpf}{fc.FaseCyclus}] |= S{m.Reeks} && PG[{_fcpf}{fc.FaseCyclus}] ? BIT5 : FALSE;");
                    }
                }
            }
            sb.AppendLine();

            CCOLCodeGenerationHelper.AddCodeTypeToStringBuilder(c, OrderedPieceGenerators, sb, CCOLCodeTypeEnum.RegCRealisatieAfhandeling, false, true, false, true, ts);

            sb.AppendLine($"{ts}RealisatieAfhandeling_Add();");
	        sb.AppendLine("}");
            sb.AppendLine();

            return sb.ToString();
        }

        private string GenerateRegCFileVerwerking(ControllerModel controller)
        {
            var sb = new StringBuilder();

            sb.AppendLine("void FileVerwerking(void)");
            sb.AppendLine("{");

            CCOLCodeGenerationHelper.AddCodeTypeToStringBuilder(controller, OrderedPieceGenerators, sb, CCOLCodeTypeEnum.RegCFileVerwerking, true, true, false, true, ts);

            sb.AppendLine($"{ts}FileVerwerking_Add();");
	        sb.AppendLine("}");
            sb.AppendLine();

            return sb.ToString();
        }

        private string GenerateRegCDetectieStoring(ControllerModel controller)
        {
            var sb = new StringBuilder();

            sb.AppendLine("void DetectieStoring(void)");
            sb.AppendLine("{");

            CCOLCodeGenerationHelper.AddCodeTypeToStringBuilder(controller, OrderedPieceGenerators, sb, CCOLCodeTypeEnum.RegCDetectieStoring, true, true, false, true, ts);

            sb.AppendLine($"{ts}DetectieStoring_Add();");
	        sb.AppendLine("}");
            sb.AppendLine();

            return sb.ToString();
        }

        private string GenerateRegCInitApplication(ControllerModel controller)
        {
            var sb = new StringBuilder();

            sb.AppendLine("void init_application(void)");
            sb.AppendLine("{");

            CCOLCodeGenerationHelper.AddCodeTypeToStringBuilder(controller, OrderedPieceGenerators, sb, CCOLCodeTypeEnum.RegCInitApplication, true, false, false, true, ts);

            sb.AppendLine("#if (!defined AUTOMAAT && !defined AUTOMAAT_TEST) && !defined VISSIM");
            sb.AppendLine($"{ts}if (!SAPPLPROG)");
            sb.AppendLine($"{ts}{ts}stuffkey(CTRLF4KEY);");
            sb.AppendLine("#endif");
            sb.AppendLine();
            if (controller.Data.SynchronisatiesType == SynchronisatiesTypeEnum.SyncFunc &&
                (controller.InterSignaalGroep.Voorstarten.Count > 0 ||
                 controller.InterSignaalGroep.Gelijkstarten.Count > 0))
            {
                sb.AppendLine($"{ts}init_realisation_timers();");
                sb.AppendLine();
            }

            CCOLCodeGenerationHelper.AddCodeTypeToStringBuilder(controller, OrderedPieceGenerators, sb, CCOLCodeTypeEnum.RegCInitApplication, false, true, false, true, ts);

            sb.AppendLine($"{ts}post_init_application();");
	        if (controller.HalfstarData.IsHalfstar)
	        {
		        sb.AppendLine($"{ts}post_init_application_halfstar();");
	        }
            sb.AppendLine("}");
            sb.AppendLine();

            return sb.ToString();
        }

        private string GenerateRegCApplication(ControllerModel controller, ICCOLGeneratorSettingsProvider settingsProvider)
        {
            var sb = new StringBuilder();
            var _hplact = settingsProvider.GetElementName("hplact");
            var _hmlact = settingsProvider.GetElementName("hmlact");
            var _schbmfix = settingsProvider.GetElementName("schbmfix");
            var _schovpriople = settingsProvider.GetElementName("schovpriople");
            var _isfix = settingsProvider.GetElementName("isfix");
            var _prmfb = settingsProvider.GetElementName("prmfb");
            var _schstar = settingsProvider.GetElementName("schstar");
            var _mstarprog = settingsProvider.GetElementName("mstarprog");

            sb.AppendLine("void application(void)");
            sb.AppendLine("{");
            sb.AppendLine($"{ts}PreApplication();");
            sb.AppendLine();
            sb.AppendLine($"{ts}TFB_max = PRM[{_prmpf}{_prmfb}];");
            sb.AppendLine($"{ts}KlokPerioden();");
            sb.AppendLine($"{ts}Aanvragen();");

            if (controller.Data.SynchronisatiesType == SynchronisatiesTypeEnum.RealFunc)
            {
                sb.AppendLine($"{ts}RealisatieTijden();");
            }

            var tsts = (controller.StarData.ToepassenStar || controller.HalfstarData.IsHalfstar) ? ts + ts : ts;

            if (controller.StarData.ToepassenStar)
            {
                sb.AppendLine($"{ts}star_reset_bits(MM[{_mpf}{_mstarprog}] != 0);");
                sb.AppendLine($"{ts}if (MM[{_mpf}{_mstarprog}] != 0)");
                sb.AppendLine($"{ts}{{");
                sb.AppendLine($"{ts}{ts}star_instellingen();");
                sb.AppendLine($"{ts}{ts}star_regelen();");
                sb.AppendLine($"{ts}}}");
            }
            if (controller.HalfstarData.IsHalfstar)
            {
                sb.Append(ts);
                if (controller.StarData.ToepassenStar)
                {
                    sb.Append("else ");
                }
                sb.AppendLine($"if (IH[{_hpf}{_hplact}])");
                sb.AppendLine($"{ts}{{");
                switch (controller.Data.TypeGroentijden)
                {
                    case GroentijdenTypeEnum.MaxGroentijden:
                        sb.AppendLine($"{tsts}Maxgroen_halfstar();");
                        break;
                    case GroentijdenTypeEnum.VerlengGroentijden:
                        sb.AppendLine($"{tsts}Verlenggroen_halfstar();");
                        break;
                }
                sb.AppendLine($"{tsts}Wachtgroen_halfstar();");
                sb.AppendLine($"{tsts}Meetkriterium();");
                sb.AppendLine($"{tsts}Meetkriterium_halfstar();");
                sb.AppendLine($"{tsts}Meeverlengen_halfstar();");
                sb.AppendLine($"{tsts}Synchronisaties_halfstar();");
                sb.AppendLine($"{tsts}RealisatieAfhandeling_halfstar();");
                sb.AppendLine($"{tsts}Alternatief_halfstar();");
                sb.AppendLine($"{tsts}FileVerwerking();");
                sb.AppendLine($"{tsts}FileVerwerking_halfstar();");
                sb.AppendLine($"{tsts}DetectieStoring();");
                sb.AppendLine($"{tsts}DetectieStoring_halfstar();");
                sb.AppendLine($"{ts}}}");
            }

            if (controller.StarData.ToepassenStar || controller.HalfstarData.IsHalfstar)
            {
                sb.AppendLine($"{ts}else");
                sb.AppendLine($"{ts}{{");
            }

            switch (controller.Data.TypeGroentijden)
            {
                case GroentijdenTypeEnum.MaxGroentijden:
                    sb.AppendLine($"{tsts}Maxgroen();");
                    break;
                case GroentijdenTypeEnum.VerlengGroentijden:
                    sb.AppendLine($"{tsts}Verlenggroen();");
                    break;
            }
            sb.AppendLine($"{tsts}Wachtgroen();");
            sb.AppendLine($"{tsts}Meetkriterium();");
            sb.AppendLine($"{tsts}Meeverlengen();");
            sb.AppendLine($"{tsts}Synchronisaties();");
            sb.AppendLine($"{tsts}RealisatieAfhandeling();");
            sb.AppendLine($"{tsts}FileVerwerking();");
            sb.AppendLine($"{tsts}DetectieStoring();");

            if (controller.HalfstarData.IsHalfstar || controller.StarData.ToepassenStar)
            {
                sb.AppendLine($"{ts}}}");
            }

            if ((controller.PrioData.PrioIngrepen.Count > 0 ||
                 controller.PrioData.HDIngrepen.Count > 0))
            {
                if (controller.HalfstarData.IsHalfstar)
                {
                    if (!controller.StarData.ToepassenStar)
                    {
                        sb.AppendLine($"{ts}if (IH[{_hpf}{_hmlact}] || SCH[{_schpf}{_schovpriople}]) AfhandelingPrio();");
                    }
                    else
                    {
                        sb.AppendLine($"{ts}if (MM[{_mpf}{_mstarprog}] == 0 && (IH[{_hpf}{_hmlact}] || SCH[{_schpf}{_schovpriople}])) AfhandelingPrio();");
                    }
                    sb.AppendLine($"{ts}else");
                    sb.AppendLine($"{ts}{{");
                    sb.AppendLine($"{ts}{ts}int fc;");
                    sb.AppendLine($"{ts}{ts}RTFB &= ~PRIO_RTFB_BIT;");
                    sb.AppendLine($"{ts}{ts}for (fc = 0; fc < FCMAX; ++fc)");
                    sb.AppendLine($"{ts}{ts}{{");
                    sb.AppendLine($"{ts}{ts}{ts}Z[fc] &= ~PRIO_Z_BIT;");
                    sb.AppendLine($"{ts}{ts}{ts}FM[fc] &= ~PRIO_FM_BIT;");
                    sb.AppendLine($"{ts}{ts}{ts}RW[fc] &= ~PRIO_RW_BIT;");
                    sb.AppendLine($"{ts}{ts}{ts}RR[fc] &= ~PRIO_RR_BIT;");
                    sb.AppendLine($"{ts}{ts}{ts}YV[fc] &= ~PRIO_YV_BIT;");
                    sb.AppendLine($"{ts}{ts}{ts}YM[fc] &= ~PRIO_YM_BIT;");
                    sb.AppendLine($"{ts}{ts}{ts}MK[fc] &= ~PRIO_MK_BIT;");
                    sb.AppendLine($"{ts}{ts}{ts}PP[fc] &= ~PRIO_PP_BIT;");
                    sb.AppendLine($"{ts}{ts}}}");
                    sb.AppendLine($"{ts}}}");
                }
                else if (controller.StarData.ToepassenStar)
                {
                    sb.AppendLine($"{ts}if (MM[{_mpf}{_mstarprog}] == 0) AfhandelingPrio();");
                }
                else
                {
                    sb.AppendLine($"{ts}AfhandelingPrio();");
                }
            }
            if (controller.Data.FixatieData.FixatieMogelijk && !controller.StarData.ToepassenStar)
            {
                if (!controller.Data.MultiModuleReeksen)
                {
                    sb.AppendLine($"{ts}Fixatie({_ispf}{_isfix}, 0, FCMAX-1, SCH[{_schpf}{_schbmfix}], PRML, ML);");
                }
                else
                {
                    foreach(var r in controller.MultiModuleMolens)
                    {
                        sb.AppendLine($"{ts}Fixatie({_ispf}{_isfix}, 0, FCMAX-1, SCH[{_schpf}{_schbmfix}], PR{r.Reeks}, {r.Reeks});");
                    }
                }
            }
            sb.AppendLine("");
            sb.AppendLine($"{ts}PostApplication();");
            if (controller.Data.KWCType != KWCTypeEnum.Geen && controller.Data.KWCUitgebreid)
            {
                sb.AppendLine($"{ts}KwcApplication();");
            }
            sb.AppendLine("}");
            sb.AppendLine();

            return sb.ToString();
        }

        private string GenerateRegCPostApplication(ControllerModel controller)
        {
            var sb = new StringBuilder();

            sb.AppendLine("void PostApplication(void)");
            sb.AppendLine("{");

            CCOLCodeGenerationHelper.AddCodeTypeToStringBuilder(controller, OrderedPieceGenerators, sb, CCOLCodeTypeEnum.RegCPostApplication, true, false, false, true, ts);
            
            sb.AppendLine($"{ts}int i = 0;");
            sb.AppendLine($"{ts}for (; i < DPMAX; ++i)");
            sb.AppendLine($"{ts}{{");
            sb.AppendLine($"{ts}{ts}TDH_old[i] = TDH[i];");
            sb.AppendLine($"{ts}{ts}DB_old[i] = DB[i];");
            sb.AppendLine($"{ts}}}");
            sb.AppendLine();

            CCOLCodeGenerationHelper.AddCodeTypeToStringBuilder(controller, OrderedPieceGenerators, sb, CCOLCodeTypeEnum.RegCPostApplication, false, true, false, true, ts);
            
            sb.AppendLine($"{ts}PostApplication_Add();");
	        if (controller.HalfstarData.IsHalfstar)
	        {
		        sb.AppendLine($"{ts}PostApplication_halfstar();");
	        }
            sb.AppendLine("}");

            return sb.ToString();
        }

        private string GenerateRegCSystemApplication(ControllerModel controller)
        {
            var sb = new StringBuilder();

            sb.AppendLine("void system_application(void)");
            sb.AppendLine("{");

            var vars = new List<string>();
            CCOLCodeGenerationHelper.AddCodeTypeToStringBuilder(controller, OrderedPieceGenerators, sb, CCOLCodeTypeEnum.RegCSystemApplication, true, false, false, true, ts, vars);
            CCOLCodeGenerationHelper.AddCodeTypeToStringBuilder(controller, OrderedPieceGenerators, sb, CCOLCodeTypeEnum.RegCPreSystemApplication, true, false, false, true, ts, vars);
            CCOLCodeGenerationHelper.AddCodeTypeToStringBuilder(controller, OrderedPieceGenerators, sb, CCOLCodeTypeEnum.RegCPostApplication, true, false, false, true, ts, vars);
            CCOLCodeGenerationHelper.AddCodeTypeToStringBuilder(controller, OrderedPieceGenerators, sb, CCOLCodeTypeEnum.RegCPostSystemApplication, true, false, false, true, ts, vars);
            CCOLCodeGenerationHelper.AddCodeTypeToStringBuilder(controller, OrderedPieceGenerators, sb, CCOLCodeTypeEnum.RegCPreSystemApplication, false, true, false, true, ts);
            
            sb.AppendLine($"{ts}pre_system_application();");
	        if (controller.HalfstarData.IsHalfstar)
	        {
		        sb.AppendLine($"{ts}pre_system_application_halfstar();");
	        }
            sb.AppendLine();

            CCOLCodeGenerationHelper.AddCodeTypeToStringBuilder(controller, OrderedPieceGenerators, sb, CCOLCodeTypeEnum.RegCSystemApplication, false, true, false, true, ts);

            if (controller.Data.GarantieOntruimingsTijden)
            {
                sb.AppendLine($"{ts}/* minimumtijden */");
                sb.AppendLine($"{ts}/* ------------- */");
                if (controller.Data.CCOLVersie >= CCOLVersieEnum.CCOL95 && controller.Data.Intergroen)
                {
                    sb.AppendLine($"{ts}check_tig_min();");
                }
                else
                {
                    sb.AppendLine($"{ts}check_to_min();");
                }
            }
            sb.AppendLine($"{ts}check_tgg_min();");
            sb.AppendLine($"{ts}check_tgl_min();");
            sb.AppendLine($"{ts}check_trg_min();");
            sb.AppendLine();

            CCOLCodeGenerationHelper.AddCodeTypeToStringBuilder(controller, OrderedPieceGenerators, sb, CCOLCodeTypeEnum.RegCPostSystemApplication, false, true, false, true, ts);

            sb.AppendLine($"{ts}post_system_application();");
	        if (controller.HalfstarData.IsHalfstar)
	        {
		        sb.AppendLine($"{ts}post_system_application_halfstar();");
	        }
            sb.AppendLine("}");
            sb.AppendLine();

            return sb.ToString();
        }


        private string GenerateRegCSystemApplication2(ControllerModel controller)
        {
            var sb = new StringBuilder();

            sb.AppendLine("void system_application2(void)");
            sb.AppendLine("{");

            CCOLCodeGenerationHelper.AddCodeTypeToStringBuilder(controller, OrderedPieceGenerators, sb, CCOLCodeTypeEnum.RegCSystemApplication2, true, true, false, true, ts);

            sb.AppendLine();
            sb.AppendLine($"{ts}post_system_application2();");

            sb.AppendLine("}");
            sb.AppendLine();

            return sb.ToString();
        }

        private string GenerateRegCDumpApplication(ControllerModel controller)
        {
            var sb = new StringBuilder();

            sb.AppendLine("#define ENDDUMP 21");
            sb.AppendLine("");
            sb.AppendLine("void dump_application(void)");
            sb.AppendLine("{");
            //sb.AppendLine("    if (!EXTRADUMP)");
            //sb.AppendLine("      EXTRADUMP = 1;");
            //sb.AppendLine("");
            //sb.AppendLine($"{tabspace}switch (EXTRADUMP)");
            //sb.AppendLine($"{tabspace}" + "{");
            //sb.AppendLine($"{tabspace}case 1: /* dump_realisation timers */");
            //sb.AppendLine($"{tabspace}{tabspace}if (!SYNCDUMP)");
            //sb.AppendLine($"{tabspace}{tabspace}" + "{");
            //sb.AppendLine($"{tabspace}{tabspace}{tabspace}DUMP = ENDDUMP;");
            //sb.AppendLine($"{tabspace}{tabspace}{tabspace}SYNCDUMP = 1;");
            //sb.AppendLine($"{tabspace}{tabspace}" + "}");
            //sb.AppendLine($"{tabspace}dump_realisation_timers();");
            //sb.AppendLine($"{tabspace}if (!SYNCDUMP)");
            //sb.AppendLine($"{tabspace}{tabspace}EXTRADUMP++;");
            //sb.AppendLine($"{tabspace}else");
            //sb.AppendLine($"{tabspace}" + "{");
            //sb.AppendLine($"{tabspace}{tabspace}DUMP = ENDDUMP;");
            //sb.AppendLine($"{tabspace}{tabspace}break;");
            //sb.AppendLine($"{tabspace}" + "}");
            //sb.AppendLine($"{tabspace}case 2: /* dump applicatiegegevens */");
            //sb.AppendLine($"{tabspace}{tabspace}if (!waitterm((mulv) 30)) uber_puts(\"\nPlaats: {controller.Data.Stad}\n\");");
            //sb.AppendLine($"{tabspace}{tabspace}if (!waitterm((mulv) 25)) uber_puts(\"Kruispunt: {controller.Data.Naam}\n\");");
            //sb.AppendLine($"{tabspace}{tabspace}if (!waitterm((mulv) 30)) uber_puts(\"         : {controller.Data.Straat1}\n\");");
            //sb.AppendLine($"{tabspace}{tabspace}if (!waitterm((mulv) 30)) uber_puts(\"         : {controller.Data.Straat2}\n\");");
            //sb.AppendLine($"{tabspace}{tabspace}if (!waitterm((mulv) 25)) uber_puts(\"Werkadres: {controller.Data.Naam}\n\n\");");
            //sb.AppendLine($"{tabspace}{tabspace}EXTRADUMP = 0;");
            //sb.AppendLine($"{tabspace}{tabspace}break;");
            //sb.AppendLine($"{tabspace}default:");
            //sb.AppendLine($"{tabspace}{tabspace}break;");
            //sb.AppendLine($"{tabspace}" + "}");
            sb.AppendLine("");
            sb.AppendLine($"{ts}post_dump_application();");
	        if (controller.HalfstarData.IsHalfstar)
	        {
		        sb.AppendLine($"{ts}post_dump_application_halfstar();");
	        }
            sb.AppendLine("}");
            sb.AppendLine();

            return sb.ToString();
        }

        private string GenerateRegCSpecialSignals(ControllerModel controller)
        {
            var sb = new StringBuilder();

            sb.AppendLine("#ifdef CCOL_IS_SPECIAL");
            sb.AppendLine("void PrioSpecialSignals();");
            sb.AppendLine("void is_special_signals(void)");
            sb.AppendLine("{");

            CCOLCodeGenerationHelper.AddCodeTypeToStringBuilder(controller, OrderedPieceGenerators, sb, CCOLCodeTypeEnum.RegCSpecialSignals, true, true, false, true, ts);

            if (controller.PrioData.PrioIngrepen.Any() ||
				controller.PrioData.HDIngrepen.Any())
		    {
				sb.AppendLine($"{ts}PrioSpecialSignals();");
			}
            sb.AppendLine($"{ts}SpecialSignals_Add();");
            sb.AppendLine("}");
            sb.AppendLine("#endif");
            sb.AppendLine();

            return sb.ToString();
        }
    }
}
