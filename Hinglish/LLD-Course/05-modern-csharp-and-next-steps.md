# Lesson 5 — Modern C#, Tech-Lead Lens aur Aage Kya Seekhein

Ab tak aap saare classic ("Gang of Four") design patterns dekh chuke ho. Yeh final lesson teen cheezon ke saath sab kuch tie together karta hai jo ek **senior engineer / tech lead** ko specifically chahiye:

1. Kaunse patterns modern C# aapke liye *khud* kar deta hai (taaki aapko unhe reinvent na karna pade)
2. Team mein patterns ko actually kaise *apply aur govern* karein (code reviews, design discussions)
3. Aage kya seekhna hai uske liye ek concrete roadmap

---

## Part A — Patterns jo modern C# aapko free mein deta hai

Senior dikhne ka ek bahut bada part yeh jaanna hota hai ki kab pattern ko hand-write **nahi** karna chahiye, kyunki language ya framework already usse idiomatically express kar deta hai. Jab built-in already exist karta ho aur aap textbook pattern ke peeche jao, toh reviews mein yeh red flag hota hai.

| Classic pattern | Modern C#/.NET mein iske jagah aap kya likhte ho |
|---|---|
| **Singleton** | `services.AddSingleton<IThing, Thing>();` — DI single lifetime own karta hai |
| **Factory / Abstract Factory** | DI container, `Func<T>` delegates, ya **keyed services** (.NET 8+) |
| **Strategy** | Ek `Func<>` delegate, ya right strategy resolve karne ke liye keyed DI |
| **Observer** | `event` / `Action`, `IObservable<T>` (Rx), `INotifyPropertyChanged`, channels |
| **Command + Mediator** | **MediatR** library (requests + handlers) |
| **Chain of Responsibility** | ASP.NET Core middleware; `HttpClient` `DelegatingHandler`s |
| **Decorator** | **Scrutor** (`.Decorate<T>()`) ke through DI decoration; middleware |
| **Iterator** | `IEnumerable<T>` + `yield return`; LINQ |
| **Prototype / Memento** | `record` types + `with` expressions |
| **Visitor** | Sealed hierarchy ke upar `switch` expressions + pattern matching |
| **Builder** | Object initializers + `required` members; `with` expressions |

> 🔑 **Rule:** agar framework already usko clearly kehta hai, toh framework use karo. Manual pattern ke peeche tab jao jab woh genuinely clarity add kare ya woh capability de jo built-in mein missing hai.

### Upar mentioned modern C# features ka quick tour

```csharp
// records — immutable data + built-in copy (Prototype/Memento for free)
public record Person(string Name, int Age);
var a = new Person("Ada", 36);
var b = a with { Age = 37 };        // copy-and-change; 'a' is untouched

// required members — safe construction without a Builder
public class Config { public required string ApiKey { get; init; } }
var c = new Config { ApiKey = "abc" };   // compiler forces you to set ApiKey

// pattern matching — replaces many Visitor/Strategy switch scenarios
string Describe(object o) => o switch
{
    int n when n < 0 => "negative number",
    int          => "number",
    string s     => $"text of length {s.Length}",
    null         => "nothing",
    _            => "something else"
};

// Dependency Injection — the container that quietly does Factory + Singleton for you
var services = new ServiceCollection();
services.AddSingleton<IClock, SystemClock>();       // one shared instance (Singleton)
services.AddScoped<IOrderRepo, SqlOrderRepo>();     // one per web request
services.AddTransient<IEmailSender, SmtpSender>();  // a fresh one each time
```

---

## Part B — Tech-Lead lens: team mein patterns apply karna

Patterns jaanna ek junior skill hai. **Unke use ko govern karna** senior/tech-lead skill hai. Yahan hai ki day to day mein iska actually matlab kya hota hai.

### 1. Pattern names ke bajaye *forces* ke baare mein baat karo
Review mein yeh line badly land hoti hai: *"Yahan Strategy use karo."*
Yeh well land hoti hai: *"Har naya payment type isi `switch` ko edit karta hai, jisse existing waale break hone ka risk hai. Kya hum har type ko apni khud ki class bana sakte hain taaki naye types purane code ko touch na karein?"*
Aapne bas Open/Closed principle describe kiya aur unhe Strategy tak *bina jargon ke* le gaye. Problem ko naam do; pattern khud follow karega.

