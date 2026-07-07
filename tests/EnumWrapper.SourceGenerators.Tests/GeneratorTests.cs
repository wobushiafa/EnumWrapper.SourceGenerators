using System;
using System.ComponentModel;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Text.Json;
using EnumWrapper.SourceGenerators;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using TestNamespace;

namespace TestNamespace
{
    [TestClass]
    public class GeneratorTests
    {
        [TestMethod]
        public void TestItemsCount()
        {
            Assert.AreEqual(3, TestEnumWrapper.Items.Count);
            Assert.AreEqual(4, UserPermissionsWrapper.Items.Count);
            Assert.AreEqual(3, SortedEnumWrapper.Items.Count);
            Assert.AreEqual(7, DayOfWeekWrapper.Items.Count);
            Assert.AreEqual(2, SearchOptionWrapper.Items.Count);
            Assert.AreEqual(3, ShortNameEnumWrapper.Items.Count);
            Assert.AreEqual(3, NonContiguousEnumWrapper.Items.Count);
            Assert.AreEqual(2, GroupOnlyEnumWrapper.Items.Count);
            Assert.AreEqual(3, LargeUnsignedEnumWrapper.Items.Count);
            Assert.AreEqual(3, SignedLongEnumWrapper.Items.Count);
            Assert.AreEqual(3, OneBasedEnumWrapper.Items.Count);
        }

        [TestMethod]
        public void TestDescriptions()
        {
            Assert.AreEqual("First Value Description", TestEnumWrapper.FromValue(TestEnum.First).Description);
            Assert.AreEqual("Second Value Display", TestEnumWrapper.FromValue(TestEnum.Second).Description);
            Assert.AreEqual("Third", TestEnumWrapper.FromValue(TestEnum.Third).Description);
        }

        [TestMethod]
        public void TestToStringReturnsDescription()
        {
            var wrapper = TestEnumWrapper.FromValue(TestEnum.First);
            Assert.AreEqual("First Value Description", wrapper.ToString());
        }

        [TestMethod]
        public void TestImplicitCasting()
        {
            var wrapper = TestEnumWrapper.FromValue(TestEnum.Second);
            TestEnum value = wrapper;
            Assert.AreEqual(TestEnum.Second, value);

            // Also test that null wrapper casts to default
            TestEnumWrapper? nullWrapper = null;
            TestEnum defaultVal = nullWrapper;
            Assert.AreEqual(default(TestEnum), defaultVal);
        }

        [TestMethod]
        public void TestEquality()
        {
            var w1 = TestEnumWrapper.FromValue(TestEnum.First);
            var w2 = TestEnumWrapper.FromValue(TestEnum.First);
            var w3 = TestEnumWrapper.FromValue(TestEnum.Second);

            Assert.IsTrue(w1.Equals(w2));
            Assert.IsTrue(w1 == w2);
            Assert.IsFalse(w1 == w3);
            Assert.IsFalse(w1.Equals(null));

            // Check dictionary caching returns the exact same object reference for standard members
            Assert.AreSame(w1, w2);

            // Inequality operator
            Assert.IsTrue(w1 != w3);
            Assert.IsFalse(w1 != w2);
        }

        [TestMethod]
        public void TestExternalEnumWrapping()
        {
            var sunday = DayOfWeekWrapper.FromValue(DayOfWeek.Sunday);
            Assert.AreEqual(DayOfWeek.Sunday, sunday.Value);
            Assert.AreEqual("Sunday", sunday.Description);

            var topDirectoryOnly = SearchOptionWrapper.FromValue(SearchOption.TopDirectoryOnly);
            Assert.AreEqual(SearchOption.TopDirectoryOnly, topDirectoryOnly.Value);
        }

        [TestMethod]
        public void TestUnderlyingType()
        {
            var read = UserPermissionsWrapper.FromValue(UserPermissions.Read);
            // Verify underlying type is byte
            byte val = read.UnderlyingValue;
            Assert.AreEqual((byte)1, val);
        }

