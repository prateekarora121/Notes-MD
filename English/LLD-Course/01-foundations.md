# Lesson 1 — Foundations (OOP + SOLID)

> This is the most important lesson. **Every pattern later is just SOLID applied to a specific situation.** If you understand this lesson, patterns will feel obvious. If you skip it, they'll feel like magic spells.

---

## Part A — A gentle OOP refresher

OOP = **Object-Oriented Programming**. It's a way of organizing code around "objects" (things) instead of just lists of instructions.

### Classes and objects

A **class** is a blueprint. An **object** is a real thing built from that blueprint.

```csharp
// The blueprint
public class Dog
{
    public string Name { get; set; }          // a property (data the dog has)
    public void Bark() => Console.WriteLine($"{Name} says Woof!"); // a method (action)
}

// Building real dogs from the blueprint
Dog rex = new Dog();   // "new" builds one object (an "instance")
rex.Name = "Rex";
rex.Bark();            // prints: Rex says Woof!
```

Read that slowly:
- `class Dog { }` is the blueprint.
- `new Dog()` builds one actual dog in memory.
- `rex` is a *variable* that points to that dog.
- `rex.Bark()` tells that specific dog to do its `Bark` action.

### Interfaces — the single most important idea in this course

An **interface** is a *contract*. It's a list of methods a class **promises** to provide — but the interface itself contains **no code**, just the promises.

Think of a **job description**. "A Driver must be able to: `Drive()`, `Park()`, `Refuel()`." The job description doesn't *do* anything — it just says what any driver must be capable of. A specific person (Alice, Bob) *fulfills* that description.

```csharp
// The contract (job description). Note: no method bodies, just promises.
public interface INotifier
{
    void Send(string message);
}

// Two different classes that FULFILL (implement) the same contract
public class EmailNotifier : INotifier
{
    public void Send(string message) => Console.WriteLine($"📧 Email: {message}");
}

public class SmsNotifier : INotifier
{
    public void Send(string message) => Console.WriteLine($"📱 SMS: {message}");
}
```

Now the magic. Because both classes fulfill `INotifier`, code that only cares about "something that can `Send`" can accept *either one* without knowing or caring which:

```csharp
INotifier notifier = new EmailNotifier(); // could be SmsNotifier — the code below doesn't change
notifier.Send("Your order shipped!");
```

> 🔑 This is **"program to an interface, not an implementation."** The variable's *type* is the contract (`INotifier`), not the specific class (`EmailNotifier`). This one habit is behind almost every pattern.

### Inheritance vs Composition

**Inheritance** = "*is-a*". A class extends another and inherits its abilities.

```csharp
public class Animal { public void Breathe() => Console.WriteLine("..."); }
public class Dog : Animal { }   // Dog IS-A Animal, so a Dog can Breathe() too
```

**Composition** = "*has-a*". A class *contains* other objects and uses them.

```csharp
public class Engine { public void Start() => Console.WriteLine("Vroom"); }

public class Car
{
    private readonly Engine _engine = new Engine(); // Car HAS-A Engine
    public void StartCar() => _engine.Start();      // Car uses the engine it holds
}
```

**Why we prefer composition:** Inheritance creates rigid family trees. If you get the tree wrong early, changing it later is painful. Composition lets you snap parts together like LEGO and rearrange freely. We'll see this repeatedly.

### Dependency Injection (DI) — you'll see this everywhere

A **dependency** is just "something a class needs." **Dependency Injection** means: instead of a class *building* what it needs itself, you *hand it in* from outside (usually via the constructor).

```csharp
// ❌ WITHOUT DI: OrderService builds its own notifier. It's now STUCK with email forever.
public class OrderService
{
    private readonly EmailNotifier _notifier = new EmailNotifier(); // hard-wired!
    public void Place() => _notifier.Send("Order placed");
}

// ✅ WITH DI: we hand the notifier in. OrderService no longer cares which kind it is.
public class OrderService
{
    private readonly INotifier _notifier;
    public OrderService(INotifier notifier)   // <-- "injected" through the constructor
    {
        _notifier = notifier;
    }
    public void Place() => _notifier.Send("Order placed");
}

// The caller decides what to inject:
var service = new OrderService(new SmsNotifier()); // easy to switch to SMS, or a fake for testing
```

Why this matters:
- **Flexible:** swap Email ↔ SMS without touching `OrderService`.
- **Testable:** in a test, inject a *fake* notifier that just records calls.
- **Honest:** the constructor now *tells you* what `OrderService` needs. No hidden surprises.

> The constructor is a class's shopping list. DI means the shopping is done for it.

---

## Part B — SOLID: the 5 rules of good class design

SOLID is five guidelines that keep code easy to change. Each letter is one rule. We'll do each with a *bad → good* example.

### S — Single Responsibility Principle (SRP)
**"A class should do one job, so it has only one reason to change."**

Analogy: A Swiss Army knife that's *also* your phone *also* your wallet sounds convenient — until one part breaks and you lose everything. Separate tools are easier to maintain.

```csharp
// ❌ BAD: this class does THREE jobs — order logic, saving to a database, and emailing.
public class Order
{
    public void CalculateTotal() { /* business logic */ }
    public void SaveToDatabase() { /* SQL code */ }
    public void SendConfirmationEmail() { /* SMTP code */ }
}
// Problem: change your email provider → you edit the Order class. Change your database →
// you edit the Order class again. Too many unrelated reasons to touch one file.
```

