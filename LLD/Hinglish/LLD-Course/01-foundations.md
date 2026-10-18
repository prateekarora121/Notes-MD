# Lesson 1 — Foundations (OOP + SOLID)

> Yeh lesson sabse important hai. **Baad ke har pattern basically SOLID hi hai, jo kisi specific situation pe apply kiya gaya hai.** Agar yeh lesson samajh gaye, to patterns obvious lagenge. Agar skip kar diya, to yeh magic spells jaise lagenge.

---

## Part A — Ek halka sa OOP refresher

OOP = **Object-Oriented Programming**. Yeh code ko "objects" (things) ke around organize karne ka tareeka hai, bas instructions ki list banane ke instead.

### Classes aur objects

Ek **class** ek blueprint hoti hai. Ek **object** ek real cheez hai jo us blueprint se banti hai.

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

Isko dhyan se padho:
- `class Dog { }` blueprint hai.
- `new Dog()` memory mein ek actual dog banata hai.
- `rex` ek *variable* hai jo us dog ko point karta hai.
- `rex.Bark()` us specific dog ko batata hai ki apna `Bark` action kare.

### Interfaces — is course ka sabse important idea

Ek **interface** ek *contract* hai. Yeh methods ki ek list hai jo ek class **promise** karti hai provide karegi — lekin interface mein khud **koi code nahi hota**, bas promises hote hain.

Ek **job description** ki tarah socho. "Ek Driver ko yeh karna aana chahiye: `Drive()`, `Park()`, `Refuel()`." Job description khud kuch *karta* nahi hai — yeh bas batata hai ki kisi bhi driver mein kya capability honi chahiye. Ek specific person (Alice, Bob) us description ko *fulfill* karta hai.

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

Ab asli magic dekho. Kyunki dono classes `INotifier` ko fulfill karti hain, jo code sirf "kisi cheez ka `Send` kar sakna" care karta hai, wo *dono mein se koi bhi* accept kar sakta hai, bina jaane ya care kiye ki kaun hai:

```csharp
INotifier notifier = new EmailNotifier(); // could be SmsNotifier — the code below doesn't change
notifier.Send("Your order shipped!");
```

> 🔑 Yahi hai **"program to an interface, not an implementation."** Variable ka *type* contract hota hai (`INotifier`), na ki specific class (`EmailNotifier`). Yehi ek habit almost har pattern ke peeche hoti hai.

### Inheritance vs Composition

**Inheritance** = "*is-a*". Ek class doosri ko extend karti hai aur uski abilities inherit karti hai.

```csharp
public class Animal { public void Breathe() => Console.WriteLine("..."); }
public class Dog : Animal { }   // Dog IS-A Animal, so a Dog can Breathe() too
```

**Composition** = "*has-a*". Ek class doosre objects ko *contain* karti hai aur unko use karti hai.

```csharp
public class Engine { public void Start() => Console.WriteLine("Vroom"); }

public class Car
{
    private readonly Engine _engine = new Engine(); // Car HAS-A Engine
    public void StartCar() => _engine.Start();      // Car uses the engine it holds
}
```

**Hum composition kyun prefer karte hain:** Inheritance rigid family trees banata hai. Agar tree shuru mein hi galat ban jaaye, to baad mein usko change karna painful hota hai. Composition tumhe parts ko LEGO jaise snap karke jodne deta hai aur freely rearrange karne deta hai. Yeh hum repeatedly dekhenge.

### Dependency Injection (DI) — yeh tumhe har jagah dikhega

Ek **dependency** basically "kisi class ko jo cheez chahiye" hoti hai. **Dependency Injection** ka matlab hai: class khud apni zarurat ki cheez *banaye*, uske instead tum use bahar se *hand in* kar dete ho (usually constructor ke through).

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

Yeh kyun matter karta hai:
- **Flexible:** `OrderService` ko touch kiye bina Email ↔ SMS swap kar sakte ho.
- **Testable:** test mein, ek *fake* notifier inject karo jo bas calls record kare.
- **Honest:** constructor ab *tumhe batata hai* ki `OrderService` ko kya chahiye. Koi hidden surprises nahi.

> Constructor ek class ki shopping list hoti hai. DI ka matlab hai ki uske liye shopping already ho gayi hai.

---

## Part B — SOLID: acchi class design ke 5 rules

SOLID paanch guidelines hain jo code ko easily changeable rakhte hain. Har letter ek rule hai. Hum har ek ko *bad → good* example ke saath karenge.

### S — Single Responsibility Principle (SRP)
**"Ek class ka sirf ek kaam hona chahiye, isliye usse change karne ka sirf ek hi reason ho."**

