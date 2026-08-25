# Lesson 2 — Creational Patterns

**Creational patterns sirf ONE cheez ke bare mein hain: *objects kaise create hote hain*.**

Objects create karne ke liye patterns ki zarurat kyun padti hai? Kyunki `new SomeClass()` jo har jagah scattered hota hai, wo aapke code ko specific classes se glue kar deta hai (Lesson 1 ka Dependency Inversion rule break karta hai). Yeh patterns aapko objects banane ke cleaner, zyada flexible tarike dete hain.

Hum paanch cover karenge, easiest pehle:
1. **Factory Method** — ek method decide kare ki kaunsi class create karni hai
2. **Abstract Factory** — matching *families* of objects create karo
3. **Builder** — ek complicated object ko step by step build karo
4. **Prototype** — ek existing object ko copy karke naya object banao
5. **Singleton** — ensure karo ki sirf ek hi instance ho hamesha

---

## 2.1 — Factory Method

### 🎯 The one-line idea
`new` ko directly call karne ke bajaye, aap ek *method* call karte ho jiska job hota hai object create karna aur return karna — isliye *decision ki kaunsi class build karni hai* ek hi jagah rehta hai.

### 🌍 Real-world analogy
Ek **pizza shop**. Aap kitchen mein jaake khud pizza assemble nahi karte. Aap counter ko batate ho "one Margherita," aur shop ka *ordering system* decide karta hai ki kaunsa recipe/chef usse banayega. Aap (customer) creation ki messy details se decoupled ho.

### 😤 The problem (a story)
Aap ek app bana rahe ho jo reports export karta hai. Pehle, sirf PDF:

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

Phir boss ko Excel bhi chahiye. Aur next month, CSV. Aap har baar isi method ko edit karte rehte ho aur ise `if`s se litter karte ho:

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
Yeh Open/Closed rule ko break karta hai — har naya format is working code ko edit karta hai, aur wahi `if` chain jahan bhi exporting hota hai copy-paste ho jaati hai.

### ✅ The solution
Creation ko ek method (ek "factory") ke peeche daal do. Callers exporter ke liye factory se puchte hain; sirf factory ko concrete classes pata hoti hain.

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
- **`IExporter`** — job description hai. Jo bhi export karta hai usme `Export` hona chahiye.
- **`PdfExporter` / `ExcelExporter`** — real workers jo contract fulfill karte hain.
- **`ExporterFactory.Create`** — single spot jo decide karta hai kaunsi concrete class build karni hai. Agar naya format aata hai, aap *sirf isi method* ko touch karte ho (ya better, ek class add karo — tech-lead note dekho).
- **`ReportController`** — concrete exporters se completely decoupled hai. Isko sirf `IExporter` pata hai. Cleaner aur testable.

> "Classic" Factory Method (textbook se) inheritance use karta hai — ek base class jisme ek `abstract Create()` hota hai jise subclasses override karte hain. Upar wala version (ek factory class ek method ke saath) real C# mein far more common, practical form hai. Pehle yeh practical form seekho.

### ⚖️ Good & bad
**Good:** scattered `new` ko remove karta hai; change karne ke liye ek jagah; Open/Closed honor karta hai; callers interface par depend karte hain.
**Bad / avoid when:** aapke paas sirf *ek* concrete type hai (tab factory pointless ceremony hai — sirf `new` use karo).

### 🧭 Tech-lead note
Real .NET mein, **Dependency Injection container khud ek factory hai**. Aap `services.AddScoped<IExporter, PdfExporter>()` register karte ho aur framework aapke liye instances create karta hai. .NET 8 ne **keyed services** add kiye jisse aap several implementations ko keys ("pdf", "excel") ke under register kar sakte ho aur key se resolve kar sakte ho — yeh ek built-in Factory Method hai. Hand-rolled factories ke bajaye inhi built-ins ko prefer karo jab tak creation logic genuinely complex na ho.

