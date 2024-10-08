﻿using System.ComponentModel;
using TLCGen.Helpers;

namespace TLCGen.Models.Enumerations
{
    [TypeConverter(typeof(EnumDescriptionTypeConverter))]
    public enum CCOLVersieEnum
    {
        [Description("8.0")]
        CCOL8,
        [Description("9.0")]
        CCOL9,
        [Description("9.5")]
        CCOL95,
        [Description("10.0")]
        CCOL100,
        [Description("11.0")]
        CCOL110,
        [Description("12.0")]
        CCOL120,
        [Description("12.1")]
        CCOL121
    }
}
