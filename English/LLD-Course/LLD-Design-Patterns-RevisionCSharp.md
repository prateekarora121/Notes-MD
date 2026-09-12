# LLD Design Patterns (C#) — Revision Notes

> Quick-revision notes derived from the 5-part LLD course. Covers every part: Foundations (OOP + SOLID) → Creational → Structural → Behavioral → Modern C# & Next Steps. These notes are meant to let you brush up each pattern without reopening the course.

---

# Part 1 — Foundations (OOP + SOLID)

> The most important part. **Every pattern later is just SOLID applied to a specific situation.** Master this and patterns feel obvious.

## A — OOP refresher

**Q: Class vs object?**
A: A **class** is a blueprint; an **object** is a real instance built from it with `new`.

```csharp
public class Dog
{
    public string Name { get; set; }
    public void Bark() => Console.WriteLine($"{Name} says Woof!");
}
Dog rex = new Dog();  // an instance
rex.Name = "Rex";
rex.Bark();
```

**Q: What is an interface, and why is it the single most important idea?**
A: A **contract** — a list of methods a class *promises* to provide, with **no code**. Like a job description. Because many classes can fulfill the same contract, code that depends on the contract can accept any of them.

```csharp
public interface INotifier { void Send(string message); }
public class EmailNotifier : INotifier { public void Send(string m) => Console.WriteLine($"Email: {m}"); }
public class SmsNotifier   : INotifier { public void Send(string m) => Console.WriteLine($"SMS: {m}"); }

INotifier notifier = new EmailNotifier(); // swap to SmsNotifier without changing callers
notifier.Send("Your order shipped!");
```
🔑 **"Program to an interface, not an implementation."** The variable's *type* is the contract, not the concrete class. This habit underlies almost every pattern.

**Q: Inheritance vs Composition — and why prefer composition?**
A: Inheritance = **"is-a"** (`Dog : Animal`). Composition = **"has-a"** (a `Car` holds an `Engine`). Prefer composition: inheritance creates rigid family trees that hurt to change later; composition snaps parts together like LEGO and rearranges freely.

```csharp
public class Engine { public void Start() => Console.WriteLine("Vroom"); }
public class Car
{
    private readonly Engine _engine = new Engine();  // HAS-A
    public void StartCar() => _engine.Start();
}
```

**Q: What is Dependency Injection (DI)?**
A: Instead of a class *building* what it needs, you *hand it in* (usually via constructor). The constructor becomes the class's honest "shopping list."

```csharp
public class OrderService
{
    private readonly INotifier _notifier;
    public OrderService(INotifier notifier) => _notifier = notifier; // injected
    public void Place() => _notifier.Send("Order placed");
}
var service = new OrderService(new SmsNotifier()); // caller decides; inject a fake in tests
```
Why: **flexible** (swap implementations), **testable** (inject fakes), **honest** (dependencies visible in the constructor).

## B — SOLID (5 rules of good class design)

**S — Single Responsibility (SRP):** *A class should do one job → one reason to change.*
```csharp
// BAD: Order does business logic + DB + email
// GOOD:
public class Order           { public decimal CalculateTotal() => 0m; }
public class OrderRepository { public void Save(Order o) { /* SQL */ } }
public class EmailService    { public void SendConfirmation(Order o) { /* SMTP */ } }
```

**O — Open/Closed (OCP):** *Open to extension, closed to modification.* Add behavior via new code, not by editing working code. Smell: a growing `if`/`switch` chain.
```csharp
// GOOD (this is literally the Strategy pattern):
public interface IShipping { decimal Cost(decimal weight); }
public class Standard : IShipping { public decimal Cost(decimal w) => w * 1.5m; }
public class Express  : IShipping { public decimal Cost(decimal w) => w * 3.0m; }
// Add Overnight = add a new class; nothing else changes.
```

**L — Liskov Substitution (LSP):** *A subtype must work anywhere the base type is expected, with no nasty surprises.*
```csharp
// BAD: Penguin : Bird overrides Fly() to throw → breaks any code calling Bird.Fly()
```
Fix: don't force `Fly()` on all birds; model honestly (e.g. a separate `IFlyingBird`).

**I — Interface Segregation (ISP):** *Prefer many small interfaces over one fat one; don't force unused methods.*
```csharp
// BAD: IMachine { Print(); Scan(); Fax(); } forces a SimplePrinter to implement Scan/Fax.
// GOOD:
public interface IPrinter { void Print(); }
public interface IScanner { void Scan(); }
public class SimplePrinter : IPrinter { public void Print() { } }
```

**D — Dependency Inversion (DIP):** *Depend on abstractions, not concretes.* The formal name for the "program to an interface" habit + DI.
```csharp
public interface IDataSource { string[] GetRows(); }
public class ReportGenerator
{
    private readonly IDataSource _source;
    public ReportGenerator(IDataSource source) => _source = source; // SQL, file, or mock
}
```
> DIP + interface habit + DI are three views of the same idea: **keep important code from being glued to replaceable details.**

