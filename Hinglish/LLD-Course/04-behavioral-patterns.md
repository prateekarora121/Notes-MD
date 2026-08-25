# Lesson 4 — Behavioral Patterns

**Behavioral patterns iske baare mein hain ki *objects kaise behave karte hain aur ek dusre se kaise baat karte hain* — responsibilities aur actions unke beech kaise share hote hain.**

Yeh sabse bada aur practically sabse useful group hai. Agar aap apne career mein sirf kuch hi patterns truly master karo, to woh **Strategy, Observer, aur Command** hone chahiye — yeh real C# mein har jagah dikhte hain.

Hum cover karenge, roughly easiest first:
1. **Strategy** ⭐ — runtime par ek algorithm swap karo
2. **Observer** ⭐ — jab kuch change ho to bahut saare objects ko notify karo
3. **Command** ⭐ — ek action ko object mein badal do (undo, queues enable karta hai)
4. **Template Method** — steps fix karo, blanks subclasses ko fill karne do
5. **State** — ek object apni state change hone par behavior change karta hai
6. **Chain of Responsibility** — request ko handlers ki line mein pass karo
7. **Mediator** — ek hub jisse objects directly baat na karein
8. **Iterator** — collection ke through step karo (C# yeh free mein deta hai)
9. **Visitor** — kisi structure ko change kiye bina naye operations add karo
10. **Memento** — object ki state save aur restore karo (undo)
11. **Interpreter** — (rare — sirf aware raho ki yeh exist karta hai)

---

## 4.1 — Strategy ⭐ (isse pehle seekho)

### 🎯 One-line idea
Kisi kaam karne ke har tarike ko apni khud ki class mein daalo, sab ek interface share karte hue, aur **runtime par swap karo ki kaunsa use karna hai.**

### 🌍 Real-world analogy
**Airport jaana.** Goal same hai (wahan pohochna), lekin aap ek *strategy* choose kar sakte ho: drive karo, taxi lo, ya train se jao. Aap apni situation (budget, time) ke basis par choose karte ho. Aap goal change kiye bina strategies switch kar sakte ho.

### 😤 Problem
Aap shipping cost calculate karte ho `if`s ke badhte hue pile ke saath:

```csharp
public decimal Calculate(Order order, string method)
{
    if (method == "standard") return order.Weight * 1.5m;
    if (method == "express")  return order.Weight * 3.0m + 10;
    if (method == "drone")    return order.Weight * 5.0m + 25;  // every new method edits this
    return 0;
}
```
Yeh Open/Closed (Lesson 1) violate karta hai: har naya shipping method working code ko edit karne par majboor karta hai, aur yeh `if` blob copy-paste ho ke idhar-udhar failta rehta hai.

### ✅ Solution
Ek interface, har algorithm ke liye ek class, runtime par choose hota hai.

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

Usage — strategy pick karo aur hand in karo:

```csharp
var calc = new ShippingCalculator(new ExpressShipping());
decimal cost = calc.GetCost(myOrder);
// Adding "drone shipping" = add ONE new class. No existing code changes.
```

### 🔍 Walkthrough
- Har algorithm apni khud ki class mein rehta hai → padhna easy, isolation mein unit-test karna easy.
- `ShippingCalculator` ko nahi pata *kaunsi* strategy uske paas hai — usse sirf `IShippingStrategy` contract pata hai. Yahi Dependency Inversion habit hai Lesson 1 se.
- Dhyan do yeh *exact same idea* hai jo Lesson 1 ke Open/Closed example mein thi. Strategy literally "OCP, packaged" hai.

### ⚖️ Fayde / Nuksan
**Fayda:** bade `if`/`switch` blocks khatam karta hai; har algorithm isolated aur testable hota hai; naye freely add karo.
**Nuksan / avoid karo jab:** sirf ek hi algorithm hamesha hoga, ya differences sirf ek-liner hain (tab simple `if` theek hai — over-engineer na karo).

### 🧭 Tech-lead note
C# mein, strategy ek **delegate** jitna lightweight ho sakta hai: `Func<Order, decimal>`. Aapko hamesha full class ki zarurat nahi hoti. Aur .NET 8 ke **keyed DI** ke saath, aap bahut saari strategies register kar sakte ho aur right one ko key se resolve kar sakte ho (jaise shipping method name se). Yeh workhorse pattern hai — isko aap constantly use karoge.

📺 [Christopher Okhravi – Strategy](https://www.youtube.com/results?search_query=christopher+okhravi+strategy+pattern) · [Refactoring Guru](https://refactoring.guru/design-patterns/strategy/csharp/example)

---

## 4.2 — Observer ⭐

### 🎯 One-line idea
Jab ek object change hota hai, woh automatically **interested objects ki list ko notify karta hai** — bina yeh jaane ki woh exactly kaun hain.

### 🌍 Real-world analogy
**Ek YouTube channel.** Jab creator video upload karta hai, *sab subscribers* automatically notified ho jaate hain. Creator har subscriber ko personally call nahi karta; woh sirf "publish" karta hai, aur jisne bhi subscribe kiya hai usse ping mil jaata hai. Subscribers kabhi bhi join ya leave kar sakte hain.

### 😤 Problem
Jab stock price change hoti hai, kai chize react karni chahiye: UI update karo, log karo, shayad alert bhejo. Agar `Stock` class inko directly call kare, to woh unn sab se tightly coupled ho jaati hai aur jab bhi naya reactor aaye to edit karni padti hai.

### ✅ Solution — idiomatic C# way `event` use karta hai
C# mein yeh pattern language mein `event` ke saath built-in hai.

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

Usage — subscribers `+=` se "subscribe" karte hain:

```csharp
var stock = new Stock();

stock.PriceChanged += price => Console.WriteLine($"UI update: {price}");   // subscriber 1
stock.PriceChanged += price => Console.WriteLine($"Log: price is {price}"); // subscriber 2

stock.Price = 99.5m;  // BOTH subscribers fire automatically
```

### 🔍 Walkthrough
- `event Action<decimal>` C# ki built-in subscriber list hai. `Action<decimal>` ka matlab hai "ek function jo decimal leta hai aur kuch return nahi karta."
- `PriceChanged?.Invoke(value)` har subscriber ko call karta hai. `?.` ka matlab hai "sirf agar kam se kam ek subscriber ho" (list empty hone par crash avoid karta hai).
- `+=` subscriber add karta hai; `-=` ek remove karta hai. `Stock` ko koi idea nahi hai ki kaun sun raha hai — total decoupling.

### ⚖️ Fayde / Nuksan
**Fayda:** loose coupling; reactions kabhi bhi add/remove karo; publisher simple rehta hai.
**Nuksan / avoid karo jab:** ⚠️ **memory leaks** — agar ek long-lived object subscribe karke kabhi `-=` se unsubscribe nahi karta, to woh garbage-collected nahi ho sakta. Yeh ek genuine production bug hai. Iske alawa, subscriber order aur re-entrancy aapko surprise kar sakte hain.

### 🧭 Tech-lead note
`event`/`Action`, `IObservable<T>` (Reactive Extensions / Rx), aur `INotifyPropertyChanged` (UI binding mein use hota hai) sab Observer pattern hi hain. Reviews mein, **missing `-=` unsubscribes hunt karo** long-lived subscribers par — leaked event handlers ek classic memory leak hain.

📺 [Christopher Okhravi – Observer](https://www.youtube.com/results?search_query=christopher+okhravi+observer+pattern) · [Refactoring Guru](https://refactoring.guru/design-patterns/observer/csharp/example)

---

## 4.3 — Command ⭐

### 🎯 One-line idea
Ek action (aur uske liye chahiye data) ko apni khud ki **object** mein wrap kar do, taaki aap usse store, queue, pass around, log, ya undo kar sako.

### 🌍 Real-world analogy
Ek **restaurant order ticket.** Jab aap order karte ho, waiter ek ticket likhta hai. Woh ticket ek *thing* hai — usse queue mein daala ja sakta hai, kitchen ko handed kiya ja sakta hai, log kiya ja sakta hai, ya cancel kiya ja sakta hai. "Yeh dish banao" action ek physical object ban gaya jise aap manage kar sakte ho.

### 😤 Problem
Aapko text editor mein undo/redo chahiye. Agar typing directly document ko mutate karti hai, to *kya* hua uska koi record nahi hota, isliye aap usse reverse nahi kar sakte.

### ✅ Solution
Har action ko `Execute()` aur `Undo()` waali ek object mein badal do.

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

Ek "invoker" history rakhta hai taaki undo kar sake:

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
- Har command ko pata hai kaise **do** aur **undo** karna hai apne aap ko, plus jo data chahiye (`_text`) usse carry karta hai.
- `Editor` ko pata nahi ki koi command *kya* karti hai — woh sirf `Execute()` call karta hai aur ek stack rakhta hai. Undo bas "last command pop karo aur `Undo()` call karo" hai.
- Kyunki commands objects hain, aap unko ek **queue** mein bhi daal sakte ho, later run kar sakte ho, ya auditing ke liye **log** kar sakte ho.

### ⚖️ Fayde / Nuksan
**Fayda:** undo/redo, queuing, logging, aur "macro" commands (ek command jo commands se bani ho) enable karta hai; action request karne waali chiz ko usse perform karne waali chiz se decouple karta hai.
**Nuksan / avoid karo jab:** aapko unn abilities mein se koi bhi nahi chahiye — tab yeh sirf ek plain method call ke liye extra classes hai.

### 🧭 Tech-lead note
Bahut popular **MediatR** library Command pattern hi hai: aap ek `Request` object bhejte ho aur ek `Handler` usse execute karta hai. Yeh .NET mein **CQRS** (read aur write operations separate karna) ka backbone hai. Aap review karoge iss code ka bahut — isse achhe se samjho.

📺 [Christopher Okhravi – Command](https://www.youtube.com/results?search_query=christopher+okhravi+command+pattern) · [Refactoring Guru](https://refactoring.guru/design-patterns/command/csharp/example)

---

## 4.4 — Template Method

### 🎯 One-line idea
Ek base class kisi process ka **fixed skeleton** define karti hai, lekin specific steps ko subclasses fill karne ke liye blanks ki tarah leave kar deti hai.

### 🌍 Real-world analogy
Ek **hot drinks ke liye recipe template.** Steps hamesha same hote hain: paani boil karo → main ingredient add karo → cup mein pour karo → condiments add karo. "Tea" aur "coffee" *specific* steps fill karte hain (tea leaves vs coffee grounds), lekin overall sequence kabhi nahi badalta.

### 😤 Problem
Aap CSV aur XML se data import karte ho. Dono same flow follow karte hain — file padho, parse karo, validate karo, save karo — lekin *parse* step different hai. Har format ke liye whole flow copy-paste karna shared parts duplicate karta hai.

### ✅ Solution
Fixed flow ko ek base class mein daalo; varying steps ko abstract banao.

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
- `Import` template method hai: yeh steps ka *order* define karta hai aur kabhi nahi badalta.
- `Parse` `abstract` hai → har subclass ko yeh **zaroor** provide karna hai.
- `Validate` `virtual` hai empty body ke saath → ek optional "hook" jise subclass *chahe to* override kar sakta hai.
- Subclasses steps ko reorder nahi kar sakte, jo correct process enforce karta hai.

### ⚖️ Fayde / Nuksan
**Fayda:** shared skeleton reuse hota hai; guarantee milti hai ki steps right order mein run hon.
**Nuksan / avoid karo jab:** yeh inheritance par depend karta hai, jo rigid hota hai. Agar aapko steps *runtime* par swap karne hain, to **Strategy** (composition) prefer karo.

### 🧭 Tech-lead note
Template Method (inheritance) aur Strategy (composition) similar problems solve karte hain. "Composition over inheritance" (Lesson 1) follow karte hue, Strategy ke liye jao jab tak shared skeleton genuinely stable aur simple na ho.

📺 [Christopher Okhravi – Template Method](https://www.youtube.com/results?search_query=christopher+okhravi+template+method+pattern) · [Refactoring Guru](https://refactoring.guru/design-patterns/template-method/csharp/example)

---

## 4.5 — State

### 🎯 One-line idea
Ek object apna **behavior** change karta hai jab uska internal **state** change hota hai — jaise ki woh ek different class mein switch ho gaya ho.

### 🌍 Real-world analogy
Ek **traffic light.** Jab yeh "Red" hai, yeh ek tarike se behave karta hai (cars stop karti hain) aur jaanta hai ki next state "Green" hai. Jab "Green" hai, yeh differently behave karta hai aur jaanta hai ki next "Yellow" hai. Har state ko pata hai kaise behave karna hai *aur* aage kya aata hai.

### 😤 Problem
Ek order ka behavior uske status par depend karta hai, jisse `switch(status)` blocks har jagah scatter ho jaate hain:

```csharp
public void Ship()
{
    if (Status == "Pending") throw new Exception("Can't ship, not paid");
    if (Status == "Paid")    Status = "Shipped";
    if (Status == "Shipped") throw new Exception("Already shipped");
    // this same switch appears in Cancel(), Refund(), etc.
}
```

### ✅ Solution
Har state ko apni khud ki class banao jise pata ho kaise behave karna hai aur next kaunsi state aati hai.

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
Har state class apne khud ke rules aur transition ko owns karti hai. `Order` bas apni current state ko delegate karta hai (`_state.Next()`). Woh sab scattered `if (Status == ...)` checks gayab ho jaate hain — logic state classes ke andar rehta hai.

### ⚖️ Fayde / Nuksan
**Fayda:** tangled state conditionals hata deta hai; har state ke rules ek jagah hote hain.
**Nuksan / avoid karo jab:** aapke paas sirf 2 simple states hain — ek boolean theek hai. Complex real-world state machines better ek library se handle hote hain.

### 🔁 State vs Strategy (dono ka shape same hai)
- **Strategy:** *aap* bahar se algorithm pick karte ho; strategies ek dusre ke baare mein nahi jaanti.
- **State:** object apna *khud ka* behavior internally switch karta hai, aur states aksar jaanti hain ki next kaunsi state aati hai.

### 🧭 Tech-lead note
Kuch states se zyada ke liye, hand-rolled transitions ke bajaye ek dedicated state-machine library jaise **Stateless** use karo — yeh allowed transitions explicit banata hai aur illegal transitions ko prevent karta hai.

📺 [Christopher Okhravi – State](https://www.youtube.com/results?search_query=christopher+okhravi+state+pattern) · [Refactoring Guru](https://refactoring.guru/design-patterns/state/csharp/example)

---

## 4.6 — Chain of Responsibility

### 🎯 One-line idea
Request ko handlers ki ek **line** ke through pass karo; har handler ya to usse handle karta hai ya next ko pass kar deta hai.

### 🌍 Real-world analogy
**Customer support tiers.** Aapka issue Tier 1 ko jaata hai. Agar woh solve nahi kar sakte, to Tier 2 ko escalate hota hai, phir Tier 3. Har tier ya to aapka request handle karta hai ya forward kar deta hai. Aapko (sender ko) yeh jaanne ki zarurat nahi hai ki actually kaunsi tier solve karegi.

### 😤 Problem
Ek incoming web request ko order mein kai checks pass karne hote hain — authenticated? valid input? logged? — aur aap chahte nahi ki ek giant method sab kuch kare, na hi caller har check se hard-wired ho.

### ✅ Solution
Har handler next ka reference rakhta hai aur request ko aage pass karta hai.

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

Usage — chain build karo, phir ek request bhejo:

```csharp
var auth = new AuthHandler();
var validation = new ValidationHandler();
auth.SetNext(validation);       // auth → validation

auth.Handle(myRequest);         // flows down the chain
```

### 🔍 Walkthrough
Har handler apna bit karta hai, phir continue karne ke liye `Next?.Handle(r)` call karta hai — *jab tak* woh chain stop karne ka decide nahi karta (jaise ek unauthenticated request reject karna). Aap handlers ko baaki ko touch kiye bina add, remove, ya reorder kar sakte ho.

### ⚖️ Fayde / Nuksan
**Fayda:** sender ko handlers se decouple karta hai; steps easily add/reorder karo; har step ek small class hai.
**Nuksan / avoid karo jab:** koi request chain ke end tak unhandled reh sakti hai; long chains trace karna hard ho sakta hai.

### 🧭 Tech-lead note
**ASP.NET Core middleware** exactly yehi pattern hai — har middleware request handle karta hai ya `next()` call karta hai. Waise hi `HttpClient` ka `DelegatingHandler` pipeline bhi. Aap already har din Chain of Responsibility par rely karte ho.

📺 [Christopher Okhravi – Chain of Responsibility](https://www.youtube.com/results?search_query=christopher+okhravi+chain+of+responsibility) · [Refactoring Guru](https://refactoring.guru/design-patterns/chain-of-responsibility/csharp/example)

---

## 4.7 — Mediator

### 🎯 One-line idea
Bahut saare objects *directly* ek dusre se baat karne (ek tangled web) ke bajaye, sab ek central **hub** ke through baat karte hain.

### 🌍 Real-world analogy
**Air traffic control.** Planes landings ek dusre ko directly radio karke coordinate nahi karte — woh chaos hoga. Sab control tower (mediator) se baat karte hain, jo sabko coordinate karta hai. Direct plane-to-plane chatter hataayo; ek hub add karo.

### 😤 Problem
Ek complex form mein, country dropdown change karne se state dropdown update hona chahiye, ek button enable hona chahiye, aur ek field clear honi chahiye. Agar har control directly har dusre control ko reference karta hai, to aapko interconnections ka ek spaghetti mil jaata hai jise change karna impossible hai.

### ✅ Solution (concept)
Har component sirf mediator ko jaanta hai. Jab kuch hota hai, woh mediator ko batata hai, aur mediator baaki ko coordinate karta hai.

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

Components sirf `mediator.Notify(this, "toggled")` call karte hain — woh kabhi ek dusre ko reference nahi karte.

### ⚖️ Fayde / Nuksan
**Fayda:** ek many-to-many mess ko simple hub-and-spoke mein badal deta hai; interaction logic ek jagah rehta hai.
**Nuksan / avoid karo jab:** mediator khud ek bloated "god object" ban sakta hai. Trivial calls ko sirf sake ke liye mediator se route na karo.

### 🧭 Tech-lead note
Phir se, **MediatR** famous .NET example hai (yeh Mediator + Command combined hai). Yeh controllers ko handlers se decouple karne ke liye great hai — lekin teams aksar isse over-apply karti hain, simple operations mein indirection add karti hain. Design reviews mein yeh trade-off weigh karo.

📺 [Christopher Okhravi – Mediator](https://www.youtube.com/results?search_query=christopher+okhravi+mediator+pattern) · [Refactoring Guru](https://refactoring.guru/design-patterns/mediator/csharp/example)

---

## 4.8 — Iterator (C# yeh free mein deta hai)

### 🎯 One-line idea
Kisi collection ke items ko ek-ek karke step through karo **bina yeh jaane ki collection unhe internally kaise store karta hai.**

### 🌍 Real-world analogy
Ek **TV remote ka "next channel" button.** Aap channels ko ek time par ek jaate ho. Aapko care nahi ki channels array mein store hain, list mein, ya cable ke through — "next" bas kaam karta hai.

### C# ki reality
Aap yeh almost never hand se implement karte ho, kyunki C# **hi** iterator pattern hai: `foreach`, `IEnumerable<T>`, aur especially `yield return` keyword.

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
`yield return` C# aapke liye automatically ek iterator bana deta hai — yeh items lazily produce karta hai (ek time par ek, sirf jab poocha jaaye). Yeh senior-level knowledge hai: samajhna ki `yield` ek hidden state machine banata hai aur LINQ lazy iteration par built hai.

### 🧭 Tech-lead note
Jaano ki `IEnumerable<T>` lazy hai (kuch bhi tab tak nahi chalta jab tak aap enumerate na karo) aur do baar enumerate karna kaam ko do baar chalata hai. Yeh bahut developers ko trip karta hai — reviews mein isse dhyan se dekho.

📺 [Refactoring Guru – Iterator (C#)](https://refactoring.guru/design-patterns/iterator/csharp/example)

---

## 4.9 — Visitor

### 🎯 One-line idea
Objects ke ek group mein **new operations** add karo **unn objects ki classes modify kiye bina.**

### 🌍 Real-world analogy
Ek **tax auditor jo different businesses visit karta hai.** Har business (restaurant, factory, shop) auditor ko andar aane deta hai aur auditor uss business type ke liye appropriate audit perform karta hai. Aap next month ek *different* kism ka visitor bhej sakte ho (ek safety inspector) bina businesses change kiye.

### Problem jo yeh solve karta hai
Aapke paas fixed classes ka ek set hai (jaise shapes: Circle, Square, Triangle) aur aapko unn sab par baar-baar *new operations* chahiye: area calculate karo, SVG mein export karo, perimeter calculate karo… Har operation ko har shape class mein method ke roop mein add karna invasive hai aur unrelated logic ko unke beech spread kar deta hai.

### ✅ Solution (concept)
Har operation ko ek separate "visitor" object mein move karo. Har shape ka ek `Accept(visitor)` method hota hai jo visitor ko back call karta hai — ek trick jise *double dispatch* kehte hain.

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

### ⚖️ Fayde / Nuksan
**Fayda:** element classes ko touch kiye bina new operations add karo.
**Nuksan:** reverse painful hai — ek nayi *shape* add karne se aapko *har* visitor update karna padta hai. Iske alawa verbose bhi hai.

### 🧭 Tech-lead note — modern C# aksar Visitor ko replace karta hai
Ek sealed type hierarchy par **pattern matching / switch expressions** ke saath, aapko aksar full Visitor ceremony ki zarurat nahi hoti:

```csharp
decimal Area(IShape shape) => shape switch
{
    Circle c => 3.14m * (decimal)(c.Radius * c.Radius),
    Square s => /* ... */ 0m,
    _        => throw new ArgumentException("Unknown shape")
};
```
Yeh kaafi kam code hai. Classic Visitor mainly genuinely complex, stable structures (jaise compiler ASTs) ke liye use karo.

📺 [Christopher Okhravi – Visitor](https://www.youtube.com/results?search_query=christopher+okhravi+visitor+pattern) · [Refactoring Guru](https://refactoring.guru/design-patterns/visitor/csharp/example)

---

## 4.10 — Memento

### 🎯 One-line idea
Ek object ki current state capture karo taaki aap usse **baad mein restore kar sako** — object ke private internals expose kiye bina.

### 🌍 Real-world analogy
Ek **video game save point.** Aap apna progress save karte ho (ek snapshot). Agar baad mein kuch galat ho jaaye, to aap save reload karte ho aur exactly wahi waapas aa jaate ho jahan aap the.

### Idea in brief
Object ek "memento" (uski state ka snapshot) produce karta hai. Kuch aur yeh snapshots store karta hai. Undo karne ke liye, aap ek snapshot object ko waapas de dete ho aur woh apne aap ko restore kar leta hai.

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

### ⚖️ Kab
Undo/redo, checkpoints, "cancel changes" features. Aksar **Command** (4.3) ke saath paired hota hai undo systems banane ke liye.

### 🧭 Tech-lead note
C# `record` types mementos ko cheap banate hain: woh design se immutable snapshots hain. Bas yaad rakhna `record` copies shallow hote hain: andar nested mutable objects still shared rehte hain.

📺 [Refactoring Guru – Memento (C#)](https://refactoring.guru/design-patterns/memento/csharp/example)

---

## 4.11 — Interpreter (rare — sirf aware raho)

### 🎯 One-line idea
Ek chhota "language" (grammar) define karo aur usme likhi sentences ko evaluate karne ka tarika.

### Honest advice
Aap yeh almost never hand se banate ho. Agar aapko mini-language ya expression parse karna hai, to ek proper library use karo (**ANTLR**, **Sprache**, **Superpower**) ya C# ke built-in `Expression` trees. Bas jaano ki pattern exist karta hai taaki naam recognize kar sako.

📺 [Refactoring Guru – Interpreter](https://refactoring.guru/design-patterns/interpreter)

---

## ✅ Lesson 4 recap

| Pattern | In one line | Real C# example |
|---|---|---|
| **Strategy** ⭐ | Runtime par algorithm swap karo | `Func<>`, keyed DI |
| **Observer** ⭐ | Jab ek change ho to bahut ko notify karo | `event`, `IObservable<T>`, Rx |
| **Command** ⭐ | Action ko object mein badlo | MediatR, CQRS, undo/redo |
| Template Method | Fixed skeleton, fill-in steps | Base-class workflows |
| State | State ke saath behavior change hota hai | Stateless library, workflows |
| Chain of Responsibility | Request ko line ke down pass karo | ASP.NET middleware |
| Mediator | Central hub, no direct talk | MediatR |
| Iterator | Collection ke through step karo | `foreach`, `yield`, LINQ |
| Visitor | Classes edit kiye bina new operations | Often replaced by pattern matching |
| Memento | State save & restore karo | `record` snapshots + Command |
| Interpreter | Mini-language evaluate karo | Use a parser library instead |

**Pehle master karne waale teen:** Strategy, Observer, Command.

**Classic "same shape, different intent" pairs jo yaad rakhne hain:**
- Strategy (aap choose karte ho) vs State (object khud switch karta hai)
- Command (action as an object) vs Strategy (algorithm as an object)

➡️ Final lesson: **`05-modern-csharp-and-next-steps.md`** — kaise modern C# quietly inme se bahut sa aapke liye karta hai, plus aapka roadmap ek senior/tech lead ke roop mein.
