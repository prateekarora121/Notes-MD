# Lesson 2 — Creational Patterns

**Creational patterns are all about ONE thing: *how objects get created*.**

Why does creating objects even need patterns? Because `new SomeClass()` scattered everywhere glues your code to specific classes (breaking the Dependency Inversion rule from Lesson 1). These patterns give you cleaner, more flexible ways to make objects.

We'll cover five, easiest first:
1. **Factory Method** — let a method decide which class to create
2. **Abstract Factory** — create matching *families* of objects
3. **Builder** — build a complicated object step by step
4. **Prototype** — make a new object by copying an existing one
5. **Singleton** — ensure there's only ever one instance

---

## 2.1 — Factory Method

### 🎯 The one-line idea
Instead of calling `new` directly, you call a *method* whose job is to create and return the object — so the *decision of which class to build* lives in one place.

### 🌍 Real-world analogy
A **pizza shop**. You don't walk into the kitchen and assemble the pizza yourself. You tell the counter "one Margherita," and the shop's *ordering system* decides which recipe/chef makes it. You (the customer) are decoupled from the messy details of creation.

### 😤 The problem (a story)
You're building an app that exports reports. At first, only PDF:

```csharp
public class ReportController
{
    public void Download(string data)
    {
        var exporter = new PdfExporter();   // hard-wired to PDF
        exporter.Export(data);
    }
}
```

Then the boss wants Excel too. And next month, CSV. You end up editing this method every time and littering it with `if`s:

```csharp
public void Download(string data, string format)
{
    IExporter exporter;
    if (format == "pdf")   exporter = new PdfExporter();
    else if (format == "excel") exporter = new ExcelExporter();
    else if (format == "csv")   exporter = new CsvExporter();
    else throw new Exception("Unknown");
    exporter.Export(data);
}
```
This breaks the Open/Closed rule — every new format edits this working code, and the same `if` chain gets copy-pasted wherever exporting happens.

### ✅ The solution
Put creation behind a method (a "factory"). Callers ask the factory for an exporter; only the factory knows the concrete classes.

```csharp
// 1. The contract every exporter fulfills
public interface IExporter { void Export(string data); }

// 2. The concrete exporters
public class PdfExporter   : IExporter { public void Export(string d) => Console.WriteLine("PDF!"); }
public class ExcelExporter : IExporter { public void Export(string d) => Console.WriteLine("Excel!"); }

// 3. The factory: the ONE place that knows how to create exporters
public class ExporterFactory
{
    public IExporter Create(string format) => format switch
    {
        "pdf"   => new PdfExporter(),
        "excel" => new ExcelExporter(),
        _       => throw new ArgumentException($"Unknown format: {format}")
    };
}

// 4. The caller no longer knows or cares about concrete classes
public class ReportController
{
    private readonly ExporterFactory _factory;
    public ReportController(ExporterFactory factory) => _factory = factory;

    public void Download(string data, string format)
    {
        IExporter exporter = _factory.Create(format); // just ask the factory
        exporter.Export(data);
    }
}
```

### 🔍 Line-by-line walkthrough
- **`IExporter`** — the job description. Everything that exports must have `Export`.
- **`PdfExporter` / `ExcelExporter`** — real workers fulfilling the contract.
- **`ExporterFactory.Create`** — the single spot that decides which concrete class to build. If a new format arrives, you touch *only this method* (or better, add a class — see the tech-lead note).
- **`ReportController`** — completely decoupled from concrete exporters. It knows only `IExporter`. Cleaner and testable.

> The "classic" Factory Method (from the textbook) uses inheritance — a base class with an `abstract Create()` that subclasses override. The version above (a factory class with a method) is the far more common, practical form in real C#. Learn this practical form first.

### ⚖️ Good & bad
**Good:** removes scattered `new`; one place to change; honors Open/Closed; callers depend on the interface.
**Bad / avoid when:** you only ever have *one* concrete type (then a factory is pointless ceremony — just use `new`).

### 🧭 Tech-lead note
In real .NET, the **Dependency Injection container is itself a factory**. You register `services.AddScoped<IExporter, PdfExporter>()` and the framework creates instances for you. .NET 8 added **keyed services** so you can register several implementations under keys ("pdf", "excel") and resolve by key — that's a built-in Factory Method. Prefer these built-ins over hand-rolled factories unless the creation logic is genuinely complex.