## C — The three "keep it sane" rules

- **KISS** — Keep It Simple. The simplest thing that works is usually right.
- **YAGNI** — You Aren't Gonna Need It. Don't add flexibility "just in case." Speculative patterns are the #1 cause of over-engineering.
- **DRY** — Don't Repeat Yourself. But extract shared code only once you actually see repetition (rule of thumb: the 3rd time). Extracting too early creates the *wrong* abstraction.

**Takeaways:** interfaces enable swapping; composition + DI enable LEGO-style assembly and easy testing; SOLID tells you when a design is getting rigid; KISS/YAGNI/DRY stop over-application. When reading any pattern, ask: *"Which SOLID rule is this protecting?"*

---

# Part 2 — Creational Patterns

**All about *how objects get created*.** Scattered `new SomeClass()` glues code to concrete classes (breaks DIP).

## 2.1 Factory Method
- **Intent:** Call a *method* whose job is to create/return the object, so the "which class to build" decision lives in one place.
- **Analogy:** Pizza shop — you order "one Margherita"; the shop decides which chef/recipe.
- **When:** You have multiple concrete types chosen at runtime and scattered `new`/`if` chains. **Avoid** when there's only one concrete type (just use `new`).

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
        => _factory.Create(format).Export(data);   // caller doesn't know concrete classes
}
```
- **Trade-offs:** Removes scattered `new`; one place to change; honors OCP; callers depend on the interface.
- **Classic vs practical:** The textbook version uses inheritance (base class with abstract `Create()`); the factory-class-with-a-method version above is the common, practical C# form.
- **Modern C#:** The **DI container is itself a factory** (`services.AddScoped<IExporter, PdfExporter>()`). .NET 8 **keyed services** register several implementations under keys and resolve by key.

## 2.2 Abstract Factory
- **Intent:** A factory that creates a whole **family of related objects** meant to be used together.
- **Analogy:** IKEA sets — pick "Scandinavian" and chair/table/lamp all match.
- **When:** You must keep a set of products consistent (e.g. themed UI). **Avoid** with only one family or unrelated products.

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

IThemeFactory factory = userPrefersDark ? new DarkThemeFactory() : new LightThemeFactory();
factory.CreateButton().Render();    // guaranteed to match...
factory.CreateCheckbox().Render();  // ...this one
```
- **Trade-offs:** Guarantees a consistent family; swap the whole set by swapping one factory. **Downside:** adding a new *product type* (e.g. `ISlider`) forces edits to *every* factory (not OCP on that axis).
- **Factory Method vs Abstract Factory:** Factory Method = **one** product; Abstract Factory = a **family**.
- **Realistic use:** Provider abstraction (matching AWS vs Azure clients: blob + queue + secrets).

## 2.3 Builder
- **Intent:** Build a complex object **step by step** with named, chainable steps instead of a monster constructor.
- **Analogy:** Custom Subway sandwich — one clear step at a time.
- **Problem it solves:** The *telescoping constructor* — `new Pizza("Large", true, false, true, ...)` is unreadable.

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
    public Pizza Build() => _pizza;   // good place to validate
}

Pizza pizza = new PizzaBuilder().Size("Large").Crust("thin").AddCheese().AddMushrooms().Build();
```
- **Key idea:** each step returns `this` → enables chaining = a **fluent interface**. `Build()` returns the finished object and is the ideal place to *validate*.
- **Trade-offs:** Very readable; optional steps obvious; can validate; can produce immutable objects. **Avoid** for simple objects.
- **Modern C#:** Object initializers + `required` members often remove the need: `new Pizza { Size = "Large", Cheese = true }`. Keep a real builder for **ordering rules/validation** or multi-step construction. (You've used `WebApplication.CreateBuilder(args)`.)

## 2.4 Prototype
- **Intent:** Create a new object by **copying an existing one** rather than building from scratch.
- **Analogy:** Photocopying a filled-in form, then tweaking a few fields.
- **When:** Objects are expensive/tedious to set up and you need many near-identical ones.

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
        Recipients = new List<string>(this.Recipients) // deep-copy the list!
    };
}
var welcome = new EmailTemplate { Subject = "Welcome!", Body = "Hello." };
var forBob = welcome.Clone();
forBob.Recipients.Add("bob@x.com");   // tweak only what differs
```
- ⚠️ **Shallow vs deep copy** — the classic bug. `Recipients = this.Recipients` would share the same list (shallow); `new List<>(...)` copies it (deep). Know which you want.
- **Trade-offs:** Skips expensive setup. **Avoid** for cheap objects or complex nested references.
- **Modern C#:** `record` + `with`:
```csharp
public record Point(int X, int Y);
var a = new Point(1, 2);
var b = a with { Y = 5 };   // b = (1,5), a unchanged — compiler-generated (shallow) prototype
```

