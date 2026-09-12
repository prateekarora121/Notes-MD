# C# / .NET Interview — Quick Revision Notes (Senior / Lead)

*Ye quick-revision notes `../Detailed Notes/A. C#-Interview-Guide.md` se derive kiye gaye hain — guide ke har section aur topic ko usi order mein cover karte hain, taaki bina guide khole har cheez brush up ho jaaye. Style Hinglish (Hindi+English mix), technical terms/code English mein.*

---

## Part I — Core Concepts: Type System & CLR

### C# / .NET aur CLR

**Q: C# kya hai?** Microsoft ka modern, OOP, strongly-typed language, .NET platform ka part. Features: OOP, strong typing, GC, structured exception handling, LINQ, async/await.

- **Strongly typed** = har variable ka type compile time par fixed; incompatible assignment bina explicit conversion ke compiler reject karta hai.
- C# vs Java: C# cross-platform via .NET; dono GC; dono multiple inheritance nahi (interfaces se); C# mein first-class `get`/`set` properties (Java mein getter/setter methods).

**CLR (Common Language Runtime)** = .NET ka execution engine. Responsibilities:
- **Memory management** — managed heap par auto allocate/deallocate, no manual malloc/free.
- **Security** — type safety enforce; managed code MSIL mein compile hoke CLR supervision mein chalta hai.
- **Exception handling** — structured model; unhandled exceptions CLR pakadta hai.
- **Garbage collection** — generational (Gen 0/1/2) auto reclamation.

> **Currency flag:** CAS (Code Access Security) legacy hai — .NET Core/.NET 5+ mein **exist nahi karta**. Modern security OS-level permissions, sandboxing (containers), code signing se. Interview mein CAS ko current mat bolo.

### .NET Framework vs .NET Core vs .NET 5–10

| | .NET Framework | .NET Core | .NET 5–10 |
|---|---|---|---|
| Platform | Sirf Windows | Cross-platform | Cross-platform |
| Perf | Baseline | Good | Har release faster |
| Desktop | Full | Limited | Full (Windows) |
| Cloud/Microservices | Limited | Haan | Haan (containers, gRPC, Minimal APIs) |

