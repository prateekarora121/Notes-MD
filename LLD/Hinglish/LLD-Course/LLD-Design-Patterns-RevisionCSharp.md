# LLD Design Patterns — Quick Revision (C#)

> Yeh quick-revision notes hain jo 5-part LLD course (Foundations → Creational → Structural → Behavioral → Modern C# & Next Steps) se derive kiye gaye hain — har part aur har pattern cover kiya gaya hai. Interview / PR review ke pehle 20 min mein poora brush-up ho jaaye, bina course kholna pade.

**Golden thread:** Har pattern basically ek SOLID rule hai jo kisi specific situation pe apply hua hai. Padhte waqt hamesha poochho: *"Yeh kaunsa SOLID rule protect kar raha hai?"*

---

# PART 1 — Foundations (OOP + SOLID)

> Sabse important lesson. Iske bina baaki patterns "magic spells" lagenge.

## OOP refresher

**Q: Class vs Object?**
A: Class = blueprint. Object = us blueprint se bana real instance (`new Dog()`).

**Q: Interface kya hai — course ka sabse important idea?**
A: Ek **contract** / job description. Methods ki list jo class **promise** karti hai provide karegi — interface mein khud koi code nahi, sirf promises.

```csharp
public interface INotifier { void Send(string message); }

public class EmailNotifier : INotifier
{ public void Send(string m) => Console.WriteLine($"Email: {m}"); }

public class SmsNotifier : INotifier
{ public void Send(string m) => Console.WriteLine($"SMS: {m}"); }

INotifier n = new EmailNotifier(); // niche wala code kabhi nahi badalta
n.Send("Your order shipped!");
```

> Yahi hai **"program to an interface, not an implementation."** Variable ka *type* contract hota hai (`INotifier`), specific class nahi. Yeh habit almost har pattern ke peeche hai.

**Q: Inheritance vs Composition?**
A: **Inheritance = "is-a"** (`Dog : Animal`). **Composition = "has-a"** (`Car` ke andar ek `Engine`).

```csharp
public class Car
{
    private readonly Engine _engine = new Engine(); // Car HAS-A Engine
    public void StartCar() => _engine.Start();
}
```

**Q: Composition kyun prefer karte hain?**
A: Inheritance rigid family trees banata hai — galat tree baad mein change karna painful. Composition LEGO jaise parts snap/rearrange karne deta hai. "Composition over inheritance."

**Q: Dependency Injection (DI) kya hai?**
A: Class khud apni zarurat ki cheez *banaye* iske bajaye tum use bahar se (usually constructor se) *hand in* karte ho.

```csharp
// WITH DI: OrderService ab kisi bhi INotifier ke saath kaam karta hai
public class OrderService
{
    private readonly INotifier _notifier;
    public OrderService(INotifier notifier) => _notifier = notifier; // injected
    public void Place() => _notifier.Send("Order placed");
}

var svc = new OrderService(new SmsNotifier()); // caller decides
```

Kyun matter karta hai: **Flexible** (Email ↔ SMS swap), **Testable** (fake inject karo), **Honest** (constructor batata hai kya chahiye — no hidden surprises).

## SOLID — 5 rules

| Letter | Rule | One-line |
|---|---|---|
| **S** | Single Responsibility | Ek class ka ek kaam → change karne ka ek hi reason |
| **O** | Open/Closed | Extension ke liye open, modification ke liye closed |
| **L** | Liskov Substitution | Subtype ko base ki jagah bina surprise ke chalna chahiye |
| **I** | Interface Segregation | Chhote focused interfaces > ek fat interface |
| **D** | Dependency Inversion | Abstractions pe depend karo, concrete classes pe nahi |

**S — SRP:** Ek class = ek job. `Order` (logic), `OrderRepository` (save), `EmailService` (email) alag rakho — taaki har class sirf ek reason se change ho.

**O — OCP:** Naya behavior = *naya code* add karo, working code edit mat karo. Smell = badhta hua `if`/`switch` chain. Fix = interface + har case apni class (yehi **Strategy** hai).

```csharp
public interface IShipping { decimal Cost(decimal weight); }
public class Standard : IShipping { public decimal Cost(decimal w) => w * 1.5m; }
public class Express  : IShipping { public decimal Cost(decimal w) => w * 3.0m; }
// Overnight? bas ek NEW class add karo — kuch aur nahi badalta
```

**L — LSP:** Subtype base ke promises honor kare. Anti-example: `Penguin : Bird` jismein `Fly()` throw karta hai → jo bhi `Bird.Fly()` call karega crash. Fix: `Fly()` ko sab birds pe force mat karo (alag `IFlyingBird`).

**I — ISP:** `IMachine { Print; Scan; Fax; }` galat — `SimplePrinter` ko Scan/Fax implement karna padta hai. Fix: `IPrinter`, `IScanner` alag.

**D — DIP:** High-level logic concrete low-level detail pe depend na kare.

```csharp
public interface IDataSource { string[] GetRows(); }
public class ReportGenerator
{
    private readonly IDataSource _source;
    public ReportGenerator(IDataSource source) => _source = source; // SQL, file, mock...
}
```

> **DIP + interface habit + DI** — teeno ek hi core idea ke views hain: *important code ko specific, replaceable details se glued hone se bachao.*

## "Keep it sane" rules (over-engineering se bachne ke liye)

- **KISS** — Keep It Simple. Simplest cheez jo kaam kare usually right hoti hai.
- **YAGNI** — You Aren't Gonna Need It. "Just in case" flexibility mat add karo; real need pe add karo. Over-engineering ki #1 wajah.
- **DRY** — Don't Repeat Yourself. Shared code sirf tab extract karo jab actually repeat dikhe (rule of thumb: 3rd time). Bahut early extract = *wrong* abstraction, jo duplication se bhi worse.

---

# PART 2 — Creational Patterns

> Sab ka ek hi focus: **objects kaise create hote hain.** Scattered `new SomeClass()` code ko concrete classes se glue kar deta hai (DIP break). Yeh patterns cleaner creation dete hain.

## 2.1 Factory Method

- **Intent:** `new` directly call karne ke bajaye ek *method* call karo jiska job hai object create + return karna. *Kaunsi class banani hai* ka decision ek jagah rehta hai.
- **Analogy:** Pizza shop — tum kitchen mein ghuske assemble nahi karte, counter ko batate ho aur ordering system decide karta hai.
- **When:** Multiple concrete types (pdf/excel/csv), aur creation logic ek jagah chahiye. **Avoid:** sirf ek concrete type ho (bas `new` use karo).

```csharp
public interface IExporter { void Export(string data); }
public class PdfExporter   : IExporter { public void Export(string d) => Console.WriteLine("PDF!"); }
public class ExcelExporter : IExporter { public void Export(string d) => Console.WriteLine("Excel!"); }

public class ExporterFactory
{
    public IExporter Create(string format) => format switch
    {
        "pdf"   => new PdfExporter(),
        "excel" => new ExcelExporter(),
        _       => throw new ArgumentException($"Unknown format: {format}")
    };
}

public class ReportController
{
    private readonly ExporterFactory _factory;
    public ReportController(ExporterFactory factory) => _factory = factory;
    public void Download(string data, string format)
        => _factory.Create(format).Export(data); // caller ko concrete classes ka pata nahi
}
```

> **Tech-lead note:** .NET mein DI container khud ek factory hai. `services.AddScoped<IExporter, PdfExporter>()`; .NET 8 **keyed services** se multiple impls key ("pdf"/"excel") se resolve kar sakte ho — built-in Factory Method. Hand-rolled factory sirf tab jab creation genuinely complex ho.
> Note: "classic" Factory Method inheritance use karta hai (base class + `abstract Create()` jise subclasses override karti hain); upar wala factory-class version real C# mein zyada common/practical hai.

## 2.2 Abstract Factory

- **Intent:** Ek factory jo related objects ki **whole family** create karta hai jo saath use hone ke liye bane hain.
- **Analogy:** IKEA sets — "Scandinavian" set liya to chair+table+lamp sab match karte hain.
- **When:** Consistent family guarantee chahiye. **Avoid:** sirf ek family ho, ya products related na hon. Naya product type (`ISlider`) add karna = *har* factory edit (yeh part OCP nahi).

```csharp
public interface IButton   { void Render(); }
public interface ICheckbox { void Render(); }

public class LightButton   : IButton   { public void Render() => Console.WriteLine("light button"); }
public class LightCheckbox : ICheckbox { public void Render() => Console.WriteLine("light checkbox"); }
public class DarkButton    : IButton   { public void Render() => Console.WriteLine("dark button"); }
public class DarkCheckbox  : ICheckbox { public void Render() => Console.WriteLine("dark checkbox"); }

public interface IThemeFactory { IButton CreateButton(); ICheckbox CreateCheckbox(); }

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

IThemeFactory f = userPrefersDark ? new DarkThemeFactory() : new LightThemeFactory();
f.CreateButton().Render();   // guaranteed same family
f.CreateCheckbox().Render(); // dark button + light checkbox mix impossible
```

- **Factory Method vs Abstract Factory:** Factory Method = **ek** product; Abstract Factory = products ki **family**.
- **Tech-lead note:** Provider abstraction (AWS vs Azure ke matching clients: blob + queue + secret). Multi-cloud ke liye great; product set often change ho to maintenance tax ban jaata hai.

## 2.3 Builder

- **Intent:** Complex object **step-by-step** readable named steps se banao, giant confusing constructor ke bajaye.
- **Analogy:** Subway sandwich — "wheat bread… turkey… add cheese… no onions… toast it."
- **Problem:** Telescoping constructor — `new Pizza("Large", true, false, true, true, false, "thin", 2)` — koi padh nahi sakta.
- **When:** Object complex, ordering/validation rules ho. **Avoid:** simple object (plain constructor ya object initializer kam code).

```csharp
public class Pizza
{
    public string Size { get; set; } = "Medium";
    public bool Cheese { get; set; }
    public bool Mushrooms { get; set; }
    public string Crust { get; set; } = "regular";
}

public class PizzaBuilder
{
    private readonly Pizza _pizza = new();
    public PizzaBuilder Size(string s)   { _pizza.Size = s; return this; }
    public PizzaBuilder AddCheese()      { _pizza.Cheese = true; return this; }
    public PizzaBuilder AddMushrooms()   { _pizza.Mushrooms = true; return this; }
    public PizzaBuilder Crust(string c)  { _pizza.Crust = c; return this; }
    public Pizza Build() => _pizza;   // yahan validate bhi kar sakte ho
}

Pizza p = new PizzaBuilder().Size("Large").Crust("thin").AddCheese().AddMushrooms().Build();
```

- Har step `return this;` karta hai → yehi chaining ("**fluent interface**") deta hai. `Build()` finished object + validation spot.
- **Tech-lead note:** Modern C# often builder ki zarurat hatata hai:
```csharp
var pizza = new Pizza { Size = "Large", Cheese = true, Crust = "thin" }; // object initializer
```
Real builder tab rakho jab construction mein ordering rules/validation ho jo initializer enforce na kar sake. `WebApplication.CreateBuilder(args)` yehi pattern hai.

## 2.4 Prototype

- **Intent:** Naya object **existing ek ko copy** karke banao, scratch se nahi.
- **Analogy:** Filled-in form ko photocopy karke sirf few fields tweak karna.
- **When:** Setup expensive/tedious ho, 100 almost-identical objects chahiye. **Avoid:** objects cheap hon, ya complex nested references jinhe copy karna hard.

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
        Recipients = new List<string>(this.Recipients) // list bhi copy karo!
    };
}