## 2.5 Singleton
- **Intent:** Guarantee **exactly one instance** app-wide, with a single access point.
- **Analogy:** The President — only one at a time.

```csharp
public sealed class AppConfig
{
    private static readonly Lazy<AppConfig> _instance = new(() => new AppConfig());
    public static AppConfig Instance => _instance.Value;   // single access point
    private AppConfig() { }                                 // private ctor → nobody else can 'new'
    public string Environment { get; } = "Production";
}
string env = AppConfig.Instance.Environment;
```
- **Key parts:** `private` ctor blocks external `new`; `Lazy<T>` gives lazy + thread-safe creation; `sealed` prevents inheritance creating a second instance.
- ⚠️ **Downsides:** it's **global state** (a hidden dependency), **hard to test** (can't easily swap a fake, parallel tests interfere), and quietly violates DIP.
- **Modern C# — challenge hand-written singletons in review:** register a normal class as a singleton in DI — same "one instance" guarantee **plus** testability and visible dependencies:
```csharp
services.AddSingleton<IAppConfig, AppConfig>();
```

### Lesson 2 recap

| Pattern | In one line | Modern C# shortcut |
|---|---|---|
| Factory Method | A method decides which class to create | DI container / keyed services |
| Abstract Factory | Create a matching *family* of objects | (still useful for providers) |
| Builder | Build a complex object step-by-step | object initializers + `required` |
| Prototype | Make a new object by copying one | `record` + `with` |
| Singleton | Exactly one instance | `services.AddSingleton<T>()` |

---

# Part 3 — Structural Patterns

**About *how you assemble objects/classes into bigger structures* while staying flexible.** (Creational = making the bricks; structural = snapping them together.)

## 3.1 Adapter
- **Intent:** A wrapper that translates one interface into another so incompatible things work together.
- **Analogy:** Travel power-plug adapter.
- **When:** You must use a third-party/legacy class whose methods don't match your interface and you can't change it.

```csharp
public interface ILogger { void Log(string message); }
public class FancyLogLibrary { public void WriteEntry(int severity, string text) { } } // can't edit

public class FancyLogAdapter : ILogger          // looks like what YOUR app wants
{
    private readonly FancyLogLibrary _library;   // holds the adaptee
    public FancyLogAdapter(FancyLogLibrary library) => _library = library;
    public void Log(string message) => _library.WriteEntry(1, message); // translate the call
}
ILogger logger = new FancyLogAdapter(new FancyLogLibrary());
logger.Log("Hello");
```
- **Trade-offs:** Use libraries/legacy without changing them; isolates you from third-party churn. **Avoid** if you control both sides and could just match directly.
- **Tech-lead note:** Main defense against **vendor lock-in** — put SDKs behind your own interfaces at the edges; swap vendors by rewriting one small adapter.

## 3.2 Decorator ⭐
- **Intent:** Add behavior by **wrapping** an object in another that shares the same interface — without touching the original.
- **Analogy:** Coffee add-ons — wrap plain coffee with milk, then caramel; still a coffee, stackable.
- **When:** Add cross-cutting features (caching, logging) without cramming them into the class (SRP) or repeating per class.

```csharp
public interface IProductRepository { Product Get(int id); }
public class SqlProductRepository : IProductRepository
{ public Product Get(int id) { Console.WriteLine("Hitting the database..."); return new Product(); } }

public class CachingProductRepository : IProductRepository
{
    private readonly IProductRepository _inner;
    private readonly Dictionary<int, Product> _cache = new();
    public CachingProductRepository(IProductRepository inner) => _inner = inner;
    public Product Get(int id)
    {
        if (_cache.TryGetValue(id, out var cached)) return cached;
        var product = _inner.Get(id);
        _cache[id] = product;
        return product;
    }
}
public class LoggingProductRepository : IProductRepository
{
    private readonly IProductRepository _inner;
    public LoggingProductRepository(IProductRepository inner) => _inner = inner;
    public Product Get(int id)
    {
        Console.WriteLine($"Getting product {id}");
        var result = _inner.Get(id);
        Console.WriteLine($"Got product {id}");
        return result;
    }
}

// Stack like coffee add-ons:
IProductRepository repo =
    new LoggingProductRepository(
        new CachingProductRepository(
            new SqlProductRepository()));
```
- **Key idea:** each wrapper implements the same interface, holds an `_inner`, adds behavior before/after, and delegates. **Order matters** (logging-outside-caching logs every call; caching-outside-logging only logs misses).
- **Trade-offs:** Add/remove features without editing classes (OCP); mix and match; small single-concern classes (SRP). **Avoid** deep stacks (hard to debug, ordering bugs).
- **Modern C#:** One of the most useful patterns. ASP.NET Core **middleware**, `HttpClient` **`DelegatingHandler`s**, and DI decoration via **Scrutor** (`services.Decorate<IProductRepository, CachingProductRepository>();`).

