using System;
using System.Collections.Immutable;
using System.Linq;
using Microsoft.CodeAnalysis;

namespace EnumWrapper.SourceGenerators
{
    public sealed class EnumToWrapInfo : IEquatable<EnumToWrapInfo>
    {
        public string Namespace { get; }
        public string EnumName { get; }
        public string EnumFullName { get; }
        public string WrapperClassName { get; }
        public string WrapperNamespace { get; }
        public ImmutableArray<EnumMemberInfo> Members { get; }
        public string UnderlyingTypeName { get; }
        public bool IsUnsignedUnderlyingType { get; }
        public bool IsFlags { get; }
        public bool HasJson { get; }
        public bool IsContiguous { get; }
        public decimal MinNumericValue { get; }
        public string XmlDocumentation { get; }
        public bool HasWpf { get; }
        public Location DeclarationLocation { get; }

        public EnumToWrapInfo(
            string @namespace,
            string enumName,
            string enumFullName,
            string wrapperClassName,
            string wrapperNamespace,
            ImmutableArray<EnumMemberInfo> members,
            string underlyingTypeName,
            bool isUnsignedUnderlyingType,
            bool isFlags,
            bool hasJson,
            bool isContiguous,
            decimal minNumericValue,
            string xmlDocumentation,
            bool hasWpf,
            Location declarationLocation)
        {
            Namespace = @namespace;
            EnumName = enumName;
            EnumFullName = enumFullName;
            WrapperClassName = wrapperClassName;
            WrapperNamespace = wrapperNamespace;
            Members = members;
            UnderlyingTypeName = underlyingTypeName;
            IsUnsignedUnderlyingType = isUnsignedUnderlyingType;
            IsFlags = isFlags;
            HasJson = hasJson;
            IsContiguous = isContiguous;
            MinNumericValue = minNumericValue;
            XmlDocumentation = xmlDocumentation;
            HasWpf = hasWpf;
            DeclarationLocation = declarationLocation;
        }

        public bool Equals(EnumToWrapInfo? other)
        {
            if (other is null) return false;
            if (ReferenceEquals(this, other)) return true;
            return Namespace == other.Namespace &&
                   EnumName == other.EnumName &&
                   EnumFullName == other.EnumFullName &&
                   WrapperClassName == other.WrapperClassName &&
                   WrapperNamespace == other.WrapperNamespace &&
                   UnderlyingTypeName == other.UnderlyingTypeName &&
                   IsUnsignedUnderlyingType == other.IsUnsignedUnderlyingType &&
                   IsFlags == other.IsFlags &&
                   HasJson == other.HasJson &&
                   IsContiguous == other.IsContiguous &&
                   MinNumericValue == other.MinNumericValue &&
                   XmlDocumentation == other.XmlDocumentation &&
                   HasWpf == other.HasWpf &&
                   Members.SequenceEqual(other.Members);
        }

        public override bool Equals(object? obj) => Equals(obj as EnumToWrapInfo);

        public override int GetHashCode()
        {
            unchecked
            {
                int hash = 17;
                hash = hash * 23 + Namespace.GetHashCode();
                hash = hash * 23 + EnumName.GetHashCode();
                hash = hash * 23 + EnumFullName.GetHashCode();
                hash = hash * 23 + WrapperClassName.GetHashCode();
                hash = hash * 23 + WrapperNamespace.GetHashCode();
                hash = hash * 23 + UnderlyingTypeName.GetHashCode();
                hash = hash * 23 + IsUnsignedUnderlyingType.GetHashCode();
                hash = hash * 23 + IsFlags.GetHashCode();
                hash = hash * 23 + HasJson.GetHashCode();
                hash = hash * 23 + IsContiguous.GetHashCode();
                hash = hash * 23 + MinNumericValue.GetHashCode();
                hash = hash * 23 + XmlDocumentation.GetHashCode();
                hash = hash * 23 + HasWpf.GetHashCode();
                foreach (var member in Members)
                {
                    hash = hash * 23 + member.GetHashCode();
                }
                return hash;
            }
        }
    }

    public sealed class EnumMemberInfo : IEquatable<EnumMemberInfo>
    {
        public string Name { get; }
        public string Description { get; }
        public string? ShortName { get; }
        public int? Order { get; }
        public string? GroupName { get; }
        public decimal NumericValue { get; }
        public string XmlDocumentation { get; }

        public EnumMemberInfo(string name, string description, string? shortName, int? order, string? groupName, decimal numericValue, string xmlDocumentation)
        {
            Name = name;
            Description = description;
            ShortName = shortName;
            Order = order;
            GroupName = groupName;
            NumericValue = numericValue;
            XmlDocumentation = xmlDocumentation;
        }

        public bool Equals(EnumMemberInfo? other)
        {
            if (other is null) return false;
            if (ReferenceEquals(this, other)) return true;
            return Name == other.Name &&
                   Description == other.Description &&
                   ShortName == other.ShortName &&
                   Order == other.Order &&
                   GroupName == other.GroupName &&
                   NumericValue == other.NumericValue &&
                   XmlDocumentation == other.XmlDocumentation;
        }

        public override bool Equals(object? obj) => Equals(obj as EnumMemberInfo);
        public override int GetHashCode() => (Name, Description, ShortName, Order, GroupName, NumericValue, XmlDocumentation).GetHashCode();
    }
}
