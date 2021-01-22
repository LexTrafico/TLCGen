﻿using System;
using System.Collections.Generic;
using TLCGen.Models;

namespace TLCGen.Generators.Shared
{
    public interface ICCOLCodePieceGenerator
    {
        int ElementGenerationOrder { get; }
        void CollectCCOLElements(ControllerModel c, ICCOLGeneratorSettingsProvider settingsProvider = null);
        bool HasFunctionLocalVariables();
        IEnumerable<Tuple<string,string, string>> GetFunctionLocalVariables(ControllerModel c, CCOLCodeTypeEnum type);
        bool HasCCOLElements();
        IEnumerable<CCOLElement> GetCCOLElements(CCOLElementTypeEnum type);
        IEnumerable<CCOLElement> GetCCOLElements();
        bool HasCCOLBitmapOutputs();
        IEnumerable<CCOLIOElement> GetCCOLBitmapOutputs();
        bool HasCCOLBitmapInputs();
        IEnumerable<CCOLIOElement> GetCCOLBitmapInputs();
        bool HasSimulationElements(ControllerModel c);
        IEnumerable<DetectorSimulatieModel> GetSimulationElements(ControllerModel c);
        int HasCode(CCOLCodeTypeEnum type);
        bool HasCodeForController(ControllerModel c, CCOLCodeTypeEnum type);
        string GetCode(ControllerModel c, CCOLCodeTypeEnum type, string ts);
        bool HasSettings();
        bool SetSettings(CCOLGeneratorClassWithSettingsModel settings, ICCOLGeneratorSettingsProvider settingsProvider);
        List<string> GetSourcesToCopy();
    }
}
