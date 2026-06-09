using System;
using System.ComponentModel;
using System.ComponentModel.DataAnnotations;
using System.IO;
using System.Linq;
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

            // 3. Invalid parse
            Assert.IsFalse(TestEnumWrapper.TryParse("InvalidText", out var result5));
            Assert.IsNull(result5);
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

            // 4. GetFlags list representation (active components)
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
    }
}