## 3.3 Facade
- **Intent:** A single simple class hiding a complicated subsystem behind one easy method.
- **Analogy:** Restaurant waiter — you order the steak; kitchen/grill/plating coordinate unseen.
- **When:** Callers keep repeating a fragile multi-subsystem "dance."

```csharp
public class CheckoutFacade
{
    private readonly InventoryService _inventory;
    private readonly PaymentService _payment;
    private readonly ShippingService _shipping;
    public CheckoutFacade(InventoryService inv, PaymentService pay, ShippingService ship)
        => (_inventory, _payment, _shipping) = (inv, pay, ship);

    public bool Checkout(Cart cart)
    {
        if (!_inventory.Reserve(cart)) return false;
        var receipt = _payment.Charge(cart.Total);
        _shipping.Schedule(cart, receipt);
        return true;
    }
}
bool ok = checkout.Checkout(cart);   // one front door
```
- **Trade-offs:** Much simpler for callers; complexity in one place. **Avoid** letting it become a **god object**. Rule: a facade should **orchestrate**, not **implement** deep business logic. (Your service/application layer is basically facades.)

## 3.4 Proxy
- **Intent:** A stand-in that looks identical to the real object but **controls access** (security, lazy-loading, caching, remoting).
- **Analogy:** A celebrity's personal assistant — checks if you're allowed, then passes the message.

```csharp
public interface IReportService { string Generate(int reportId); }
public class RealReportService : IReportService
{ public string Generate(int id) => $"Report #{id} data..."; }

public class ReportServiceProxy : IReportService
{
    private readonly IReportService _real;
    private readonly IUserContext _user;
    public ReportServiceProxy(IReportService real, IUserContext user) => (_real, _user) = (real, user);
    public string Generate(int id)
    {
        if (!_user.HasPermission("reports.read")) throw new UnauthorizedAccessException();
        return _real.Generate(id);   // gate, then delegate
    }
}
```
- **🔁 Proxy vs Decorator (same shape, different intent):** Decorator *adds behavior/features*; Proxy *controls access*.
- **Trade-offs:** Transparently adds access control/lazy-loading/caching. **Avoid** overusing / blurring responsibilities.
- **Modern C#:** EF Core lazy-loading proxies; dynamic proxies behind Moq/NSubstitute.

## 3.5 Composite
- **Intent:** Treat a **tree of objects** (whole + parts) the same as a single object.
- **Analogy:** Folders — a folder contains files and other folders; "how big?" recurses.

```csharp
public interface IFileSystemItem { long GetSizeBytes(); }

public class FileItem : IFileSystemItem
{
    private readonly long _size;
    public FileItem(long size) => _size = size;
    public long GetSizeBytes() => _size;             // leaf
}
public class FolderItem : IFileSystemItem
{
    private readonly List<IFileSystemItem> _children = new();
    public void Add(IFileSystemItem item) => _children.Add(item);
    public long GetSizeBytes() => _children.Sum(c => c.GetSizeBytes()); // recursion
}

var root = new FolderItem();
root.Add(new FileItem(100));
var sub = new FolderItem();
sub.Add(new FileItem(50));
root.Add(sub);
long total = root.GetSizeBytes(); // 150
```
- **Key idea:** leaf and group share one interface; the group loops over children (which may be groups). Write the logic once, works at any depth.
- **Trade-offs:** Clean for trees (menus, org charts, UI, file systems); no special-casing leaves vs groups. **Avoid** if the data isn't really a tree. Pairs naturally with **Visitor**.

## 3.6 Bridge
- **Intent:** When a design varies along **two independent dimensions**, split them into two hierarchies so they don't multiply.
- **Analogy:** TV + remote — remotes talk to any TV through a standard contract; both evolve separately.
- **Problem:** `VectorCircle`, `RasterCircle`, `VectorSquare`... class explosion (shapes × renderers).

```csharp
// Dimension 1: how to render
public interface IRenderer { void DrawCircle(float radius); }
public class VectorRenderer : IRenderer { public void DrawCircle(float r) => Console.WriteLine($"Vector r={r}"); }
public class RasterRenderer : IRenderer { public void DrawCircle(float r) => Console.WriteLine($"Pixels r={r}"); }

// Dimension 2: what shape
public abstract class Shape
{
    protected readonly IRenderer Renderer;   // the "bridge"
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
// New shape OR new renderer = ONE new class, not one-per-combination.
```
- **🔁 Bridge vs Adapter:** Adapter fixes a mismatch *after the fact*; Bridge is *designed up front* to separate two dimensions.
- **Trade-offs:** Avoids combinatorial explosion. **Avoid** if you really vary along only one dimension.

