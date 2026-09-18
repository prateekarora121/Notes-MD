# TypeScript + Angular Interview Guide (Senior Full-Stack, 10+ YOE)

> Rewritten for interview prep. Every topic follows the same pattern: **plain-English explanation → code example → common mistakes → interview Q&A (including tricky follow-ups) → one-line revision summary.**
> Examples are drawn from a **Dealer Management Dashboard** (Angular 19) — entities like `Vehicle`, `Dealer`, `SalesOrder`, `Inventory` — so the patterns map directly onto real work, not toy examples.

---

## How to use this guide

- Skim the **Quick Revision Sheet** (last section) the night before an interview.
- Read a topic top to bottom the first time; on repeat passes, only read the **Interview Q&A** and **Common Mistakes** boxes.
- Anything marked 🎯 is a question that has come up repeatedly in real TypeScript/Angular interviews at the senior level.

---

## Table of Contents

1. [Part 1 — Core TypeScript Fundamentals](#part-1--core-typescript-fundamentals)
2. [Part 2 — Intermediate Type System](#part-2--intermediate-type-system)
3. [Part 3 — Advanced Types & Generics](#part-3--advanced-types--generics)
4. [Part 4 — Object-Oriented TypeScript](#part-4--object-oriented-typescript)
5. [Part 5 — Error Handling](#part-5--error-handling)
6. [Part 6 — Performance](#part-6--performance)
7. [Part 7 — Best Practices Cheat Sheet](#part-7--best-practices-cheat-sheet)
8. [Part 8 — Common Pitfalls](#part-8--common-pitfalls)
9. [Part 9 — Rapid-Fire Interview Q&A](#part-9--rapid-fire-interview-qa)
10. [Quick Revision Sheet](#quick-revision-sheet)

---

## Part 1 — Core TypeScript Fundamentals

### 1.1 What is TypeScript, and why use it?

**Simple explanation:** TypeScript is a typed superset of JavaScript built by Microsoft. It compiles ("transpiles") down to plain JavaScript — types exist only at compile time and are completely erased at runtime.

**Why teams use it:**
- **Catches a whole class of bugs at compile time** — wrong property names, wrong argument shapes, null/undefined misuse — instead of in production.
- **Makes large refactors safe.** Renaming a field in a 500-file Angular app is mechanically safe because the compiler flags every broken usage.
- **Better developer experience** — IntelliSense, "go to definition," inline docs.
- **Self-documenting APIs** — a function signature tells you the contract without reading the implementation.

**Senior-level nuance (interviewers probe this):** TypeScript's type system is *deliberately unsound* in several places — `any`, type assertions, array index access without `noUncheckedIndexedAccess`, bivariant method parameters. This is a pragmatic trade-off to stay compatible with JavaScript. Be ready to name *where* it's unsound and how to tighten it (see `strict` flags, §2.10).

🎯 **Q: Does TypeScript make your code faster at runtime?**
A: No — types are 100% erased. The only runtime cost comes from constructs that emit real JS: non-`const` enums, decorators + `reflect-metadata`, namespaces. Plain interfaces/types cost nothing.

### 1.2 Toolchain basics

```bash
npm install -g typescript
tsc -v                # check version
tsc filename.ts        # compiles to filename.js
```

A minimal `tsconfig.json`:

```json
{ "compilerOptions": { "target": "ES6", "strict": true } }
```

A **realistic** config for an Angular 19 project (this is what you'll actually be asked to defend in an interview):

```json
{
  "compilerOptions": {
    "target": "ES2022",
    "module": "ESNext",
    "moduleResolution": "Bundler",
    "lib": ["ES2022", "DOM"],
    "strict": true,
    "noUncheckedIndexedAccess": true,
    "exactOptionalPropertyTypes": true,
    "esModuleInterop": true,
    "skipLibCheck": true,
    "isolatedModules": true,
    "forceConsistentCasingInFileNames": true
  }
}
```

An interviewer at senior level expects you to explain *why* each flag matters, not just that `strict: true` exists — see §2.10.

### 1.3 Primitive & special types

- **Primitives:** `string`, `number`, `boolean`, `null`, `undefined`, `bigint`, `symbol`.
- **Special types:** `any`, `unknown`, `void`, `never`.

### 1.4 `any` vs `unknown` vs `void` vs `never` 🎯

This four-way comparison is one of the most common TypeScript interview questions.

| Type | Meaning | Assignable **to** other types? | Assignable **from** other types? | Typical use |
|---|---|---|---|---|
| `any` | "Turn off type checking" | Yes, to anything | Yes, from anything | Legacy JS interop, last resort |
| `unknown` | "Could be anything — prove it before using it" | No (without narrowing/assertion) | Yes, from anything | External/untrusted input: API responses, `JSON.parse`, catch blocks |
| `void` | "No meaningful return value" | Only to `void`/`any`/`undefined` | N/A (return position) | Callback / function return type |
| `never` | "This path is unreachable / no value can satisfy this" | Assignable everywhere (bottom type) | Nothing except `never` itself | Exhaustiveness checks, functions that always throw |

**Key gotcha:** `any` is both a top type *and* a bottom type at once — this is what makes it dangerous, and why `unknown` was introduced in TS 3.0 as the type-safe alternative.

**Dealer Dashboard example:**

```typescript
async function fetchVehicleById(id: string): Promise<unknown> {
  const res = await fetch(`/api/vehicles/${id}`);
  return res.json(); // we genuinely don't know the shape yet
}

function isVehicle(v: unknown): v is Vehicle {
  return typeof v === 'object' && v !== null && 'vin' in v && 'model' in v;
}

const raw = await fetchVehicleById('123');
if (isVehicle(raw)) {
  console.log(raw.model); // safe — narrowed to Vehicle
}
```

**Common mistake:** treating `any` as "the fix for the red squiggly line" instead of reaching for `unknown` + narrowing. This silently reintroduces the exact bug class TypeScript exists to prevent.

🎯 **Follow-up: Why does `unknown` exist when we already have `any`?**
A: `unknown` forces you to narrow (via `typeof`, a type guard, or a schema validator like `zod`) before doing anything with the value, which keeps the type system sound while still letting you say "I don't know this type yet" at I/O boundaries.

### 1.5 Type inference vs type annotation

```typescript
let message = "Hello";      // inferred as string
let age: number = 25;       // explicitly annotated
```

**Senior rule of thumb:** prefer inference for local variables (less noise, same safety), but prefer explicit annotations at **module boundaries** — function parameters, exported return types, public class members. At a boundary, inference can silently widen or narrow when the implementation changes, breaking consumers with no visible signal.

### 1.6 Type assertions

```typescript
let someValue: any = "Hello";
let strLength: number = (someValue as string).length;
```

Assertions (`as`) tell the compiler "trust me" and do **zero runtime validation**. A double assertion, `as unknown as T`, sidesteps even TS's assertion-compatibility check — a red flag interviewers catch instantly. Whenever data crosses a runtime boundary (HTTP response, `localStorage`, third-party library), prefer type guards or a validation library (`zod`, `io-ts`) over assertions.

### 1.7 Type aliases vs interfaces 🎯

| Feature | `interface` | `type` |
|---|---|---|
| Extending | `extends`, supports multiple inheritance | Via intersection (`&`), not `extends` |
| Declaration merging | Yes — multiple `interface X {}` blocks merge | No |
| Object shapes | Yes | Yes |
| Unions / tuples / primitives | No | Yes — `type T = string \| number` |
| Mapped / conditional types | No | Yes |

**Practical default:** use `interface` for object/class contracts that might be extended or merged (public DTOs, Angular `@Input` bags). Use `type` for unions, function signatures, and type-level utilities. They're structurally compatible with each other — an `interface` can extend a `type` alias and vice versa.

> Note: type aliases are sometimes described as "not extendable." That's only true of the `extends` keyword — they compose fine through intersections (`type C = A & B`), so it's an oversimplification, not a hard limitation.

### 1.8 Optional & readonly properties

```typescript
interface Dealer {
  name: string;
  region?: string;          // optional
}

interface Vehicle {
  readonly vin: string;     // cannot be reassigned after creation
}
```

**Gotcha:** `readonly` is **shallow**. It stops the property itself from being *rebound*, but not the nested object/array it points to from being mutated. For deep immutability use `Readonly<T>` recursively, or a library like Immer for real update patterns.

### 1.9 Function & method overloading

```typescript
function toDisplayPrice(amount: number): string;
function toDisplayPrice(amount: string): string;
function toDisplayPrice(amount: any): string {
  return `$${amount}`;
}
```

TS overloads are purely a **compile-time construct** — underneath there is one real JS function (the "implementation signature," which callers never see). This differs fundamentally from C#, where overloads are genuinely distinct methods resolved by the CLR at compile time. In TS, you're describing multiple *call-site shapes* for a single implementation, not creating multiple methods.

---

## Part 2 — Intermediate Type System

### 2.1 Regular functions vs arrow functions

| Feature | Regular function | Arrow function |
|---|---|---|
| `this` binding | Dynamic (depends on caller) | Lexical (inherited from enclosing scope) |
| `arguments` object | Available | Not available |
| Use in classes | Preferred for methods you intend to override | Good for callbacks that need captured `this` (event handlers) |
| `new`-able | Yes | No |

**Angular-relevant gotcha:** an arrow-function class property (`onSave = () => {...}`) correctly binds `this` for template event bindings, but creates **a new function per instance**, which has a real memory cost at scale and breaks `@HostListener`-style decorator binding that expects a real prototype method.

```typescript
function sum(...amounts: number[]) {
  return amounts.reduce((acc, n) => acc + n, 0);
}
```

### 2.2 Union, intersection & literal types

```typescript
let status: 'pending' | 'approved' | 'rejected';

interface HasVin { vin: string; }
interface HasPrice { price: number; }
type SellableVehicle = HasVin & HasPrice;
```

**Gotcha:** intersecting two types that have conflicting property types for the same key (e.g. `{ a: string } & { a: number }`) collapses that property to `never` — it does **not** produce an error. Classic interview trap.

### 2.3 Tuples

```typescript
let coordinate: [number, number] = [28.6, 77.2];
let readonlyPair: readonly [number, number] = [10, 20];
```

**Labeled & variadic tuples** (modern TS):

```typescript
type Point = [x: number, y: number]; // labels are documentation only, no runtime effect

type Concat<T extends unknown[], U extends unknown[]> = [...T, ...U];
type Combined = Concat<[1, 2], [3, 4]>; // [1, 2, 3, 4]
```

### 2.4 `readonly T[]` / `ReadonlyArray<T>` as defensive API design

**The problem:** a plain `T[]` parameter doesn't tell callers whether the function will mutate their array.

```typescript
function sortedTotal(prices: number[]): number {
  prices.sort((a, b) => a - b); // legal — but mutates the CALLER's array
  return prices.reduce((sum, p) => sum + p, 0);
}

const cart = [30, 10, 20];
sortedTotal(cart);
console.log(cart); // [10, 20, 30] — surprise mutation
```

**The fix:**

```typescript
function sortedTotal(prices: readonly number[]): number {
  // prices.sort(...);  // compile error — mutating methods removed from the type
  return [...prices].sort((a, b) => a - b).reduce((sum, p) => sum + p, 0);
}
```

- `readonly T[]` and `ReadonlyArray<T>` are the same type, two spellings.
- This is a **compile-time-only contract** — no `Object.freeze` involved. A mutable `T[]` is freely assignable *into* a `readonly T[]` parameter, never the reverse.
- Pairs naturally with `OnPush`/immutable-state patterns in Angular: a function typed to accept `readonly T[]` is guaranteed not to be the source of an accidental in-place mutation that breaks reference-equality change detection.
- **Boundary case:** it's shallow, exactly like `Readonly<T>` — `readonly Point[]` stops `push`/`arr[0] = ...` but not `arr[0].x = 5` unless `Point` itself is readonly.

### 2.5 Enums — and why senior devs avoid them

```typescript
enum OrderStatus { Pending, Approved, Rejected }
```

**Problems senior interviewers expect you to raise:**
- Enums generate real runtime JS objects (reverse-mapping objects), adding to bundle size.
- `const enum` avoids this by inlining values at compile time — but it's **incompatible with `isolatedModules`**, which modern bundlers (esbuild, swc, Vite, and Angular's esbuild-based builder since v17) require.
- They don't tree-shake as cleanly as literal unions.

**Modern replacement — literal union + optional `satisfies`-checked value map:**

```typescript
type OrderStatus = 'pending' | 'approved' | 'rejected';

const OrderStatusValues = {
  Pending: 'pending',
  Approved: 'approved',
  Rejected: 'rejected',
} as const satisfies Record<string, OrderStatus>;
```

This gives exhaustiveness checking, zero runtime cost, and full compatibility with esbuild-based Angular builds.

### 2.6 Structural typing vs nominal typing 🎯

The single most common "coming from a nominally-typed language" conceptual question.

- **C#/Java are nominally typed** — two classes with identical members are still incompatible unless one explicitly implements/extends the other. Type identity is based on the *declared name*.
- **TypeScript is structurally typed** (duck typing, enforced at compile time) — two types are compatible if their *shapes* match, regardless of name or declared relationship.

```typescript
interface DealerLocation { lat: number; lng: number; }
class MapPin { lat = 0; lng = 0; }

function plot(p: DealerLocation) { console.log(p.lat, p.lng); }

plot(new MapPin());        // OK — same shape
plot({ lat: 1, lng: 2 });  // OK — structurally matches
```

**Practical consequences worth mentioning:**
- **Excess property checks** fire only on **object literals** assigned directly, not on variables:
  ```typescript
  plot({ lat: 1, lng: 2, extra: true }); // error: 'extra' does not exist
  const p = { lat: 1, lng: 2, extra: true };
  plot(p); // compiles fine — checked structurally, not as a fresh literal
  ```
- `private`/`protected` members participate in a *nominal-ish* check — two classes with identically-named private members from different declarations are **not** considered structurally compatible.
- `unique symbol` and **branded/tagged types** are TypeScript's idiomatic way to simulate nominal typing when you need to stop two structurally-identical types from being mixed up.

### 2.7 Branded (nominal) types — full worked example

**The problem:** `DealerId` and `VehicleId` are both plain `string` at the type level, so TypeScript happily lets you pass one where the other is expected:

```typescript
type DealerId = string;
type VehicleId = string;

function getDealer(id: DealerId) { /* ... */ }

const vehicleId: VehicleId = 'veh_123';
getDealer(vehicleId); // compiles — silently wrong
```

**The fix — a branded type plus a constructor function:**

```typescript
type DealerId = string & { readonly __brand: 'DealerId' };
type VehicleId = string & { readonly __brand: 'VehicleId' };

// The constructor function is the ONLY sanctioned way to produce a branded value.
// Real validation lives here.
function toDealerId(raw: string): DealerId {
  if (!raw) throw new Error('DealerId cannot be empty');
  return raw as DealerId; // the one deliberate, encapsulated assertion
}

function getDealer(id: DealerId): void {
  console.log('Fetching dealer', id);
}

const dealerId = toDealerId('dlr_001');
getDealer(dealerId); // OK
// getDealer(toVehicleId('veh_123')); // compile error — caught!
```

### 2.8 Type narrowing & type guards

| Technique | Example | Notes |
|---|---|---|
| `typeof` | `if (typeof x === "string")` | Primitives only |
| `instanceof` | `if (err instanceof Error)` | Classes |
| `in` operator | `if ("vin" in obj)` | Property presence |
| Equality narrowing | `if (status === "approved")` | Narrows literal unions |
| Discriminant property | `if (shape.kind === "circle")` | See discriminated unions below |
| User-defined type predicate | `function isVehicle(x): x is Vehicle` | The only mechanism for custom runtime validation logic |
| Assertion functions | `function assertDefined(x): asserts x is NonNullable<...>` | Narrows the *rest of the enclosing scope* after the call, unlike predicates which only narrow inside one conditional |

```typescript
function assertIsDefined<T>(val: T): asserts val is NonNullable<T> {
  if (val === undefined || val === null) {
    throw new Error('Expected value to be defined');
  }
}

function process(vin?: string) {
  assertIsDefined(vin);
  console.log(vin.toUpperCase()); // narrowed to string for the rest of the function
}
```

### 2.9 Discriminated unions & exhaustiveness checking 🎯

```typescript
interface CashSale { kind: 'cash'; amount: number; }
interface FinancedSale { kind: 'financed'; amount: number; monthlyPayment: number; }
type Sale = CashSale | FinancedSale;

function total(sale: Sale): number {
  switch (sale.kind) {
    case 'cash': return sale.amount;
    case 'financed': return sale.amount;
  }
}
```

**The important follow-up:** what happens when someone adds a `LeaseSale` variant and forgets to update `total()`? Without an exhaustiveness guard, this silently compiles and returns `undefined` at runtime. Fix with a `never`-based guard:

```typescript
interface LeaseSale { kind: 'lease'; amount: number; leaseTermMonths: number; }
type Sale = CashSale | FinancedSale | LeaseSale;

function total(sale: Sale): number {
  switch (sale.kind) {
    case 'cash': return sale.amount;
    case 'financed': return sale.amount;
    case 'lease': return sale.amount;
    default:
      // If a case is missing above, `sale` here is NOT `never`,
      // so this fails to compile.
      const _exhaustiveCheck: never = sale;
      return _exhaustiveCheck;
  }
}
```

This pattern (discriminated union + `never` exhaustiveness check) is one of the strongest signals of TS seniority — it's a natural fit for API response types, NgRx actions, and state machines in the Dealer Dashboard (e.g. `OrderState = 'draft' | 'submitted' | 'approved' | 'delivered'`).

### 2.10 Optional chaining & nullish coalescing

```typescript
console.log(dealer.address?.city);      // undefined if address is missing
const displayName = dealer.name ?? 'Unnamed Dealer';
```

**Gotcha:** `??` is not the same as `||`. `0`, `""`, and `false` are valid values under `??`, but incorrectly get replaced under `||`. A classic bug when migrating old `||`-based default logic without re-auditing falsy-but-valid values (e.g. a vehicle with `discount: 0` should keep `0`, not fall back to a default).

### 2.11 `strictNullChecks` and the `strict` family of flags 🎯

`strict: true` is a bundle flag. Here's what it turns on:

| Flag | Effect | Real-world impact |
|---|---|---|
| `strictNullChecks` | `null`/`undefined` are not part of every type by default | The single biggest bug-catcher; forces explicit `T \| null` and narrowing |
| `noImplicitAny` | Errors on inferred `any` | Prevents silent type-safety holes, especially on untyped params |
| `strictFunctionTypes` | Function parameters checked contravariantly | Prevents unsound function assignment |
| `strictBindCallApply` | `.bind`/`.call`/`.apply` type-checked against the function signature | Catches wrong-arg bugs on these rarely-typed APIs |
| `strictPropertyInitialization` | Class properties must be initialized in the constructor, or explicitly allow `undefined` | Common Angular pain point — DI-populated fields need `!` or constructor init |
| `noImplicitThis` | Errors when `this` would implicitly be `any` | Relevant for regular functions used as callbacks |
| `useUnknownInCatchVariables` | `catch (e)` is typed `unknown`, not `any` | Forces safe error handling — see Part 5 |

**Flags NOT included in `strict` but essential in real senior-grade configs:**
- `noUncheckedIndexedAccess` — makes `arr[i]` / `record[key]` return `T | undefined` instead of `T`, closing a real hole around index signatures.
- `exactOptionalPropertyTypes` — distinguishes "key can be absent" from "key can be present with value `undefined`."
- `noFallthroughCasesInSwitch` — catches missing `break`/`return` in a `switch`.

🎯 **Q: Your team wants to disable `strictNullChecks` to speed up a migration. What do you push back with?**
A: You lose compile-time guarantees on every nullable path in the *entire* app, not just the migrated files — any possibly-null value becomes a latent `TypeError`. Most modern third-party `.d.ts` files also assume `strictNullChecks` is on, so inference from libraries becomes less trustworthy too. Prefer incremental adoption (per-file suppressions, or scoped `strict` via project references) over a blanket disable.

---

## Part 3 — Advanced Types & Generics

### 3.1 Generics & generic constraints

```typescript
function identity<T>(arg: T): T { return arg; }

function getLength<T extends { length: number }>(arg: T) { return arg.length; }
```

**Generic defaults and inter-referencing constraints** (common in typed HTTP clients / reducers):

```typescript
function getField<T, K extends keyof T = keyof T>(obj: T, key: K): T[K] {
  return obj[key];
}
```

**Dealer Dashboard example — a typed repository:**

```typescript
interface Repository<T extends { id: string }> {
  getById(id: string): Promise<T | undefined>;
  save(entity: T): Promise<void>;
}

class VehicleRepository implements Repository<Vehicle> {
  async getById(id: string) { /* ... */ return undefined; }
  async save(v: Vehicle) { /* ... */ }
}
```

### 3.2 Generic variance (covariance / contravariance)

Valuable to raise explicitly if you're coming from C#, since it bridges directly to `in`/`out` variance annotations (`IEnumerable<out T>`, `IComparer<in T>`).

- **Covariance:** if `Dog` is a subtype of `Animal`, is `Dog[]` a subtype of `Animal[]`? TypeScript says **yes** for arrays and return positions — technically unsound (you could push a `Cat` into what the compiler thinks is `Dog[]`), but pragmatic and matches real JS usage.
- **Contravariance:** function *parameter* types should narrow safely in the opposite direction. Under `strictFunctionTypes`, standalone function types are checked contravariantly, catching the unsound case:

```typescript
type AnimalHandler = (a: Animal) => void;
type DogHandler = (d: Dog) => void;

let handler: AnimalHandler;
let dogHandler: DogHandler = (d) => console.log(d.bark());
// handler = dogHandler; // unsound without strictFunctionTypes — handler could now be called with a Cat
```

**Known quirk:** method syntax (`interface X { fn(a: Animal): void }`) is checked *bivariantly* even under `strictFunctionTypes`, for backward-compatibility reasons, while property syntax with a function type (`interface X { fn: (a: Animal) => void }`) is checked *contravariantly* (strictly). This is an obscure but real distinguishing question at senior level.

Unlike C#, TypeScript has **no explicit `in`/`out` variance annotations for user generics** — variance is structurally *inferred* from how the type parameter is used, not explicitly declared.

### 3.3 `keyof`, `typeof`, and indexed access types

```typescript
type Vehicle = { vin: string; model: string; price: number };
type VehicleKeys = keyof Vehicle; // "vin" | "model" | "price"

let sample = { model: 'Sedan', year: 2024 };
type SampleType = typeof sample;

type ModelType = Vehicle['model'];           // string
type AllValues = Vehicle[keyof Vehicle];      // string | number
```

### 3.4 Mapped types

```typescript
type ReadonlyVehicle = { readonly [K in keyof Vehicle]: Vehicle[K] };
```

**Key remapping with `as`, and `+`/`-` modifiers** (TS 4.1+):

```typescript
type Mutable<T> = { -readonly [K in keyof T]-?: T[K] };

type Getters<T> = { [K in keyof T as `get${Capitalize<string & K>}`]: () => T[K] };

interface Dealer { name: string; region: string; }
type DealerGetters = Getters<Dealer>;
// { getName: () => string; getRegion: () => string }
```

### 3.5 Conditional types & `infer`

```typescript
type IsString<T> = T extends string ? 'yes' : 'no';

type ReturnTypeOf<T> = T extends (...args: any[]) => infer R ? R : never;
```

**Distributive conditional types** — a frequently-tested subtlety. When the checked type is a *naked* type parameter, TS distributes the conditional over each union member:

```typescript
type ToArray<T> = T extends any ? T[] : never;
type Result = ToArray<string | number>; // string[] | number[]  (distributed)

type ToArrayNonDist<T> = [T] extends [any] ? T[] : never;
type Result2 = ToArrayNonDist<string | number>; // (string | number)[]  (wrapping opts out)
```

This is exactly how built-in `Exclude`/`Extract`/`NonNullable` are implemented — understanding it explains *why* those utilities work, not just what they do.

### 3.6 Template literal types

```typescript
type EventName = 'click' | 'hover' | 'focus';
type HandlerName = `on${Capitalize<EventName>}`;
// "onClick" | "onHover" | "onFocus"

type ExtractParams<T extends string> =
  T extends `${string}:${infer Param}/${infer Rest}`
    ? Param | ExtractParams<Rest>
    : T extends `${string}:${infer Param}`
      ? Param
      : never;

type Params = ExtractParams<'/dealers/:dealerId/vehicles/:vin'>; // "dealerId" | "vin"
```

### 3.7 Recursive type depth limits (`TS2589`)

Every senior TS engineer eventually hits this when writing a "clever" recursive type (deep params extractor, `DeepPartial`, JSON-path types).

**Why it happens:** the compiler evaluates conditional/recursive types by expansion, with a hard internal recursion-depth limit (roughly ~50 instantiation levels) to protect against genuinely infinite type-level recursion. A naive recursive counter/concatenation type over a large or unbounded structure crosses that limit.

**Mitigation strategies:**
1. **Cap recursion depth explicitly:**
   ```typescript
   type DeepPartialBounded<T, Depth extends number = 5> =
     Depth extends 0 ? T
     : T extends object ? { [K in keyof T]?: DeepPartialBounded<T[K], Prev[Depth]> }
     : T;
   ```
2. **Break circularity explicitly** — for genuinely circular graphs (`Order`/`Customer`), model the flat DTO shape you actually need (`OrderSummary` with `customerId: string`) instead of deep-transforming the full circular domain graph.
3. **Prefer pre-built, well-tested utility types** (`Partial`, `Pick`, or a vetted library like `type-fest`) over hand-rolled unbounded recursion.
4. **Simplify the base case** — wrapping in a tuple (`[T] extends [U]`) to opt out of distribution can dramatically cut the number of instantiations.

### 3.8 Utility types deep dive

| Utility | Effect | Simplified implementation |
|---|---|---|
| `Partial<T>` | All properties optional | `{ [K in keyof T]?: T[K] }` |
| `Required<T>` | All properties mandatory | `{ [K in keyof T]-?: T[K] }` |
| `Readonly<T>` | All properties readonly | `{ readonly [K in keyof T]: T[K] }` |
| `Pick<T, K>` | Select a subset of keys | `{ [P in K]: T[P] }` |
| `Omit<T, K>` | Remove a subset of keys | `Pick<T, Exclude<keyof T, K>>` |
| `Record<K, T>` | Build object type from key union + value type | `{ [P in K]: T }` |
| `NonNullable<T>` | Strip `null`/`undefined` | `T extends null \| undefined ? never : T` |
| `Extract<T, U>` | Keep union members assignable to `U` | `T extends U ? T : never` |
| `Exclude<T, U>` | Remove union members assignable to `U` | `T extends U ? never : T` |
| `ReturnType<T>` | Function's return type | `T extends (...a: any[]) => infer R ? R : never` |
| `Parameters<T>` | Function's parameter tuple | `T extends (...a: infer P) => any ? P : never` |
| `InstanceType<T>` | Instance type of a constructor | `T extends new (...a: any[]) => infer R ? R : any` |

Interviewers often ask you to **implement one of these from scratch** — know the right-hand column, not just the names.

```typescript
interface Vehicle { vin: string; model: string; price: number; discount?: number; }

type VehicleSummary = Pick<Vehicle, 'vin' | 'model'>;
type VehicleNoDiscount = Omit<Vehicle, 'discount'>;
type VehicleRegistry = Record<string, Vehicle>;
```

**Utility types often missed:**

```typescript
// Awaited<T> — unwraps nested Promise types (critical since TS 4.5)
async function fetchVehicle(): Promise<Vehicle> { return {} as Vehicle; }
type Fetched = Awaited<ReturnType<typeof fetchVehicle>>; // Vehicle, not Promise<Vehicle>

// DeepPartial isn't built in — a common "write this" task
type DeepPartial<T> = T extends object ? { [K in keyof T]?: DeepPartial<T[K]> } : T;
```

### 3.9 `as const` and `satisfies` 🎯

```typescript
const statuses = ['pending', 'approved', 'rejected'] as const;
// readonly ["pending", "approved", "rejected"]

const dealer = { name: 'Downtown Motors', region: 'North' } satisfies { name: string; region: string };
```

**Why `satisfies` matters — and how it differs from a type annotation:**

```typescript
// Annotation WIDENS the variable — you lose literal info
const config1: Record<string, string | number> = { retries: 3, mode: 'fast' };
config1.retries; // type: string | number (widened)

// satisfies VALIDATES against the type but KEEPS the narrower inferred type
const config2 = { retries: 3, mode: 'fast' } satisfies Record<string, string | number>;
config2.retries; // type: number (preserved!)
```

`satisfies` gives you both: compile-time validation that the shape conforms to a contract, *and* precise, narrow inferred types for downstream use. It's now the idiomatic way to type constant config objects.

### 3.10 Ambient declarations, module augmentation & declaration merging

```typescript
// Ambient declaration (.d.ts)
declare function jQuery(selector: string): any;

// Module augmentation — extending a third-party library's types
declare module 'some-library' {
  interface SomeInterface { newMethod(): void; }
}

// Declaration merging
interface Window { dealerConfig: string; }
interface Window { featureFlags: string[]; }
// Window now has both properties merged.
```

Triple-slash directives are largely legacy in modern ESM projects. The modern way to extend third-party types (e.g. adding a custom property to Express's `Request` in an Angular Universal backend) is **module augmentation**.

### 3.11 Decorators & metadata (Angular relevance) 🎯

Decorators are functions applied to classes/members/parameters at declaration time via `@expression` syntax, enabling declarative metadata and behavior injection.

```typescript
function Log(target: any, propertyKey: string, descriptor: PropertyDescriptor) {
  const original = descriptor.value;
  descriptor.value = function (...args: any[]) {
    console.log(`Calling ${propertyKey} with`, args);
    return original.apply(this, args);
  };
  return descriptor;
}

class PricingService {
  @Log
  calculateDiscount(base: number) { return base * 0.9; }
}
```

**Key points:**
- Angular's decorators (`@Component`, `@NgModule`, `@Injectable`, `@Input`) rely on **`reflect-metadata`** plus the `emitDecoratorMetadata`/`experimentalDecorators` compiler flags to implement dependency injection — DI resolves constructor parameter types at runtime from emitted metadata. This is *why* Angular DI silently breaks if you use an interface (erased at compile time, no runtime trace) as an injection token instead of a class or `InjectionToken`.
- **Standards-track decorators** (TC39 Stage 3, default in TypeScript 5.0+) have **different runtime semantics** than the legacy `experimentalDecorators: true` model Angular has historically used. This is a live migration concern — new decorators are plain functions receiving `(value, context)` instead of `(target, key, descriptor)`, don't rely on `reflect-metadata`, and compose differently with class fields. Mixing legacy and standard decorator semantics in one project/library boundary causes subtle bugs.

### 3.12 Module resolution: ESM vs CommonJS

| Aspect | CommonJS (CJS) | ES Modules (ESM) |
|---|---|---|
| Syntax | `require()` / `module.exports` | `import` / `export` |
| Loading | Synchronous, runtime-resolved | Static, analyzable at compile time (enables tree-shaking) |
| `tsconfig` setting | `"module": "CommonJS"` | `"module": "ESNext"` / `"NodeNext"` / `"Bundler"` |

**Practical gotchas:**
- For an Angular/Vite frontend, pick `moduleResolution: "Bundler"` (lets the bundler handle resolution). For an actual Node.js backend service, pick `"NodeNext"` to match Node's real ESM runtime rules.
- Tree-shaking needs **static, analyzable ESM `import`/`export`** — dynamic `require()` or CommonJS re-exports defeat it, which is why library authors are pushed toward ESM-only or dual builds.
- Angular CLI (esbuild-based since v17) expects ESM throughout the build graph; a transitive CommonJS dependency triggers the well-known `CommonJS or AMD dependencies can cause optimization bailouts` warning.

---

## Part 4 — Object-Oriented TypeScript

### 4.1 Access modifiers

```typescript
class Dealer {
  public name: string;
  private taxId: string;
  protected region: string;
  constructor(name: string, taxId: string, region: string) {
    this.name = name; this.taxId = taxId; this.region = region;
  }
}
```

| Modifier | Access scope |
|---|---|
| `public` (default) | Accessible anywhere |
| `private` | Only inside the same class |
| `protected` | Class and subclasses |

**Senior nuance vs C#:** TS's `private`/`protected` are **compile-time only** — erased in the emitted JS, so runtime code can access `instance['taxId']` via bracket notation, or see it via `JSON.stringify`. For *true* runtime privacy, use native JS private fields (`#field`), enforced by the JS engine itself:

```typescript
class Account {
  #balance = 0; // truly private at runtime, invisible even to Object.keys()
  deposit(amount: number) { this.#balance += amount; }
}
```

### 4.2 Inheritance, overriding, `super`

```typescript
class Vehicle {
  move() { console.log('Moving...'); }
}
class ElectricVehicle extends Vehicle {
  charge() { console.log('Charging...'); }
}
```

**The `override` keyword (TS 4.3+):** without it, renaming or removing a base-class method silently orphans a subclass "override" — it just becomes an unrelated new method. Enable `noImplicitOverride` in `tsconfig.json` to force explicit annotations:

```typescript
class PremiumDealer extends Dealer {
  override greet() {      // compiler verifies Dealer.greet actually exists
    super.greet();
  }
}
```

### 4.3 Abstract classes vs interfaces

| Feature | Abstract class | Interface |
|---|---|---|
| Methods | Concrete and abstract | Signatures only |
| Instantiation | Cannot | Cannot (not even a runtime "type") |
| Properties | Can have defaults/init logic | Type definitions only |
| Multiple inheritance | No (single class inheritance) | Yes (`interface C extends A, B`) |
| Runtime existence | Yes — a real JS class in emitted output | No — fully erased |

**Interview framing:** choose an abstract class when subclasses share real reusable implementation (template-method pattern). Choose an interface for a pure contract, especially when multiple unrelated classes need to satisfy it.

### 4.4 Multiple interface implementation & extension

```typescript
interface HasVin { vin: string; }
interface HasPrice { price: number; }
interface Sellable extends HasVin, HasPrice { listedDate: Date; }

interface Auditable { audit(): void; }
interface Notifiable { notify(): void; }
class Order implements Auditable, Notifiable {
  audit() { /* ... */ }
  notify() { /* ... */ }
}
```

**Gotcha:** `implements` only checks the **public** shape — it doesn't verify private member behavior, and it's not a runtime check (fully erased). Don't confuse `implements` (compile-time contract check) with actually inheriting behavior.

### 4.5 Mixins

```typescript
function Auditable<T extends new (...args: any[]) => {}>(Base: T) {
  return class extends Base {
    logAction(action: string) { console.log('Audit:', action); }
  };
}
class Order {}
const AuditableOrder = Auditable(Order);
```

Mixins are TS/JS's answer to the lack of multiple class inheritance — conceptually similar to C# extension methods or default interface methods, but implemented as runtime class-factory functions rather than a language feature.

### 4.6 Private constructors & singletons

**Senior nuance:** in Angular, prefer DI-scoped singletons (`@Injectable({ providedIn: 'root' })`) over the classic GoF singleton pattern. Angular's injector already guarantees one instance per scope, is testable (mockable via DI), and avoids hidden global state.

```typescript
@Injectable({ providedIn: 'root' })
export class InventoryCacheService {
  private cache = new Map<string, Vehicle>();
}
```

### 4.7 Index signatures

```typescript
interface Translations { [key: string]: string; }
```

Pair with `noUncheckedIndexedAccess`: without it, `translations['missingKey']` types as `string` even though it's actually `undefined` at runtime — a very common source of `undefined.toUpperCase()`-style production crashes.

### 4.8 The `this` type & fluent APIs

```typescript
class QueryBuilder {
  withStatus(status: string): this { /* ... */ return this; }
}
```

A method returning `this` correctly returns the *subclass* type when called on a subclass instance — this is what makes fluent/chainable builder APIs subclass-safe.

### 4.9 Parameter properties shorthand

```typescript
class VehicleService {
  // Declares AND assigns `http`/`router` as private fields in one line
  constructor(private http: HttpClient, private router: Router) {}
}
```

Pure syntax sugar over declaring the field and assigning it in the constructor body — but it's the dominant style in Angular/NestJS constructor DI.

---

## Part 5 — Error Handling

### 5.1 `try`/`catch`/`finally` & custom errors

```typescript
class ApiError extends Error {
  constructor(message: string, public statusCode: number) {
    super(message);
    this.name = 'ApiError';
  }
}
```

**Gotcha carried over from JS:** subclassing `Error` when compiling to older JS targets can break `instanceof` checks against the custom error class, due to how class inheritance downlevels to ES5. On `target: ES2015` or newer this isn't an issue; verify for your specific target if unsure.

### 5.2 Typed catch clauses (`unknown` in catch) 🎯

Since TypeScript 4.4, under `strict` (specifically `useUnknownInCatchVariables`), `catch (error)` types `error` as `unknown`, not `any` — because JS allows throwing *any* value (`throw "a string"`, `throw 42`, `throw { code: 500 }` are all valid).

```typescript
try {
  riskyOperation();
} catch (error: unknown) {
  if (error instanceof Error) {
    console.log(error.message); // safe — narrowed
  } else {
    console.log('Unknown error', error);
  }
}
```

For APIs that throw structured non-`Error` values (common with some HTTP clients), pair with a type guard:

```typescript
interface ApiErrorShape { code: number; message: string; }
function isApiErrorShape(e: unknown): e is ApiErrorShape {
  return typeof e === 'object' && e !== null && 'code' in e && 'message' in e;
}

try {
  await callApi();
} catch (error: unknown) {
  if (isApiErrorShape(error)) {
    console.error(`API error ${error.code}: ${error.message}`);
  } else if (error instanceof Error) {
    console.error(error.message);
  } else {
    console.error('Unexpected throw value', error);
  }
}
```

---

## Part 6 — Performance

### 6.1 Compiler performance & type-checking cost

- **Project references** (`references` + `composite: true`) split a large codebase into independently type-checked/cached projects, enabling incremental builds — critical in Nx/Angular monorepos.
- **`skipLibCheck: true`** skips type-checking `.d.ts` files in `node_modules`, often significantly cutting build time, at the cost of not catching errors that originate purely in third-party type definitions.
- Deep conditional/recursive types can measurably slow the type checker or hit its recursion-depth limit — a known real-world cost of "clever" type-level programming.
- **`isolatedModules: true`** is required by single-file transpilers (esbuild, swc, Babel) that transpile files independently without full program type information — it forbids constructs that need cross-file type knowledge (e.g. re-exporting a type without `export type`).

### 6.2 Runtime performance: erasure, enums & bundle size

- **Types are 100% erased at runtime** — using types, generics, interfaces, or type aliases has zero runtime cost.
- **Constructs that DO have runtime cost:** non-`const` enums (emit an object + reverse mapping), decorators + `reflect-metadata`, namespaces (emit IIFE wrappers). Parameter properties are negligible (just a trivial constructor assignment).
- Prefer literal unions over enums, and plain interfaces/types over classes when you only need compile-time shape — every `class` emits real constructor/prototype JS, while `interface`/`type` emit nothing.

---

## Part 7 — Best Practices Cheat Sheet

- Turn on `strict` from day one (ideally plus `noUncheckedIndexedAccess` and `exactOptionalPropertyTypes`) — retrofitting strictness later is far more expensive.
- Prefer `unknown` over `any` at every I/O boundary (HTTP responses, `JSON.parse`, third-party callbacks); narrow explicitly before use.
- Prefer literal union types over `enum` for new code; reserve `const enum` only when you fully control the build pipeline and don't need ESM/isolatedModules compatibility.
- Use discriminated unions + exhaustiveness (`never`) checks for any domain modeling with multiple variants (API responses, NgRx actions, state machines).
- Use `satisfies` instead of a type annotation for typed constant objects/config, to preserve literal type precision.
- Validate untrusted data at runtime with a schema library (`zod`, `io-ts`, class-validator) rather than trusting a type assertion — types don't exist at runtime and can't protect you from malformed payloads.
- Keep function/module boundary types explicit (parameters and exported return types); let inference handle the rest internally.
- Treat `as` assertions as a last resort; never chain `as unknown as T` without a comment justifying why no safer narrowing was possible.
- Use `readonly`/`Readonly<T>` on inputs you don't intend to mutate, and prefer immutable update patterns (spread, `structuredClone`, immer) — especially in Angular's change-detection-sensitive code.
- Enable `noImplicitOverride` on any class hierarchy to catch silent override drift.

---

## Part 8 — Common Pitfalls

- **Treating `any` as "the fix for the red squiggly"** instead of using `unknown` + narrowing.
- **Assuming `readonly` is deep** — it's shallow; nested objects/arrays stay mutable.
- **Forgetting exhaustiveness checks on discriminated unions** — a new variant silently compiles and produces `undefined` at runtime instead of a compile error.
- **Confusing `??` with `||`** — `||` incorrectly treats `0`, `""`, `false` as "missing."
- **Excess property check blind spot** — assigning through an intermediate variable bypasses the literal-freshness check that would otherwise catch a typo'd property name.
- **Relying on `private`/`protected` for real security** — they're compile-time only; use `#privateFields` if runtime enforcement matters.
- **`const enum` + `isolatedModules`/modern bundlers** — incompatible with many current build setups (esbuild/swc, Angular's esbuild builder).
- **Assuming index signatures guarantee presence** — without `noUncheckedIndexedAccess`, `dict[key]` types as `T`, not `T | undefined`, hiding real `undefined` risk.
- **Assuming caught errors are always `Error` instances** — JS allows throwing any value; code that does `error.message` without narrowing will crash on non-`Error` throws.
- **Mixing legacy and standard decorator semantics** across a project/library boundary.
- **Over-engineering type-level logic** (deeply recursive conditional types) that technically works but tanks IDE responsiveness and compiler performance for marginal type-safety gain.

---

## Part 9 — Rapid-Fire Interview Q&A

**Q: For a function that parses an external API response, why choose `unknown` over `any`?**
A: `any` disables type checking entirely, so any property access or method call on the parsed result compiles even if wrong, deferring the bug to runtime. `unknown` forces the caller to narrow before doing anything with the value — catching unsafe usage of unvalidated external data at exactly the boundary where malformed payloads cause the most, and the costliest, bugs.

**Q: How do you guarantee a `switch` over a discriminated union stays exhaustive as new variants are added?**
A: Add a `default` branch that assigns the narrowed remaining value to a variable typed `never`. If every case is handled, the compiler has narrowed the union to nothing by the time it reaches `default`, so the assignment type-checks. If a new variant is added and a case is missed, the unhandled variant remains in the type at `default`, and assigning it to `never` becomes a compile error — turning a silent runtime bug into a build failure.

**Q: What's the practical difference between `interface` and `type` today, and which do you default to?**
A: Both describe object shapes and are largely structurally interchangeable — an interface can extend a type alias and vice versa. Real differences: interfaces support declaration merging and `extends`-based multiple inheritance; type aliases can express unions, tuples, and mapped/conditional types that interfaces can't. Default to `interface` for object/class contracts meant to be extended or merged (public DTOs, Angular component inputs), and `type` for unions, function signatures, and type-level utilities.

**Q: Your team wants to disable `strictNullChecks` to speed up a legacy migration. What's your pushback?**
A: It's the single highest-value strict flag — without it, `null`/`undefined` are implicitly assignable to every type, so the compiler can't distinguish a guaranteed value from a possibly-missing one anywhere in the app. Disabling it reintroduces the exact `TypeError: cannot read property of undefined` bug class across the *entire* codebase, not just migrated files, and most modern `.d.ts` files from `node_modules` assume it's on, making library inference less trustworthy too. Prefer incremental, scoped adoption over a blanket disable.

**Q: Explain structural typing and give an example of a surprising result it causes.**
A: TypeScript compares types by shape, not by declared name — any object with a compatible set of members satisfies a type, regardless of what it claims to implement. Surprising case: excess property checks only apply to object *literals* assigned directly into a typed location; assign the literal to a variable first and pass that variable instead, and the check doesn't fire — an extra or typo'd property silently slips through, because it's now a structural assignability check, not a literal-freshness check.

**Q: How do generics in TypeScript differ from C#, and where does that matter in practice?**
A: C# generics are reified — the CLR knows the concrete type argument at runtime (`typeof(T)` works, `is T` checks work per closed generic type). TypeScript generics are fully erased at compile time — `T` doesn't exist at runtime, so runtime type-checking against a generic parameter, or patterns like `new T()`, aren't directly possible; you need an explicit constructor reference or a runtime discriminant passed as a value. This matters when porting patterns like generic factories or repository base classes from C# — the TS equivalent needs an explicit constructor parameter (`new (...args: any[]) => T`).

**Q: What's the risk of using `enum` in a modern Angular app built with esbuild, and what do you use instead?**
A: Regular (non-const) enums emit real runtime objects with reverse mappings, adding to bundle size and defeating tree-shaking. `const enum` avoids this by inlining values at compile time, but is incompatible with `isolatedModules`, which esbuild-based builds (including Angular's esbuild builder since v17) require. The idiomatic replacement is a literal union type, optionally paired with an `as const satisfies Record<...>` object for an iterable value list — same type safety, zero runtime cost, full build-tool compatibility.

---

## Quick Revision Sheet

- **`any`** = checking off. **`unknown`** = checking on, narrow before use. **`never`** = unreachable/bottom type. **`void`** = no meaningful return.
- **`interface`**: mergeable, `extends`-based, object contracts. **`type`**: unions/tuples/mapped types, not mergeable.
- **`readonly`** is shallow — nested data still mutable.
- **Structural typing**: shape matters, not declared name. Excess-property checks only fire on literals, not variables.
- **Branded types**: `string & { __brand: 'X' }` + constructor function = simulated nominal typing.
- **Discriminated unions + `never` exhaustiveness check** = compile-time safety net for adding new variants.
- **`??`** respects `0`/`""`/`false`; **`||`** does not.
- **`strict: true`** turns on `strictNullChecks`, `noImplicitAny`, `strictFunctionTypes`, `strictBindCallApply`, `strictPropertyInitialization`, `noImplicitThis`, `useUnknownInCatchVariables` — but NOT `noUncheckedIndexedAccess` or `exactOptionalPropertyTypes`, which you should add separately.
- **`satisfies`** validates against a type while keeping the narrow inferred type; a plain annotation widens.
- **Enums** have runtime cost and break with `isolatedModules`; prefer literal unions.
- **`catch (e)`** is `unknown` under `strict` — always narrow with `instanceof Error` or a type guard before using `.message`.
- **Angular DI** depends on `reflect-metadata` + decorator metadata — never use an interface as an injection token (it's erased at runtime); use a class or `InjectionToken`.
- **Types are 100% erased at runtime** — zero cost, except for enums, decorators+metadata, and namespaces, which do emit real JS.
- **`TS2589`** (recursive type too deep) → bound the recursion depth, break circularity, or prefer built-in/vetted utility types.