var welcome = new EmailTemplate { Subject = "Welcome!", Body = "Hello." };
var forBob = welcome.Clone();
forBob.Recipients.Add("bob@x.com"); // sirf difference tweak
```

> ⚠️ **Shallow vs Deep copy** (classic bug): `new List<string>(this.Recipients)` = deep (values copy). Agar `Recipients = this.Recipients` likha hota to dono same list share karte → Bob dono mein add ho jaata. Reference share = **shallow**, values copy = **deep**. Jaano kaunsa chahiye.

- **Tech-lead note:** `record` + `with` built-in prototype hai (shallow):
```csharp
public record Point(int X, int Y);
var b = a with { Y = 5 };  // clone-and-modify; nested refs abhi bhi shared
```

## 2.5 Singleton

- **Intent:** Guarantee karo poori app mein **exactly one instance**, single shared access point ke saath.
- **Analogy:** Country ka President — ek time pe ek.
- **When:** Kuch truly ek baar exist kare (single config). **Avoid (bahut downsides!):** global state (hidden dependency), test karna hard, DIP violate karta hai.

```csharp
public sealed class AppConfig
{
    private static readonly Lazy<AppConfig> _instance = new(() => new AppConfig());
    public static AppConfig Instance => _instance.Value;   // single access point
    private AppConfig() { }   // private ctor → koi aur `new` nahi kar sakta
    public string Environment { get; } = "Production";
}

