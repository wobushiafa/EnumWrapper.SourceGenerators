using EnumWrapper.SourceGenerators;

namespace TestNamespace
{
    /// <summary>
    /// This is the primary test enum used in generator assertions.
    /// </summary>
    [GenerateEnumWrapper]
    public enum TestEnum
    {
        /// <summary>
        /// The first value comment summary.
        /// </summary>
        [System.ComponentModel.Description("First Value Description")]
        First,

        /// <summary>
        /// The second value comment summary.
        /// </summary>
        [System.ComponentModel.DataAnnotations.Display(Name = "Second Value Display")]
        Second,

        /// <summary>
        /// The third value comment summary.
        /// </summary>
        Third // Fallback
    }

    [System.Flags]
    [GenerateEnumWrapper]
    public enum UserPermissions : byte
    {
        [System.ComponentModel.Description("None")]
        None = 0,

        [System.ComponentModel.Description("Read Permission")]
        Read = 1,

        [System.ComponentModel.Description("Write Permission")]
        Write = 2,

        [System.ComponentModel.Description("Execute Permission")]
        Execute = 4
    }

    [GenerateEnumWrapper]
    public enum SortedEnum
    {
        [System.ComponentModel.DataAnnotations.Display(Name = "Value A", Order = 10, GroupName = "Group 1")]
        A = 1,

        [System.ComponentModel.DataAnnotations.Display(Name = "Value B", Order = 5, GroupName = "Group 1")]
        B = 2,

        [System.ComponentModel.DataAnnotations.Display(Name = "Value C", Order = 20, GroupName = "Group 2")]
        C = 3
    }
}