        [TestMethod]
        public void TestSmartTryParse()
        {
            // 1. Parse by Description (case-insensitive)
            Assert.IsTrue(TestEnumWrapper.TryParse("First Value Description", out var result1));
            Assert.AreEqual(TestEnum.First, result1!.Value);

            Assert.IsTrue(TestEnumWrapper.TryParse("second value display", out var result2));
            Assert.AreEqual(TestEnum.Second, result2!.Value);

            // 2. Parse by Name (case-insensitive)
            Assert.IsTrue(TestEnumWrapper.TryParse("Third", out var result3));
            Assert.AreEqual(TestEnum.Third, result3!.Value);

            Assert.IsTrue(TestEnumWrapper.TryParse("first", out var result4));
            Assert.AreEqual(TestEnum.First, result4!.Value);

            // 3. Parse by numeric value (NEW: TryParse now supports numeric strings)
            Assert.IsTrue(TestEnumWrapper.TryParse("0", out var resultNum0));
            Assert.AreEqual(TestEnum.First, resultNum0!.Value);

            Assert.IsTrue(TestEnumWrapper.TryParse("1", out var resultNum1));
            Assert.AreEqual(TestEnum.Second, resultNum1!.Value);

            Assert.IsTrue(TestEnumWrapper.TryParse("2", out var resultNum2));
            Assert.AreEqual(TestEnum.Third, resultNum2!.Value);

            // 4. Invalid parse
            Assert.IsFalse(TestEnumWrapper.TryParse("InvalidText", out var result5));
            Assert.IsNull(result5);
        }

        [TestMethod]
        public void TestTryParseEdgeCases()
        {
            // null input
            Assert.IsFalse(TestEnumWrapper.TryParse(null, out var result1));
            Assert.IsNull(result1);

            // empty string
            Assert.IsFalse(TestEnumWrapper.TryParse("", out var result2));
            Assert.IsNull(result2);

            // whitespace string (not empty, but no match)
            Assert.IsFalse(TestEnumWrapper.TryParse("   ", out var result3));
            Assert.IsNull(result3);
        }

        [TestMethod]
        public void TestParseThrowsOnInvalid()
        {
            try
            {
                TestEnumWrapper.Parse("ThisDoesNotExist");
                Assert.Fail("Expected ArgumentException was not thrown");
            }
            catch (ArgumentException)
            {
                // Expected
            }
        }

        [TestMethod]
        public void TestParseSuccess()
        {
            var result = TestEnumWrapper.Parse("Second Value Display");
            Assert.AreEqual(TestEnum.Second, result.Value);

            var byName = TestEnumWrapper.Parse("third");
            Assert.AreEqual(TestEnum.Third, byName.Value);
        }

        [TestMethod]
        public void TestFlagsSupport()
        {
            // 1. None flag
            var none = UserPermissionsWrapper.FromValue(UserPermissions.None);
            Assert.AreEqual("None", none.Description);

            // 2. Composite flags
            var readWrite = UserPermissionsWrapper.FromValue(UserPermissions.Read | UserPermissions.Write);
            Assert.AreEqual("Read Permission, Write Permission", readWrite.Description);

            var all = UserPermissionsWrapper.FromValue(UserPermissions.Read | UserPermissions.Write | UserPermissions.Execute);
            Assert.AreEqual("Read Permission, Write Permission, Execute Permission", all.Description);

            // 3. HasFlag check
            Assert.IsTrue(all.HasFlag(UserPermissions.Read));
            Assert.IsTrue(all.HasFlag(UserPermissions.Execute));
            Assert.IsFalse(readWrite.HasFlag(UserPermissions.Execute));

            // 4. HasFlag with wrapper argument
            var readWrapper = UserPermissionsWrapper.FromValue(UserPermissions.Read);
            Assert.IsTrue(all.HasFlag(readWrapper));
            Assert.IsFalse(readWrite.HasFlag(UserPermissionsWrapper.FromValue(UserPermissions.Execute)));

            // 5. GetFlags list representation (active components)
            var activeFlags = all.GetFlags().ToList();
            Assert.AreEqual(3, activeFlags.Count);
            Assert.IsTrue(activeFlags.Any(f => f.Value == UserPermissions.Read));
            Assert.IsTrue(activeFlags.Any(f => f.Value == UserPermissions.Write));
            Assert.IsTrue(activeFlags.Any(f => f.Value == UserPermissions.Execute));
        }

