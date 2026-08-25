# Lesson 5 — Modern C#, the Tech-Lead Lens & What to Learn Next

You've now met all the classic ("Gang of Four") design patterns. This final lesson ties it together with three things a **senior engineer / tech lead** specifically needs:

1. Which patterns modern C# does *for you* (so you don't reinvent them)
2. How to actually *apply and govern* patterns on a team (code reviews, design discussions)
3. A concrete roadmap for what to learn next

---

## Part A — Patterns that modern C# gives you for free

A huge part of looking senior is knowing when **not** to hand-write a pattern because the language or framework already expresses it idiomatically. Reaching for a textbook pattern when a built-in exists is a red flag in reviews.

| Classic pattern | What you write in modern C#/.NET instead |
|---|---|
| **Singleton** | `services.AddSingleton<IThing, Thing>();` — DI owns the single lifetime |
| **Factory / Abstract Factory** | The DI container, `Func<T>` delegates, or **keyed services** (.NET 8+) |
| **Strategy** | A `Func<>` delegate, or keyed DI to resolve the right strategy |
| **Observer** | `event` / `Action`, `IObservable<T>` (Rx), `INotifyPropertyChanged`, channels |
| **Command + Mediator** | The **MediatR** library (requests + handlers) |
| **Chain of Responsibility** | ASP.NET Core middleware; `HttpClient` `DelegatingHandler`s |
| **Decorator** | DI decoration via **Scrutor** (`.Decorate<T>()`); middleware |
| **Iterator** | `IEnumerable<T>` + `yield return`; LINQ |
| **Prototype / Memento** | `record` types + `with` expressions |
| **Visitor** | `switch` expressions + pattern matching over a sealed hierarchy |
| **Builder** | Object initializers + `required` members; `with` expressions |

> 🔑 **The rule:** if the framework already says it clearly, use the framework. Reach for the manual pattern only when it genuinely adds clarity or a capability the built-in lacks.

### A quick tour of the modern C# features referenced above

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

## Part B — The Tech-Lead lens: applying patterns on a team

Knowing patterns is a junior skill. **Governing their use** is the senior/tech-lead skill. Here's what that actually means day to day.

### 1. Talk about *forces*, not pattern names
In a review, this lands poorly: *"Use a Strategy here."*
This lands well: *"Every new payment type edits this `switch`, which risks breaking the existing ones. Can we make each type its own class so new ones don't touch old code?"*
You just described the Open/Closed principle and led them to Strategy *without jargon*. Name the problem; let the pattern follow.

### 2. Fight over-engineering (this is your #1 job here)
The most common mistake you'll see from eager developers is applying patterns *preemptively*. For every abstraction in a PR, ask:
> *"What concrete, likely change does this make cheaper — and is that change actually coming?"*
If the answer is "no real change, just in case," that's a **YAGNI** violation. A plain method or a simple `if` is often the correct, senior choice. **The best code is the simplest code that solves the real problem.**

### 3. Keep the "same shape, different intent" pairs straight
These come up constantly in reviews and interviews:

| Pair | How to tell them apart |
|---|---|
| **Strategy vs State** | Strategy: *you* pick the algorithm. State: the object switches its *own* behavior and knows its next state. |
| **Adapter vs Bridge** | Adapter: fix a mismatch between things that already exist. Bridge: designed up front to separate two dimensions. |
| **Decorator vs Proxy** | Decorator: *add* behavior. Proxy: *control access*. |
| **Factory Method vs Abstract Factory** | Factory Method: one product. Abstract Factory: a matching *family* of products. |
| **Command vs Strategy** | Command: an *action* as an object (with undo/queue). Strategy: an *algorithm* as an object. |

### 4. Watch for pattern-shaped tech debt
Specific things to flag in reviews:
- **Leaked Observer subscriptions** (subscribed with `+=`, never `-=` → memory leak).
- **God-object Facades / Mediators** that accumulated business logic instead of just coordinating.
- **Hand-rolled Singletons** that should be DI-managed.
- **Decorator/middleware ordering bugs** (the wrong wrapper on the outside).
- **Speculative Abstract Factories / interfaces with a single implementation** added "for flexibility" nobody needs.

### 5. Connect LLD up to architecture
These small patterns are the bricks that bigger architectures are built from:
- **Clean / Hexagonal architecture** = Dependency Inversion (Lesson 1) applied at the system scale, using Adapters at the boundaries.
- **DDD tactical patterns** (Repository, Aggregate, Value Object) build on these.
- **CQRS** is Command + Mediator at the application layer.
Your fluency in LLD is what makes your high-level design decisions credible.