### 2. Over-engineering se ladho (yahan yeh tumhara #1 job hai)
Eager developers mein sabse common mistake yeh dekhne ko milti hai ki woh patterns ko *preemptively* apply karte hain. PR mein har abstraction ke liye yeh poocho:
> *"Yeh konsa concrete, likely change cheaper banata hai — aur kya woh change actually aa raha hai?"*
Agar jawab hai "koi real change nahi, just in case," toh yeh **YAGNI** violation hai. Ek plain method ya simple `if` aksar correct, senior choice hota hai. **Best code woh simplest code hai jo real problem solve karta hai.**

### 3. "Same shape, different intent" pairs ko clear rakho
Yeh reviews aur interviews mein constantly aate hain:

| Pair | Inhe alag kaise pehchane |
|---|---|
| **Strategy vs State** | Strategy: algorithm *aap* pick karte ho. State: object khud apna behavior switch karta hai aur apna next state jaanta hai. |
| **Adapter vs Bridge** | Adapter: already exist kar rahi cheezon ke beech mismatch fix karta hai. Bridge: do dimensions ko separate karne ke liye pehle se design kiya jaata hai. |
| **Decorator vs Proxy** | Decorator: behavior *add* karta hai. Proxy: access *control* karta hai. |
| **Factory Method vs Abstract Factory** | Factory Method: ek product. Abstract Factory: products ki matching *family*. |
| **Command vs Strategy** | Command: ek object ke roop mein *action* (undo/queue ke saath). Strategy: ek object ke roop mein *algorithm*. |

### 4. Pattern-shaped tech debt ke liye dhyan rakho
Reviews mein flag karne wali specific cheezein:
- **Leaked Observer subscriptions** (`+=` se subscribe kiya, kabhi `-=` nahi kiya → memory leak).
- **God-object Facades / Mediators** jinhone sirf coordinate karne ke bajaye business logic accumulate kar li ho.
- **Hand-rolled Singletons** jo actually DI-managed hone chahiye.
- **Decorator/middleware ordering bugs** (galat wrapper outside hona).
- **Speculative Abstract Factories / single implementation waale interfaces** jo "for flexibility" add kiye gaye hon jinki kisi ko zarurat nahi.

### 5. LLD ko architecture se connect karo
Yeh chhote patterns wo bricks hain jinse bade architectures bante hain:
- **Clean / Hexagonal architecture** = Dependency Inversion (Lesson 1) ko system scale par apply karna, boundaries par Adapters use karke.
- **DDD tactical patterns** (Repository, Aggregate, Value Object) inhi ke upar build hote hain.
- **CQRS** application layer par Command + Mediator hai.
LLD mein aapki fluency hi hai jo aapke high-level design decisions ko credible banati hai.

---

## Part C — Gang of Four se aage: patterns jo ek tech lead ko bhi jaanne chahiye

23 classic patterns hi poori story nahi hain. Yeh "enterprise" patterns real C# codebases mein constantly dikhte hain:

| Pattern | Yeh kya karta hai | Kis baat ka dhyan rakhein |
|---|---|---|
| **Repository** | Data access ko ek interface (`IOrderRepository`) ke peeche abstract karta hai | Bina wajah Entity Framework Core ko thin repository mein wrap mat karo — EF already ek repository/unit-of-work hai. Isse sirf real benefit ke liye add karo. |
| **Unit of Work** | Multiple changes ko ek commit/transaction mein group karta hai | EF Core ka `DbContext` already ek hai. |
| **Options pattern** | `IOptions<T>` ke through strongly-typed configuration | Settings read karne ka standard .NET way. |
| **Result / Either** | Exceptions throw karne ke bajaye success-or-error ko ek value ke roop mein return karna | Expected failures (validation) ke liye great; truly exceptional cases ke liye exceptions rakho. |
| **Specification** | Ek business rule / query filter ko reusable, combinable object ke roop mein encapsulate karna | Complex, reused query logic ke liye handy. |
| **Null Object** | `null` ke bajaye interface ka ek "do-nothing" implementation | Null-checks hata deta hai (e.g., ek `NullLogger` jo messages discard kar deta hai). |
| **CQRS** | *Read* model ko *write* model se separate karna | Powerful hai lekin complexity add karta hai — simple CRUD par apply mat karo. |