string env = AppConfig.Instance.Environment;
```

- `private ctor` + `Lazy<T>` (lazy + thread-safe) + `sealed` (no inheritance = no 2nd instance).
- **Tech-lead note — code review mein challenge karo:** Juniors overuse karte hain. .NET mein hand se rarely likho — DI se register karo:
```csharp
services.AddSingleton<IAppConfig, AppConfig>();
```
Same "one instance" guarantee + testability + honest visible dependencies.

### Lesson 2 recap
| Pattern | One line | Modern C# shortcut |
|---|---|---|
| Factory Method | Ek method decide kare kaunsi class | DI container / keyed services |
| Abstract Factory | Matching *family* create karo | (providers ke liye still useful) |
| Builder | Complex object step-by-step | object initializers + `required` |
| Prototype | Object copy karke naya banao | `record` + `with` |
| Singleton | Exactly one instance | `services.AddSingleton<T>()` |

---

# PART 3 — Structural Patterns

> Objects/classes ko bade structures mein **assemble** karna, sab flexible rakhte hue. Creational = LEGO bricks banana; Structural = unhe smart ways mein snap karna.

## 3.1 Adapter

- **Intent:** Wrapper jo ek interface ko doosre mein **translate** karta hai, taaki do incompatible cheezein saath chalein.
- **Analogy:** Travel power plug adapter.
- **When:** Third-party/legacy library use karni ho jise edit nahi kar sakte. **Avoid:** dono sides control karte ho (directly match karo).

```csharp
public interface ILogger { void Log(string message); }

// Third-party — edit nahi kar sakte
public class FancyLogLibrary { public void WriteEntry(int severity, string text) { } }

public class FancyLogAdapter : ILogger   // aapka wanted interface
{
    private readonly FancyLogLibrary _library;
    public FancyLogAdapter(FancyLogLibrary library) => _library = library;
    public void Log(string message) => _library.WriteEntry(1, message); // translate
}

ILogger logger = new FancyLogAdapter(new FancyLogLibrary());
logger.Log("Hello");
```

- **Tech-lead note:** **Vendor lock-in** ke against main defense. Third-party SDKs ko system ke edges pe apne interfaces ke peeche daalo; core logic sirf *aapke* contracts pe depend kare.

## 3.2 Decorator ⭐

- **Intent:** Object mein naya behavior add karo use **doosre object mein wrap** karke jo **same interface** share karta hai — original class touch kiye bina.
- **Analogy:** Coffee add-ons — plain coffee → +milk → +caramel; result still coffee. Wrappers kisi bhi combo mein stack.
- **When:** Features add/remove flexibly (caching, logging) SRP + OCP honor karte hue. **Avoid:** deep stacks debug karna hard, ordering bugs easy.

```csharp
public interface IProductRepository { Product Get(int id); }
public class SqlProductRepository : IProductRepository
{ public Product Get(int id) { Console.WriteLine("DB..."); return new Product(); } }

public class CachingProductRepository : IProductRepository
{
    private readonly IProductRepository _inner;
    private readonly Dictionary<int, Product> _cache = new();
    public CachingProductRepository(IProductRepository inner) => _inner = inner;
    public Product Get(int id)
    {
        if (_cache.TryGetValue(id, out var c)) return c;
        var p = _inner.Get(id); _cache[id] = p; return p;
    }
}

public class LoggingProductRepository : IProductRepository
{
    private readonly IProductRepository _inner;
    public LoggingProductRepository(IProductRepository inner) => _inner = inner;
    public Product Get(int id)
    {
        Console.WriteLine($"Getting {id}");
        var r = _inner.Get(id);
        Console.WriteLine($"Got {id}");
        return r;
    }
}

