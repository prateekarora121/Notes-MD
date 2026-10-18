# LLD & Design Patterns — A Beginner's Course (C#)

👋 **Read this page first.** It explains what this whole thing is, in plain English, before you touch a single pattern.

---

## What is "LLD"?

**LLD = Low-Level Design.**

Imagine you're building a house.

- **High-Level Design (HLD)** is the architect's sketch: "kitchen here, two bathrooms, garage on the left." It's the *big picture* — which major parts exist and how they connect.
- **Low-Level Design (LLD)** is the detailed blueprint for *one room*: where each pipe goes, how the wiring is laid out, which switch controls which light.

In software:
- HLD = "We'll have a web app, a database, and a payment service that talk to each other."
- **LLD = How you organize the actual C# classes inside one of those services** so the code is easy to read, change, and test.

**This course is about LLD:** how to arrange classes and objects well.

---

## What is a "Design Pattern"?

A design pattern is a **reusable solution to a problem that programmers hit over and over again.**

Analogy: In cooking, "sautéing" is a pattern. Nobody owns it. It's just a *named, proven technique* for a common situation ("I need to cook vegetables quickly with a little fat and high heat"). Once you know the word "sauté," a chef can tell you a whole technique in one word.

Design patterns are the same. They are **named, proven techniques** for arranging code. Once you and your teammates know the names, you can say *"let's use a Strategy here"* and everyone instantly understands the plan.

> 🔑 **Key mindset:** You do NOT memorize patterns to sprinkle them everywhere. You learn them so you can *recognize a problem* and reach for the right tool. Using a pattern where it isn't needed makes code *worse*. We'll keep repeating this.

---

## Do I need to know C# already?

You need to know **just the basics**: what a class is, what a method is, and how to make an object with `new`. If any of these feel shaky, read **Lesson 1** slowly — it re-teaches the object-oriented ideas we rely on, from scratch.

---

## The order to read these lessons

Do NOT jump around. Each lesson builds on the last.

| # | File | What you'll learn | Difficulty |
|---|------|-------------------|-----------|
| 1 | `01-foundations.md` | OOP refresher + the 5 SOLID rules (the "why" behind every pattern) | 🟢 Start here |
| 2 | `02-creational-patterns.md` | Patterns about *creating* objects (Factory, Builder, Singleton…) | 🟢 Beginner |
| 3 | `03-structural-patterns.md` | Patterns about *assembling* objects (Adapter, Decorator, Facade…) | 🟡 Beginner+ |
| 4 | `04-behavioral-patterns.md` | Patterns about *object behavior & communication* (Strategy, Observer, Command…) | 🟡 Core |
| 5 | `05-modern-csharp-and-next-steps.md` | How modern C# does many patterns for you + what to learn next as a tech lead | 🔵 Advanced |

---

## How each pattern is taught

Every pattern in this course follows the **same 7 steps**, so you always know what to expect:

1. **🎯 The one-line idea** — what it does, in a sentence.
2. **🌍 Real-world analogy** — a non-code example from everyday life.
3. **😤 The problem (a story)** — messy code *without* the pattern, so you feel the pain.
4. **✅ The solution** — the same code *with* the pattern.
5. **🔍 Line-by-line walkthrough** — we read the code together, slowly.
6. **⚖️ Good & bad** — pros, cons, and *when NOT to use it*.
7. **🧭 Tech-lead note** — what matters for your senior role & code reviews.

---

## A glossary of scary words (bookmark this)

You'll meet these terms constantly. Here they are in plain English:

| Word | Plain-English meaning |
|------|----------------------|
| **Class** | A blueprint for creating objects. `class Car { }` |
| **Object / instance** | A real thing built from a blueprint. `new Car()` |
| **Interface** | A *contract* — a list of methods a class promises to have, with no code inside. Think "job description." |
| **Implement** | To fulfill an interface's contract by writing the actual method bodies. |
| **Method** | A function that belongs to a class (an action an object can do). |
| **Abstraction** | Hiding messy details behind a simple front. A car's steering wheel is an abstraction over the engine. |
| **Concrete class** | A real, usable class (opposite of an interface/abstract). You can `new` it. |
| **Dependency** | Anything a class needs to do its job (e.g., a `PaymentService` needs an `EmailSender`). |
| **Coupling** | How tightly two pieces of code are tied together. **Loose coupling = good** (easy to change one without breaking the other). |
| **Inject / Dependency Injection (DI)** | Instead of a class *creating* the things it needs, you *hand* them to it (usually through its constructor). We'll show this a lot. |
| **Instantiate** | A fancy word for "create an object" (`new Something()`). |
| **Polymorphism** | "Many forms." Different classes responding to the same method call in their own way. A `Dog` and a `Cat` both have `MakeSound()`, but the sound differs. |
| **Inheritance** | One class getting the features of another (`class Dog : Animal`). "Dog *is an* Animal." |
| **Composition** | Building a class out of other objects it *holds* ("*has-a*"). A `Car` *has an* `Engine`. |

Don't try to memorize this table. Just come back to it whenever a word confuses you.

---

## Two golden rules to carry through the whole course

1. **"Program to an interface, not an implementation."**
   Depend on the *job description* (interface), not a specific *worker* (concrete class). This makes swapping workers painless. You'll understand this fully by the end of Lesson 1.

2. **"Prefer composition over inheritance."**
   Building things by *combining small parts* is usually more flexible than building tall family trees of classes. Again — Lesson 1 makes this click.

Ready? Open **`01-foundations.md`**. Take it slow. 🚀