Analogy: Ek Swiss Army knife jo *saath mein* tumhara phone *bhi* hai aur tumhara wallet *bhi* hai, sunne mein convenient lagta hai — jab tak ek part kharab nahi hota aur tum sab kuch kho dete ho. Separate tools maintain karna easier hota hai.

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
**"Code *extension* ke liye open hona chahiye lekin *modification* ke liye closed."**
Matlab: naya behavior add karne ke liye tumhe *naya code* add karna chahiye, existing, working code ko edit nahi karna chahiye.

Iska tell-tale smell hota hai ek `switch`/`if` chain jo har naye case ke saath badhta jaata hai:

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

> Dhyan do: OCP *literally* wo reason hai jiski wajah se Strategy pattern exist karta hai. Patterns basically SOLID hi hain, naam ke saath.

### L — Liskov Substitution Principle (LSP)
**"Agar code ek base type expect karta hai, to koi bhi subtype wahan bina kisi nasty surprise ke kaam karna chahiye."**

Analogy: Ek job description mein likha hai "Driver." Agar tum kisi aise insaan ko hire karte ho jo description fit karta hai lekin actually *drive nahi kar sakta* aur car crash kar deta hai, to usne contract violate kar diya. Subtypes ko apne base type ke promises honor karne chahiye.

```csharp
// ❌ BAD: Penguin claims to be a Bird that can Fly, but can't. Code using Bird.Fly() breaks.
public class Bird { public virtual void Fly() { } }
public class Penguin : Bird
{
    public override void Fly() => throw new NotSupportedException("Penguins can't fly!");
}
// Any method that takes a Bird and calls Fly() will crash when handed a Penguin. Broken substitution.
```
Fix: `Fly()` ko sabhi birds pe force mat karo. Isko honestly model karo (jaise, ek separate `IFlyingBird` contract).

### I — Interface Segregation Principle (ISP)
**"Kisi class ko wo methods implement karne pe force mat karo jo usko chahiye hi nahi. Ek giant interface ke bajaye multiple small interfaces prefer karo."**

Analogy: Ek "restaurant staff" job description jisme *cook, serve, wash dishes, AND accounting* sab required ho, silly hai. Isko split karo: `ICook`, `IServer`, `IAccountant`. Log wahi roles lete hain jo woh actually karte hain.

```csharp
// ❌ BAD: one fat interface. A SimplePrinter is forced to implement Scan/Fax it can't do.
public interface IMachine { void Print(); void Scan(); void Fax(); }

// ✅ GOOD: small, focused contracts. Implement only what applies.
public interface IPrinter { void Print(); }
public interface IScanner { void Scan(); }
public class SimplePrinter : IPrinter { public void Print() { } } // no forced empty Scan/Fax
```

### D — Dependency Inversion Principle (DIP)
**"Abstractions (interfaces) pe depend karo, concrete classes pe nahi."**
Yeh "program to an interface" habit aur Dependency Injection ka formal naam hai.

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

> **DIP + interface habit + DI teeno ek hi core idea ke different views hain:** *important code ko specific, replaceable details se glued hone se bachao.*

---

## Part C — Teen "keep it sane" rules

Patterns problems solve karte hain — lekin sabse badi beginner mistake hai unko use karna jab koi problem hi *nahi* hai. Yeh teen rules tumhe honest rakhte hain:

- **KISS — "Keep It Simple."** Jo simplest cheez kaam kare, usually wahi right hoti hai. Ek plain method most of the time ek fancy pattern se better hota hai.
- **YAGNI — "You Aren't Gonna Need It."** Flexibility "just in case" add mat karo. Jab real need aaye tab add karo. Speculative patterns over-engineered code ki #1 wajah hote hain.
- **DRY — "Don't Repeat Yourself."** Logic copy-paste karne se avoid karo — lekin shared code sirf tab extract karo jab tumhe *actually* repeat hota dikhe (rule of thumb: 3rd time). Bahut early extract karna *wrong* shared abstraction bana deta hai, jo thodi duplication se bhi worse hota hai.

---

## ✅ Ab tumhe kya pata hai (aur yeh kyun matter karta hai)

- **Interfaces** tumhe implementations freely swap karne dete hain → flexible design ka heart.
- **Composition + DI** tumhe behavior LEGO jaise assemble karne dete hain aur easily test karne dete hain.
- **SOLID** tumhe batata hai ki *kab* tumhara design rigid ya tangled ho raha hai.
- **KISS / YAGNI / DRY** tumhe patterns over-apply karne se rokte hain.

Next lessons ka har pattern inhi ideas mein se ek hai, jo ek specific recurring situation ke liye packaged hai. Jab tum koi pattern padho, to yeh puchte raho: *"Yeh kaunsa SOLID rule protect kar raha hai?"* Tumhe almost hamesha answer mil jaayega.

➡️ Next: **`02-creational-patterns.md`** — patterns is baare mein ki *objects kaise create hote hain*.