---

## Part D — Aapka roadmap (order mein)

Aapne poora course padh liya hai. Yahan hai ki reading ko skill mein kaise convert karein.

### Step 1 — Foundations ko cement karo (is week)
**Lesson 1** ko dobara padho jab tak interfaces, composition, aur Dependency Injection natural na feel hone lagein. Baaki sab kuch inhi par rest karta hai. Agar sirf ek cheez yaad rakhni ho, toh yeh: *"interfaces par depend karo, jo chahiye woh inject karo."*

### Step 2 — "Core four" ko haath se banao (next 1–2 weeks)
Ek scratch console project mein, inme se har ek ko ek baar, memory se, copy-paste kiye bina implement karo:
1. **Strategy** (payment ya shipping calculator)
2. **Observer** (C# `event` use karke)
3. **Decorator** (ek caching + logging wrapper)
4. **Factory Method** (ek simple factory class)

Yeh chaar ~80% real-world use cover karte hain. Inhe khud banana, unke baare mein padhne se 10× zyada worth hai.

### Step 3 — Inhe wild mein recognize karne ki practice karo (ongoing)
Koi bhi .NET codebase kholo (ASP.NET Core source bhi), aur woh patterns spot karo jo ab aap jaante ho: middleware (Chain of Responsibility), `HttpClient` handlers (Decorator/Chain), MediatR (Command/Mediator), DI registration (Factory/Singleton).

### Step 4 — Tech-lead muscle ki practice karo (ongoing)
Har code review mein, yeh do senior questions poocho:
- *"Agar hum isse **nahi** change karte, toh yeh kaunsa SOLID principle violate karega?"* (pattern add karne ko justify karta hai)
- *"Kya yeh abstraction apna keep earn kar rahi hai, ya yeh YAGNI hai?"* (ek hatane ko justify karta hai)

### Step 5 — Architecture tak level up karo (next few months)
Jab LLD comfortable ho jaye, tab **Clean Architecture**, **Domain-Driven Design (DDD)**, aur **CQRS** study karo. Yeh saare same principles hain, bas larger scale par.

---

## 📚 Master resource list

**Video**
- 🎥 **Christopher Okhravi – Design Patterns playlist** — sabse clear conceptual explanations, har pattern ke liye ek video: [search](https://www.youtube.com/results?search_query=christopher+okhravi+design+patterns+playlist)
- 🎥 **Nick Chapsas** — modern, C#-specific, aur honest hai ki kab patterns obsolete ho jaate hain: [channel](https://www.youtube.com/@nickchapsas)
- 🎥 **Amichai Mantinband** — real .NET mein patterns + clean architecture: [channel](https://www.youtube.com/@amantinband)
- 🎥 **Derek Banas – Design Patterns** — fast overview series: [search](https://www.youtube.com/results?search_query=derek+banas+design+patterns)

**Written**
- 📖 **Refactoring Guru** — har pattern ke liye C# examples, beautifully illustrated: https://refactoring.guru/design-patterns/csharp
- 📖 **DoFactory – .NET Design Patterns** — C# reference: https://www.dofactory.com/net/design-patterns
- 📖 *Head First Design Patterns* — concepts absorb karne ke liye sabse friendly book (Java examples hain, lekin ideas directly transfer hote hain).
- 📖 *Dependency Injection Principles, Practices, and Patterns* (Seemann & van Deursen) — yeh book "DI replaces half the patterns" waala mindset click kar deti hai.

> **Video links par ek note:** yeh specific video IDs ke bajaye YouTube *search* links aur channel links hain, kyunki individual video URLs time ke saath change ho jaate hain jabki search/channel links valid rehte hain. Named creator ka top result pick karo.

---

## 🎓 Aapne kar diya

Aapne shuru kiya tha jab aapko yeh bhi nahi pata tha ki interface kya hota hai. Ab aap saare 23 classic design patterns samajhte ho, modern C# unhe kaise reshape karta hai, aur — sabse important — yeh judgment bhi ki **kab unhe use nahi karna hai**. Yehi last part hai jo ek senior engineer ko us insaan se alag karta hai jisne bas ek list memorize kar li ho.

Jao, "core four" ko haath se banao. Yeh hai aapka next move. 🚀
