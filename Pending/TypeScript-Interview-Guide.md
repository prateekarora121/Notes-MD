# TypeScript Interview Guide — Beginner to Advanced

> TypeScript is a superset of JavaScript that adds static typing. This guide assumes you already know JS (see the companion JavaScript guide) and focuses on what TS adds.

---

## Table of Contents
1. [Why TypeScript & Basic Types](#1-why-typescript--basic-types)
2. [Interfaces vs Type Aliases](#2-interfaces-vs-type-aliases)
3. [Union, Intersection & Literal Types](#3-union-intersection--literal-types)
4. [Functions in TypeScript](#4-functions-in-typescript)
5. [Generics](#5-generics)
6. [Classes & OOP in TS](#6-classes--oop-in-ts)
7. [Type Narrowing & Type Guards](#7-type-narrowing--type-guards)
8. [Utility Types](#8-utility-types)
9. [Mapped & Conditional Types](#9-mapped--conditional-types)
10. [Enums & Tuples](#10-enums--tuples)
11. [Modules, Namespaces & Declaration Files](#11-modules-namespaces--declaration-files)
12. [Decorators](#12-decorators)
13. [tsconfig & Compiler Concepts](#13-tsconfig--compiler-concepts)
14. [Advanced/Senior-Level Questions](#14-advanced-senior-level-questions)

---

## 1. Why TypeScript & Basic Types

TypeScript adds **static type checking** at compile time, catching bugs before runtime, plus better IDE autocomplete/refactoring support. It compiles down ("transpiles") to plain JavaScript.

```typescript
let age: number = 30;
let name: string = "Riya";
let isActive: boolean = true;
let tags: string[] = ["a", "b"];
let tuple: [string, number] = ["age", 30]; // fixed-length, fixed-type array
let anything: any = "avoid this — disables type checking entirely";
let unknownVal: unknown = "safer than any — must narrow before use";
let nothing: void = undefined; // typically used as function return type
let notReached: never; // represents values that never occur (e.g., function that always throws)

function greet(name: string): string {
  return `Hello, ${name}`;
}

// Type inference — TS infers types even without annotations
let inferred = "hello"; // inferred as string, reassigning to a number would error
```

**Interview Questions — Basics**
1. What is TypeScript, and what problem does it solve over plain JS?
2. Difference between `any`, `unknown`, and `never`?
   - `any` — disables type checking (unsafe escape hatch).
   - `unknown` — type-safe counterpart to `any`; must narrow the type before using it.
   - `never` — represents values that can never occur (e.g., a function that always throws, or exhaustive switch fallthrough).
3. Does TypeScript exist at runtime? *(No — types are erased during compilation; TS is a compile-time-only tool)*
4. What is type inference, and when should you still add explicit annotations? *(function parameters, public API boundaries, complex return types)*

---

## 2. Interfaces vs Type Aliases

```typescript
// Interface — extendable, mergeable, best for object shapes
interface User {
  id: number;
  name: string;
  email?: string; // optional property
  readonly createdAt: Date; // cannot be reassigned after creation
}

interface Admin extends User {
  permissions: string[];
}

// Declaration merging — unique to interfaces
interface User {
  age: number; // merges with the User interface above
}

// Type alias — more flexible, can represent unions, primitives, tuples
type ID = string | number;
type Point = { x: number; y: number };
type Callback = (data: string) => void;

type AdminType = User & { permissions: string[] }; // intersection instead of extends
```

**Key differences:**
| Feature | `interface` | `type` |
|---|---|---|
| Extending | `extends` keyword | `&` intersection |
| Declaration merging | Yes (auto-merges) | No (error on duplicate) |
| Union types | No | Yes |
| Primitives/tuples | No | Yes |
| Best for | Object shapes, public APIs, class contracts | Unions, mapped types, utility compositions |

**Interview Questions — Interfaces vs Types**
1. When would you use `interface` vs `type`? *(interface for object shapes/OOP-style contracts that may be extended by consumers; type for unions, tuples, or complex compositions)*
2. What is declaration merging, and which one supports it?
3. Can a `type` be used with `implements` in a class? *(Yes, as long as it describes an object shape, not a union)*
4. Can two `type` aliases with the same name coexist in the same scope? *(No — TS throws a duplicate identifier error, unlike interfaces which merge)*

---

## 3. Union, Intersection & Literal Types

```typescript
// Union — value can be ONE of several types
type Status = "loading" | "success" | "error"; // string literal union
function printId(id: number | string) {
  console.log(id);
}

// Intersection — combines multiple types into one
type Named = { name: string };
type Aged = { age: number };
type Person = Named & Aged; // must have BOTH name and age

const p: Person = { name: "Dev", age: 25 };

// Discriminated unions — extremely common pattern for state modeling
type LoadingState = { status: "loading" };
type SuccessState = { status: "success"; data: string[] };
type ErrorState = { status: "error"; error: string };
type RequestState = LoadingState | SuccessState | ErrorState;

function render(state: RequestState) {
  switch (state.status) {
    case "loading": return "Loading...";
    case "success": return state.data.join(", "); // TS knows `data` exists here
    case "error": return state.error;              // TS knows `error` exists here
  }
}
```

**Interview Questions — Unions/Intersections**
1. What's the difference between a union type and an intersection type?
2. What is a discriminated union, and why is it useful? *(lets TS narrow the exact shape based on a common "tag"/"discriminant" property — very common for modeling API/UI state)*
3. Can you use array/object destructuring with union types safely? What has to happen first? *(you generally need to narrow the type first — see Section 7)*

---

## 4. Functions in TypeScript

```typescript
// Optional & default parameters
function buildUrl(base: string, path?: string, protocol: string = "https") {
  return `${protocol}://${base}${path ?? ""}`;
}

// Rest parameters
function sum(...nums: number[]): number {
  return nums.reduce((a, b) => a + b, 0);
}

// Function overloads — multiple call signatures for the same function
function parseInput(input: string): string[];
function parseInput(input: number): number;
function parseInput(input: string | number): string[] | number {
  if (typeof input === "string") return input.split(",");
  return input;
}
parseInput("a,b,c"); // string[]
parseInput(42);      // number

// Function types
type MathOp = (a: number, b: number) => number;
const add: MathOp = (a, b) => a + b;

// `this` parameter typing
function handleClick(this: HTMLButtonElement, event: MouseEvent) {
  console.log(this.disabled); // TS knows `this` is a button
}
```

**Interview Questions — Functions**
1. What are function overloads and when would you use them? *(when a function's return type depends on the input type in a way generics can't cleanly express)*
2. How do you type optional vs default parameters?
3. How can you explicitly type `this` inside a function?
4. What's the difference between `void` and `undefined` as a return type? *(`void` signals "the return value should be ignored," `undefined` is a more literal type that actually enforces returning `undefined`)*

---

## 5. Generics

Generics let you write reusable, type-safe code that works across multiple types instead of duplicating logic per type.

```typescript
// Generic function
function identity<T>(value: T): T {
  return value;
}
identity<string>("hello"); // T = string
identity(42);              // T inferred as number

// Generic with constraints
function getProperty<T, K extends keyof T>(obj: T, key: K): T[K] {
  return obj[key];
}
const user = { name: "Om", age: 30 };
getProperty(user, "name"); // OK, returns string
getProperty(user, "email"); // Error — 'email' is not a key of user

// Generic interfaces
interface ApiResponse<T> {
  data: T;
  status: number;
  error?: string;
}
const response: ApiResponse<string[]> = { data: ["a", "b"], status: 200 };

// Generic classes
class Stack<T> {
  private items: T[] = [];
  push(item: T): void { this.items.push(item); }
  pop(): T | undefined { return this.items.pop(); }
  peek(): T | undefined { return this.items[this.items.length - 1]; }
}
const numberStack = new Stack<number>();
numberStack.push(1);
numberStack.push(2);
console.log(numberStack.pop()); // 2

// Multiple generic parameters
function merge<A, B>(a: A, b: B): A & B {
  return { ...a, ...b };
}
const merged = merge({ name: "X" }, { age: 1 }); // { name: string; age: number }

// Default generic type
interface Box<T = string> {
  value: T;
}
const stringBox: Box = { value: "hi" }; // uses default T = string
```

**Interview Questions — Generics**
1. What problem do generics solve? Give an example of code duplication generics eliminate.
2. What does `<T extends keyof T>` (generic constraint) do, and why is it useful? *(restricts T to be a valid property key of another type, catching typos at compile time)*
3. Difference between `Stack<T>` (generic class) and using `any[]`? *(generics preserve type safety per-instance; `any` loses all type checking)*
4. How do generic defaults work (`<T = string>`)?
5. Implement a generic `filter` function type-safely.
```typescript
function myFilter<T>(arr: T[], predicate: (item: T) => boolean): T[] {
  return arr.filter(predicate);
}
```

---

## 6. Classes & OOP in TS

```typescript
class Employee {
  // Access modifiers: public (default), private, protected, readonly
  public name: string;
  private salary: number;
  protected department: string;
  readonly id: number;

  constructor(name: string, salary: number, department: string, id: number) {
    this.name = name;
    this.salary = salary;
    this.department = department;
    this.id = id;
  }

  // Shorthand: parameter properties (very common in interviews)
  // constructor(public name: string, private salary: number) {}

  getSalary(): number {
    return this.salary; // accessible within class
  }
}

const emp = new Employee("Nina", 50000, "Engineering", 1);
console.log(emp.name);    // OK — public
console.log(emp.salary);  // Error — private, only accessible within Employee class

// Abstract classes — cannot be instantiated directly, define a contract for subclasses
abstract class Shape {
  abstract area(): number; // must be implemented by subclasses
  describe(): string {
    return `Area is ${this.area()}`;
  }
}
class Circle extends Shape {
  constructor(private radius: number) { super(); }
  area(): number { return Math.PI * this.radius ** 2; }
}
// new Shape(); // Error — cannot instantiate abstract class

// Interfaces implemented by classes
interface Flyable {
  fly(): void;
}
class Bird implements Flyable {
  fly() { console.log("Flying!"); }
}
```

**Access Modifier Summary**
| Modifier | Accessible from |
|---|---|
| `public` (default) | Anywhere |
| `private` | Only within the declaring class |
| `protected` | Declaring class + subclasses |
| `readonly` | Can be read anywhere, set only at declaration/constructor |

**Interview Questions — Classes**
1. Difference between `private`, `protected`, and `public`?
2. What's the difference between an `abstract class` and an `interface`? *(abstract classes can have implementation + state and are instantiated via subclassing; interfaces are pure contracts, no implementation, and a class can implement multiple interfaces but extend only one class)*
3. What are parameter properties (constructor shorthand)?
4. Can a TS class implement multiple interfaces? Can it extend multiple classes? *(implements: yes multiple; extends: only one — single inheritance)*
5. Does TypeScript's `private` provide true runtime privacy like JS `#field`? *(No — TS `private` is compile-time only and erased at runtime; use `#field` for true runtime privacy)*

---

## 7. Type Narrowing & Type Guards

TypeScript narrows a broader type down to a more specific one within a conditional block.

```typescript
// typeof narrowing
function printValue(value: string | number) {
  if (typeof value === "string") {
    console.log(value.toUpperCase()); // TS knows it's a string here
  } else {
    console.log(value.toFixed(2)); // TS knows it's a number here
  }
}

// instanceof narrowing
class Dog { bark() { console.log("Woof"); } }
class Cat { meow() { console.log("Meow"); } }
function makeSound(animal: Dog | Cat) {
  if (animal instanceof Dog) {
    animal.bark();
  } else {
    animal.meow();
  }
}

// 'in' operator narrowing
type Fish = { swim: () => void };
type Bird = { fly: () => void };
function move(animal: Fish | Bird) {
  if ("swim" in animal) {
    animal.swim();
  } else {
    animal.fly();
  }
}

// Custom type guard (user-defined type predicate)
interface Cat2 { meow(): void }
interface Dog2 { bark(): void }
function isCat(pet: Cat2 | Dog2): pet is Cat2 {
  return (pet as Cat2).meow !== undefined;
}
function handlePet(pet: Cat2 | Dog2) {
  if (isCat(pet)) {
    pet.meow(); // narrowed to Cat2
  }
}

// Non-null assertion (!) — tells TS "trust me, this isn't null/undefined"
function getLength(str: string | null) {
  return str!.length; // use sparingly — bypasses null checking
}
```

**Interview Questions — Narrowing**
1. What is type narrowing? List 4 ways to narrow a union type in TS.
2. How do you write a custom type guard function? What does `pet is Cat2` mean? *(a type predicate — tells the compiler the return value confirms the input's narrowed type)*
3. What does the non-null assertion operator (`!`) do, and why should it be used sparingly? *(it silences the compiler's null check without runtime protection — can cause runtime errors if wrong)*
4. Difference between type narrowing and type casting/assertion (`as`)? *(narrowing is safe, compiler-verified; `as` is an unchecked assertion that can lie to the compiler)*

---

## 8. Utility Types

TypeScript ships several built-in utility types for common transformations.

```typescript
interface User {
  id: number;
  name: string;
  email: string;
  age?: number;
}

// Partial — makes all properties optional
type PartialUser = Partial<User>; // { id?: number; name?: string; ... }

// Required — makes all properties required
type RequiredUser = Required<User>; // age becomes required too

// Readonly — makes all properties readonly
type ReadonlyUser = Readonly<User>;

// Pick — select a subset of properties
type UserPreview = Pick<User, "id" | "name">; // { id: number; name: string }

// Omit — exclude specific properties
type UserWithoutEmail = Omit<User, "email">;

// Record — construct an object type with specific keys and value type
type Roles = "admin" | "editor" | "viewer";
type RolePermissions = Record<Roles, string[]>;
const perms: RolePermissions = {
  admin: ["read", "write", "delete"],
  editor: ["read", "write"],
  viewer: ["read"]
};

// Exclude / Extract — for union types
type AllTypes = string | number | boolean;
type NoBoolean = Exclude<AllTypes, boolean>; // string | number
type OnlyBoolean = Extract<AllTypes, boolean>; // boolean

// ReturnType / Parameters — extract function signature types
function createUser(name: string, age: number) { return { name, age }; }
type CreateUserReturn = ReturnType<typeof createUser>; // { name: string; age: number }
type CreateUserParams = Parameters<typeof createUser>; // [string, number]

// NonNullable
type MaybeString = string | null | undefined;
type DefinitelyString = NonNullable<MaybeString>; // string
```

**Interview Questions — Utility Types**
1. What does `Partial<T>` do, and when is it useful? *(e.g., PATCH/update endpoints where only some fields are provided)*
2. Difference between `Pick` and `Omit`?
3. What is `Record<K, V>` used for? Give an example.
4. How would you extract a function's return type without calling it? *(`ReturnType<typeof fn>`)*
5. Implement `Partial<T>` yourself using a mapped type (see next section).

---

## 9. Mapped & Conditional Types

### Mapped Types — build a new type by transforming each property of an existing type

```typescript
// Recreating Partial<T> and Readonly<T> from scratch
type MyPartial<T> = { [K in keyof T]?: T[K] };
type MyReadonly<T> = { readonly [K in keyof T]: T[K] };
type MyRecord<K extends string | number | symbol, V> = { [P in K]: V };

// Mapped type with modifiers (+/- to add/remove optional or readonly)
type Concrete<T> = { [K in keyof T]-?: T[K] }; // removes optional (-?)
type Mutable<T> = { -readonly [K in keyof T]: T[K] }; // removes readonly

// Key remapping (TS 4.1+) with `as`
type Getters<T> = {
  [K in keyof T as `get${Capitalize<string & K>}`]: () => T[K];
};
interface Person { name: string; age: number; }
type PersonGetters = Getters<Person>;
// { getName: () => string; getAge: () => number }
```

### Conditional Types — types that branch based on a condition

```typescript
type IsString<T> = T extends string ? true : false;
type A = IsString<"hello">; // true
type B = IsString<42>;      // false

// infer keyword — extract a type from within another type
type ElementType<T> = T extends (infer U)[] ? U : T;
type Num = ElementType<number[]>; // number

type ReturnTypeCustom<T> = T extends (...args: any[]) => infer R ? R : never;
function foo() { return "hello"; }
type FooReturn = ReturnTypeCustom<typeof foo>; // string

// Distributive conditional types (applies per union member)
type ToArray<T> = T extends any ? T[] : never;
type Result = ToArray<string | number>; // string[] | number[]

// Template literal types (TS 4.1+)
type EventName<T extends string> = `on${Capitalize<T>}`;
type ClickEvent = EventName<"click">; // "onClick"
```

**Interview Questions — Mapped/Conditional Types**
1. What is a mapped type? Implement `Readonly<T>` and `Partial<T>` from scratch.
2. What does the `infer` keyword do? Give an example.
3. What are distributive conditional types, and when do they "distribute" over a union?
4. What are template literal types used for? Give a practical example. *(typed event names, typed route paths, typed CSS-in-JS keys)*
5. Explain `keyof`, `typeof`, and indexed access types (`T[K]`).
```typescript
interface Config { debug: boolean; port: number; }
type ConfigKeys = keyof Config;        // "debug" | "port"
type PortType = Config["port"];        // number
const cfg = { debug: true };
type CfgType = typeof cfg;             // { debug: boolean }
```

---

## 10. Enums & Tuples

```typescript
// Numeric enum (auto-incrementing)
enum Direction { Up, Down, Left, Right } // Up=0, Down=1, Left=2, Right=3
console.log(Direction.Up); // 0
console.log(Direction[0]); // "Up" — reverse mapping (numeric enums only)

// String enum — no reverse mapping, but more readable at runtime/debugging
enum Status {
  Active = "ACTIVE",
  Inactive = "INACTIVE"
}

// const enum — fully inlined at compile time, no runtime object generated (more performant)
const enum Level { Low, Medium, High }
let l: Level = Level.Medium; // compiles to `let l = 1;` directly

// Modern alternative: union of string literals (often preferred over enums)
type StatusLiteral = "ACTIVE" | "INACTIVE"; // no runtime footprint at all, simpler

// Tuples — fixed-length, fixed-order arrays
let point: [number, number] = [10, 20];
let entry: [string, number, boolean?] = ["key", 1]; // optional element
let labeled: [x: number, y: number] = [1, 2]; // labeled tuple (better DX/readability)

function useState<T>(initial: T): [T, (newVal: T) => void] {
  let value = initial;
  const setValue = (newVal: T) => { value = newVal; };
  return [value, setValue]; // tuple return — familiar from React's useState!
}
```

**Interview Questions — Enums & Tuples**
1. Difference between numeric and string enums?
2. What is a `const enum`, and why might you prefer it? *(no runtime object — fully inlined, smaller bundle size)*
3. Why do many teams prefer union-of-string-literals over enums? *(enums generate runtime code and have some quirky behaviors; literal unions are zero-runtime-cost and integrate better with plain JS objects)*
4. What is a tuple, and how does it differ from a regular array type? *(fixed length and fixed type per position, vs arrays which are variable-length and homogeneous)*
5. Where have you seen tuples used in real-world APIs? *(React's `useState` returns a tuple `[value, setter]`)*

---

## 11. Modules, Namespaces & Declaration Files

```typescript
// math.ts
export interface Point { x: number; y: number; }
export function distance(a: Point, b: Point): number {
  return Math.sqrt((a.x - b.x) ** 2 + (a.y - b.y) ** 2);
}

// app.ts
import { distance, Point } from "./math";

// Declaration files (.d.ts) — describe types for existing JS libraries
// mylib.d.ts
declare module "mylib" {
  export function doSomething(input: string): number;
}

// Ambient/global declarations
declare global {
  interface Window {
    myCustomProperty: string;
  }
}

// Namespaces — legacy way to organize code, mostly replaced by ES modules
namespace Utils {
  export function double(n: number) { return n * 2; }
}
Utils.double(5);
```

**Interview Questions — Modules**
1. What are `.d.ts` files used for? *(providing type information for plain JS libraries that don't ship their own types, e.g. via DefinitelyTyped `@types/` packages)*
2. Difference between namespaces and ES modules? *(namespaces are a TS-only, older code organization construct; ES modules are the standard, file-based, tree-shakeable approach — namespaces are largely legacy now)*
3. How do you add type definitions for a third-party JS library that doesn't have types? *(install `@types/<package>` from DefinitelyTyped, or write a custom `.d.ts` declaration)*

---

## 12. Decorators

Decorators are a (Stage 3 proposal / experimental in TS) feature for annotating and modifying classes and their members — heavily used in frameworks like Angular and NestJS.

```typescript
// Requires "experimentalDecorators": true in tsconfig.json (legacy decorators)

function LogClass(constructor: Function) {
  console.log(`Class created: ${constructor.name}`);
}

function LogMethod(target: any, propertyKey: string, descriptor: PropertyDescriptor) {
  const original = descriptor.value;
  descriptor.value = function (...args: any[]) {
    console.log(`Calling ${propertyKey} with`, args);
    return original.apply(this, args);
  };
}

@LogClass
class Calculator {
  @LogMethod
  add(a: number, b: number) {
    return a + b;
  }
}
const calc = new Calculator(); // logs "Class created: Calculator"
calc.add(2, 3); // logs "Calling add with [2, 3]"

// Property decorator example (commonly seen in Angular/NestJS)
function Required(target: any, propertyKey: string) {
  console.log(`${propertyKey} is required`);
}
class Form {
  @Required
  username!: string;
}
```

**Interview Questions — Decorators**
1. What are decorators used for in TypeScript? Name a framework that relies heavily on them. *(Angular for `@Component`, `@Injectable`; NestJS for `@Controller`, `@Get`)*
2. What's the difference between a class decorator, method decorator, and property decorator?
3. Are decorators a stable JS feature? *(They were an experimental/proposal feature for a long time; a newer, different decorators proposal reached Stage 3 in TC39 and is being adopted — worth mentioning you're aware the API has evolved.)*

---

## 13. tsconfig & Compiler Concepts

```json
{
  "compilerOptions": {
    "target": "ES2020",           // output JS version
    "module": "ESNext",           // module system for output
    "strict": true,                // enables all strict type-checking options
    "noImplicitAny": true,         // error on implicit 'any' types
    "strictNullChecks": true,      // null/undefined must be explicitly handled
    "esModuleInterop": true,       // better interop between CommonJS and ES modules
    "skipLibCheck": true,          // skip type-checking of .d.ts files (faster builds)
    "outDir": "./dist",
    "rootDir": "./src",
    "declaration": true,           // generate .d.ts files for consumers
    "sourceMap": true              // generate source maps for debugging
  }
}
```

**Interview Questions — Compiler/tsconfig**
1. What does `strict: true` actually enable? Name 2-3 of the flags it bundles. *(`strictNullChecks`, `noImplicitAny`, `strictFunctionTypes`, `alwaysStrict`, etc.)*
2. What's the difference between `target` and `module` in tsconfig? *(`target` = JS language version output; `module` = module system output, e.g. CommonJS vs ESNext)*
3. What does `strictNullChecks` prevent? *(assigning `null`/`undefined` to a type that doesn't explicitly allow it — catches a huge class of runtime "cannot read property of undefined" bugs)*
4. How does TypeScript get compiled/run? *(`tsc` compiles to plain JS; tools like `ts-node`, `esbuild`, `swc`, or bundlers like Vite/webpack with a TS loader handle dev-time execution)*

---

## 14. Advanced / Senior-Level Questions

1. **How does structural typing (duck typing) in TS differ from nominal typing in languages like Java/C#?**
```typescript
interface Point { x: number; y: number; }
function logPoint(p: Point) { console.log(p.x, p.y); }
const obj = { x: 1, y: 2, z: 3 }; // has EXTRA property
logPoint(obj); // OK! TS only checks structural compatibility, not exact type identity
```

2. **Explain type erasure and why TS has zero runtime overhead for type-only code.** *(all type annotations, interfaces, and generics are stripped away during compilation — none of it exists at runtime, which is why you can't do `if (x instanceof SomeInterface)`.)*

3. **How do you type a Redux-style reducer with discriminated unions?**
```typescript
type Action =
  | { type: "increment"; payload: number }
  | { type: "decrement"; payload: number }
  | { type: "reset" };

function reducer(state: number, action: Action): number {
  switch (action.type) {
    case "increment": return state + action.payload;
    case "decrement": return state - action.payload;
    case "reset": return 0;
    default: {
      const _exhaustive: never = action; // compiler error if a case is missed!
      return state;
    }
  }
}
```

4. **What is the "exhaustiveness check" pattern using `never`, and why is it valuable?** *(Assigning the leftover value to a variable typed `never` inside the `default` case — if a new union member is added later and not handled, TS throws a compile error instead of silently falling through.)*

5. **How would you type a generic API client function?**
```typescript
async function apiGet<T>(url: string): Promise<T> {
  const res = await fetch(url);
  if (!res.ok) throw new Error(`HTTP ${res.status}`);
  return res.json() as Promise<T>;
}
interface UserDTO { id: number; name: string; }
const user = await apiGet<UserDTO>("/api/user/1"); // user is typed as UserDTO
```

6. **Difference between `as const` and a regular type annotation?**
```typescript
let arr1 = [1, 2, 3];            // number[]
let arr2 = [1, 2, 3] as const;   // readonly [1, 2, 3] — literal, immutable tuple type
```

7. **How do you handle third-party JS libraries without type definitions?** *(Install community types from `@types/package-name`, or declare a minimal ambient module yourself with `declare module "package-name";`)*

8. **What's the difference between `interface` merging and module augmentation?** *(Module augmentation lets you add properties to types from external modules/libraries — e.g., extending Express's `Request` type with a custom `user` property — leveraging the same declaration-merging mechanism as interfaces.)*
```typescript
declare global {
  namespace Express {
    interface Request {
      user?: { id: string };
    }
  }
}
```

9. **When would you use `unknown` over `any` in a function parameter, and how do you safely use an `unknown` value?** *(`unknown` forces you to narrow/validate before use, preventing accidental unsafe operations — e.g., parsing untrusted JSON from an API.)*
```typescript
function processInput(input: unknown) {
  if (typeof input === "string") {
    console.log(input.toUpperCase()); // safe, narrowed
  }
}
```

10. **How does TypeScript's structural typing handle excess property checks on object literals vs variables?**
```typescript
interface Config { name: string; }
function setup(c: Config) {}
setup({ name: "x", extra: 1 }); // Error — excess property check on OBJECT LITERALS
const obj = { name: "x", extra: 1 };
setup(obj); // OK — no excess check when passing a pre-existing variable (structural typing applies)
```

---

## Quick-Fire Rapid Q&A

| Question | Answer |
|---|---|
| Is TS a compiled or interpreted language? | Compiled (transpiled) to JS, then JS is interpreted/JIT-compiled |
| Does TS catch errors at runtime? | No — compile-time only, types are erased |
| `interface` vs `type` — pick one default? | Interface for object shapes/public APIs, type for everything else |
| What is structural typing? | Type compatibility based on shape, not declared name |
| Can TS fully guarantee type safety? | No — `any`, type assertions (`as`), and `!` can all bypass it |
| What does `keyof` do? | Produces a union of an object type's property names |
| What's a type predicate? | A function return type like `x is Foo` used in custom type guards |
| Strict mode's biggest practical benefit? | `strictNullChecks` catches null/undefined bugs at compile time |

---

*End of TypeScript guide. Practice by writing code for each example from memory, then explaining the "why" out loud — most 10-YOE interviews focus heavily on trade-off reasoning, not just syntax recall.*