📺 Watch: [Christopher Okhravi – Factory Method](https://www.youtube.com/results?search_query=christopher+okhravi+factory+method+pattern) · Read: [Refactoring Guru (C#)](https://refactoring.guru/design-patterns/factory-method/csharp/example)

---

## 2.2 — Abstract Factory

### 🎯 The one-line idea
Ek factory jo **related objects ki ek whole family** create karta hai jo saath mein use hone ke liye banaye gaye hain.

### 🌍 Real-world analogy
**IKEA furniture sets.** Agar aap "Scandinavian" set khareedte ho, to sab kuch match karta hai — chair, table, aur lamp same style share karte hain. Agar aap "Industrial" set khareedte ho, to phir aapko matching chair, table, aur lamp milta hai. Aap *ek set* pick karte ho aur ek coordinated family paate ho. Abstract Factory "pick a set" mechanism hai.

### 😤 The problem
Aapke app ko dono ek **Light theme** aur ek **Dark theme** support karna chahiye. Har theme ka matching button, checkbox, aur text box hai. Agar aap inhe one by one `if (dark) new DarkButton() else new LightButton()` ke saath create karte ho jo har jagah scattered ho, to eventually aap galti se ek dark button ko light checkbox ke saath mix kar dete ho.

### ✅ The solution
Ek factory *per family*, har ek ek full matched set produce karta hai.

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

Usage — family ko ek baar pick karo, sab kuch consistent rehta hai:

```csharp
IThemeFactory factory = userPrefersDark ? new DarkThemeFactory() : new LightThemeFactory();
IButton   button   = factory.CreateButton();   // guaranteed to match...
ICheckbox checkbox = factory.CreateCheckbox();  // ...this one
button.Render();
checkbox.Render();
```

### 🔍 Walkthrough
Trick yeh hai: aap *factory* ko ek baar choose karte ho (`DarkThemeFactory`), aur uske baad wo jo bhi object banata hai wo same family ka hota hai. Ab yeh *impossible* hai ki galti se ek dark button light checkbox ke saath mix ho jaye.

### ⚖️ Good & bad
**Good:** ek consistent family guarantee karta hai; whole set ko ek factory swap karke swap kar sakte ho.
**Bad / avoid when:** aapke paas sirf ek family hai, ya products really related nahi hain. Iske alawa, ek *new product type* (kaho, `ISlider`) add karna matlab *every* factory ko edit karna — wo part Open/Closed nahi hai.

**Factory Method vs Abstract Factory (common confusion):**
- Factory Method **ek** product create karta hai.
- Abstract Factory related products ki ek **family** create karta hai.

### 🧭 Tech-lead note
Realistic use hai **provider abstraction** — e.g., ek factory jo AWS vs Azure ke liye matching set of clients produce karta hai (blob store + queue + secret store). Multi-cloud ya swappable-backend designs ke liye great hai. Isse maintenance tax banne se dhyan rakhna jab product set often change hota hai.

📺 [Christopher Okhravi – Abstract Factory](https://www.youtube.com/results?search_query=christopher+okhravi+abstract+factory) · [Refactoring Guru](https://refactoring.guru/design-patterns/abstract-factory/csharp/example)

---

## 2.3 — Builder

### 🎯 The one-line idea
Ek complex object **step by step** build karo readable, named steps ke saath, ek giant confusing constructor ke bajaye.

### 🌍 Real-world analogy
**Custom Subway sandwich order karna.** Aap saare 10 ingredients ek saath nahi chillate. Aap step by step jaate ho: "wheat bread… turkey… add cheese… no onions… toast it." End mein aapko apna sandwich mil jaata hai. Har step clear aur optional hai.

### 😤 The problem
Ek object jisme kaafi optional settings hoti hain, ek monster constructor ki taraf le jaata hai:

```csharp
// What do all these arguments even mean? Which are optional? Easy to swap two by accident.
var pizza = new Pizza("Large", true, false, true, true, false, "thin", 2);
```
Isko koi bhi padh nahi sakta. 3rd `false` "extra cheese" hai ya "gluten free"? Isse *telescoping constructor* problem kehte hain.

### ✅ The solution
Named, chainable steps ke saath ek builder:

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

Usage plain English ki tarah padhta hai:

```csharp
Pizza pizza = new PizzaBuilder()
    .Size("Large")
    .Crust("thin")
    .AddCheese()
    .AddMushrooms()
    .Build();
```

### 🔍 Walkthrough
- Har step method ek cheez change karta hai aur phir **`return this;`** karta hai — builder ko khud return karna hi wo cheez hai jo aapko calls *chain* karne deti hai (`.Size().AddCheese()…`). Isse chaining style ko **fluent interface** kehte hain.
- **`Build()`** finished object return karta hai. Yeh *validate* karne ka bhi perfect jagah hai ("a pizza must have a size") wapas handover karne se pehle.

### ⚖️ Good & bad
**Good:** super readable; optional steps obvious hote hain; return karne se pehle validate kar sakte ho; immutable objects produce kar sakte ho.
**Bad / avoid when:** object simple hai — ek plain constructor ya object initializer kam code hai.

### 🧭 Tech-lead note
Modern C# often builder ki zarurat remove kar deta hai: **object initializers** plus `required` members readable, safe construction dete hain:
```csharp
var pizza = new Pizza { Size = "Large", Cheese = true, Crust = "thin" }; // clear, no builder needed
```
Ek real builder rakho jab construction mein **ordering rules ya validation** ho jo ek plain initializer enforce nahi kar sakta, ya jab aap same object ko kaafi small steps mein pure code mein build karte ho. Aap already builders use kar chuke ho: ASP.NET Core ka `WebApplication.CreateBuilder(args)` exactly yeh pattern hai.

📺 [Christopher Okhravi – Builder](https://www.youtube.com/results?search_query=christopher+okhravi+builder+pattern) · [Refactoring Guru](https://refactoring.guru/design-patterns/builder/csharp/example)

---

## 2.4 — Prototype

### 🎯 The one-line idea
Ek naya object **existing ek ko copy karke** create karo, scratch se build karne ke bajaye.

### 🌍 Real-world analogy
**Ek filled-in form ko photocopy karna.** Har baar saare standard fields re-write karne ke bajaye, aap ek filled template rakhte ho aur usko photocopy karte ho, phir un few fields ko tweak karte ho jo different hain.

### 😤 The problem
Kuch objects expensive ya tedious hote hain set up karne ke liye (lots of configuration, ek slow database call, etc.). Agar aapko 100 *almost identical* ones chahiye, to har ek ko scratch se rebuild karna wasteful hai.

### ✅ The solution
Object ko ek `Clone()` method do jo apni khud ki copy return kare.

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
- `Clone()` same values ke saath ek fresh object return karta hai.
- ⚠️ **Shallow vs deep copy** — classic beginner bug. Notice karo humne `new List<string>(this.Recipients)` likha *aur* `Recipients = this.Recipients` nahi. Agar humne same list share ki hoti, to copy mein Bob ko add karna *bhi* original mein Bob add kar deta, kyunki dono memory mein *same* list ki taraf point karte. Values ko andar copy karna ek **deep copy** hai; reference share karna ek **shallow copy** hai. Jaano aapko kaunsa chahiye.

### ⚖️ Good & bad
**Good:** expensive setup skip karta hai; "template" objects ke liye handy hai.
**Bad / avoid when:** objects build karne mein cheap hain, ya complex nested references contain karte hain jo sahi tarike se copy karna hard hai.

### 🧭 Tech-lead note
Modern C# ke paas yeh built in hai **records** ke liye:
```csharp
public record Point(int X, int Y);
var a = new Point(1, 2);
var b = a with { Y = 5 };   // clone-and-modify in one line → b is (1, 5), a is unchanged
```
`with` expression ek compiler-generated prototype hai (shallow). Sirf yaad rakho yeh shallow hai — nested reference objects abhi bhi shared rehte hain.

📺 [Refactoring Guru – Prototype (C#)](https://refactoring.guru/design-patterns/prototype/csharp/example)

---

## 2.5 — Singleton

### 🎯 The one-line idea
Guarantee karo ki ek class ki whole application mein **exactly one instance** ho, ek single shared access point ke saath.

### 🌍 Real-world analogy
Kisi **country ka President**. Ek time mein sirf ek hota hai, aur sab "the President" refer karte hain. Aap har baar jab aapko chahiye ho ek naya president create nahi karte.

### 😤 The problem
Kuch cheezein truly ek baar exist honi chahiye — e.g., ek single configuration object jisse sab log read karte hain. Agar app ke different parts apna apna `new` karein, to wo disagree kar sakte hain.

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
- **`private AppConfig()`** — private constructor key hai. Yeh kisi bhi doosre code ko `new AppConfig()` karne se rokta hai. Ek instance paane ka *sirf* tarika `Instance` ke through hai.
- **`Lazy<AppConfig>`** — .NET ka built-in helper jo (a) object ko sirf first use par create karta hai aur (b) safe hai even if multiple threads ek saath ask karein.
- **`sealed`** — inheritance ko prevent karta hai, jo otherwise ek second instance create kar sakta tha.

### ⚖️ Good & bad — read this carefully
**Good:** ek instance guarantee karta hai.
**Bad (many downsides!):**
- Yeh **global state** hai — koi bhi code isse reach kar sakta hai, isliye yeh ek hidden dependency ban jaata hai jo aap constructor mein nahi dekh sakte.
- Yeh **test karna hard** hai — aap easily ek fake swap in nahi kar sakte, aur parallel mein run hone wale tests interfere kar sakte hain.
- Yeh quietly Lesson 1 ke Dependency Inversion rule ko violate karta hai.

### 🧭 Tech-lead note — **challenge hand-written singletons in code review**
Yeh wo pattern hai jise juniors overuse karte hain aur seniors push back karte hain. .NET mein aap almost never ek singleton hand se likhte ho. Instead aap ek normal class ko DI container ke saath singleton ki tarah register karte ho:
```csharp
services.AddSingleton<IAppConfig, AppConfig>();
```
Aapko *same* "only one instance" guarantee milti hai **plus** testability (tests mein ek fake inject karo) aur honest, visible dependencies (yeh constructors mein show hota hai). Jab aap ek PR mein ek hand-rolled `.Instance` singleton dekho, to puchho ki kya DI ko iski lifetime own karni chahiye instead.

📺 [Nick Chapsas – why DI beats manual Singleton](https://www.youtube.com/results?search_query=nick+chapsas+singleton+dependency+injection) · [Refactoring Guru](https://refactoring.guru/design-patterns/singleton/csharp/example)

---

## ✅ Lesson 2 recap

| Pattern | In one line | Modern C# shortcut |
|---|---|---|
| Factory Method | Ek method decide karta hai kaunsi class create karni hai | DI container / keyed services |
| Abstract Factory | Objects ki ek matching *family* create karo | (still useful for providers) |
| Builder | Ek complex object ko step-by-step build karo | object initializers + `required` |
| Prototype | Ek object ko copy karke naya object banao | `record` + `with` |
| Singleton | Exactly one instance | `services.AddSingleton<T>()` |

➡️ Next: **`03-structural-patterns.md`** — objects ko bigger, flexible structures mein *assemble* kaise karein.
