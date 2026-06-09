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

    /// <summary>
    /// Test enum that uses Display with ShortName attribute.
    /// </summary>
    [GenerateEnumWrapper]
    public enum ShortNameEnum
    {
        [System.ComponentModel.DataAnnotations.Display(Name = "First Value", ShortName = "1st")]
        First,

        [System.ComponentModel.DataAnnotations.Display(Name = "Second Value", ShortName = "2nd")]
        Second,

        [System.ComponentModel.DataAnnotations.Display(Name = "Third Value", ShortName = "3rd")]
        Third
    }

    // Non-contiguous enum (values 10, 20, 30) to test dictionary fallback
    [GenerateEnumWrapper]
    public enum NonContiguousEnum
    {
        [System.ComponentModel.Description("Ten")]
        Item10 = 10,
        [System.ComponentModel.Description("Twenty")]
        Item20 = 20,
        [System.ComponentModel.Description("Thirty")]
        Item30 = 30
    }

    // Enum with Display attribute but only GroupName/Order, no explicit Name
    [GenerateEnumWrapper]
    public enum GroupOnlyEnum
    {
        [System.ComponentModel.DataAnnotations.Display(GroupName = "Alpha", Order = 2)]
        First,

        [System.ComponentModel.DataAnnotations.Display(GroupName = "Beta", Order = 1)]
        Second
    }
}
