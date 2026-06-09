using System.IO;
using EnumWrapper.SourceGenerators;

// Regulates the compilation-level wrappers in a formal configuration location
[assembly: GenerateEnumWrapperFor(typeof(FileMode), WrapperClassName = "FileModeWrapper", CustomNamespace = "EnumWrapper.SourceGenerators.Sample")]
