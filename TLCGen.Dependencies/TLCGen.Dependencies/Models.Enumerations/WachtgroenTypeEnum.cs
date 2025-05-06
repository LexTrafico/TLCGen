using System.ComponentModel;
using TLCGen.Helpers;

namespace TLCGen.Models.Enumerations
{
    [TypeConverter(typeof(EnumDescriptionTypeConverter))]
    public enum WachtgroenTypeEnum
    {
        [Description("Geen")]
        GroenVasthouden,
        [Description("WSG zonder aanvraag")]
        GroenVasthouden,
        [Description("WSG met aanvraag")]
        GroenVasthoudenEnAanvragen,
    }
}
