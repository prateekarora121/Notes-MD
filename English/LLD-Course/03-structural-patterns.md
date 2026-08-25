# Lesson 3 — Structural Patterns

**Structural patterns are about *how you assemble objects and classes into bigger structures* — while keeping everything flexible.**

If creational patterns are about *making* the LEGO bricks, structural patterns are about *snapping them together* in smart ways.

We'll cover seven:
1. **Adapter** — make two incompatible things work together
2. **Decorator** — add features by wrapping (⭐ very useful in real C#)
3. **Facade** — a simple front door over a complex system
4. **Proxy** — a stand-in that controls access
5. **Composite** — treat a tree of objects like a single object
6. **Bridge** — split two dimensions that would otherwise explode
7. **Flyweight** — share data to save memory (rare)

---

## 3.1 — Adapter

### 🎯 The one-line idea
A wrapper that translates one interface into another, so two things that *weren't built to work together* can.

### 🌍 Real-world analogy
A **travel power plug adapter.** Your laptop charger has US pins; the wall socket in Europe is different. You don't rewire your laptop or the building — you put a small adapter in between that translates one shape to the other.

### 😤 The problem
Your app expects everything that logs to fulfill *your* interface:

```csharp
public interface ILogger { void Log(string message); }
```

But you want to use a popular third-party logging library whose method is named differently and takes different arguments — and you **can't change its code**:

```csharp
// Third-party library — you can't edit this.
public class FancyLogLibrary
{
    public void WriteEntry(int severity, string text) { /* ... */ }
}
```

`FancyLogLibrary` doesn't have a `Log(string)` method, so your app can't use it directly.

### ✅ The solution
Write a small adapter that *implements your interface* and, inside, calls the third-party library the way it expects.

```csharp
public class FancyLogAdapter : ILogger        // looks like what YOUR app wants
{
    private readonly FancyLogLibrary _library; // holds the thing being adapted

    public FancyLogAdapter(FancyLogLibrary library) => _library = library;

    public void Log(string message)           // your app calls this...
    {
        _library.WriteEntry(1, message);       // ...and it translates to the library's call
    }
}
```

Usage — the rest of your app has no idea a third-party library is involved:

```csharp
ILogger logger = new FancyLogAdapter(new FancyLogLibrary());
logger.Log("Hello");   // your app speaks its own language; the adapter translates
```

### 🔍 Walkthrough
- The adapter **implements the interface your code wants** (`ILogger`).
- It **holds** an instance of the incompatible class (`FancyLogLibrary`).
- Inside each method, it **translates** your call into the library's call. That's the whole trick.

### ⚖️ Good & bad
**Good:** lets you use libraries/legacy code without changing them or your app; isolates you from third-party churn.
**Bad / avoid when:** you control both sides and could just make them match directly.

### 🧭 Tech-lead note
Adapters are your main defense against **vendor lock-in.** Put third-party SDKs behind your own interfaces at the edges of your system; your core business logic then depends only on *your* contracts and you can swap vendors by rewriting one small adapter.

📺 [Christopher Okhravi – Adapter](https://www.youtube.com/results?search_query=christopher+okhravi+adapter+pattern) · [Refactoring Guru](https://refactoring.guru/design-patterns/adapter/csharp/example)

---

## 3.2 — Decorator ⭐

### 🎯 The one-line idea
Add new behavior to an object by **wrapping it in another object** that shares the same interface — without touching the original class.

### 🌍 Real-world analogy
**Coffee add-ons.** You start with a plain coffee. Wrap it with "add milk." Wrap that with "add caramel." Each wrapper adds cost and description, but the result is *still a coffee* you can drink. You can stack wrappers in any combination.

### 😤 The problem
You have a repository that reads products from a database:

```csharp
public interface IProductRepository { Product Get(int id); }
public class SqlProductRepository : IProductRepository
{
    public Product Get(int id) { Console.WriteLine("Hitting the database..."); return new Product(); }
}
```

Now you want to **add caching** (don't hit the DB twice for the same id). And **logging**. The naive approach is to cram caching and logging code *into* `SqlProductRepository`, but that violates Single Responsibility (Lesson 1) and you'd repeat it for every repository.

### ✅ The solution
Create wrappers that implement the **same interface**, hold an "inner" one, add their bit, and pass the call along.

```csharp
// Caching wrapper
public class CachingProductRepository : IProductRepository
{
    private readonly IProductRepository _inner;                 // the thing being wrapped
    private readonly Dictionary<int, Product> _cache = new();

    public CachingProductRepository(IProductRepository inner) => _inner = inner;

    public Product Get(int id)
    {
        if (_cache.TryGetValue(id, out var cached))
            return cached;                       // serve from cache, skip the inner call

        var product = _inner.Get(id);            // otherwise ask the wrapped repository
        _cache[id] = product;                    // and remember it
        return product;
    }
}

// Logging wrapper
public class LoggingProductRepository : IProductRepository
{
    private readonly IProductRepository _inner;
    public LoggingProductRepository(IProductRepository inner) => _inner = inner;

    public Product Get(int id)
    {
        Console.WriteLine($"Getting product {id}");  // add behavior before...
        var result = _inner.Get(id);
        Console.WriteLine($"Got product {id}");       // ...and after
        return result;
    }
}
```

Now **stack** them like coffee add-ons:

```csharp
IProductRepository repo =
    new LoggingProductRepository(     // outermost: logs
        new CachingProductRepository( // then caches
            new SqlProductRepository()// innermost: real DB work
        ));

repo.Get(1); // logs → checks cache → hits DB → caches → logs
repo.Get(1); // logs → cache hit! → no DB → logs
```

### 🔍 Walkthrough
- Every wrapper **implements the same `IProductRepository`**, so from the outside a wrapped repo looks identical to a plain one. That's why you can stack them endlessly.
- Each wrapper **holds an inner** repository and calls it (`_inner.Get(id)`), adding its own behavior before/after.
- The order you nest them in **matters**: logging-outside-caching logs every call; caching-outside-logging would only log on cache misses.

### ⚖️ Good & bad
**Good:** add/remove features without editing existing classes (Open/Closed); mix and match freely; each concern stays in its own small class (Single Responsibility).
**Bad / avoid when:** deep stacks get hard to debug ("which of the 5 wrappers did that?"), and ordering bugs are easy.

### 🧭 Tech-lead note
This is **one of the most useful patterns in day-to-day C#.** You've used it already:
- **ASP.NET Core middleware** is a decorator/chain over the request.
- **`HttpClient` `DelegatingHandler`s** wrap HTTP calls to add retries, auth, logging.
- The **Scrutor** library lets the DI container auto-wrap services: `services.Decorate<IProductRepository, CachingProductRepository>();`
In reviews, scrutinize **wrapper ordering** — it's the usual source of subtle bugs.

📺 [Christopher Okhravi – Decorator](https://www.youtube.com/results?search_query=christopher+okhravi+decorator+pattern) · [Nick Chapsas – Decorators in .NET](https://www.youtube.com/results?search_query=nick+chapsas+decorator+pattern) · [Refactoring Guru](https://refactoring.guru/design-patterns/decorator/csharp/example)

---

## 3.3 — Facade

### 🎯 The one-line idea
A single, simple class that hides a complicated subsystem behind one easy method.

### 🌍 Real-world analogy
A **restaurant waiter.** You say "I'll have the steak." Behind the scenes the kitchen, grill, sauce station, and plating team all coordinate — but you never talk to them. The waiter is your simple front door to a complex operation.

### 😤 The problem
Placing an order actually involves several subsystems:

```csharp
// The caller has to know and correctly orchestrate ALL of this:
var stock = new InventoryService();
var pay   = new PaymentService();
var ship  = new ShippingService();

if (stock.Reserve(cart)) {
    var receipt = pay.Charge(cart.Total);
    ship.Schedule(cart, receipt);
}
```
Every place that checks out must repeat this dance correctly. That's fragile and leaks complexity everywhere.

### ✅ The solution
Wrap the whole dance in one facade method.

```csharp
public class CheckoutFacade
{
    private readonly InventoryService _inventory;
    private readonly PaymentService _payment;
    private readonly ShippingService _shipping;

    public CheckoutFacade(InventoryService inv, PaymentService pay, ShippingService ship)
        => (_inventory, _payment, _shipping) = (inv, pay, ship);

    public bool Checkout(Cart cart)          // one simple front door
    {
        if (!_inventory.Reserve(cart)) return false;
        var receipt = _payment.Charge(cart.Total);
        _shipping.Schedule(cart, receipt);
        return true;
    }
}
```

Now callers just do:

```csharp
bool ok = checkout.Checkout(cart);   // that's it
```

### 🔍 Walkthrough
The facade *holds* the subsystems and *coordinates* them in the right order. Callers get one method and are shielded from the complexity.

### ⚖️ Good & bad
**Good:** dramatically simpler for callers; complexity lives in one place.
**Bad / avoid when:** the facade grows into a "god object" that does everything. Keep it as a *coordinator*, not a dumping ground for business rules.

### 🧭 Tech-lead note
The "service" or "application" layer in most apps is essentially a set of facades. The healthy rule: a facade should **orchestrate** (call things in order) but not **implement** deep business logic itself.

📺 [Christopher Okhravi – Facade](https://www.youtube.com/results?search_query=christopher+okhravi+facade+pattern) · [Refactoring Guru](https://refactoring.guru/design-patterns/facade/csharp/example)

---

## 3.4 — Proxy

### 🎯 The one-line idea
A stand-in object that looks identical to the real one but **controls access** to it (adds security, delays loading, caches, etc.).

### 🌍 Real-world analogy
A **celebrity's personal assistant.** You can't reach the celebrity directly. You talk to the assistant, who checks if you're allowed, and only *then* passes your message along. From your side it feels like talking to the celebrity.

### 😤 The problem
You have a service that generates sensitive reports. Not everyone should be allowed to run it, but you don't want to scatter permission checks everywhere or bloat the report class with security logic.

### ✅ The solution
A proxy that implements the same interface, checks access first, then delegates to the real object.

```csharp
public interface IReportService { string Generate(int reportId); }

public class RealReportService : IReportService
{
    public string Generate(int id) => $"Report #{id} data...";  // the expensive/sensitive real work
}

public class ReportServiceProxy : IReportService
{
    private readonly IReportService _real;
    private readonly IUserContext _user;

    public ReportServiceProxy(IReportService real, IUserContext user)
        => (_real, _user) = (real, user);

    public string Generate(int id)
    {
        if (!_user.HasPermission("reports.read"))          // access control...
            throw new UnauthorizedAccessException();
        return _real.Generate(id);                          // ...then delegate to the real one
    }
}
```

### 🔍 Walkthrough
Same interface as the real service, so callers can't tell the difference. The proxy adds a gate (`HasPermission`) before letting the call through. Other proxy flavors do lazy-loading ("don't build the heavy object until first use") or caching.

### ⚖️ Good & bad
**Good:** transparently adds access control, lazy-loading, or caching without changing the real class or the caller.
**Bad / avoid when:** it's easy to overuse and blur responsibilities.

### 🔁 Proxy vs Decorator (they look identical!)
Same shape — both wrap an object with the same interface. The difference is **intent**:
- **Decorator** *adds new behavior/features* (caching, logging as a feature).
- **Proxy** *controls access* to the object (security, lazy-loading, remoting).

### 🧭 Tech-lead note
Entity Framework Core's lazy-loading proxies and the dynamic proxies behind most mocking libraries (Moq, NSubstitute) are this pattern in action. You use proxies constantly without writing them.

📺 [Christopher Okhravi – Proxy](https://www.youtube.com/results?search_query=christopher+okhravi+proxy+pattern) · [Refactoring Guru](https://refactoring.guru/design-patterns/proxy/csharp/example)

---

## 3.5 — Composite

### 🎯 The one-line idea
Treat a **tree of objects** (a whole and its parts) the same way you'd treat a single object.

### 🌍 Real-world analogy
**Folders on your computer.** A folder can contain files *and* other folders. When you ask "how big is this folder?", it figures out its size by adding up everything inside — files and sub-folders alike — without you caring how deep it goes.

### 😤 The problem
You have single items (files) and groups (folders), and you keep writing different code for "is this a single thing or a group?" every time you want the total size.

### ✅ The solution
Make both the single item and the group implement the **same interface**. The group's implementation just loops over its children (which might themselves be groups).

```csharp
public interface IFileSystemItem { long GetSizeBytes(); }

// A leaf (single item)
public class FileItem : IFileSystemItem
{
    private readonly long _size;
    public FileItem(long size) => _size = size;
    public long GetSizeBytes() => _size;
}

// A composite (a group that can contain leaves AND other groups)
public class FolderItem : IFileSystemItem
{
    private readonly List<IFileSystemItem> _children = new();
    public void Add(IFileSystemItem item) => _children.Add(item);

    public long GetSizeBytes() => _children.Sum(child => child.GetSizeBytes()); // recursion!
}
```

Usage — nested as deep as you like, but you call it the same way:

```csharp
var root = new FolderItem();
root.Add(new FileItem(100));
var sub = new FolderItem();
sub.Add(new FileItem(50));
root.Add(sub);

long total = root.GetSizeBytes(); // 150 — the folder handled the recursion for you
```

### 🔍 Walkthrough
The key line is `_children.Sum(child => child.GetSizeBytes())`. Because each child is an `IFileSystemItem`, it might be a file (returns its size) *or* a folder (which recurses again). You wrote the logic once and it works for any depth.

### ⚖️ Good & bad
**Good:** clean handling of tree structures (menus, org charts, UI elements, file systems); client code doesn't special-case leaves vs groups.
**Bad / avoid when:** your data isn't really a tree — don't force it.

### 🧭 Tech-lead note
Pairs naturally with the **Visitor** pattern (Lesson 4) when you need many different operations over the same tree.

📺 [Christopher Okhravi – Composite](https://www.youtube.com/results?search_query=christopher+okhravi+composite+pattern) · [Refactoring Guru](https://refactoring.guru/design-patterns/composite/csharp/example)

---

## 3.6 — Bridge

### 🎯 The one-line idea
When a design varies along **two independent dimensions**, split them into two separate hierarchies so they don't multiply into a mess.

### 🌍 Real-world analogy
**A TV and its remote.** There are many TV brands *and* many remote types. You don't build a separate remote for every single TV brand (Sony-remote, LG-remote, Samsung-remote × basic/universal/voice…). Instead, remotes talk to any TV through a standard contract. The two can evolve separately.

### 😤 The problem
You have `Shape`s (Circle, Square) and each must draw in two ways: as vector or as raster. If you make a class per combination — `VectorCircle`, `RasterCircle`, `VectorSquare`, `RasterSquare` — you get an explosion. Add a Triangle and you need two more. Add a third rendering mode and you need one per shape. It multiplies: shapes × renderers.

### ✅ The solution
Separate "what to draw" (Shape) from "how to draw" (Renderer). The Shape *holds* a Renderer.

```csharp
// Dimension 1: how to render
public interface IRenderer { void DrawCircle(float radius); }
public class VectorRenderer : IRenderer { public void DrawCircle(float r) => Console.WriteLine($"Vector circle r={r}"); }
public class RasterRenderer : IRenderer { public void DrawCircle(float r) => Console.WriteLine($"Pixels circle r={r}"); }

// Dimension 2: what shape
public abstract class Shape
{
    protected readonly IRenderer Renderer;      // the "bridge" to the other dimension
    protected Shape(IRenderer renderer) => Renderer = renderer;
    public abstract void Draw();
}

public class Circle : Shape
{
    private readonly float _radius;
    public Circle(IRenderer renderer, float radius) : base(renderer) => _radius = radius;
    public override void Draw() => Renderer.DrawCircle(_radius);
}
```

Usage — combine the two dimensions freely, no combinatorial classes:

```csharp
new Circle(new VectorRenderer(), 5).Draw();  // vector circle
new Circle(new RasterRenderer(), 5).Draw();  // raster circle
// Add a Square class OR a new Renderer — each is ONE new class, not one-per-combination.
```

### 🔍 Walkthrough
The `Shape` doesn't know *how* rendering happens — it just calls `Renderer`. Shapes and renderers grow independently. That's the "bridge": the `Renderer` field connecting the two hierarchies.

### ⚖️ Good & bad
**Good:** avoids class explosion when you have two independent axes of change.
**Bad / avoid when:** you only really vary along one dimension — then it's needless indirection.

### 🔁 Bridge vs Adapter
- **Adapter** fixes a mismatch *after the fact* (two things that already exist don't fit).
- **Bridge** is *designed up front* to keep two dimensions separate.

📺 [Christopher Okhravi – Bridge](https://www.youtube.com/results?search_query=christopher+okhravi+bridge+pattern) · [Refactoring Guru](https://refactoring.guru/design-patterns/bridge/csharp/example)

---

## 3.7 — Flyweight (rare — skim this)

### 🎯 The one-line idea
When you have a *huge* number of similar objects, **share the parts they have in common** to save memory.

### 🌍 Real-world analogy
A **book's letters.** A page has thousands of the letter "e," but the printer doesn't store a unique description of "e" thousands of times — it reuses one definition of "e" and just places it in many positions.

### The idea in brief
Split each object's data into:
- **Intrinsic** state — shared, unchanging (e.g., a tree's texture/model in a game).
- **Extrinsic** state — unique per object (e.g., each tree's x/y position).

Store one shared copy of the intrinsic part and hand it to all the objects, keeping only the extrinsic part per-object. A forest of 1,000,000 trees stores the tree model *once*.

### ⚖️ When
Only when you genuinely have millions of objects and memory is a real problem. Otherwise it's premature optimization — skip it.

### 🧭 Tech-lead note
In everyday C# this shows up as **string interning**, cached immutable value objects, and `ArrayPool<T>` for reusing buffers. You rarely implement the classic pattern by hand in business apps.

📺 [Refactoring Guru – Flyweight (C#)](https://refactoring.guru/design-patterns/flyweight/csharp/example)

---

## ✅ Lesson 3 recap

| Pattern | In one line | You've already used it as… |
|---|---|---|
| Adapter | Translate one interface to another | Wrapping any third-party SDK |
| **Decorator** ⭐ | Add features by wrapping | ASP.NET middleware, HttpClient handlers |
| Facade | Simple front door over complexity | Your service/application layer |
| Proxy | A stand-in that controls access | EF Core lazy loading, mocking libraries |
| Composite | Treat a tree like a single object | File systems, menus, UI trees |
| Bridge | Separate two independent dimensions | (designed up front) |
| Flyweight | Share data across many objects | String interning, `ArrayPool<T>` |

**Remember the confusing trio:** Adapter (*fix a mismatch*), Decorator (*add behavior*), Proxy (*control access*) all wrap an object — the difference is **why**.

➡️ Next: **`04-behavioral-patterns.md`** — the biggest and most useful group: how objects *behave* and *talk to each other*.