```csharp
// ✅ GOOD: one job each.
public class Order          { public decimal CalculateTotal() => 0m; }
public class OrderRepository{ public void Save(Order o) { /* SQL */ } }
public class EmailService   { public void SendConfirmation(Order o) { /* SMTP */ } }
// Now each class changes for exactly one reason.
```

### O — Open/Closed Principle (OCP)
**"Code should be open to *extension* but closed to *modification*."**
Meaning: you should be able to add new behavior by adding *new code*, not by editing existing, working code.

The tell-tale smell is a `switch`/`if` chain that grows every time there's a new case:

```csharp
// ❌ BAD: every new shipping type forces you to EDIT this method (risking existing cases).
public decimal GetCost(string type, decimal weight)
{
    if (type == "standard") return weight * 1.5m;
    if (type == "express")  return weight * 3.0m;
    // next month: add "overnight" here... and again... and again...
    return 0;
}
```

```csharp
// ✅ GOOD: define a contract; each shipping type is its own class. Adding a new one
// means adding a NEW class, not editing existing ones. (This is the Strategy pattern!)
public interface IShipping { decimal Cost(decimal weight); }
public class Standard : IShipping { public decimal Cost(decimal w) => w * 1.5m; }
public class Express  : IShipping { public decimal Cost(decimal w) => w * 3.0m; }
// Overnight? Just add: public class Overnight : IShipping { ... }  — nothing else changes.
```

> Notice: OCP is *literally* the reason the Strategy pattern exists. Patterns are just SOLID with names.

### L — Liskov Substitution Principle (LSP)
**"If code expects a base type, any subtype must work there without nasty surprises."**

Analogy: A job description says "Driver." If you hire someone who fits the description but actually *can't drive* and crashes the car, they violated the contract. Subtypes must honor the promises of their base type.

```csharp
// ❌ BAD: Penguin claims to be a Bird that can Fly, but can't. Code using Bird.Fly() breaks.
public class Bird { public virtual void Fly() { } }
public class Penguin : Bird
{
    public override void Fly() => throw new NotSupportedException("Penguins can't fly!");
}
// Any method that takes a Bird and calls Fly() will crash when handed a Penguin. Broken substitution.
```
Fix: don't force `Fly()` onto all birds. Model it honestly (e.g., a separate `IFlyingBird` contract).

### I — Interface Segregation Principle (ISP)
**"Don't force a class to implement methods it doesn't need. Prefer many small interfaces over one giant one."**

Analogy: A "restaurant staff" job description requiring *cook, serve, wash dishes, AND do accounting* is silly. Split it: `ICook`, `IServer`, `IAccountant`. People take only the roles they actually do.

```csharp
// ❌ BAD: one fat interface. A SimplePrinter is forced to implement Scan/Fax it can't do.
public interface IMachine { void Print(); void Scan(); void Fax(); }

// ✅ GOOD: small, focused contracts. Implement only what applies.
public interface IPrinter { void Print(); }
public interface IScanner { void Scan(); }
public class SimplePrinter : IPrinter { public void Print() { } } // no forced empty Scan/Fax
```

### D — Dependency Inversion Principle (DIP)
**"Depend on abstractions (interfaces), not on concrete classes."**
This is the formal name for the "program to an interface" habit and for Dependency Injection.

```csharp
// ❌ BAD: high-level business logic depends directly on a specific low-level detail.
public class ReportGenerator
{
    private readonly SqlDatabase _db = new SqlDatabase(); // welded to SQL Server forever
}

// ✅ GOOD: depend on an abstraction, inject the concrete one.
public interface IDataSource { string[] GetRows(); }
public class ReportGenerator
{
    private readonly IDataSource _source;
    public ReportGenerator(IDataSource source) => _source = source; // could be SQL, a file, a mock...
}
```

> **DIP + the interface habit + DI are three views of the same core idea:** *keep the important code from being glued to specific, replaceable details.*

---

## Part C — The three "keep it sane" rules

Patterns solve problems — but the biggest beginner mistake is using them when there's *no* problem. These three rules keep you honest:

- **KISS — "Keep It Simple."** The simplest thing that works is usually right. A plain method beats a fancy pattern most of the time.
- **YAGNI — "You Aren't Gonna Need It."** Don't add flexibility "just in case." Add it when a real need appears. Speculative patterns are the #1 cause of over-engineered code.
- **DRY — "Don't Repeat Yourself."** Avoid copy-pasting logic — but only extract shared code once you *actually* see it repeated (rule of thumb: the 3rd time). Extracting too early creates the *wrong* shared abstraction, which is worse than a little duplication.

---

## ✅ What you now know (and why it matters)

- **Interfaces** let you swap implementations freely → the heart of flexible design.
- **Composition + DI** let you assemble behavior like LEGO and test easily.
- **SOLID** tells you *when* your design is getting rigid or tangled.
- **KISS / YAGNI / DRY** stop you from over-applying patterns.

Every pattern in the next lessons is one of these ideas, packaged for a specific recurring situation. When you read a pattern, keep asking: *"Which SOLID rule is this protecting?"* You'll almost always find the answer.

➡️ Next: **`02-creational-patterns.md`** — patterns about *how objects get created*.
