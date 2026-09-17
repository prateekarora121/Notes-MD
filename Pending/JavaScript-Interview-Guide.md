# JavaScript Interview Guide — Beginner to Advanced

> A complete, code-first reference for interview prep. Each topic has: **explanation → working code → common interview questions**.

---

## Table of Contents
1. [Basics: Variables, Data Types, Coercion](#1-basics)
2. [Operators & Equality](#2-operators--equality)
3. [Functions Deep Dive](#3-functions-deep-dive)
4. [Scope, Hoisting & the `this` Keyword](#4-scope-hoisting--this)
5. [Closures](#5-closures)
6. [Objects & Prototypes](#6-objects--prototypes)
7. [Arrays & Iteration Methods](#7-arrays--iteration-methods)
8. [Destructuring, Spread & Rest](#8-destructuring-spread--rest)
9. [ES6+ Classes & OOP](#9-es6-classes--oop)
10. [Asynchronous JavaScript](#10-asynchronous-javascript)
11. [Event Loop Internals](#11-event-loop-internals)
12. [Error Handling](#12-error-handling)
13. [Modules](#13-modules)
14. [Advanced Patterns (Currying, Debounce, Throttle, Memoization)](#14-advanced-patterns)
15. [Design Patterns in JS](#15-design-patterns)
16. [Memory Management & Performance](#16-memory-management--performance)
17. [DOM & Browser Concepts](#17-dom--browser-concepts)
18. [Senior-Level / System-Design-Adjacent Questions](#18-senior-level-questions)

---

## 1. Basics

### Variables: `var`, `let`, `const`
- `var` — function-scoped, hoisted and initialized with `undefined`, can be redeclared.
- `let` — block-scoped, hoisted but **not** initialized (Temporal Dead Zone), cannot be redeclared in same scope.
- `const` — block-scoped, must be initialized at declaration, binding is immutable (but object/array **contents** are still mutable).

```javascript
function varTest() {
  if (true) {
    var x = 1; // function-scoped
  }
  console.log(x); // 1 — accessible outside the if block
}

function letTest() {
  if (true) {
    let y = 1; // block-scoped
  }
  console.log(y); // ReferenceError: y is not defined
}

const arr = [1, 2, 3];
arr.push(4);      // OK — mutating contents
console.log(arr); // [1,2,3,4]
arr = [5, 6];      // TypeError: Assignment to constant variable.
```

### Data Types
JavaScript has 7 primitive types + `object`:
`string`, `number`, `bigint`, `boolean`, `undefined`, `symbol`, `null`, and `object` (includes arrays, functions, dates).

```javascript
typeof "hello"     // "string"
typeof 42          // "number"
typeof 42n         // "bigint"
typeof true        // "boolean"
typeof undefined   // "undefined"
typeof Symbol()    // "symbol"
typeof null        // "object"  <-- famous JS bug, kept for backward compatibility
typeof {}          // "object"
typeof []          // "object"
typeof function(){} // "function"
```

### Type Coercion
JS is dynamically and weakly typed — it converts types implicitly in many operations.

```javascript
console.log(1 + "1");     // "11"  (number -> string)
console.log("5" - 1);     // 4     (string -> number)
console.log("5" + 1);     // "51"  (+ prefers string concatenation)
console.log(true + true); // 2     (boolean -> number)
console.log([] + []);     // ""    (arrays -> strings, concatenated)
console.log([] + {});     // "[object Object]"
console.log(1 == "1");    // true  (coercion happens with ==)
console.log(1 === "1");   // false (no coercion with ===)
```

**Interview Questions — Basics**
1. What's the difference between `var`, `let`, and `const`? *(scope, hoisting, redeclaration, TDZ)*
2. Why does `typeof null === "object"`? *(legacy bug from JS's original implementation, kept for compatibility)*
3. What is the Temporal Dead Zone? *(the period between entering scope and the `let`/`const` declaration being evaluated, where accessing the variable throws)*
4. Is a `const` array/object truly immutable? *(no — only the binding is frozen; use `Object.freeze()` for shallow immutability)*
5. Explain implicit type coercion with 2–3 tricky examples (`[] + []`, `[] == false`, `null == undefined`). *(`[] + []` becomes an empty string because arrays convert to strings; `[] == false` is true after both sides are coerced to zero; `null == undefined` is true only with loose equality.)*

---

## 2. Operators & Equality

```javascript
// == does type coercion, === does NOT
console.log(0 == false);   // true
console.log(0 === false);  // false
console.log(null == undefined); // true
console.log(null === undefined); // false
console.log(NaN == NaN);   // false — NaN is never equal to anything, even itself
console.log(Object.is(NaN, NaN)); // true — Object.is handles this edge case

// Optional chaining & nullish coalescing (ES2020)
const user = { profile: { name: "Asha" } };
console.log(user?.profile?.age);        // undefined (no error even though age doesn't exist)
console.log(user?.address?.city);       // undefined (no error even though address doesn't exist)
console.log(user.profile.age ?? "N/A"); // "N/A" — nullish coalescing (only null/undefined trigger fallback)
console.log(0 || "default");            // "default" — falsy trap
console.log(0 ?? "default");            // 0 — nullish coalescing doesn't treat 0 as nullish
```

**Interview Questions — Operators**
1. Difference between `==` and `===`? When would you intentionally use `==`? *(checking for `null`/`undefined` together: `x == null`)*
2. Why is `NaN === NaN` false? How do you correctly check for `NaN`? *(`Number.isNaN()` or `Object.is`)*
3. Difference between `??` and `||`? *(`??` only falls back on `null`/`undefined`; `||` falls back on any falsy value like `0`, `""`, `false`)*
4. What does optional chaining (`?.`) do and how does it short-circuit? *(It returns `undefined` instead of throwing when the value immediately before `?.` is `null` or `undefined`; the rest of that property/call chain is skipped.)*

---

## 3. Functions Deep Dive

### Function Declarations vs Expressions vs Arrow Functions

```javascript
// Function declaration — hoisted fully (can call before definition)
console.log(add(2, 3)); // 5
function add(a, b) { return a + b; }

// Function expression — NOT hoisted (only variable is hoisted, not assignment)
console.log(sub(5, 2)); // TypeError: sub is not a function
var sub = function (a, b) { return a - b; };

// Arrow function — concise, no own `this`, no `arguments`, cannot be used as constructor
const mul = (a, b) => a * b;
console.log(mul(2, 3)); // 6
```

### Key differences: Arrow vs Regular Functions
| Feature | Regular Function | Arrow Function |
|---|---|---|
| `this` binding | dynamic (depends on call site) | lexical (inherited from enclosing scope) |
| `arguments` object | yes | no |
| Can be used as constructor (`new`) | yes | no |
| `prototype` property | yes | no |

```javascript
const obj = {
  name: "Regular",
  regularFn: function () {
    console.log(this.name); // "Regular" — `this` = obj
  },
  arrowFn: () => {
    console.log(this.name); // undefined — `this` = enclosing (module/global) scope
  }
};
obj.regularFn();
obj.arrowFn();
```

### Higher-Order Functions
A function that takes another function as an argument or returns a function.

```javascript
function withLogging(fn) {
  return function (...args) {
    console.log(`Calling with args: ${args}`);
    return fn(...args);
  };
}
const loggedAdd = withLogging((a, b) => a + b);
loggedAdd(2, 3); // logs "Calling with args: 2,3", returns 5
```

### IIFE (Immediately Invoked Function Expression)

```javascript
(function () {
  console.log("Runs immediately, creates its own scope");
})();

// Used historically to avoid polluting global scope, now largely replaced by modules
```

### `call`, `apply`, `bind`

```javascript
function greet(greeting) {
  console.log(`${greeting}, ${this.name}`);
}
const person = { name: "Ravi" };

greet.call(person, "Hello");         // Hello, Ravi   (args passed individually)
greet.apply(person, ["Hi"]);         // Hi, Ravi      (args passed as array)
const boundGreet = greet.bind(person);
boundGreet("Hey");                   // Hey, Ravi     (returns new function, `this` permanently bound)
```

**Interview Questions — Functions**
1. Difference between function declarations and function expressions in terms of hoisting? *(Function declarations are hoisted with their implementation and can be called before the declaration. Function expressions are assigned only when execution reaches the assignment; `let`/`const` expressions are in the TDZ and `var` expressions are initially `undefined`.)*
2. Why can't arrow functions be used as constructors or have their own `this`? *(Arrow functions do not have `[[Construct]]`, so `new` cannot use them. They also capture `this` lexically from the surrounding scope instead of creating a call-time binding.)*
3. Explain `call`, `apply`, and `bind` with an example. When would you use each? *(`call` invokes immediately with individual arguments, `apply` invokes immediately with an array-like argument list, and `bind` returns a new function with `this` and optional arguments fixed. Use them to control context or partially apply a function.)*
4. What is a higher-order function? Give 2 real examples (`map`, `debounce`). *(A function that accepts another function, returns one, or both. `map` accepts a transformation callback; `debounce` returns a wrapper that delays the supplied callback.)*
5. What is an IIFE and why was it used before ES6 modules? *(An Immediately Invoked Function Expression runs as soon as it is defined. It created a private function scope and avoided global-variable collisions before module systems were widely available.)*
6. How do default parameters work, and can they reference earlier parameters? *(A default is evaluated when the argument is `undefined`, including when the argument is omitted. It can reference earlier parameters, but not later parameters or variables in the function body.)*
```javascript
function greet(name, greeting = `Hello, ${name}`) { console.log(greeting); }
greet("Sam"); // "Hello, Sam"
```

---

## 4. Scope, Hoisting & `this`

### Hoisting
Declarations are moved to the top of their scope during compilation — but only the *declaration*, not the *initialization*.

```javascript
console.log(a); // undefined (var is hoisted & initialized to undefined)
var a = 5;

console.log(b); // ReferenceError (TDZ — let is hoisted but not initialized)
let b = 10;

console.log(sayHi()); // Works! Function declarations are fully hoisted
function sayHi() { return "hi"; }
```

### Scope Chain
When a variable is accessed, JS looks in the current scope, then outward through enclosing scopes, until it reaches global scope.

```javascript
let global = "I am global";
function outer() {
  let outerVar = "I am outer";
  function inner() {
    let innerVar = "I am inner";
    console.log(innerVar, outerVar, global); // all accessible
  }
  inner();
}
outer();
```

### The `this` Keyword — Determined by HOW a function is called, not where it's defined
```javascript
// 1. Simple call — `this` = undefined (strict mode) or global object (non-strict)
function standalone() { console.log(this); }
standalone();

// 2. Method call — `this` = the object before the dot
const obj = { greet() { console.log(this); } };
obj.greet(); // this = obj

// 3. Constructor call — `this` = newly created object
function Person(name) { this.name = name; }
const p = new Person("Alex"); // this = new object

// 4. Explicit binding — call/apply/bind
function show() { console.log(this.value); }
show.call({ value: 42 }); // this = { value: 42 }

// 5. Arrow functions — `this` = lexical (from enclosing scope), ignores all the above rules
const arrowObj = {
  value: 10,
  regular() {
    setTimeout(function () { console.log(this.value); }, 0); // undefined — `this` = global/undefined
  },
  arrow() {
    setTimeout(() => { console.log(this.value); }, 0); // 10 — `this` inherited from arrow()
  }
};
```

**Interview Questions — Scope & `this`**
1. Explain hoisting for `var`, `let`, `const`, and function declarations vs expressions. *(`var` declarations are initialized to `undefined`; `let` and `const` are hoisted but remain uninitialized in the TDZ; function declarations are fully hoisted; function expressions follow the hoisting behavior of their variable.)*
2. What is the scope chain and how does lexical scoping work? *(Each function/block records where it was written. An identifier is resolved from the current scope outward through those enclosing scopes, regardless of where the function is eventually called.)*
3. How is `this` determined in JavaScript? Walk through the 4-5 binding rules. *(In priority order: `new` binds the new instance; `call`/`apply`/`bind` explicitly bind it; a method call binds it to the receiver; a plain call uses `undefined` in strict mode or the global object otherwise; arrow functions inherit it lexically.)*
4. Classic trap: why does `this` become `undefined` inside a regular callback passed to `setTimeout`, and how do you fix it? *(use arrow function, or `.bind(this)`)*
5. What's the difference between lexical scope and dynamic scope? *(JS uses lexical scope for variables, but `this` behaves more dynamically)*

---

## 5. Closures

A closure is formed when a function "remembers" the variables from its lexical scope even after the outer function has returned.

```javascript
function makeCounter() {
  let count = 0; // private variable, only accessible via the closure
  return function () {
    count++;
    return count;
  };
}
const counter = makeCounter();
console.log(counter()); // 1
console.log(counter()); // 2
console.log(counter()); // 3
// `count` is NOT accessible directly — it's fully encapsulated
```

### Classic Closure Interview Trap — Loop + `var`

```javascript
// BROKEN with var
for (var i = 1; i <= 3; i++) {
  setTimeout(() => console.log(i), 100); // logs 4, 4, 4 (var is function-scoped, shared across iterations)
}

// FIXED with let
for (let i = 1; i <= 3; i++) {
  setTimeout(() => console.log(i), 100); // logs 1, 2, 3 (let creates a new binding per iteration)
}

// FIXED with var + IIFE (the pre-ES6 solution)
for (var i = 1; i <= 3; i++) {
  (function (captured) {
    setTimeout(() => console.log(captured), 100);
  })(i);
}
```

### Practical Use: Module Pattern / Data Privacy

```javascript
function createBankAccount(initialBalance) {
  let balance = initialBalance; // private
  return {
    deposit(amount) { balance += amount; return balance; },
    withdraw(amount) {
      if (amount > balance) throw new Error("Insufficient funds");
      balance -= amount;
      return balance;
    },
    getBalance() { return balance; }
  };
}
const account = createBankAccount(100);
account.deposit(50);
console.log(account.getBalance()); // 150
console.log(account.balance);      // undefined — truly private
```

**Interview Questions — Closures**
1. What is a closure? Give a real-world use case. *(A closure is a function plus references to variables from its lexical scope. A counter, factory, event handler, or module can use it to preserve private state after the outer function returns.)*
2. Why does the classic `var` in a `for` loop + `setTimeout` example print the same number every time, and how do you fix it 2 different ways? *(`var` creates one function-scoped binding, so callbacks read its final value. Use `let` for a per-iteration binding or capture the value with an IIFE/factory.)*
3. How would you use closures to implement data privacy / private variables in JS (before `#privateFields` existed)? *(Declare the state inside a factory function and return only methods that close over it. Callers can use the methods but cannot access the state directly.)*
4. What are potential downsides of closures? *(memory retention — variables captured by a closure aren't garbage collected while the closure is alive, can cause memory leaks if not careful)*
5. Implement a function `once(fn)` that ensures `fn` only runs once, using closures.
```javascript
function once(fn) {
  let called = false, result;
  return function (...args) {
    if (!called) { result = fn(...args); called = true; }
    return result;
  };
}
```

---

## 6. Objects & Prototypes

### Prototypal Inheritance
Every JS object has an internal link `[[Prototype]]` to another object, accessible via `Object.getPrototypeOf()` or the deprecated `__proto__`. Property lookups walk up this **prototype chain**.

```javascript
const animal = {
  eat() { console.log(`${this.name} is eating`); }
};
const dog = Object.create(animal); // dog's prototype = animal
dog.name = "Rex";
dog.eat(); // "Rex is eating" — eat() found via prototype chain

console.log(dog.hasOwnProperty("name")); // true
console.log(dog.hasOwnProperty("eat"));  // false — it's inherited
```

### Constructor Functions & `prototype`

```javascript
function Person(name) {
  this.name = name;
}
Person.prototype.greet = function () {
  console.log(`Hi, I'm ${this.name}`);
};
const p1 = new Person("Meera");
const p2 = new Person("Karan");
p1.greet(); // "Hi, I'm Meera"
// greet is shared on the prototype — NOT duplicated per instance (memory efficient)
console.log(p1.greet === p2.greet); // true
```

### `new` keyword — what it actually does
```javascript
function myNew(Constructor, ...args) {
  const obj = Object.create(Constructor.prototype); // 1. create new object linked to prototype
  const result = Constructor.apply(obj, args);        // 2. call constructor with `this` = obj
  return result instanceof Object ? result : obj;      // 3. return obj unless constructor returns its own object
}
```

### Object utility methods

```javascript
const obj1 = { a: 1, b: 2 };
const obj2 = Object.assign({}, obj1, { c: 3 }); // shallow clone + merge
console.log(obj2); // { a: 1, b: 2, c: 3 }

const frozen = Object.freeze({ x: 1 });
frozen.x = 99; // silently fails (throws in strict mode)
console.log(frozen.x); // 1

console.log(Object.keys(obj1));    // ['a', 'b']
console.log(Object.values(obj1));  // [1, 2]
console.log(Object.entries(obj1)); // [['a',1],['b',2]]

// Deep clone (modern way)
const deep = structuredClone({ nested: { a: 1 } });
```

**Interview Questions — Objects & Prototypes**
1. What is the prototype chain? How does property lookup work? *(JavaScript first checks an object's own properties, then follows `[[Prototype]]` links until it finds the property or reaches `null`. Methods placed on a prototype are therefore shared by instances.)*
2. Difference between `Object.create()`, constructor functions, and ES6 `class` for inheritance? *(`Object.create(proto)` directly creates an object with the chosen prototype; constructor functions combine initialization with a `.prototype`; `class` provides clearer syntax for constructor/prototype behavior and inheritance but still uses prototypes.)*
3. What does the `new` keyword actually do internally? (4 steps) *(It creates an object, links it to the constructor's prototype, calls the constructor with that object as `this`, and returns the constructor's object result when one is explicitly returned; otherwise it returns the new object.)*
4. Difference between `Object.freeze()`, `Object.seal()`, and `const`? *(freeze = no add/remove/modify props; seal = no add/remove but can modify existing; const = can't reassign variable)*
5. How do you deep clone an object? *(`structuredClone()`, `JSON.parse(JSON.stringify())` — with caveats on functions/undefined/circular refs — or a recursive/library approach)*
6. `__proto__` vs `prototype` — what's the difference? *(`prototype` is a property on constructor functions; `__proto__` is the actual link on an instance to its prototype)*

---

## 7. Arrays & Iteration Methods

```javascript
const nums = [1, 2, 3, 4, 5];

// map — transforms each element, returns new array
console.log(nums.map(n => n * 2)); // [2,4,6,8,10]

// filter — keeps elements matching a condition
console.log(nums.filter(n => n % 2 === 0)); // [2,4]

// reduce — accumulates into a single value
console.log(nums.reduce((acc, n) => acc + n, 0)); // 15

// find / findIndex
console.log(nums.find(n => n > 3));      // 4
console.log(nums.findIndex(n => n > 3)); // 3

// some / every
console.log(nums.some(n => n > 4));  // true
console.log(nums.every(n => n > 0)); // true

// forEach — no return value, just iterates
nums.forEach(n => console.log(n));

// flat / flatMap
console.log([1, [2, [3, [4]]]].flat(2)); // [1,2,3,[4]]
console.log([1, 2, 3].flatMap(n => [n, n * 2])); // [1,2,2,4,3,6]

// Sorting — mutates original array, default sort is by STRING (common trap!)
console.log([10, 1, 21, 2].sort());              // [1, 10, 2, 21]  <-- WRONG for numbers!
console.log([10, 1, 21, 2].sort((a, b) => a - b)); // [1, 2, 10, 21] <-- correct numeric sort
```

### Implementing `reduce`, `map`, `filter` from scratch (common senior-level question)

```javascript
Array.prototype.myMap = function (callback) {
  const result = [];
  for (let i = 0; i < this.length; i++) {
    result.push(callback(this[i], i, this));
  }
  return result;
};

Array.prototype.myReduce = function (callback, initialValue) {
  let acc = initialValue;
  let startIdx = 0;
  if (acc === undefined) { acc = this[0]; startIdx = 1; }
  for (let i = startIdx; i < this.length; i++) {
    acc = callback(acc, this[i], i, this);
  }
  return acc;
};

console.log([1, 2, 3].myMap(x => x * 2));      // [2,4,6]
console.log([1, 2, 3].myReduce((a, b) => a + b, 0)); // 6
```

**Interview Questions — Arrays**
1. Difference between `map` and `forEach`? *(`map` returns a new array, `forEach` returns `undefined`)*
2. How does `reduce` work? Implement `map` using `reduce`.
```javascript
const mapWithReduce = (arr, fn) => arr.reduce((acc, el) => [...acc, fn(el)], []);
```
3. Why does `[10, 1, 21].sort()` NOT sort numerically by default, and how do you fix it? *(Without a comparator, elements are converted to strings and compared lexicographically. Use `.sort((a, b) => a - b)` for ascending numbers or `.sort((a, b) => b - a)` for descending numbers.)*
4. Difference between `slice` and `splice`? *(`slice` is non-mutating, returns a copy; `splice` mutates the original array)*
5. How do you remove duplicates from an array? *(`[...new Set(arr)]`)*
6. Implement your own `Array.prototype.flat`. *(Use recursion and a depth parameter: iterate each element, recursively flatten arrays while `depth > 0`, and append non-arrays. A production implementation should also validate `this` and preserve sparse-array semantics.)*

---

## 8. Destructuring, Spread & Rest

```javascript
// Array destructuring
const [first, second, ...rest] = [1, 2, 3, 4, 5];
console.log(first, second, rest); // 1 2 [3,4,5]

// Object destructuring with renaming & defaults
const { name: userName = "Guest", age = 18 } = { name: "Zara" };
console.log(userName, age); // "Zara" 18

// Nested destructuring
const { address: { city } } = { address: { city: "Delhi" } };
console.log(city); // "Delhi"

// Swapping variables
let a = 1, b = 2;
[a, b] = [b, a];
console.log(a, b); // 2 1

// Spread — expands an iterable
const arr1 = [1, 2];
const arr2 = [...arr1, 3, 4]; // [1,2,3,4]
const merged = { ...{ a: 1 }, ...{ b: 2 } }; // { a: 1, b: 2 }

// Rest — collects remaining args into an array
function sum(...numbers) {
  return numbers.reduce((a, b) => a + b, 0);
}
console.log(sum(1, 2, 3, 4)); // 10
```

**Interview Questions — Destructuring/Spread/Rest**
1. Difference between rest and spread operators? *(rest collects values into an array/object; spread expands them)*
2. How do you swap two variables without a temp variable using destructuring? *(`[a, b] = [b, a]` evaluates the right side first, then assigns both values.)*
3. How does spread create a shallow copy, and what's the gotcha with nested objects? *(Spread copies only top-level properties. Nested objects and arrays remain shared references, so mutating a nested value changes the original; use `structuredClone` or another deliberate deep-copy strategy when needed.)*
```javascript
const original = { a: 1, nested: { b: 2 } };
const copy = { ...original };
copy.nested.b = 99;
console.log(original.nested.b); // 99 — nested objects are still shared references!
```

---

## 9. ES6+ Classes & OOP

```javascript
class Animal {
  #secretId; // private field (ES2022)
  static count = 0; // static property

  constructor(name) {
    this.name = name;
    this.#secretId = Math.random();
    Animal.count++;
  }

  speak() { // instance method — shared via prototype
    console.log(`${this.name} makes a sound`);
  }

  static getCount() { // static method — called on class, not instance
    return Animal.count;
  }

  get id() { // getter
    return this.#secretId;
  }
}

class Dog extends Animal {
  speak() {
    super.speak(); // call parent method
    console.log(`${this.name} barks`);
  }
}

const d = new Dog("Rex");
d.speak();
// "Rex makes a sound"
// "Rex barks"
console.log(Animal.getCount()); // 1
console.log(d.id); // some random number
console.log(d.#secretId); // SyntaxError — truly private, not just convention
```

**Key facts:**
- `class` is syntactic sugar over prototypal inheritance — under the hood it still uses prototypes.
- Class declarations are NOT hoisted the way function declarations are (they're in the TDZ).
- `#field` (ES2022) provides true private fields, unlike the old `_field` naming convention which is just a hint.

**Interview Questions — Classes**
1. Is JavaScript's `class` truly a new OOP paradigm, or syntactic sugar? *(sugar over prototype-based inheritance)*
2. Difference between static and instance methods/properties? *(Static members belong to the class itself and are called through `ClassName`; instance members belong to each object and are called through an instance. Static members cannot directly access instance state.)*
3. How does `super` work in inheritance? What happens if you forget `super()` in a subclass constructor? *(ReferenceError: must call super before accessing `this`)*
4. How do private class fields (`#field`) differ from just naming a property `_field`? *(true privacy — inaccessible and non-enumerable outside the class, enforced by the engine)*
5. Explain the difference between classical (Java/C++) inheritance and prototypal inheritance. *(Classical inheritance derives instances from classes and fixed type hierarchies. Prototypal inheritance delegates property lookup from one object to another; JavaScript classes are syntax over that prototype model.)*

---

## 10. Asynchronous JavaScript

### Callbacks → Promises → Async/Await evolution

```javascript
// 1. Callback style (can lead to "callback hell")
function fetchDataCallback(callback) {
  setTimeout(() => callback(null, "data"), 1000);
}
fetchDataCallback((err, data) => {
  if (err) return console.error(err);
  console.log(data);
});

// 2. Promises
function fetchDataPromise() {
  return new Promise((resolve, reject) => {
    setTimeout(() => {
      const success = true;
      success ? resolve("data") : reject(new Error("failed"));
    }, 1000);
  });
}
fetchDataPromise()
  .then(data => console.log(data))
  .catch(err => console.error(err))
  .finally(() => console.log("done"));

// 3. Async/Await (syntactic sugar over promises)
async function getData() {
  try {
    const data = await fetchDataPromise();
    console.log(data);
  } catch (err) {
    console.error(err);
  } finally {
    console.log("done");
  }
}
getData();
```

### Promise Combinators

```javascript
const p1 = Promise.resolve(1);
const p2 = new Promise(res => setTimeout(() => res(2), 100));
const p3 = Promise.reject(new Error("fail"));

// Promise.all — waits for ALL, rejects fast if ANY rejects
Promise.all([p1, p2]).then(console.log); // [1, 2]

// Promise.allSettled — waits for ALL, never rejects, gives status of each
Promise.allSettled([p1, p3]).then(console.log);
// [{status:'fulfilled', value:1}, {status:'rejected', reason: Error}]

// Promise.race — resolves/rejects as soon as the FIRST settles
Promise.race([p1, p2]).then(console.log); // 1

// Promise.any — resolves as soon as the FIRST fulfills, ignores rejections unless all reject
Promise.any([p3, p2]).then(console.log); // 2
```

### Building a Promise from scratch (common senior question)

```javascript
class MyPromise {
  constructor(executor) {
    this.state = "pending";
    this.value = undefined;
    this.callbacks = [];

    const resolve = (value) => {
      if (this.state !== "pending") return;
      this.state = "fulfilled";
      this.value = value;
      this.callbacks.forEach(cb => cb.onFulfilled(value));
    };
    const reject = (reason) => {
      if (this.state !== "pending") return;
      this.state = "rejected";
      this.value = reason;
      this.callbacks.forEach(cb => cb.onRejected(reason));
    };
    try { executor(resolve, reject); } catch (e) { reject(e); }
  }

  then(onFulfilled, onRejected) {
    return new MyPromise((resolve, reject) => {
      const handle = () => {
        try {
          if (this.state === "fulfilled") resolve(onFulfilled ? onFulfilled(this.value) : this.value);
          if (this.state === "rejected") reject(onRejected ? onRejected(this.value) : this.value);
        } catch (e) { reject(e); }
      };
      if (this.state === "pending") {
        this.callbacks.push({ onFulfilled: handle, onRejected: handle });
      } else {
        handle();
      }
    });
  }
}
```

**Interview Questions — Async JS**
1. Explain the evolution: callbacks → promises → async/await. What problem does each solve? *(Callbacks enabled completion handlers but became difficult to compose and error-handle when nested. Promises represent a future result and provide chaining/composition. `async`/`await` gives promise-based code a readable sequential style while preserving asynchronous behavior.)*
2. Difference between `Promise.all`, `Promise.allSettled`, `Promise.race`, `Promise.any`? *(`all` fulfills when all fulfill and rejects on the first rejection; `allSettled` waits for every result; `race` settles on the first fulfillment or rejection; `any` fulfills on the first fulfillment and rejects only when all reject.)*
3. What happens if you don't handle a rejected promise? *(unhandled promise rejection — may crash Node process or log a console warning in browsers)*
4. How does `async/await` handle errors? Why do we still need `try/catch`? *(A rejected promise awaited inside an async function throws at the `await` expression. `try/catch` lets the function recover, translate, log, or rethrow the error; without it, the async function returns a rejected promise to its caller.)*
5. Is `await` blocking? *(No — it pauses the async function but doesn't block the main thread/event loop; other code keeps running)*
6. What's the output? *(The output is `1`, `4`, `3`, `2`: synchronous logs run first, promise callbacks run as microtasks, and the timer runs as a macrotask.)*
```javascript
console.log("1");
setTimeout(() => console.log("2"), 0);
Promise.resolve().then(() => console.log("3"));
console.log("4");
// Output: 1, 4, 3, 2  — sync code first, then microtasks (promises), then macrotasks (setTimeout)
```

---

## 11. Event Loop Internals

The event loop is what allows JS (single-threaded) to handle async operations without blocking.

**Components:**
- **Call Stack** — where synchronous code executes, LIFO.
- **Web APIs / Node APIs** — browser/Node provides these for async ops (`setTimeout`, DOM events, `fetch`).
- **Microtask Queue** — Promises, `queueMicrotask`, `MutationObserver` callbacks. **Higher priority.**
- **Macrotask (Callback/Task) Queue** — `setTimeout`, `setInterval`, I/O, UI rendering.

**Rule: after every single macrotask, the engine drains the ENTIRE microtask queue before picking the next macrotask (or rendering).**

```javascript
console.log("start");                                  // 1. sync

setTimeout(() => console.log("timeout"), 0);            // 5. macrotask

Promise.resolve()
  .then(() => console.log("promise 1"))                 // 3. microtask
  .then(() => console.log("promise 2"));                 // 4. microtask (chained)

console.log("end");                                     // 2. sync

// Output order: start, end, promise 1, promise 2, timeout
```

### Visual Mental Model
```
Call Stack (runs sync code)
     │
     ▼
Microtask Queue (Promises) ──┐   drained COMPLETELY before next macrotask
     │                       │
     ▼                       │
Macrotask Queue (setTimeout, I/O) ◄
```

**Interview Questions — Event Loop**
1. Explain the JS event loop: call stack, microtask queue, macrotask queue. *(Synchronous code runs on the call stack. Completed async callbacks wait in queues; after the current stack clears, the runtime drains microtasks before taking the next macrotask, repeating this cycle.)*
2. Why do Promises run before `setTimeout(fn, 0)`? *(Promise reactions are microtasks, while timers enqueue macrotasks. The event loop drains microtasks after synchronous code and before the next timer task.)*
3. Trace through the output of a mixed sync/async/promise/setTimeout code snippet (very common — practice several). *(First record synchronous logs, then process promise/`queueMicrotask` callbacks in FIFO order, including newly queued microtasks, and only then process timer or I/O callbacks.)*
4. Is JavaScript single-threaded? How does it handle async operations then? *(single-threaded execution, but the runtime environment — browser/Node — provides multi-threaded APIs behind the scenes for I/O, timers, etc.)*
5. What's the difference between microtasks and macrotasks, and name 2 examples of each? *(Microtasks are drained before the next macrotask; examples are Promise reactions and `queueMicrotask`. Macrotasks are scheduled tasks such as `setTimeout` callbacks and I/O callbacks.)*

---

## 12. Error Handling

```javascript
// Basic try/catch/finally
try {
  JSON.parse("{invalid json}");
} catch (err) {
  console.error(err.message); // "Unexpected token i in JSON..."
} finally {
  console.log("Always runs");
}

// Custom Error classes
class ValidationError extends Error {
  constructor(message, field) {
    super(message);
    this.name = "ValidationError";
    this.field = field;
  }
}

function validateAge(age) {
  if (age < 0) throw new ValidationError("Age cannot be negative", "age");
  return age;
}

try {
  validateAge(-5);
} catch (err) {
  if (err instanceof ValidationError) {
    console.log(`Validation failed on field: ${err.field}`);
  } else {
    throw err; // re-throw unknown errors
  }
}

// Error handling in async code
async function riskyOperation() {
  try {
    const res = await fetch("/api/data");
    if (!res.ok) throw new Error(`HTTP ${res.status}`);
    return await res.json();
  } catch (err) {
    console.error("Failed:", err.message);
    throw err; // propagate to caller
  }
}

// Global error handlers
window.addEventListener("error", (e) => console.log("Global error:", e.message));
window.addEventListener("unhandledrejection", (e) => console.log("Unhandled promise:", e.reason));
```

**Interview Questions — Error Handling**
1. How do you create custom error types, and why would you? *(Extend `Error`, call `super(message)`, set a meaningful `name`, and add structured fields such as a validation key. Custom types let callers distinguish expected domain failures from programming or infrastructure errors.)*
2. What's the difference between `throw` inside a `try` block vs inside a `.catch()`? *(Either throw transfers control to the nearest matching handler. A throw in `try` enters that try's `catch`; a throw in `catch` replaces the handled failure and propagates outward unless another surrounding handler catches it.)*
3. How do you catch errors in async/await code globally? *(`unhandledrejection` event, or wrapping in try/catch)*
4. What happens to code in the `finally` block if both `try` and `catch` have `return` statements? *(`finally` always executes, and if it has its own `return`, it overrides earlier returns)*

---

## 13. Modules

```javascript
// math.js — named exports
export const PI = 3.14159;
export function add(a, b) { return a + b; }
export default function multiply(a, b) { return a * b; } // default export

// app.js — importing
import multiply, { PI, add } from './math.js';
import * as MathUtils from './math.js'; // namespace import

// Dynamic import (code-splitting, lazy loading)
async function loadModule() {
  const module = await import('./math.js');
  console.log(module.add(2, 3));
}
```

**CommonJS (Node.js legacy) vs ES Modules**
```javascript
// CommonJS
const { add } = require('./math'); // synchronous
module.exports = { add };

// ES Modules
import { add } from './math.js';   // asynchronous, static analysis possible
export { add };
```

**Interview Questions — Modules**
1. Difference between CommonJS (`require`) and ES Modules (`import/export`)? *(CommonJS is traditionally synchronous, runtime-loaded, and uses `module.exports`/`require`. ESM has statically analyzable `import`/`export`, live bindings, top-level `await` support, and is the standard browser/module format; Node supports both with configuration.)*
2. What is tree-shaking, and why does it work with ES Modules but not CommonJS? *(ESM has static, analyzable imports/exports, so bundlers can remove unused code; CommonJS imports are dynamic/runtime, harder to statically analyze)*
3. Named vs default exports — when would you use each? *(Named exports make a module's public API explicit and allow multiple exports with stable names. A default export represents the module's primary value but permits arbitrary import naming; prefer named exports when a module exposes several related APIs.)*
4. What is dynamic `import()` used for? *(lazy loading / code splitting)*

---

## 14. Advanced Patterns

### Debounce — delay execution until after a pause in calls (e.g., search-as-you-type)

```javascript
function debounce(fn, delay) {
  let timeoutId;
  return function (...args) {
    clearTimeout(timeoutId);
    timeoutId = setTimeout(() => fn.apply(this, args), delay);
  };
}
const debouncedSearch = debounce((query) => console.log("Searching:", query), 300);
// rapid calls -> only the LAST call after 300ms of silence actually executes
```

### Throttle — ensure a function runs at most once per interval (e.g., scroll handler)

```javascript
function throttle(fn, limit) {
  let inThrottle = false;
  return function (...args) {
    if (!inThrottle) {
      fn.apply(this, args);
      inThrottle = true;
      setTimeout(() => (inThrottle = false), limit);
    }
  };
}
const throttledScroll = throttle(() => console.log("scroll event"), 200);
```

### Currying — transforming a function to take arguments one at a time

```javascript
function curry(fn) {
  return function curried(...args) {
    if (args.length >= fn.length) return fn.apply(this, args);
    return (...args2) => curried.apply(this, [...args, ...args2]);
  };
}
function add3(a, b, c) { return a + b + c; }
const curriedAdd = curry(add3);
console.log(curriedAdd(1)(2)(3));    // 6
console.log(curriedAdd(1, 2)(3));    // 6
console.log(curriedAdd(1, 2, 3));    // 6
```

### Memoization — cache expensive function results

```javascript
function memoize(fn) {
  const cache = new Map();
  return function (...args) {
    const key = JSON.stringify(args);
    if (cache.has(key)) return cache.get(key);
    const result = fn.apply(this, args);
    cache.set(key, result);
    return result;
  };
}
const slowSquare = (n) => { for (let i = 0; i < 1e8; i++); return n * n; };
const fastSquare = memoize(slowSquare);
fastSquare(5); // slow first time
fastSquare(5); // instant — cached
```

### Deep Clone (from scratch)

```javascript
function deepClone(obj, seen = new WeakMap()) {
  if (obj === null || typeof obj !== "object") return obj;
  if (seen.has(obj)) return seen.get(obj); // handle circular references
  const clone = Array.isArray(obj) ? [] : {};
  seen.set(obj, clone);
  for (const key in obj) {
    clone[key] = deepClone(obj[key], seen);
  }
  return clone;
}
```

**Interview Questions — Advanced Patterns**
1. Difference between debounce and throttle? When would you use each? *(debounce: search input, resize-end; throttle: scroll handlers, rate-limiting API calls)*
2. Implement `debounce` and `throttle` from scratch (very common — expect to write this live). *(The implementations above use a closure to retain timer state: debounce clears and replaces its timer on every call; throttle runs immediately, then suppresses calls until its interval expires.)*
3. What is currying, and what's a real-world use case? *(partial application, function composition, configurable utility functions)*
4. What is memoization, and what's a tradeoff of using it? *(trades memory for speed; can also cause stale results if inputs are objects/mutable)*
5. Implement a deep clone function that handles circular references. *(The `deepClone` implementation above stores already-cloned objects in a `WeakMap`; when it sees an object again, it returns the stored clone instead of recursing forever.)*
6. Implement `curry` from scratch. *(The `curry` implementation above accumulates arguments in a closure and calls the original function once the collected count reaches `fn.length`.)*

---

## 15. Design Patterns

```javascript
// Singleton — ensure only one instance exists
class Singleton {
  static #instance;
  constructor() {
    if (Singleton.#instance) return Singleton.#instance;
    Singleton.#instance = this;
  }
}
console.log(new Singleton() === new Singleton()); // true

// Observer / Pub-Sub — decoupled event-driven communication
class EventEmitter {
  #listeners = {};
  on(event, callback) {
    (this.#listeners[event] ??= []).push(callback);
  }
  emit(event, ...args) {
    (this.#listeners[event] || []).forEach(cb => cb(...args));
  }
}
const emitter = new EventEmitter();
emitter.on("data", (msg) => console.log("Received:", msg));
emitter.emit("data", "hello"); // "Received: hello"

// Factory — create objects without specifying exact class
function createShape(type) {
  const shapes = {
    circle: () => ({ type: "circle", area: (r) => Math.PI * r * r }),
    square: () => ({ type: "square", area: (s) => s * s })
  };
  return shapes[type]?.();
}

// Module pattern (using closures, pre-ES6 modules)
const CounterModule = (function () {
  let count = 0;
  return { increment: () => ++count, reset: () => (count = 0) };
})();
```

**Interview Questions — Design Patterns**
1. Implement the Singleton pattern and explain when it's useful (and its downsides — global state, hard to test). *(The `Singleton` example above returns the same instance from every construction. It can suit one shared coordinator or configuration object, but global mutable state makes tests and dependencies harder to reason about; dependency injection is often preferable.)*
2. Implement a simple pub-sub / event emitter from scratch. *(The `EventEmitter` above stores callbacks by event name, adds listeners with `on`, and invokes a snapshot of the listeners in `emit`. A production version should also provide `off`, listener error isolation, and cleanup.)*
3. What's the difference between the Factory pattern and using `class` constructors directly? *(A constructor directly creates one known concrete type. A factory hides creation decisions and can select among implementations, apply configuration, or return cached objects without coupling callers to concrete classes.)*
4. Name design patterns commonly used in frontend frameworks (Observer in React state, Factory in component creation, Singleton in Redux store). *(Observer/pub-sub appears in event systems and reactive state; factories create components or services; module/singleton patterns provide shared configuration; adapter and facade patterns isolate framework or API details.)*

---

## 16. Memory Management & Performance

### Garbage Collection
JS uses **automatic garbage collection** via the **mark-and-sweep** algorithm — objects unreachable from the "root" (global object, active call stack) get collected.

```javascript
let obj = { data: "large data" };
obj = null; // original object becomes unreachable, eligible for GC
```

### Common Memory Leak Sources
```javascript
// 1. Forgotten timers/intervals holding references
const heavyData = { /* large object */ };
setInterval(() => console.log(heavyData), 1000); // never cleared -> heavyData never GC'd

// 2. Detached DOM nodes still referenced in JS
let detachedNode = document.getElementById("el");
document.body.removeChild(detachedNode); // detached from DOM, but `detachedNode` var still holds a reference

// 3. Closures unintentionally retaining large scope
function outer() {
  const largeArray = new Array(1000000).fill("x");
  return function inner() { console.log("hi"); }; // doesn't use largeArray, but... 
  // engines are usually smart enough to GC unused vars, but be cautious with closures capturing scope
}

// 4. Global variables (accidental, without 'use strict')
function leak() {
  accidentalGlobal = "I'm now global forever"; // no var/let/const!
}
```

### WeakMap / WeakSet — allow garbage collection of keys
```javascript
let obj = { id: 1 };
const wm = new WeakMap();
wm.set(obj, "metadata"); // held WEAKLY — doesn't prevent GC
obj = null; // now eligible for garbage collection, WeakMap entry auto-removed
```

**Interview Questions — Memory & Performance**
1. How does JavaScript's garbage collector work? *(mark-and-sweep — traces reachability from roots)*
2. Name 3 common causes of memory leaks in JS applications. *(Uncleared timers or subscriptions, event listeners attached without removal, and detached DOM nodes or unbounded global caches that remain referenced are common causes. Long-lived closures can also retain large object graphs.)*
3. Difference between `Map`/`Set` and `WeakMap`/`WeakSet`? When would you use the Weak variants? *(caching metadata tied to an object's lifecycle without preventing GC)*
4. How would you detect a memory leak in a running web app? *(Chrome DevTools Memory tab — heap snapshots, allocation timelines)*
5. Why is it good practice to always clean up event listeners and intervals in component unmount/cleanup logic? *(Cleanup releases references, prevents callbacks from running against destroyed UI, avoids duplicate handlers after remounts, and prevents timers/subscriptions from retaining components and their state.)*

---

## 17. DOM & Browser Concepts

```javascript
// Event delegation — attach ONE listener to a parent instead of many to children
document.getElementById("list").addEventListener("click", (e) => {
  if (e.target.tagName === "LI") {
    console.log("Clicked item:", e.target.textContent);
  }
});
// Efficient for dynamic lists — new <li> elements automatically work without re-binding

// Event bubbling vs capturing
element.addEventListener("click", handler, true);  // capturing phase (top-down)
element.addEventListener("click", handler, false); // bubbling phase (bottom-up), default

// stopPropagation vs preventDefault
button.addEventListener("click", (e) => {
  e.stopPropagation(); // stops event from bubbling to parent listeners
  e.preventDefault();  // stops default browser action (e.g., form submit, link navigation)
});

// localStorage vs sessionStorage vs cookies
localStorage.setItem("theme", "dark");    // persists until explicitly cleared, ~5-10MB, not sent to server
sessionStorage.setItem("tempData", "1");  // cleared when tab closes
document.cookie = "user=abc; max-age=3600"; // sent with every HTTP request, small size (~4KB)
```

**Interview Questions — DOM/Browser**
1. What is event delegation and why is it useful for performance? *(Attach one handler to a stable ancestor and inspect `event.target` or `closest()` to handle child events. It reduces listener count and automatically supports dynamically added children.)*
2. Explain event bubbling vs capturing with a real DOM example. *(For a button inside a div, capturing travels document → div → button, while bubbling travels button → div → document. A parent listener can observe the button click during bubbling, or intercept it during capture.)*
3. Difference between `stopPropagation()` and `preventDefault()`? *(`stopPropagation()` prevents the event from continuing through the capture/bubble path; `preventDefault()` cancels the browser's default action, such as navigation or form submission. Neither one automatically does the other's job.)*
4. Compare `localStorage`, `sessionStorage`, and cookies — storage limits, persistence, server communication. *(`localStorage` persists by origin and is not sent automatically; `sessionStorage` is scoped to a tab/session and is also client-only; cookies are small, can expire, and are sent with matching requests. Sensitive cookies should use `HttpOnly`, `Secure`, and an appropriate `SameSite` policy.)*
5. What is the difference between `DOMContentLoaded` and `window.onload`? *(`DOMContentLoaded` fires when HTML is parsed; `load` fires after all resources — images, CSS — are also loaded)*

---

## 18. Senior-Level Questions

These test depth of understanding expected from 5-10+ years of experience.

1. **Explain how `async/await` is implemented under the hood.** *(Generators + Promises — the async function is essentially a state machine driven by a generator, where `await` pauses execution until a promise settles, similar to `yield`.)*

2. **How would you optimize a web app that's re-rendering too often / feels sluggish?**
   - Debounce/throttle expensive handlers
   - Use `requestAnimationFrame` for visual updates
   - Virtualize long lists
   - Memoize expensive computations
   - Avoid unnecessary object/array creation in hot paths (which breaks referential equality checks in frameworks)

3. **What's the difference between deep and shallow comparison, and why does it matter in frameworks like React?**
```javascript
const a = { x: 1 };
const b = { x: 1 };
console.log(a === b); // false — different references (shallow compare fails)
console.log(JSON.stringify(a) === JSON.stringify(b)); // true — deep equal
```

4. **Explain `Object.is()` vs `===`.** *(Nearly identical, but `Object.is(NaN, NaN)` is `true` and `Object.is(0, -0)` is `false`, unlike `===`.)*

5. **How do you implement a rate limiter / API request queue in JS?**
```javascript
class RequestQueue {
  #queue = [];
  #running = 0;
  constructor(maxConcurrent) { this.max = maxConcurrent; }
  add(fn) {
    return new Promise((resolve, reject) => {
      this.#queue.push({ fn, resolve, reject });
      this.#run();
    });
  }
  #run() {
    if (this.#running >= this.max || !this.#queue.length) return;
    this.#running++;
    const { fn, resolve, reject } = this.#queue.shift();
    fn().then(resolve, reject).finally(() => {
      this.#running--;
      this.#run();
    });
  }
}
```

6. **What is a polyfill, and how would you write one for `Array.prototype.includes`?**
```javascript
if (!Array.prototype.myIncludes) {
  Array.prototype.myIncludes = function (target) {
    for (let i = 0; i < this.length; i++) {
      if (this[i] === target) return true;
    }
    return false;
  };
}
```

7. **Explain "temporal dead zone" performance implications and strict mode.** *(`'use strict'` catches silent errors — assigning to undeclared vars, duplicate params — and disables some legacy footguns like `this` defaulting to the global object.)*

8. **How does V8 (Chrome/Node's JS engine) optimize code?** *(JIT compilation — interprets first via Ignition, then hot code paths get optimized/compiled via TurboFan; uses hidden classes for object shape optimization, so keeping consistent object shapes improves performance.)*

9. **What's the difference between `structuredClone`, `JSON.parse(JSON.stringify())`, and a manual deep clone?** *(`structuredClone` handles `Date`, `Map`, `Set`, circular refs, and more — but not functions; `JSON` methods lose functions, `undefined`, `Symbol`, dates become strings, and throw on circular references.)*

10. **Explain how you'd design a client-side caching layer with TTL (time-to-live) expiration.**
```javascript
class TTLCache {
  #store = new Map();
  set(key, value, ttlMs) {
    this.#store.set(key, { value, expiry: Date.now() + ttlMs });
  }
  get(key) {
    const entry = this.#store.get(key);
    if (!entry) return undefined;
    if (Date.now() > entry.expiry) { this.#store.delete(key); return undefined; }
    return entry.value;
  }
}
```

---

## Quick-Fire Rapid Q&A (Common Warm-Up Round)

| Question | Answer |
|---|---|
| Is JS single-threaded? | Yes, one call stack, but async via event loop + Web/Node APIs |
| What's a pure function? | Same input → same output, no side effects |
| `undefined` vs `null`? | `undefined` = not assigned; `null` = intentional absence of value |
| What's an IIFE? | Immediately Invoked Function Expression |
| What is hoisting? | Declarations moved to top of scope at compile time |
| What is `NaN`? | "Not a Number" — result of invalid math ops, `typeof NaN === "number"` |
| Truthy but tricky falsy values? | `0`, `""`, `null`, `undefined`, `NaN`, `false` are falsy; `[]` and `{}` are TRUTHY |
| What's a generator function? | `function*` — pausable/resumable function using `yield` |
| Symbol use case? | Unique, collision-free object property keys |
| `Array.isArray([])` vs `typeof []`? | `Array.isArray` correctly returns `true`; `typeof` returns `"object"` |

```javascript
// Generators example
function* idGenerator() {
  let id = 1;
  while (true) yield id++;
}
const gen = idGenerator();
console.log(gen.next().value); // 1
console.log(gen.next().value); // 2
```

---

*End of JavaScript guide — see the companion TypeScript-Interview-Guide.md for TS-specific concepts.*
