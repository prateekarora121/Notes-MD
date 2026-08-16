# Low-Level Design (LLD) & Design Patterns in C#
### A Basic → Advanced Guide for Senior Engineers / Tech Leads

> **How to use this guide**
> Work top-to-bottom the first time. Each pattern follows the same structure so you can compare them quickly later:
> **Intent → Problem → Structure → C# Example → Pros/Cons → When to use / avoid → Senior/Tech-Lead lens → Video & resources.**
> The "Tech-Lead lens" sections are the ones that matter most for your role: they focus on trade-offs, code-review signals, and when a pattern is over-engineering.

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
## 1. Foundations You Must Own First

Design patterns are *solutions*. SOLID and its friends are the *forces* that make you reach for a pattern. As a tech lead, you'll justify most decisions in these terms, not by naming a pattern.

### SOLID

| Principle | One-liner | C# smell it fixes |
|---|---|---|
| **S**ingle Responsibility | A class has one reason to change | God classes, `Manager`/`Helper` doing everything |
| **O**pen/Closed | Open for extension, closed for modification | `switch` on a type enum that grows every sprint |
| **L**iskov Substitution | Subtypes must be usable through the base contract | `NotSupportedException` in an override |
| **I**nterface Segregation | Many small interfaces > one fat one | Implementers throwing on methods they don't need |
| **D**ependency Inversion | Depend on abstractions, not concretions | `new SqlConnection(...)` buried in business logic |

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
- **DRY** – Don't Repeat Yourself, but *duplication is cheaper than the wrong abstraction*. Extract only after the 3rd repetition with the same reason to change.
- **KISS** – Keep It Simple. Most CRUD does not need a pattern.
- **YAGNI** – Don't build extensibility points "just in case." This is the #1 thing to enforce in reviews.
- **Composition over Inheritance** – Favor `has-a` (inject collaborators) over deep `is-a` hierarchies. C# has no multiple inheritance; composition + interfaces is the idiom.

### GRASP (advanced — good for tech leads)
General Responsibility Assignment Software Patterns: *Information Expert, Creator, Controller, Low Coupling, High Cohesion, Polymorphism, Pure Fabrication, Indirection, Protected Variations.* These give you a vocabulary for *why* you assign a responsibility to a class — invaluable in design discussions.

