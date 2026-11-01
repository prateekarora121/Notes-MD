# Interview Prep Master — Consolidated

> **Maqsad:** Teen source files — *Interview Prep Master Q&A – Hinglish*, *NET Full Stack Interview Q&A Sheet*, aur *Full-Stack .NET Interview Prep Sheet (10 YOE)* — ko ek jagah organize kiya gaya hai. Topics by subject hain, source ke mutabik nahin.
>
> **Answer format:** Pehle 1–2 line ka direct answer ya short spoken version, phir depth/reasoning, phir trade-off/pitfall, phir practical scenario ya code example, phir followed-up follow-ups. `[apna example]` ko apne real experience se replace karein; numbers, ownership, outcomes invent na karein.
>
> **Kaise padhein:** Senior interviews mein definition ke baad trade-offs, production behavior, failure modes aur debugging par drill hoti hai. "Commonly asked" ka matlab preparation themes hain, kisi specific interviewer ka guarantee nahin.

---

## Table of Contents

1. [C# & .NET Fundamentals](#1-c--net-fundamentals)
2. [OOP, SOLID & Design Patterns](#2-oop-solid--design-patterns)
3. [ADO.NET, LINQ & Data Access](#3-adonet-linq--data-access)
4. [ASP.NET Core & Web API](#4-aspnet-core--web-api)
5. [EF Core & Architecture](#5-ef-core--architecture)
6. [Authentication, Authorization & Security](#6-authentication-authorization--security)
7. [SQL Server](#7-sql-server)
8. [Microservices, Messaging & Migration](#8-microservices-messaging--migration)
9. [System Design & Performance](#9-system-design--performance)
10. [Logging, Monitoring & Observability](#10-logging-monitoring--observability)
11. [AWS](#11-aws)
12. [Angular](#12-angular)
13. [TypeScript](#13-typescript)
14. [JavaScript & Runtime](#14-javascript--runtime)
15. [GitHub Actions, Deployment & Artifactory](#15-github-actions-deployment--artifactory)
16. [Testing, BDD & Compliance](#16-testing-bdd--compliance)
17. [Agile & Team Collaboration](#17-agile--team-collaboration)
18. [Coding, DSA & Problem Solving](#18-coding-dsa--problem-solving)
19. [Behavioral, Scenarios & Resume Deep Dive](#19-behavioral-scenarios--resume-deep-dive)
20. [Company Interview Reports & Practice](#20-company-interview-reports--practice)
21. [Coverage Audit](#21-coverage-audit)
22. [Quick Cheat Sheet](#22-quick-cheat-sheet)
23. [References & Research Limits](#23-references--research-limits)

---

## 1. C# & .NET Fundamentals

### Q1.1 C# code execute kaise hota hai? CLR, IL aur JIT ka role kya hai?

**Short answer:** C# compiler source ko Intermediate Language (IL) aur metadata wale assembly mein compile karta hai. Runtime par CLR assembly load karta hai; JIT hot methods ke IL ko current machine ke native code mein compile karta hai.

**Answer:** CLR type safety, exceptions, garbage collection aur threading jaise services deta hai. JIT sirf hot methods compile karta hai — AOT publishing kuch ya saara compilation deployment se pehle kar sakta hai.

**Pitfall:** "C# hamesha interpreter se chalta hai" ya "JIT poora app startup par ek hi baar compile karta hai" — dono oversimplifications hain. Tiered compilation, runtime version aur deployment model behavior badal sakte hain.

**Scenario:** Cold-start sensitive serverless service mein startup aur memory important hain — AOT consider karein, lekin reflection/dynamic-code limitations, compatibility aur publish size validate karke.

**Follow-ups**
- **JIT ka fayda?** Runtime profiling ke baad hot code optimize ho sakta hai; cost startup par compilation aur runtime dependency hai.
- **Managed code kya hai?** Aisa code jo CLR ke runtime services ke under execute hota hai; unmanaged native code ko memory/resources ke liye explicit lifetime management chahiye.
- **CLI/CTS/CLS?** CLI standard/runtime model; CTS common type system; CLS cross-language compatibility subset.

---

### Q1.2 Value type aur reference type mein kya antar hai?

**Short answer:** Value types (`int`, `struct`, `enum`, `bool`) apna data directly hold karte hain — assignment par data copy hota hai. Reference types (`class`, `interface`, `delegate`, `string`, arrays) heap object ka reference hold karte hain — assignment par reference copy hota hai, object nahin.

**Answer:** Value type variable mein value khud hoti hai; assignment/call ke waqt value semantics ke mutabik copy ho sakti hai. Reference type variable object ka reference rakhta hai; reference copy karne par dono variables same object ko point karte hain.

**Important nuance — "stack vs heap" oversimplification:** Actual storage location sirf "stack vs heap" se decide nahin hoti — JIT, object layout, aur lifetime matter karte hain. Ek `struct` field jo kisi `class` instance ke andar ho, woh heap par store hoti hai us class ke saath. Interview mein "value types always live on the stack" bolna galat impression deta hai.

```csharp
struct P { public int X; }
var a = new P { X = 1 }; var b = a; b.X = 2;   // a.X still 1 — copy semantics

class C { public int X; }
var c1 = new C { X = 1 }; var c2 = c1; c2.X = 2; // c1.X is now 2 — shared reference
```

**`record` class vs `record struct`:** `record` reference type hai value-oriented equality ke saath; `record struct` value type hai value equality ke saath. Dono automatically immutable nahin hote — `init`/`readonly` aur exposed mutable members check karein.

**Boxing / Unboxing:** Value type ko `object` ya compatible interface reference ke roop mein use karne par boxing hoti hai — heap allocation trigger hoti hai. Hot paths mein yeh costly hai; generics (`List<T>`) boxing avoid karte hain.

**`struct` kab use karein:** Small (~16 bytes), immutable, aur short-lived ho (`readonly struct`, `Span<T>`, `record struct`). Mutable, large structs copy-cost aur confusing semantics dete hain.

**`string` special case:** `string` reference type hai lekin immutable hai aur value-like equality semantics deta hai. Repeated concatenation loop mein `StringBuilder` use karein; few values ke liye simple `+` behtar readability deta hai.

**Pitfall:** "Struct hamesha stack par aur class hamesha heap par" — yeh interview mein oversimplification hai.

**Follow-ups**
- **Boxing kya hai?** Value type ko `object` reference ke roop mein use karne par; hot path mein allocations inspect karein.
- **`StringBuilder` sealed hai?** Haan, .NET mein `StringBuilder` sealed class hai.
- **`record` ka `with` expression?** Shallow copy with selected properties changed; nested referenced objects same instance ho sakte hain.

---

### Q1.3 Constructor inheritance chain mein kis order mein chalte hain?

**Answer:** `new C()` mein, agar `C : B : A`, constructor bodies pehle `A`, phir `B`, phir `C` chalte hain. Har class ke instance field initializers us class ke constructor body se pehle execute hote hain. `base(args)` base constructor select karta hai; `this(args)` isi class ke doosre constructor ko chain karta hai.

**Pitfall:** Base constructor se virtual method call karne par derived override derived constructor body se pehle chal sakta hai; derived state abhi ready na ho. Aisa virtual dispatch constructors mein avoid karein.

**Follow-ups**
- **Agar `base(...)` omit ho?** Compiler implicit parameterless base constructor call karta hai; woh accessible/existent na ho to explicit constructor chain likhni hogi.
- **`this` aur `base` ka farq?** `this` current class constructor ya member ko target karta hai; `base` parent implementation/constructor ko.

---

### Q1.4 `async`/`await` thread create karta hai?

**Short answer:** Nahin. `async` method compiler-generated state machine banata hai; `await` incomplete asynchronous operation par method ko suspend karke control caller ko lauta sakta hai bina thread block kiye.

**Answer:** I/O-bound kaam ke liye thread block kiye bina scalability milti hai. CPU-bound kaam ke liye `Task.Run` use karein.

**Depth:**
- `.Result`/`.Wait()` se async call block karne par thread starvation/deadlock risk badhta hai.
- `async void` avoid karein except event handlers; return `Task`/`ValueTask`.
- Independent calls concurrently run karein: `await Task.WhenAll(t1, t2)`.
- `CancellationToken` through all layers pass karein.
- `ValueTask` hot paths ke liye jab frequently complete synchronously ho.

**Pitfall:** `.Result`/`.Wait()` sync-over-async pattern library code mein deadlock de sakta hai. Fire-and-forget task exceptions/lifetime ignore na karein.

**Scenario:** API external service call karti hai — `HttpClient` ka async API await karein, request cancellation token propagate karein, finite timeout set karein.

**Follow-ups**
- **`Task` vs `Thread`?** `Task` asynchronous work/completion ko represent karta hai; `Thread` dedicated execution thread hai (~1 MB stack, expensive).
- **`Task.WhenAll` kya karta hai?** Multiple tasks ko concurrently await karta hai; apne aap concurrency cap nahin karta.
- **`Task.Run` kab?** CPU-bound work thread pool par schedule karne ke liye; I/O-bound async methods ko `Task.Run` se wrap karna unnecessary hai.
- **`Task.Factory.StartNew` pitfall?** Advanced scheduler/options control deta hai lekin async delegate par nested-task pitfalls hain; `Task.Run` prefer karein.

---

### Q1.5 GC hai to `IDisposable` kyun chahiye?

**Answer:** GC managed memory ki reachability handle karta hai, par file handles, sockets, database connections jaise unmanaged resources ke liye deterministic cleanup `IDisposable`/`IAsyncDisposable` use hota hai. `using`/`await using` scope exit par cleanup guarantee karte hain, including exception path.

**Pitfall:** Finalizer ko normal cleanup mechanism na banayein; finalization nondeterministic hai. Disposed object ko dobara use na karein. Ownership clear rakhein.

**GC generations:** Gen 0 (short-lived, collected most often), Gen 1 (buffer), Gen 2 (long-lived). LOH (85,000+ bytes) Gen 2 ke saath collect hota hai. `ArrayPool<T>` churn kam karta hai.

**Memory leaks managed code mein:** Event handlers unsubscribed na hon, static/caches jo grow karein, `Timer`s, captured closures, `HttpClient` per request (socket exhaustion).

**Follow-ups**
- **`Dispose` vs finalizer?** `Dispose` deterministic caller-controlled cleanup; finalizer last-resort nondeterministic native-resource safety net. `SafeHandle` prefer karein.
- **`GC.Collect()` kab?** Application code mein routine fix ke taur par nahin; measured specialized scenario ke bina avoid karein.

---

### Q1.6 DI lifetimes — Transient, Scoped, Singleton kya hain?

**Answer:** Transient har resolution par naya instance; Scoped ek scope (web request mein ek request) ke andar shared; Singleton application lifetime tak ek instance.

| Lifetime | Instance | Typical use |
|---|---|---|
| **Transient** | Har baar naya | Lightweight, stateless services |
| **Scoped** | Per HTTP request | `DbContext`, unit-of-work, per-request state |
| **Singleton** | App lifetime mein ek | Config, caches, `HttpClient` factory, shared stateless services |

**Pitfall:** Singleton mein scoped dependency capture karna captive dependency hai — scoped object request boundary ke baad bhi reuse ho sakta hai. `ValidateScopes` development mein yeh flag karta hai. Singleton thread-safe hona chahiye. Background worker mein `IServiceScopeFactory` se explicit scope create karein.

**Scenario:** `DbContext` usually scoped hota hai. Singleton cache ko data chahiye ho to scoped service constructor mein inject na karein; operation ke dauran scope/factory ke zariye resolve karein.

**Follow-ups**
- **Captive dependency kya?** Long-lived service (singleton) chhote lifetime ki dependency (scoped) capture kar le, toh scoped object unexpectedly reuse hota hai.
- **`IOptions` lifetimes?** `IOptions<T>` stable; `IOptionsSnapshot<T>` scoped/per-request; `IOptionsMonitor<T>` change notifications ke saath singleton-safe.
- **Keyed services?** .NET 8+ mein `AddKeyedScoped<IPayment, StripePayment>("stripe")` se named implementations resolve.

---

### Q1.7 Race condition, lock, `volatile` aur `Interlocked` mein kya farq hai?

**Answer:** Race condition tab hoti hai jab concurrent operations ka result timing par depend kare. Compound shared-state update ko `lock`/Monitor se mutually exclusive banayein; simple atomic counter ke liye `Interlocked`; visibility flag ke liye `volatile` ho sakta hai — lekin `volatile` `count++` ko atomic nahin banata. `SemaphoreSlim` bounded concurrency aur async `WaitAsync` ke liye.

**Pitfall:** `lock(this)`, public object, `string` ya boxed value type par lock na karein. Locks ka inconsistent order deadlock de sakta hai. `await` normal `lock` block mein use nahin kar sakte.

**Follow-ups**
- **Deadlock conditions?** Mutual exclusion, hold-and-wait, no preemption, circular wait. Consistent lock order, short critical sections, timeouts.
- **Mutex vs SemaphoreSlim?** Mutex exclusive ownership aur cross-process; `SemaphoreSlim` in-process bounded concurrency aur async wait.
- **Thread pool starvation?** Sync-block pool threads pool ko exhaust karte hain; sync-over-async aur blocking I/O avoid karein.

---

### Q1.8 Records, `init`, auto-properties, delegates aur events?

**Answer:** `record` value-oriented equality/copy syntax deta hai. Auto-property compiler-generated backing storage ka concise syntax hai. `init` object initialization ke waqt assignment allow karta hai, referenced mutable object ko deeply immutable nahin banata. Delegate method reference/type-safe callback hai; `event` publisher ko invocation control deta hai.

**Pitfall:** `Equals` aur `GetHashCode` consistent hone chahiye, khaaskar hash collection keys par. Event subscriber unsubscribe na kare to memory leak ho sakta hai.

**Follow-ups**
- **`init` vs `set`?** `init` initializer/constructor context tak assignment restrict karta hai; `set` normal lifetime mein mutate ho sakta hai.
- **`event` vs public delegate?** Public delegate ko caller overwrite/invoke kar sakta hai; event publisher invocation authority protect karta hai.
- **Delegates (built-in)?** `Action`, `Func<>`, `Predicate<T>` common built-ins hain. LINQ lambdas, callbacks, middleware (`RequestDelegate`) bhi delegates ke basis par hain.

---

### Q1.9 `==`, `Equals()` aur `ReferenceEquals()` kya compare karte hain?

**Answer:** `==` type-specific ho sakta hai — strings value compare karti hain, class default mein references compare. `Equals()` logical equality define/override karta hai. `ReferenceEquals()` exactly same object reference check karta hai.

**Pitfall:** `Equals` override karne par `GetHashCode` ko consistent update na karna hash collections ko break karta hai. Equality contract transitive, symmetric, reflexive aur stable hona chahiye.

**Follow-ups**
- **`string ==`?** String contents ordinally compare karta hai; culture-sensitive comparison ke liye `StringComparison` explicitly choose karein.
- **Struct equality?** Value equality expected; hot path mein default implementation cost profile karein; `record struct` value semantics deta hai.

---

### Q1.10 `ref`, `out`, `in`; `const`, `readonly`, `static readonly`?

**Answer:** `ref` caller-initialized variable ko by-reference read/write pass karta hai; `out` callee ko definitely assign karna hota hai; `in` by-reference read-only parameter. `const` compile-time constant; `readonly` instance field constructor mein set; `static readonly` type-level runtime initialization.

**Pitfall:** Public `const` consumer assemblies mein inline hota hai — library versioning ke liye `static readonly` consider karein.

**Follow-ups**
- **`readonly` field vs `init` property?** Field implementation detail; `init` public construction contract restrict karta hai.

---

### Q1.11 `IEnumerable`, `ICollection`, `IList` aur `List<T>` mein kya farq hai?

**Answer:** `IEnumerable<T>` sirf iteration; `ICollection<T>` count/mutation add karta hai; `IList<T>` index-based access; `List<T>` concrete resizable array-backed implementation. API mein smallest capability interface expose karein jo caller ko chahiye.

**Pitfall:** `IEnumerable` ko repeatable ya in-memory collection guarantee na samjhein — lazy, single-use, expensive, ya DB-backed ho sakta hai.

**Follow-ups**
- **`yield return`?** Iterator state machine banata hai; values on-demand enumerate hoti hain.
- **`List<T>` vs `LinkedList<T>`?** List indexing/cache locality; linked list known-node insertion O(1) lekin search O(n) aur overhead zyada.

---

### Q1.12 Exception handling best practices?

**Answer:** Expected validation/business outcomes ko explicit result/validation path se; exceptional failures ko specific exceptions se. Catch wahan karein jahan recover, translate, ya cleanup kar sakte hain. `throw ex;` stack trace reset karta hai; `throw;` preserve karta hai.

**Pitfall:** Bare catch/silent swallow, normal branching ke liye exceptions, duplicate log/rethrow, sensitive data logs mein.

**Follow-ups**
- **`finally` ya `using`?** Resource lifetime ke liye `using`/`await using`; generic cleanup ke liye `finally`.
- **.NET Framework vs modern .NET?** Framework Windows-only legacy; modern .NET cross-platform unified runtime. Migration mein dependency/hosting/API compatibility test karein.

---

### Q1.13 GC generations aur LOH ka behavior?

**Answer:** .NET GC reachable managed objects retain karta hai, unreachable collect karta hai. Short-lived allocations Gen 0 mein; survive karne wale promote hote hain. LOH (85,000+ bytes) Gen 2 ke saath collect hota hai. GC nondeterministic hai — native resources dispose karein.

**Pitfall:** Unbounded cache/event references, large temporary buffers, allocation churn aur pinned objects pressure/fragmentation la sakte hain.

**Scenario:** Memory steadily climb kare to heap snapshot aur allocation profile se retention path trace karein — sirf `GC.Collect()` call karna fix nahin.

---

### Q1.14 `var`, `dynamic`, anonymous types aur extension methods?

**Answer:** `var` compile-time type inference hai, runtime dynamic nahin; `dynamic` member binding runtime par; anonymous type local inferred immutable-property shape; extension method static method ko instance-call syntax deta hai.

**Pitfall:** `dynamic` compile-time safety/IDE refactoring kam karta hai. Extension methods real instance member se precedence nahin lete.

**Follow-ups**
- **`var` kab?** RHS se type obvious ho to readability; obscure intent ho to explicit type.
- **Anonymous type public return?** Method return type awkward hoga; named DTO/record use karein.

---

## 2. OOP, SOLID & Design Patterns

### Q2.1 OOP ke four pillars kya hain?

| Pillar | Matlab | Problem solve karta hai | C# mechanism |
|---|---|---|---|
| Encapsulation | Data ko code ke saath bundle karo, internal state hide karo | Outside code object ko invalid state mein na daale | `private` fields, properties, access modifiers |
| Abstraction | *Kya* karta hai dikhao, *kaise* karta hai hide karo | Callers simple contract par depend karein, messy details nahin | `interface`, `abstract class` |
| Inheritance | Class doosri class ke members acquire kare aur specialize kare | Common behaviour reuse; "is-a" relationships | `class Dog : Animal` |
| Polymorphism | Ek interface, kai implementations; runtime par sahi behaviour choose hota hai | Nayi types bina existing code change kiye add ho sakti hain | `virtual`/`override`, interfaces, overloading, generics |

**Talking point:** Pillars milkar kaam karte hain. `PaymentProcessor` abstraction gateway details hide karta hai (abstraction), har gateway apne credentials private rakhta hai (encapsulation), gateways base class se logging/retries share karte hain (inheritance), aur checkout code `Process()` call karta hai without caring which gateway it holds (polymorphism).

---

### Q2.2 SOLID principles ko practical example se samjhaiye.

**Answer:**
- **S (SRP):** Ek module ka ek coherent reason to change. `OrderService` ko `OrderValidator`, `OrderRepository`, `EmailNotifier` mein split karein.
- **O (OCP):** Extension se behavior add ho bina stable code edit kiye. Nayi `IDiscountStrategy` implementation add karein, if/else chain edit nahin.
- **L (LSP):** Subtype apne base contract ko todhe bina replace ho. `Square : Rectangle` jo `SetWidth` semantics break kare — LSP violation. Fix: `IShape` with `Area` alag design karein.
- **I (ISP):** Clients ko unused methods par depend na karna pade. `IReadRepository` aur `IWriteRepository` alag rakhein.
- **D (DIP):** High-level policy concrete details ke bajay abstractions par depend kare. Controller `IOrderService` par depend kare, DI container implementation inject kare.

**Pitfall:** SRP ka matlab har method ke liye alag class banana nahin. SOLID trade-offs aur change boundaries ke tools hain, laws nahin.

**Follow-ups**
- **DIP aur DI same hain?** Nahin. DIP design principle hai; DI dependencies supply karne ki technique.
- **Interface vs abstract class?** Interface capability/multiple implementation composition; abstract class shared state/default implementation aur single inheritance. Actual shared invariants se decide karein.
- **Composition over inheritance kyun?** Runtime par combine/replace asaan; rigid base-class coupling kam.

---

### Q2.3 Polymorphism ke types aur virtual dispatch kaise kaam karta hai?

**Answer:** Polymorphism ka matlab "many forms" — same call alag behavior different objects par.

**Types in C#:**
- **Compile-time (static):** Method overloading (same name, different parameters), operator overloading, generics.
- **Run-time (dynamic):** `virtual`/`abstract` methods jo derived classes mein `override` hote hain; actual type runtime par decide karta hai.
- **Parametric:** Generics jaise `List<T>`.

```csharp
public abstract class Notification
{
    public abstract void Send(string message);
}
public class EmailNotification : Notification
{
    public override void Send(string message) => Console.WriteLine($"Email: {message}");
}
public class SmsNotification : Notification
{
    public override void Send(string message) => Console.WriteLine($"SMS: {message}");
}

List<Notification> channels = new() { new EmailNotification(), new SmsNotification() };
foreach (var c in channels) c.Send("Order shipped"); // right Send() chosen at run time
```

**Virtual dispatch:** Har type ka virtual method table (vtable) hota hai; object ek hidden pointer apne type's vtable ki taraf rakhta hai. `c.Send()` call par runtime object ka vtable dekh ke sahi implementation jump karta hai. Non-virtual calls compile time par resolve hote hain (early binding), slightly faster aur inline ho sakte hain.

**Follow-ups**
- **Overloading vs overriding?** Overloading compile time, same class; overriding runtime, derived class base redefine karta hai.
- **`new` (method hiding)?** Alag member banata hai — base reference se call base member tak jaata hai; hiding usually code smell hai.
- **`sealed override`?** Current derived class par virtual override seal karta hai; further derived override nahin kar sakte.
- **`abstract` class constructor?** Constructor rakh sakti hai; derived instance create hote waqt base constructor shared state initialize karta hai. Direct instantiation nahin ho sakti.

---

### Q2.4 Abstract class vs interface — kaise choose karein?

| | Abstract class | Interface |
|---|---|---|
| Purpose | Shared base with common state and partial implementation ("is-a") | Pure capability contract ("can-do") |
| Inheritance | Only one | Many implement kar sakte hain |
| State (fields) | Haan | No instance fields |
| Constructors | Haan | Nahin |
| Best use | Closely related types jo code + state share karein | Unrelated types jo capability share karein (`IDisposable`, `ILogger`) |

**Decision rule:** Types share *code aur state* karein aur closely related hon to abstract class. Unrelated types ko *capability* chahiye, ya multiple inheritance of contract, ya easy mocking — to interface. Common combo: `interface IReport` + `abstract class ReportBase : IReport` with boilerplate.

---

### Q2.5 LSP — Bird/Penguin example?

**Answer:** Agar caller `Bird` ko use karke `Fly()` expect karta hai, to `Penguin : Bird` ko exception throw karke contract todna nahin chahiye. Base type ka behavior contract sab subtypes ke liye valid hona chahiye. Better model: `Bird` ko general behavior tak rakhen aur `IFlyable` sirf udne wale birds implement karein.

**Pitfall:** Sirf method signatures compatible hona kaafi nahin; preconditions stronger, postconditions weaker, ya invariants break karne wala subtype bhi LSP violate karta hai.

---

### Q2.6 Factory, Strategy, Decorator, Adapter, Repository kab use karein?

**Answer:**
- **Factory:** Object creation decision encapsulate karta hai.
- **Strategy:** Interchangeable algorithm select karta hai runtime par.
- **Decorator:** Existing contract ke around behavior layer karta hai bina inheritance ke.
- **Adapter:** Incompatible interfaces ko bridge karta hai.
- **Repository:** Persistence details ko domain-facing abstraction ke peeche rakhta hai.

```csharp
// Strategy pattern
public interface IDiscount { decimal Apply(decimal amount); }
public class Festive : IDiscount { public decimal Apply(decimal a) => a * 0.9m; }
public class Checkout {
    private readonly IDiscount _d;
    public Checkout(IDiscount d) => _d = d;
    public decimal Total(decimal amount) => _d.Apply(amount);
}
```

**Pitfall:** Generic Repository se EF Core ke useful query capabilities chhupakar complexity badh sakti hai. DbContext already Unit of Work hai.

**Follow-ups**
- **Decorator vs inheritance?** Decorator runtime composition se behavior stack; inheritance static hierarchy aur tight coupling.
- **Singleton pattern ka risk?** Hidden global state, testing difficulty, concurrency. DI container se singleton lifetime register karna behtar.
- **Strategy vs State?** Strategy mein client algorithm select karta hai; State mein object's internal mode behavior control karta hai.
- **Command vs Strategy?** Command action/request ko object banata hai (queue, undo); Strategy interchangeable algorithm.

---

### Q2.7 LLD interview problem kaise approach karein?

**Answer:** Pehle scope aur requirements clarify karein; nouns se entities identify, responsibilities define, variation points par pattern choose, edge cases/thread-safety discuss.

- **Logger:** `LogMessage` immutable data; threshold filtering; `ILogFormatter` strategy; multiple `ILogAppender` sinks; thread-safe writes; bounded queue, flush aur graceful shutdown.
- **File system:** Composite pattern — `File` leaf aur `Directory` dono `IFileSystemEntry` expose karein; parent/child updates, permissions, deletion semantics define.
- **Vending machine:** State pattern — `Idle`, `HasCredit`, `Dispensing`, `OutOfStock`; invalid transitions, insufficient balance, change, concurrent purchase handle karein.
- **Thread-safe cache:** Concurrent map ke saath expiration/eviction; compound check-then-create atomic nahin ban jata sirf collection thread-safe hone se.
- **Rate limiter:** Token bucket/fixed ya sliding window; per-client partition key; distributed deployment behavior specify karein.
- **Parking lot:** Vehicle/spot/ticket/payment; concurrent spot allocation atomic ho.

**Pitfall:** Pattern impose karne se pehle problem samjho. Logger ko static global state banana testability hurt karta hai.

---

### Q2.8 Clean Architecture / layered architecture kaise structure karein?

**Answer:** Concentric layers — dependency rule: dependencies inward only.
1. **Domain:** Entities, value objects, business rules; no dependencies.
2. **Application:** Use cases, commands/queries/handlers, interfaces, DTOs, validation.
3. **Infrastructure:** EF Core, repositories, external APIs, email; Application interfaces implement karta hai.
4. **Presentation/API:** Controllers, minimal APIs, Angular; composition root (DI wiring).

**Benefits:** Testable business logic, framework/DB independence, clear boundaries. **Costs:** More projects, mapping aur boilerplate. **Practical:** Long-lived complex domains ke liye; simple CRUD apps mein simpler vertical-slice architecture consider karein.

---

### Q2.9 CQRS aur Mediator pattern?

**Answer:** CQRS — Command Query Responsibility Segregation — writes (commands) aur reads (queries) ke models separate karta hai. Commands state change karte hain, return little/nothing; queries data return karte hain, state change nahin karte. MediatR ya hand-rolled handlers `IRequest<T>` aur pipeline behaviours (validation, logging, transaction) implement karte hain.

**Levels:** Same DB with separate models → separate read store with events (eventual consistency) → Event Sourcing. **Trade-off:** More code aur complexity; simple CRUD ke liye avoid karein.

**Pitfall:** MediatR commercial ho gaya hai newer major versions mein — Wolverine/Mediator source generators ya plain handlers alternatives hain.

---

## 3. ADO.NET, LINQ & Data Access

### Q3.1 `DataReader`, `DataSet` aur `DataAdapter` mein kya farq hai?

**Answer:** `DataReader` forward-only, read-only, connected/streaming — large results aur low overhead ke liye. `DataSet` disconnected in-memory relational data, multiple tables/relations — memory-heavy ho sakta hai. `DataAdapter` database aur `DataSet`/`DataTable` ke beech fill/update bridge.

**Pitfall:** Reader open ho to connection resource occupied rehta hai; jaldi consume/dispose karein.

**Follow-ups**
- **`ExecuteNonQuery`, `ExecuteScalar`, `ExecuteReader`?** Respectively affected-row count, first row ka first value, aur row stream.
- **Connection pooling?** Closed logical connections ko provider pool mein reuse karta hai; dispose/close karna pool return ke liye zaroori.
- **ADO.NET vs Dapper vs EF Core?** ADO.NET maximum control; Dapper lightweight SQL mapping; EF Core change tracking/LINQ productivity. CQRS-style use both: EF Core for writes, Dapper for hot read paths.
- **`SqlBulkCopy` kab?** SQL Server mein large row batches efficiently load karne ke liye.

---

### Q3.2 SQL injection se kaise bachenge?

**Answer:** User input ko SQL text mein concatenate na karein; parameterized commands use karein. Parameters values ko data ke roop mein bhejte hain, query syntax ke roop mein nahin.

**Pitfall:** Escaping ya client-side validation parameterized SQL ka substitute nahin. Dynamic table/column identifiers parameterize nahin hote — strict allow-list mapping chahiye.

**Scenario:** Search API mein sort field client deta hai; accepted values ko known column expressions ke allow-list se map karein.

**Follow-ups**
- **Stored procedure bhi unsafe?** Haan, agar woh dynamic SQL concatenate kare.
- **`sp_executesql`?** Dynamic SQL ke liye parameters ke saath use karein; identifiers allow-list se map karein.

---

### Q3.3 LINQ deferred execution kya hai?

**Answer:** `Where`/`Select` jaise operators query describe karte hain; enumeration (`foreach`, `ToList`, `Count`) par execution hoti hai. Isse pipeline compose hoti hai.

**Pitfall:** Query ko baar-baar enumerate karna duplicate database round trips kar sakta hai. Closure variable ko query banate waqt snapshot nahin kiya jata; execution ke samay captured value use hoti hai.

**Follow-ups**
- **`IEnumerable<T>` vs `IQueryable<T>`?** `IEnumerable` in-process enumeration; `IQueryable` expression tree provider ko SQL translate karne deta hai — EF Core. `IQueryable` ko arbitrary layer tak expose karna provider coupling de sakta hai.
- **`Select` vs `SelectMany`?** `Select` per input ek result/map; `SelectMany` nested sequences flatten.
- **`First` vs `Single`?** `First` kam-se-kam ek maangta hai; `Single` exactly ek. Data invariant enforce karna ho to `Single` helpful.
- **`GroupBy` vs `ToLookup`?** `GroupBy` deferred; `ToLookup` immediately reusable lookup materialize karta hai.
- **PLINQ kab?** Independent CPU-bound work, enough data aur safe ordering/side effects ho tab measure karke.

---

### Q3.4 LINQ query slow hai — kahan se shuru karein?

**Answer:** Pehle query execution boundary aur generated SQL/plan dekhein, phir data volume, indexes, projection, joins aur enumeration count. Sirf zaroori columns select karein; DB-side filtering rakhein.

**Pitfall:** `ToList()` ko har operator ke baad daalna materialize cost badhata hai; lekin boundary par materialize karna repeated query se sasta ho sakta hai. Benchmark/profile karein.

---

## 4. ASP.NET Core & Web API

### Q4.1 Middleware pipeline kya hai aur order kyun important hai?

**Short answer:** Middleware request ko order mein process karta hai; har component next invoke, short-circuit, ya response modify kar sakta hai.

**Answer:** Response aam taur par reverse direction mein unwind hote waqt pass hota hai. "Onion" model: request inward jaata hai, endpoint tak pahunchta hai, response outward aata hai.

```csharp
app.Use(async (ctx, next) => {
    var sw = Stopwatch.StartNew();
    await next();
    app.Logger.LogInformation("{Path} took {Ms}ms", ctx.Request.Path, sw.ElapsedMilliseconds);
});
```

**Recommended order:** ExceptionHandler → HSTS → HttpsRedirection → StaticFiles → Routing → CORS → Authentication → Authorization → RateLimiter → Endpoints.

**Pitfall:** Authentication se pehle authorization, endpoint routing/CORS galat jagah, ya broad middleware se exception chhupana — security/functionality bugs.

**Follow-ups**
- **`Use`, `Run`, `Map`, `UseWhen`?** `Use` aage call kar sakta hai; `Run` terminal; `Map` path branch (rejoins nahin); `UseWhen` condition-based branch (rejoins).
- **Middleware vs filter?** Middleware broad HTTP pipeline concern; MVC filters action/controller stages par kaam karte hain.

---

### Q4.2 Classic ASP.NET MVC 4 request lifecycle aur filters?

**Answer:** IIS/ASP.NET pipeline request leta hai; routing controller/action select karti hai; controller factory/controller execute hota hai; model binding/validation, filters, action aur ActionResult/view rendering ke baad response.

**Filter order:** Authorization → Action (OnActionExecuting) → action → Action (OnActionExecuted) → Result → Exception.

**Pitfall:** ASP.NET Core middleware order se MVC filter order alag hai; version-specific behavior apply na karein.

**Follow-ups**
- **ViewBag/ViewData/TempData?** ViewBag dynamic wrapper over ViewData; dono current request; TempData redirect ke paar one redirect. Typed view model prefer karein.
- **ViewModel kyun?** Domain entity over-posting, data exposure aur persistence coupling se bachata hai.
- **Areas?** Large app ko feature modules/controllers/views mein organize.

---

### Q4.3 REST API mein idempotency aur HTTP status codes?

**Answer:** Idempotent request ki identical retries se final server state same rehna chahiye. `GET`, `PUT`, `DELETE` semantics idempotent; `POST` aam taur par nahin.

**Status codes:** `201` created, `202` accepted for async work, `204` no content, `400` invalid request, `401` unauthenticated, `403` forbidden, `404` absent, `409` conflict, `422` unprocessable, `429` rate limited.

**Pitfall:** `401` aur `403` mix na karein. Non-idempotent payment/order action par retry bina dedupe key ke duplicate effects deta hai.

**Follow-ups**
- **PUT vs PATCH?** PUT resource representation replace/desired state set karta hai; PATCH partial modifications.
- **Offset vs cursor pagination?** Offset simple random page access; cursor large/changing datasets mein stable continuation.
- **`202 Accepted` kab?** Work accept hua, complete nahin. Status polling/resource URL aur eventual failure contract dein.
- **ProblemDetails (RFC 7807)?** Machine-readable error contract; status/title/detail/instance fields.

---

### Q4.4 Minimal APIs ya Controllers — kaunsa choose karein?

**Answer:** Dono ASP.NET Core endpoints banate hain. Minimal APIs focused services ke liye concise; controllers conventions, filters, grouping aur MVC organization de sakte hain. Team familiarity, API complexity, validation/versioning/testing needs se choose karein.

**Follow-ups**
- **Kestrel kya hai?** ASP.NET Core ka cross-platform web server. Production mein reverse proxy TLS termination, forwarding aur edge policies ke liye.
- **`IHttpClientFactory` kyun?** Handler pooling, named/typed clients — socket exhaustion/DNS staleness risks address karta hai.
- **Rate limiting?** Built-in `AddRateLimiter` (fixed/sliding window, token bucket); per-client/user/tenant policy; `429` aur `Retry-After`.

---

### Q4.5 Production API ko resilient kaise banayein?

**Answer:** Timeouts, cancellation propagation, bounded retries with backoff+jitter, circuit breaker (Polly), bulkhead/concurrency caps, rate limits, health checks aur observability combine karein. Retry sirf transient failures aur idempotent/replay-safe operations par.

**Circuit breaker states:** Closed requests pass; threshold ke baad Open fail-fast; Half-open limited probes se recovery test.

**Pitfall:** Retry + timeout + queue redelivery multiply hokar request storm bana sakte hain.

**Follow-ups**
- **Health check vs readiness?** Liveness process restart signal; readiness traffic receive readiness. Dependency failure liveness mein rakhna restart loop bana sakta hai.

---

### Q4.6 DI container disposal, `DbContext`/`HttpClient` registration?

**Answer:** Container se resolve hua disposable service aam taur par us scope/container se dispose hota hai. `DbContext` per unit-of-work/request scoped; `HttpClient` ke liye `IHttpClientFactory` typed/named clients use karein. Same `DbContext` concurrently share na karein — thread-safe nahin.

**Pitfall:** `HttpClient` per request `new` karna socket/DNS lifecycle problems laata hai. Container se resolve transient disposable ko root provider se capture karna disposal delay kar sakta hai.

**Follow-ups**
- **Keyed services?** Same interface ki named implementations resolve karta hai — explicit finite variants ke liye.
- **Constructor injection vs service locator?** Constructor injection dependency/ownership visible aur testable; service locator hidden runtime dependency.

---

### Q4.7 DI lifetime bug — User A ko User B ka data dikh raha hai?

**Answer:** Request-specific mutable state singleton/static service mein, shared cache key missing, reused `DbContext`, session affinity ya tenant scoping leak. Correlation/tenant/user IDs trace karein; DI registrations, object lifetimes, cache key aur data query predicates inspect karein.

**Pitfall:** Sirf auth token decode karke data authorization assume karna; cache key mein tenant/user missing. Yeh privacy/security incident hai — escalate karein.

**Follow-ups**
- **Singleton thread-safe hona enough?** Nahin — shared state tenant/user-scoped ho to thread-safety sirf simultaneous writes protect karegi, data isolation nahin.

---

### Q4.8 MVC/Web API input validation aur CSRF/XSS/over-posting?

**Answer:** Server-side validation authoritative. Cookie-authenticated browser POST par anti-forgery token validate karein. Output contextual encoding, Razor default encoding, narrow CSP, DTO/view model allow-listed fields use karein.

**Pitfall:** Client-side validation security boundary nahin. `Html.Raw`/dangerous DOM APIs untrusted content execute kara sakte hain. CORS CSRF protection nahin.

**Follow-ups**
- **CSRF vs XSS?** CSRF browser ke automatically attached credentials exploit karta hai; XSS same-origin script execute karke requests/data access.
- **CORS?** Browser cross-origin response access policy; explicit origins/methods/headers allow-list; credentials ke saath wildcard origin nahin.

---

### Q4.9 Global exception handling aur API versioning?

**Answer:** .NET 8+ `IExceptionHandler` + ProblemDetails se central exception handling; internal stack/secrets client ko leak na karein.

```csharp
public class GlobalExceptionHandler(ILogger<GlobalExceptionHandler> log) : IExceptionHandler {
    public async ValueTask<bool> TryHandleAsync(HttpContext ctx, Exception ex, CancellationToken ct) {
        log.LogError(ex, "Unhandled: {TraceId}", ctx.TraceIdentifier);
        var (status, title) = ex switch {
            ValidationException   => (400, "Validation failed"),
            NotFoundException     => (404, "Not found"),
            _                     => (500, "Server error")
        };
        ctx.Response.StatusCode = status;
        await ctx.Response.WriteAsJsonAsync(new ProblemDetails {
            Status = status, Title = title, Extensions = { ["traceId"] = ctx.TraceIdentifier } }, ct);
        return true;
    }
}
```

**Versioning:** URI segment (`/api/v1/orders`), query string, header ya media type. URI most cache-friendly. Deprecated versions ko phaseout policy ke saath.

---

### Q4.10 Slow API troubleshoot kaise karein?

**Answer:** Measure before changing. Top-down:
1. Reproduce/scope: kaunsa endpoint, kaunsa percentile, since when.
2. Distributed tracing/APM, logs, metrics.
3. Layer identify: network → app → downstream → database.
4. Database (most common): slow query log, execution plan, N+1, `SELECT *`, parameter sniffing, locking.
5. Application: sync-over-async, excessive allocations, chatty downstream calls, missing `HttpClientFactory`.
6. Fix aur load test se verify.

---

## 5. EF Core & Architecture

### Q5.1 Tracking aur `AsNoTracking()` mein farq?

**Answer:** Tracking query entities ko `DbContext` change tracker mein attach karta hai — update workflow mein useful. Read-only query ke liye `AsNoTracking()` tracking overhead kam karta hai. `AsNoTrackingWithIdentityResolution()` read-only results mein duplicate entity instances resolve kar sakta hai.

**Pitfall:** Read result ko modify karke save karna ho to no-tracking entity automatic update nahin hogi. `DbContext` thread-safe nahin — concurrent operations mein share na karein.

**Follow-ups**
- **N+1 problem?** Parent query ke baad har item ke liye child query chalna. Projection, explicit/eager loading, batching ya split query se fix.
- **Eager vs lazy loading?** Eager related data query mein laata hai; lazy access par baad mein (hidden round trips); explicit developer-controlled separate query.
- **Split query?** Multiple related collections ko separate queries mein load karke cartesian explosion kam; consistency/extra round-trip trade-off.

---

### Q5.2 EF Core vs Dapper?

| | EF Core | Dapper |
|---|---|---|
| Type | Full ORM | Micro-ORM |
| Productivity | High: LINQ, change tracking, migrations | SQL khud likhna padta hai |
| Performance | Good; tracking overhead (mitigate with `AsNoTracking`) | Near raw ADO.NET |
| Best for | CRUD, domain models, rapid development | Hot read paths, complex reports, bulk, stored procs |

**Senior answer:** CQRS-style dono use karein — EF Core writes ke liye, Dapper performance-critical reads ke liye. Same connection/transaction share kar sakte hain (`context.Database.GetDbConnection()`).

---

### Q5.3 EF Core migration aur production schema updates?

**Answer:** Schema changes ko reviewed, version-controlled migrations mein rakhein. Expand-contract sequence: backward-compatible additions pehle; app dual-read/write ya backfill; phir old schema removal alag release mein.

**Pitfall:** Production startup par migrations race mein chalana, long blocking migration, ya rollback plan ke bina destructive change risky.

**Follow-ups**
- **Optimistic concurrency?** Rowversion/concurrency token update condition mein expected version verify karta hai; conflict par merge/retry/user resolution.
- **Compiled queries?** Repeated stable query shape mein overhead kam; pehle profile karein.

---

### Q5.4 Repository pattern — benefits aur criticism?

**Answer:** Decouples business logic from persistence, enables unit testing with fakes, centralises queries. **Criticism:** `DbContext` already Unit of Work aur `DbSet<T>` repository jaisa hai. Generic repository EF ke `IQueryable` power hide kar sakta hai. **Pragmatic:** Specific repositories for complex aggregates, direct `DbContext` for simple cases. Specification pattern ya CQRS alternatives hain.

---

## 6. Authentication, Authorization & Security

### Q6.1 Authentication, authorization, OAuth2, OIDC aur JWT ka relation?

**Answer:** Authentication user/client ki identity establish karta hai; authorization us identity ko action/resource ke liye permission deta hai. OAuth 2.0 delegated authorization framework hai; OpenID Connect uske upar identity layer. JWT ek token format hai — OAuth/OIDC nahin.

**JWT structure:** Header (algorithm) + Payload (claims: `sub`, `exp`, `iss`, `aud`, roles) + Signature. Signed, not encrypted — secrets JWT mein mat rakho. Short-lived access token (5–15 min) + rotating refresh token.

```csharp
builder.Services.AddAuthentication(JwtBearerDefaults.AuthenticationScheme)
  .AddJwtBearer(o => o.TokenValidationParameters = new() {
      ValidateIssuer = true, ValidateAudience = true, ValidateLifetime = true,
      ValidateIssuerSigningKey = true, ClockSkew = TimeSpan.FromMinutes(1),
      ValidIssuer = cfg["Jwt:Issuer"], ValidAudience = cfg["Jwt:Audience"],
      IssuerSigningKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(cfg["Jwt:Key"]!))
  });
```

**Pitfall:** `alg=none`, algorithm confusion, wrong audience/issuer, weak key checks critical mistakes. Token decode karna validation nahin.

**Follow-ups**
- **OAuth flow?** Public SPA/mobile: Authorization Code + PKCE; machine-to-machine: client credentials. Implicit/password grants avoid/deprecated.
- **SAML vs OIDC?** OIDC modern web/mobile JSON/HTTP ecosystem; SAML enterprise browser SSO. IdP/client capability decide karte hain.
- **Role vs policy?** Role coarse group membership; policy claims/resource/context ke multiple requirements express karta hai.

---

### Q6.2 Access token aur refresh token ko kaise secure karein?

**Answer:** Refresh token rotation, reuse detection, revoke/logout workflow, secure storage, narrow audience/scope aur TLS implement karein. Browser app ke liye BFF + HttpOnly/Secure/SameSite cookie pattern token ko browser JavaScript se door rakhta hai.

**Pitfall:** Long-lived bearer token ko `localStorage` mein rakhna XSS se chori ka risk rakhta hai. JWT revocation instant nahin — short expiry, denylist/version check jaise trade-offs.

**Follow-ups**
- **CSRF vs XSS?** CSRF browser ko automatically attached credential ke saath unwanted request karwata hai; XSS attacker ko origin mein script chalane deta hai. HttpOnly cookie XSS token reading kam karta hai, par XSS request issue kar sakta hai.
- **Multi-tenancy?** Tenant identity trusted auth context se resolve karein; client-supplied tenant id blindly trust na karein.

---

### Q6.3 WebAuthn/passkeys aur mTLS kya solve karte hain?

**Answer:** Passkeys/WebAuthn public-key challenge-response authentication dete hain — phishing-resistant. mTLS transport layer par client certificate se service/client identity verify karta hai. DPoP sender-constrained token ka ek aur approach.

**Pitfall:** Certificate lifecycle/rotation, device recovery, origin binding, account recovery aur fallback flows design karne hote hain.

---

### Q6.4 OWASP Top 10 — kaunse items matter karte hain?

**Answer:** Broken access control, cryptographic failures, injection, insecure design, security misconfiguration, vulnerable components, authentication failures, integrity failures, logging failures, SSRF. Mitigations: parameterized queries, output encoding, server-side authorization every request par, dependency scanning (Dependabot, NuGet audit), security headers.

**API Security checklist:**
- HTTPS everywhere, HSTS, TLS 1.2+
- OAuth2/OIDC + JWT bearer; resource ownership check (IDOR/BOLA prevent)
- Validate/sanitise; parameterized queries; limit payload size; DTO over-posting protection
- CORS explicit origins, never `*` with credentials
- Rate limiting; WAF/API gateway
- Secrets in vault, never in source
- Audit logs, alerting, pen-tests

---

## 7. SQL Server

### Q7.1 Index seek vs scan aur SARGability?

**Answer:** Seek index key ke relevant range tak pahunch sakta hai; scan index/table ke zyada rows read karta hai. SARGable predicate index access enable karta hai — indexed column par function/cast lagane se seek ruk sakta hai.

**Non-SARGable:** `WHERE YEAR(OrderDate) = 2026` — function on column.
**SARGable:** `WHERE OrderDate >= '2026-01-01' AND OrderDate < '2027-01-01'`.

**Pitfall:** Har query mein index add karna write/storage cost badhata hai. Seek hamesha fast aur scan hamesha slow nahin — chhoti table ya large result ke liye scan sahi ho sakta hai.

**Follow-ups**
- **Covering index?** Query ke filter/join/projection columns index mein available; base-table key lookup bach sakta hai.
- **Statistics?** Optimizer ko data distribution estimate karne mein help karte hain. Stale stats wrong plan de sakte hain.
- **Parameter sniffing?** Compile ke waqt parameter value ka plan reuse ho kar doosre values par poor ho sakta hai. Query Store, plan analysis ke baad targeted solution choose karein.

---

### Q7.2 Clustered vs nonclustered index?

| | Clustered | Non-clustered |
|---|---|---|
| Kya | Table data ka physical order define karta hai; leaf level = data pages | Separate structure, row locator (clustering key) ke saath |
| Count per table | **Ek** | Kai (up to 999) |
| Best for | Range scans, ordered retrieval, narrow/unique/ever-increasing keys | Selective lookups, covering queries, filtering non-key columns |

**Depth:** Narrow, static, unique, ever-increasing clustered key choose karein (IDENTITY int/bigint). Random GUIDs page splits aur fragmentation laate hain; `NEWSEQUENTIALID()` better. `INCLUDE` columns se covering index banayein; over-indexing writes slow karta hai.

**Follow-ups**
- **GUID kab?** Distributed generation/merge requirement par; sequential GUID ya nonclustered clustered key workload ke saath.
- **FK kyun?** Referential integrity; child FK indexes automatically nahin bante — explicitly create karein.

---

### Q7.3 Transactions, ACID aur isolation levels?

**Answer:** Atomicity all-or-nothing; consistency constraints valid rakhti hai; isolation concurrent transactions ke effects control karti hai; durability committed data preserve karti hai.

| Isolation level | Dirty read | Non-repeatable | Phantom | Notes |
|---|---|---|---|---|
| READ UNCOMMITTED | Haan | Haan | Haan | Unreliable data |
| READ COMMITTED (default) | Nahin | Haan | Haan | RCSI ke saath row versions |
| REPEATABLE READ | Nahin | Nahin | Haan | Shared locks until commit |
| SERIALIZABLE | Nahin | Nahin | Nahin | Range locks; highest blocking |
| SNAPSHOT | Nahin | Nahin | Nahin | Row versioning; optimistic |

**Pitfall:** Long transactions locks/version store hold karte hain. External service call ko DB transaction ke beech wait na karayein. Production mein RCSI prefer karein OLTP ke liye; `NOLOCK` smell hai, fix nahin.

**Follow-ups**
- **DELETE vs TRUNCATE vs DROP?** DELETE DML row-by-row with predicate; TRUNCATE all rows deallocate/reset fast; DROP object remove.
- **`WHERE` vs `HAVING`?** `WHERE` grouping se pehle rows filter; `HAVING` grouped/aggregate result filter.
- **`UNION` vs `UNION ALL`?** `UNION` duplicates remove (extra cost); `UNION ALL` rows preserve.

---

### Q7.4 Stored procedure, function, view, trigger aur cursor?

**Answer:** Stored procedure task/command, multiple outputs/transactions; function composable expression/table value; view saved relational query; trigger DML event par implicit execute; cursor row-by-row iteration.

**Pitfall:** Stored procedure automatic secure nahin if dynamic SQL concatenate kare. Scalar UDF/cursor/trigger hidden cost. Prefer set-based operations over cursors.

---

### Q7.5 SQL query production mein slow ho gayi — kya karein?

**Answer:** Query aur parameters reproduce/identify karein; Query Store/history, actual plan, wait stats, logical reads, blocking/deadlocks inspect karein. Cardinality, missing/inefficient indexes, non-SARGable expressions, implicit conversion, spills, parameter sensitivity check karein.

**Follow-ups**
- **CTE vs temp table?** CTE query structuring/recursion; temp table intermediate data persist/index/statistics kar sakti hai.
- **OLTP vs OLAP?** OLTP frequent small transactional reads/writes; OLAP large analytical scans/aggregations.
- **Deadlock mitigation?** Consistent access order, short transactions, appropriate indexes, deadlock graph analyze karein.

---

### Q7.6 Window functions aur SQL coding drills?

**Answer:** Window function rows preserve karte hue related set par rank/aggregate calculate karti hai. `ROW_NUMBER` unique sequence; `RANK` tie gaps; `DENSE_RANK` no gaps; `LAG/LEAD`, running totals common.

```sql
-- Department-wise second distinct highest salary
SELECT * FROM (
  SELECT Dept, Name, Salary,
         DENSE_RANK() OVER (PARTITION BY Dept ORDER BY Salary DESC) AS rnk
  FROM Employees) x
WHERE rnk = 2;

-- Duplicate rows delete
WITH d AS (
  SELECT *, ROW_NUMBER() OVER (PARTITION BY Email ORDER BY Id) rn FROM Users)
DELETE FROM d WHERE rn > 1;
```

| Prompt | Reliable pattern |
|---|---|
| Top N per group | `DENSE_RANK()` distinct tiers; `ROW_NUMBER()` exactly N rows |
| Median | `PERCENTILE_CONT(0.5)` ya ordered middle rows |
| Recursive org chart | Recursive CTE with anchor + manager-child join |
| Gaps-and-islands | `ROW_NUMBER()` sequence subtraction |
| Sessionization | `LAG(event_time)` per user; gap threshold boundary |
| EXISTS vs IN | `NOT EXISTS` prefer karein — NULL-safe |
| Pivot | `SUM(CASE WHEN category=... THEN amount ELSE 0 END)` |

---

### Q7.7 Optimistic vs pessimistic concurrency?

**Answer:** Optimistic: no locks held; detect conflicts at save time using `rowversion` column (EF `[Timestamp]`) aur handle `DbUpdateConcurrencyException`. Pessimistic: rows upfront lock (`UPDLOCK`). Optimistic web apps mein low conflict rates ke liye suit karta hai.

---

### Q7.8 Duplicate invoice generation concurrent requests mein kaise rokein?

**Answer:** Idempotency aur mutual exclusion data layer par chahiye — app-level checks alone race karte hain.

**Defence in layers:**
1. **DB unique constraint** (ultimate guard): `CREATE UNIQUE INDEX UX_Invoice_Order ON Invoices(OrderId);`
2. **Idempotency key:** Client sends `Idempotency-Key` header (GUID); server stores `(key → response/invoiceId)` unique constraint ke saath.
3. **Transaction + locking:** `SELECT ... WITH (UPDLOCK, HOLDLOCK)` ya atomic state transition: `UPDATE Orders SET Status='Invoiced' WHERE Id=@id AND Status='Ready'`.
4. **Distributed lock:** Redis `SET NX PX`/RedLock — always with TTL.
5. **Messaging:** Queue with single consumer per order key; idempotent handlers; inbox/outbox pattern.
6. **Frontend:** Button disable UX nicety only, never protection.

---

### Q7.9 Pagination at scale — millions of records?

**Answer:** `OFFSET/FETCH` deep pages par slow (skip karna costly). Keyset (seek) pagination use karein:

```sql
SELECT TOP (@PageSize) Id, CreatedAt, ...
FROM Orders
WHERE (CreatedAt < @LastCreatedAt) OR (CreatedAt = @LastCreatedAt AND Id < @LastId)
ORDER BY CreatedAt DESC, Id DESC;
```

Cursor/continuation token return karein. **Index:** `(CreatedAt DESC, Id DESC)` covering index. **Avoid exact `COUNT(*)`:** approximate total, cache, ya "10,000+". Cap `pageSize` server-side; cache hot pages.

---

## 8. Microservices, Messaging & Migration

### Q8.1 Monolith ko microservices mein kab split karein?

**Answer:** Team/domain boundaries, independent deployment/scaling, ownership, change coupling aur operational maturity measure karke. Microservices tab worthwhile jab autonomy/scale benefit complexity (network, data consistency, observability, deployment) se zyada ho.

**Pitfall:** "Microservices always scale better" ya database tables ke hisaab se split karna. Distributed monolith mein synchronous call chains aur shared database coupling bachi reh sakti hai.

**Scenario:** Strangler Fig — edge/gateway se selected capability nayi service route karein, old monolith functional rahe; traffic/data gradually migrate, reconcile, monitor, rollback path rakhein.

**Follow-ups**
- **Bounded context?** Domain model aur language ki boundary; service ownership candidate.
- **Database per service?** Data ownership/coupling kam; cross-service queries/events aur eventual consistency handle karni padti hai.

---

### Q8.2 Saga, Outbox aur idempotency kaise milkar kaam karte hain?

**Answer:** Saga multi-service business transaction ko local transactions + events/commands mein todta hai; failure par compensating action business effect reverse karta hai. Transactional Outbox business update aur event record ko ek local DB transaction mein commit karta hai. Consumer Inbox/dedupe key duplicate delivery safe banata hai.

**Pitfall:** "Exactly once" transport claim par depend na karein; at-least-once delivery + idempotent processing design karein. Compensation hamesha literal rollback nahin — email bhejna reversible nahin.

**Follow-ups**
- **Choreography vs orchestration?** Choreography mein services events par react karti hain — loose coupling, flow visibility mushkil. Orchestrator central workflow coordinate karta hai — visibility achhi, coordinator coupling trade-off.
- **DLQ ka use?** Repeatedly failing message isolate, inspect, alert aur controlled replay; DLQ silent graveyard na banayein.

---

### Q8.3 Queue, pub-sub aur event stream kab choose karein?

**Answer:**
- **SQS/Queue:** Work items consumers mein distribute karta hai; buffering/work distribution.
- **SNS/pub-sub:** Event ko multiple subscribers tak fan-out.
- **Kinesis/event stream:** Ordered partitioned log; consumers offset se independently read/replay.
- **EventBridge:** Event routing/filtering integrations ke liye.

**SQS Standard vs FIFO:** Standard high throughput/at-least-once with possible reordering; FIFO ordering/deduplication features.

**Pitfall:** Message broker ko durable business state ka automatic replacement samajhna. DLQ, duplicate delivery, poison messages configure/monitor karein.

---

### Q8.4 Zero-downtime data migration kaise karein?

**Answer:** Expand-contract: schema expand; backfill/bulk copy ya CDC se data sync; dual-read/write carefully; checksums/counts se reconcile; canary/cutover; old path remove rollback window ke baad.

**Rename column:** Add new nullable column → dual-write/backfill/check → read new → stop old writes → wait rollback window → drop old.

**Pitfall:** Dual-write without source-of-truth/ordering/retry strategy divergence la sakta hai. CDC lag, duplicate events, replay idempotency explicitly handle karein.

---

## 9. System Design & Performance

### Q9.1 System design interview ko structure kaise karein?

**Answer:** Requirements clarify (users, use cases, SLO, consistency/security), rough capacity estimate, APIs/data model, high-level components, bottlenecks/failure modes, scaling, observability, security aur cost. Har decision ke trade-offs batao; interviewer feedback ke baad evolve karein.

**Follow-ups**
- **CAP?** Network partition ke waqt distributed system consistency aur availability mein trade-off; "just choose consistency vs availability" kehna incomplete.
- **Back-of-envelope?** Daily users × actions/user ÷ seconds/day, peak multiplier, bytes/event × retention — order-of-magnitude sanity check.

---

### Q9.2 Scalable .NET + Angular application design?

**Answer:**

```
Browser (Angular SPA, CDN)
   → WAF / API Gateway: TLS, auth, rate limiting
   → Load balancer → stateless ASP.NET Core API (containers, autoscaling)
        → Redis cache
        → SQL Server (primary + read replicas), optional NoSQL/Search
        → Message broker → background workers
        → Blob storage (files)
   Cross-cutting: Identity provider (OIDC/JWT), Secrets vault, OpenTelemetry, CI/CD
```

**Key decisions:** Stateless services (JWT; Redis for shared state) → horizontal scaling. Modular monolith first, microservices only where scale/team boundaries demand it. CQRS for reads vs writes. Outbox pattern for reliable events. Polly (retry, circuit breaker, timeout, bulkhead).

---

### Q9.3 Cache, replicas, sharding aur consistent hashing?

**Answer:** Cache hot/read-heavy data aur latency reduce karta hai; read replica read scaling/DR mein help; sharding data ko multiple nodes par distribute karta hai. Consistent hashing node changes par remapping reduce kar sakta hai. Pehle measured bottleneck aur access pattern establish karein.

**In-memory vs Redis:**
| | `IMemoryCache` | Redis |
|---|---|---|
| Location | App process mein | External shared |
| Multi-instance | Each instance ka own copy (inconsistent) | Shared by all instances |
| Survives restart | Nahin | Haan (with persistence) |

Use in-memory for small, rarely-changing per-node data. Redis for scaled-out: sessions, shared caches, rate-limit counters, distributed locks.

**Pitfall:** Cache invalidation/staleness, hot keys, stampede. Early shard na karein — operational complexity badhta hai.

---

### Q9.4 Excel-based tax filing/auditing platform design?

**Answer:** Excel plugin trusted UI/client, business authority nahin. BFF/API par user/tenant authorization, schema/template version, file size/type limits, malware scanning, row validation, idempotent submission enforce karein. Import command durable job state ke saath queue/worker tak; parse/validate, domain command aur audit result store karein.

**Follow-ups**
- **SQL Server vs Cosmos DB?** Relational constraints, joins, transactions → SQL Server; known partition-key access, scale, flexible documents → Cosmos. Cost/consistency/migration compare karein.
- **CQRS zaroori?** Separate write command from query projection only when workflow/read-model needs justify it.

---

### Q9.5 Production incident kaise handle karein?

**Answer:** Impact/SLO/region-scope identify; dashboard/log/trace/alarms inspect. **Mitigate first** (rollback/traffic shift/scale), then RCA. Blameless postmortem, action owners ke saath.

**STAR example:**
- **Situation:** Orders list screen 10–15 seconds load, p95 API 12s, DB CPU spiking.
- **Action:** API call slow confirm (network tab), N+1 queries detect (lazy loading), non-SARGable `WHERE YEAR(CreatedAt)`, Angular duplicate requests (double subscribe + undebounced search).
- **Fix:** Date range predicate, covering index, `Select` DTO projection `AsNoTracking`, keyset pagination. Angular: single `toSignal` source, `debounceTime(300)`, `switchMap`, `OnPush`.
- **Result:** p95 dropped from 12s to ~300ms; tests added.

---

## 10. Logging, Monitoring & Observability

### Q10.1 Structured logging aur correlation/trace IDs kyun?

**Answer:** Structured log message ko template + named properties ke roop mein store karta hai — query/filter reliable hote hain. Correlation ID related request/events ko link karta hai; distributed trace ke TraceId/SpanId request path aur timing represent karte hain. Metrics aggregate behavior/SLOs dikhate hain.

**Pitfall:** Full request bodies/tokens/PII log karna, high-cardinality metric labels, duplicate logging — cost/privacy risk. Log levels aur redaction define karein.

**Follow-ups**
- **OpenTelemetry?** Vendor-neutral APIs/SDKs/protocols traces, metrics, logs instrumentation/export; backend storage/query product alag.
- **Sampling?** High-volume traces mein representative subset retain karein; errors/slow events retain karne ki policy.

---

## 11. AWS

### Q11.1 Lambda vs containers/EC2 kab?

**Answer:** Lambda event-driven, bursty, bounded stateless work ke liye operational burden kam; containers ECS/Fargate long-running services, custom runtime aur predictable workloads; EC2 OS/network/runtime control. Workload duration, cold start, concurrency, cost model se choose karein.

**Follow-ups**
- **Lambda retries?** Trigger par depend; at-least-once delivery assume karke idempotency design.
- **Fargate vs ECS?** ECS orchestrator; Fargate serverless compute option. EKS managed Kubernetes.

---

### Q11.2 AWS par ASP.NET API + Angular deploy kaise karein?

**Answer:** Angular static assets → private S3 origin → CloudFront (HTTPS, caching, SPA fallback); API → ECS/Fargate ya Lambda behind ALB/API Gateway; data → RDS/DynamoDB; IAM roles, VPC, TLS, secrets, CI/CD, CloudWatch/OpenTelemetry, backups aur WAF.

**Code examples:**

```csharp
// S3 upload
var client = new AmazonS3Client(RegionEndpoint.APSouth1); // credentials from role
var transfer = new TransferUtility(client);
await transfer.UploadAsync(stream, "my-bucket", $"docs/{id}.pdf");

// Pre-signed URL
var url = client.GetPreSignedURL(new GetPreSignedUrlRequest {
    BucketName = "my-bucket", Key = $"docs/{id}.pdf",
    Verb = HttpVerb.GET, Expires = DateTime.UtcNow.AddMinutes(10) });
```

```csharp
// Lambda handler
public class Function {
    public async Task<string> Handler(SQSEvent evt, ILambdaContext ctx) {
        foreach (var m in evt.Records)
            ctx.Logger.LogInformation($"Processing {m.Body}");
        return "done";
    }
}
```

**Pitfall:** Access key source/config mein rakhna, bucket public karna, untrusted client ko arbitrary key dena.

---

### Q11.3 DynamoDB data model design?

**Answer:** Access patterns pehle likhein; partition/sort key aur secondary indexes un queries ko support karne ke liye design karein. Partition key distribution, hot-key risk, item size, conditional writes, TTL/streams validate karein.

**Pitfall:** Relational schema seedha copy karna ya arbitrary ad-hoc querying expect karna.

---

### Q11.4 AWS IAM aur networking kaise secure karein?

**Answer:** Least-privilege IAM roles/policies, workload identity, short-lived credentials; private/public subnet routes, security groups, NACLs, VPC endpoints, egress paths define karein.

**Pitfall:** Code/repo mein static access keys, wildcard `*` policies, public S3 exposure, overly broad trust relationship.

**Follow-ups**
- **Security group vs NACL?** Security group stateful resource-level virtual firewall; NACL subnet-level stateless rule list.
- **ALB vs NLB?** ALB Layer 7 HTTP(S) routing; NLB Layer 4 high-throughput/TCP. Protocol decide karta hai.
- **CloudFormation vs CDK vs Terraform?** CloudFormation AWS-native; CDK C#/TypeScript se CloudFormation synthesize; Terraform multi-cloud state management. Team expertise aur drift governance decide karein.

---

### Q11.5 AWS messaging aur Well-Architected trade-offs?

**Answer:** SQS buffering/work distribution; SNS fan-out; EventBridge event routing; Kinesis retained ordered partitioned stream. Well-Architected pillars — operational excellence, security, reliability, performance efficiency, cost optimization, sustainability — design review lens hain.

**Follow-ups**
- **CloudFront/Route 53?** CloudFront edge CDN/cache/security delivery; Route 53 DNS/domain routing/health-check.
- **AWS cost optimization?** Tag/account allocation, right-size, autoscale/schedule, storage lifecycle, commitments only after stable baseline.

---

## 12. Angular

### Q12.1 Standalone components aur NgModules mein kya difference hai?

**Answer:** Standalone component apni dependencies/imports declare karke feature composition karta hai — modern Angular mein recommended/default. NgModules older/legacy organization. Migration incremental ho sakti hai.

**Follow-ups**
- **`@if`/`@for`?** Built-in template control flow; `@for` mein stable `track` expression define karna DOM efficiency ke liye important.
- **`inject()` vs constructor injection?** Dono DI resolve karte hain; `inject()` functional guards/interceptors/factories aur field initialization mein useful.
- **Lifecycle hooks order?** `ngOnChanges` → `ngOnInit` → `ngDoCheck` → `ngAfterContentInit` → `ngAfterContentChecked` → `ngAfterViewInit` → `ngAfterViewChecked` → `ngOnDestroy`.

---

### Q12.2 OnPush change detection aur signals?

**Short answer:** Default mein component tree har async event par check hoti hai (Zone.js). `OnPush` mein sirf tab check hota hai jab input reference change ho, component mein event fire ho, async/signal emit kare, ya `markForCheck()` call ho.

```ts
@Component({ changeDetection: ChangeDetectionStrategy.OnPush, ... })
```

**OnPush + immutable object:**
```ts
// Parent mutate karta hai property — OnPush child update nahin hoga (reference unchanged)
this.user.name = 'X';  // BAD

// Reference replace karein
this.user = { ...this.user, name: 'X' };  // GOOD
```

**Signals:** Fine-grained reactivity — sirf affected views update. Zoneless change detection enable karte hain.

```ts
count = signal(0);
double = computed(() => this.count() * 2);
constructor() { effect(() => console.log(this.count())); }
this.count.set(5);
this.count.update(c => c + 1);
```

**Follow-ups**
- **Signals vs RxJS?** Signals synchronous state/derived UI state ke liye; RxJS async streams, cancellation, operator composition ke liye. Interop: `toSignal(obs$)`, `toObservable(sig)`.

---

### Q12.3 RxJS operators — switchMap, mergeMap, concatMap, exhaustMap?

| Operator | Behaviour | Use case |
|---|---|---|
| `switchMap` | Previous inner cancel karta hai jab naya aata hai | Typeahead search, route-param loads |
| `mergeMap` | Sab concurrent run karta hai | Independent parallel calls, fire-and-forget |
| `concatMap` | Queue karta hai; ek at a time in order | Sequential saves jahan order matters |
| `exhaustMap` | Inner active ho to nayi values ignore karta hai | Login/submit button (double-submit prevent) |

```ts
search$.pipe(switchMap(q => this.http.get(`/api/s?q=${q}`)));
clicks$.pipe(exhaustMap(() => this.http.post('/api/pay', body)));
```

**Pitfall:** `switchMap` writes (POST/PUT) par cancel request already server par reach ho sakta hai — mutations ke liye `concatMap`/`exhaustMap` use karein.

---

### Q12.4 Observable vs Promise?

| | Promise | Observable |
|---|---|---|
| Values | Ek (ya error) | 0..n values over time |
| Execution | Eager | Lazy (runs on subscribe) |
| Cancellation | Nahin | Haan (unsubscribe) |
| Operators | `then/catch/finally` | 100+ operators |
| Use | One-shot async | Streams, events, WebSockets, HTTP in Angular |

**Pitfall:** `HttpClient` cold Observable hai — subscribe karne se execute hota hai; double subscribe → double HTTP call.

---

### Q12.5 Memory leaks prevent kaise karein Angular mein?

**Answer:** `async` pipe auto-unsubscribe karta hai. `takeUntilDestroyed()` (Angular 16+) use karein; older pattern: `destroy$ = new Subject<void>()` + `takeUntil(this.destroy$)` + complete in `ngOnDestroy`.

```ts
constructor() { this.data$.pipe(takeUntilDestroyed()).subscribe(...); }
```

**Pitfall:** `setInterval`/`setTimeout` clear na karna, dynamic components destroy na karna, event listeners remove na karna.

---

### Q12.6 Reactive forms vs template-driven forms?

| | Reactive | Template-driven |
|---|---|---|
| Model defined in | Component class (`FormGroup`, `FormControl`) | Template (`ngModel`) |
| Testing | Easy bina DOM ke | DOM chahiye |
| Dynamic forms | Easy (`FormArray`) | Mushkil |
| Typed forms | Angular 14+ | Limited |
| Best for | Complex, dynamic, large forms | Simple forms/prototypes |

---

### Q12.7 HTTP interceptors aur route guards?

**HTTP interceptors:** JWT attach, global error handling (401 → refresh/redirect), loading spinner, retry, correlation IDs.
```ts
export const authInterceptor: HttpInterceptorFn = (req, next) => {
  const token = inject(AuthService).token();
  return next(token ? req.clone({ setHeaders: { Authorization: `Bearer ${token}` } }) : req);
};
```

**Route guards:** `canActivate`, `canDeactivate` (unsaved changes), `canMatch` (lazy loading ke liye). Client-side guards UX only — real security API par enforce hoti hai.

---

### Q12.8 Angular performance optimize kaise karein?

- **Change detection:** OnPush everywhere, signals, avoid function calls in templates (pure pipes/computed use karein), consider zoneless.
- **Rendering:** `@for` with `track`, virtual scrolling (CDK), pagination, `@defer` blocks.
- **Loading:** Lazy-load routes/components, `NgOptimizedImage`, SSR/hydration, bundle analysis.
- **Bundle:** Production builds (AOT + tree-shaking), `source-map-explorer`, budgets in `angular.json`.
- **RxJS:** `takeUntilDestroyed`/`async` pipe, `shareReplay` for duplicate HTTP calls, debounce inputs.
- **Diagnose:** Angular DevTools profiler, Chrome Performance, Lighthouse, Core Web Vitals.

---

### Q12.9 Angular 19 key features?

- **Standalone by default:** Components, directives, pipes standalone unless opted out.
- **Signals maturity:** `input()`, `output()`, `model()`, `viewChild()` stable; `linkedSignal`, `resource()`/`rxResource()` experimental.
- **Incremental hydration:** `@defer` triggers ke saath SSR apps ke liye; event replay.
- **Zoneless:** Experimental via `provideExperimentalZonelessChangeDetection()`.
- **Control flow:** `@if`, `@for` (required `track`), `@switch`, `@let`, `@defer`.
- **Tooling:** esbuild/Vite application builder, HMR for styles/templates.

---

## 13. TypeScript

### Q13.1 `any`, `unknown`, `never` aur `void`?

**Answer:** `any` type checking bypass karta hai; `unknown` value ko use karne se pehle narrow karwata hai; `never` impossible/unreachable result; `void` function meaningful return value provide nahin karta.

**Pitfall:** TypeScript types compile-time par erase hote hain; runtime validation automatically nahin hoti. API JSON ko type assertion se trusted banana validation nahin.

**Follow-ups**
- **`interface` vs `type`?** Interface declaration merging/extension; type unions/intersections/conditional compositions ke liye flexible.
- **Union narrowing?** `typeof`, `in`, custom type guards ya discriminant field. Exhaustive `never` branch future cases detect kar sakta hai.
- **`as const` vs `satisfies`?** `as const` literal/readonly inference narrow; `satisfies` expression ko target shape check karta hai.

---

### Q13.2 Generics, variance aur strict null checks?

**Answer:** Generics reusable type-safe APIs banate hain; constraints valid operations enforce karte hain. Variance batata hai generic types subtyping mein kaunsi direction compatible hain. `strictNullChecks` null states ko type system mein explicit karta hai.

**Follow-ups**
- **`readonly` runtime immutable?** Nahin; TypeScript compile-time access restriction. Runtime `Object.freeze` alag concern.
- **Enums?** Runtime representation aur numeric reverse mapping; literal unions/`as const` alternatives simpler APIs mein.

---

## 14. JavaScript & Runtime

### Q14.1 `var`, `let`, `const`, hoisting aur Temporal Dead Zone?

**Answer:** `var` function-scoped; declaration hoist hokar `undefined` se initialize hoti hai. `let`/`const` block-scoped; TDZ mein access par `ReferenceError`. `const` binding reassign nahin hoti, par object properties automatically immutable nahin.

```js
console.log(a); var a = 1;    // undefined
console.log(b); let b = 2;    // ReferenceError (TDZ)
for (var i = 0; i < 3; i++) setTimeout(() => console.log(i)); // 3 3 3
for (let j = 0; j < 3; j++) setTimeout(() => console.log(j)); // 0 1 2
```

**Best practice:** Default `const`, `let` when reassigning, `var` avoid karein.

---

### Q14.2 JavaScript coercion, equality, `this` aur prototype chain?

**Answer:** `===` type coercion ke bina compare karta hai; `==` coercion rules apply karta hai — app logic mein strict comparison prefer karein. `this` call-site par depend karta hai; arrow function apna `this` bind nahin karti, lexical `this` capture karti hai. Property lookup prototype chain traverse karta hai.

**Pitfall:** Prototype pollution risk — untrusted keys ko normal object par blindly merge na karein.

---

### Q14.3 Event loop, microtask aur macrotask order?

**Answer:** Stack execute → microtask queue drain (Promise reactions, `await` continuations) → macrotask queue (setTimeout, I/O). Current task ke baad microtasks drain hote hain before next task.

```js
console.log('A');
setTimeout(() => console.log('timer'));
Promise.resolve().then(() => console.log('promise'));
console.log('B');
// Output: A, B, promise, timer
```

**Follow-ups**
- **CPU-heavy task UI freeze?** Main thread busy; chunked work ya Web Worker use karein.

---

### Q14.4 `Promise.all`, `allSettled`, `race`, `any`?

| Method | Behaviour |
|---|---|
| `Promise.all` | Sab fulfill par ordered results; first rejection par reject |
| `allSettled` | Sabka status/result deta hai |
| `race` | First settled (fulfill ya reject) |
| `any` | First fulfillment; sab reject par `AggregateError` |

**Pitfall:** `Promise.all` rejection par baaki operations cancel nahin karta. `AbortController` se explicitly abort karein.

---

### Q14.5 Closures, IIFE, spread/rest, debounce/throttle?

**Closures:** Function apne lexical scope ke saath bundled; outer variables access kar sakta hai. Memory leak risk: event listeners remove na karna, `setInterval` clear na karna, captured DOM nodes.

**IIFE:** Immediately Invoked Function Expression — private scope banata hai; ES modules ne mostly replace kar diya.

**Spread vs Rest:** Spread expands iterable; Rest collects remaining elements.

**Debounce vs Throttle:**
| | Debounce | Throttle |
|---|---|---|
| Behaviour | Events stop hone ke baad N ms baad run | At most once every N ms |
| Use | Search-as-you-type, auto-save | Scroll/mousemove, button spam |

RxJS: `debounceTime`, `throttleTime`.

---

### Q14.6 ESM aur CommonJS modules?

**Answer:** ESM static `import`/`export`, live bindings, top-level `await`, browser-native. CommonJS `require`/`module.exports` runtime loading, older Node ecosystem.

**Pitfall:** Named/default export interop, cyclic dependencies, module resolution aur side effects ignore karna. Tree-shaking guarantee nahin — package metadata/import style/bundler matter.

**Follow-ups**
- **Circular imports?** Initialization order/partially initialized exports; dependency direction simplify karein.

---

## 15. GitHub Actions, Deployment & Artifactory

### Q15.1 Secure CI/CD pipeline kaise banayein?

**Answer:** Trigger par least-privilege jobs; deterministic restore; build, tests, lint/security checks, artifact creation, provenance/versioning; immutable artifact ko environment approvals ke through promote. Deploy ke baad health/SLO verify, rollback strategy.

**Pitfall:** PR workflows ko secrets dena, `pull_request_target` mein untrusted code execute karna, floating third-party action tags, broad `GITHUB_TOKEN` permissions, long-lived cloud secrets risky hain.

**Follow-ups**
- **OIDC kyun?** GitHub job ko cloud provider ke saath short-lived federated credential exchange; static key store/rotate ki need kam.
- **Cache vs artifact?** Cache performance optimization, evict ho sakti hai; artifact reliable workflow output transfer/store ke liye.
- **Reusable workflow vs composite action?** Workflow jobs/orchestration reuse; composite action steps package/reuse.

---

### Q15.2 Blue-green, canary, rolling aur feature flags?

**Answer:** Rolling instances ko batches mein update; blue-green parallel environments switch; canary small traffic new build ko; feature flag deployment se code release aur user exposure alag karta hai.

**Pitfall:** Flag stale rehne se branching complexity. Percentage rollout ko health metrics ke bina automatic safe na samjhein. Rollback ke baad schema compatibility check karein.

---

### Q15.3 .NET service ke liye production Docker image?

**Answer:** Multi-stage Dockerfile: SDK stage restore/build/publish; minimal ASP.NET runtime stage mein sirf published output. Layer/cache order stable rakhein, pinned base image/digest, vulnerability scanning, non-root user, explicit port/health behavior, resource limits, graceful shutdown, logs to stdout/stderr.

**Pitfall:** SDK/compiler/source/test files runtime image mein bhejna; root user; floating base tag; secrets in `ENV`/`ARG`; oversized image.

---

### Q15.4 JFrog Artifactory ka role?

**Answer:** Build artifacts aur packages ko proxy, cache, host, version aur promote karta hai. Local/remote/virtual repository choices, retention, immutable release, access control, provenance/SBOM aur vulnerability scanning.

**Pitfall:** Release ko mutable `latest` label se identify karna; untrusted remote packages ka blind consumption; credentials leakage.

---

## 16. Testing, BDD & Compliance

### Q16.1 Unit, integration, contract aur end-to-end tests ka balance?

**Answer:** Unit tests isolated logic fast test karte hain; integration tests real infrastructure boundaries (DB, HTTP pipeline) validate; consumer-driven contract tests service compatibility; E2E user/business flow. Pyramid mein fast focused tests zyada, expensive E2E targeted.

**Follow-ups**
- **BDD kya hai?** Business-readable Given/When/Then examples se behavior specify/validate. Tool use karna alone collaboration guarantee nahin.
- **Test doubles?** Dummy unused parameter; stub canned response; spy calls record; mock expectations verify; fake lightweight working implementation.
- **`WebApplicationFactory`?** ASP.NET Core integration-test host create/configure karke routing, middleware, DI aur HTTP behavior test.

---

### Q16.2 TDD/BDD, test doubles, coverage aur async tests?

**Answer:** TDD Red-Green-Refactor; BDD shared Given-When-Then behavior/acceptance examples; unit tests fast isolated, integration tests boundaries, E2E critical flow.

```csharp
[Fact]
public async Task GetOrder_WhenNotFound_ReturnsNull()
{
    var repository = new Mock<IOrderRepository>();
    repository.Setup(r => r.GetByIdAsync(42)).ReturnsAsync((Order?)null);
    var service = new OrderService(repository.Object);

    var result = await service.GetOrderAsync(42);

    Assert.Null(result);
    repository.Verify(r => r.GetByIdAsync(42), Times.Once);
}
```

BDD example:
```gherkin
Feature: Order discount
  Scenario: Loyalty customer gets a discount
    Given a loyalty customer with a cart total of 200
    When the customer checks out
    Then the payable amount should be 190
```

**Follow-ups**
- **Frameworks/tools?** xUnit/NUnit/MSTest; Moq/NSubstitute/FakeItEasy; FluentAssertions; AutoFixture/Bogus. Angular: Jasmine/Karma ya Jest.
- **Async errors test?** Test `async Task` ho, method `await` karein, `ThrowsAsync` se expected exception assert karein. `.Result`/`.Wait()` se deadlock/wrapped exceptions risk.
- **Coverage/mutation?** Coverage gaps identify karne ka signal, not a target. Stryker.NET mutation testing se pata chalega ki tests meaningful changes detect karte hain.
- **Deterministic tests?** `TimeProvider`/`IClock`, filesystem abstraction, fixed fake values; real clock/random seed par depend na karein.

---

### Q16.3 GDPR aur HIPAA ko engineer ke roop mein kaise approach karein?

**Answer:** Pehle product/legal/security team se applicability aur data scope confirm karein. Data minimization, purpose/access limitation, encryption, auditability, retention/deletion, incident process implement karke evidence maintain karein.

**GDPR:** EU residents ka personal data. Data subject rights: access, rectification, erasure, portability. Breach notification 72 hours authority ko. Consent records purpose/timestamp/version ke saath.

**HIPAA:** US healthcare — Protected Health Information. Technical safeguards: unique user IDs, least privilege, audit controls, encryption (AES-256 at rest, TLS 1.2+ in transit), session timeouts, backup/DR. Business Associate Agreement vendors ke saath.

**Pitfall:** "Encryption on hai, isliye compliant" ya coding interview mein legal certification claim karna. Compliance organizational/process responsibility hai.

**Follow-ups**
- **PII log ho jaye to?** Exposure scope contain, access restrict, incident procedure follow, retention/deletion policy apply.
- **RBAC/claims/ABAC?** Roles group permissions; claims describe subject/resource facts; ABAC attributes/context evaluate karta hai. Authorization server-side har resource boundary par enforce karein.

---

## 17. Agile & Team Collaboration

### Q17.1 Scrum aur Kanban ka practical difference?

**Answer:** Scrum time-boxed Sprint, defined accountabilities/events/artifacts aur Sprint Goal. Kanban continuous flow, WIP limits aur cycle/lead time manage karta hai. Team ka work type aur delivery constraints decide karte hain.

**Pitfall:** Velocity ko individual performance metric banana ya points ko time/commitment samajhna. Ceremony complete karna outcome delivery ke barabar nahin.

**Follow-ups**
- **Sprint commitment?** Team Sprint Goal forecast karti hai; changing information par scope negotiate ho sakta hai — quality bar/DoD sacrifice nahin.
- **Retrospective result?** Chhota measurable improvement action owner aur review date ke saath; blame session nahin.
- **Lead time vs cycle time?** Lead time request se delivery; cycle time work start se completion.

---

## 18. Coding, DSA & Problem Solving

### Q18.1 Algorithm interview problem kaise approach karein?

**Answer:** Problem apne words mein restate karein, inputs/constraints/edge cases poochhein, brute-force baseline batayein, suitable data structure/algorithm choose karke correctness explain karein, complexity derive karein, phir code + tests. Assumptions aloud rakhein.

**Pitfall:** Sorted input assumption, duplicates, empty/single item, integer overflow, null, stable ordering aur off-by-one ignore na karein.

---

### Q18.2 Core algorithm problems — approach aur complexity?

| Problem | Short approach | Complexity |
|---|---|---|
| Reverse string / palindrome | Two pointers se ends compare; normalization define karein | O(n) time; O(1) extra |
| Second-largest distinct | One pass mein largest/second-largest distinct track | O(n) time, O(1) space |
| Fibonacci | Iterative rolling previous two; recursion-naive exponential | O(n) time, O(1) space |
| Rotate matrix 90° clockwise | Transpose, phir har row reverse | O(n²) time, O(1) extra |
| Two Sum | Complement hash map mein check/store | O(n) average, O(n) space |
| Queue using two stacks | Enqueue input stack; output empty par transfer | Amortized O(1) |
| Longest substring without repeats | Sliding window + last-seen index | O(n) time |
| Maximum subarray (Kadane) | `max(current, prior + value)`; global max track | O(n) time, O(1) space |
| Merge sorted lists | Dono heads compare karke smaller output | O(m+n) |
| Climbing stairs (1 or 2) | `ways[n] = ways[n-1] + ways[n-2]` iterative | O(n) time, O(1) space |
| Unweighted shortest path | BFS from source | O(V+E) |
| Weighted shortest path | Dijkstra + priority queue (non-negative edges only) | O((V+E) log V) |

```csharp
// Palindrome check
static bool IsPalindrome(string value) {
    if (value is null) throw new ArgumentNullException(nameof(value));
    var left = 0; var right = value.Length - 1;
    while (left < right) {
        while (left < right && !char.IsLetterOrDigit(value[left])) left++;
        while (left < right && !char.IsLetterOrDigit(value[right])) right--;
        if (char.ToLowerInvariant(value[left]) != char.ToLowerInvariant(value[right])) return false;
        left++; right--;
    }
    return true;
}

// Second highest salary
var secondHighest = employees
    .Select(e => e.Salary).Distinct()
    .OrderByDescending(s => s).Skip(1)
    .Select(s => (decimal?)s).FirstOrDefault();

// Department grouping
var byDept = employees.GroupBy(e => e.Department)
    .Select(g => new { Dept = g.Key, Count = g.Count(), Avg = g.Average(e => e.Salary) });
```

---

### Q18.3 BFS, topological sort, LRU cache?

**BFS (Open the Lock):** Har 4-digit state graph node; ek digit ±1 wrap karna edge; deadends blocked. Sab edges equal cost → BFS minimum moves deta hai.

**Topological sort (Course Schedule II):** Prerequisite graph build karke Kahn algorithm: indegree count; zero-indegree queue; pop course, append result, outgoing neighbors decrement. Sab courses processed → valid order; warna cycle.

**LRU Cache:** `Dictionary` + `LinkedList` ya .NET `LinkedList<(int key, int value)>` combined. `Get`: node tail pe move; `Put`: existing update ya add + head/tail eviction.

---

### Q18.4 Process/thread, TCP/UDP aur "URL enter karne par kya hota hai"?

**Answer:** Process ka isolated virtual address space/resources; ek process ke threads heap share karte hain, stacks/execution state alag. TCP ordered reliable byte stream; UDP datagrams mein delivery/order guarantee nahin. URL flow: browser URL parse/DNS resolve → connection/TLS negotiate → HTTP request → response → resources load/render.

**Follow-ups**
- **Concurrency vs parallelism?** Concurrency multiple work ko interleave/manage; parallelism multiple cores par ek hi waqt execute.

---

### Q18.5 Puzzle questions mein reasoning kaise dikhayein?

**Answer:** Assumptions, allowed operations aur information constraints restate karein; brute-force possibilities se eliminate karke invariant/strategy explain karein. Interviewer ko sirf final answer nahin, reasoning sunayein.

- **Do ropes 45 min:** Rope A dono ends se, B ek end se light. A 30 min mein khatam; us waqt B ka doosra end light → remaining 15 min.
- **8 balls ek heavier:** 3 vs 3. Balance ho to bache 2 compare; tilt ho to heavier group ke 1 vs 1.
- **25 horses top 3:** 5 heats, group winners ki 6th race. A1 overall fastest; A2, A3, B1, B2, C1 ki 7th race top two ko A1 ke saath rank karein.

---

## 19. Behavioral, Scenarios & Resume Deep Dive

### Q19.1 "Tell me about yourself" ka jawab?

**Answer framework (60 seconds):** "Main `[years/domain]` se `[core stack]` par kaam kar raha/rahi hoon. Recent role mein `[product/problem]` ki ownership `[scope/team]` ke saath thi. Mera contribution `[specific decision/action]` tha, jiska measured result `[verified outcome]` hua. Ab `[target role]` mein `[relevant strength]` apply karna chahta/chahti hoon."

**Pitfall:** Resume ko line-by-line padhna, vague "hardworking" claims, team result ko apna sole result batana.

---

### Q19.2 Project deep-dive ka structure?

**Answer:** Business problem → constraints/SLO → architecture/boundaries/data flow → apna role → key decision aur alternatives → failure/security/scale handling → outcome/evidence → learned trade-off. Ek diagram aur ek incident/lesson ready rakhein.

**Senior follow-ups:**
- **"Yeh design kyun choose kiya?"** Alternatives, decision criteria, trade-off, evidence.
- **"Production mein kya fail hua?"** Impact, detection, mitigation, root cause, prevention; blame nahin.
- **"Agar dobara design karte?"** Current constraints ke saath specific change; hindsight ko original context se alag rakhein.

---

### Q19.3 Strength/weakness, conflict, failure aur "why this role"?

**Answer:** Behavioral story STAR (Situation, Task, Action, Result) mein; action mein "maine" kya kiya; result measurable/verified; learning kya hai. Weakness real but manageable — improvement behavior/evidence bataye. Role-fit answer job ke actual requirements ko apne experience se connect kare.

**Pitfall:** Fake weakness ("perfectionist"), blame, confidentiality breach, hypothetical ko real incident batana. Conflict story mein respectful disagreement aur decision ke baad execution dikhayein.

---

### Q19.4 Technical challenge, prevention aur influence-without-authority?

**Answer:** STAR mein real production problem: impact/constraints, aapki responsibility, root cause investigation, mitigation, permanent prevention (test/monitoring/design/process), outcome/evidence. Influence story: shared goal, stakeholders, evidence/options, alignment aur follow-through.

---

### Q19.5 Zero-downtime release kaise karein?

**Answer:** Backward-compatible expand-contract: additive schema first; deploy code compatible with old/new; backfill/validate; switch reads/writes; monitor; later remove obsolete schema. Immutable artifact once build, promote through gated environments; canary/blue-green/rolling.

**Pitfall:** Old aur new code concurrently running during rollout with incompatible schema; rollback to code incompatible with migrated data.

---

### Q19.6 Technical disagreement kaise handle karein?

**Answer:** Shared goal/data clarify; options compare karein evidence/trade-offs ke saath; decide via owner; commit after decision aur revisit with new data. ADR (Architecture Decision Record) mein document karein.

---

## 20. Company Interview Reports & Practice

### Microsoft India — SSE Job-Fitment Report (Jan 2026)

**Report evidence:** 45-minute round: behavioral questions, code review, phir BFS DSA. **Behavioral:** sabse technically challenging issue aur prevention; change kyun chahiye; manager authority ke bina influence. **Code review:** Python file — missing validation aur credentials console par print. **DSA:** *Open the Lock* — BFS/minimum moves.

**Match boundary:** Company aur SSE level match; 8–10 YOE aur .NET verify nahin. Code review Python tha — .NET interview ka proof na samjhein. **Confidence: medium for reported round/questions.**

#### Q. Code review mein credentials print ho rahe hain aur validation missing hai?

**Answer:** Secret ko output/log se remove; exposure hua ho to incident process, rotate/revoke credential, logs/access/retention assess. Validation ko trust boundary par add karein, malformed/empty/range/authorization cases test karein.

**Follow-ups**
- **Code-review priority?** First secret/data leakage aur security/correctness; then validation/edge cases, error handling, concurrency, maintainability.
- **Respectful feedback kaise?** Specific risk aur reproduction; question/suggestion, severity aur minimal safe fix.

---

### Amazon India — SDE II Report (May 2025)

**Report evidence:** ~3.5 years experience, Hyderabad. Rounds: Course Schedule II, ATM LLD, Amazon grocery parking-lot HLD, Reorganize String, system-design MCQs, Leadership Principles.

**Match boundary:** 3.5 YOE, .NET mention nahin. **Adjacent large-company senior-style preparation, na target experience ka direct benchmark.**

#### Q. ATM ka LLD design?

**Answer:** Scope clarify (account/card/auth, cash denominations, withdraw/deposit/balance, limits, audit). Entities: ATM/session, card/account, transaction, cash inventory, bank gateway; state transitions explicit; cash reservation aur account debit ko failure-safe/idempotent workflow banayein.

**Pitfall:** Cash jams, partial dispense, network timeout after debit, duplicate request, counterfeit/empty denomination. Distributed ATM/core-banking atomic transaction assume na karein.

---

### r/dotnet — Senior .NET Developer Interview Discussion (Nov 2025)

Community discussion — `IEnumerable<T>` / `IQueryable<T>` / `IAsyncEnumerable<T>` senior .NET evaluation question example ke roop mein. Company report nahin; community discussion hai.

**`IAsyncEnumerable<T>` kab?** Large result streams ko async enumerate karne ke liye jab server push kare ya database cursor-style streaming chahiye — pura set ek hi waqt memory mein load karne ke bajay.

---

## 21. Coverage Audit

Yeh section teen source files ke topics ka mapping deta hai. Source files mein 184 NET Full Stack prompts (OOP Q1.1–1.18, Middleware/DI Q2.1–2.17, aur Q1–Q149), 23 Hinglish sections, aur 9 new_interview_prep sections the.

| Topic area | Consolidated section |
|---|---|
| C#/.NET runtime, types, OOP, collections, exceptions, async/concurrency, GC | Sections 1, 2, 18 |
| ADO.NET, LINQ, data access | Section 3 |
| ASP.NET Core middleware, DI, Web API, MVC 4 | Section 4 |
| EF Core, repository, architecture patterns | Section 5 |
| Authentication, authorization, security, compliance | Sections 6, 16 |
| SQL Server — indexes, transactions, queries, migrations | Section 7 |
| Microservices, Saga, Outbox, messaging, migration | Section 8 |
| System design, cache, sharding, pagination | Section 9 |
| Logging, monitoring, observability | Section 10 |
| AWS — Lambda, ECS, S3, IAM, DynamoDB, networking, IaC | Section 11 |
| Angular — components, forms, RxJS, signals, lifecycle, performance | Section 12 |
| TypeScript — types, generics, narrowing | Section 13 |
| JavaScript — hoisting, closures, event loop, modules | Section 14 |
| GitHub Actions, Docker, deployment strategies, Artifactory | Section 15 |
| Testing (TDD/BDD), frameworks, GDPR, HIPAA | Section 16 |
| Agile, Scrum, Kanban | Section 17 |
| DSA, algorithms, puzzles | Section 18 |
| Behavioral, project deep-dive, resume defense | Section 19 |
| Company interview reports aur practice problems | Section 20 |

### Hinglish workspace ke source guides (cross-reference)

Workspace mein detailed source notes in master sections se align karte hain. Key references:
- **Monolith-to-Microservices Phase 1–11** → Sections 8–11, 14, 18.
- **AWS detailed/revision pairs** → Section 11.
- **Angular 19, LINQ, EF Core, Auth guides** → Sections 5, 6, 12.
- **Concurrency fundamentals** → Sections 1, 4, 9.
- **Roslyn/runtime scripting** → Section 19 Q19.7.
- **Migration phases checklist** → Section 8.

---

## 22. Quick Cheat Sheet

| Topic | One-liner |
|---|---|
| Value vs ref | Copy data vs copy reference; boxing costs allocations |
| async/await | State machine; frees thread during I/O; never `.Result` |
| Task vs Thread | Task = abstraction (pool); Thread = OS thread (~1 MB) |
| IEnumerable vs IQueryable | In-memory delegates vs expression trees → SQL |
| DI lifetimes | Transient new each time; Scoped per request; Singleton one; captive dep avoid |
| Middleware vs Filters | Pipeline-wide infra vs MVC-context logic |
| JWT | Signed (not encrypted) claims; validate sig/iss/aud/exp; short-lived + refresh |
| N+1 | Query per parent row; fix with Include/projection/batch |
| CQRS | Separate write and read models |
| OnPush | Checked only on input-reference change, event, async/signal, markForCheck |
| switchMap/mergeMap/concatMap/exhaustMap | Cancel old / parallel / queue / ignore new |
| Debounce vs throttle | Wait for pause vs limit rate |
| Signals vs RxJS | State vs streams; combine with toSignal/toObservable |
| Clustered vs non-clustered | Data order (1/table) vs separate lookup structure |
| EXISTS vs IN | Existence short-circuit, NULL-safe vs membership; prefer NOT EXISTS |
| Deadlock | Circular lock wait; consistent order, short tx, RCSI, retry |
| Pagination at scale | Keyset pagination + covering index, avoid COUNT(*) |
| Duplicate prevention | Unique constraint + idempotency key + atomic state change |
| Outbox pattern | Business state + event record ek local DB transaction mein |
| Strangler Fig | Old monolith functional, new service gradually traffic receive karta hai |

**Interview delivery tips:**
1. Pehle 1-line definition dein.
2. Trade-off add karein ("depends on… I'd choose X because…").
3. Real project example aur ek number ke saath back karein.
4. Jo pata nahin, honestly bolein aur kaise find karein batayein.

---

## 23. References & Research Limits

### Public interview-question collections (prep themes ke evidence ke roop mein)

- [GeeksforGeeks — C# Interview Questions](https://www.geeksforgeeks.org/c-sharp/c-sharp-interview-questions/)
- [InterviewBit — .NET Interview Questions](https://www.interviewbit.com/dot-net-interview-questions/)
- [InterviewBit — ADO.NET Interview Questions](https://www.interviewbit.com/ado-net-interview-questions/)
- [InterviewBit — Design Patterns Interview Questions](https://www.interviewbit.com/design-patterns-interview-questions/)
- [InterviewBit — Microservices Interview Questions](https://www.interviewbit.com/microservices-interview-questions/)
- [InterviewBit — SQL Interview Questions](https://www.interviewbit.com/sql-interview-questions/)
- [InterviewBit — AWS Interview Questions](https://www.interviewbit.com/aws-interview-questions/)
- [InterviewBit — Angular Interview Questions](https://www.interviewbit.com/angular-interview-questions/)
- [InterviewBit — TypeScript Interview Questions](https://www.interviewbit.com/typescript-interview-questions/)
- [InterviewBit — System Design Interview Questions](https://www.interviewbit.com/system-design-interview-questions/)
- [InterviewBit — Agile Interview Questions](https://www.interviewbit.com/agile-interview-questions/)
- [InterviewBit — Entity Framework Interview Questions](https://www.interviewbit.com/entity-framework-interview-questions)
- [InterviewBit — Behavioral Interview Questions](https://www.interviewbit.com/behavioral-interview-questions/)

### Official technical documentation

- [Microsoft Learn — C# Guide](https://learn.microsoft.com/en-us/dotnet/csharp/)
- [Microsoft Learn — ADO.NET Overview](https://learn.microsoft.com/en-us/dotnet/framework/data/adonet/ado-net-overview)
- [Microsoft Learn — ASP.NET Core security](https://learn.microsoft.com/en-us/aspnet/core/security/)
- [Microsoft Learn — LINQ in C#](https://learn.microsoft.com/en-us/dotnet/csharp/linq/)
- [Microsoft Learn — ASP.NET Core fundamentals](https://learn.microsoft.com/en-us/aspnet/core/fundamentals/)
- [Microsoft Learn — .NET Microservices Architecture](https://learn.microsoft.com/en-us/dotnet/architecture/microservices/)
- [Microsoft Learn — SQL Server performance](https://learn.microsoft.com/en-us/sql/relational-databases/performance/monitor-and-tune-for-performance)
- [AWS — Well-Architected Framework](https://docs.aws.amazon.com/wellarchitected/latest/framework/welcome.html)
- [AWS — Lambda Developer Guide](https://docs.aws.amazon.com/lambda/latest/dg/welcome.html)
- [AWS — DynamoDB Developer Guide](https://docs.aws.amazon.com/amazondynamodb/latest/developerguide/Introduction.html)
- [Angular — Overview](https://angular.dev/overview)
- [TypeScript — Handbook](https://www.typescriptlang.org/docs/handbook/intro.html)
- [GitHub Docs — GitHub Actions](https://docs.github.com/en/actions)
- [JFrog — Artifactory overview](https://docs.jfrog.com/artifactory/docs/jfrog-artifactory.md)
- [OpenTelemetry documentation](https://opentelemetry.io/docs/)
- [MDN — JavaScript execution model](https://developer.mozilla.org/en-US/docs/Web/JavaScript/Reference/Execution_model)
- [Manifesto for Agile Software Development](https://agilemanifesto.org/)

### Company interview reports

- **Microsoft India SSE (Jan 2026):** Search-result mein 45-minute round report — behavioral, code review (Python), BFS Open the Lock. Original post URL independently verify nahin hua; .NET/YOE match nahin.
- **Amazon SDE II India (May 2025):** ~3.5 YOE candidate — Course Schedule II, ATM LLD, parking-lot HLD. .NET stack mention nahin; not a target-profile match.
- **r/dotnet (Nov 2025):** Senior .NET evaluation discussion — `IEnumerable<T>`/`IQueryable<T>`/`IAsyncEnumerable<T>` example. Community discussion, company report nahin.

> **Note:** Documentation links technical behavior ke liye hain; interview frequency/public-report claims alag evidence maangte hain. Framework/cloud features version ke saath evolve hote hain — interview se pehle current official docs check karein.