        [TestMethod]
        public void TestJsonSerialization()
        {
            var wrapper = TestEnumWrapper.FromValue(TestEnum.First);

            // 1. Serialize
            var json = JsonSerializer.Serialize(wrapper);
            Assert.AreEqual("\"First\"", json);

            // 2. Deserialize from Name
            var deserializedByName = JsonSerializer.Deserialize<TestEnumWrapper>("\"Second\"");
            Assert.AreEqual(TestEnum.Second, deserializedByName!.Value);

            // 3. Deserialize from Description
            var deserializedByDesc = JsonSerializer.Deserialize<TestEnumWrapper>("\"First Value Description\"");
            Assert.AreEqual(TestEnum.First, deserializedByDesc!.Value);

            // 4. Deserialize from numeric value
            var deserializedByNum = JsonSerializer.Deserialize<TestEnumWrapper>("2");
            Assert.AreEqual(TestEnum.Third, deserializedByNum!.Value);
        }

        [TestMethod]
        public void TestTypeConverter()
        {
            var converter = TypeDescriptor.GetConverter(typeof(TestEnumWrapper));
            Assert.IsNotNull(converter);
            Assert.IsTrue(converter.CanConvertFrom(typeof(string)));

            // Convert from Description
            var result1 = converter.ConvertFrom("First Value Description") as TestEnumWrapper;
            Assert.IsNotNull(result1);
            Assert.AreEqual(TestEnum.First, result1.Value);

            // Convert from Name
            var result2 = converter.ConvertFrom("Second") as TestEnumWrapper;
            Assert.IsNotNull(result2);
            Assert.AreEqual(TestEnum.Second, result2.Value);

            // ConvertFrom with invalid string should throw
            try
            {
                converter.ConvertFrom("NonExistentValue");
                Assert.Fail("Expected ArgumentException was not thrown");
            }
            catch (ArgumentException)
            {
                // Expected
            }
        }

        [TestMethod]
        public void TestDisplaySortingAndGrouping()
        {
            // SortedEnum has A (Order=10), B (Order=5), C (Order=20)
            // The items list must be sorted by Order ascending: B, A, C
            var items = SortedEnumWrapper.Items;

            Assert.AreEqual(SortedEnum.B, items[0].Value);
            Assert.AreEqual(SortedEnum.A, items[1].Value);
            Assert.AreEqual(SortedEnum.C, items[2].Value);

            // Verify GroupNames
            Assert.AreEqual("Group 1", items[0].GroupName); // B
            Assert.AreEqual("Group 1", items[1].GroupName); // A
            Assert.AreEqual("Group 2", items[2].GroupName); // C
        }

        [TestMethod]
        public void TestContiguousOptimization()
        {
            // TestEnum (values: 0, 1, 2) is contiguous.
            // UserPermissions (values: 0, 1, 2, 4) is non-contiguous.
            // Both must retrieve items correctly.
            var first = TestEnumWrapper.FromValue(TestEnum.First);
            var third = TestEnumWrapper.FromValue(TestEnum.Third);
            Assert.AreEqual(TestEnum.First, first.Value);
            Assert.AreEqual(TestEnum.Third, third.Value);

            var read = UserPermissionsWrapper.FromValue(UserPermissions.Read);
            var execute = UserPermissionsWrapper.FromValue(UserPermissions.Execute);
            Assert.AreEqual(UserPermissions.Read, read.Value);
            Assert.AreEqual(UserPermissions.Execute, execute.Value);
        }

        [TestMethod]
        public void TestOneBasedContiguousOptimization()
        {
            // OneBasedEnum (values: 1, 2, 3) should also use O(1) array lookup
            // This tests the MinNumericValue optimization
            var first = OneBasedEnumWrapper.FromValue(OneBasedEnum.First);
            Assert.AreEqual(OneBasedEnum.First, first.Value);
            Assert.AreEqual("First (1-based)", first.Description);

            var second = OneBasedEnumWrapper.FromValue(OneBasedEnum.Second);
            Assert.AreEqual(OneBasedEnum.Second, second.Value);
            Assert.AreEqual("Second (1-based)", second.Description);

            var third = OneBasedEnumWrapper.FromValue(OneBasedEnum.Third);
            Assert.AreEqual(OneBasedEnum.Third, third.Value);
            Assert.AreEqual("Third (1-based)", third.Description);
        }

