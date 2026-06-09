using System;
using System.IO;
using EnumWrapper.SourceGenerators;

// Regulates the compilation-level wrappers for the Unit Test assembly in a formal configuration location
[assembly: GenerateEnumWrapperFor(typeof(DayOfWeek), WrapperClassName = "DayOfWeekWrapper")]
[assembly: GenerateEnumWrapperFor(typeof(SearchOption), WrapperClassName = "SearchOptionWrapper")]
