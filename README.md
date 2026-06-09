# EnumWrapper.SourceGenerators

Bilingual Documentation: [English](#english) | [中文说明](#chinese-documentation)

---

<a name="english"></a>
## English

`EnumWrapper.SourceGenerators` is a high-performance, zero-overhead C# Source Generator library that automatically wraps enums into strongly-typed model classes at compile time. It is designed to bridge the gap between backend enum logic and UI bindings (WPF, Avalonia, MAUI, WinForms) by resolving `[Description]` and `[Display]` attributes into a bindable structure, completely eliminating the need for runtime reflection.

### Key Features

1. **Smart Parsing**: `TryParse()` matches string inputs against descriptions, member names (case-insensitive), or underlying numeric values.
2. **Conditional WPF Support**: Automatically generates a WPF `IValueConverter` singleton if WPF assembly references are detected.
3. **TypeConverter Integration**: Native `TypeConverter` allows XAML parsers to assign strings directly to wrapper properties (e.g. `Status="Pending Payment"`).
4. **DisplayAttribute Sorting & Grouping**: Compile-time sorting via `Order` property and native categorization via `GroupName`.
5. **O(1) Array Lookup**: Detects sequential, 0-started contiguous enums and optimizes the lookup dictionary into direct array indexing.
6. **Bilingual XML Documentation Copying**: Automatically copies original `/// <summary>` comments to the generated classes and properties.
7. **Zero-Dependency JSON Serialization**: Injects a custom converter for `System.Text.Json` if the JSON library is referenced.
8. **Flags Enum Bitwise Support**: Multi-flags evaluation supporting `HasFlag()`, `GetFlags()`, and comma-separated Descriptions.
9. **Compiler Diagnostics**: Emits compile-time errors (`EWG001`, `EWG002`) if generator attributes are misused.

---

### Installation

Add the analyzer reference to your `.csproj` file:

```xml
<ItemGroup>
  <ProjectReference Include="path/to/EnumWrapper.SourceGenerators.csproj" 
                    OutputItemType="Analyzer" 
                    ReferenceOutputAssembly="false" />
</ItemGroup>
```

---

### Detailed Usage Guide

#### 1. Local Enum Wrapping

Annotate your enum with the `[GenerateEnumWrapper]` attribute. Write documentation comments to take advantage of XML comment copying:

```csharp
using System.ComponentModel;
using System.ComponentModel.DataAnnotations;
using EnumWrapper.SourceGenerators;

namespace MyApp
{
    /// <summary>
    /// Represents the current shipping status of an order.
    /// </summary>
    [GenerateEnumWrapper]
    public enum OrderStatus
    {
        /// <summary>Waiting for user payment.</summary>
        [Description("Pending Payment")]
        Pending,
        
        /// <summary>Item shipped out from the warehouse.</summary>
        [Description("Shipped to Customer")]
        Shipped,
        
        /// <summary>Package signed and delivered.</summary>
        [Display(Name = "Delivered Successfully", Order = 1, GroupName = "Finished")]
        Delivered,
        
        /// <summary>Order was cancelled.</summary>
        Cancelled
    }
}
```

This generates `OrderStatusWrapper`.

##### UI Binding (ComboBox / ListBox)
Bind directly to `Items`. Since `ToString()` returns `Description`, **no `DisplayMemberPath` is needed**:

```xml
<ComboBox ItemsSource="{x:Static local:OrderStatusWrapper.Items}"
          SelectedValue="{Binding SelectedStatus, Mode=TwoWay}"
          SelectedValuePath="Value" />
```

##### TypeConverter (XAML Direct String Assignment)
Assign string values directly to properties of type `OrderStatusWrapper` in XAML:

```xml
<local:MyCustomControl SelectedStatus="Shipped to Customer" />
```

##### Smart Parsing in Code
```csharp
// Parse by Description
if (OrderStatusWrapper.TryParse("Shipped to Customer", out var w1))
{
    OrderStatus status = w1; // Implicitly cast back to OrderStatus.Shipped
}

// Parse by Name (case-insensitive)
if (OrderStatusWrapper.TryParse("pending", out var w2))
{
    Console.WriteLine(w2.Description); // "Pending Payment"
}
```

---

#### 2. BCL / Third-Party External Enums Wrapping

If you cannot modify the enum (e.g. system enums), declare them in a formal configuration location like `Properties/AssemblyInfo.cs`:

```csharp
using System.IO;
using EnumWrapper.SourceGenerators;

[assembly: GenerateEnumWrapperFor(typeof(FileMode), WrapperClassName = "FileModeWrapper", CustomNamespace = "MyApp")]
```

Now `FileModeWrapper` is available in `MyApp` namespace.

---

#### 3. Flags Enums (Bitwise Bitmask Support)

For flags enums, the wrapper computes combined descriptions dynamically and exposes bitwise operations:

```csharp
[System.Flags]
[GenerateEnumWrapper]
public enum UserPermissions : byte
{
    [Description("Read Permission")]
    Read = 1,
    [Description("Write Permission")]
    Write = 2,
    [Description("Execute Permission")]
    Execute = 4
}
```

##### C# Bitwise Helpers
```csharp
var readWrite = UserPermissionsWrapper.FromValue(UserPermissions.Read | UserPermissions.Write);

// 1. Dynamic combined description join
Console.WriteLine(readWrite.Description); // "Read Permission, Write Permission"

// 2. HasFlag validation
bool canExecute = readWrite.HasFlag(UserPermissions.Execute); // false

// 3. GetFlags flattening (ideal for CheckBox lists)
List<UserPermissionsWrapper> activeFlags = readWrite.GetFlags().ToList();
// Contains wrappers for Read and Write
```

---

#### 4. WPF Conditional IValueConverter

If WPF is referenced, the generator creates a companion value converter singleton. Use it in XAML to convert raw enums to wrappers:

```xml
<TextBlock Text="{Binding Status, Converter={x:Static local:OrderStatusToWrapperConverter.Instance}}" />
```

---

#### 5. JSON Serialization

If `System.Text.Json` is referenced, serialization is supported out of the box:

```csharp
var wrapper = OrderStatusWrapper.FromValue(OrderStatus.Shipped);

// Serializes directly to its C# string name
string json = JsonSerializer.Serialize(wrapper); // "\"Shipped\""

// Deserializes from Name, Description, or numeric value
var w1 = JsonSerializer.Deserialize<OrderStatusWrapper>("\"Shipped to Customer\"");
var w2 = JsonSerializer.Deserialize<OrderStatusWrapper>("1"); // OrderStatus.Shipped
```

---
---

<a name="chinese-documentation"></a>
## 中文说明

`EnumWrapper.SourceGenerators` 是一个高性能、零运行时开销�?C# 增量源生成器（Source Generator）库，在编译期自动将任意枚举包装为强类型的模型类。它旨在消除反射，简化枚举逻辑�?UI 框架（WPF、Avalonia、MAUI、WinForms）之间的绑定，自动解�?`[Description]` �?`[Display]` 特性�?
### 核心特�?
1. **智能解析**：`TryParse()` 自动匹配描述文本、枚举名称（不区分大小写）或底层数值�?2. **条件 WPF 转换�?*：检测到项目引用 WPF 时，自动生成方便 XAML 直接使用�?`IValueConverter` 单例�?3. **XAML 属性直�?*：生�?`TypeConverter`，允许在 XAML 中直接为包装类属性赋予字符串字面量（�?`Status="Pending Payment"`）�?4. **编译期排序与分组**：解�?`DisplayAttribute` �?`Order` 进行编译期排序，提供 `GroupName` 属性用�?UI 分组�?5. **O(1) 数组寻址**：对于从 0 开始连续递增的枚举，自动省去 Dictionary 查表，优化为 O(1) 数组索引定位�?6. **文档注释完美拷贝**：自动将您在枚举上编写的 `/// <summary>` XML 注释拷贝至生成的类与属性上，保留完美的 IDE 悬停提示�?7. **零依�?JSON 序列�?*：条件装配对 `System.Text.Json` 的支持，让包装类�?Web API 传输中无缝反序列化（支持从数值、名字、描述还原）�?8. **标志位域（Flags）支�?*：支�?`HasFlag()`、`GetFlags()`，且多选组合值（�?`Read | Write`）会自动用逗号拼接 Description�?9. **编译期诊断报�?*：在开发人员误用特性（例如将特性贴�?class 上）时，直接�?IDE 中报红线编译错误（`EWG001`, `EWG002`）�?
---

### 安装

将源生成器作为分析器引用至您�?`.csproj` 项目文件中：

```xml
<ItemGroup>
  <ProjectReference Include="path/to/EnumWrapper.SourceGenerators.csproj" 
                    OutputItemType="Analyzer" 
                    ReferenceOutputAssembly="false" />
</ItemGroup>
```

---

### 具体使用指南

#### 1. 包装本地枚举

只需在您的枚举上标注 `[GenerateEnumWrapper]` 特性。建议编�?XML 文档注释，以便生成器将其自动拷贝�?
```csharp
using System.ComponentModel;
using System.ComponentModel.DataAnnotations;
using EnumWrapper.SourceGenerators;

namespace MyApp
{
    /// <summary>
    /// 订单发货状�?    /// </summary>
    [GenerateEnumWrapper]
    public enum OrderStatus
    {
        /// <summary>等待用户付款</summary>
        [Description("等待付款")]
        Pending,
        
        /// <summary>商品已出库发�?/summary>
        [Description("已向客户发货")]
        Shipped,
        
        /// <summary>客户已签�?/summary>
        [Display(Name = "送达成功", Order = 1, GroupName = "已完�?)]
        Delivered,
        
        /// <summary>订单已取�?/summary>
        Cancelled
    }
}
```

这将自动为您在同命名空间下生�?`OrderStatusWrapper` 包装类�?
##### 界面绑定 (WPF / Avalonia XAML)
直接绑定至静�?`Items` 属性。由�?`ToString()` 被重写为返回 Description�?*不需要写 `DisplayMemberPath`**�?
```xml
<ComboBox ItemsSource="{x:Static local:OrderStatusWrapper.Items}"
          SelectedValue="{Binding SelectedStatus, Mode=TwoWay}"
          SelectedValuePath="Value" />
```

##### XAML 字符串直�?(TypeConverter)
�?XAML 中，您可以将字符串字面量直接赋给类型�?`OrderStatusWrapper` 的控件属性：

```xml
<local:MyCustomControl SelectedStatus="已向客户发货" />
```

##### C# 智能类型转换与解�?```csharp
// 1. 根据描述文本解析
if (OrderStatusWrapper.TryParse("已向客户发货", out var w1))
{
    OrderStatus status = w1; // 隐式转换回原生枚举�?OrderStatus.Shipped
}

// 2. 根据名字解析（不区分大小写）
if (OrderStatusWrapper.TryParse("pending", out var w2))
{
    Console.WriteLine(w2.Description); // 输出 "等待付款"
}
```

---

#### 2. 包装系统或第三方外部枚举

若无法直接在枚举源码上加特性（如系统的 `System.IO.FileMode`），建议在专门的配置文件（如 `Properties/AssemblyInfo.cs`）中统一定义�?
```csharp
using System.IO;
using EnumWrapper.SourceGenerators;

[assembly: GenerateEnumWrapperFor(typeof(FileMode), WrapperClassName = "FileModeWrapper", CustomNamespace = "MyApp")]
```

这将�?`MyApp` 命名空间下生�?`FileModeWrapper`�?
---

#### 3. 位域标志枚举（Flags Bitmask�?
针对标志枚举，生成的包装类支持动态描述拼接和位运算辅助方法：

```csharp
[System.Flags]
[GenerateEnumWrapper]
public enum UserPermissions : byte
{
    [Description("读取权限")]
    Read = 1,
    [Description("写入权限")]
    Write = 2,
    [Description("执行权限")]
    Execute = 4
}
```

##### C# 位运算使�?```csharp
var readWrite = UserPermissionsWrapper.FromValue(UserPermissions.Read | UserPermissions.Write);

// 1. 动态逗号拼接描述
Console.WriteLine(readWrite.Description); // 输出 "读取权限, 写入权限"

// 2. HasFlag 校验
bool canExecute = readWrite.HasFlag(UserPermissions.Execute); // false

// 3. GetFlags 平铺拆分（适合多�?CheckBox 列表�?List<UserPermissionsWrapper> activeFlags = readWrite.GetFlags().ToList();
// 包含 "读取权限" �?"写入权限" 两个单独包装实例
```

---

#### 4. WPF 专用�?IValueConverter 绑定

在引用了 WPF 基础库的项目中，可以直接使用生成器自动附带生成的 WPF 值转换器单例�?
```xml
<TextBlock Text="{Binding Status, Converter={x:Static local:OrderStatusToWrapperConverter.Instance}}" />
```

---

#### 5. JSON 序列化支�?
如果项目引用�?`System.Text.Json`，无需任何配置，包装类就能无缝传输�?
```csharp
var wrapper = OrderStatusWrapper.FromValue(OrderStatus.Shipped);

// 序列化：直接输出为枚举字符串名称
string json = JsonSerializer.Serialize(wrapper); // "\"Shipped\""

// 反序列化：支持从名字、描述、或底层数字直接还原为包装实�?var w1 = JsonSerializer.Deserialize<OrderStatusWrapper>("\"已向客户发货\"");
var w2 = JsonSerializer.Deserialize<OrderStatusWrapper>("1"); // 还原�?OrderStatus.Shipped 实体
```

---

### License

Licensed under the [MIT License](LICENSE).

