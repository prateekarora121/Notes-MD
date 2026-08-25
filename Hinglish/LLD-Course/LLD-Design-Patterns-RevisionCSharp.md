# Low-Level Design (LLD) & Design Patterns in C#
### Senior Engineers / Tech Leads ke liye Basic → Advanced Guide

> **Yeh guide kaise use karein**
> Pehli baar top-to-bottom padho. Har pattern same structure follow karta hai taaki aap baad mein unhe jaldi compare kar sako:
> **Intent → Problem → Structure → C# Example → Pros/Cons → When to use / avoid → Senior/Tech-Lead lens → Video & resources.**
> "Tech-Lead lens" sections aapke role ke liye sabse important hain: yeh trade-offs, code-review signals, aur pattern kab over-engineering hota hai, is par focus karte hain.

---

## Table of Contents

1. [Foundations You Must Own First](#1-foundations)
   - SOLID principles
   - DRY, KISS, YAGNI, Composition over Inheritance
   - GRASP (advanced)
2. [Creational Patterns](#2-creational-patterns)
   - Factory Method, Abstract Factory, Builder, Prototype, Singleton
3. [Structural Patterns](#3-structural-patterns)
   - Adapter, Bridge, Composite, Decorator, Facade, Flyweight, Proxy
4. [Behavioral Patterns](#4-behavioral-patterns)
   - Strategy, Observer, Command, State, Template Method, Chain of Responsibility, Mediator, Iterator, Visitor, Memento, Interpreter
5. [Modern C# Idioms & When Patterns "Disappear"](#5-modern-csharp)
6. [The Tech-Lead Playbook: Applying This in Reviews & Design](#6-tech-lead-playbook)
7. [Curated Learning Path & Master Resources](#7-resources)

---

<a name="1-foundations"></a>
## 1. Foundations Jo Aapko Pehle Aani Chahiye

Design patterns *solutions* hote hain. SOLID aur uske friends woh *forces* hain jo aapko kisi pattern ki taraf le jaate hain. Tech lead ke roop mein, aap apne zyadatar decisions ko inhi terms mein justify karoge, na ki kisi pattern ka naam lekar.

### SOLID

| Principle | One-liner | C# smell it fixes |
|---|---|---|
| **S**ingle Responsibility | Ek class ka sirf ek reason hona chahiye change karne ka | God classes, `Manager`/`Helper` jo sab kuch karte hain |
| **O**pen/Closed | Extension ke liye open, modification ke liye closed | `switch` kisi type enum par jo har sprint mein badhta hai |
| **L**iskov Substitution | Subtypes ko base contract ke through usable hona chahiye | Override mein `NotSupportedException` |
| **I**nterface Segregation | Ek fat interface se better hai many small interfaces | Implementers jo unused methods par throw karte hain |
| **D**ependency Inversion | Abstractions par depend karo, concretions par nahi | Business logic mein buried `new SqlConnection(...)` |

```csharp
// ❌ Violates DIP + OCP: business logic bound to a concrete sink and grows via switch
public class OrderService
{
    public void Place(Order o)
    {
        var email = new SmtpEmailSender();      // concrete dependency
        email.Send(o.CustomerEmail, "Confirmed");
    }
}

// ✅ Depend on an abstraction; new notification channels don't touch OrderService
public interface INotifier { Task NotifyAsync(Order order); }

public class OrderService
{
    private readonly INotifier _notifier;
    public OrderService(INotifier notifier) => _notifier = notifier; // DI
    public Task PlaceAsync(Order o) => _notifier.NotifyAsync(o);
}
```

### DRY, KISS, YAGNI, Composition over Inheritance
- **DRY** – Don't Repeat Yourself, lekin *duplication galat abstraction se cheaper hota hai*. Extract sirf tab karo jab same reason se 3rd baar repetition ho.
- **KISS** – Keep It Simple. Zyadatar CRUD ko kisi pattern ki zarurat nahi hoti.
- **YAGNI** – "Just in case" extensibility points mat banao. Reviews mein enforce karne wali yeh #1 sabse important cheez hai.
- **Composition over Inheritance** – Deep `is-a` hierarchies ke bajaye `has-a` (collaborators inject karo) prefer karo. C# mein multiple inheritance nahi hota; composition + interfaces hi idiom hai.

### GRASP (advanced — tech leads ke liye good)
General Responsibility Assignment Software Patterns: *Information Expert, Creator, Controller, Low Coupling, High Cohesion, Polymorphism, Pure Fabrication, Indirection, Protected Variations.* Yeh aapko ek vocabulary dete hain ki *kyun* aap kisi class ko responsibility assign karte ho — design discussions mein invaluable hai.

📺 **Foundations resources**
- SOLID (C#): [Nick Chapsas – SOLID playlist](https://www.youtube.com/results?search_query=nick+chapsas+solid+principles)
- SOLID deep dive: [Refactoring Guru – Design Principles](https://refactoring.guru/design-patterns)
- GRASP: [search "GRASP principles explained"](https://www.youtube.com/results?search_query=GRASP+principles+explained)

---

<a name="2-creational-patterns"></a>
## 2. Creational Patterns
*Control karta hai **how** objects create hote hain, construction ko use se decouple karta hai.*

---

### 2.1 Factory Method
- **Intent:** Ek object create karne ke liye interface define karo, lekin subclasses/implementations ko decide karne do ki kaunsi class instantiate karni hai.
- **Problem:** Ek class ko objects create karne hain lekin concrete type hardcode nahi karna chahiye.
- **Structure:** `Creator` ek factory method declare karta hai jo `Product` abstraction return karta hai; concrete creators isko override karte hain.

```csharp
public interface IPaymentProcessor { Task<Receipt> ChargeAsync(decimal amount); }

public abstract class PaymentGateway
{
    // Factory Method
    protected abstract IPaymentProcessor CreateProcessor();

    public Task<Receipt> ProcessAsync(decimal amount)
        => CreateProcessor().ChargeAsync(amount); // uses the product via abstraction
}

public class StripeGateway : PaymentGateway
{
    protected override IPaymentProcessor CreateProcessor() => new StripeProcessor();
}
```
- **Pros:** Concrete coupling remove karta hai; OCP honor karta hai; creation logic centralize karta hai.
- **Cons:** Class explosion; jab simple DI-registered factory delegate kaam kar jaata hai to often overkill hota hai.
- **Use when:** Create karne wala type subclass/context par depend karta hai aur aap chahte ho ki subclasses isko extend karein.
- **Avoid when:** `Func<T>` ya DI container already type resolve kar deta hai.
- 🧭 **Tech-Lead lens:** Modern C# mein, 80% Factory Method use cases DI registration + `IServiceProvider`/keyed services (.NET 8+) mein collapse ho jaate hain. Classic pattern sirf tab use karo jab creation logic khud polymorphic ho.

📺 [Christopher Okhravi – Factory Method](https://www.youtube.com/results?search_query=christopher+okhravi+factory+method+pattern) · [Refactoring Guru](https://refactoring.guru/design-patterns/factory-method/csharp/example)

---

### 2.2 Abstract Factory
- **Intent:** Concrete classes specify kiye bina related objects ki **families** produce karo.
- **Problem:** Aapko objects ke consistent sets chahiye (e.g., ek whole UI theme, ya ek whole cloud provider ke clients).
- **Structure:** Multiple create methods wala `IAbstractFactory`; concrete factories matching family produce karte hain.

```csharp
public interface ICloudFactory
{
    IBlobStore CreateBlobStore();
    IQueue CreateQueue();
}

public class AzureFactory : ICloudFactory
{
    public IBlobStore CreateBlobStore() => new AzureBlob();
    public IQueue     CreateQueue()     => new AzureServiceBusQueue();
}

public class AwsFactory : ICloudFactory
{
    public IBlobStore CreateBlobStore() => new S3Blob();
    public IQueue     CreateQueue()     => new SqsQueue();
}
```
- **Pros:** Compatible families guarantee karta hai; ek factory swap karke whole family swap kar sakte ho.
- **Cons:** Family mein naya product add karne se har factory change hoti hai (us axis ke liye OCP violate karta hai).
- **Use when:** Multi-provider / multi-platform families jinhe consistent rehna zaruri hai.
- **Avoid when:** Aapke paas sirf ek family hai ya products unrelated hain.
- 🧭 **Tech-Lead lens:** Provider abstractions (multi-cloud, DB dialects) ke liye great hai. Dhyan rakhna ki jab product set volatile ho to yeh maintenance tax ban sakta hai.

📺 [Christopher Okhravi – Abstract Factory](https://www.youtube.com/results?search_query=christopher+okhravi+abstract+factory) · [Refactoring Guru](https://refactoring.guru/design-patterns/abstract-factory/csharp/example)

---

### 2.3 Builder
- **Intent:** Complex objects ko step by step construct karo; same process different representations bana sakta hai.
- **Problem:** Bahut zyada (especially optional) parameters wale constructors — "telescoping constructors."
- **Structure:** Ek `Builder` fluent methods ke through state accumulate karta hai aur `Build()` par final product return karta hai.

```csharp
public class HttpRequest
{
    public string Url { get; init; }
    public string Method { get; init; } = "GET";
    public IReadOnlyDictionary<string,string> Headers { get; init; }
    public string? Body { get; init; }
}

public class HttpRequestBuilder
{
    private string _url = "";
    private string _method = "GET";
    private readonly Dictionary<string,string> _headers = new();
    private string? _body;

    public HttpRequestBuilder Url(string u)            { _url = u; return this; }
    public HttpRequestBuilder Method(string m)         { _method = m; return this; }
    public HttpRequestBuilder Header(string k, string v){ _headers[k] = v; return this; }
    public HttpRequestBuilder Body(string b)           { _body = b; return this; }

    public HttpRequest Build() => new()
    {
        Url = _url, Method = _method, Headers = _headers, Body = _body
    };
}

// Usage
var req = new HttpRequestBuilder()
    .Url("https://api.example.com").Method("POST")
    .Header("Authorization", "Bearer …").Body("{}")
    .Build();
```
- **Pros:** Readable construction; immutable products; `Build()` mein invariants validate karta hai.
- **Cons:** Extra boilerplate; simple objects ke liye overkill hota hai.
- **Use when:** Bahut optional params hain, step-wise construction chahiye, ya aapko validation ke saath immutability chahiye.
- **Avoid when:** C# `init` + object initializers + `required` members already cleanly read ho rahe hain.
- 🧭 **Tech-Lead lens:** C# 11+ mein, `required` members aur `with` expressions bahut se builders replace kar dete hain. Builder tab rakho jab construction mein *ordering rules ya validation* ho jo plain initializer enforce nahi kar sakta.

📺 [Christopher Okhravi – Builder](https://www.youtube.com/results?search_query=christopher+okhravi+builder+pattern) · [Refactoring Guru](https://refactoring.guru/design-patterns/builder/csharp/example)

---

### 2.4 Prototype
- **Intent:** Existing instance ko clone karke naye objects create karo.
- **Problem:** Object creation expensive ya configuration-heavy hai; aapko preconfigured instance ki copies chahiye.
- **Structure:** Ek `Clone()` method (shallow ya deep).

```csharp
public interface IPrototype<T> { T Clone(); }

public class Document : IPrototype<Document>
{
    public string Title { get; set; } = "";
    public List<string> Sections { get; set; } = new();

    public Document Clone() => new()
    {
        Title = Title,
        Sections = new List<string>(Sections) // deep copy of the list
    };
}
```
- **Pros:** Expensive init avoid karta hai; "template" objects ke liye convenient hai.
- **Cons:** Deep vs shallow copy bugs; cycles wale object graphs clone karna hard hota hai.
- **Use when:** Costly-to-build template se bahut se near-identical objects chahiye.
- **Avoid when:** Objects build karna cheap hai, ya `record` `with` copying kaafi hai.
- 🧭 **Tech-Lead lens:** C# `record` + `with` aapko compiler-generated (shallow) prototype deta hai. Deep vs shallow ke baare mein explicit raho — yeh shared-reference bugs ka classic source hai.

📺 [Refactoring Guru – Prototype (C#)](https://refactoring.guru/design-patterns/prototype/csharp/example)

---

### 2.5 Singleton
- **Intent:** Ensure karo ki class ka exactly ek instance ho, global access point ke saath.
- **Problem:** Exactly ek shared resource (rare — usually ek config ya cache).

```csharp
// Thread-safe, lazy. But prefer DI (see lens).
public sealed class AppClock
{
    private static readonly Lazy<AppClock> _instance = new(() => new AppClock());
    public static AppClock Instance => _instance.Value;
    private AppClock() { }
    public DateTime UtcNow => DateTime.UtcNow;
}
```
- **Pros:** Single instance, lazy init.
- **Cons:** Global state, hidden dependencies, test/mock karna hard, parallel tests ke hostile.
- **Use when:** Almost never hand se.
- **Avoid when:** Aapke paas DI container hai — jo hai hi.
- 🧭 **Tech-Lead lens:** **Yeh wahi pattern hai jise reviews mein challenge karna chahiye.** Iske bajaye `services.AddSingleton<T>()` register karo — aapko single-instance semantics *plus* testability aur explicit dependencies milte hain. Hand-rolled singletons usually ek design smell hote hain.

📺 [Nick Chapsas – Singleton / why DI wins](https://www.youtube.com/results?search_query=nick+chapsas+singleton+dependency+injection) · [Refactoring Guru](https://refactoring.guru/design-patterns/singleton/csharp/example)

---

<a name="3-structural-patterns"></a>
## 3. Structural Patterns
*Objects ko larger structures mein compose karo while unhe flexible rakhte hue.*

---

### 3.1 Adapter
- **Intent:** Ek interface ko doosre interface mein convert karo jo client expect karta hai.
- **Problem:** Ek third-party/legacy class aapke interface se match nahi karti.

```csharp
public interface ILogger { void Log(string message); }

// Third-party we can't change:
public class SerilogSink { public void Write(LogLevel lvl, string msg) { /* … */ } }

// Adapter
public class SerilogAdapter : ILogger
{
    private readonly SerilogSink _sink;
    public SerilogAdapter(SerilogSink sink) => _sink = sink;
    public void Log(string message) => _sink.Write(LogLevel.Information, message);
}
```
- **Pros:** Kisi bhi side ko touch kiye bina incompatible code integrate karta hai; 3rd-party churn ko isolate karta hai.
- **Cons:** Extra indirection.
- **Use when:** External/legacy APIs ko apne khud ke contract ke peeche wrap karna ho.
- 🧭 **Tech-Lead lens:** Vendor lock-in ke against aapka primary defense. Boundary par adapters domain ko clean aur swappable rakhte hain.

📺 [Christopher Okhravi – Adapter](https://www.youtube.com/results?search_query=christopher+okhravi+adapter+pattern) · [Refactoring Guru](https://refactoring.guru/design-patterns/adapter/csharp/example)

---

### 3.2 Bridge
- **Intent:** Ek abstraction ko uske implementation se decouple karo taaki dono independently vary kar sakein.
- **Problem:** Ek class hierarchy **two** independent dimensions ke along explode hoti hai (e.g., *Shape* × *Renderer*).

```csharp
public interface IRenderer { void RenderCircle(float radius); }

public abstract class Shape
{
    protected readonly IRenderer Renderer; // the "bridge"
    protected Shape(IRenderer renderer) => Renderer = renderer;
    public abstract void Draw();
}

public class Circle : Shape
{
    private readonly float _radius;
    public Circle(IRenderer r, float radius) : base(r) => _radius = radius;
    public override void Draw() => Renderer.RenderCircle(_radius);
}
// Now Shapes × Renderers combine freely without N×M subclasses.
```
- **Pros:** Combinatorial subclass explosion avoid karta hai; dono axes independently evolve hote hain.
- **Cons:** Zyada upfront indirection; grasp karna harder hota hai.
- **Use when:** Variation ke two orthogonal dimensions hon.
- 🧭 **Tech-Lead lens:** Often Adapter ke saath confuse hota hai. Adapter = *ek existing mismatch fix karna*; Bridge = *upfront designed* karke do axes separate karna.

📺 [Christopher Okhravi – Bridge](https://www.youtube.com/results?search_query=christopher+okhravi+bridge+pattern) · [Refactoring Guru](https://refactoring.guru/design-patterns/bridge/csharp/example)

---

### 3.3 Composite
- **Intent:** Individual objects aur objects ki compositions ko uniformly treat karo (tree structures).
- **Problem:** Clients ko leaves aur containers ko differently handle karna padta hai.

```csharp
public interface IFileSystemNode { long GetSize(); }

public class FileLeaf : IFileSystemNode
{
    private readonly long _size;
    public FileLeaf(long size) => _size = size;
    public long GetSize() => _size;
}

public class DirectoryNode : IFileSystemNode
{
    private readonly List<IFileSystemNode> _children = new();
    public void Add(IFileSystemNode node) => _children.Add(node);
    public long GetSize() => _children.Sum(c => c.GetSize()); // recursion
}
```
- **Pros:** Uniform treatment; trees ke liye natural hai (menus, org charts, file systems, UI).
- **Cons:** Over-generalize ho sakta hai; leaf vs composite ki type safety blurry ho jaati hai.
- **Use when:** Recursive part-whole hierarchies.
- 🧭 **Tech-Lead lens:** Tree par operations ke liye Visitor ke saath naturally pairs karta hai.

📺 [Christopher Okhravi – Composite](https://www.youtube.com/results?search_query=christopher+okhravi+composite+pattern) · [Refactoring Guru](https://refactoring.guru/design-patterns/composite/csharp/example)

---

### 3.4 Decorator
- **Intent:** Object ko wrap karke usme dynamically responsibilities attach karo.
- **Problem:** Aapko behavior (caching, logging, retry) add karna hai bina subclass explosion ke ya original edit kiye.

```csharp
public interface IProductRepository { Task<Product> GetAsync(int id); }

public class SqlProductRepository : IProductRepository
{
    public Task<Product> GetAsync(int id) => /* hit DB */ default!;
}

// Decorator adds caching, same interface
public class CachingProductRepository : IProductRepository
{
    private readonly IProductRepository _inner;
    private readonly IMemoryCache _cache;
    public CachingProductRepository(IProductRepository inner, IMemoryCache cache)
        => (_inner, _cache) = (inner, cache);

    public async Task<Product> GetAsync(int id)
        => (await _cache.GetOrCreateAsync($"prod:{id}", _ => _inner.GetAsync(id)))!;
}
// Stack them: retry → caching → logging → sql
```
- **Pros:** Open/Closed; runtime par behaviors compose karta hai; har concern isolated hota hai.
- **Cons:** Bahut se small wrappers; deep stacks debug karna; order matter karta hai.
- **Use when:** Same contract par layered cross-cutting concerns.
- 🧭 **Tech-Lead lens:** Real C# ke *most useful* patterns mein se ek. Note: ASP.NET Core middleware aur `HttpClient` ke liye `DelegatingHandler` Decorator/Chain hi hain, disguise mein. Reviews mein decorator ki *ordering* scrutinize karo.

📺 [Christopher Okhravi – Decorator](https://www.youtube.com/results?search_query=christopher+okhravi+decorator+pattern) · [Nick Chapsas – Decorators in .NET](https://www.youtube.com/results?search_query=nick+chapsas+decorator+pattern) · [Refactoring Guru](https://refactoring.guru/design-patterns/decorator/csharp/example)

---

### 3.5 Facade
- **Intent:** Complex subsystem ke upar ek simplified interface provide karo.
- **Problem:** Clients bahut si collaborating classes ke details mein drown ho jaate hain.

```csharp
public class OrderCheckoutFacade
{
    private readonly IInventory _inventory;
    private readonly IPaymentGateway _payment;
    private readonly IShipping _shipping;
    // ctor injection …

    public async Task<Result> CheckoutAsync(Cart cart)
    {
        if (!await _inventory.ReserveAsync(cart)) return Result.Fail("Out of stock");
        var receipt = await _payment.ChargeAsync(cart.Total);
        await _shipping.ScheduleAsync(cart, receipt);
        return Result.Ok();
    }
}
```
- **Pros:** Simple entry point; subsystem ke saath coupling reduce karta hai.
- **Cons:** Agar logic accumulate ho jaaye to god-object ban sakta hai.
- **Use when:** Small API ke peeche complex subsystem ko tame karna ho.
- 🧭 **Tech-Lead lens:** Application/service layer classes often Facades hi hoti hain. Dhyan rakhna ki woh *orchestrate* karein, business rules *implement* na karein.

📺 [Christopher Okhravi – Facade](https://www.youtube.com/results?search_query=christopher+okhravi+facade+pattern) · [Refactoring Guru](https://refactoring.guru/design-patterns/facade/csharp/example)

---

### 3.6 Flyweight
- **Intent:** Memory bachane ke liye bahut se objects mein common state share karo.
- **Problem:** Duplicated intrinsic state wale millions objects.
- **Structure:** *intrinsic* (shared) state ko *extrinsic* (per-instance) state se split karo; shared part ko cache karo.
- **Use when:** Huge object counts (game particles, glyphs, map tiles).
- **Avoid when:** Object counts modest hain — premature optimization.
- 🧭 **Tech-Lead lens:** Typical business apps mein rare hota hai. C# string interning, `ArrayPool<T>`, aur cached immutable value objects hi practical incarnations hain.

📺 [Refactoring Guru – Flyweight (C#)](https://refactoring.guru/design-patterns/flyweight/csharp/example)

---

### 3.7 Proxy
- **Intent:** Ek placeholder jo doosre object ke access ko control karta hai (lazy, remote, protection, virtual).
- **Problem:** Aapko access intercept karna hai — lazy loading, access control, remoting, caching.

```csharp
public interface IReportService { byte[] Generate(int id); }

public class ReportServiceProxy : IReportService
{
    private readonly IReportService _real;
    private readonly IUserContext _user;
    public ReportServiceProxy(IReportService real, IUserContext user)
        => (_real, _user) = (real, user);

    public byte[] Generate(int id)
    {
        if (!_user.HasPermission("reports.read"))
            throw new UnauthorizedAccessException();
        return _real.Generate(id); // control access before delegating
    }
}
```
- **Pros:** Transparently control add karta hai (same interface).
- **Cons:** Decorator jaisa lagta hai; overuse hone par responsibility blur ho jaati hai.
- **Use when:** Lazy init, access control, remoting, virtual proxies.
- 🧭 **Tech-Lead lens:** EF Core lazy-loading proxies aur Castle DynamicProxy (jo bahut si mocking/AOP libs use karti hain) yehi pattern hain. Decorator *behavior add* karta hai; Proxy *access control* karta hai — same shape, different intent.

📺 [Christopher Okhravi – Proxy](https://www.youtube.com/results?search_query=christopher+okhravi+proxy+pattern) · [Refactoring Guru](https://refactoring.guru/design-patterns/proxy/csharp/example)

---

<a name="4-behavioral-patterns"></a>
## 4. Behavioral Patterns
*Objects ke beech algorithms, responsibilities, aur communication manage karo.*

---

### 4.1 Strategy ⭐ (pehle yeh seekho)
- **Intent:** Interchangeable algorithms ki ek family define karo; runtime par ek select karo.
- **Problem:** Ek kaam karne ke multiple ways hain, dynamically chosen; aap bade `switch`/`if` chains khatam karna chahte ho.

```csharp
public interface IShippingStrategy { decimal Calculate(Order order); }

public class StandardShipping : IShippingStrategy
{ public decimal Calculate(Order o) => o.Weight * 1.5m; }

public class ExpressShipping : IShippingStrategy
{ public decimal Calculate(Order o) => o.Weight * 3.0m + 10; }

public class ShippingCalculator
{
    private readonly IShippingStrategy _strategy;
    public ShippingCalculator(IShippingStrategy strategy) => _strategy = strategy;
    public decimal GetCost(Order o) => _strategy.Calculate(o);
}
```
- **Pros:** OCP; isolation mein testable; conditionals remove karta hai.
- **Cons:** Zyada types; client ko pata hona chahiye kaunsi strategy pick karni hai.
- **Use when:** Interchangeable algorithms/policies.
- 🧭 **Tech-Lead lens:** Workhorse pattern hai. C# mein, `Func<Order,decimal>` ek lightweight Strategy hai. Key se strategies resolve karne ke liye keyed DI (.NET 8) ke saath combine karo.

📺 [Christopher Okhravi – Strategy](https://www.youtube.com/results?search_query=christopher+okhravi+strategy+pattern) · [Refactoring Guru](https://refactoring.guru/design-patterns/strategy/csharp/example)

---

### 4.2 Observer
- **Intent:** One-to-many dependency; jab subject change hota hai, observers ko notify kiya jaata hai.
- **Problem:** Objects ko bina tight coupling ke doosre object ke state changes par react karna padta hai.

```csharp
// Idiomatic C#: events
public class Stock
{
    public event Action<decimal>? PriceChanged;
    private decimal _price;
    public decimal Price
    {
        get => _price;
        set { _price = value; PriceChanged?.Invoke(value); }
    }
}
// Subscribers: stock.PriceChanged += p => Console.WriteLine($"New price {p}");
```
- **Pros:** Loose coupling; dynamic subscriptions.
- **Cons:** Un-unsubscribed handlers se memory leaks; ordering/reentrancy surprises.
- **Use when:** Event-driven notifications.
- 🧭 **Tech-Lead lens:** C# `event`/`Action`, `IObservable<T>`/Rx, aur `INotifyPropertyChanged` sab Observer hi hain. **Long-lived objects mein leaked subscriptions** (lapsed listeners) par dhyan rakhna — ek common production bug hai.

📺 [Christopher Okhravi – Observer](https://www.youtube.com/results?search_query=christopher+okhravi+observer+pattern) · [Refactoring Guru](https://refactoring.guru/design-patterns/observer/csharp/example)

---

### 4.3 Command
- **Intent:** Ek request ko object ke roop mein encapsulate karo (params, undo, queue, log).
- **Problem:** Aapko operations parameterize, queue, log, ya undo karne hain.

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
// Invoker keeps a history stack for undo/redo.
```
- **Pros:** Undo/redo, queuing, logging, macro commands; sender ko receiver se decouple karta hai.
- **Cons:** Bahut si small classes.
- **Use when:** Undo/redo, task queues, transactional actions, CQRS commands.
- 🧭 **Tech-Lead lens:** **MediatR** requests/handlers hi Command pattern hain aur .NET CQRS mein ubiquitous hain. Isko deeply samjho — aap bahut sa review karoge.

📺 [Christopher Okhravi – Command](https://www.youtube.com/results?search_query=christopher+okhravi+command+pattern) · [Refactoring Guru](https://refactoring.guru/design-patterns/command/csharp/example)

---

### 4.4 State
- **Intent:** Jab object ka internal state change ho to uska behavior alter karo — yeh *appears* hota hai jaise class change ho rahi ho.
- **Problem:** Behavior state par depend karta hai aur aapke paas sprawling `switch(state)` blocks hain.

```csharp
public interface IOrderState { IOrderState Next(); string Status { get; } }

public class PendingState : IOrderState
{ public string Status => "Pending"; public IOrderState Next() => new PaidState(); }

public class PaidState : IOrderState
{ public string Status => "Paid"; public IOrderState Next() => new ShippedState(); }

public class ShippedState : IOrderState
{ public string Status => "Shipped"; public IOrderState Next() => this; }
```
- **Pros:** State conditionals remove karta hai; har state ke rules localized hote hain.
- **Cons:** Har state ke liye class; transitions scattered ho sakte hain.
- **Use when:** State-specific behavior wale well-defined state machines.
- 🧭 **Tech-Lead lens:** Complex workflows ke liye hand-rolling ke bajaye ek real state-machine lib (e.g., **Stateless**) consider karo. State (behavior changes) ko Strategy (algorithm choice) se distinguish karo — same shape, different intent.

📺 [Christopher Okhravi – State](https://www.youtube.com/results?search_query=christopher+okhravi+state+pattern) · [Refactoring Guru](https://refactoring.guru/design-patterns/state/csharp/example)

---

### 4.5 Template Method
- **Intent:** Base class mein algorithm ka skeleton define karo, steps ko subclasses ko defer karo.
- **Problem:** Kai algorithms structure share karte hain lekin specific steps mein differ karte hain.

```csharp
public abstract class DataImporter
{
    // Template method — fixed skeleton
    public void Import(string path)
    {
        var raw = Read(path);
        var records = Parse(raw);
        Validate(records);
        Save(records);
    }
    protected abstract string Read(string path);
    protected abstract IEnumerable<Record> Parse(string raw);
    protected virtual void Validate(IEnumerable<Record> r) { } // hook
    protected abstract void Save(IEnumerable<Record> records);
}
```
- **Pros:** Skeleton reuse hota hai; invariant order enforce karta hai.
- **Cons:** Inheritance-bound hai; rigid ho sakta hai.
- **Use when:** Fixed process, variable steps.
- 🧭 **Tech-Lead lens:** Jab otherwise multiple inheritance ya runtime step swapping ki zarurat ho to Strategy/composition prefer karo. Template Method aapko class hierarchy mein lock kar deta hai.

📺 [Christopher Okhravi – Template Method](https://www.youtube.com/results?search_query=christopher+okhravi+template+method+pattern) · [Refactoring Guru](https://refactoring.guru/design-patterns/template-method/csharp/example)

---

### 4.6 Chain of Responsibility
- **Intent:** Ek request ko handlers ki chain ke along pass karo jab tak koi ek usko handle na kare.
- **Problem:** Multiple potential handlers hain; aap nahi chahte ki sender kisi specific handler se coupled ho.

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
        if (!r.IsAuthenticated) throw new UnauthorizedAccessException();
        Next?.Handle(r);
    }
}
// authHandler.SetNext(validationHandler).SetNext(loggingHandler);
```
- **Pros:** Sender/receiver decouple karta hai; handlers freely add/reorder kar sakte ho.
- **Cons:** Request unhandled ja sakta hai; trace karna harder hota hai.
- **Use when:** Pipelines, validation chains, middleware.
- 🧭 **Tech-Lead lens:** **ASP.NET Core middleware** aur `HttpClient` ke `DelegatingHandler`s Chain of Responsibility hi hain. Aap already isko daily use karte ho.

📺 [Christopher Okhravi – Chain of Responsibility](https://www.youtube.com/results?search_query=christopher+okhravi+chain+of+responsibility) · [Refactoring Guru](https://refactoring.guru/design-patterns/chain-of-responsibility/csharp/example)

---

### 4.7 Mediator
- **Intent:** Objects ke beech communication centralize karo taaki woh ek doosre ko directly refer na karein.
- **Problem:** Many-to-many coupling (direct references ka "spaghetti").
- **Use when:** Complex UI/component interactions, request→handler decoupling.
- 🧭 **Tech-Lead lens:** **MediatR** (in-process) canonical .NET example hai — yeh Mediator + Command hai. Over-use se bacho: trivial calls ko mediator ke through route karna benefit ke bina indirection add karta hai. Design reviews mein yeh trade-off discuss karo.

📺 [Christopher Okhravi – Mediator](https://www.youtube.com/results?search_query=christopher+okhravi+mediator+pattern) · [Refactoring Guru](https://refactoring.guru/design-patterns/mediator/csharp/example)

---

### 4.8 Iterator
- **Intent:** Underlying representation expose kiye bina elements ko sequentially access karo.
- 🧭 **Tech-Lead lens:** C# aapko yeh free mein deta hai: `IEnumerable<T>`/`IEnumerator<T>` aur `yield return`. Aap rarely isko hand se implement karte ho — lekin yeh jaanna ki `yield` ek lazy state machine produce karta hai, senior-level knowledge hai.

```csharp
public IEnumerable<int> Fibonacci()
{
    int a = 0, b = 1;
    while (true) { yield return a; (a, b) = (b, a + b); } // lazy iterator
}
```
📺 [Refactoring Guru – Iterator (C#)](https://refactoring.guru/design-patterns/iterator/csharp/example)

---

### 4.9 Visitor
- **Intent:** Object structure ki classes modify kiye bina usme operations add karo (double dispatch).
- **Problem:** Aapko ek stable class hierarchy ke upar bahut se unrelated operations chahiye.
- **Pros:** Element classes touch kiye bina naye operations.
- **Cons:** Naya *element* type add karne se har visitor change karna padta hai; verbose hota hai.
- **Use when:** Stable element hierarchy, frequently added operations (compilers/ASTs).
- 🧭 **Tech-Lead lens:** Powerful hai lekin heavy hai. Modern C# mein, sealed type hierarchy ke upar **pattern matching / switch expressions** often Visitor ko far less code se replace kar dete hain.

```csharp
// Modern C# alternative to Visitor
decimal Area(Shape s) => s switch
{
    Circle c    => MathF.PI * c.Radius * c.Radius,
    Rectangle r => r.Width * r.Height,
    _           => throw new ArgumentOutOfRangeException()
};
```
📺 [Christopher Okhravi – Visitor](https://www.youtube.com/results?search_query=christopher+okhravi+visitor+pattern) · [Refactoring Guru](https://refactoring.guru/design-patterns/visitor/csharp/example)

---

### 4.10 Memento
- **Intent:** Encapsulation violate kiye bina object ka internal state capture aur restore karo.
- **Use when:** Undo/redo, snapshots, checkpoints.
- 🧭 **Tech-Lead lens:** Undo ke liye often Command ke saath paired hota hai. C# `record` snapshots mementos ko cheap aur immutable bana dete hain.

📺 [Refactoring Guru – Memento (C#)](https://refactoring.guru/design-patterns/memento/csharp/example)

---

### 4.11 Interpreter (rare)
- **Intent:** Ek grammar define karo aur usme sentences interpret karo.
- 🧭 **Tech-Lead lens:** Almost never hand se build karna — parser lib (ANTLR, Sprache, Superpower) ya `Expression` trees use karo. Jaan lo ki yeh exist karta hai; iske liye reach mat karo.

📺 [Refactoring Guru – Interpreter](https://refactoring.guru/design-patterns/interpreter)

---

<a name="5-modern-csharp"></a>
## 5. Modern C# Idioms & Jab Patterns "Disappear" Ho Jaate Hain

Ek senior/tech-lead differentiator: yeh jaanna ki language ab aapko kaunse GoF patterns free mein de deti hai.

| Classic pattern | Modern C# / .NET replacement |
|---|---|
| Singleton | `services.AddSingleton<T>()` (DI lifetime) |
| Factory / Abstract Factory | DI container, `Func<T>`, keyed services (.NET 8), `IServiceProvider` |
| Strategy | `Func<>` delegates, keyed DI |
| Observer | `event` / `Action`, `IObservable<T>` (Rx), `INotifyPropertyChanged`, channels |
| Command + Mediator | **MediatR** requests/handlers |
| Chain of Responsibility | ASP.NET Core middleware, `DelegatingHandler` |
| Decorator | DI decoration (Scrutor `.Decorate<T>()`), middleware |
| Iterator | `IEnumerable<T>` + `yield return`, LINQ |
| Prototype / Memento | `record` + `with` expressions |
| Visitor | `switch` expressions + pattern matching over sealed hierarchies |
| Builder | `required` members, object initializers, `with` |

**Reviews ke liye rule of thumb:** Agar language ya framework already intent ko idiomatically express kar rahi hai, to usko prefer karo. Textbook pattern ke liye reach karo sirf tab jab woh genuine clarity ya capability add kare.

📺 [Nick Chapsas channel (modern C#/.NET)](https://www.youtube.com/@nickchapsas) · [Amichai Mantinband (patterns & clean arch in C#)](https://www.youtube.com/@amantinband)

---

<a name="6-tech-lead-playbook"></a>
## 6. The Tech-Lead Playbook: Reviews & Design Mein Isko Apply Karna

Patterns jaanne se aage, aapka role hai unke use ko **govern** karna. Actively karne wali cheezein:

1. **Force ka naam lo, pattern ka nahi.** "Yeh OCP violate karta hai kyunki har naya payment type is switch ko edit karta hai" "yahan Strategy use karo" se better lands karta hai.
2. **Over-engineering (YAGNI) ke against guard karo.** Sabse common junior mistake yeh hai ki patterns preemptively apply kar diye jaate hain. Poocho: *"Yeh abstraction kaunsa concrete change cheaper banata hai — aur kya woh change likely hai?"*
3. **Confusable pairs par dhyan do** (frequent interview & review topics):
   - Strategy vs State (algorithm choice vs behavior-by-state)
   - Adapter vs Bridge (fix mismatch vs designed-in two axes)
   - Decorator vs Proxy (add behavior vs control access)
   - Factory Method vs Abstract Factory (one product vs a family)
4. **Composition + DI prefer karo.** Zyadatar "kaunsa pattern?" debates "ek abstraction inject karo" mein dissolve ho jaate hain.
5. **Pattern-shaped tech debt track karo:** leaked Observer subscriptions, god-object Facades, mediator-for-everything, decorator ordering bugs.
6. **LLD ko architecture se connect karo:** yeh patterns Clean/Hexagonal architecture, DDD tactical patterns (Repository, Unit of Work, Aggregate), aur CQRS ke neeche ke building blocks hain. LLD fluency hi aapki HLD ko credible banati hai.

### Tech lead ke liye jaanne layak adjacent patterns (GoF nahi)
- **Repository & Unit of Work** (data access abstraction — lekin EF Core ko needlessly wrap mat karo)
- **Options pattern** (`IOptions<T>` config)
- **Result / Either** (exceptions ke bina error handling)
- **Specification pattern** (composable query/business rules)
- **Null Object** (null checks avoid karna)
- **CQRS + Mediator** (read/write separation)

---

<a name="7-resources"></a>
## 7. Curated Learning Path & Master Resources

### Suggested order (basic → advanced)
1. SOLID + composition over inheritance
2. Strategy → Observer → Decorator → Factory Method (the "daily four")
3. Command → State → Template Method → Chain of Responsibility
4. Adapter → Facade → Proxy → Composite
5. Abstract Factory → Builder → Bridge → Visitor
6. Modern C# replacements (Section 5) + adjacent tech-lead patterns (Section 6)
7. Rare/heavy: Flyweight, Interpreter, Memento, Prototype

### Master video resources
- 🎥 **Christopher Okhravi – Design Patterns playlist** (best conceptual explanations, ~1 video/pattern): [playlist search](https://www.youtube.com/results?search_query=christopher+okhravi+design+patterns+playlist)
- 🎥 **Nick Chapsas** – modern, C#-specific, patterns kab obsolete hain uspar opinionated: [channel](https://www.youtube.com/@nickchapsas)
- 🎥 **Amichai Mantinband** – real .NET mein patterns + clean architecture: [channel](https://www.youtube.com/@amantinband)
- 🎥 **Derek Banas – Design Patterns** (fast overview series): [search](https://www.youtube.com/results?search_query=derek+banas+design+patterns)

### Master written resources
- 📖 **Refactoring Guru** (har pattern ke *liye* C# examples hain): https://refactoring.guru/design-patterns/csharp
- 📖 **DoFactory – .NET Design Patterns** (C# reference): https://www.dofactory.com/net/design-patterns
- 📖 *Head First Design Patterns* (concepts) + *Design Patterns* (GoF, the original)
- 📖 *Dependency Injection Principles, Practices, and Patterns* — Seemann & van Deursen ("DI replaces patterns" mindset ke liye essential)

> **Video links par Note:** Maine YouTube *search* links use kiye hain (jo always current results par resolve hote hain) aur stable site links, individual video URLs hard-code karne ke bajaye jo time ke saath rot ho jaate hain. Named creator se top result pick karo.

---

*Guide ek Senior Engineer / Tech Lead ke liye generated hai, C# focus ke saath. Suggested next step: "daily four" (Strategy, Observer, Decorator, Factory Method) pick karo, har ek ko ek scratch project mein once implement karo, phir Section 5 dobara padho yeh dekhne ke liye ki modern .NET mein same kaam idiomatically kaise karoge.*
