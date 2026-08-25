# Lesson 4 — Behavioral Patterns

**Behavioral patterns are about *how objects behave and talk to each other* — how responsibilities and actions are shared between them.**

This is the biggest and most practically useful group. If you only truly master a few patterns in your career, make them **Strategy, Observer, and Command** — they show up everywhere in real C#.

We'll cover, roughly easiest first:
1. **Strategy** ⭐ — swap an algorithm at runtime
2. **Observer** ⭐ — notify many objects when something changes
3. **Command** ⭐ — turn an action into an object (enables undo, queues)
4. **Template Method** — fix the steps, let subclasses fill in the blanks
5. **State** — an object changes behavior as its state changes
6. **Chain of Responsibility** — pass a request down a line of handlers
7. **Mediator** — a hub so objects don't talk directly
8. **Iterator** — step through a collection (C# gives this for free)
9. **Visitor** — add new operations to a structure without changing it
10. **Memento** — save & restore an object's state (undo)
11. **Interpreter** — (rare — just be aware it exists)

---

## 4.1 — Strategy ⭐ (learn this one first)

### 🎯 The one-line idea
Put each way of doing something into its own class, all sharing one interface, and **swap which one you use at runtime.**

### 🌍 Real-world analogy
**Getting to the airport.** The goal is the same (get there), but you can pick a *strategy*: drive, take a taxi, or ride the train. You choose based on your situation (budget, time). You can switch strategies without changing the goal.

### 😤 The problem
You calculate shipping cost with a growing pile of `if`s:

```csharp
public decimal Calculate(Order order, string method)
{
    if (method == "standard") return order.Weight * 1.5m;
    if (method == "express")  return order.Weight * 3.0m + 10;
    if (method == "drone")    return order.Weight * 5.0m + 25;  // every new method edits this
    return 0;
}
```
This violates Open/Closed (Lesson 1): each new shipping method forces edits to working code, and this `if` blob tends to get copy-pasted around.

### ✅ The solution
One interface, one class per algorithm, chosen at runtime.

```csharp
// 1. The contract shared by every algorithm
public interface IShippingStrategy { decimal Calculate(Order order); }

// 2. Each algorithm is its own small, testable class
public class StandardShipping : IShippingStrategy
{ public decimal Calculate(Order o) => o.Weight * 1.5m; }

public class ExpressShipping : IShippingStrategy
{ public decimal Calculate(Order o) => o.Weight * 3.0m + 10; }

// 3. The context receives whichever strategy it should use
public class ShippingCalculator
{
    private readonly IShippingStrategy _strategy;
    public ShippingCalculator(IShippingStrategy strategy) => _strategy = strategy; // injected!
    public decimal GetCost(Order order) => _strategy.Calculate(order);
}
```

Usage — pick the strategy and hand it in:

```csharp
var calc = new ShippingCalculator(new ExpressShipping());
decimal cost = calc.GetCost(myOrder);
// Adding "drone shipping" = add ONE new class. No existing code changes.
```

### 🔍 Walkthrough
- Each algorithm lives in its own class → easy to read, easy to unit-test in isolation.
- `ShippingCalculator` doesn't know *which* strategy it has — it only knows the `IShippingStrategy` contract. That's the Dependency Inversion habit from Lesson 1.
- Notice this is the *exact same idea* as the Open/Closed example in Lesson 1. Strategy is literally "OCP, packaged."

### ⚖️ Good & bad
**Good:** kills big `if`/`switch` blocks; each algorithm is isolated and testable; add new ones freely.
**Bad / avoid when:** there's only ever one algorithm, or the differences are a one-liner (then a simple `if` is fine — don't over-engineer).

### 🧭 Tech-lead note
In C#, a strategy can be as lightweight as a **delegate**: `Func<Order, decimal>`. You don't always need a full class. And with .NET 8 **keyed DI**, you can register many strategies and resolve the right one by key (e.g., by the shipping method name). This is the workhorse pattern — you'll use it constantly.