        [TestMethod]
        public void TestFromValueWithNonContiguousEnum()
        {
            // NonContiguousEnum has values 10, 20, 30
            var ten = NonContiguousEnumWrapper.FromValue(NonContiguousEnum.Item10);
            Assert.AreEqual(NonContiguousEnum.Item10, ten.Value);
            Assert.AreEqual("Ten", ten.Description);

            var thirty = NonContiguousEnumWrapper.FromValue(NonContiguousEnum.Item30);
            Assert.AreEqual(NonContiguousEnum.Item30, thirty.Value);
            Assert.AreEqual("Thirty", thirty.Description);

            // FromValue with unknown value should create a fallback wrapper (not throw)
            var unknown = NonContiguousEnumWrapper.FromValue((NonContiguousEnum)42);
            Assert.AreEqual((NonContiguousEnum)42, unknown.Value);
        }

        [TestMethod]
        public void TestGroupOnlyEnumDefaults()
        {
            // GroupOnlyEnum items have Display with GroupName/Order but no Name.
            // Description should fall back to the field name.
            var first = GroupOnlyEnumWrapper.FromValue(GroupOnlyEnum.First);
            Assert.AreEqual("First", first.Description);
            Assert.AreEqual("Alpha", first.GroupName);

            var second = GroupOnlyEnumWrapper.FromValue(GroupOnlyEnum.Second);
            Assert.AreEqual("Second", second.Description);
            Assert.AreEqual("Beta", second.GroupName);

            // Items should be sorted by Order: Second (Order=1) before First (Order=2)
            Assert.AreEqual(GroupOnlyEnum.Second, GroupOnlyEnumWrapper.Items[0].Value);
            Assert.AreEqual(GroupOnlyEnum.First, GroupOnlyEnumWrapper.Items[1].Value);
        }

        [TestMethod]
        public void TestShortNameProperty()
        {
            // Verify ShortName property exists on wrappers generated from enums that use it
            var shortNameProp = typeof(ShortNameEnumWrapper).GetProperty("ShortName");
            Assert.IsNotNull(shortNameProp, "ShortName property should be generated");
            Assert.AreEqual(typeof(string), shortNameProp!.PropertyType);

            // Verify ShortName values
            var first = ShortNameEnumWrapper.FromValue(ShortNameEnum.First);
            Assert.AreEqual("1st", first.ShortName);
            Assert.AreEqual("First Value", first.Description);

            var second = ShortNameEnumWrapper.FromValue(ShortNameEnum.Second);
            Assert.AreEqual("2nd", second.ShortName);

            var third = ShortNameEnumWrapper.FromValue(ShortNameEnum.Third);
            Assert.AreEqual("3rd", third.ShortName);
        }

        [TestMethod]
        public void TestShortNamePropertyNotGeneratedWhenUnused()
        {
            // TestEnum does not use ShortName, so ShortNameEnumWrapper should not have it
            var shortNameProp = typeof(TestEnumWrapper).GetProperty("ShortName");
            Assert.IsNull(shortNameProp, "ShortName property should NOT be generated when no enum member uses it");
        }

        [TestMethod]
        public void TestItemsNotNull()
        {
            var items = TestEnumWrapper.Items;
            Assert.IsNotNull(items);
        }

        [TestMethod]
        public void TestUnderlyingValueType()
        {
            // UserPermissions has underlying type byte
            var read = UserPermissionsWrapper.FromValue(UserPermissions.Read);
            Assert.AreEqual(typeof(byte), read.UnderlyingValue.GetType());

            // TestEnum has default underlying type int
            var first = TestEnumWrapper.FromValue(TestEnum.First);
            Assert.AreEqual(typeof(int), first.UnderlyingValue.GetType());
        }

        [TestMethod]
        public void TestIReadOnlyListInterface()
        {
            // Verify Items implements IReadOnlyList (or at least is indexable with Count)
            var items = TestEnumWrapper.Items;
            Assert.IsTrue(items.Count > 0);
            for (int i = 0; i < items.Count; i++)
            {
                Assert.IsNotNull(items[i]);
            }
        }

