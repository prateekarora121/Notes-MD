# Lesson 3 — Structural Patterns

**Structural patterns iske baare mein hain ki *aap objects aur classes ko bade structures mein kaise assemble karte ho* — while sab kuch flexible rakhte hue.**

Agar creational patterns LEGO bricks *banane* ke baare mein hain, toh structural patterns unhe smart ways mein *snap together karne* ke baare mein hain.

Hum saat cover karenge:
1. **Adapter** — do incompatible things ko saath kaam karwao
2. **Decorator** — wrapping ke through features add karo (⭐ real C# mein bahut useful)
3. **Facade** — ek complex system ke upar ek simple front door
4. **Proxy** — ek stand-in jo access control karta hai
5. **Composite** — objects ke tree ko ek single object jaisa treat karo
6. **Bridge** — do dimensions ko split karo jo otherwise explode ho jaate
7. **Flyweight** — memory bachane ke liye data share karo (rare)

---

## 3.1 — Adapter

### 🎯 One-line idea
Ek wrapper jo ek interface ko doosre mein translate karta hai, taaki do cheezein jo *saath kaam karne ke liye nahi banayi gayi thi* woh kaam kar sakein.

### 🌍 Real-world analogy
Ek **travel power plug adapter.** Aapke laptop charger mein US pins hain; Europe mein wall socket different hai. Aap laptop ya building ko rewire nahi karte — aap beech mein ek small adapter daal dete ho jo ek shape ko doosre mein translate kar deta hai.

### 😤 Problem
Aapki app expect karti hai ki jo bhi log karta hai woh *aapke* interface ko fulfill kare:

```csharp
public interface ILogger { void Log(string message); }
```

Lekin aap ek popular third-party logging library use karna chahte ho jiska method differently naam ka hai aur different arguments leta hai — aur aap **iska code change nahi kar sakte**:

```csharp
// Third-party library — you can't edit this.
public class FancyLogLibrary
{
    public void WriteEntry(int severity, string text) { /* ... */ }
}
```

`FancyLogLibrary` ke paas `Log(string)` method nahi hai, isliye aapki app ise directly use nahi kar sakti.

### ✅ Solution
Ek chota adapter likho jo *aapka interface implement kare* aur, andar, third-party library ko waise call kare jaise woh expect karti hai.

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

Usage — aapki app ke rest ko koi idea nahi ki ek third-party library involved hai:

```csharp
ILogger logger = new FancyLogAdapter(new FancyLogLibrary());
logger.Log("Hello");   // your app speaks its own language; the adapter translates
```

### 🔍 Walkthrough
- Adapter **aapke code ka wanted interface implement karta hai** (`ILogger`).
- Yeh incompatible class ka instance **hold** karta hai (`FancyLogLibrary`).
- Har method ke andar, yeh aapki call ko library ki call mein **translate** karta hai. Yehi poora trick hai.

### ⚖️ Fayde & Nuksan
**Fayda:** libraries/legacy code use karne dete ho unhe ya apni app ko change kiye bina; third-party churn se isolate karta hai.
**Nuksan / avoid karo jab:** aap dono sides control karte ho aur unhe directly match bhi kar sakte ho.

### 🧭 Tech-lead note
Adapters aapka main defense hain **vendor lock-in** ke against. Third-party SDKs ko apni system ke edges par apne khud ke interfaces ke peeche daalo; aapka core business logic phir sirf *aapke* contracts par depend karta hai aur aap ek chote adapter ko rewrite karke vendors swap kar sakte ho.

📺 [Christopher Okhravi – Adapter](https://www.youtube.com/results?search_query=christopher+okhravi+adapter+pattern) · [Refactoring Guru](https://refactoring.guru/design-patterns/adapter/csharp/example)

---

## 3.2 — Decorator ⭐

### 🎯 One-line idea
Ek object mein naya behavior add karo use **doosre object mein wrap karke** jo same interface share karta hai — original class ko touch kiye bina.

### 🌍 Real-world analogy
**Coffee add-ons.** Aap ek plain coffee se start karte ho. Ise "add milk" mein wrap karo. Usse "add caramel" mein wrap karo. Har wrapper cost aur description add karta hai, lekin result *still a coffee* hai jo aap peh sakte ho. Aap wrappers ko kisi bhi combination mein stack kar sakte ho.

### 😤 Problem
Aapke paas ek repository hai jo database se products read karti hai:

```csharp
public interface IProductRepository { Product Get(int id); }
public class SqlProductRepository : IProductRepository
{
    public Product Get(int id) { Console.WriteLine("Hitting the database..."); return new Product(); }
}
```

Ab aap **caching add** karna chahte ho (same id ke liye DB ko do baar hit na karo). Aur **logging**. Naive approach yeh hai ki caching aur logging code ko `SqlProductRepository` *mein* cram kar do, lekin yeh Single Responsibility (Lesson 1) violate karta hai aur aapko har repository ke liye ise repeat karna padega.

### ✅ Solution
Wrappers banao jo **same interface** implement karte hain, ek "inner" one hold karte hain, apna bit add karte hain, aur call ko aage pass karte hain.

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

Ab inhe coffee add-ons jaise **stack** karo:

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
- Har wrapper **same `IProductRepository` implement karta hai**, isliye outside se ek wrapped repo bilkul plain repo jaisa dikhta hai. Isi liye aap unhe endlessly stack kar sakte ho.
- Har wrapper **ek inner** repository hold karta hai aur use call karta hai (`_inner.Get(id)`), apna behavior before/after add karte hue.
- Jis order mein aap unhe nest karte ho woh **matter karta hai**: logging-outside-caching har call ko log karta hai; caching-outside-logging sirf cache misses par log karega.

### ⚖️ Fayde & Nuksan
**Fayda:** existing classes edit kiye bina features add/remove karo (Open/Closed); freely mix and match karo; har concern apni chhoti class mein rehta hai (Single Responsibility).
**Nuksan / avoid karo jab:** deep stacks debug karna hard ho jaata hai ("5 wrappers mein se kisne yeh kiya?"), aur ordering bugs easy hain.

### 🧭 Tech-lead note
Yeh **day-to-day C# mein sabse useful patterns mein se ek hai.** Aap ise already use kar chuke ho:
- **ASP.NET Core middleware** request ke upar ek decorator/chain hai.
- **`HttpClient` `DelegatingHandler`s** HTTP calls ko wrap karte hain retries, auth, logging add karne ke liye.
- **Scrutor** library DI container ko services auto-wrap karne deti hai: `services.Decorate<IProductRepository, CachingProductRepository>();`
Reviews mein, **wrapper ordering** ko scrutinize karo — yeh usually subtle bugs ka source hota hai.

📺 [Christopher Okhravi – Decorator](https://www.youtube.com/results?search_query=christopher+okhravi+decorator+pattern) · [Nick Chapsas – Decorators in .NET](https://www.youtube.com/results?search_query=nick+chapsas+decorator+pattern) · [Refactoring Guru](https://refactoring.guru/design-patterns/decorator/csharp/example)

---

## 3.3 — Facade

### 🎯 One-line idea
Ek single, simple class jo ek complicated subsystem ko ek easy method ke peeche hide kar deti hai.

### 🌍 Real-world analogy
Ek **restaurant waiter.** Aap kehte ho "I'll have the steak." Behind the scenes kitchen, grill, sauce station, aur plating team sab coordinate karte hain — lekin aap unse kabhi baat nahi karte. Waiter aapka simple front door hai ek complex operation ke liye.

### 😤 Problem
Ek order place karne mein actually several subsystems involved hain:

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
Har jagah jahan checkout hota hai usse yeh dance correctly repeat karna padta hai. Yeh fragile hai aur complexity ko everywhere leak karta hai.

### ✅ Solution
Poore dance ko ek facade method mein wrap kar do.

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

Ab callers sirf yeh karte hain:

```csharp
bool ok = checkout.Checkout(cart);   // that's it
```

### 🔍 Walkthrough
Facade subsystems ko *hold* karta hai aur unhe right order mein *coordinate* karta hai. Callers ko ek method milta hai aur woh complexity se shielded rehte hain.

### ⚖️ Fayde & Nuksan
**Fayda:** callers ke liye dramatically simpler; complexity ek jagah rehti hai.
**Nuksan / avoid karo jab:** facade ek "god object" ban jaaye jo sab kuch karta hai. Ise ek *coordinator* rakho, business rules ka dumping ground nahi.

### 🧭 Tech-lead note
Most apps mein "service" ya "application" layer essentially facades ka ek set hai. Healthy rule: ek facade **orchestrate** karna chahiye (cheezein order mein call karna) lekin deep business logic khud **implement** nahi karna chahiye.

📺 [Christopher Okhravi – Facade](https://www.youtube.com/results?search_query=christopher+okhravi+facade+pattern) · [Refactoring Guru](https://refactoring.guru/design-patterns/facade/csharp/example)

---

## 3.4 — Proxy

### 🎯 One-line idea
Ek stand-in object jo real object jaisa hi dikhta hai lekin uske **access ko control karta hai** (security, delayed loading, caching, etc. add karta hai).

### 🌍 Real-world analogy
Ek **celebrity's personal assistant.** Aap celebrity tak directly nahi pahunch sakte. Aap assistant se baat karte ho, jo check karta hai ki aap allowed ho ya nahi, aur tab hi aapka message aage pass karta hai. Aapki side se yeh celebrity se baat karne jaisa feel hota hai.

### 😤 Problem
Aapke paas ek service hai jo sensitive reports generate karti hai. Sabko ise run karne ki permission nahi honi chahiye, lekin aap permission checks ko everywhere scatter nahi karna chahte ya report class ko security logic se bloat nahi karna chahte.

### ✅ Solution
Ek proxy jo same interface implement karta hai, pehle access check karta hai, phir real object ko delegate karta hai.

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
Real service jaisa hi interface, isliye callers difference nahi bata sakte. Proxy call ko through hone se pehle ek gate add karta hai (`HasPermission`). Doosre proxy flavors lazy-loading karte hain ("heavy object ko first use tak build mat karo") ya caching.

### ⚖️ Fayde & Nuksan
**Fayda:** real class ya caller ko change kiye bina transparently access control, lazy-loading, ya caching add karta hai.
**Nuksan / avoid karo jab:** ise overuse karna easy hai aur responsibilities blur ho jaati hain.

### 🔁 Proxy vs Decorator (yeh identical dikhte hain!)
Same shape — dono same interface ke saath ek object ko wrap karte hain. Difference **intent** mein hai:
- **Decorator** *naya behavior/features add karta hai* (caching, logging as a feature).
- **Proxy** object ke *access ko control karta hai* (security, lazy-loading, remoting).

### 🧭 Tech-lead note
Entity Framework Core ke lazy-loading proxies aur most mocking libraries (Moq, NSubstitute) ke peeche ke dynamic proxies yeh pattern hi action mein hai. Aap proxies constantly use karte ho unhe likhe bina.

📺 [Christopher Okhravi – Proxy](https://www.youtube.com/results?search_query=christopher+okhravi+proxy+pattern) · [Refactoring Guru](https://refactoring.guru/design-patterns/proxy/csharp/example)

---

## 3.5 — Composite

### 🎯 One-line idea
Objects ke ek **tree** (a whole and its parts) ko waise hi treat karo jaise aap ek single object treat karte.

### 🌍 Real-world analogy
**Aapke computer par folders.** Ek folder mein files *aur* doosre folders bhi ho sakte hain. Jab aap poochte ho "yeh folder kitna bada hai?", yeh apna size figure out karta hai andar ki sab cheezein add karke — files aur sub-folders sab, aap iski chinta kiye bina ki yeh kitna deep jaata hai.

### 😤 Problem
Aapke paas single items (files) aur groups (folders) hain, aur aap har baar different code likhte rehte ho "yeh single thing hai ya group?" jab bhi aapko total size chahiye.

### ✅ Solution
Single item aur group dono ko **same interface** implement karwao. Group ka implementation apne children ke upar loop karta hai (jo khud groups bhi ho sakte hain).

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

Usage — jitna deep chahiye nested karo, lekin call same tarah karte ho:

```csharp
var root = new FolderItem();
root.Add(new FileItem(100));
var sub = new FolderItem();
sub.Add(new FileItem(50));
root.Add(sub);

long total = root.GetSizeBytes(); // 150 — the folder handled the recursion for you
```

### 🔍 Walkthrough
Key line hai `_children.Sum(child => child.GetSizeBytes())`. Kyunki har child ek `IFileSystemItem` hai, yeh ek file ho sakta hai (apna size return karta hai) *ya* ek folder (jo phir se recurse karta hai). Aapne logic ek baar likha aur yeh kisi bhi depth ke liye kaam karta hai.

### ⚖️ Fayde & Nuksan
**Fayda:** tree structures (menus, org charts, UI elements, file systems) ki clean handling; client code leaves vs groups ke liye special-case nahi karta.
**Nuksan / avoid karo jab:** aapka data really ek tree nahi hai — force mat karo.

### 🧭 Tech-lead note
Yeh naturally **Visitor** pattern (Lesson 4) ke saath pairs karta hai jab aapko same tree par many different operations chahiye ho.

📺 [Christopher Okhravi – Composite](https://www.youtube.com/results?search_query=christopher+okhravi+composite+pattern) · [Refactoring Guru](https://refactoring.guru/design-patterns/composite/csharp/example)

---

## 3.6 — Bridge

### 🎯 One-line idea
Jab ek design **do independent dimensions** ke along vary karta hai, unhe do separate hierarchies mein split kar do taaki woh multiply ho kar mess na banaye.

### 🌍 Real-world analogy
**Ek TV aur uska remote.** Kaafi TV brands *aur* kaafi remote types hain. Aap har single TV brand ke liye ek separate remote nahi banate (Sony-remote, LG-remote, Samsung-remote × basic/universal/voice…). Instead, remotes kisi bhi TV se ek standard contract ke through baat karte hain. Dono independently evolve kar sakte hain.

### 😤 Problem
Aapke paas `Shape`s hain (Circle, Square) aur har ek ko do ways mein draw karna hai: vector ya raster ke roop mein. Agar aap har combination ke liye ek class banao — `VectorCircle`, `RasterCircle`, `VectorSquare`, `RasterSquare` — toh explosion ho jaata hai. Triangle add karo aur do aur classes chahiye. Ek third rendering mode add karo aur har shape ke liye ek chahiye. Yeh multiply hota hai: shapes × renderers.

### ✅ Solution
"Kya draw karna hai" (Shape) ko "kaise draw karna hai" (Renderer) se separate karo. Shape ek Renderer *hold* karta hai.

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

Usage — dono dimensions ko freely combine karo, koi combinatorial classes nahi:

```csharp
new Circle(new VectorRenderer(), 5).Draw();  // vector circle
new Circle(new RasterRenderer(), 5).Draw();  // raster circle
// Add a Square class OR a new Renderer — each is ONE new class, not one-per-combination.
```

### 🔍 Walkthrough
`Shape` ko nahi pata *kaise* rendering hota hai — yeh sirf `Renderer` ko call karta hai. Shapes aur renderers independently grow karte hain. Yehi hai "bridge": `Renderer` field jo dono hierarchies ko connect karta hai.

### ⚖️ Fayde & Nuksan
**Fayda:** do independent axes of change hone par class explosion avoid karta hai.
**Nuksan / avoid karo jab:** aap really sirf ek dimension ke along vary karte ho — tab yeh needless indirection hai.

### 🔁 Bridge vs Adapter
- **Adapter** ek mismatch ko *after the fact* fix karta hai (do cheezein jo already exist karti hain woh fit nahi hoti).
- **Bridge** *designed up front* hota hai do dimensions ko separate rakhne ke liye.

📺 [Christopher Okhravi – Bridge](https://www.youtube.com/results?search_query=christopher+okhravi+bridge+pattern) · [Refactoring Guru](https://refactoring.guru/design-patterns/bridge/csharp/example)

---

## 3.7 — Flyweight (rare — isko skim karo)

### 🎯 One-line idea
Jab aapke paas *huge* number of similar objects hain, unke common parts ko **share karo** memory bachane ke liye.

### 🌍 Real-world analogy
Ek **book ke letters.** Ek page par letter "e" ke thousands hote hain, lekin printer "e" ki unique description ko thousands times store nahi karta — yeh "e" ki ek definition reuse karta hai aur ise sirf many positions mein place karta hai.

### Idea briefly
Har object ka data split karo:
- **Intrinsic** state — shared, unchanging (e.g., ek tree ka texture/model ek game mein).
- **Extrinsic** state — har object ke liye unique (e.g., har tree ka x/y position).

Intrinsic part ki ek shared copy store karo aur sab objects ko de do, sirf extrinsic part ko per-object rakhte hue. 1,000,000 trees ka ek forest tree model ko *ek baar* store karta hai.

### ⚖️ Kab
Sirf jab aapke paas genuinely millions of objects hain aur memory ek real problem hai. Otherwise yeh premature optimization hai — skip karo.

### 🧭 Tech-lead note
Everyday C# mein yeh **string interning**, cached immutable value objects, aur buffers reuse karne ke liye `ArrayPool<T>` ke roop mein dikhta hai. Business apps mein aap classic pattern ko rarely hand se implement karte ho.

📺 [Refactoring Guru – Flyweight (C#)](https://refactoring.guru/design-patterns/flyweight/csharp/example)

---

## ✅ Lesson 3 recap

| Pattern | Ek line mein | Aap already use kar chuke ho iska use... |
|---|---|---|
| Adapter | Ek interface ko doosre mein translate karo | Kisi bhi third-party SDK ko wrap karna |
| **Decorator** ⭐ | Wrapping karke features add karo | ASP.NET middleware, HttpClient handlers |
| Facade | Complexity ke upar simple front door | Aapki service/application layer |
| Proxy | Ek stand-in jo access control karta hai | EF Core lazy loading, mocking libraries |
| Composite | Ek tree ko single object jaisa treat karo | File systems, menus, UI trees |
| Bridge | Do independent dimensions separate karo | (designed up front) |
| Flyweight | Many objects ke across data share karo | String interning, `ArrayPool<T>` |

**Confusing trio yaad rakhna:** Adapter (*mismatch fix karna*), Decorator (*behavior add karna*), Proxy (*access control karna*) sab ek object ko wrap karte hain — difference **kyun** mein hai.

➡️ Next: **`04-behavioral-patterns.md`** — sabse bada aur sabse useful group: objects *kaise behave* karte hain aur ek doosre se *baat* karte hain.
