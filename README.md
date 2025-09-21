# Krins.Analyzer

用于解决 AOT 反射相关问题的源码生成器，需要搭配 `Krins.Analyzer.Attributes` 使用

## Background

[LINQ](https://learn.microsoft.com/zh-cn/dotnet/csharp/linq/) 是基于将查询功能直接集成到 C# 语言的一组技术的名称，搭配 [System.Linq.Dynamic.Core](https://github.com/zzzprojects/System.Linq.Dynamic.Core) 可以非常方便地解析字符串表达式，例如现有类 User

```csharp
public partial class User
{
    public uint Uid { get; set; }
    
    public string Name { get; set; } = string.Empty;
}
```

需要判断所有用户的 Uid 是否均大于 1000

```csharp
var ret = users.All(u => u.Uid > 1000);
```

实际应用中的判断条件可能非常复杂（比如有些使用者希望判断 Uid < 1000，有些希望判断 Name 是否以某个字符开头等等），代码中直接遍历各种可能性不太方便，因此可以考虑让使用者自行设计 LINQ 表达式。使用 `System.Linq.Dynamic.Core` 可以非常简便的实现

```csharp
var expression = "users.All(u => u.Uid < 1000 && u.Name.Length == 2)";

// 生成表达式树
var expressionParam = Expression.Parameter(typeof(User[]), "users");
// 第二个参数为表达式返回值类型
var lambdaExpression = DynamicExpressionParser.ParseLambda(new[] { expressionParam }, typeof(bool), expression);

// 编译表达式树为委托
var func = (Func<User[], bool>)lambdaExpression.Compile();

// 计算
var ret = func(users);
```

然而很遗憾的是，上述代码在 AOT 后无法正常运行，因此考虑替代方案 [NCalc](https://github.com/ncalc/ncalc) 和 [PanoramicData.NCalcExtensions](https://github.com/panoramicdata/PanoramicData.NCalcExtensions)

```csharp
var expression = new ExtendedExpression("Uid < 1000 && Name.Length == 2");
if (expression.HasErrors())
{
 	return;
}

// 添加参数
expression.Parameters.Add("Uid", typeof(uint));
expression.Parameters.Add("Name", typeof(string));

// 编译表达式
Func<User, bool> CustomizedFilter = u =>
{
	expression.Parameters["Uid"] = u.Uid;
    expression.Parameters["Name"] = u.Name;

    if (expression.Evaluate() is not bool ret)
    {
        return false;
    }
    return ret;
};

users.All(u => CustomizedFilter(u))
```

由于 `NCalc` 不支持通过 `.` 引用对象内的属性，因此只能逐个添加 User 内的属性，当属性比较多时费时费力

## AotReflection

将 `AotReflection` 属性附加至 User 后，生成如下代码如下

```csharp
public partial class User
{
    public static Dictionary<string, Type> PropertyTypes = new()
    {
        {nameof(Uid), typeof(uint)},
        {nameof(Name), typeof(string)},
    };

    public object GetValue(string name) => name switch
    {
        nameof(Uid) => Uid,
        nameof(Name) => Name,
        _ => throw new ArgumentException(nameof(name))
    }
}
```

因此上述代码可简化为

```csharp
var expression = new ExtendedExpression("Uid < 1000 && Name.Length == 2");
if (expression.HasErrors())
{
 	return;
}

// 添加参数
foreach (var propertyType in User.PropertyTypes)
{
    expression.Parameters.Add(propertyType.Key, propertyType.Value);
}

// 编译表达式
Func<User, bool> CustomizedFilter = u =>
{
	foreach (var propertyType in User.PropertyTypes)
    {
        expression.Parameters[propertyType.Key] = u.GetValue(propertyType.Key);
    }

    if (expression.Evaluate() is not bool ret)
    {
        return false;
    }
    return ret;
};

users.All(u => CustomizedFilter(u))
```