- Mid-2026: **.NET 10 = current LTS** (Nov 2025, C# 14 ke saath). LTS ~3 saal, STS ~18 months. Exact EOL dates Microsoft lifecycle docs se verify karo.

### Build & Execution Pipeline

1. Source compile (Roslyn `csc`) → 2. IL + metadata (`.dll`/`.exe`) → 3. Assembly linking → 4. JIT compile (runtime par IL→native, cached) → 5. Execution under CLR (GC, exceptions, security, threading) → 6. Runtime optimization (AOT, tiered compilation, NGEN).

> One-liner: **Compile → IL (.dll/.exe) → Link → JIT/AOT to machine code → Execute under CLR.**

### Value Types vs Reference Types

- **Value types**: `int, float, char, bool, struct, enum` — data directly store, typically stack par (jab tak box ya heap object ka field na ho).
- **Reference types**: `string, object, array, class, interface` — heap par data ka reference (pointer) store.

### var, object, dynamic

| Type | Description |
|---|---|
| `var` | Statically typed, compile time infer |
| `object` | Base of all types; cast chahiye |
| `dynamic` | Runtime resolve, no compile-time check (late binding, DLR overhead) |

`var` rules: type hint ke bina null nahi; declaration par initialize zaroori; type baad mein change nahi; sirf local scope. Use jab type obvious ho / anonymous types / LINQ; avoid jab readability giray.

### == vs Equals() vs ReferenceEquals()

- `==` default: value types ke liye values, reference types ke liye references. Operator-overload ho sakta hai.
- `Equals()` value equality, override ho sakta hai.
- `ReferenceEquals()` overloads bypass karke raw reference compare.

```csharp
public override bool Equals(object obj) => obj is Employee e && e.Id == Id;
public override int GetHashCode() => Id.GetHashCode();
public static bool operator ==(Employee a, Employee b) => a.Id == b.Id;
public static bool operator !=(Employee a, Employee b) => !(a == b);
```

> **Gotcha:** `Equals()` override karo to `GetHashCode()` bhi **zaroor** — warna `Dictionary`/`HashSet` toot jaata hai (equal objects ko same hash chahiye).

### Nullable Value Types & Nullable Reference Types (NRTs)

```csharp
int? age = null;              // Nullable<int>
Console.WriteLine(age ?? 18); // 18
string? name = null;          // NRT (C# 8+, opt-in <Nullable>enable</Nullable>)
```

- NRTs = **compile-time, opt-in** analysis; **koi runtime null-check add nahi hoti**; sirf warnings (CS8600-series), errors nahi by default.
- **Retrofit dard**: legacy codebase par enable karne se hazaron warnings. File-by-file rollout (`#nullable enable` pragma) prefer.
- NRT runtime par non-null prove nahi karta — reflection/JSON deserialization/un-annotated library se null aa sakta hai → NRE phir bhi.
- Escape hatch `!` (null-forgiving) compiler silence karta hai bina runtime check ke — overuse = same bug wapas.
- **Rollout answer**: `.csproj` per project enable, public APIs/DTOs se top-down fix, warnings-as-errors jab clean, `[NotNull]`/`[MaybeNull]`/`[AllowNull]` edge cases ke liye.

### Implicit vs Explicit Conversion

Implicit = no data loss, automatic. Explicit = possible loss, manual `(type)` cast.
```csharp
int x = 10; double y = x; int z = (int)y;
```

### Boxing & Unboxing

- **Boxing**: value type → object (stack→heap copy), `object o = 10;`
- **Unboxing**: object → value type, `int n = (int)o;`
- Cost: har boxing naya heap object (16–24 bytes overhead even 4-byte int ke liye) + GC Gen0 pressure; unboxing par runtime type check. Classic slow scenario: `ArrayList` (boxed ints) vs `List<int>` (generic, no boxing).

---

## Part II — Core Concepts: OOP

### The Four Pillars

Encapsulation, Inheritance, Polymorphism, Abstraction.
- **Encapsulation** — data direct access restrict, properties/methods se controlled expose.

### Inheritance vs Composition

- **Inheritance ("is-a")**: C# sirf **single class inheritance**. Deep hierarchy = tight coupling, base changes ripple.
- **Composition ("has-a")**: chhote interfaces combine — multiple interfaces implement karke behaviors compose. `List<T>` aur `Dictionary` dono `IEnumerable<T>` implement karte hain bina same tree ke.

> **Guideline:** composition > inheritance; inheritance sirf genuine "is-a" + shared invariants ke liye.

### Polymorphism: Compile-Time vs Runtime

- **Compile-time (static)**: method/operator overloading. Fayda: readability, no virtual overhead. Nuksan: fixed at compile time, ambiguity errors.
- **Runtime (dynamic)**: `virtual`/`override` ya interface, CLR resolve karta hai.
```csharp
Animal a = new Dog();  // compiler sees Animal; CLR resolves Dog.Speak() at runtime
a.Speak();             // Bark
```
Fayda: stable abstraction ke against naye implementations plug (Factory/Strategy/Template Method). Nuksan: dispatch overhead, weaker JIT inlining.

### Overloading vs Overriding vs Hiding (new)

| Keyword | Meaning |
|---|---|
| `virtual` | Override ho sakta hai |
| `override` | Virtual method ka naya impl |
| `new` | Base method **hide** karta hai (override nahi) |

Hiding **reference type** par resolve hota hai, object ke runtime type par nahi:
```csharp
Base b = new Derived();
b.Show();            // "Base" — reference type se resolved
((Derived)b).Show(); // "Derived"
```
`new` kab: backward compatibility; base method virtual nahi; base type se base behavior + derived se specialized; non-virtual framework method customize. Agar base control mein hai → `virtual`+`override` prefer.

### Interface vs Abstract Class

| Aspect | Interface | Abstract Class |
|---|---|---|
| Idea | Class CAN DO (capability) | Class IS (identity) |
| Methods | No impl (default C# 8+) | Abstract + implemented |
| Fields/Constructors | Nahi | Haan |
| Inheritance | Multiple | Single |

- Interface fayde: multiple inheritance, loose coupling, DI/mocking great, composition, default impls (C# 8+). Nuksan: no fields/ctors, over-engineering risk.
- Abstract class fayde: code reuse, shared state, organized hierarchy. Nuksan: single-inheritance, tighter coupling, directly instantiate nahi (par ctor derived creation par chalta hai).

> **Guideline:** interface se start; abstract class sirf jab shared state/behavior genuinely chahiye.

### struct vs class

| | struct (value) | class (reference) |
|---|---|---|
| Memory | Stack (unless boxed/field) | Heap |
| Passing | By value (copy) | By reference |
| Inheritance | Sirf interfaces | Class inheritance |
| GC | No overhead | GC-managed |
| Mutability | Usually immutable | Mutable |

Facts: by value pass (mutations original ko affect nahi); C# 10 se parameterless ctor allowed; mutable struct = common bug; boxing jab `object`/interface mein convert; **≤16 bytes** guideline (large struct copy expensive).

### sealed, static, partial classes

- `sealed` — inherit nahi ho sakti.
- `static` — instantiate nahi, sirf static members, static ctor ho sakta hai, utility methods ke liye.
- `partial` — class multiple files mein split; generated code (EF scaffolding, WinForms designer) + hand-written separate; parallel dev merge conflicts kam.

### Access Modifiers

| Modifier | Reach |
|---|---|
| `public` | Sab jagah |
| `private` | Sirf class ke andar |
| `protected` | Class + derived (kisi bhi asm) |
| `internal` | Same assembly |
| `protected internal` | Same asm OR derived |
| `private protected` | Same asm AND derived |

Top-level classes sirf `public`/`internal`; nested koi bhi.

### Records & record struct

`record` = reference type, built-in **value-based equality**, `ToString()`, convention se immutable. DTOs/value objects ke liye ideal.
```csharp
public record Product(int Id, string Name, decimal Price);
var p1 = new Product(1, "Laptop", 999.99m);
var p2 = new Product(1, "Laptop", 999.99m);
p1 == p2;                             // True (value equality)
var p3 = p1 with { Price = 899.99m }; // non-destructive mutation
```
`record struct` (C# 10) = same value-equality par value type, no heap alloc — chhote hot value objects (`Money`, `Coordinates`).

| | class | record | record struct |
|---|---|---|---|
| Equality | Reference | Value | Value |
| Storage | Heap | Heap | Stack (usually) |
| Mutability | Mutable | Immutable (init-only) | Mutable unless readonly |

### Pattern Matching & Switch Expressions

```csharp
string Describe(object obj) => obj switch
{
    int n when n < 0 => "negative",
    int n => $"number {n}",
    string s => $"string len {s.Length}",
    Product { Price: > 1000 } => "expensive",  // property pattern
    null => "nothing",
    _ => "unknown"
};
bool IsAdult(int age) => age is >= 18 and < 120;      // relational + logical (C# 9)
if (numbers is [1, 2, 3]) { }                         // list pattern (C# 11)
if (numbers is [var first, .., var last]) { }
```
Nested `if/else` + type-check chains ka idiomatic replacement.

---

## Part III — Constructors & Object Creation

Constructor = class ke same naam, no return type, object creation par auto-run, valid state + invariants enforce.

### Types of Constructors

1. **Default** — no params; koi ctor define karte hi implicit default hat jaata hai.
2. **Parameterized** — mandatory data enforce; DI ke saath common.
3. **Overloaded** — `: this(...)` chaining se duplication avoid.
4. **Static** — static members init; per type ek baar; no params/modifiers; **per class ek**; iske andar exception app crash → light rakho.
5. **Private** — external instantiation prevent; Singleton/factory/static utility.
   ```csharp
   private static Logger _instance;
   private Logger() { }
   public static Logger Instance => _instance ??= new Logger();
   ```
6. **Copy** — C# built-in nahi deta; hand-write karo.

### Constructors in Abstract Classes

Abstract class mein ctor ho sakta hai — derived object creation par run hota hai, shared state init + setup enforce. Directly instantiate nahi hoti par ctor phir bhi chalta hai.

### Step-by-Step Object Creation

`new` → CLR type determine → heap par memory allocate (fields **zero-init** ctor se pehle) → object reference (stack) → ctor resolution (compile time) → **base ctor pehle** (`object()`→derived) → field initializers → ctor body → reference assign → lifetime/GC.

> One-liner: **Memory alloc → zero init → ctor selection → base ctor → field initializers → ctor body → reference assignment.**

**Q&A:**
- Ctor virtual? **Nahi.**
- Exceptions throw? Haan, par sirf argument-validation.
- Static vs instance? Static per type ek baar; instance per object.
- Multiple static ctors? **Nahi, ek.**

Best practices: lightweight (no I/O/DB), args early validate, immutability prefer, DI mein ek unambiguous public ctor, complex init ke liye factory.

### init, required, Primary Constructors (C# 11/12)

```csharp
public string Name { get; init; }     // sirf construction time set
public required int Age { get; set; } // C# 11 — compiler forces set
var p = new Person { Name = "Alice", Age = 30 };
```
- `init` — immutability bina har-combination ctor overload ke.
- `required` — missing-data bug compile time par catch.

**Primary constructors** (C# 12, records se classes/structs tak):
```csharp
public class ProductService(IRepository repo, ILogger<ProductService> logger)
{
    public async Task<Product> GetAsync(int id) {
        logger.LogInformation("Fetching {Id}", id);
        return await repo.GetProductAsync(id);
    }
}
```
> Caution: primary ctor params auto fields nahi bante — compiler sirf tab hidden backing field banata hai jab param method body mein capture ho.

---

## Part IV — Intermediate: Members & Language Features

### Properties vs Fields

`public int F;` (no encapsulation) vs `public int P { get; set; }` (encapsulated). Property = get/set control.

### const vs readonly vs static

| | const | readonly | static |
|---|---|---|---|
| Set | Compile-time, declaration | Runtime, ctor | Instances ke across shared |
| Change baad? | Nahi | Nahi (ctor ke baad) | n/a |
| Storage | IL metadata mein baked | Instance/type field | Per type ek copy |

### ref vs out vs in

| | `ref` | `out` | `in` |
|---|---|---|---|
| Init pehle? | Haan | Nahi | Haan |
| Use | Read + modify | Value(s) return (`int.TryParse`) | Large struct by reference, read-only (copy avoid) |

### params, Named Parameters, Indexers

```csharp
void Print(params int[] n) { }        // variable args
Greet(age: 25, name: "Alice");        // named — order-independent
public int this[int i] { get => arr[i]; set => arr[i] = value; } // indexer
```

### Extension Methods

Static method jo existing type mein "add" hota hai bina modify kiye; first param par `this`.
```csharp
public static bool IsEven(this int n) => n % 2 == 0;
10.IsEven(); // True
```
Compiler `x.IsEven()` ko `Ext.IsEven(x)` rewrite karta hai — dikhne mein instance, under-the-hood static call. LINQ ke `Where`/`Select`/`OrderBy` sab `IEnumerable<T>` extensions hain.

### Generics — Why Not Slow

```csharp
public class Box<T> { public T Value { get; set; } }
```
- Compile-time: ek baar type-check, value types ke liye no boxing.
- Runtime: **saare reference-type instantiations ek impl share** karte hain; **har distinct value-type instantiation ka apna specialized native code** (`Box<int>` vs `Box<double>` alag; `Box<string>` reference types ke saath share).
- Net: boxing avoid, memory kam, aggressive JIT inlining.

### Tuples & Anonymous Types

```csharp
var t = ("John", 30); t.Item1;                 // unnamed
(string Name, int Age) n = ("John", 30); n.Name; // named
var a = new { Name = "John", Age = 30 };         // anonymous
```

### Reflection, Attributes, dynamic, ExpandoObject

```csharp
Type t = typeof(string);                 // compile-time
Type t2 = Type.GetType("System.String"); // runtime (from string)
[Obsolete("deprecated")] void Old() { }
dynamic v = "Hi"; v = 10;                // late binding, DLR overhead
dynamic e = new ExpandoObject(); e.Name = "John"; // runtime properties
```
`dynamic` compile-time checking skip (flexibility for COM interop/dynamic JSON/scripting) par DLR call-site caching se slower.

### yield return and Iterators

```csharp
IEnumerable<int> Get() { yield return 1; yield return 2; }
```
Compiler state machine banata hai (`IEnumerable/IEnumerator`) — deferred execution, har `MoveNext()` wahi se resume jahan chhoda.

### Fluent Interfaces

Methods same/related object return karte hain → chainable, sentence-like. Har fluent method chaining karta hai par har chaining fluent nahi (fluent = readable DSL target).
```csharp
public Calculator Add(int x) { _result += x; return this; }
```
.NET examples: `StringBuilder`, LINQ, ASP.NET middleware: `app.UseRouting().UseAuthentication().UseAuthorization().MapControllers();`

### Deep Copy vs Shallow Copy

| | Shallow | Deep |
|---|---|---|
| Copies | References (nested shared) | New instances (independent) |

```csharp
Person clone = (Person)this.MemberwiseClone(); // shallow — nested refs shared
```
Deep copy: recursive clone / serialize round-trip (simple par slow, sab serializable) / copy ctor (fastest par shape sync). Built-in "deep clone" nahi hai.

### Static Abstract/Virtual Interface Members & Generic Math (C# 11)

C# 11 se pehle interfaces sirf instance members. Ab `static abstract`/`static virtual` → **generic math**:
```csharp
public interface IShape<T> where T : IShape<T> {
    static abstract T Create(double size);
    static abstract double Area(T shape);
}
```
Headline: `System.Numerics.INumber<T>` — ek generic method jo `int`/`double`/`decimal`/custom numeric par real operators (`+`,`-`,`*`) ke saath kaam kare, bina per-type duplicate ya `dynamic`/reflection ke.

### Source Generators

Roslyn compiler plugin jo **compile time par code inspect karke additional C# source emit** karta hai — reflection-heavy metaprogramming ka modern alternative.
- Uses: `System.Text.Json` `[JsonSerializable]`+`JsonSerializerContext` (AOT-friendly), `LoggerMessage`, `[GeneratedRegex]`, MVVM/DI libraries.
- Kyun matter: industry Native AOT/trimming/fast cold-start ki taraf → compile-time codegen. "Main reflection ki jagah source generator use karunga AOT ke liye" = strong senior signal.
```csharp
[JsonSerializable(typeof(Product))]
internal partial class AppJsonContext : JsonSerializerContext { }
var json = JsonSerializer.Serialize(product, AppJsonContext.Default.Product);
```

---

## Part V — Delegates, Events & Lambdas

### Delegates

Type-safe function pointer — secure, type-checked, methods ko params ki tarah pass.
```csharp
public delegate void Notify(string msg);
Notify n = Console.WriteLine;
```
Types: **single-cast** (ek method), **multicast** (`+=`, registration order mein invoke).

### Func, Action, Predicate

| Delegate | Inputs | Returns | Use |
|---|---|---|---|
| `Func<T,TResult>` | 0–16 | value | Computations, LINQ Select/Where |
| `Action<T>` | 0–16 | void | Logging, ForEach |
| `Predicate<T>` | 1 | bool | Conditions, FindAll |

Fayde: loose coupling, callbacks, multicast, LINQ/async foundation. Nuksan: overuse traceability kharab, null delegate throw (`?.Invoke()` guard).

### Events

Event = delegate ko wrap, **Publisher–Subscriber**. `event` keyword external ko sirf `+=`/`-=` deta hai — invoke/overwrite nahi. Yahi core encapsulation vs raw public delegate.
```csharp
public event AlarmEventHandler OnAlarm;
OnAlarm?.Invoke("Wake up!");
```
Real scenarios: UI events, stock-price notification (`if(price != value)` par fire), e-commerce `OrderPlaced` decouple, microservices pub-sub (Kafka/RabbitMQ/SNS/SQS).

Fayde: loose coupling, multicast, stronger encapsulation. Nuksan: **un-unsubscribed handlers = memory leak** (long-lived publisher subscriber ka reference hold karta hai), debugging harder.

### Delegate vs Event

| | Delegate | Event |
|---|---|---|
| Assignable? | Haan | Nahi (sirf `+=`/`-=`) |
| Kaun invoke? | Access wala koi bhi | Sirf publisher class |
| Encapsulation | Weaker | Stronger |

> Analogy: delegate = car keys de dena (kabhi bhi drive); event = ride ke liye invite (publisher decide karta hai). Reusable libraries mein **events** expose karo.

### Lambda & Anonymous Methods

```csharp
Func<int,int> sq = x => x * x;                       // modern
Action<int> p = delegate(int x) { ... };             // older anonymous-method
```

---

## Part VI — Collections & LINQ

### LINQ Fundamentals

```csharp
var evens = numbers.Where(n => n % 2 == 0); // deferred — enumerate hone tak execute nahi
```

### IEnumerable vs IQueryable

| | IEnumerable | IQueryable |
|---|---|---|
| Namespace | System.Collections | System.Linq |
| Execution | In-memory (client) | Data source (server) |
| Best for | In-memory collections | Remote (EF Core) |
| Mechanism | LINQ to Objects | Expression trees → SQL |
| Perf | Saari rows load phir filter | Sirf matching fetch |

### Expression Trees & EF Core LINQ→SQL

Follow-up: *"EF Core mera LINQ SQL mein kaise convert karta hai?"* → **expression trees**.

`IQueryable<T>` par LINQ method lambda ko IL mein compile **nahi** karta — `Expression<Func<T,bool>>` ke liye ek **data structure (AST) banata hai jo code describe karta hai**, jise provider inspect/translate kare.

| | `Func<T,bool>` | `Expression<Func<T,bool>>` |
|---|---|---|
| Produce | Compiled IL delegate | Object graph (Expression tree) |
| Run | In-process | Provider tree walk karke translate |
| Use | IEnumerable/LINQ-to-Objects | IQueryable/EF Core |

**Pipeline:** LINQ → compiler Expression tree → `IQueryProvider` tree walk → EF query compiler → SQL AST → provider-specific SQL generator → ADO.NET execute → entities materialize.

- Har `Where`/`Select` **execute nahi** karta — tree ko naye node mein wrap karta hai (deferred).
- Jo translator recognize nahi karta wo **runtime throw** (EF Core 3.0+ default error; pehle silent client-eval = N+1 bug source). Gotcha: `c => MyHelper.IsValid(c)` translate nahi hota.
- `.ToList()` too early = translation tod deta hai (ab `IEnumerable`/`Func` bind hota hai, SQL push-down nahi).

> One-liner: "IQueryable methods `Expression<Func<>>` lete hain — compiler provider ko lambda ka *description* deta hai, provider (EF Core) usse walk karke SQL banata hai. Tree-walking + pattern-matching, koi magic nahi."

### PLINQ / AsParallel() Trade-offs

PLINQ (`.AsParallel()`) LINQ-to-Objects ko multiple cores par parallelize — source partition + results merge. **Free win nahi.**
```csharp
numbers.AsParallel().Where(IsExpensivePredicate).Select(Transform).ToList();
```
- **Help karta hai jab**: per-element work CPU-bound + non-trivial, collection kaafi bada, operations independent.
- **Hurt karta hai**: partitioning overhead, result merging cost (`.AsUnordered()` relax karta hai), over-subscription (cores > threads contention), **I/O-bound = wrong fit** (use `Parallel.ForEachAsync`), exceptions `AggregateException` mein.

> Senior answer: "PLINQ sirf tab jab profiling dikhaye ek CPU-bound LINQ-to-Objects op large collection par bottleneck hai. I/O fan-out ke liye `Parallel.ForEachAsync`/`Task.WhenAll`."

### IEnumerable vs ICollection vs IList vs IReadOnlyList

- `IEnumerable<T>` — read-only forward iteration (`foreach`); no Count/indexer.
- `ICollection<T>` — `Add`/`Remove`/`Count`/`Contains`.
- `IList<T>` — indexer, `Insert`/`RemoveAt`.
- `IReadOnlyList<T>`/`IReadOnlyCollection<T>` — read-only indexed/count bina mutation; public API return type (safer than `List<T>`, cheaper than `.AsReadOnly()`; par cast-back possible → signal, not hard guarantee).

### List vs Array

| | Array | List |
|---|---|---|
| Fixed size? | Haan | Nahi |
| Perf | Faster | Thoda slower |

`List<T>` internally array wrap karta hai + resizing (amortized doubling), bounds checking, safety → overhead.

### Dictionary vs Hashtable

| | Hashtable | Dictionary<K,V> |
|---|---|---|
| Type safety | Nahi (boxes values) | Haan (generic) |
| Thread safety | Legacy: single-writer/multi-reader | Nahi — use `ConcurrentDictionary` |
| Recommend | Avoid | Preferred |

### ReadOnlyCollection vs List

`new List<int>{1,2,3}.AsReadOnly()` — modification nahi; API boundaries par safety/immutability.

### Jagged vs Multidimensional Arrays

```csharp
int[][] jagged = new int[2][]; // array of arrays, independent row sizes, extra indirection
int[,] grid = new int[2, 3];   // rectangular, single contiguous block (faster for true grid)
```

### Covariance & Contravariance

- **Covariance (`out`)** — more-derived → base reference (output positions).
- **Contravariance (`in`)** — base → derived expected (input positions).
```csharp
IEnumerable<string> strings = ...;
IEnumerable<object> objects = strings; // out T ki wajah se allowed
```
`out T` = T sirf output positions mein → `IEnumerable<T>` safely covariant (read kar sakte, `Add(T)` nahi).

### String vs StringBuilder

| | String | StringBuilder |
|---|---|---|
| Mutable? | Nahi (har change new instance) | Haan (in place) |
| Perf | Repeated edits slower | Faster |

`string` immutable — har `+=`/`Replace`/`Substring` naya object. .NET literals **intern** karta hai (`"abc"=="abc"` reference se true) par runtime-built strings auto-intern nahi. `StringBuilder` internal mutable buffer, `new StringBuilder(capacity)` se **pre-size** karo.

### .NET 6+ LINQ Additions

```csharp
Product cheapest = products.MinBy(p => p.Price);        // .NET 6 — element, not key
foreach (var batch in products.Chunk(2)) { }            // .NET 6 — fixed batches
var onePer = products.DistinctBy(p => p.Category);      // .NET 6 — de-dup by key
var sorted = new[]{3,1,2}.Order();                      // .NET 7 — OrderBy(x=>x) shorthand
products.Select(p => p.Price).OrderDescending();        // .NET 7
```
| Method | Replaces |
|---|---|
| `MinBy`/`MaxBy` | `OrderBy(key).First()` (poori sort avoid) |
| `Chunk` | Hand-rolled batching |
| `DistinctBy` | `GroupBy(key).Select(g=>g.First())` |
| `Order`/`OrderDescending` | `OrderBy(x=>x)` |

> `MinBy`/`MaxBy` **element** return karte hain (not key); tie par iteration order ka first.

### LINQ Gotchas

- **Multiple enumeration**: same non-materialized query par `.Count()` phir `.First()` = query 2 baar execute (IQueryable = 2 DB round-trips). Fix: `.ToList()` ek baar.
- **Deferred + captured loop variables**: C# 5+ `foreach` ka har iteration apna variable; classic `for` loop index abhi bhi single shared variable (loop-local copy karo).
- **First/Single semantics**: `First()` empty par throws; `FirstOrDefault()` default; `Single()` zero ya >1 par throws (uniqueness assert); `SingleOrDefault()` sirf >1 par throws.
- **`.ToList()` IQueryable par too early** = poori table memory mein + client-side filter → slow endpoint.
- **Custom equality** (`Distinct`/`GroupBy`/`Except`) ko `Equals`/`GetHashCode` override ya `IEqualityComparer<T>` chahiye — warna reference equality bug.

---

## Part VII — Memory Management & Garbage Collection

### Stack vs Heap

| | Stack | Heap |
|---|---|---|
| Stores | Value types, references, call frames | Objects, reference-type data |
| Mgmt | Auto, scope-based | GC |
| Speed | Bahut fast | Slower (GC overhead) |
| Lifetime | Method/block | Jab tak reference |
| Fragmentation | Nahi | Ho sakta (GC compact) |

### Garbage Collection

Mechanics: **Mark** (roots se reachable identify) → **Sweep** (unreachable remove) → **Compact** (optional, fragmentation kam).

**Generational**: Gen 0 (naye, frequent collect: method-locals) → Gen 1 (medium) → Gen 2 (long-lived: caches, statics; kam collect).

- **Triggers**: memory pressure, allocation threshold, `GC.Collect()` (discouraged — out-of-schedule full collection).
- **LOH**: >85 KB objects, Gen 2 ke saath collect, auto-compact nahi (historically); `GCSettings.LargeObjectHeapCompactionMode` se request.
- Modes: **Workstation** (default, single-threaded), **Server** (multi-threaded, ASP.NET Core default), **Concurrent/Background** (Gen 2 background thread par, app freeze nahi).

> `GC.Collect()` avoid — GC heuristics defeat, throughput hurt. Sirf known one-off large burst ke baad.

### GC Diagnostics Tooling for Production

Tools: `dotnet tool install -g`; running process ko **PID se attach**, no restart/code change.

| Tool | Kya | Kab |
|---|---|---|
| `dotnet-counters` | Live counters (heap/gen sizes, alloc rate, ThreadPool queue, GC pause) | **First** — cheap, batata hai kya + roughly kaisi problem |
| `dotnet-gcdump` | Heap snapshot (object graph, counts, retention) bina lambe pause | **Second** — kaunse objects accumulate + kaun root kar raha |
| `dotnet-trace` | CPU/runtime event trace (allocation call stacks) | Jab **code mein kahan** allocations chahiye |

**Walkthrough:** `dotnet-counters ps` → `dotnet-counters monitor --process-id <pid> System.Runtime` (Gen 2 heap jo full GC ke baad kabhi shrink na ho = leak signature vs high-but-stable churn) → do `dotnet-gcdump` snapshots minutes apart → diff (jis type ka count disproportionately grow hua, retention path dekho) → agar zaroorat ho `dotnet-trace` allocation call stacks ke liye.

> Sequencing = seniority signal: cheapest/safest (counters) → most invasive (trace).

### Dispose() vs Finalize()

- `Dispose()` (`IDisposable`) — **explicit, deterministic** cleanup of unmanaged resources.
- `Finalize()` (`~ClassName()`) — GC-called, **non-deterministic**, slower, last-resort safety net.

> **GC = memory. Dispose() = resources.** GC unmanaged ko promptly release nahi karta.

```csharp
using var fs = new FileStream("test.txt", FileMode.Open); // Dispose guaranteed even on exception
```

Full Dispose pattern:
```csharp
public void Dispose() { Dispose(true); GC.SuppressFinalize(this); }
protected virtual void Dispose(bool disposing) {
    if (!disposed) {
        if (disposing) { /* managed resources */ }
        // unmanaged resources unconditionally
        disposed = true;
    }
}
~ResourceHolder() { Dispose(false); }
```

| | Dispose() | Finalize() |
|---|---|---|
| In | IDisposable | object (destructor) |
| Called by | Developer/using | GC |
| Determinism | Deterministic | Non-deterministic |
| Reusability | Idempotent (safe multiple) | GC once |

> **`HttpClient` reuse/DI-managed (via `IHttpClientFactory`)**, per request dispose nahi — warna socket exhaustion. Best practices: `using` prefer, sirf owned dispose karo, injected deps mat dispose karo, Dispose lightweight, Dispose se throw mat karo.

### Weak References

`WeakReference<T>` — GC collect kar sakta hai par app retrieve bhi (agar alive); object ko rooted nahi rakhta.
```csharp
if (weakRef.TryGetTarget(out var target)) { /* still alive */ }
```
Uses: large reclaimable caches, event-subscriber leak avoid; `ConditionalWeakTable<K,V>` = lifetime extend kiye bina extra data attach.

### Memory Leaks in .NET

GC hai isliye "leaks" = **unintentional rooting**. Sources (frequency order):
1. Un-unsubscribed event handlers.
2. Long-lived delegates mein captured closures (`this` capture).
3. Eviction ke bina static caches (`static Dictionary` jo grow karti rahe).
4. DI captive dependencies.
5. `HttpClient` misuse.

### IAsyncDisposable

`Dispose()` sync hai — par kuch cleanup async (stream flush, DB async close). C# 8 → `IAsyncDisposable` + `await using`:
```csharp
public async ValueTask DisposeAsync() {
    await _stream.FlushAsync();
    await _stream.DisposeAsync();
}
await using var resource = new AsyncResource();
```
Type dono implement kar sakta hai; sync `Dispose()` fallback bhi resources release kare. Modern `DbContext`/`SqlConnection`/`Stream` `IAsyncDisposable` implement karte hain.

---

## Part VIII — Advanced: Multithreading & Async

### Thread vs Task (TPL)

| | Thread | Task |
|---|---|---|
| Level | Low (OS-managed) | High (.NET-managed) |
| Execution | Dedicated OS thread | ThreadPool thread |
| Cost | Expensive | Optimized (pooled) |
| Return | Kuch nahi | `Task<T>` |
| Exceptions | Manual | Built-in (`AggregateException`) |
| Best | Long-running background | Short-lived, I/O-bound |

### Task Lifecycle & Exception Handling

States: `Created → WaitingToRun → Running → WaitingForChildren → RanToCompletion/Faulted/Canceled`.

- Task exception capture hota hai, await/`.Wait()`/`.Result` par (re)throw. Uncaught crash nahi karta (jab tak observe na ho). Multiple = `AggregateException.InnerExceptions`.

**Cancellation** (cooperative):
```csharp
using var cts = new CancellationTokenSource(TimeSpan.FromSeconds(5));
try { await DoWorkAsync(cts.Token); }
catch (OperationCanceledException) { /* expected */ }

async Task DoWorkAsync(CancellationToken token) {
    token.ThrowIfCancellationRequested();
    await Task.Delay(100, token);
}
```
`CreateLinkedTokenSource(a, b)` — koi bhi source cancel ho to cancel.

### Thread Safety Primitives

```csharp
private readonly object _lock = new();
lock (_lock) { /* critical section */ }
```
`lock` **sirf sync** — `await` ke across hold nahi (compiler forbids). Async mutual exclusion → `SemaphoreSlim`.

### async/await Fundamentals

`async` = method asynchronous mark + `await` enable. `await` = calling thread block kiye bina asynchronously suspend jab tak op complete.
```csharp
public async Task<string> GetDataAsync() {
    using var client = new HttpClient();
    return await client.GetStringAsync("https://example.com");
}
```

### Async State-Machine Internals

Compiler har `async` method ko `IAsyncStateMachine`-implementing class/struct mein rewrite karta hai; body → jump-table `MoveNext()`.

Mechanics:
1. Async method call = poora body run nahi — synchronously pehle incomplete `await` tak. `AsyncTaskMethodBuilder<T>` `Task<T>` immediately return karta hai (yahi object jispar `SetResult`/`SetException`).
2. `awaiter.IsCompleted` synchronously check (fast path) — already done to suspend nahi, `GetResult()` inline. "await" context switch guarantee nahi.
3. Incomplete → awaiter par continuation register (`OnCompleted`/`UnsafeOnCompleted`), control caller ko return — **yahi thread free hota hai**.
4. Resume = koi doosra infra code `MoveNext()` dobara call karta hai; `_state` batata hai kahan jump.
5. Exceptions `MoveNext()` se propagate nahi hoti — catch hoke `_builder.SetException(ex)` par store → isi liye async Task exception await par surface, aur `async void` dangerous.
6. Awaiter contract: `GetAwaiter()` → `IsCompleted`, `GetResult()`, `OnCompleted`/`UnsafeOnCompleted` (duck-typed, no interface req) → Task/ValueTask/custom awaitable.

> **State machine struct ya class?** Default `struct` (sync-complete common case mein heap alloc avoid), sirf pehli genuine suspension par heap par box. Ye compiler ke allocation-avoidance tricks mein se ek.

### Asynchrony vs Multithreading

| | Async | Multithreading |
|---|---|---|
| Model | Single thread, block avoid | Multiple explicit threads |
| Best | I/O-bound | CPU-bound |
| Resources | Efficient | Costlier |
| Parallel? | Zaroori nahi (non-blocking) | Truly parallel |

I/O par async; CPU-bound par multithreading/Parallel/PLINQ. Combine: `await Task.Run(() => CpuWork())` (ASP.NET Core caveat neeche).

### Deadlocks & Race Conditions

```csharp
// Lock-ordering deadlock: Thread A lock(obj1)→lock(obj2), Thread B lock(obj2)→lock(obj1)
lock (obj1) { lock (obj2) { } }
Parallel.For(0, 1000, _ => count++); // race — non-atomic, lost updates
```

### Task Parallel Library (TPL)

TPL (`System.Threading.Tasks`) = raw threads ke upar higher-level abstraction; `Task`s ThreadPool par.
```csharp
Task.Run(() => ...); Task.Wait();
Parallel.For(1, 5, i => ...);
var results = await Task.WhenAll(t1, t2);
```
**TPL vs async/await**: TPL = APIs (`Task`/`Parallel`/`TaskFactory`), low-level control. async/await = TPL ke upar syntactic sugar. Analogy: TPL = engine, async/await = automatic transmission.

| Method | Purpose |
|---|---|
| `Task.Run()` | Background thread, value return |
| `Task.Wait()`/`.Result` | **Block** — ASP.NET/UI mein avoid |
| `Task.WhenAll()` | Sab finish tak concurrently await |
| `Task.WhenAny()` | Pehla complete par |
| `Task.Delay()` | Non-blocking delay |
| `Task.FromResult()` / `Task.CompletedTask` | Completed task |
| `ContinueWith()` | Continuation (ab mostly `await`) |
| `Task.Factory.StartNew()` | Older — `Task.Run` drop-in **nahi** (nested Task unwrap nahi, `.Unwrap()` chahiye) |

### Task.Run vs StartNew(LongRunning) vs Parallel.ForEachAsync

`Task.Run` = 95%+ default (sensible defaults, auto-unwrap).

| | `Task.Run` | `StartNew(..., LongRunning)` |
|---|---|---|
| Thread | ThreadPool (shared) | Dedicated (pool bypass) |
| Unwrap | Automatic | Manual `.Unwrap()` |
| When | Bounded CPU-bound work | Unbounded blocking loop (dedicated polling/consumer) |
| Overuse | Safe | Bahut sare = pooling defeat, OS thread exhaust |

**`Parallel.ForEachAsync` (.NET 6+)** = bounded async concurrency ka modern built-in (manual `SemaphoreSlim` replace):
```csharp
await Parallel.ForEachAsync(urls,
    new ParallelOptions { MaxDegreeOfParallelism = 8, CancellationToken = ct },
    async (url, token) => await DownloadAsync(url, token));
```
Throttling + cancellation + exception aggregation handle karta hai. "Downstream concurrent calls kaise limit?" ka go-to.

### "Main Thread" in ASP.NET Core

**ASP.NET Core mein no dedicated UI/request thread.** Startup thread `Program.cs` chalata hai; `app.Run()` ke baad Kestrel listen karta hai, har request **ThreadPool thread** se.
```csharp
public async Task<Product> Get(int id) { await Task.Delay(5000); return new Product(); } // thread release, sync Sleep blocks
```
`await` par thread pool mein wapas; complete par **possibly different thread** resume — **no thread affinity** (WinForms ke unlike). 100 concurrent requests ≠ 100 threads. Kestrel = IOCP (Windows)/epoll (Linux).

> **Nuance:** controller ke andar CPU-work ko `Task.Run()` mein wrap karna ASP.NET Core mein **mild anti-pattern** — sirf pool thread A→B move + overhead; yahan free karne ke liye UI thread hai hi nahi. True async I/O prefer, ya background worker/queue.

### Async Streams (IAsyncEnumerable<T>)

```csharp
async IAsyncEnumerable<int> Generate() {
    for (int i = 1; i <= 5; i++) { await Task.Delay(1000); yield return i; }
}
await foreach (var n in Generate()) Console.WriteLine(n);
```
Data async process jaise aata hai, bina poori collection materialize — large result sets stream/paging.

### ConfigureAwait(false) and SynchronizationContext

`SynchronizationContext` = await ke baad continuation "kahan" resume. UI frameworks (WPF/WinForms/old ASP.NET) mein captured context (wapas UI thread). **ASP.NET Core mein no SynchronizationContext** → any ThreadPool thread → "no thread affinity".

`ConfigureAwait(false)` = captured context par resume mat karo.
```csharp
var r = await _httpClient.GetAsync(url).ConfigureAwait(false);
```
Guidance (2026):
- **Library code**: har await par `ConfigureAwait(false)` good practice (context-marshal cost + deadlock avoid).
- **ASP.NET Core app code**: largely unnecessary (no context). Sirf shared library ke liye rakho jo context-sensitive host mein bhi chal sakti hai.

### The Classic Sync-Over-Async Deadlock

**#1 senior async question.**
```csharp
void Button_Click(...) { var result = GetDataAsync().Result; } // BLOCKS UI thread
async Task<string> GetDataAsync() { await Task.Delay(1000); return "done"; } // continuation UI thread chahiye
```
Kyun: (1) UI thread `.Result` par block; (2) continuation captured SynchronizationContext (UI thread) par schedule; (3) UI thread blocked → continuation kabhi run nahi; (4) deadlock.

**ASP.NET Core mein nahi hota** (no context) par load ke under **thread-pool starvation** ho sakta hai.

Fixes (order): (1) **await all the way up**, sync se async par `.Result`/`.Wait()` mat karo; (2) poori chain `ConfigureAwait(false)`; (3) `Task.Run(() => AsyncMethod()).Result` (context sidestep, still blocks).

### async void — Why Dangerous

```csharp
async void ProcessOrder() { await Task.Delay(100); throw new Exception("boom"); } // DANGER
```
- Exception caller catch nahi kar sakta — no Task to observe → SynchronizationContext par throw → **process crash**.
- Await nahi ho sakta — sequencing/testing/error handling harder.
- Legitimate: **top-level event handlers** (framework `void` signature) — par internally `async Task` ko delegate + try/catch.
- **Rule: hamesha `async Task`** even "nothing returns" ke liye.
- Tests: `async void` test silently pass hota hai — xUnit/NUnit `async Task` hone chahiye.

### Task vs ValueTask

`Task<T>` = heap-allocated reference type. `ValueTask<T>` = struct — synchronously-available result (no alloc) ya wraps `Task<T>`.
```csharp
public ValueTask<int> GetCachedOrComputeAsync(int key) {
    if (_cache.TryGetValue(key, out var v)) return new ValueTask<int>(v); // sync, zero alloc
    return new ValueTask<int>(ComputeAsync(key));                        // async
}
```
| | Task<T> | ValueTask<T> |
|---|---|---|
| Type | Reference (heap) | Struct |
| Best | General async | Hot path, frequently sync (cache hits) |
| Multiple awaits? | Haan | **Nahi** — ek baar, immediately await |
| Store later? | Haan | Nahi (cache mat karo) |

> "Sab jagah ValueTask?" **Nahi** — default `Task<T>`, `ValueTask` sirf proven hot path (caching/buffered reader) par. Restrictions misuse-prone; benefit rarely matters outside hot code.

---

## Part IX — Data Access: ADO.NET

**ADO.NET** = low-level data-access framework. Full SQL control, EF Core se faster/lighter (no tracking/LINQ-translation). High-perf apps, fine-grained microservices, legacy.

- **Connected model**: `App → Connection → Command → DataReader → DB` (connection open).
- **Disconnected model**: `App → DataAdapter → DataSet/DataTable → DB` (memory mein, connection close).
- Providers: `SqlConnection`, `SqlCommand`, `SqlDataReader`, `SqlDataAdapter`, `SqlTransaction`. Modern: **`Microsoft.Data.SqlClient`** (`System.Data.SqlClient` legacy/deprecated).
- **Connection pooling** default on — `Close()`/`Dispose()` reuse karta hai. "Open late, close early" via `using`; leaked connections = pool exhaust.

| Method | Returns | Use |
|---|---|---|
| `ExecuteReader()` | SqlDataReader (forward-only) | Stream large datasets — fastest |
| `ExecuteNonQuery()` | Row count | INSERT/UPDATE/DELETE |
| `ExecuteScalar()` | Single value | Aggregates/existence |

**Parameterized queries** — SQL injection prevent + plan reuse:
```csharp
cmd.Parameters.Add("@Id", SqlDbType.Int).Value = id;   // correct
```
Explicit `SqlDbType` > `.AddWithValue()` (infer se plan-cache bloat + implicit-conversion index scans).

**Transactions** — `BeginTransaction`/`Commit`/`Rollback`, ACID. Scope small rakho.

| Isolation | Dirty | Non-Repeatable | Phantom | Notes |
|---|---|---|---|---|
| Read Uncommitted | Y | Y | Y | Fastest, least safe |
| Read Committed (default) | N | Y | Y | Common |
| Repeatable Read | N | N | Y | Read locks longer |
| Serializable | N | N | N | Slowest, fully isolated |
| Snapshot | N | N | N | Row-versioning, no blocking reads |

- **Async** (`OpenAsync`, `ExecuteReaderAsync`) — **throughput** improve (threads free), single-query latency nahi.

| | ADO.NET | Dapper | EF Core |
|---|---|---|---|
| Abstraction | Raw SQL + manual mapping | Micro-ORM (auto mapping) | Full ORM (LINQ, tracking, migrations) |
| Perf | Fastest | Very fast | Slow (improving) |
| Productivity | Lowest | Medium | Highest |
| Best | Hot paths | High-perf + convenience | CRUD business apps |

> EF Core default; hot-path par Dapper/ADO.NET — sirf jab profiling dikhaye. Pre-optimize mat karo.

**Pitfalls:** pooling na samajhna, concatenated SQL, open connections chhodna, readers dispose na karna, async ignore (starvation), DataSet overuse, isolation levels discuss na karna.

---

## Part X — Design Principles & Patterns

### SOLID

| | Purpose |
|---|---|
| SRP | Ek class, ek responsibility |
| OCP | Extension open, modification closed |
| LSP | Subtypes base ke liye substitutable |
| ISP | Unused methods force mat karo |
| DIP | Abstractions par depend, concretions par nahi |

- SRP: salary calc + report → split. OCP: `if/else` payment chain → `IPayment`. LSP: `Square:Rectangle` contract break → dono `Shape`. ISP: fat `Worker` → `IWorkable`/`IEatable`. DIP: `Computer` injected `IKeyboard`/`IMonitor`.

### Dependency Injection

```csharp
public Client(IService service) { _service = service; } // constructor injection
```
`Microsoft.Extensions.DependencyInjection`: (1) registration service map (interface→impl); (2) resolution (recursively deps resolve, ctor injection); (3) lifetimes; (4) mechanism = **runtime reflection** (compiler rewrite nahi).

| Lifetime | Behavior |
|---|---|
| Transient | Har request naya instance |
| Scoped | Per HTTP request/scope ek |
| Singleton | Poori app lifetime ek |

### Serialization & Deserialization

```csharp
string json = JsonSerializer.Serialize(obj);
Person p = JsonSerializer.Deserialize<Person>(json);
```
`System.Text.Json` = modern default. **Untrusted data ke liye `BinaryFormatter`/`[Serializable]` USE NA KARO** — RCE vulns, .NET mein by default disabled. Perf/AOT ke liye `System.Text.Json` source generators (reflection eliminate). `Newtonsoft.Json` older codebases mein.

### AutoMapper

```csharp
var config = new MapperConfiguration(cfg => cfg.CreateMap<Person, PersonDTO>());
```
**Debate:** convenience par reflection cost, mapping bugs runtime par (compile time nahi), complex config = hard-to-debug DSL. Senior teams **explicit manual mapping** (extension/static factory) ya **source-generated mappers** (Mapperly) par shift. Answer: "AutoMapper simple/low-stakes ke liye fine, business-critical/perf-sensitive ke liye explicit/source-generated — compile-time safety + debuggability wins."

### Architectural Patterns

- **MVC** (web), **MVVM** (WPF/Xamarin/Blazor), **Command** (undo/redo), **CQRS** (read/write separate).
- **Repository + Unit of Work** — `Repository<T>` per aggregate; UoW multiple ops atomic. EF Core `DbContext` already UoW hai → EF ke upar generic repository aksar redundant.
- **Mediator** (MediatR) — senders/handlers decouple, CQRS ke saath.
- **Clean/Onion Architecture** — concentric layers (center Domain → Application → Infra/Presentation), dependencies inward.

### Microservices

Small, independently deployable. .NET: ASP.NET Core + Docker + Kubernetes + API Gateway.

### The Captive Dependency Problem

**Singleton** jo constructor-injected **Scoped/Transient** capture kar le:
```csharp
public class CacheService { private readonly AppDbContext _db; ... } // Singleton captures Scoped!
```
`DbContext` de facto singleton ban jaata hai → thread-safety violations (DbContext thread-safe nahi) + stale state. Built-in container `ValidateScopes=true` (Development default) se detect + throw karta hai. **Fix:** `IServiceScopeFactory` inject karo, zaroorat par naya scope:
```csharp
using var scope = scopeFactory.CreateScope();
var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
```

### Keyed DI Services (.NET 8+)

Same interface ke multiple impls, key se distinguish:
```csharp
builder.Services.AddKeyedScoped<INotificationService, EmailService>("email");
builder.Services.AddKeyedScoped<INotificationService, SmsService>("sms");
public class OrderService([FromKeyedServices("email")] INotificationService notifier) { }
```
Pehle factory delegate / manual `Dictionary<string,IService>` ki jagah.

---

## Part XI — Cross-Cutting Concerns: Logging & Exceptions

`ILogger<T>` (`Microsoft.Extensions.Logging`) — structured logging, class-based category. Providers: Console/Debug/EventLog/App Insights; third-party: Serilog, NLog.

**Log levels** (kam→zyada): `Trace, Debug, Information, Warning, Error, Critical`.

**Structured logging** (concatenation se better — searchable/queryable):
```csharp
_logger.LogInformation("User {UserId} with name {UserName} logged in.", userId, user);
```

**Serilog:**
```csharp
Log.Logger = new LoggerConfiguration().WriteTo.Console()
    .WriteTo.File("logs/log.txt", rollingInterval: RollingInterval.Day).CreateLogger();
builder.Host.UseSerilog();
```

**Exception logging** — hamesha exception object pass (full stack trace):
```csharp
catch (Exception ex) { _logger.LogError(ex, "An error occurred."); }
```

**Architecture:** unhandled ke liye **global exception-handling middleware** (clean controllers, consistent responses, centralized logging). Local `try/catch` sirf jab recover/fallback/context-add/cleanup.

**`IExceptionHandler` (.NET 8)** — typed alternative to `UseExceptionHandler` lambda:
```csharp
public async ValueTask<bool> TryHandleAsync(HttpContext ctx, Exception ex, CancellationToken ct) {
    if (ex is not ValidationException ve) return false; // next handler
    ctx.Response.StatusCode = StatusCodes.Status400BadRequest;
    await ctx.Response.WriteAsJsonAsync(new ProblemDetails { Title = "Validation failed", Detail = ve.Message }, ct);
    return true;
}
// AddExceptionHandler<ValidationExceptionHandler>(); app.UseExceptionHandler();
```
`ProblemDetails` (RFC 7807) ke saath pair karo.

---

## Part XII — Modern C# Language Features (C# 9–14)

**Collection Expressions (C# 12):**
```csharp
int[] numbers = [1, 2, 3, 4, 5];
int[] combined = [..numbers, 6, 7];   // spread
```

**`field` keyword (C# 14)** — backing field bina auto-property accessor mein logic:
```csharp
public string Name { get => field; set => field = value?.Trim() ?? throw new ArgumentNullException(nameof(value)); }
```

**Extension Members (C# 14)** — extension properties/static/operators:
```csharp
public static class StringExtensions {
    extension(string s) { public bool IsPalindrome => s.SequenceEqual(s.Reverse()); }
}
"level".IsPalindrome; // True
```

Aur:
- **File-scoped namespaces (C# 10)** — `namespace MyApp;` (indentation kam).
- **Global usings (C# 10)** — `global using System;` project-wide.
- **Top-level statements (C# 9) + minimal hosting** — no explicit `Main`; `WebApplication.CreateBuilder(args)`.
  ```csharp
  var builder = WebApplication.CreateBuilder(args);
  builder.Services.AddControllers();
  var app = builder.Build(); app.MapControllers(); app.Run();
  ```
- **Raw string literals (C# 11)** — `"""..."""` (JSON/regex bina escaping).
- **Native AOT** — native code AOT compile (Part XV).

---

## Part XIII — Minimal APIs, EF Core & Caching

### Minimal APIs vs Controllers

```csharp
app.MapGet("/products/{id}", async (int id, IProductService svc) => await svc.GetAsync(id))
   .Produces<Product>(200).Produces(404);
```
| | Minimal APIs | MVC Controllers |
|---|---|---|
| Boilerplate | Bahut low (lambdas) | Zyada (classes/attributes) |
| Best | Microservices, simple/high-throughput | Large APIs, complex binding, filters/versioning |
| Startup perf | Faster | Slower |
| Native AOT | First-class | Weaker (improving) |

Grouping + filters:
```csharp
var products = app.MapGroup("/products").RequireAuthorization();
app.MapPost("/products", CreateProduct).AddEndpointFilter<ValidationFilter<ProductDto>>();
```

### EF Core

**N+1 problem** — common question:
```csharp
// BAD — N+1
var orders = context.Orders.ToList();
foreach (var o in orders) Console.WriteLine(o.Customer.Name); // lazy-loads per row
// GOOD — eager load
var orders = context.Orders.Include(o => o.Customer).ToList();
// GOOD — projection
var s = context.Orders.Select(o => new { o.Id, CustomerName = o.Customer.Name }).ToList();
```
Detect via SQL logging; multiple `Include` collections ke liye `AsSplitQuery()` (cartesian explosion avoid).

**Tracking vs no-tracking:**
```csharp
var p = context.Products.First(x => x.Id == 1);        // tracked — SaveChanges se pehle chahiye
var list = context.Products.AsNoTracking().ToList();   // faster read-only
```
Rule: modify+save na karna ho to `AsNoTracking()`.

**Migrations:** `dotnet ef migrations add X` / `dotnet ef database update`. Small/reversible, applied migration edit mat karo (naya add), generated SQL review, CI ke liye `--idempotent`.

### Caching

| Type | Scope | Example |
|---|---|---|
| `IMemoryCache` | In-process | Per-node computed value |
| `IDistributedCache` | Shared (Redis/SQL) | Session state, shared lookup |
| `HybridCache` (.NET 9+) | L1 in-memory + L2 distributed | Speed + cross-instance consistency |
| Output Caching | Full HTTP responses | Public GET endpoints |

**HybridCache** — L1+L2 unify + **cache stampede** solve (concurrent misses par sirf ek recompute):
```csharp
builder.Services.AddHybridCache();
await cache.GetOrCreateAsync($"product:{id}", async token => await repo.GetProductAsync(id),
    new HybridCacheEntryOptions { Expiration = TimeSpan.FromMinutes(10) });
```

**Output caching:** `builder.Services.AddOutputCache(...)`; `app.MapGet(...).CacheOutput("Products");`
**Redis:** `builder.Services.AddStackExchangeRedisCache(o => o.Configuration = ...);`

> **"Design caching for product catalog API"**: cache-aside (check→miss→DB→populate), TTLs vs explicit invalidation, stampede protection (HybridCache/distributed lock), cache-key design (tenants/versions collision avoid).

---

## Part XIV — Resilience, Auth & Security

### Resilience & Rate Limiting

**Rate limiting (.NET 7+):**
```csharp
builder.Services.AddRateLimiter(o => o.AddFixedWindowLimiter("fixed",
    opt => { opt.Window = TimeSpan.FromSeconds(10); opt.PermitLimit = 5; opt.QueueLimit = 2; }));
app.UseRateLimiter();
app.MapGet("/products", GetProducts).RequireRateLimiting("fixed");
```
| Algorithm | Good for |
|---|---|
| Fixed Window | Simple quota |
| Sliding Window | Fairer (bursts smooth) |
| Token Bucket | Sustained cap + short bursts |
| Concurrency Limiter | In-flight cap (protect downstream) |

**Polly** (via `Microsoft.Extensions.Http.Resilience`):
```csharp
builder.Services.AddHttpClient<PaymentClient>().AddResilienceHandler("payment-pipeline", b => {
    b.AddRetry(new RetryStrategyOptions { MaxRetryAttempts = 3, BackoffType = DelayBackoffType.Exponential });
    b.AddCircuitBreaker(new CircuitBreakerStrategyOptions { FailureRatio = 0.5, MinimumThroughput = 10 });
    b.AddTimeout(TimeSpan.FromSeconds(5));
});
```
- **Retry** — re-attempt, exponential backoff + jitter (thundering herd avoid).
- **Circuit Breaker** — threshold ke baad cooldown, fast fail. States: `Closed → Open (failures) → HalfOpen (cooldown) → Closed (trial ok)/Open (trial fail)`.
- **Timeout** — call bound, threads/connections free.
- **Bulkhead** — resource pools isolate.

> "Retry forever kyun nahi?" — down service ke against retry outage bigadta hai; retry + circuit breaker combine.

**Idempotency** — `POST /orders` safe retry: client `Idempotency-Key` header; server key→result persist + duplicates short-circuit.

### Authentication & Authorization

**JWT Bearer** — stateless, horizontally scalable, `Authorization: Bearer` header:
```csharp
builder.Services.AddAuthentication(JwtBearerDefaults.AuthenticationScheme)
    .AddJwtBearer(o => o.TokenValidationParameters = new TokenValidationParameters {
        ValidateIssuer = true, ValidateAudience = true, ValidateLifetime = true, ValidateIssuerSigningKey = true, ...
    });
app.UseAuthentication(); app.UseAuthorization();
```

**OAuth2 vs OIDC:** OAuth2 = *authorization* (kaun kya access); OIDC = uske upar *identity* (user kaun). External IdP delegate (Entra ID, Auth0, Keycloak, Duende).

| Flow | Use |
|---|---|
| Authorization Code + PKCE | SPA/mobile (modern default; Implicit avoid) |
| Client Credentials | Service-to-service |
| Refresh Token | Silent token renew |

**Claims/policy-based** (hardcoded roles se better):
```csharp
options.AddPolicy("CanEditProducts", p => p.RequireClaim("permission", "products.edit"));
[Authorize(Policy = "CanEditProducts")]
```
"User kya kar sakta" ko "kaunsa role" se decouple → better scale.

**Other:** CORS explicit (`AddCors`/`UseCors`); CSRF mainly cookie-auth (JWT header kam exposed); Data Protection API (cookies/tokens encrypt); **secrets `appsettings.json` mein kabhi nahi** — User Secrets (local), Key Vault/env vars (prod).

---

## Part XV — Performance & Low-Allocation Programming

**`Span<T>`/`Memory<T>`** — `Span<T>` = stack-only, allocation-free view over contiguous memory, copy bina slicing:
```csharp
ReadOnlySpan<char> text = "Hello, World!";
ReadOnlySpan<char> hello = text.Slice(0, 5);  // no alloc, view
Span<int> nums = stackalloc int[5];            // stack, zero heap
```
`Memory<T>` = heap-friendly, async boundaries ke across usable (`Span<T>` = `ref struct`, async/field mein nahi).

**`ArrayPool<T>`** — shared pool se rent/return:
```csharp
byte[] buffer = ArrayPool<byte>.Shared.Rent(1024);
try { } finally { ArrayPool<byte>.Shared.Return(buffer); }
```

**`string.Create()`** — directly destination buffer, intermediate allocs avoid.
**`record struct`/`struct`** — small short-lived value objects heap se door (GC pressure kam).

**BenchmarkDotNet** — de-facto micro-benchmarking; "kaise fast karoge" = before/after measure:
```csharp
[MemoryDiagnoser]
public class StringBenchmarks {
    [Benchmark(Baseline = true)] public string Concat() => "a" + "b" + "c";
    [Benchmark] public string SB() => new StringBuilder().Append("a").Append("b").Append("c").ToString();
}
```

**Native AOT** — native code AOT compile, no JIT warm-up, smaller footprint, <50ms startup. Trade-offs: no runtime reflection codegen (source-generator alternatives chahiye), kuch libraries incompatible. Uses: containers, serverless, CLI.

| Technique | Solves |
|---|---|
| `Span<T>`/`Memory<T>` | Buffer copying avoid |
| `ArrayPool<T>` | Repeated alloc/GC churn |
| `ValueTask<T>` | Frequently-sync hot path Task alloc |
| `record struct` | Small value objects off heap |
| Native AOT | JIT warm-up eliminate |
| Source generators | Runtime reflection eliminate |

---

## Part XVI — Advanced Concurrency Primitives

### Monitor, SpinLock, False Sharing, Thread-Pool Starvation

**`lock` → `Monitor`:**
```csharp
lock (_lockObj) { } // ≈
bool taken = false;
try { Monitor.Enter(_lockObj, ref taken); /* ... */ }
finally { if (taken) Monitor.Exit(_lockObj); }
```
`Monitor.Enter`/`Exit` object ke **sync block** (header) se exclusive lock — isi liye lock object reference type. Mistakes: boxed value type, string literal (interned/shared), public `this`. `Monitor.Wait`/`Pulse`/`PulseAll` = low-level condition-variable signaling (producer-consumer ke peeche, modern code higher-level types prefer kare).

**`SpinLock`/`SpinWait`** — `lock`/`Monitor` blocked thread ko sleep karata hai (context switch, ~microseconds). Very short critical section ke liye spinning (busy-wait) sasta ho sakta hai:
```csharp
private SpinLock _spinLock = new SpinLock();
bool taken = false;
try { _spinLock.Enter(ref taken); /* few instructions, not DB call */ }
finally { if (taken) _spinLock.Exit(); }
```
`SpinWait` briefly spin phir yield/sleep. **Rule: `SpinLock`/`SpinWait` = ultra-short measured contention only (low-level runtime code); app code mein `lock`/`Monitor` default correct** — "fancy tool kab NAHI use karna" = seniority signal.

**False sharing / cache-line contention** — CPUs 64-byte lines mein cache. Different threads ke unrelated fields same line par → write invalidation → performance cliff:
```csharp
[StructLayout(LayoutKind.Explicit, Size = 128)]
public struct PaddedCounters {
    [FieldOffset(0)]  public long Counter1;
    [FieldOffset(64)] public long Counter2; // apna cache line
}
```
Fix: `[StructLayout]` padding ya hot counters separate objects. "Lock-free counter multi-core par worse kyun" = false sharing.

**ThreadPool starvation** — pool worker count slowly ~1/500ms badhata hai (slow ramp-up = load latency spikes ka common cause):
- **Symptoms:** requests queue up + latency badhti hai **jabki CPU low** (bottleneck thread availability). `dotnet-counters` ThreadPool Queue Length/Thread Count. Root cause = **pool threads par blocking calls** (sync I/O `.Result`/`.Wait()`, `Task.Run` wrapping blocking, misplaced LongRunning).
- `ThreadPool.SetMinThreads(200, 200)` = minimum raise (band-aid, fix nahi — real fix = blocking calls remove).

> "Low CPU + growing queue → pehle blocking calls dhoondo; `SetMinThreads` symptom mask; fix = properly async."

**`SemaphoreSlim`** — throttle concurrent access, async (`WaitAsync`), N callers:
```csharp
await _semaphore.WaitAsync();
try { await CallApiAsync(); } finally { _semaphore.Release(); }
```

**`ReaderWriterLockSlim`** — many readers OR one writer; read-heavy shared state (in-memory cache).

**`Interlocked` & `volatile`:**
```csharp
Interlocked.Increment(ref _counter);   // atomic, no lock — cheaper than lock for counters/flags
private volatile bool _isRunning;      // per-core caching se updates hide na ho
```

**Concurrent collections:**
| Type | Use |
|---|---|
| `ConcurrentDictionary` | Thread-safe key/value cache |
| `ConcurrentQueue`/`ConcurrentStack` | Thread-safe FIFO/LIFO |
| `BlockingCollection` | Bounded producer-consumer (blocking Add/Take) |

**`System.Threading.Channels`** — modern async producer-consumer (older BlockingCollection replace):
```csharp
var channel = Channel.CreateUnbounded<int>();
await channel.Writer.WriteAsync(42); channel.Writer.Complete();
await foreach (var item in channel.Reader.ReadAllAsync()) { }
```
Use: `BackgroundService` jo API-populated channel se work read kare (request handling ↔ processing decouple).

**Selection:** `lock`/Monitor (sync mutual exclusion) · SemaphoreSlim (async, N entrants throttle) · ReaderWriterLockSlim (read-heavy) · Interlocked (lock-free atomics) · Channels (async pipelines).

---

## Part XVII — Microservices, Messaging & CQRS

**REST vs gRPC:**
| | REST/OpenAPI | gRPC |
|---|---|---|
| Transport | HTTP/1.1, JSON | HTTP/2, binary Protobuf |
| Perf | Achha | Faster (smaller payloads, multiplexed) |
| Contract | OpenAPI (loose) | `.proto` (strict, codegen) |
| Streaming | Limited | First-class bidirectional |
| Browser | Native | grpc-web/proxy |
| Best | Public APIs, browsers | Internal service-to-service, low latency |

**Message brokers:**
| Broker | Model | Use |
|---|---|---|
| RabbitMQ | Message queue (AMQP) | Task queues, routing |
| Kafka | Distributed log/event streaming | High-throughput, event sourcing, replay |
| Azure Service Bus | Managed queue/topic | Enterprise .NET, dead-lettering, sessions |

**Queue** (once consumed, removed) vs **topic/pub-sub** (har subscriber ko deliver). Decoupling: `OrderPlaced` → Inventory + Shipping independently react.

**CQRS with MediatR:**
```csharp
public record CreateOrderCommand(int ProductId, int Quantity) : IRequest<int>;
public class CreateOrderHandler : IRequestHandler<CreateOrderCommand, int> {
    public async Task<int> Handle(CreateOrderCommand req, CancellationToken ct) { return newOrderId; }
}
// await mediator.Send(new CreateOrderCommand(...));
```
Benefits: thin controllers, isolated testable handlers, `IPipelineBehavior<>` cross-cutting (validation/logging/transactions).

**Saga pattern** — microservices ACID transaction share nahi kar sakte → local transactions sequence + compensating actions (reserve→charge→confirm; payment fail → release inventory).
| Style | Description |
|---|---|
| Orchestration | Central coordinator explicitly calls + compensations |
| Choreography | Services events par react, no coordinator, harder trace |

**DDD vocabulary:** Entity (identity persist), Value Object (values-defined, no identity — `record` fit), Aggregate (cluster + Aggregate Root = consistency boundary), Bounded Context (model valid boundary, ~1:1 microservice), Domain Event (`OrderPlaced`).

---

## Part XVIII — Observability, Testing & Full-Stack Integration

### Observability & Health Checks

Three pillars: logs + **distributed tracing** + metrics.

**OpenTelemetry** — vendor-neutral standard:
```csharp
builder.Services.AddOpenTelemetry()
    .WithTracing(t => t.AddAspNetCoreInstrumentation().AddHttpClientInstrumentation().AddSource("MyApp").AddOtlpExporter())
    .WithMetrics(m => m.AddAspNetCoreInstrumentation().AddRuntimeInstrumentation());
```
Trace/correlation ID service boundaries ke across propagate — production latency/failures debug essential.

**Health checks:**
```csharp
builder.Services.AddHealthChecks().AddSqlServer(cs).AddCheck<RedisHealthCheck>("redis");
app.MapHealthChecks("/health");
```
K8s/LB poll — readiness (traffic milna chahiye?) + liveness (restart?).

### Testing

| Layer | Tooling | Covers |
|---|---|---|
| Unit | xUnit/NUnit + Moq/NSubstitute | Business logic isolation, mocked deps |
| Integration | `WebApplicationFactory<T>`, Testcontainers | API + real/containerized DB/cache |
| E2E/UI | Playwright, Selenium | Full user flows |

```csharp
var repoMock = new Mock<IProductRepository>();
repoMock.Setup(r => r.GetAsync(1)).ReturnsAsync(new Product { Id = 1, Stock = 0 });
await Assert.ThrowsAsync<InsufficientStockException>(() => service.CreateOrderAsync(1, 1));
```
**Testcontainers** — Docker mein real disposable DB/Redis per test run (real fidelity, no shared stateful env).

**Philosophy:** architectural boundaries (repositories, HTTP clients) par mock, internal details par nahi (brittle avoid); behavior/outcomes test > exact calls; AAA (Arrange/Act/Assert); **`async void` tests mein bhi dangerous** → `async Task`.

### Full-Stack Integration

**SignalR** — real-time bidirectional (WebSockets, SSE/long-polling fallback):
```csharp
public class NotificationHub : Hub {
    public async Task SendMessage(string user, string msg) => await Clients.All.SendAsync("ReceiveMessage", user, msg);
}
```

**Blazor hosting:**
| Model | Runs | Notes |
|---|---|---|
| Server | Server, UI via SignalR | Small download, persistent connection |
| WASM | Browser | True client-side C#, offline, larger download |
| Hybrid/MAUI | Native shell + Blazor UI | Shared web + native code |

**BFF (Backend-for-Frontend)** — dedicated backend per frontend: downstream aggregate, auth token exchange, response shape; browser directly internal services se baat nahi karta.

**CORS in practice** — *"React `localhost:3000` `localhost:5001` API call nahi kar sakta"*: same-origin policy cross-origin block; API ko CORS middleware se explicit origins opt in:
```csharp
builder.Services.AddCors(o => o.AddPolicy("SpaPolicy",
    p => p.WithOrigins("https://myapp.com").AllowAnyHeader().AllowAnyMethod().AllowCredentials()));
app.UseCors("SpaPolicy");
```

### Senior-Level Interviews Mein Kya Different

- Kam "define X", zyada scenario reasoning ("design rate limiter/notification service", "debug production memory leak" — actual tools naam lo).
- Production experience + architectural judgment weighted.
- Weakness signal: bina follow-up "it depends". Strong = "it depends" + **decision framework + default recommendation**.
- "Why not X instead?" probing — trade-offs test.
- Rehearse: URL shortener/rate limiter design; "3 sequential downstream calls ka P99 kaise kam" (`Task.WhenAll` parallelize, caching, circuit breaker); "monolith module → microservice no downtime" (strangler-fig, dual-write/CDC sync, feature-flag cutover).

---

## Part XIX — Swagger / OpenAPI & API Documentation

> Framing: ye ASP.NET Core/Web API tooling hai, C# language feature nahi. C#-relevant slice = XML doc-comments (`///`, `<summary>`, `<param>`) + attributes.

**OpenAPI vs Swagger:** OpenAPI (OAS) = REST APIs describe karne ka standard (JSON/YAML) = contract. Swagger = tooling ecosystem (UI, Editor, Codegen) jo OpenAPI *implement* karta hai.

**Swashbuckle:**
```csharp
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();
app.UseSwagger(); app.UseSwaggerUI(); // /swagger
```

**JWT in Swagger UI** — do parts: **security definition** (scheme exist, Authorize button) + **security requirement** (kaunse ops, padlocks). Pitfall: JWT middleware wire karna par OpenAPI security scheme bhoolna → Authorize button appear nahi hota.

**.NET 9+ mein Swashbuckle default se remove:**
| Version | OpenAPI |
|---|---|
| .NET 8 & pehle | Swashbuckle included, OpenAPI 3.0 |
| .NET 9 | Built-in `Microsoft.AspNetCore.OpenApi`, 3.0, no UI, no XML comments |
| .NET 10 | Built-in OpenAPI 3.1, Native AOT friendly |

Reasons: Swashbuckle maintenance gaps + Native AOT (reflection-heavy). Swashbuckle dead nahi, manually add ho sakta.
```csharp
builder.Services.AddOpenApi();  // generation only, no UI
app.MapOpenApi();               // /openapi/v1.json
```
| UI tool | Role |
|---|---|
| Swagger UI | Classic interactive |
| Scalar | Modern (dark mode, multi-lang, `MapScalarApiReference()`) |
| NSwag | Client SDK gen (TS/C#) |
| ReDoc | Clean read-only docs |

**OpenAPI structure:** `openapi`, `info`, `servers`, `paths`, `components` (reusable via `$ref`), `security`, `tags`. 3.1 = JSON Schema 2020-12, webhooks top-level, nullability `type: [string, null]` (vs 3.0 `nullable: true`).

**Versioning:** URL path (`/api/v1/products`), query (`?api-version=1.0`), header, media type. `Asp.Versioning.Mvc` + API explorer per-version Swagger doc. Existing version contract mein breaking changes kabhi nahi — naya version ship.

**Documenting responses:**
```csharp
[ProducesResponseType(typeof(Product), StatusCodes.Status200OK)]
[ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status404NotFound)]
```
`ProblemDetails` (RFC 7807) prefer. `<GenerateDocumentationFile>true</GenerateDocumentationFile>` + XML feed → summary/param UI mein.

**Comparisons:** Swagger UI (live code reflect) vs Postman (drift possible); code-first (fast, intent se lag) vs contract-first (parallel teams/public APIs, contract source of truth); REST/OpenAPI (fixed endpoints, mature) vs GraphQL (client-specified, over/under-fetch kam).

**Secure Swagger in production** — sirf Development mein serve (ya toggle); auth ke peeche (admin policy); network restrict (IP allow-list/VPN/internal ingress); internal APIs raw JSON only (public UI = free recon); examples mein secrets/tokens leak nahi.

**Advanced topics:** API linting (Spectral — style/consistency CI); mock server (Prism/WireMock — frontend build before backend); OpenAPI validation pipelines (lint + diff breaking changes + contract tests); AsyncAPI (event-driven/async APIs — queues/WebSockets/Kafka); schema evolution (additive-only, `deprecated: true` before removal, type kabhi na badlo, consumer-driven contracts like Pact).

---

## Part XX — Terminology Reference

| Term | What | Runs alone? | Example |
|---|---|---|---|
| Library | Reusable code | Nahi | `System.Collections` |
| DLL | Library ka compiled binary | Nahi | `Newtonsoft.Json.dll` |
| EXE | Executable app | Haan | `MyApp.exe` |
| Framework/SDK | Libraries + runtime (SDK: compilers/templates/CLI) | Nahi | .NET 8/10 |
| Package | NuGet format (`.nupkg`) | Nahi | `Newtonsoft.Json` |

**Managed vs unmanaged:** managed = .NET language, MSIL, CLR supervision (memory/type-safety/security). Unmanaged = CLR ke bahar (file handles, DB connections, sockets, native memory) → explicit `Dispose()`.

---

## Best Practices Checklist

- Composition > inheritance; hierarchies shallow.
- Interface se start; abstract class sirf shared state/behavior par.
- Constructors lightweight (no I/O/DB); args early validate; immutability.
- `Equals()` + `GetHashCode()` saath override.
- Read-only EF queries `AsNoTracking()`; N+1 avoid `Include()`.
- Default `Task`; `ValueTask` sirf profiling evidence par.
- Hamesha `async Task`, kabhi `async void` (framework event handlers except).
- Shared library code `ConfigureAwait(false)`; ASP.NET Core largely optional.
- Global exception middleware / `IExceptionHandler` > scattered try/catch.
- Reusable APIs se **events** expose, raw delegates nahi.
- Structured logging (`{Placeholder}`) > concatenation.
- API errors `ProblemDetails` (RFC 7807).
- Secrets source control mein nahi; User Secrets/Key Vault.
- Retry + circuit breaker; indefinite retry nahi.
- Performance change before/after benchmark (BenchmarkDotNet).
- EF migrations small/reversible/reviewed.

## Common Pitfalls Checklist

- Sync-over-async `.Result`/`.Wait()` context-capturing → deadlock.
- `async void` silently exceptions swallow (tests mein bhi).
- Per-request `HttpClient` dispose/recreate (use `IHttpClientFactory`).
- Singleton capturing Scoped/Transient (captive dependency).
- Multiple enumeration of `IQueryable`/lazy `IEnumerable`.
- `.ToList()` IQueryable too early → client-side eval.
- String-concatenated SQL / `AddWithValue` overuse — injection + plan bloat.
- Un-unsubscribed event handlers → leak.
- `dynamic`/reflection overuse jahan source generator/static typing kaam kare.
- Production mein Swagger UI restrict kiye bina expose.
- `GC.Collect()` routine tool ki tarah.

---

## Sample Interview Q&A (Rapid Fire)

**Q: Record vs class?** A: Records value-based equality + `ToString()` free + `with` non-destructive mutation; classes reference equality + default mutable. DTOs/value objects → records; small allocation-sensitive → `record struct`.

**Q: `.Result` kabhi deadlock, kabhi nahi?** A: Deadlock jab calling thread `.Result` par block + continuation captured `SynchronizationContext` (usi thread) par resume chahiye (WPF/WinForms/old ASP.NET). ASP.NET Core mein no context → specific deadlock nahi, par load par ThreadPool starve.

**Q: Task vs ValueTask kab?** A: `ValueTask` sirf proven hot path jahan results frequently sync (cache-hit) + profiling evidence. Warna `Task` default; ValueTask single-await restriction misuse-prone.

**Q: Equals() ke saath GetHashCode() kyun?** A: `Dictionary`/`HashSet` pehle hash se bucket, phir `Equals()` disambiguate. Equal objects ke different hash → lookups silently fail.

**Q: N+1 kya, fix?** A: Parent fetch phir har row par lazy additional query. Fix: eager `Include()`, projection `Select()`, multiple Includes `AsSplitQuery()`.

**Q: AutoMapper controversial kyun?** A: Compile-time safety + debuggability convenience ke liye trade — mapping bugs runtime par, complex config hard-to-maintain DSL. Senior teams explicit/source-generated mapper prefer.

**Q: Captive dependency?** A: Longer-lived service (Singleton) constructor mein shorter-lived (Scoped/Transient) capture → intended lifetime se zyada hold → `DbContext` thread-safety bugs. Fix: `IServiceScopeFactory` se fresh scope.

**Q: Production Swagger secure?** A: Development tak restrict ya auth/network (IP allow-list/VPN) ke peeche; internal APIs raw JSON only (UI = free recon); real tokens/hostnames leak nahi.

**Q: Async vs multithreading?** A: Async/await = I/O par thread block na karna, inherently parallelism nahi, I/O-bound ideal. Multithreading = true parallelism multiple threads par, CPU-bound ideal, synchronization cost.

**Q: Production memory leak debug?** A: Cheap/non-invasive se: `dotnet-counters monitor` live PID (Gen 2 heap GC cycles ke across upward = leak signature vs high churn). Climb → do `dotnet-gcdump` snapshots minutes apart, diff (disproportionate grow + root: un-unsubscribed handler / unbounded static cache / captive DbContext / closure). Sirf phir `dotnet-trace` allocation call stacks ke liye.

**Q: .NET 9 mein Swagger change?** A: Swashbuckle default template se drop, built-in `Microsoft.AspNetCore.OpenApi` (generation only, no UI) — maintenance gaps + Native AOT incompatibility. UI ab separately choose (Swagger UI/Scalar/ReDoc/NSwag).
