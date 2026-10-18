# LLD & Design Patterns — Ek Beginner's Course (C#)

👋 **Yeh page pehle padho.** Isme plain English mein explain kiya gaya hai ki yeh sab kya hai, kisi bhi pattern ko touch karne se pehle.

---

## "LLD" kya hai?

**LLD = Low-Level Design.**

Socho tum ek ghar bana rahe ho.

- **High-Level Design (HLD)** architect ka sketch hai: "kitchen yahan, do bathroom, garage left mein." Yeh *big picture* hai — kaunse major parts exist karte hain aur woh kaise connect hote hain.
- **Low-Level Design (LLD)** *ek room* ke liye detailed blueprint hai: har pipe kahan jaata hai, wiring kaise layout hoti hai, kaunsa switch kaunsi light control karta hai.

Software mein:
- HLD = "Humare paas ek web app hoga, ek database, aur ek payment service jo ek dusre se baat karenge."
- **LLD = Tum us services mein se ek ke andar actual C# classes ko kaise organize karte ho** taaki code padhna, change karna, aur test karna easy ho.

**Yeh course LLD ke baare mein hai:** classes aur objects ko achhe se arrange kaise karein.

---

## "Design Pattern" kya hai?

Design pattern ek **reusable solution hai us problem ka jo programmers ko baar-baar face karni padti hai.**

Analogy: Cooking mein, "sautéing" ek pattern hai. Koi bhi isko own nahi karta. Yeh sirf ek *named, proven technique* hai ek common situation ke liye ("mujhe vegetables ko quickly thodi fat aur high heat ke saath cook karna hai"). Ek baar "sauté" word pata chal gaya, ek chef aapko ek word mein poori technique bata sakta hai.

Design patterns same hote hain. Yeh **named, proven techniques** hain code arrange karne ke liye. Ek baar tum aur tumhare teammates naam jaan gaye, tum keh sakte ho *"chalo yahan ek Strategy use karte hain"* aur sab instantly plan samajh jaate hain.

> 🔑 **Key mindset:** Tum patterns memorize nahi karte ho taaki unhe har jagah sprinkle kar do. Tum unhe seekhte ho taaki tum *ek problem recognize kar sako* aur right tool reach kar sako. Pattern use karna jahan zarurat nahi hai, code ko *worse* bana deta hai. Hum yeh baat baar-baar repeat karte rahenge.

---

## Kya mujhe C# already pata hona chahiye?

Tumhe sirf **basics** pata hone chahiye: class kya hai, method kya hai, aur `new` se object kaise banate hain. Agar in mein se koi bhi shaky feel ho, **Lesson 1** slowly padho — yeh un object-oriented ideas ko scratch se re-teach karta hai jin par hum depend karte hain.

---

## In lessons ko padhne ka order

Jump around mat karo. Har lesson pichhle par build hota hai.

| # | File | Tum kya seekhoge | Difficulty |
|---|------|-------------------|-----------|
| 1 | `01-foundations.md` | OOP refresher + 5 SOLID rules (har pattern ke peeche ka "why") | 🟢 Start here |
| 2 | `02-creational-patterns.md` | Patterns jo *objects create karne* ke baare mein hain (Factory, Builder, Singleton…) | 🟢 Beginner |
| 3 | `03-structural-patterns.md` | Patterns jo *objects assemble karne* ke baare mein hain (Adapter, Decorator, Facade…) | 🟡 Beginner+ |
| 4 | `04-behavioral-patterns.md` | Patterns jo *object behavior & communication* ke baare mein hain (Strategy, Observer, Command…) | 🟡 Core |
| 5 | `05-modern-csharp-and-next-steps.md` | Modern C# kaise bahut se patterns tumhare liye kar deta hai + tech lead ke tor par aage kya seekhna hai | 🔵 Advanced |

---

## Har pattern kaise taught kiya jaata hai

Course mein har pattern **same 7 steps** follow karta hai, taaki tumhe hamesha pata rahe ki kya expect karna hai:

1. **🎯 One-line idea** — yeh kya karta hai, ek sentence mein.
2. **🌍 Real-world analogy** — everyday life se ek non-code example.
3. **😤 Problem (ek story)** — pattern *ke bina* messy code, taaki tumhe pain feel ho.
4. **✅ Solution** — same code pattern *ke saath*.
5. **🔍 Line-by-line walkthrough** — hum code ko saath mein slowly padhte hain.
6. **⚖️ Good & bad** — pros, cons, aur *kab use NAHI karna*.
7. **🧭 Tech-lead note** — tumhare senior role & code reviews ke liye kya matter karta hai.

---

## Scary words ki glossary (isko bookmark karo)

Tumhe yeh terms constantly milenge. Yahan plain English mein hain:

| Word | Plain-English meaning |
|------|----------------------|
| **Class** | Objects create karne ke liye ek blueprint. `class Car { }` |
| **Object / instance** | Blueprint se banaya gaya ek real thing. `new Car()` |
| **Interface** | Ek *contract* — methods ki ek list jo class promise karti hai rakhegi, andar koi code nahi. "Job description" jaise socho. |
| **Implement** | Interface ke contract ko fulfill karna actual method bodies likh kar. |
| **Method** | Ek function jo class ka hota hai (ek action jo object kar sakta hai). |
| **Abstraction** | Messy details ko ek simple front ke peeche hide karna. Car ka steering wheel engine ke upar ek abstraction hai. |
| **Concrete class** | Ek real, usable class (opposite interface/abstract ka). Tum ise `new` kar sakte ho. |
| **Dependency** | Kuch bhi jo class ko apna kaam karne ke liye chahiye (e.g., `PaymentService` ko `EmailSender` chahiye). |
| **Coupling** | Do pieces of code kitne tightly ek dusre se tied hain. **Loose coupling = good** (ek ko change karna easy, doosre ko break kiye bina). |
| **Inject / Dependency Injection (DI)** | Class khud us cheezon ko *create* karne ke bajaye jo usko chahiye, tum usko woh cheezein *hand* karte ho (usually constructor ke through). Hum yeh bahut dikhayenge. |
| **Instantiate** | "Ek object create karna" ke liye fancy word (`new Something()`). |
| **Polymorphism** | "Many forms." Different classes same method call ka apne apne tareeke se respond karte hain. Ek `Dog` aur `Cat` dono ka `MakeSound()` hota hai, lekin sound different hota hai. |
| **Inheritance** | Ek class doosri class ki features paati hai (`class Dog : Animal`). "Dog *is an* Animal." |
| **Composition** | Ek class ko doosre objects se banana jo yeh *hold* karta hai ("*has-a*"). Ek `Car` *has an* `Engine`. |

Iss table ko memorize karne ki koshish mat karo. Jab bhi koi word confuse kare, wapas yahan aa jaana.

---

## Do golden rules jo poore course mein carry karni hain

1. **"Program to an interface, not an implementation."**
   *Job description* (interface) par depend karo, ek specific *worker* (concrete class) par nahi. Isse workers swap karna painless ho jaata hai. Yeh tumhe Lesson 1 ke end tak fully samajh aa jayega.

2. **"Prefer composition over inheritance."**
   *Small parts ko combine* karke cheezein banana usually classes ke tall family trees banane se zyada flexible hota hai. Phir se — Lesson 1 isko click karayega.

Ready ho? **`01-foundations.md`** kholo. Slow lo. 🚀