## 3.7 Flyweight (rare — skim)
- **Intent:** With a *huge* number of similar objects, **share common parts** to save memory.
- **Analogy:** A book's letters — one definition of "e" reused in many positions.
- **Idea:** Split state into **intrinsic** (shared, unchanging — e.g. a tree's model/texture) and **extrinsic** (unique per object — e.g. x/y position). Store the intrinsic part once. A forest of 1,000,000 trees stores the model once.
- **When:** Only when you genuinely have millions of objects and memory is a real problem — else premature optimization.
- **Modern C#:** Shows up as **string interning**, cached immutable value objects, and `ArrayPool<T>`.

### Lesson 3 recap

| Pattern | In one line | Already used as… |
|---|---|---|
| Adapter | Translate one interface to another | Wrapping any third-party SDK |
| **Decorator** ⭐ | Add features by wrapping | ASP.NET middleware, HttpClient handlers |
| Facade | Simple front door over complexity | Service/application layer |
| Proxy | A stand-in that controls access | EF Core lazy loading, mocking libraries |
| Composite | Treat a tree like a single object | File systems, menus, UI trees |
| Bridge | Separate two independent dimensions | (designed up front) |
| Flyweight | Share data across many objects | String interning, `ArrayPool<T>` |

**Confusing wrapping trio:** Adapter (*fix a mismatch*), Decorator (*add behavior*), Proxy (*control access*) — same shape, the difference is **why**.

---

# Part 4 — Behavioral Patterns

**About *how objects behave and talk to each other*.** The biggest, most useful group. **Master first: Strategy, Observer, Command.**

## 4.1 Strategy ⭐
- **Intent:** Put each way of doing something into its own class sharing one interface, and **swap it at runtime**.
- **Analogy:** Getting to the airport — drive, taxi, or train; same goal, swappable method.

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
// Adding "drone shipping" = one new class, no existing code changes.
```
- **Key idea:** the context knows only the interface (DIP). This is literally **"OCP, packaged."**
- **Trade-offs:** Kills big `if`/`switch`; each algorithm isolated and testable. **Avoid** when there's only one algorithm or a one-liner difference.
- **Modern C#:** A strategy can be a **delegate** (`Func<Order, decimal>`); .NET 8 **keyed DI** resolves the right one by key. The workhorse pattern.

## 4.2 Observer ⭐
- **Intent:** When one object changes, it automatically **notifies interested objects** without knowing who they are.
- **Analogy:** YouTube channel — upload once, all subscribers get notified; they join/leave anytime.

```csharp
public class Stock
{
    public event Action<decimal>? PriceChanged;   // built-in subscriber list
    private decimal _price;
    public decimal Price
    {
        get => _price;
        set { _price = value; PriceChanged?.Invoke(value); } // notify all
    }
}
var stock = new Stock();
stock.PriceChanged += p => Console.WriteLine($"UI update: {p}");
stock.PriceChanged += p => Console.WriteLine($"Log: {p}");
stock.Price = 99.5m;   // both subscribers fire
```
- **Key idea:** `event Action<decimal>` is C#'s subscriber list; `?.Invoke` fires only if subscribers exist; `+=` subscribes, `-=` unsubscribes. The publisher never knows who's listening.
- **Trade-offs:** Loose coupling; add/remove reactions anytime. ⚠️ **Memory leaks** — a long-lived object that subscribes and never `-=` can't be garbage-collected (real production bug). Subscriber order/re-entrancy can surprise.
- **Modern C#:** `event`/`Action`, `IObservable<T>` (Rx), `INotifyPropertyChanged` (UI binding). In reviews, **hunt for missing `-=`**.

## 4.3 Command ⭐
- **Intent:** Wrap an action (and its data) into an **object** so you can store, queue, pass, log, or undo it.
- **Analogy:** Restaurant order ticket — the action "make this dish" becomes a manageable thing.

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
- **Key idea:** each command knows how to **do** and **undo** itself and carries its data; the invoker just calls `Execute()` and keeps a stack. Because commands are objects, you can also queue or log them.
- **Trade-offs:** Enables undo/redo, queuing, logging, macros; decouples requester from performer. **Avoid** when you need none of those.
- **Modern C#:** **MediatR** is Command (request + handler), the backbone of **CQRS** in .NET.

## 4.4 Template Method
- **Intent:** A base class defines the **fixed skeleton** of a process; subclasses fill in specific steps.
- **Analogy:** Hot-drink recipe — boil water → add main ingredient → pour → add condiments; tea/coffee fill in specifics.

```csharp
public abstract class DataImporter
{
    public void Import(string path)            // template method — NOT virtual, order locked
    {
        var raw     = ReadFile(path);
        var records = Parse(raw);               // varies
        Validate(records);
        Save(records);
        Console.WriteLine("Import complete.");
    }
    protected string ReadFile(string path) => "raw data";        // shared
    protected abstract List<string> Parse(string raw);            // MUST override
    protected virtual void Validate(List<string> records) { }     // optional hook
    protected void Save(List<string> records) => Console.WriteLine($"Saved {records.Count}");
}
public class CsvImporter : DataImporter
{
    protected override List<string> Parse(string raw) => raw.Split(',').ToList();
}
```
- **Key idea:** `abstract` steps must be provided; `virtual` steps are optional hooks; subclasses can't reorder → enforces the process.
- **Trade-offs:** Reuse skeleton; guarantee step order. **Avoid** when you need to swap steps at *runtime* — prefer **Strategy** (composition over inheritance).

## 4.5 State
- **Intent:** An object changes its **behavior** when its internal **state** changes — as if it became a different class.
- **Analogy:** Traffic light — each color behaves differently and knows the next state.

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
- **Key idea:** each state owns its rules + transition; the object delegates to its current state. Scattered `if (Status == ...)` disappears.
- **🔁 State vs Strategy:** Strategy — *you* pick the algorithm from outside, strategies don't know each other. State — the object switches its *own* behavior and states often know the next state.
- **Trade-offs:** Removes tangled state conditionals. **Avoid** for 2 simple states (use a bool). For complex machines use a library like **Stateless**.

## 4.6 Chain of Responsibility
- **Intent:** Pass a request along a **line of handlers**; each handles it or forwards to the next.
- **Analogy:** Customer support tiers — Tier 1 → 2 → 3, each handles or escalates.

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
        if (!r.IsAuthenticated) { Console.WriteLine("Rejected: not authenticated"); return; }
        Console.WriteLine("Auth OK");
        Next?.Handle(r);
    }
}
public class ValidationHandler : Handler
{
    public override void Handle(Request r)
    {
        if (string.IsNullOrEmpty(r.Body)) { Console.WriteLine("Rejected: empty body"); return; }
        Console.WriteLine("Validation OK");
        Next?.Handle(r);
    }
}
var auth = new AuthHandler();
auth.SetNext(new ValidationHandler());
auth.Handle(myRequest);
```
- **Key idea:** each handler does its bit then `Next?.Handle(r)` — unless it stops the chain. Add/remove/reorder without touching others.
- **Trade-offs:** Decouples sender from handlers; small classes. **Avoid** if a request may fall off unhandled; long chains are hard to trace.
- **Modern C#:** ASP.NET Core **middleware** and `HttpClient` `DelegatingHandler` pipeline are exactly this.

## 4.7 Mediator
- **Intent:** Objects talk through **one central hub** instead of directly (no tangled web).
- **Analogy:** Air traffic control — planes talk to the tower, not each other.

```csharp
public interface IDialogMediator { void Notify(object sender, string ev); }
public class RegistrationDialog : IDialogMediator
{
    public Button SubmitButton { get; set; } = new();
    public Checkbox AgreeTerms { get; set; } = new();
    public void Notify(object sender, string ev)
    {
        if (sender == AgreeTerms && ev == "toggled")
            SubmitButton.Enabled = AgreeTerms.Checked;   // one place coordinates
    }
}
// Components just call mediator.Notify(this, "toggled") — they never reference each other.
```
- **Trade-offs:** Many-to-many mess → hub-and-spoke; interaction logic in one place. **Avoid** letting the mediator become a **god object** or routing trivial calls through it.
- **Modern C#:** **MediatR** (Mediator + Command); great for decoupling controllers from handlers, but often over-applied.

## 4.8 Iterator (C# gives it for free)
- **Intent:** Step through a collection's items without knowing how it stores them.
- **Analogy:** TV remote "next channel" button.

```csharp
public IEnumerable<int> FirstThreeEvens()
{
    yield return 0;   // hands back one item at a time, pausing between each
    yield return 2;
    yield return 4;
}
foreach (var n in FirstThreeEvens())   // foreach uses an iterator under the hood
    Console.WriteLine(n);
```
- **Key idea:** C# *is* the iterator pattern — `foreach`, `IEnumerable<T>`, `yield return` (a hidden lazy state machine); LINQ builds on lazy iteration.
- **Watch:** `IEnumerable<T>` is lazy (nothing runs until enumerated) and enumerating twice runs the work twice.

## 4.9 Visitor
- **Intent:** Add **new operations** to a group of objects **without modifying their classes**.
- **Analogy:** A tax auditor visiting different businesses; send a different visitor (safety inspector) next month without changing the businesses.

```csharp
public interface IShapeVisitor { void Visit(Circle c); void Visit(Square s); }
public interface IShape { void Accept(IShapeVisitor visitor); }

public class Circle : IShape
{
    public float Radius = 5;
    public void Accept(IShapeVisitor v) => v.Visit(this);   // double dispatch
}
public class AreaCalculator : IShapeVisitor   // a new operation = a new visitor; shapes never change
{
    public void Visit(Circle c) => Console.WriteLine(3.14f * c.Radius * c.Radius);
    public void Visit(Square s) => Console.WriteLine("square area...");
}
```
- **Trade-offs:** Add operations without touching element classes. **Downside:** adding a new *shape* forces updating *every* visitor; also verbose.
- **Modern C#:** Often replaced by **pattern matching / switch expressions** over a sealed hierarchy:
```csharp
decimal Area(IShape shape) => shape switch
{
    Circle c => 3.14m * (decimal)(c.Radius * c.Radius),
    Square s => 0m,
    _        => throw new ArgumentException("Unknown shape")
};
```
Reach for classic Visitor mainly for complex, stable structures (compiler ASTs).

## 4.10 Memento
- **Intent:** Capture an object's state so you can **restore it later**, without exposing internals.
- **Analogy:** Video-game save point.

```csharp
public record EditorSnapshot(string Content);   // immutable snapshot
public class TextEditor
{
    public string Content { get; private set; } = "";
    public void Type(string text) => Content += text;
    public EditorSnapshot Save() => new(Content);              // snapshot
    public void Restore(EditorSnapshot s) => Content = s.Content; // roll back
}
```
- **When:** Undo/redo, checkpoints, "cancel changes." Often paired with **Command** for undo systems.
- **Modern C#:** `record` types make mementos cheap (immutable by design) — but copies are shallow (nested mutable objects still shared).

## 4.11 Interpreter (rare — just be aware)
- **Intent:** Define a small "language" (grammar) and a way to evaluate sentences in it.
- **Advice:** You almost never hand-build this. Use a parser library (**ANTLR**, **Sprache**, **Superpower**) or C# `Expression` trees. Just recognize the name.

### Lesson 4 recap

| Pattern | In one line | Real C# example |
|---|---|---|
| **Strategy** ⭐ | Swap an algorithm at runtime | `Func<>`, keyed DI |
| **Observer** ⭐ | Notify many when one changes | `event`, `IObservable<T>`, Rx |
| **Command** ⭐ | Turn an action into an object | MediatR, CQRS, undo/redo |
| Template Method | Fixed skeleton, fill-in steps | Base-class workflows |
| State | Behavior changes with state | Stateless library |
| Chain of Responsibility | Pass request down a line | ASP.NET middleware |
| Mediator | Central hub, no direct talk | MediatR |
| Iterator | Step through a collection | `foreach`, `yield`, LINQ |
| Visitor | New operations without editing classes | Often replaced by pattern matching |
| Memento | Save & restore state | `record` snapshots + Command |
| Interpreter | Evaluate a mini-language | Use a parser library instead |

**Master first:** Strategy, Observer, Command.
**Same-shape/different-intent pairs:** Strategy (you choose) vs State (object switches itself); Command (an action as object) vs Strategy (an algorithm as object).

---

# Part 5 — Modern C#, the Tech-Lead Lens & Next Steps

## A — Patterns modern C# gives you for free
Seniority is partly knowing when **not** to hand-write a pattern the language/framework already expresses.

| Classic pattern | Modern C#/.NET instead |
|---|---|
| Singleton | `services.AddSingleton<IThing, Thing>();` — DI owns the lifetime |
| Factory / Abstract Factory | DI container, `Func<T>`, or **keyed services** (.NET 8+) |
| Strategy | A `Func<>` delegate, or keyed DI |
| Observer | `event` / `Action`, `IObservable<T>` (Rx), `INotifyPropertyChanged`, channels |
| Command + Mediator | **MediatR** (requests + handlers) |
| Chain of Responsibility | ASP.NET Core middleware; `HttpClient` `DelegatingHandler`s |
| Decorator | DI decoration via **Scrutor** (`.Decorate<T>()`); middleware |
| Iterator | `IEnumerable<T>` + `yield return`; LINQ |
| Prototype / Memento | `record` types + `with` expressions |
| Visitor | `switch` expressions + pattern matching over a sealed hierarchy |
| Builder | Object initializers + `required` members; `with` expressions |

> 🔑 **Rule:** if the framework already says it clearly, use the framework. Hand-roll only when it adds real clarity or a capability the built-in lacks.

```csharp
// records — immutable data + built-in copy
public record Person(string Name, int Age);
var a = new Person("Ada", 36);
var b = a with { Age = 37 };            // copy-and-change; 'a' untouched

// required members — safe construction without a Builder
public class Config { public required string ApiKey { get; init; } }
var c = new Config { ApiKey = "abc" };

// pattern matching — replaces many Visitor/Strategy switch scenarios
string Describe(object o) => o switch
{
    int n when n < 0 => "negative number",
    int              => "number",
    string s         => $"text of length {s.Length}",
    null             => "nothing",
    _                => "something else"
};

// DI container — quietly does Factory + Singleton for you
var services = new ServiceCollection();
services.AddSingleton<IClock, SystemClock>();      // one shared instance
services.AddScoped<IOrderRepo, SqlOrderRepo>();    // one per web request
services.AddTransient<IEmailSender, SmtpSender>(); // a fresh one each time
```

## B — The Tech-Lead lens
1. **Talk about *forces*, not pattern names.** Don't say "use a Strategy." Say "every new payment type edits this `switch`, risking existing ones — can each type be its own class?" Name the problem (OCP); let the pattern follow.
2. **Fight over-engineering (your #1 job).** For every abstraction ask: *"What concrete, likely change does this make cheaper — and is it actually coming?"* If "just in case," it's a **YAGNI** violation. The best code is the simplest that solves the real problem.
3. **Keep the "same shape, different intent" pairs straight:**

| Pair | How to tell apart |
|---|---|
| Strategy vs State | Strategy: *you* pick the algorithm. State: the object switches its own behavior and knows its next state. |
| Adapter vs Bridge | Adapter: fix a mismatch between existing things. Bridge: designed up front to separate two dimensions. |
| Decorator vs Proxy | Decorator: *add* behavior. Proxy: *control access*. |
| Factory Method vs Abstract Factory | Factory Method: one product. Abstract Factory: a matching *family*. |
| Command vs Strategy | Command: an *action* as an object (undo/queue). Strategy: an *algorithm* as an object. |

4. **Watch for pattern-shaped tech debt:** leaked Observer subscriptions (`+=` without `-=`); god-object Facades/Mediators; hand-rolled Singletons that should be DI-managed; Decorator/middleware ordering bugs; speculative Abstract Factories / single-implementation interfaces added "for flexibility."
5. **Connect LLD up to architecture:** Clean/Hexagonal = DIP at system scale with Adapters at boundaries; DDD tactical patterns (Repository, Aggregate, Value Object) build on these; CQRS = Command + Mediator at the application layer.

## C — Beyond the Gang of Four (enterprise patterns)

| Pattern | What it does | Watch out for |
|---|---|---|
| Repository | Abstracts data access behind an interface | Don't wrap EF Core in a thin repository for no reason — EF is already repository/unit-of-work. |
| Unit of Work | Groups changes into one commit/transaction | EF Core's `DbContext` already is one. |
| Options pattern | Strongly-typed config via `IOptions<T>` | The standard .NET way to read settings. |
| Result / Either | Return success-or-error as a value, not exceptions | Great for expected failures (validation); keep exceptions for truly exceptional cases. |
| Specification | Encapsulate a business rule / query filter as a reusable, combinable object | Handy for complex, reused query logic. |
| Null Object | A do-nothing implementation instead of `null` | Removes null-checks (e.g. `NullLogger`). |
| CQRS | Separate read model from write model | Powerful but adds complexity — not for simple CRUD. |

## D — Roadmap
1. **Cement foundations:** re-read Lesson 1 until interfaces, composition, DI feel natural. If one thing sticks: *"depend on interfaces, inject what you need."*
2. **Build the "core four" by hand** (no copy-paste): Strategy, Observer (C# `event`), Decorator (caching + logging), Factory Method. These cover ~80% of real use.
3. **Recognize patterns in the wild:** middleware (Chain), `HttpClient` handlers (Decorator/Chain), MediatR (Command/Mediator), DI registration (Factory/Singleton).
4. **Practice the tech-lead muscle:** in every review ask — *"Which SOLID principle would this violate if we DON'T change it?"* (justifies adding a pattern) and *"Is this abstraction earning its keep, or YAGNI?"* (justifies removing one).
5. **Level up to architecture:** Clean Architecture, DDD, CQRS — same principles at larger scale.

## Master resources
- **Video:** Christopher Okhravi (clearest conceptual, one per pattern); Nick Chapsas (modern, C#-specific, honest about obsolete patterns); Amichai Mantinband (patterns + clean architecture); Derek Banas (fast overview).
- **Written:** Refactoring Guru (C# examples, https://refactoring.guru/design-patterns/csharp); DoFactory .NET Design Patterns (https://www.dofactory.com/net/design-patterns); *Head First Design Patterns*; *Dependency Injection Principles, Practices, and Patterns* (Seemann & van Deursen).

**Bottom line:** You know all 23 classic patterns, how modern C# reshapes them, and — most importantly — the judgment to know **when not to use them**. Go build the "core four" by hand.