---

## Part C — Beyond the Gang of Four: patterns a tech lead should also know

The 23 classic patterns aren't the whole story. These "enterprise" patterns show up constantly in real C# codebases:

| Pattern | What it does | Watch out for |
|---|---|---|
| **Repository** | Abstracts data access behind an interface (`IOrderRepository`) | Don't wrap Entity Framework Core in a thin repository for no reason — EF is already a repository/unit-of-work. Add one only for real benefit. |
| **Unit of Work** | Groups multiple changes into one commit/transaction | EF Core's `DbContext` already is one. |
| **Options pattern** | Strongly-typed configuration via `IOptions<T>` | The standard .NET way to read settings. |
| **Result / Either** | Return success-or-error as a value instead of throwing exceptions | Great for expected failures (validation); keep exceptions for truly exceptional cases. |
| **Specification** | Encapsulate a business rule / query filter as a reusable, combinable object | Handy for complex, reused query logic. |
| **Null Object** | A "do-nothing" implementation of an interface instead of `null` | Removes null-checks (e.g., a `NullLogger` that discards messages). |
| **CQRS** | Separate the *read* model from the *write* model | Powerful but adds complexity — don't apply to simple CRUD. |

---

## Part D — Your roadmap (in order)

You've read the whole course. Here's how to turn reading into skill.

### Step 1 — Cement the foundations (this week)
Re-read **Lesson 1** until interfaces, composition, and Dependency Injection feel natural. Everything else rests on these. If only one thing sticks, make it: *"depend on interfaces, inject what you need."*

### Step 2 — Build the "core four" by hand (next 1–2 weeks)
In a scratch console project, implement each of these once, from memory, no copy-paste:
1. **Strategy** (payment or shipping calculator)
2. **Observer** (using C# `event`)
3. **Decorator** (a caching + logging wrapper)
4. **Factory Method** (a simple factory class)

These four cover ~80% of real-world use. Building them yourself is worth 10× reading about them.

### Step 3 — Practice recognizing them in the wild (ongoing)
Open any .NET codebase (even the ASP.NET Core source) and spot the patterns you now know: middleware (Chain of Responsibility), `HttpClient` handlers (Decorator/Chain), MediatR (Command/Mediator), DI registration (Factory/Singleton).

### Step 4 — Practice the tech-lead muscle (ongoing)
In every code review, ask the two senior questions:
- *"Which SOLID principle would this violate if we DON'T change it?"* (justifies adding a pattern)
- *"Is this abstraction earning its keep, or is it YAGNI?"* (justifies removing one)

### Step 5 — Level up to architecture (next few months)
Once LLD is comfortable, study **Clean Architecture**, **Domain-Driven Design (DDD)**, and **CQRS**. They're the same principles at a larger scale.

---

## 📚 Master resource list

**Video**
- 🎥 **Christopher Okhravi – Design Patterns playlist** — the clearest conceptual explanations, one video per pattern: [search](https://www.youtube.com/results?search_query=christopher+okhravi+design+patterns+playlist)
- 🎥 **Nick Chapsas** — modern, C#-specific, honest about when patterns are obsolete: [channel](https://www.youtube.com/@nickchapsas)
- 🎥 **Amichai Mantinband** — patterns + clean architecture in real .NET: [channel](https://www.youtube.com/@amantinband)
- 🎥 **Derek Banas – Design Patterns** — fast overview series: [search](https://www.youtube.com/results?search_query=derek+banas+design+patterns)

**Written**
- 📖 **Refactoring Guru** — C# examples for every pattern, beautifully illustrated: https://refactoring.guru/design-patterns/csharp
- 📖 **DoFactory – .NET Design Patterns** — C# reference: https://www.dofactory.com/net/design-patterns
- 📖 *Head First Design Patterns* — the friendliest book for absorbing the concepts (Java examples, but the ideas transfer directly).
- 📖 *Dependency Injection Principles, Practices, and Patterns* (Seemann & van Deursen) — the book that makes the "DI replaces half the patterns" mindset click.

> **A note on the video links:** these are YouTube *search* links and channel links rather than specific video IDs, because individual video URLs change over time while search/channel links stay valid. Pick the top result from the named creator.

---

## 🎓 You made it

You started not knowing what an interface was. You now understand all 23 classic design patterns, how modern C# reshapes them, and — most importantly — the judgment to know **when not to use them**. That last part is what separates a senior engineer from someone who just memorized a list.

Go build the "core four" by hand. That's your next move. 🚀
