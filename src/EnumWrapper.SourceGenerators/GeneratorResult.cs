using System;
using Microsoft.CodeAnalysis;

namespace EnumWrapper.SourceGenerators
{
    public sealed class GeneratorResult<T> : IEquatable<GeneratorResult<T>> where T : class, IEquatable<T>
    {
        public T? Value { get; }
        public DiagnosticInfo? Diagnostic { get; }

        public GeneratorResult(T value)
        {
            Value = value;
        }

        public GeneratorResult(DiagnosticInfo diagnostic)
        {
            Diagnostic = diagnostic;
        }

        public bool Equals(GeneratorResult<T>? other)
        {
            if (other is null) return false;
            if (ReferenceEquals(this, other)) return true;
            return EqualityComparer<T?>.Default.Equals(Value, other.Value) &&
                   EqualityComparer<DiagnosticInfo?>.Default.Equals(Diagnostic, other.Diagnostic);
        }

        public override bool Equals(object? obj) => Equals(obj as GeneratorResult<T>);
        public override int GetHashCode() => (Value, Diagnostic).GetHashCode();
    }

    public sealed class DiagnosticInfo : IEquatable<DiagnosticInfo>
    {
        public string Id { get; }
        public string Message { get; }
        public DiagnosticSeverity Severity { get; }
        public Location Location { get; }

        public DiagnosticInfo(string id, string message, DiagnosticSeverity severity, Location location)
        {
            Id = id;
            Message = message;
            Severity = severity;
            Location = location;
        }

        public bool Equals(DiagnosticInfo? other)
        {
            if (other is null) return false;
            return Id == other.Id &&
                   Message == other.Message &&
                   Severity == other.Severity &&
                   Location.Equals(other.Location);
        }

        public override bool Equals(object? obj) => Equals(obj as DiagnosticInfo);
        public override int GetHashCode() => (Id, Message, Severity, Location).GetHashCode();
    }
}