// Stack like coffee add-ons:
IProductRepository repo =
    new LoggingProductRepository(
        new CachingProductRepository(
            new SqlProductRepository()));
```

- Har wrapper same interface implement karta hai → endlessly stackable. **Ordering matters:** logging-outside-caching har call log karta hai; caching-outside-logging sirf misses log karega.
- **Tech-lead note — day-to-day C# ka sabse useful pattern:** ASP.NET Core middleware, `HttpClient` `DelegatingHandler`s, aur **Scrutor** (`services.Decorate<IProductRepository, CachingProductRepository>();`). Reviews mein wrapper **ordering** scrutinize karo.

## 3.3 Facade

- **Intent:** Single simple class jo complicated subsystem ko ek easy method ke peeche hide kare.
- **Analogy:** Restaurant waiter — "I'll have the steak," kitchen/grill/plating tumse baat nahi karte.
- **When:** Callers ko simplify karna. **Avoid:** facade "god object" ban jaaye — coordinator rakho, business logic dumping ground nahi.

```csharp
public class CheckoutFacade
{
    private readonly InventoryService _inventory;
    private readonly PaymentService _payment;
    private readonly ShippingService _shipping;
    public CheckoutFacade(InventoryService inv, PaymentService pay, ShippingService ship)
        => (_inventory, _payment, _shipping) = (inv, pay, ship);

    public bool Checkout(Cart cart)   // ek simple front door
    {
        if (!_inventory.Reserve(cart)) return false;
        var receipt = _payment.Charge(cart.Total);
        _shipping.Schedule(cart, receipt);
        return true;
    }
}
// caller: bool ok = checkout.Checkout(cart);
```

- **Tech-lead note:** Most apps ki "service"/"application" layer basically facades ka set hai. Rule: facade **orchestrate** kare (order mein call), deep business logic khud **implement** na kare.

## 3.4 Proxy

- **Intent:** Stand-in object jo real object jaisa dikhta hai lekin uske **access ko control** karta hai (security, lazy-loading, caching).
- **Analogy:** Celebrity ka personal assistant — pehle check karta hai, phir message forward karta hai.

```csharp
public interface IReportService { string Generate(int reportId); }
public class RealReportService : IReportService
{ public string Generate(int id) => $"Report #{id} data..."; }

public class ReportServiceProxy : IReportService
{
    private readonly IReportService _real;
    private readonly IUserContext _user;
    public ReportServiceProxy(IReportService real, IUserContext user)
        => (_real, _user) = (real, user);

    public string Generate(int id)
    {
        if (!_user.HasPermission("reports.read"))
            throw new UnauthorizedAccessException();  // access control...
        return _real.Generate(id);                     // ...then delegate
    }
}
```

- **🔁 Proxy vs Decorator (same shape!):** Difference **intent** mein — Decorator *behavior/features add* karta hai; Proxy *access control* karta hai.
- **Tech-lead note:** EF Core lazy-loading proxies aur mocking libraries (Moq, NSubstitute) ke dynamic proxies yehi pattern hain.

## 3.5 Composite

- **Intent:** Objects ke **tree** (whole + parts) ko single object jaise treat karo.
- **Analogy:** Folders — folder mein files *aur* sub-folders; "size?" recursively add ho jaata hai.

```csharp
public interface IFileSystemItem { long GetSizeBytes(); }

public class FileItem : IFileSystemItem   // leaf
{
    private readonly long _size;
    public FileItem(long size) => _size = size;
    public long GetSizeBytes() => _size;
}

public class FolderItem : IFileSystemItem  // composite
{
    private readonly List<IFileSystemItem> _children = new();
    public void Add(IFileSystemItem item) => _children.Add(item);
    public long GetSizeBytes() => _children.Sum(c => c.GetSizeBytes()); // recursion
}
```

- Key line `_children.Sum(c => c.GetSizeBytes())` — child file ho ya folder, dono `IFileSystemItem`; logic ek baar likha, kisi bhi depth pe chalta hai.
- **When:** Tree structures (menus, org charts, UI, file systems). **Avoid:** data really tree na ho.
- **Tech-lead note:** **Visitor** pattern ke saath naturally pairs (same tree pe many operations).

## 3.6 Bridge

- **Intent:** Jab design **do independent dimensions** ke along vary kare, unhe do separate hierarchies mein split karo taaki multiply hoke mess na banein.
- **Analogy:** TV aur remote — har TV brand ke liye alag remote nahi banate; standard contract se baat karte hain.
- **Problem:** Shapes (Circle, Square) × renderers (vector, raster) = `VectorCircle`, `RasterCircle`, ... class explosion.

```csharp
public interface IRenderer { void DrawCircle(float radius); }
public class VectorRenderer : IRenderer { public void DrawCircle(float r) => Console.WriteLine($"Vector r={r}"); }
public class RasterRenderer : IRenderer { public void DrawCircle(float r) => Console.WriteLine($"Pixels r={r}"); }

public abstract class Shape
{
    protected readonly IRenderer Renderer;  // "bridge" to other dimension
    protected Shape(IRenderer renderer) => Renderer = renderer;
    public abstract void Draw();
}

public class Circle : Shape
{
    private readonly float _radius;
    public Circle(IRenderer renderer, float radius) : base(renderer) => _radius = radius;
    public override void Draw() => Renderer.DrawCircle(_radius);
}