📺 **Foundations resources**
- SOLID (C#): [Nick Chapsas – SOLID playlist](https://www.youtube.com/results?search_query=nick+chapsas+solid+principles)
- SOLID deep dive: [Refactoring Guru – Design Principles](https://refactoring.guru/design-patterns)
- GRASP: [search "GRASP principles explained"](https://www.youtube.com/results?search_query=GRASP+principles+explained)

---

<a name="2-creational-patterns"></a>
## 2. Creational Patterns
*Control **how** objects are created, decoupling construction from use.*

---

### 2.1 Factory Method
- **Intent:** Define an interface for creating an object, but let subclasses/implementations decide which class to instantiate.
- **Problem:** A class needs to create objects but shouldn't hardcode the concrete type.
- **Structure:** `Creator` declares a factory method returning a `Product` abstraction; concrete creators override it.

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
- **Pros:** Removes concrete coupling; honors OCP; centralizes creation logic.
- **Cons:** Class explosion; often overkill when a simple DI-registered factory delegate works.
- **Use when:** The type to create depends on subclass/context and you want subclasses to extend it.
- **Avoid when:** A `Func<T>` or DI container already resolves the type.
- 🧭 **Tech-Lead lens:** In modern C#, 80% of Factory Method use cases collapse into DI registration + `IServiceProvider`/keyed services (.NET 8+). Reach for the classic pattern only when creation logic itself is polymorphic.

📺 [Christopher Okhravi – Factory Method](https://www.youtube.com/results?search_query=christopher+okhravi+factory+method+pattern) · [Refactoring Guru](https://refactoring.guru/design-patterns/factory-method/csharp/example)

---

### 2.2 Abstract Factory
- **Intent:** Produce **families** of related objects without specifying concrete classes.
- **Problem:** You need consistent sets of objects (e.g., a whole UI theme, or a whole cloud provider's clients).
- **Structure:** An `IAbstractFactory` with multiple create methods; concrete factories produce a matching family.

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
- **Pros:** Guarantees compatible families; swap the whole family by swapping one factory.
- **Cons:** Adding a new product to the family changes every factory (violates OCP for that axis).
- **Use when:** Multi-provider / multi-platform families that must stay consistent.
- **Avoid when:** You only have one family or products are unrelated.
- 🧭 **Tech-Lead lens:** Great for provider abstractions (multi-cloud, DB dialects). Watch for it becoming a maintenance tax when the product set is volatile.

📺 [Christopher Okhravi – Abstract Factory](https://www.youtube.com/results?search_query=christopher+okhravi+abstract+factory) · [Refactoring Guru](https://refactoring.guru/design-patterns/abstract-factory/csharp/example)

---

### 2.3 Builder
- **Intent:** Construct complex objects step by step; the same process can build different representations.
- **Problem:** Constructors with many (especially optional) parameters — "telescoping constructors."
- **Structure:** A `Builder` accumulates state via fluent methods and returns the final product on `Build()`.

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
- **Pros:** Readable construction; immutable products; validates invariants in `Build()`.
- **Cons:** Extra boilerplate; overkill for simple objects.
- **Use when:** Many optional params, step-wise construction, or you want immutability with validation.
- **Avoid when:** C# `init` + object initializers + `required` members already read cleanly.
- 🧭 **Tech-Lead lens:** In C# 11+, `required` members and `with` expressions replace many builders. Keep the builder when construction has *ordering rules or validation* that a plain initializer can't enforce.

📺 [Christopher Okhravi – Builder](https://www.youtube.com/results?search_query=christopher+okhravi+builder+pattern) · [Refactoring Guru](https://refactoring.guru/design-patterns/builder/csharp/example)

---

### 2.4 Prototype
- **Intent:** Create new objects by cloning an existing instance.
- **Problem:** Object creation is expensive or configuration-heavy; you want copies of a preconfigured instance.
- **Structure:** A `Clone()` method (shallow or deep).

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
- **Pros:** Avoids expensive init; convenient for "template" objects.
- **Cons:** Deep vs shallow copy bugs; cloning object graphs with cycles is hard.
- **Use when:** Many near-identical objects from a costly-to-build template.
- **Avoid when:** Objects are cheap to build, or `record` `with` copying suffices.
- 🧭 **Tech-Lead lens:** C# `record` + `with` gives you a compiler-generated (shallow) prototype. Be explicit about deep vs shallow — it's a classic source of shared-reference bugs.

📺 [Refactoring Guru – Prototype (C#)](https://refactoring.guru/design-patterns/prototype/csharp/example)

---

### 2.5 Singleton
- **Intent:** Ensure a class has exactly one instance with a global access point.
- **Problem:** Exactly one shared resource (rare — usually a config or cache).

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
- **Cons:** Global state, hidden dependencies, hard to test/mock, hostile to parallel tests.
- **Use when:** Almost never by hand.
- **Avoid when:** You have a DI container — which you do.
- 🧭 **Tech-Lead lens:** **This is the pattern to challenge in reviews.** Register `services.AddSingleton<T>()` instead — you get single-instance semantics *plus* testability and explicit dependencies. Hand-rolled singletons are usually a design smell.

📺 [Nick Chapsas – Singleton / why DI wins](https://www.youtube.com/results?search_query=nick+chapsas+singleton+dependency+injection) · [Refactoring Guru](https://refactoring.guru/design-patterns/singleton/csharp/example)

---

<a name="3-structural-patterns"></a>
## 3. Structural Patterns
*Compose objects into larger structures while keeping them flexible.*

---

### 3.1 Adapter
- **Intent:** Convert one interface into another the client expects.
- **Problem:** A third-party/legacy class doesn't match your interface.

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
- **Pros:** Integrates incompatible code without touching either side; isolates 3rd-party churn.
- **Cons:** Extra indirection.
- **Use when:** Wrapping external/legacy APIs behind your own contract.
- 🧭 **Tech-Lead lens:** Your primary defense against vendor lock-in. Adapters at the boundary keep the domain clean and swappable.

📺 [Christopher Okhravi – Adapter](https://www.youtube.com/results?search_query=christopher+okhravi+adapter+pattern) · [Refactoring Guru](https://refactoring.guru/design-patterns/adapter/csharp/example)

---

### 3.2 Bridge
- **Intent:** Decouple an abstraction from its implementation so both can vary independently.
- **Problem:** A class hierarchy explodes along **two** independent dimensions (e.g., *Shape* × *Renderer*).

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
- **Pros:** Avoids combinatorial subclass explosion; both axes evolve independently.
- **Cons:** More upfront indirection; harder to grasp.
- **Use when:** Two orthogonal dimensions of variation.
- 🧭 **Tech-Lead lens:** Often confused with Adapter. Adapter = *fix an existing mismatch*; Bridge = *designed up front* to separate two axes.

📺 [Christopher Okhravi – Bridge](https://www.youtube.com/results?search_query=christopher+okhravi+bridge+pattern) · [Refactoring Guru](https://refactoring.guru/design-patterns/bridge/csharp/example)

---

### 3.3 Composite
- **Intent:** Treat individual objects and compositions of objects uniformly (tree structures).
- **Problem:** Clients must handle leaves and containers differently.

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
- **Pros:** Uniform treatment; natural for trees (menus, org charts, file systems, UI).
- **Cons:** Can over-generalize; type safety of leaf vs composite gets blurry.
- **Use when:** Recursive part-whole hierarchies.
- 🧭 **Tech-Lead lens:** Pairs naturally with Visitor for operations over the tree.

📺 [Christopher Okhravi – Composite](https://www.youtube.com/results?search_query=christopher+okhravi+composite+pattern) · [Refactoring Guru](https://refactoring.guru/design-patterns/composite/csharp/example)

---

### 3.4 Decorator
- **Intent:** Attach responsibilities to an object dynamically by wrapping it.
- **Problem:** You need to add behavior (caching, logging, retry) without subclass explosion or editing the original.

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
- **Pros:** Open/Closed; compose behaviors at runtime; each concern isolated.
- **Cons:** Many small wrappers; debugging deep stacks; order matters.
- **Use when:** Cross-cutting concerns layered on the same contract.
- 🧭 **Tech-Lead lens:** One of the *most useful* patterns in real C#. Note: ASP.NET Core middleware and `DelegatingHandler` for `HttpClient` are Decorator/Chain in disguise. Scrutin­ize decorator *ordering* in reviews.

📺 [Christopher Okhravi – Decorator](https://www.youtube.com/results?search_query=christopher+okhravi+decorator+pattern) · [Nick Chapsas – Decorators in .NET](https://www.youtube.com/results?search_query=nick+chapsas+decorator+pattern) · [Refactoring Guru](https://refactoring.guru/design-patterns/decorator/csharp/example)

---

### 3.5 Facade
- **Intent:** Provide a simplified interface over a complex subsystem.
- **Problem:** Clients drown in the details of many collaborating classes.

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
- **Pros:** Simple entry point; reduces coupling to the subsystem.
- **Cons:** Can become a god-object if it accumulates logic.
- **Use when:** Taming a complex subsystem behind a small API.
- 🧭 **Tech-Lead lens:** Application/service layer classes are often Facades. Watch that they *orchestrate*, not *implement* business rules.

📺 [Christopher Okhravi – Facade](https://www.youtube.com/results?search_query=christopher+okhravi+facade+pattern) · [Refactoring Guru](https://refactoring.guru/design-patterns/facade/csharp/example)

---

### 3.6 Flyweight
- **Intent:** Share common state across many objects to save memory.
- **Problem:** Millions of objects with duplicated intrinsic state.
- **Structure:** Split *intrinsic* (shared) from *extrinsic* (per-instance) state; cache the shared part.
- **Use when:** Huge object counts (game particles, glyphs, map tiles).
- **Avoid when:** Object counts are modest — premature optimization.
- 🧭 **Tech-Lead lens:** Rare in typical business apps. C# string interning, `ArrayPool<T>`, and cached immutable value objects are the practical incarnations.

📺 [Refactoring Guru – Flyweight (C#)](https://refactoring.guru/design-patterns/flyweight/csharp/example)

---

### 3.7 Proxy
- **Intent:** A placeholder controlling access to another object (lazy, remote, protection, virtual).
- **Problem:** You need to intercept access — lazy loading, access control, remoting, caching.

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
- **Pros:** Adds control transparently (same interface).
- **Cons:** Looks like Decorator; blurs responsibility if overused.
- **Use when:** Lazy init, access control, remoting, virtual proxies.
- 🧭 **Tech-Lead lens:** EF Core lazy-loading proxies and Castle DynamicProxy (used by many mocking/AOP libs) are this pattern. Decorator *adds behavior*; Proxy *controls access* — same shape, different intent.

📺 [Christopher Okhravi – Proxy](https://www.youtube.com/results?search_query=christopher+okhravi+proxy+pattern) · [Refactoring Guru](https://refactoring.guru/design-patterns/proxy/csharp/example)

---

<a name="4-behavioral-patterns"></a>
## 4. Behavioral Patterns
*Manage algorithms, responsibilities, and communication between objects.*

---

### 4.1 Strategy ⭐ (learn this first)
- **Intent:** Define a family of interchangeable algorithms; select one at runtime.
- **Problem:** Multiple ways to do one thing, chosen dynamically; you want to kill big `switch`/`if` chains.

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
- **Pros:** OCP; testable in isolation; removes conditionals.
- **Cons:** More types; client must know which strategy to pick.
- **Use when:** Interchangeable algorithms/policies.
- 🧭 **Tech-Lead lens:** The workhorse pattern. In C#, a `Func<Order,decimal>` is a lightweight Strategy. Combine with keyed DI (.NET 8) to resolve strategies by key.

📺 [Christopher Okhravi – Strategy](https://www.youtube.com/results?search_query=christopher+okhravi+strategy+pattern) · [Refactoring Guru](https://refactoring.guru/design-patterns/strategy/csharp/example)

---

### 4.2 Observer
- **Intent:** One-to-many dependency; when the subject changes, observers are notified.
- **Problem:** Objects must react to another object's state changes without tight coupling.

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
- **Cons:** Memory leaks from un-unsubscribed handlers; ordering/reentrancy surprises.
- **Use when:** Event-driven notifications.
- 🧭 **Tech-Lead lens:** C# `event`/`Action`, `IObservable<T>`/Rx, and `INotifyPropertyChanged` are all Observer. **Watch for leaked subscriptions** (lapsed listeners) in long-lived objects — a common production bug.

📺 [Christopher Okhravi – Observer](https://www.youtube.com/results?search_query=christopher+okhravi+observer+pattern) · [Refactoring Guru](https://refactoring.guru/design-patterns/observer/csharp/example)

---

### 4.3 Command
- **Intent:** Encapsulate a request as an object (params, undo, queue, log).
- **Problem:** You need to parameterize, queue, log, or undo operations.

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
- **Pros:** Undo/redo, queuing, logging, macro commands; decouples sender from receiver.
- **Cons:** Many small classes.
- **Use when:** Undo/redo, task queues, transactional actions, CQRS commands.
- 🧭 **Tech-Lead lens:** **MediatR** requests/handlers are the Command pattern and are ubiquitous in .NET CQRS. Understand it deeply — you'll review a lot of it.

📺 [Christopher Okhravi – Command](https://www.youtube.com/results?search_query=christopher+okhravi+command+pattern) · [Refactoring Guru](https://refactoring.guru/design-patterns/command/csharp/example)

---

### 4.4 State
- **Intent:** Alter an object's behavior when its internal state changes — it *appears* to change class.
- **Problem:** Behavior depends on state and you have sprawling `switch(state)` blocks.

```csharp
public interface IOrderState { IOrderState Next(); string Status { get; } }

public class PendingState : IOrderState
{ public string Status => "Pending"; public IOrderState Next() => new PaidState(); }

public class PaidState : IOrderState
{ public string Status => "Paid"; public IOrderState Next() => new ShippedState(); }

public class ShippedState : IOrderState
{ public string Status => "Shipped"; public IOrderState Next() => this; }
```
- **Pros:** Removes state conditionals; each state's rules are localized.
- **Cons:** Class per state; transitions can be scattered.
- **Use when:** Well-defined state machines with state-specific behavior.
- 🧭 **Tech-Lead lens:** For complex workflows consider a real state-machine lib (e.g., **Stateless**) instead of hand-rolling. Distinguish State (behavior changes) from Strategy (algorithm choice) — same shape, different intent.

📺 [Christopher Okhravi – State](https://www.youtube.com/results?search_query=christopher+okhravi+state+pattern) · [Refactoring Guru](https://refactoring.guru/design-patterns/state/csharp/example)

---

### 4.5 Template Method
- **Intent:** Define an algorithm's skeleton in a base class, deferring steps to subclasses.
- **Problem:** Several algorithms share structure but differ in specific steps.

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
- **Pros:** Reuse skeleton; enforce invariant order.
- **Cons:** Inheritance-bound; can be rigid.
- **Use when:** Fixed process, variable steps.
- 🧭 **Tech-Lead lens:** Prefer Strategy/composition when you'd otherwise need multiple inheritance or runtime step swapping. Template Method locks you into the class hierarchy.

📺 [Christopher Okhravi – Template Method](https://www.youtube.com/results?search_query=christopher+okhravi+template+method+pattern) · [Refactoring Guru](https://refactoring.guru/design-patterns/template-method/csharp/example)

---

### 4.6 Chain of Responsibility
- **Intent:** Pass a request along a chain of handlers until one handles it.
- **Problem:** Multiple potential handlers; you don't want the sender coupled to a specific one.

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
- **Pros:** Decouples sender/receiver; add/reorder handlers freely.
- **Cons:** Request may go unhandled; harder to trace.
- **Use when:** Pipelines, validation chains, middleware.
- 🧭 **Tech-Lead lens:** **ASP.NET Core middleware** and `HttpClient` `DelegatingHandler`s are Chain of Responsibility. You already use it daily.

📺 [Christopher Okhravi – Chain of Responsibility](https://www.youtube.com/results?search_query=christopher+okhravi+chain+of+responsibility) · [Refactoring Guru](https://refactoring.guru/design-patterns/chain-of-responsibility/csharp/example)

---

### 4.7 Mediator
- **Intent:** Centralize communication between objects so they don't refer to each other directly.
- **Problem:** Many-to-many coupling ("spaghetti" of direct references).
- **Use when:** Complex UI/component interactions, decoupling request→handler.
- 🧭 **Tech-Lead lens:** **MediatR** (in-process) is the canonical .NET example — it's Mediator + Command. Beware over-use: routing trivial calls through a mediator adds indirection without benefit. Discuss this trade-off in design reviews.

📺 [Christopher Okhravi – Mediator](https://www.youtube.com/results?search_query=christopher+okhravi+mediator+pattern) · [Refactoring Guru](https://refactoring.guru/design-patterns/mediator/csharp/example)

---

### 4.8 Iterator
- **Intent:** Sequentially access elements without exposing the underlying representation.
- 🧭 **Tech-Lead lens:** C# gives you this for free: `IEnumerable<T>`/`IEnumerator<T>` and `yield return`. You rarely implement it by hand — but knowing `yield` produces a lazy state machine is senior-level knowledge.

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
- **Intent:** Add operations to an object structure without modifying its classes (double dispatch).
- **Problem:** You need many unrelated operations over a stable class hierarchy.
- **Pros:** New operations without touching element classes.
- **Cons:** Adding a new *element* type forces changing every visitor; verbose.
- **Use when:** Stable element hierarchy, frequently added operations (compilers/ASTs).
- 🧭 **Tech-Lead lens:** Powerful but heavy. In modern C#, **pattern matching / switch expressions** over a sealed type hierarchy often replaces Visitor with far less code.

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
- **Intent:** Capture and restore an object's internal state without violating encapsulation.
- **Use when:** Undo/redo, snapshots, checkpoints.
- 🧭 **Tech-Lead lens:** Often paired with Command for undo. C# `record` snapshots make mementos cheap and immutable.

📺 [Refactoring Guru – Memento (C#)](https://refactoring.guru/design-patterns/memento/csharp/example)

---

### 4.11 Interpreter (rare)
- **Intent:** Define a grammar and interpret sentences in it.
- 🧭 **Tech-Lead lens:** Almost never build by hand — use a parser lib (ANTLR, Sprache, Superpower) or `Expression` trees. Know it exists; don't reach for it.

📺 [Refactoring Guru – Interpreter](https://refactoring.guru/design-patterns/interpreter)

---

<a name="5-modern-csharp"></a>
## 5. Modern C# Idioms & When Patterns "Disappear"

A senior/tech-lead differentiator: knowing which GoF patterns the language now gives you for free.

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

**Rule of thumb for reviews:** If the language or framework already expresses the intent idiomatically, prefer that. Reach for the textbook pattern only when it adds genuine clarity or capability.

📺 [Nick Chapsas channel (modern C#/.NET)](https://www.youtube.com/@nickchapsas) · [Amichai Mantinband (patterns & clean arch in C#)](https://www.youtube.com/@amantinband)

---

<a name="6-tech-lead-playbook"></a>
## 6. The Tech-Lead Playbook: Applying This in Reviews & Design

Beyond knowing patterns, your role is to **govern their use**. Things to actively do:

1. **Name the force, not the pattern.** "This violates OCP because every new payment type edits this switch" lands better than "use Strategy here."
2. **Guard against over-engineering (YAGNI).** The most common junior mistake is applying patterns preemptively. Ask: *"What concrete change does this abstraction make cheaper — and is that change likely?"*
3. **Watch the confusable pairs** (frequent interview & review topics):
   - Strategy vs State (algorithm choice vs behavior-by-state)
   - Adapter vs Bridge (fix mismatch vs designed-in two axes)
   - Decorator vs Proxy (add behavior vs control access)
   - Factory Method vs Abstract Factory (one product vs a family)
4. **Prefer composition + DI.** Most "which pattern?" debates dissolve into "inject an abstraction."
5. **Track pattern-shaped tech debt:** leaked Observer subscriptions, god-object Facades, mediator-for-everything, decorator ordering bugs.
6. **Connect LLD to architecture:** these patterns are the building blocks under Clean/Hexagonal architecture, DDD tactical patterns (Repository, Unit of Work, Aggregate), and CQRS. LLD fluency is what makes your HLD credible.

### Adjacent patterns worth knowing for a tech lead (not GoF)
- **Repository & Unit of Work** (data access abstraction — but don't wrap EF Core needlessly)
- **Options pattern** (`IOptions<T>` config)
- **Result / Either** (error handling without exceptions)
- **Specification pattern** (composable query/business rules)
- **Null Object** (avoid null checks)
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
- 🎥 **Nick Chapsas** – modern, C#-specific, opinionated on when patterns are obsolete: [channel](https://www.youtube.com/@nickchapsas)
- 🎥 **Amichai Mantinband** – patterns + clean architecture in real .NET: [channel](https://www.youtube.com/@amantinband)
- 🎥 **Derek Banas – Design Patterns** (fast overview series): [search](https://www.youtube.com/results?search_query=derek+banas+design+patterns)

### Master written resources
- 📖 **Refactoring Guru** (has C# examples for *every* pattern): https://refactoring.guru/design-patterns/csharp
- 📖 **DoFactory – .NET Design Patterns** (C# reference): https://www.dofactory.com/net/design-patterns
- 📖 *Head First Design Patterns* (concepts) + *Design Patterns* (GoF, the original)
- 📖 *Dependency Injection Principles, Practices, and Patterns* — Seemann & van Deursen (essential for the "DI replaces patterns" mindset)

> **Note on video links:** I've used YouTube *search* links (which always resolve to current results) and stable site links rather than hard-coding individual video URLs that rot over time. Pick the top result from the named creator.

---

*Guide generated for a Senior Engineer / Tech Lead, C# focus. Suggested next step: pick the "daily four" (Strategy, Observer, Decorator, Factory Method), implement each once in a scratch project, then re-read Section 5 to see how you'd do the same thing idiomatically in modern .NET.*
