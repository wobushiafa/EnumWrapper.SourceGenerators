using System.ComponentModel;
using System.ComponentModel.DataAnnotations;

using EnumWrapper.SourceGenerators;

namespace EnumWrapper.SourceGenerators.Sample
{
    [GenerateEnumWrapper]
    public enum OrderStatus
    {
        [Description("Pending Payment")]
        Pending,

        [Description("Shipped to Customer")]
        Shipped,

        [Display(Name = "Delivered Successfully")]
        Delivered,

        // No description attribute, falls back to name automatically
        Cancelled
    }
}