new Circle(new VectorRenderer(), 5).Draw();
new Circle(new RasterRenderer(), 5).Draw();
// naya Shape YA naya Renderer = ek new class, per-combination nahi
```

- **🔁 Bridge vs Adapter:** Adapter mismatch ko *after the fact* fix karta hai (already existing cheezein); Bridge *designed up front* hota hai do dimensions separate rakhne ke liye.
- **Avoid:** really sirf ek dimension vary kare (needless indirection).

## 3.7 Flyweight (rare — skim)

- **Intent:** *Huge* number of similar objects ke common parts **share** karo memory bachane ke liye.
- **Analogy:** Book ke letters — "e" ki definition ek baar, positions many.
- **Idea:** State split — **Intrinsic** (shared, unchanging: tree ka texture/model) vs **Extrinsic** (per-object unique: x/y position). Intrinsic ek copy share, extrinsic per-object. 1M trees = model ek baar.
- **When:** Sirf genuinely millions of objects + real memory problem. Otherwise premature optimization — skip.
- **Tech-lead note:** Everyday C# mein string interning, cached immutable value objects, `ArrayPool<T>`.

### Lesson 3 recap
| Pattern | Ek line | Aap already use karte ho |
|---|---|---|
| Adapter | Interface translate karo | Kisi bhi third-party SDK ko wrap |
| **Decorator** ⭐ | Wrapping se features add | ASP.NET middleware, HttpClient handlers |
| Facade | Complexity pe simple front door | Service/application layer |
| Proxy | Stand-in jo access control kare | EF Core lazy loading, mocking libs |
| Composite | Tree ko single object jaise | File systems, menus, UI trees |
| Bridge | Do independent dimensions separate | (up front design) |
| Flyweight | Objects ke across data share | String interning, `ArrayPool<T>` |

> **Confusing trio:** Adapter (*mismatch fix*), Decorator (*behavior add*), Proxy (*access control*) — teeno wrap karte hain, difference **kyun** mein.

---

# PART 4 — Behavioral Patterns

> Objects kaise behave karte hain aur ek dusre se baat karte hain. Sabse bada + useful group. **Core four master karo: Strategy, Observer, Command.**

## 4.1 Strategy ⭐

- **Intent:** Har algorithm apni class mein (ek interface share), aur **runtime par swap** karo ki kaunsa use karna hai.
- **Analogy:** Airport jaana — drive/taxi/train; goal same, strategy choose.
- **Strategy = "OCP, packaged."** Yeh Lesson 1 ke OCP example wala exact idea hai.

```csharp
public interface IShippingStrategy { decimal Calculate(Order order); }
public class StandardShipping : IShippingStrategy { public decimal Calculate(Order o) => o.Weight * 1.5m; }
public class ExpressShipping  : IShippingStrategy { public decimal Calculate(Order o) => o.Weight * 3.0m + 10; }

public class ShippingCalculator
{
    private readonly IShippingStrategy _strategy;
    public ShippingCalculator(IShippingStrategy strategy) => _strategy = strategy; // injected
    public decimal GetCost(Order order) => _strategy.Calculate(order);
}

var calc = new ShippingCalculator(new ExpressShipping());
// "drone shipping" = ek new class, koi existing code nahi badalta
```

- **Avoid:** hamesha ek hi algorithm ho, ya differences one-liner (simple `if` theek).
- **Tech-lead note:** C# mein strategy ek **delegate** (`Func<Order, decimal>`) jitna lightweight ho sakta hai. .NET 8 **keyed DI** se strategies key se resolve. Workhorse pattern.

## 4.2 Observer ⭐

- **Intent:** Jab object change ho, woh automatically **interested objects ki list ko notify** kare — bina jaane woh kaun hain.
- **Analogy:** YouTube channel — creator "publish" karta hai, sab subscribers ping. Kabhi bhi join/leave.
- **Idiomatic C#:** `event` language mein built-in hai.

```csharp
public class Stock
{
    public event Action<decimal>? PriceChanged;   // built-in subscriber list
    private decimal _price;
    public decimal Price
    {
        get => _price;
        set { _price = value; PriceChanged?.Invoke(value); }  // sabko notify
    }
}

var stock = new Stock();
stock.PriceChanged += p => Console.WriteLine($"UI update: {p}");
stock.PriceChanged += p => Console.WriteLine($"Log: {p}");
stock.Price = 99.5m;  // DONO subscribers fire
```

- `+=` subscribe, `-=` unsubscribe. `?.Invoke` = "sirf agar koi subscriber ho." Publisher ko pata nahi kaun sun raha — total decoupling.
- **Avoid / ⚠️:** **Memory leaks** — long-lived object subscribe kare aur kabhi `-=` na kare to GC nahi hoga. Genuine production bug. Subscriber order/re-entrancy bhi surprise.
- **Tech-lead note:** `event`/`Action`, `IObservable<T>` (Rx), `INotifyPropertyChanged` sab Observer hain. Reviews mein **missing `-=` unsubscribes** hunt karo.

## 4.3 Command ⭐

- **Intent:** Action (+ uska data) ko apni **object** mein wrap karo — store, queue, pass, log, ya undo karne ke liye.
- **Analogy:** Restaurant order ticket — ek *thing* jise queue/hand/log/cancel kar sakte ho.

```csharp
public interface ICommand { void Execute(); void Undo(); }

public class AddTextCommand : ICommand
{
    private readonly Document _doc;
    private readonly string _text;
    public AddTextCommand(Document doc, string text) => (_doc, _text) = (doc, text);
    public void Execute() => _doc.Append(_text);
    public void Undo()    => _doc.RemoveLast(_text.Length);
}