📺 [Christopher Okhravi – Strategy](https://www.youtube.com/results?search_query=christopher+okhravi+strategy+pattern) · [Refactoring Guru](https://refactoring.guru/design-patterns/strategy/csharp/example)

---

## 4.2 — Observer ⭐

### 🎯 The one-line idea
When one object changes, it automatically **notifies a list of interested objects** — without knowing exactly who they are.

### 🌍 Real-world analogy
**A YouTube channel.** When a creator uploads a video, *all subscribers* get notified automatically. The creator doesn't call each subscriber personally; they just "publish," and everyone who subscribed gets pinged. Subscribers can join or leave anytime.

### 😤 The problem
When a stock price changes, several things must react: update the UI, log it, maybe send an alert. If the `Stock` class calls each of them directly, it's tightly coupled to all of them and must be edited whenever a new reactor appears.

### ✅ The solution — the idiomatic C# way uses `event`
C# has this pattern baked into the language with `event`.

```csharp
public class Stock
{
    // An event = a built-in list of subscribers to notify.
    public event Action<decimal>? PriceChanged;

    private decimal _price;
    public decimal Price
    {
        get => _price;
        set
        {
            _price = value;
            PriceChanged?.Invoke(value);   // notify everyone who subscribed
        }
    }
}
```

Usage — subscribers "subscribe" with `+=`:

```csharp
var stock = new Stock();

stock.PriceChanged += price => Console.WriteLine($"UI update: {price}");   // subscriber 1
stock.PriceChanged += price => Console.WriteLine($"Log: price is {price}"); // subscriber 2

stock.Price = 99.5m;  // BOTH subscribers fire automatically
```

### 🔍 Walkthrough
- `event Action<decimal>` is C#'s built-in subscriber list. `Action<decimal>` means "a function that takes a decimal and returns nothing."
- `PriceChanged?.Invoke(value)` calls every subscriber. The `?.` means "only if there's at least one subscriber" (avoids a crash when the list is empty).
- `+=` adds a subscriber; `-=` removes one. The `Stock` has no idea who's listening — total decoupling.

### ⚖️ Good & bad
**Good:** loose coupling; add/remove reactions anytime; the publisher stays simple.
**Bad / avoid when:** ⚠️ **memory leaks** — if a long-lived object subscribes and never does `-=` to unsubscribe, it can't be garbage-collected. This is a genuine production bug. Also, subscriber order and re-entrancy can surprise you.

### 🧭 Tech-lead note
`event`/`Action`, `IObservable<T>` (Reactive Extensions / Rx), and `INotifyPropertyChanged` (used by UI binding) are all the Observer pattern. In reviews, **hunt for missing `-=` unsubscribes** on long-lived subscribers — leaked event handlers are a classic memory leak.

📺 [Christopher Okhravi – Observer](https://www.youtube.com/results?search_query=christopher+okhravi+observer+pattern) · [Refactoring Guru](https://refactoring.guru/design-patterns/observer/csharp/example)

---

## 4.3 — Command ⭐

### 🎯 The one-line idea
Wrap an action (and the data it needs) into its own **object**, so you can store it, queue it, pass it around, log it, or undo it.

### 🌍 Real-world analogy
A **restaurant order ticket.** When you order, the waiter writes a ticket. That ticket is a *thing* — it can be put in a queue, handed to the kitchen, logged, or cancelled. The action "make this dish" became a physical object you can manage.

### 😤 The problem
You want undo/redo in a text editor. If typing directly mutates the document, there's no record of *what* happened, so you can't reverse it.

### ✅ The solution
Turn each action into an object with `Execute()` and `Undo()`.

```csharp
public interface ICommand
{
    void Execute();
    void Undo();
}

public class AddTextCommand : ICommand
{
    private readonly Document _doc;
    private readonly string _text;

    public AddTextCommand(Document doc, string text) => (_doc, _text) = (doc, text);

    public void Execute() => _doc.Append(_text);            // do it
    public void Undo()    => _doc.RemoveLast(_text.Length); // reverse it
}
```

An "invoker" keeps a history so it can undo:

```csharp
public class Editor
{
    private readonly Stack<ICommand> _history = new();

    public void Run(ICommand command)
    {
        command.Execute();
        _history.Push(command);   // remember it
    }

    public void Undo()
    {
        if (_history.Count > 0)
            _history.Pop().Undo(); // reverse the most recent command
    }
}
```

### 🔍 Walkthrough
- Each command knows how to **do** and **undo** itself, plus carries the data it needs (`_text`).
- The `Editor` doesn't know what any command *does* — it just calls `Execute()` and keeps a stack. Undo is just "pop the last command and call `Undo()`."
- Because commands are objects, you could also put them in a **queue**, run them later, or **log** them for auditing.

### ⚖️ Good & bad
**Good:** enables undo/redo, queuing, logging, and "macro" commands (a command made of commands); decouples the thing requesting an action from the thing performing it.
**Bad / avoid when:** you don't need any of those abilities — then it's just extra classes for a plain method call.

### 🧭 Tech-lead note
The hugely popular **MediatR** library is the Command pattern: you send a `Request` object and a `Handler` executes it. It's the backbone of **CQRS** (separating read and write operations) in .NET. You'll review a lot of this code — understand it well.

📺 [Christopher Okhravi – Command](https://www.youtube.com/results?search_query=christopher+okhravi+command+pattern) · [Refactoring Guru](https://refactoring.guru/design-patterns/command/csharp/example)

---

## 4.4 — Template Method

### 🎯 The one-line idea
A base class defines the **fixed skeleton** of a process, but leaves specific steps as blanks for subclasses to fill in.

### 🌍 Real-world analogy
A **recipe template for hot drinks.** The steps are always: boil water → add the main ingredient → pour into cup → add condiments. "Tea" and "coffee" fill in the *specific* steps (tea leaves vs coffee grounds), but the overall sequence never changes.

### 😤 The problem
You import data from CSV and from XML. Both follow the same flow — read the file, parse it, validate, save — but the *parse* step differs. Copy-pasting the whole flow for each format duplicates the shared parts.

### ✅ The solution
Put the fixed flow in a base class; make the varying steps abstract.

```csharp
public abstract class DataImporter
{
    // The "template method" — the fixed skeleton. Notice it's NOT virtual: the order is locked.
    public void Import(string path)
    {
        var raw     = ReadFile(path);
        var records = Parse(raw);      // <- the step that varies
        Validate(records);
        Save(records);
        Console.WriteLine("Import complete.");
    }

    protected string ReadFile(string path) => "raw data";      // shared step
    protected abstract List<string> Parse(string raw);          // subclasses MUST fill this in
    protected virtual void Validate(List<string> records) { }   // optional "hook" step
    protected void Save(List<string> records) => Console.WriteLine($"Saved {records.Count}"); // shared
}

public class CsvImporter : DataImporter
{
    protected override List<string> Parse(string raw) => raw.Split(',').ToList(); // CSV-specific
}
```

### 🔍 Walkthrough
- `Import` is the template method: it defines the *order* of steps and never changes.
- `Parse` is `abstract` → each subclass **must** provide it.
- `Validate` is `virtual` with an empty body → an optional "hook" a subclass *may* override.
- Subclasses can't reorder the steps, which enforces the correct process.

### ⚖️ Good & bad
**Good:** reuse the shared skeleton; guarantee the steps run in the right order.
**Bad / avoid when:** it relies on inheritance, which is rigid. If you need to swap steps at *runtime*, prefer **Strategy** (composition) instead.

### 🧭 Tech-lead note
Template Method (inheritance) and Strategy (composition) solve similar problems. Following "prefer composition over inheritance" (Lesson 1), reach for Strategy unless the shared skeleton is genuinely stable and simple.

📺 [Christopher Okhravi – Template Method](https://www.youtube.com/results?search_query=christopher+okhravi+template+method+pattern) · [Refactoring Guru](https://refactoring.guru/design-patterns/template-method/csharp/example)

---

## 4.5 — State

### 🎯 The one-line idea
An object changes its **behavior** when its internal **state** changes — as if it switched to a different class.

### 🌍 Real-world analogy
A **traffic light.** When it's "Red," it behaves one way (cars stop) and knows the next state is "Green." When "Green," it behaves differently and knows the next is "Yellow." Each state knows how to behave *and* what comes next.

### 😤 The problem
An order's behavior depends on its status, leading to `switch(status)` blocks scattered everywhere:

```csharp
public void Ship()
{
    if (Status == "Pending") throw new Exception("Can't ship, not paid");
    if (Status == "Paid")    Status = "Shipped";
    if (Status == "Shipped") throw new Exception("Already shipped");
    // this same switch appears in Cancel(), Refund(), etc.
}
```

### ✅ The solution
Make each state its own class that knows how to behave and what state comes next.

```csharp
public interface IOrderState
{
    string Name { get; }
    IOrderState Next();   // returns the state to transition into
}

public class PendingState : IOrderState
{
    public string Name => "Pending";
    public IOrderState Next() => new PaidState();
}

public class PaidState : IOrderState
{
    public string Name => "Paid";
    public IOrderState Next() => new ShippedState();
}

public class ShippedState : IOrderState
{
    public string Name => "Shipped";
    public IOrderState Next() => this;   // terminal state — stays put
}

// The object that HAS a state
public class Order
{
    private IOrderState _state = new PendingState();
    public string Status => _state.Name;
    public void Advance() => _state = _state.Next();
}
```

### 🔍 Walkthrough
Each state class owns its own rules and transition. The `Order` just delegates to its current state (`_state.Next()`). All those scattered `if (Status == ...)` checks disappear — the logic lives inside the state classes.

### ⚖️ Good & bad
**Good:** removes tangled state conditionals; each state's rules are in one place.
**Bad / avoid when:** you have only 2 simple states — a boolean is fine. Complex real-world state machines are better handled by a library.

### 🔁 State vs Strategy (they share the same shape)
- **Strategy:** *you* pick the algorithm from outside; the strategies don't know about each other.
- **State:** the object switches its *own* behavior internally, and states often know which state comes next.

### 🧭 Tech-lead note
For anything beyond a few states, use a dedicated state-machine library like **Stateless** rather than hand-rolling transitions — it makes the allowed transitions explicit and prevents illegal ones.

📺 [Christopher Okhravi – State](https://www.youtube.com/results?search_query=christopher+okhravi+state+pattern) · [Refactoring Guru](https://refactoring.guru/design-patterns/state/csharp/example)

---

## 4.6 — Chain of Responsibility

### 🎯 The one-line idea
Pass a request along a **line of handlers**; each one either handles it or passes it to the next.

### 🌍 Real-world analogy
**Customer support tiers.** Your issue goes to Tier 1. If they can't solve it, they escalate to Tier 2, then Tier 3. Each tier either handles your request or forwards it. You (the sender) don't need to know which tier will actually solve it.

### 😤 The problem
An incoming web request must pass several checks in order — authenticated? valid input? logged? — and you don't want one giant method doing all of it, nor the caller hard-wired to each check.

### ✅ The solution
Each handler holds a reference to the next and passes the request along.

```csharp
public abstract class Handler
{
    protected Handler? Next;
    public Handler SetNext(Handler next) { Next = next; return next; } // enables chaining
    public abstract void Handle(Request request);
}

public class AuthHandler : Handler
{
    public override void Handle(Request r)
    {
        if (!r.IsAuthenticated) { Console.WriteLine("Rejected: not authenticated"); return; }
        Console.WriteLine("Auth OK");
        Next?.Handle(r);   // pass to the next handler
    }
}

public class ValidationHandler : Handler
{
    public override void Handle(Request r)
    {
        if (string.IsNullOrEmpty(r.Body)) { Console.WriteLine("Rejected: empty body"); return; }
        Console.WriteLine("Validation OK");
        Next?.Handle(r);
    }
}
```

Usage — build the chain, then send one request in:

```csharp
var auth = new AuthHandler();
var validation = new ValidationHandler();
auth.SetNext(validation);       // auth → validation

auth.Handle(myRequest);         // flows down the chain
```

### 🔍 Walkthrough
Each handler does its bit, then calls `Next?.Handle(r)` to continue — *unless* it decides to stop the chain (like rejecting an unauthenticated request). You can add, remove, or reorder handlers without touching the others.

### ⚖️ Good & bad
**Good:** decouples sender from the handlers; add/reorder steps easily; each step is a small class.
**Bad / avoid when:** a request might fall off the end unhandled; long chains can be hard to trace.

### 🧭 Tech-lead note
**ASP.NET Core middleware** is exactly this pattern — each middleware handles the request or calls `next()`. So is the `HttpClient` `DelegatingHandler` pipeline. You already rely on Chain of Responsibility every day.

📺 [Christopher Okhravi – Chain of Responsibility](https://www.youtube.com/results?search_query=christopher+okhravi+chain+of+responsibility) · [Refactoring Guru](https://refactoring.guru/design-patterns/chain-of-responsibility/csharp/example)

---

## 4.7 — Mediator

### 🎯 The one-line idea
Instead of many objects talking *directly* to each other (a tangled web), they all talk through **one central hub**.

### 🌍 Real-world analogy
**Air traffic control.** Planes don't coordinate landings by radioing each other directly — that would be chaos. They all talk to the control tower (the mediator), which coordinates everyone. Remove direct plane-to-plane chatter; add one hub.

### 😤 The problem
In a complex form, changing the country dropdown must update the state dropdown, enable a button, and clear a field. If each control directly references every other control, you get a spaghetti of interconnections that's impossible to change.

### ✅ The solution (concept)
Each component knows only the mediator. When something happens, it tells the mediator, and the mediator coordinates the rest.

```csharp
public interface IDialogMediator { void Notify(object sender, string ev); }

public class RegistrationDialog : IDialogMediator
{
    // The mediator holds references to the components and coordinates them.
    public Button SubmitButton { get; set; } = new();
    public Checkbox AgreeTerms { get; set; } = new();

    public void Notify(object sender, string ev)
    {
        if (sender == AgreeTerms && ev == "toggled")
            SubmitButton.Enabled = AgreeTerms.Checked;   // one place coordinates the interaction
    }
}
```

The components just call `mediator.Notify(this, "toggled")` — they never reference each other.

### ⚖️ Good & bad
**Good:** turns a many-to-many mess into simple hub-and-spoke; interaction logic lives in one place.
**Bad / avoid when:** the mediator itself can become a bloated "god object." Don't route trivial calls through a mediator just for the sake of it.

### 🧭 Tech-lead note
Again, **MediatR** is the famous .NET example (it's Mediator + Command combined). It's great for decoupling controllers from handlers — but teams often over-apply it, adding indirection for simple operations. Weigh that trade-off in design reviews.

📺 [Christopher Okhravi – Mediator](https://www.youtube.com/results?search_query=christopher+okhravi+mediator+pattern) · [Refactoring Guru](https://refactoring.guru/design-patterns/mediator/csharp/example)

---

## 4.8 — Iterator (C# gives you this for free)

### 🎯 The one-line idea
Step through the items in a collection one by one **without needing to know how the collection stores them** internally.

### 🌍 Real-world analogy
A **TV remote's "next channel" button.** You go through channels one at a time. You don't care whether channels are stored in an array, a list, or over cable — "next" just works.

### The C# reality
You almost never implement this by hand, because C# **is** the iterator pattern: `foreach`, `IEnumerable<T>`, and especially the `yield return` keyword.

```csharp
public IEnumerable<int> FirstThreeEvens()
{
    yield return 0;   // 'yield' hands back one item at a time, pausing between each
    yield return 2;
    yield return 4;
}

foreach (var n in FirstThreeEvens())   // foreach IS using an iterator under the hood
    Console.WriteLine(n);
```

### 🔍 Walkthrough
`yield return` is C# building an iterator for you automatically — it produces items lazily (one at a time, only when asked). This is senior-level knowledge: understanding that `yield` creates a hidden state machine and that LINQ is built on lazy iteration.

### 🧭 Tech-lead note
Know that `IEnumerable<T>` is lazy (nothing runs until you enumerate it) and that enumerating twice runs the work twice. This trips up many developers — worth watching for in reviews.

📺 [Refactoring Guru – Iterator (C#)](https://refactoring.guru/design-patterns/iterator/csharp/example)

---

## 4.9 — Visitor

### 🎯 The one-line idea
Add **new operations** to a group of objects **without modifying those objects' classes.**

### 🌍 Real-world analogy
A **tax auditor visiting different businesses.** Each business (restaurant, factory, shop) lets the auditor in and the auditor performs the audit appropriate to that business type. You can send a *different* kind of visitor next month (a safety inspector) without changing the businesses.

### The problem it solves
You have a fixed set of classes (say, shapes: Circle, Square, Triangle) and you keep needing *new operations* over all of them: calculate area, export to SVG, calculate perimeter… Adding each operation as a method to every shape class is invasive and spreads unrelated logic across them.

### ✅ The solution (concept)
Move each operation into a separate "visitor" object. Each shape has an `Accept(visitor)` method that calls the visitor back — a trick called *double dispatch*.

```csharp
public interface IShapeVisitor { void Visit(Circle c); void Visit(Square s); }

public interface IShape { void Accept(IShapeVisitor visitor); }

public class Circle : IShape
{
    public float Radius = 5;
    public void Accept(IShapeVisitor v) => v.Visit(this);   // calls the matching Visit
}

// A new operation = a new visitor class. The shapes never change.
public class AreaCalculator : IShapeVisitor
{
    public void Visit(Circle c) => Console.WriteLine(3.14f * c.Radius * c.Radius);
    public void Visit(Square s) => Console.WriteLine("square area...");
}
```

### ⚖️ Good & bad
**Good:** add new operations without touching the element classes.
**Bad:** the reverse is painful — adding a new *shape* forces you to update *every* visitor. Also verbose.

### 🧭 Tech-lead note — modern C# often replaces Visitor
With **pattern matching / switch expressions** over a sealed type hierarchy, you often don't need the full Visitor ceremony:

```csharp
decimal Area(IShape shape) => shape switch
{
    Circle c => 3.14m * (decimal)(c.Radius * c.Radius),
    Square s => /* ... */ 0m,
    _        => throw new ArgumentException("Unknown shape")
};
```
This is far less code. Reach for classic Visitor mainly for genuinely complex, stable structures (like compiler ASTs).

📺 [Christopher Okhravi – Visitor](https://www.youtube.com/results?search_query=christopher+okhravi+visitor+pattern) · [Refactoring Guru](https://refactoring.guru/design-patterns/visitor/csharp/example)

---

## 4.10 — Memento

### 🎯 The one-line idea
Capture an object's current state so you can **restore it later** — without exposing the object's private internals.

### 🌍 Real-world analogy
A **video game save point.** You save your progress (a snapshot). If things go wrong later, you reload the save and you're back exactly where you were.

### The idea in brief
The object produces a "memento" (a snapshot of its state). Something else stores these snapshots. To undo, you hand a snapshot back to the object and it restores itself.

```csharp
public record EditorSnapshot(string Content);   // the memento (immutable snapshot)

public class TextEditor
{
    public string Content { get; private set; } = "";
    public void Type(string text) => Content += text;

    public EditorSnapshot Save() => new(Content);            // create a snapshot
    public void Restore(EditorSnapshot s) => Content = s.Content; // roll back to it
}
```

### ⚖️ When
Undo/redo, checkpoints, "cancel changes" features. Often paired with **Command** (4.3) to build undo systems.

### 🧭 Tech-lead note
C# `record` types make mementos cheap: they're immutable snapshots by design. Just remember `record` copies are shallow — nested mutable objects inside are still shared.

📺 [Refactoring Guru – Memento (C#)](https://refactoring.guru/design-patterns/memento/csharp/example)

---

## 4.11 — Interpreter (rare — just be aware)

### 🎯 The one-line idea
Define a small "language" (a grammar) and a way to evaluate sentences written in it.

### The honest advice
You almost never build this by hand. If you need to parse a mini-language or expression, use a proper library (**ANTLR**, **Sprache**, **Superpower**) or C#'s built-in `Expression` trees. Just know the pattern exists so you recognize the name.

📺 [Refactoring Guru – Interpreter](https://refactoring.guru/design-patterns/interpreter)

---

## ✅ Lesson 4 recap

| Pattern | In one line | Real C# example |
|---|---|---|
| **Strategy** ⭐ | Swap an algorithm at runtime | `Func<>`, keyed DI |
| **Observer** ⭐ | Notify many when one changes | `event`, `IObservable<T>`, Rx |
| **Command** ⭐ | Turn an action into an object | MediatR, CQRS, undo/redo |
| Template Method | Fixed skeleton, fill-in steps | Base-class workflows |
| State | Behavior changes with state | Stateless library, workflows |
| Chain of Responsibility | Pass request down a line | ASP.NET middleware |
| Mediator | Central hub, no direct talk | MediatR |
| Iterator | Step through a collection | `foreach`, `yield`, LINQ |
| Visitor | New operations without editing classes | Often replaced by pattern matching |
| Memento | Save & restore state | `record` snapshots + Command |
| Interpreter | Evaluate a mini-language | Use a parser library instead |

**The three to master first:** Strategy, Observer, Command.

**The classic "same shape, different intent" pairs to keep straight:**
- Strategy (you choose) vs State (object switches itself)
- Command (an action as an object) vs Strategy (an algorithm as an object)

➡️ Final lesson: **`05-modern-csharp-and-next-steps.md`** — how modern C# quietly does many of these for you, plus your roadmap as a senior/tech lead.