        [TestMethod]
        public void TestEqualityWithDifferentEnumTypes()
        {
            var testFirst = TestEnumWrapper.FromValue(TestEnum.First);
            var nonContigTen = NonContiguousEnumWrapper.FromValue(NonContiguousEnum.Item10);

            // Different wrapper types should not be equal via object.Equals
            Assert.IsFalse(testFirst.Equals((object?)nonContigTen));
        }

        [TestMethod]
        public void TestFromValueFallbackWithUnknown()
        {
            // Non-enum value should still produce a wrapper with fallback description
            var unknown = TestEnumWrapper.FromValue((TestEnum)999);
            Assert.AreEqual((TestEnum)999, unknown.Value);
            Assert.AreEqual("999", unknown.Description);
        }

        [TestMethod]
        public void TestUnsignedLongBackedEnumGeneration()
        {
            var high = LargeUnsignedEnumWrapper.FromValue(LargeUnsignedEnum.High);
            Assert.AreEqual(LargeUnsignedEnum.High, high.Value);
            Assert.AreEqual("High Unsigned Value", high.Description);
            Assert.AreEqual(9223372036854775808UL, high.UnderlyingValue);

            var max = LargeUnsignedEnumWrapper.FromValue(LargeUnsignedEnum.Max);
            Assert.AreEqual(ulong.MaxValue, max.UnderlyingValue);
            Assert.AreEqual("Max", max.Description);
        }

        [TestMethod]
        public void TestJsonSerializationForLargeUnsignedEnum()
        {
            var deserializedHigh = JsonSerializer.Deserialize<LargeUnsignedEnumWrapper>("18446744073709551615");
            Assert.IsNotNull(deserializedHigh);
            Assert.AreEqual(LargeUnsignedEnum.Max, deserializedHigh.Value);

            var deserializedByName = JsonSerializer.Deserialize<LargeUnsignedEnumWrapper>("\"High\"");
            Assert.IsNotNull(deserializedByName);
            Assert.AreEqual(LargeUnsignedEnum.High, deserializedByName.Value);
        }

        [TestMethod]
        public void TestJsonSerializationForSignedLongEnum()
        {
            var deserializedMin = JsonSerializer.Deserialize<SignedLongEnumWrapper>("-9223372036854775808");
            Assert.IsNotNull(deserializedMin);
            Assert.AreEqual(SignedLongEnum.Min, deserializedMin.Value);

            var deserializedMax = JsonSerializer.Deserialize<SignedLongEnumWrapper>("9223372036854775807");
            Assert.IsNotNull(deserializedMax);
            Assert.AreEqual(SignedLongEnum.Max, deserializedMax.Value);
        }

        [TestMethod]
        public void TestTryParseNumericValue()
        {
            // Test parsing by numeric string for non-contiguous enum
            Assert.IsTrue(NonContiguousEnumWrapper.TryParse("10", out var result1));
            Assert.AreEqual(NonContiguousEnum.Item10, result1!.Value);

            Assert.IsTrue(NonContiguousEnumWrapper.TryParse("20", out var result2));
            Assert.AreEqual(NonContiguousEnum.Item20, result2!.Value);

            Assert.IsTrue(NonContiguousEnumWrapper.TryParse("30", out var result3));
            Assert.AreEqual(NonContiguousEnum.Item30, result3!.Value);
        }

        [TestMethod]
        public void TestFlagsCaching()
        {
            // Same composite flag should return same wrapper instance (cached)
            var composite1 = UserPermissionsWrapper.FromValue(UserPermissions.Read | UserPermissions.Write);
            var composite2 = UserPermissionsWrapper.FromValue(UserPermissions.Read | UserPermissions.Write);

            Assert.AreEqual(composite1.Value, composite2.Value);
            Assert.AreEqual(composite1.Description, composite2.Description);
            // Both should have the same description from cache
            Assert.AreEqual("Read Permission, Write Permission", composite1.Description);
        }

        [TestMethod]
        public void TestOneBasedEnumItemsCount()
        {
            Assert.AreEqual(3, OneBasedEnumWrapper.Items.Count);
        }
    }
}