public class Editor
{
    private readonly Stack<ICommand> _history = new();
    public void Run(ICommand command) { command.Execute(); _history.Push(command); }
    public void Undo() { if (_history.Count > 0) _history.Pop().Undo(); }
}
```

- Har command khud jaanta hai do + undo + apna data carry karta hai. `Editor` ko command ka content pata nahi — sirf `Execute()` + stack. Objects hone se queue/log bhi ho sakte hain.
- **Avoid:** in abilities mein se kuch na chahiye (tab extra classes fizool).
- **Tech-lead note:** **MediatR** = Command pattern (Request + Handler). .NET mein **CQRS** ka backbone.

## 4.4 Template Method

- **Intent:** Base class process ka **fixed skeleton** define kare, specific steps subclasses ke liye blanks chhode.
- **Analogy:** Hot drinks recipe — boil → add ingredient → pour → condiments; tea/coffee sirf specific steps fill.

```csharp
public abstract class DataImporter
{
    public void Import(string path)   // template method — NOT virtual, order locked
    {
        var raw     = ReadFile(path);
        var records = Parse(raw);       // varies
        Validate(records);
        Save(records);
    }
    protected string ReadFile(string path) => "raw data";       // shared
    protected abstract List<string> Parse(string raw);           // subclass MUST fill
    protected virtual void Validate(List<string> records) { }    // optional hook
    protected void Save(List<string> records) => Console.WriteLine($"Saved {records.Count}");
}

public class CsvImporter : DataImporter
{
    protected override List<string> Parse(string raw) => raw.Split(',').ToList();
}
```

- `Import` order fix karta hai (never changes). `abstract Parse` = must fill; `virtual Validate` = optional hook. Subclasses reorder nahi kar sakte.
- **Avoid:** inheritance rigid hai — runtime pe steps swap chahiye to **Strategy** (composition) prefer karo.
- **Tech-lead note:** Template Method (inheritance) vs Strategy (composition) similar problem. "Composition over inheritance" — Strategy le lo jab tak skeleton genuinely stable/simple na ho.

## 4.5 State

- **Intent:** Object apna **behavior** change kare jab uska internal **state** change ho — jaise different class mein switch ho gaya.
- **Analogy:** Traffic light — Red apne rules + jaanta hai next Green; Green apne rules + next Yellow.
- **Problem:** `switch(status)` blocks har method (Ship/Cancel/Refund) mein scatter.

```csharp
public interface IOrderState { string Name { get; } IOrderState Next(); }

public class PendingState : IOrderState { public string Name => "Pending"; public IOrderState Next() => new PaidState(); }
public class PaidState    : IOrderState { public string Name => "Paid";    public IOrderState Next() => new ShippedState(); }
public class ShippedState : IOrderState { public string Name => "Shipped"; public IOrderState Next() => this; } // terminal

public class Order
{
    private IOrderState _state = new PendingState();
    public string Status => _state.Name;
    public void Advance() => _state = _state.Next();
}
```

- Har state apne rules + transition own karti hai; `Order` delegate karta hai. Scattered `if (Status == ...)` gayab.
- **🔁 State vs Strategy:** Strategy — *aap* bahar se algorithm pick karte ho, strategies ek dusre ko nahi jaanti. State — object khud internally switch karta hai, aur states aksar next state jaanti hain.
- **Avoid:** sirf 2 simple states (boolean theek). Complex machines: **Stateless** library.

## 4.6 Chain of Responsibility

- **Intent:** Request ko handlers ki **line** se pass karo; har handler ya handle kare ya next ko pass kare.
- **Analogy:** Customer support tiers — Tier 1 → 2 → 3 escalate.

```csharp
public abstract class Handler
{
    protected Handler? Next;
    public Handler SetNext(Handler next) { Next = next; return next; }
    public abstract void Handle(Request request);
}

public class AuthHandler : Handler
{
    public override void Handle(Request r)
    {
        if (!r.IsAuthenticated) { Console.WriteLine("Rejected: not auth"); return; }
        Next?.Handle(r);
    }
}
public class ValidationHandler : Handler
{
    public override void Handle(Request r)
    {
        if (string.IsNullOrEmpty(r.Body)) { Console.WriteLine("Rejected: empty"); return; }
        Next?.Handle(r);
    }
}

var auth = new AuthHandler();
auth.SetNext(new ValidationHandler());  // auth → validation
auth.Handle(myRequest);
```

- Har handler apna bit + `Next?.Handle(r)` — jab tak stop na kare. Handlers add/remove/reorder easily.
- **Avoid:** request end tak unhandled reh sakti hai; long chains trace karna hard.
- **Tech-lead note:** **ASP.NET Core middleware** aur `HttpClient` `DelegatingHandler` pipeline exactly yeh.

## 4.7 Mediator

- **Intent:** Objects directly (tangled web) baat karne ke bajaye ek central **hub** se baat karein.
- **Analogy:** Air traffic control — planes directly radio nahi karte, control tower coordinate karta hai.

```csharp
public interface IDialogMediator { void Notify(object sender, string ev); }

