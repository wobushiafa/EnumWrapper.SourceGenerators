# EnumWrapper.SourceGenerators

Bilingual Documentation: [English](#english) | [中文说明](#chinese-documentation)

---

<a name="english"></a>
## English

`EnumWrapper.SourceGenerators` is a high-performance, zero-overhead C# incremental source generator that automatically wraps enums into strongly-typed model classes at compile time. It bridges the gap between backend enum logic and UI bindings (WPF, Avalonia, MAUI, WinForms) by resolving `[Description]` and `[Display]` attributes into a bindable structure, completely eliminating the need for runtime reflection.

### Key Features

1. **Smart Parsing**: `TryParse()` matches string inputs against descriptions, member names (case-insensitive), or underlying numeric values.
2. **Conditional WPF Support**: Automatically generates a WPF `IValueConverter` singleton if WPF assembly references are detected.
3. **TypeConverter Integration**: Native `TypeConverter` allows XAML parsers to assign strings directly to wrapper properties (e.g. `Status="Pending Payment"`). The generated converter also carries a `[DisplayName]` attribute for friendly designer display.
4. **DisplayAttribute Sorting & Grouping**: Compile-time sorting via `Order` property and native categorization via `GroupName`.
5. **O(1) Array Lookup**: Detects contiguous enums and optimizes the lookup into direct array indexing. Supports enums starting from any value (e.g. 1, 2, 3). A separate value-ordered lookup array ensures correct retrieval even when `DisplayAttribute.Order` differs from numeric ordering.
6. **Bilingual XML Documentation Copying**: Automatically copies original `/// <summary>` comments to the generated classes and properties.
7. **Zero-Dependency JSON Serialization**: Injects a custom converter for `System.Text.Json` if the JSON library is referenced.
8. **Flags Enum Bitwise Support**: Multi-flags evaluation supporting `HasFlag()`, `GetFlags()`, and comma-separated Descriptions.
9. **Compiler Diagnostics**: Emits compile-time errors (`EWG001`–`EWG004`) for invalid attribute usage and configuration conflicts.
10. **Display ShortName Support**: Extracts the `ShortName` property from `[Display(ShortName = ...)]` and generates a corresponding `ShortName` property on the wrapper class when any member uses it.

---

### Installation

Install the package via NuGet:

```bash
dotnet add package EnumWrapper.SourceGenerators
```

Or reference it directly in your `.csproj` file as a development dependency:

```xml
<ItemGroup>
  <PackageReference Include="EnumWrapper.SourceGenerators" Version="1.2.0" PrivateAssets="all" />
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

This generates `OrderStatusWrapper` in the same namespace.

> [!TIP]
> **Optional: Customizing Class Name & Namespace**
> By default, the wrapper class is automatically generated in the same namespace as the enum, using the name `{EnumName}Wrapper`. You only need to specify them if you want to override the defaults:
> ```csharp
> [GenerateEnumWrapper(WrapperClassName = "MyStatusWrapper", CustomNamespace = "MyCustomNamespace.Domain")]
> public enum OrderStatus
> {
>     // ...
> }
> ```

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

##### Generated Class API Structure

The generated class is a `partial class` implementing `IEquatable<T>`, allowing you to easily extend it in your own partial class files. It exposes the following APIs:

- **Instance Properties**:
  - `Value`: The original enum type value (e.g. `OrderStatus`).
  - `Description`: The human-readable string (resolved from `[Description]`, `[Display(Name = ...)]`, or fallbacks to the field name).
  - `ShortName`: The compact label from `[Display(ShortName = ...)]` (only generated when any member uses it; returns `null` otherwise).
  - `UnderlyingValue`: The numeric representation cast to the enum's underlying type (e.g. `byte`, `int`, `long`).
  - `GroupName`: The group category name specified in `[Display(GroupName = ...)]` (returns `null` if not specified).
- **Static Properties**:
  - `Items`: An `IReadOnlyList<T>` containing all sorted wrapper instances (useful for binding to UI controls).
- **Static Methods**:
  - `FromValue(TEnum)`: O(1) array lookup or cached dictionary mapping to resolve a wrapper instance from the enum value.
  - `TryParse(string, out TWrapper)` / `Parse(string)`: Smart text parsers matching against description, name, or numeric value.
- **Operators & Conversions**:
  - Overrides `ToString()` to return the `Description` property.
  - Supports **implicit casting** from the wrapper class back to the original enum type (`TEnum`).
  - Implements `==` and `!=` operators.

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

### Compiler Diagnostics

| ID | Description |
|---|---|
| `EWG001` | `[GenerateEnumWrapper]` attribute applied to a non-enum type. |
| `EWG002` | `[GenerateEnumWrapperFor]` targets a non-enum type. |
| `EWG003` | The same enum is configured to generate the same wrapper multiple times. |
| `EWG004` | Two different enums are configured to generate wrappers with the same fully qualified name. |

---

---

<a name="chinese-documentation"></a>
## 中文说明

`EnumWrapper.SourceGenerators` 是一个高性能、零运行时开销的 C# 增量源生成器（Source Generator）库，在编译期自动将任意枚举包装为强类型的模型类。它旨在消除反射，简化枚举逻辑与 UI 框架（WPF、Avalonia、MAUI、WinForms）之间的绑定，自动解析 `[Description]` 和 `[Display]` 特性。

### 核心特性

1. **智能解析**：`TryParse()` 自动匹配描述文本、枚举名称（不区分大小写）或底层数值。
2. **条件 WPF 转换器**：检测到项目引用 WPF 时，自动生成方便 XAML 直接使用的 `IValueConverter` 单例。
3. **XAML 属性直赋**：生成 `TypeConverter`，允许在 XAML 中直接为包装类属性赋予字符串字面量（如 `Status="Pending Payment"`）。生成的转换器还带有 `[DisplayName]` 特性便于设计器友好显示。
4. **编译期排序与分组**：解析 `DisplayAttribute` 的 `Order` 进行编译期排序，提供 `GroupName` 属性用于 UI 分组。
5. **O(1) 数组寻址**：对于连续递增的枚举，自动省去 Dictionary 查表，优化为 O(1) 数组索引定位。支持从任意值开始的连续枚举（如 1, 2, 3）。即使 `DisplayAttribute.Order` 排序与数值顺序不一致也能正确映射。
6. **文档注释完美拷贝**：自动将您在枚举上编写的 `/// <summary>` XML 注释拷贝至生成的类与属性上，保留完美的 IDE 悬停提示。
7. **零依赖 JSON 序列化**：条件装配对 `System.Text.Json` 的支持，让包装类在 Web API 传输中无缝反序列化（支持从数值、名字、描述还原）。
8. **标志位域（Flags）支持**：支持 `HasFlag()`、`GetFlags()`，且多选组合值（如 `Read | Write`）会自动用逗号拼接 Description。
9. **编译期诊断报错**：在开发人员误用特性时，直接在 IDE 中报红线编译错误（`EWG001`–`EWG004`）。
10. **ShortName 简标支持**：提取 `[Display(ShortName = ...)]` 属性，在有成员使用时自动在包装类上生成 `ShortName` 属性，为空间有限的 UI 场景提供简洁标签。

---

### 安装

使用 NuGet 命令行安装包：

```bash
dotnet add package EnumWrapper.SourceGenerators
```

或者直接在您的 `.csproj` 项目文件中添加包引用（设为开发依赖项）：

```xml
<ItemGroup>
  <PackageReference Include="EnumWrapper.SourceGenerators" Version="1.2.0" PrivateAssets="all" />
</ItemGroup>
```

---

### 具体使用指南

#### 1. 包装本地枚举

只需在您的枚举上标注 `[GenerateEnumWrapper]` 特性。建议编写 XML 文档注释，以便生成器将其自动拷贝：

```csharp
using System.ComponentModel;
using System.ComponentModel.DataAnnotations;
using EnumWrapper.SourceGenerators;

namespace MyApp
{
    /// <summary>
    /// 订单发货状态
    /// </summary>
    [GenerateEnumWrapper]
    public enum OrderStatus
    {
        /// <summary>等待用户付款</summary>
        [Description("等待付款")]
        Pending,
        
        /// <summary>商品已出库发送</summary>
        [Description("已向客户发货")]
        Shipped,
        
        /// <summary>客户已签收</summary>
        [Display(Name = "送达成功", Order = 1, GroupName = "已完结")]
        Delivered,
        
        /// <summary>订单已取消</summary>
        Cancelled
    }
}
```

这将自动为您在同命名空间下生成 `OrderStatusWrapper` 包装类。

> [!TIP]
> **（可选）自定义生成的类名与命名空间**
> 这些配置是**完全可选的**。默认情况下，生成器会自动使用枚举所在的命名空间，并生成名为 `{EnumName}Wrapper` 的类。只有在您需要自定义生成的类名或命名空间时，才需要显式设置它们：
> ```csharp
> [GenerateEnumWrapper(WrapperClassName = "MyStatusWrapper", CustomNamespace = "MyCustomNamespace.Domain")]
> public enum OrderStatus
> {
>     // ...
> }
> ```

##### 界面绑定 (WPF / Avalonia XAML)

直接绑定至静态 `Items` 属性。由于 `ToString()` 被重写为返回 Description，**不需要写 `DisplayMemberPath`**：

```xml
<ComboBox ItemsSource="{x:Static local:OrderStatusWrapper.Items}"
          SelectedValue="{Binding SelectedStatus, Mode=TwoWay}"
          SelectedValuePath="Value" />
```

##### XAML 字符串直赋 (TypeConverter)

在 XAML 中，您可以将字符串字面量直接赋给类型为 `OrderStatusWrapper` 的控件属性：

```xml
<local:MyCustomControl SelectedStatus="已向客户发货" />
```

##### C# 智能类型转换与解析

```csharp
// 1. 根据描述文本解析
if (OrderStatusWrapper.TryParse("已向客户发货", out var w1))
{
    OrderStatus status = w1; // 隐式转换回原生枚举值 OrderStatus.Shipped
}

// 2. 根据名字解析（不区分大小写）
if (OrderStatusWrapper.TryParse("pending", out var w2))
{
    Console.WriteLine(w2.Description); // 输出 "等待付款"
}
```

##### 生成的包装类 API 结构

生成的类是一个实现了 `IEquatable<T>` 的 `partial class`，这使得您可以通过定义同名的 partial 类轻松扩展自定义成员。生成的类包含以下核心 API：

- **实例属性**：
  - `Value`：原始的枚举值类型（例如 `OrderStatus`）。
  - `Description`：人类可读的友好描述（依次从 `[Description]`、`[Display(Name = ...)]` 获取，默认回退为字段名称）。
  - `ShortName`：从 `[Display(ShortName = ...)]` 提取的紧凑标签（仅当有成员使用时生成；未使用时返回 `null`）。
  - `UnderlyingValue`：该枚举值对应的底层数值，类型与枚举的底层类型一致（例如 `byte`、`int`、`long`）。
  - `GroupName`：在 `[Display(GroupName = ...)]` 中指定的分组类别名称（未指定时返回 `null`）。
- **静态属性**：
  - `Items`：一个只读列表 `IReadOnlyList<T>`，包含所有排序后的包装类实例（非常适合直接绑定至 UI 控件）。
- **静态方法**：
  - `FromValue(TEnum)`：将原始枚举值转换为对应的包装类实例（支持 O(1) 数组寻址或缓存字典查表优化）。
  - `TryParse(string, out TWrapper)` / `Parse(string)`：智能文本解析方法，支持通过描述、名字或数字还原。
- **运算符与转换**：
  - 重写了 `ToString()` 方法，直接返回 `Description`。
  - 支持**隐式转换**，可直接将包装类实例隐式转换为原始枚举类型（`TEnum`）。
  - 实现了重载的 `==` 和 `!=` 比较运算符。

---

#### 2. 包装系统或第三方外部枚举

若无法直接在枚举源码上加特性（如系统的 `System.IO.FileMode`），建议在专门的配置文件（如 `Properties/AssemblyInfo.cs`）中统一定义：

```csharp
using System.IO;
using EnumWrapper.SourceGenerators;

[assembly: GenerateEnumWrapperFor(typeof(FileMode), WrapperClassName = "FileModeWrapper", CustomNamespace = "MyApp")]
```

这将在 `MyApp` 命名空间下生成 `FileModeWrapper`。

---

#### 3. 位域标志枚举（Flags Bitmask）

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

##### C# 位运算使用

```csharp
var readWrite = UserPermissionsWrapper.FromValue(UserPermissions.Read | UserPermissions.Write);

// 1. 动态逗号拼接描述
Console.WriteLine(readWrite.Description); // 输出 "读取权限, 写入权限"

// 2. HasFlag 校验
bool canExecute = readWrite.HasFlag(UserPermissions.Execute); // false

// 3. GetFlags 平铺拆分（适合多选 CheckBox 列表）
List<UserPermissionsWrapper> activeFlags = readWrite.GetFlags().ToList();
// 包含 "读取权限" 和 "写入权限" 两个单独包装实例
```

---

#### 4. WPF 专用的 IValueConverter 绑定

在引用了 WPF 基础库的项目中，可以直接使用生成器自动附带生成的 WPF 值转换器单例：

```xml
<TextBlock Text="{Binding Status, Converter={x:Static local:OrderStatusToWrapperConverter.Instance}}" />
```

---

#### 5. JSON 序列化支持

如果项目引用了 `System.Text.Json`，无需任何配置，包装类就能无缝传输：

```csharp
var wrapper = OrderStatusWrapper.FromValue(OrderStatus.Shipped);

// 序列化：直接输出为枚举字符串名称
string json = JsonSerializer.Serialize(wrapper); // "\"Shipped\""

// 反序列化：支持从名字、描述、或底层数字直接还原为包装实体
var w1 = JsonSerializer.Deserialize<OrderStatusWrapper>("\"已向客户发货\"");
var w2 = JsonSerializer.Deserialize<OrderStatusWrapper>("1"); // 还原为 OrderStatus.Shipped 实体
```

---

### 编译器诊断

| ID | 说明 |
|---|---|
| `EWG001` | `[GenerateEnumWrapper]` 特性被应用到了非枚举类型上。 |
| `EWG002` | `[GenerateEnumWrapperFor]` 的目标类型不是枚举。 |
| `EWG003` | 同一个枚举被多次配置生成同一个包装类。 |
| `EWG004` | 两个不同的枚举被配置生成相同全限定名的包装类。 |

---

### License

Licensed under the [MIT License](LICENSE).