📺 Watch: [Christopher Okhravi – Factory Method](https://www.youtube.com/results?search_query=christopher+okhravi+factory+method+pattern) · Read: [Refactoring Guru (C#)](https://refactoring.guru/design-patterns/factory-method/csharp/example)

---

## 2.2 — Abstract Factory

### 🎯 The one-line idea
A factory that creates a whole **family of related objects** that are meant to be used together.

### 🌍 Real-world analogy
**IKEA furniture sets.** If you buy the "Scandinavian" set, everything matches — the chair, table, and lamp share a style. If you buy the "Industrial" set, you again get a matching chair, table, and lamp. You pick *one set* and get a coordinated family. An Abstract Factory is the "pick a set" mechanism.

### 😤 The problem
Your app must support both a **Light theme** and a **Dark theme**. Each theme has a matching button, checkbox, and text box. If you create these one by one with `if (dark) new DarkButton() else new LightButton()` scattered everywhere, you'll eventually mix a dark button with a light checkbox by accident.

### ✅ The solution
One factory *per family*, each producing a full matched set.

```csharp
// Product contracts
public interface IButton   { void Render(); }
public interface ICheckbox { void Render(); }

// Light family
public class LightButton   : IButton   { public void Render() => Console.WriteLine("⬜ light button"); }
public class LightCheckbox : ICheckbox { public void Render() => Console.WriteLine("⬜ light checkbox"); }

// Dark family
public class DarkButton    : IButton   { public void Render() => Console.WriteLine("⬛ dark button"); }
public class DarkCheckbox  : ICheckbox { public void Render() => Console.WriteLine("⬛ dark checkbox"); }

// The Abstract Factory: promises to create a full matching family
public interface IThemeFactory
{
    IButton   CreateButton();
    ICheckbox CreateCheckbox();
}

public class LightThemeFactory : IThemeFactory
{
    public IButton   CreateButton()   => new LightButton();
    public ICheckbox CreateCheckbox() => new LightCheckbox();
}

public class DarkThemeFactory : IThemeFactory
{
    public IButton   CreateButton()   => new DarkButton();
    public ICheckbox CreateCheckbox() => new DarkCheckbox();
}
```

Usage — pick the family once, everything stays consistent:

```csharp
IThemeFactory factory = userPrefersDark ? new DarkThemeFactory() : new LightThemeFactory();
IButton   button   = factory.CreateButton();   // guaranteed to match...
ICheckbox checkbox = factory.CreateCheckbox();  // ...this one
button.Render();
checkbox.Render();
```

### 🔍 Walkthrough
The trick: you choose the *factory* once (`DarkThemeFactory`), and from then on every object it makes belongs to the same family. It's now *impossible* to accidentally mix a dark button with a light checkbox.

### ⚖️ Good & bad
**Good:** guarantees a consistent family; swap the whole set by swapping one factory.
**Bad / avoid when:** you only have one family, or the products aren't really related. Also, adding a *new product type* (say, `ISlider`) means editing *every* factory — that part isn't Open/Closed.

**Factory Method vs Abstract Factory (common confusion):**
- Factory Method creates **one** product.
- Abstract Factory creates a **family** of related products.

### 🧭 Tech-lead note
The realistic use is **provider abstraction** — e.g., a factory that produces a matching set of clients for AWS vs Azure (blob store + queue + secret store). Great for multi-cloud or swappable-backend designs. Watch for it becoming a maintenance tax when the product set changes often.

📺 [Christopher Okhravi – Abstract Factory](https://www.youtube.com/results?search_query=christopher+okhravi+abstract+factory) · [Refactoring Guru](https://refactoring.guru/design-patterns/abstract-factory/csharp/example)

---

## 2.3 — Builder

### 🎯 The one-line idea
Build a complex object **step by step** with readable, named steps, instead of a giant confusing constructor.

### 🌍 Real-world analogy
**Ordering a custom Subway sandwich.** You don't shout all 10 ingredients at once. You go step by step: "wheat bread… turkey… add cheese… no onions… toast it." At the end you get your sandwich. Each step is clear and optional.

### 😤 The problem
An object with many optional settings leads to a monster constructor:

```csharp
// What do all these arguments even mean? Which are optional? Easy to swap two by accident.
var pizza = new Pizza("Large", true, false, true, true, false, "thin", 2);
```
Nobody can read that. Is the 3rd `false` "extra cheese" or "gluten free"? This is called the *telescoping constructor* problem.

### ✅ The solution
A builder with named, chainable steps:

```csharp
public class Pizza
{
    public string Size { get; set; } = "Medium";
    public bool Cheese { get; set; }
    public bool Mushrooms { get; set; }
    public string Crust { get; set; } = "regular";
    public override string ToString() => $"{Size} {Crust}-crust pizza (cheese={Cheese}, mushrooms={Mushrooms})";
}

public class PizzaBuilder
{
    private readonly Pizza _pizza = new();

    public PizzaBuilder Size(string size)   { _pizza.Size = size; return this; }
    public PizzaBuilder AddCheese()         { _pizza.Cheese = true; return this; }
    public PizzaBuilder AddMushrooms()      { _pizza.Mushrooms = true; return this; }
    public PizzaBuilder Crust(string crust) { _pizza.Crust = crust; return this; }

    public Pizza Build() => _pizza;   // hand over the finished pizza
}
```

Usage reads like plain English:

```csharp
Pizza pizza = new PizzaBuilder()
    .Size("Large")
    .Crust("thin")
    .AddCheese()
    .AddMushrooms()
    .Build();
```

### 🔍 Walkthrough
- Each step method changes one thing and then **`return this;`** — returning the builder itself is what lets you *chain* calls (`.Size().AddCheese()…`). This chaining style is called a **fluent interface**.
- **`Build()`** returns the finished object. It's also the perfect place to *validate* ("a pizza must have a size") before handing it back.

### ⚖️ Good & bad
**Good:** super readable; optional steps are obvious; can validate before returning; can produce immutable objects.
**Bad / avoid when:** the object is simple — a plain constructor or object initializer is less code.

### 🧭 Tech-lead note
Modern C# often removes the need for a builder: **object initializers** plus `required` members give readable, safe construction:
```csharp
var pizza = new Pizza { Size = "Large", Cheese = true, Crust = "thin" }; // clear, no builder needed
```
Keep a real builder when construction has **ordering rules or validation** a plain initializer can't enforce, or when you build the same object in many small steps across code. You've already used builders: ASP.NET Core's `WebApplication.CreateBuilder(args)` is exactly this pattern.

📺 [Christopher Okhravi – Builder](https://www.youtube.com/results?search_query=christopher+okhravi+builder+pattern) · [Refactoring Guru](https://refactoring.guru/design-patterns/builder/csharp/example)

---

## 2.4 — Prototype

### 🎯 The one-line idea
Create a new object by **copying an existing one** instead of building it from scratch.

### 🌍 Real-world analogy
**Photocopying a filled-in form.** Instead of re-writing all the standard fields every time, you keep one filled template and photocopy it, then tweak the few fields that differ.

### 😤 The problem
Some objects are expensive or tedious to set up (lots of configuration, a slow database call, etc.). If you need 100 *almost identical* ones, rebuilding each from scratch is wasteful.

### ✅ The solution
Give the object a `Clone()` method that returns a copy of itself.

```csharp
public class EmailTemplate
{
    public string Subject { get; set; } = "";
    public string Body { get; set; } = "";
    public List<string> Recipients { get; set; } = new();

    public EmailTemplate Clone() => new()
    {
        Subject = this.Subject,
        Body = this.Body,
        Recipients = new List<string>(this.Recipients) // copy the list too! (see warning)
    };
}
```

Usage:

```csharp
var welcome = new EmailTemplate { Subject = "Welcome!", Body = "Hello and welcome." };

var welcomeForBob = welcome.Clone();          // start from the template
welcomeForBob.Recipients.Add("bob@x.com");    // tweak only what's different
```

### 🔍 Walkthrough & a crucial warning
- `Clone()` returns a fresh object with the same values.
- ⚠️ **Shallow vs deep copy** — the classic beginner bug. Notice we wrote `new List<string>(this.Recipients)` and *not* `Recipients = this.Recipients`. If we'd shared the same list, adding Bob to the copy would *also* add Bob to the original, because both would point to the *same* list in memory. Copying the values inside is a **deep copy**; sharing the reference is a **shallow copy**. Know which you want.

### ⚖️ Good & bad
**Good:** skips expensive setup; handy for "template" objects.
**Bad / avoid when:** objects are cheap to build, or contain complex nested references that are hard to copy correctly.

### 🧭 Tech-lead note
Modern C# has this built in for **records**:
```csharp
public record Point(int X, int Y);
var a = new Point(1, 2);
var b = a with { Y = 5 };   // clone-and-modify in one line → b is (1, 5), a is unchanged
```
The `with` expression is a compiler-generated prototype (shallow). Just remember it's shallow — nested reference objects are still shared.

📺 [Refactoring Guru – Prototype (C#)](https://refactoring.guru/design-patterns/prototype/csharp/example)

---

## 2.5 — Singleton

### 🎯 The one-line idea
Guarantee a class has **exactly one instance** in the whole application, with a single shared access point.

### 🌍 Real-world analogy
The **President of a country**. There's only one at a time, and everyone refers to "the President." You don't create a new president every time you need one.

### 😤 The problem
Some things should truly exist once — e.g., a single configuration object everyone reads from. If different parts of the app each `new` their own, they might disagree.

### ✅ The solution (the thread-safe C# way)

```csharp
public sealed class AppConfig
{
    // Lazy<T> builds the single instance the first time it's needed, and is thread-safe.
    private static readonly Lazy<AppConfig> _instance = new(() => new AppConfig());

    public static AppConfig Instance => _instance.Value;   // the single access point

    private AppConfig() { }   // private constructor → nobody else can `new` this

    public string Environment { get; } = "Production";
}
```

Usage:

```csharp
string env = AppConfig.Instance.Environment; // same instance everywhere
```

### 🔍 Walkthrough
- **`private AppConfig()`** — the private constructor is the key. It stops any other code from doing `new AppConfig()`. The *only* way to get one is through `Instance`.
- **`Lazy<AppConfig>`** — .NET's built-in helper that (a) creates the object only on first use and (b) is safe even if multiple threads ask at once.
- **`sealed`** — prevents inheritance, which could otherwise create a second instance.

### ⚖️ Good & bad — read this carefully
**Good:** guarantees one instance.
**Bad (many downsides!):**
- It's **global state** — any code can reach it, so it becomes a hidden dependency you can't see in a constructor.
- It's **hard to test** — you can't easily swap in a fake, and tests running in parallel can interfere.
- It quietly violates the Dependency Inversion rule from Lesson 1.

### 🧭 Tech-lead note — **challenge hand-written singletons in code review**
This is the pattern juniors overuse and seniors push back on. In .NET you almost never write a singleton by hand. Instead you register a normal class with the DI container as a singleton:
```csharp
services.AddSingleton<IAppConfig, AppConfig>();
```
You get the *same* "only one instance" guarantee **plus** testability (inject a fake in tests) and honest, visible dependencies (it shows up in constructors). When you see a hand-rolled `.Instance` singleton in a PR, ask whether DI should own its lifetime instead.

📺 [Nick Chapsas – why DI beats manual Singleton](https://www.youtube.com/results?search_query=nick+chapsas+singleton+dependency+injection) · [Refactoring Guru](https://refactoring.guru/design-patterns/singleton/csharp/example)

---

## ✅ Lesson 2 recap

| Pattern | In one line | Modern C# shortcut |
|---|---|---|
| Factory Method | A method decides which class to create | DI container / keyed services |
| Abstract Factory | Create a matching *family* of objects | (still useful for providers) |
| Builder | Build a complex object step-by-step | object initializers + `required` |
| Prototype | Make a new object by copying one | `record` + `with` |
| Singleton | Exactly one instance | `services.AddSingleton<T>()` |

➡️ Next: **`03-structural-patterns.md`** — how to *assemble* objects into bigger, flexible structures.