public class RegistrationDialog : IDialogMediator
{
    public Button SubmitButton { get; set; } = new();
    public Checkbox AgreeTerms { get; set; } = new();
    public void Notify(object sender, string ev)
    {
        if (sender == AgreeTerms && ev == "toggled")
            SubmitButton.Enabled = AgreeTerms.Checked;  // ek jagah coordination
    }
}
// components sirf mediator.Notify(this, "toggled") call karte hain
```

- **Avoid:** mediator "god object" ban sakta hai. Trivial calls route mat karo.
- **Tech-lead note:** **MediatR** (Mediator + Command). Controllers ko handlers se decouple karta hai — teams over-apply karti hain, simple ops mein indirection. Review mein trade-off weigh karo.

## 4.8 Iterator (C# free mein deta hai)

- **Intent:** Collection ke items ek-ek step karo **bina jaane collection internally kaise store karta hai.**
- **Analogy:** TV remote ka "next channel."
- **C# reality:** `foreach`, `IEnumerable<T>`, `yield return` — hand se almost never likhte.

```csharp
public IEnumerable<int> FirstThreeEvens()
{
    yield return 0;   // ek time par ek item, beech mein pause
    yield return 2;
    yield return 4;
}
foreach (var n in FirstThreeEvens()) Console.WriteLine(n);
```

- `yield return` hidden state machine banata hai; items lazily produce. LINQ isi pe built.
- **Tech-lead note:** `IEnumerable<T>` **lazy** hai (enumerate na karo to kuch nahi chalta); **do baar enumerate = kaam do baar.** Reviews mein dhyan se dekho.

## 4.9 Visitor

- **Intent:** Objects ke group mein **new operations** add karo **unki classes modify kiye bina.**
- **Analogy:** Tax auditor jo different businesses visit karta hai; agle month safety inspector bhejo bina businesses change kiye.
- **Trick:** *Double dispatch* — har element ka `Accept(visitor)` jo visitor ko back call karta hai.

```csharp
public interface IShapeVisitor { void Visit(Circle c); void Visit(Square s); }
public interface IShape { void Accept(IShapeVisitor visitor); }

public class Circle : IShape
{
    public float Radius = 5;
    public void Accept(IShapeVisitor v) => v.Visit(this);   // matching Visit
}

public class AreaCalculator : IShapeVisitor  // naya operation = naya visitor; shapes never change
{
    public void Visit(Circle c) => Console.WriteLine(3.14f * c.Radius * c.Radius);
    public void Visit(Square s) => Console.WriteLine("square area...");
}
```

- **Fayda:** element classes touch kiye bina new operations. **Nuksan:** reverse painful — nayi *shape* add = *har* visitor update. Verbose bhi.
- **Tech-lead note — modern C# aksar replace karta hai:** sealed hierarchy pe pattern matching:
```csharp
decimal Area(IShape shape) => shape switch
{
    Circle c => 3.14m * (decimal)(c.Radius * c.Radius),
    Square s => 0m,
    _        => throw new ArgumentException("Unknown shape")
};
```
Classic Visitor mainly complex stable structures (compiler ASTs) ke liye.

## 4.10 Memento

- **Intent:** Object ki current state capture karo taaki **baad mein restore** kar sako — private internals expose kiye bina.
- **Analogy:** Video game save point.

```csharp
public record EditorSnapshot(string Content);   // immutable snapshot (memento)

public class TextEditor
{
    public string Content { get; private set; } = "";
    public void Type(string text) => Content += text;
    public EditorSnapshot Save() => new(Content);
    public void Restore(EditorSnapshot s) => Content = s.Content;
}
```

- **When:** Undo/redo, checkpoints, "cancel changes." Aksar **Command** (4.3) ke saath paired.
- **Tech-lead note:** `record` types mementos cheap banate hain (immutable). Yaad rakho `record` copies shallow — nested mutable objects still shared.

## 4.11 Interpreter (rare — sirf aware raho)

- **Intent:** Chhota "language" (grammar) define karo + usme likhi sentences evaluate karo.
- **Honest advice:** Hand se almost never banate. Mini-language/expression parse karna ho to library use karo (**ANTLR**, **Sprache**, **Superpower**) ya C# `Expression` trees. Bas naam recognize karo.

### Lesson 4 recap
| Pattern | One line | Real C# |
|---|---|---|
| **Strategy** ⭐ | Runtime pe algorithm swap | `Func<>`, keyed DI |
| **Observer** ⭐ | Ek change → bahut ko notify | `event`, `IObservable<T>`, Rx |
| **Command** ⭐ | Action ko object mein | MediatR, CQRS, undo/redo |
| Template Method | Fixed skeleton, fill-in steps | Base-class workflows |
| State | State ke saath behavior change | Stateless library |
| Chain of Responsibility | Request ko line se pass | ASP.NET middleware |
| Mediator | Central hub, no direct talk | MediatR |
| Iterator | Collection step karo | `foreach`, `yield`, LINQ |
| Visitor | Classes edit bina new operations | Often replaced by pattern matching |
| Memento | State save & restore | `record` snapshots + Command |
| Interpreter | Mini-language evaluate | Parser library instead |

> **Same shape, different intent:** Strategy (aap choose) vs State (object khud switch); Command (action-as-object) vs Strategy (algorithm-as-object).

---

# PART 5 — Modern C#, Tech-Lead Lens aur Aage Kya

## Part A — Patterns jo modern C# free mein deta hai

> **Rule:** agar framework already clearly kehta hai, framework use karo. Manual pattern tab jab genuinely clarity/capability add kare. Built-in ke hote hue textbook pattern = review mein red flag.

| Classic pattern | Modern C#/.NET mein iski jagah |
|---|---|
| **Singleton** | `services.AddSingleton<IThing, Thing>();` |
| **Factory / Abstract Factory** | DI container, `Func<T>`, keyed services (.NET 8+) |
| **Strategy** | `Func<>` delegate, ya keyed DI |
| **Observer** | `event`/`Action`, `IObservable<T>` (Rx), `INotifyPropertyChanged`, channels |
| **Command + Mediator** | **MediatR** (requests + handlers) |
| **Chain of Responsibility** | ASP.NET Core middleware; `DelegatingHandler`s |
| **Decorator** | **Scrutor** (`.Decorate<T>()`); middleware |
| **Iterator** | `IEnumerable<T>` + `yield return`; LINQ |
| **Prototype / Memento** | `record` + `with` |
| **Visitor** | Sealed hierarchy pe `switch` + pattern matching |
| **Builder** | Object initializers + `required`; `with` |

```csharp
// records — immutable data + built-in copy (Prototype/Memento free)
public record Person(string Name, int Age);
var b = a with { Age = 37 };        // copy-and-change; 'a' untouched

// required members — safe construction without Builder
public class Config { public required string ApiKey { get; init; } }
var c = new Config { ApiKey = "abc" };

// pattern matching — replaces many Visitor/Strategy switches
string Describe(object o) => o switch
{
    int n when n < 0 => "negative number",
    int          => "number",
    string s     => $"text of length {s.Length}",
    null         => "nothing",
    _            => "something else"
};

// DI — container jo quietly Factory + Singleton karta hai
services.AddSingleton<IClock, SystemClock>();      // one shared instance
services.AddScoped<IOrderRepo, SqlOrderRepo>();    // one per web request
services.AddTransient<IEmailSender, SmtpSender>(); // fresh har baar
```

## Part B — Tech-Lead lens

1. **Pattern names ke bajaye *forces* ki baat karo.** ❌ "Yahan Strategy use karo." ✅ "Har naya payment type isi `switch` ko edit karta hai, existing break hone ka risk — kya har type apni class bana sakte hain?" Problem ko naam do, pattern khud follow karega.
2. **Over-engineering se ladho (#1 job).** Har abstraction pe poochho: *"Yeh kaunsa concrete, likely change cheaper banata hai — aur woh change aa raha hai?"* Agar "just in case" → **YAGNI violation**. Best code = simplest code jo real problem solve kare.
3. **"Same shape, different intent" pairs clear rakho:**

| Pair | Farak |
|---|---|
| Strategy vs State | Strategy: *aap* algorithm pick. State: object khud switch + next state jaanta |
| Adapter vs Bridge | Adapter: existing mismatch fix. Bridge: up-front do dimensions separate |
| Decorator vs Proxy | Decorator: behavior *add*. Proxy: access *control* |
| Factory Method vs Abstract Factory | Ek product vs matching *family* |
| Command vs Strategy | Command: *action* (undo/queue). Strategy: *algorithm* |

4. **Pattern-shaped tech debt flag karo:** Leaked Observer subscriptions (`+=` bina `-=` → leak); God-object Facades/Mediators; hand-rolled Singletons (DI hone chahiye); Decorator/middleware ordering bugs; speculative Abstract Factories / single-impl interfaces "for flexibility."
5. **LLD ko architecture se connect karo:** Clean/Hexagonal = DIP system scale par (boundaries pe Adapters); DDD tactical (Repository, Aggregate, Value Object) inhi pe build; CQRS = app layer pe Command + Mediator.

## Part C — GoF se aage: enterprise patterns

| Pattern | Kya karta hai | Dhyan |
|---|---|---|
| **Repository** | Data access ko interface (`IOrderRepository`) ke peeche abstract | EF Core ko bina wajah thin repo mein wrap mat karo — EF already repository/unit-of-work hai |
| **Unit of Work** | Multiple changes ek commit/transaction mein group | EF Core `DbContext` already ek hai |
| **Options pattern** | `IOptions<T>` se strongly-typed config | Settings read karne ka standard .NET way |
| **Result / Either** | Exception ke bajaye success-or-error ko value return | Expected failures (validation) ke liye; truly exceptional cases ke liye exceptions |
| **Specification** | Business rule / query filter reusable combinable object | Complex reused query logic |
| **Null Object** | `null` ke bajaye "do-nothing" impl (e.g. `NullLogger`) | Null-checks hata deta hai |
| **CQRS** | Read model ko write model se separate | Powerful but complex — simple CRUD pe mat lagao |

## Part D — Roadmap

1. **Foundations cement karo:** Lesson 1 dobara jab tak interfaces + composition + DI natural na lagein. Ek line: *"interfaces par depend karo, jo chahiye woh inject karo."*
2. **"Core four" haath se banao (memory se, no copy-paste):** Strategy (payment/shipping), Observer (`event`), Decorator (caching + logging), Factory Method. Yeh ~80% real use cover karte hain.
3. **Wild mein recognize karo:** middleware (CoR), `HttpClient` handlers (Decorator/Chain), MediatR (Command/Mediator), DI registration (Factory/Singleton).
4. **Tech-lead muscle:** Har review mein 2 questions — *"Isse nahi change karte to kaunsa SOLID violate hoga?"* (add justify) aur *"Kya yeh abstraction apna keep earn kar rahi hai ya YAGNI?"* (remove justify).
5. **Architecture tak level up:** Clean Architecture, DDD, CQRS — same principles, larger scale.

## Master resources
- **Video:** Christopher Okhravi (clearest conceptual), Nick Chapsas (modern C#, kab patterns obsolete), Amichai Mantinband (patterns + clean arch), Derek Banas (fast overview).
- **Written:** Refactoring Guru (`refactoring.guru/design-patterns/csharp`), DoFactory (.NET), *Head First Design Patterns* (friendliest), *Dependency Injection Principles, Practices, and Patterns* (Seemann & van Deursen — "DI replaces half the patterns" click karti hai).

> **Final takeaway:** Aap 23 classic patterns, modern C# unhe kaise reshape karta hai, aur — sabse important — **kab unhe use NAHI karna** samajhte ho. Yehi judgment senior engineer ko list-memorizer se alag karta hai. Ab "core four" haath se banao. 🚀
