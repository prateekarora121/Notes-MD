# TypeScript + Angular Interview Guide (Senior Full-Stack, 10+ YOE)

> Interview prep ke liye rewrite kiya gaya hai. Har topic same pattern follow karta hai: **simple explanation → code example → common mistakes → interview Q&A (tricky follow-ups sahit) → one-line revision summary.**
> Examples ek **Dealer Management Dashboard** (Angular 19) se liye gaye hain — entities jaise `Vehicle`, `Dealer`, `SalesOrder`, `Inventory` — taaki patterns directly real work par map ho, toy examples nahi.

---

## Is guide ko kaise use karein

- Interview se pehle raat ko **Quick Revision Sheet** (last section) skim kar lo.
- Pehli baar top se bottom tak padho; repeat passes mein sirf **Interview Q&A** aur **Common Mistakes** boxes padho.
- 🎯 mark wale questions senior-level TypeScript/Angular interviews mein baar-baar aate hain.

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

### 1.1 TypeScript kya hai, aur ise kyun use karein?

**Simple explanation:** TypeScript, Microsoft dwara banaya gaya JavaScript ka ek typed superset hai. Yeh plain JavaScript mein compile ("transpile") ho jaata hai — types sirf compile time par exist karte hain aur runtime par completely erase ho jaate hain.

**Teams isko kyun use karti hain:**
- **Compile time par hi bugs ki poori class catch ho jaati hai** — galat property names, galat argument shapes, null/undefined ka misuse — production mein jaane ke bajaye.
- **Large refactors ko safe banata hai.** 500-file wale Angular app mein ek field rename karna mechanically safe hai kyunki compiler har broken usage ko flag kar deta hai.
- **Better developer experience** — IntelliSense, "go to definition," inline docs.
- **Self-documenting APIs** — ek function signature, implementation padhe bina hi contract bata deta hai.

**Senior-level nuance (interviewers isko probe karte hain):** TypeScript ka type system kaafi jagah **jaan-boojh kar unsound** hai — `any`, type assertions, `noUncheckedIndexedAccess` ke bina array index access, bivariant method parameters. Yeh JavaScript ke saath compatible rehne ke liye ek pragmatic trade-off hai. Ready raho yeh batane ke liye ki *kahan* yeh unsound hai aur ise kaise tighten karein (dekhein `strict` flags, §2.11).

🎯 **Q: Kya TypeScript aapke code ko runtime par faster banata hai?**
A: Nahi — types 100% erase ho jaate hain. Runtime cost sirf un constructs se aata hai jo real JS emit karte hain: non-`const` enums, decorators + `reflect-metadata`, namespaces. Plain interfaces/types ka koi cost nahi hota.

### 1.2 Toolchain basics

```bash
npm install -g typescript
tsc -v                # version check karo
tsc filename.ts        # filename.js compile hoti hai
```

Ek minimal `tsconfig.json`:

```json
{ "compilerOptions": { "target": "ES6", "strict": true } }
```

Ek **realistic** config Angular 19 project ke liye (yeh wahi hai jise interview mein defend karna padta hai):

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

Senior level par interviewer expect karta hai ki aapko har flag ka *"kyun"* pata ho, sirf yeh nahi ki `strict: true` exist karta hai. Neeche har option ko example ke saath explain kiya gaya hai — yeh ek bahut common interview area hai ("apna `tsconfig` explain karo").

#### `compilerOptions` — field by field breakdown

**`target: "ES2022"`**
Yeh decide karta hai ki TypeScript kis JavaScript version mein compile (downlevel) karega. `ES2022` ka matlab hai output mein modern JS features (class fields, top-level `await`, etc.) directly use ho sakti hain, purane syntax mein convert kiye bina.

```typescript
class Dealer {
  region = 'North'; // ES2022 target par yeh native class field syntax mein hi emit hota hai
}
```

Agar aap `target: "ES5"` set karte, to compiler class ko function/prototype pattern mein rewrite kar deta (zyada, uglier output code) taaki purane browsers support ho sakein. Angular 19 modern browsers target karta hai, isliye `ES2022` sahi choice hai — chhota, faster bundle.

**`module: "ESNext"`**
Yeh control karta hai ki compiled output mein `import`/`export` statements kis module system mein emit hongi. `ESNext` ka matlab hai native ES module syntax as-is rakha jaata hai (bundler ise handle karega), `CommonJS` (`require`/`module.exports`) mein convert nahi kiya jaata.

```typescript
// Source:
import { VehicleService } from './vehicle.service';
// module: "ESNext" ke saath, output mein yeh import statement waisa hi rehta hai
// module: "CommonJS" ke saath, output ban jaata: const { VehicleService } = require('./vehicle.service');
```

ESM static hota hai (compile time par analyzable), isliye tree-shaking enable karta hai — Angular bundle size ke liye critical.

**`moduleResolution: "Bundler"`**
Yeh algorithm decide karta hai ki TypeScript `import './x'` jaisi statement ko actual file kaise dhoondhta hai. `"Bundler"` mode (TS 5.0+) esbuild/Vite jaise modern bundlers ka behavior replicate karta hai — Node ke strict ESM extension rules ke bina `package.json` ka `exports` field follow karta hai.

```typescript
import { formatCurrency } from 'my-utils';
// "Bundler" resolution: my-utils ke package.json ke "exports" map ko dekhega,
// bina yeh demand kiye ki relative imports mein ".js" extension explicitly ho
```

Angular/Vite frontend ke liye `"Bundler"` sahi hai; actual Node.js backend service ke liye `"NodeNext"` chahiye hota (Node ke real runtime rules match karne ke liye).

**`lib: ["ES2022", "DOM"]`**
Yeh batata hai ki kaunse built-in type declarations include karni hain — yeh JS ka actual output nahi badalta, sirf yeh batata hai ki compiler ko kaunse globals/APIs ke bare mein pata hona chahiye.

```typescript
// "DOM" included hai, isliye yeh compile hota hai:
document.querySelector('.vehicle-card');

// "DOM" hata do (jaise ek pure Node backend project mein), aur upar wali line
// compile error degi: "Cannot find name 'document'."
```

`ES2022` array/object methods jaise `Array.prototype.at()` ke liye type definitions deta hai; `DOM` browser APIs (`document`, `window`, `fetch`) ke liye deta hai — Angular frontend code ko dono chahiye.

**`strict: true`**
Yeh ek single flag hai jo strict type-checking flags ka poora bundle on kar deta hai (`strictNullChecks`, `noImplicitAny`, `strictFunctionTypes`, wagairah — poori list §2.11 mein hai). Yeh "TypeScript ko actually strict banane wala" master switch hai.

```typescript
// strict: false ke saath, yeh silently compile ho jaata hai:
function greet(name) { return 'Hello ' + name; } // name implicitly 'any'

// strict: true ke saath, yeh error deta hai:
// "Parameter 'name' implicitly has an 'any' type."
function greet(name: string) { return 'Hello ' + name; } // explicit type chahiye
```

**`noUncheckedIndexedAccess: true`**
`strict` mein *included nahi* hai, lekin senior configs mein essential hai. Yeh array index ya object index access ke result mein `| undefined` add kar deta hai, kyunki index par kuch exist karta hai yeh guarantee nahi hoti.

```typescript
const vehicles: Vehicle[] = [];
const first = vehicles[0];
// noUncheckedIndexedAccess: false → first ka type Vehicle hai (galat! array khaali hai)
// noUncheckedIndexedAccess: true  → first ka type Vehicle | undefined hai (sahi)

console.log(first.vin); // true wale case mein yeh compile error deta hai jab tak aap check na karo
```

Yeh production ke `Cannot read property 'vin' of undefined` jaise crashes ka ek bahut common source pकड़ता hai.

**`exactOptionalPropertyTypes: true`**
Yeh "key absent hai" ko "key present hai `undefined` value ke saath" se distinguish karta hai — subtle hai lekin API contracts ke liye matter karta hai.

```typescript
interface DealerFilter { region?: string; }

// exactOptionalPropertyTypes: false ke saath dono valid hain:
const f1: DealerFilter = {};                  // region completely absent
const f2: DealerFilter = { region: undefined }; // region present but undefined — same treated

// exactOptionalPropertyTypes: true ke saath, f2 error deta hai —
// "region" ko explicitly undefined assign karna optional-absent se different treat hota hai
```

**`esModuleInterop: true`**
Yeh CommonJS packages (jo `module.exports = ...` use karte hain) ko clean, modern `import` syntax ke saath import karna aasan banata hai, jaisे woh native ESM default exports hon.

```typescript
// esModuleInterop: false ke saath (purana, awkward tareeka):
import * as express from 'express';

// esModuleInterop: true ke saath (clean, modern tareeka):
import express from 'express';
```

**`skipLibCheck: true`**
Yeh `node_modules` ke andar `.d.ts` (type declaration) files ka type-checking **skip** kar deta hai, sirf aapke apne source code ka type-check karta hai. Build time significantly kam hota hai.

```
// Bina skipLibCheck ke: tsc kisi bhi third-party library ke internal .d.ts
// files mein type errors bhi check karta hai — bahut slow, aur aksar
// aapke control se bahar hote hain (library ka apna bug ho sakta hai)

// skipLibCheck: true ke saath: sirf aapki .ts files check hoti hain
```

Trade-off: agar ek purely third-party type-definition file mein hi koi type error hai, wo catch nahi hogi — lekin practically zyada tar teams ise "worth it" maanti hain.

**`isolatedModules: true`**
Yeh un constructs ko **forbid** karta hai jinhe correctly compile hone ke liye "poore program" ka type knowledge chahiye hota — kyunki esbuild/swc jaise modern transpilers har file ko **independently** (ek doosre se knowledge share kiye bina) transpile karte hain.

```typescript
// Yeh isolatedModules ke saath error degi (Angular ka esbuild builder is flag
// ko require karta hai):
type Vehicle = { vin: string };
export { Vehicle }; // ambiguous hai — yeh ek type hai ya value, single-file
                      // transpiler ko cross-file lookup ke bina pata nahi chal sakta

// Fix — explicitly bata do ki yeh type-only export hai:
export type { Vehicle };
```

**`forceConsistentCasingInFileNames: true`**
Yeh ensure karta hai ki har jagah ek file ko import karte waqt uska casing (upper/lowercase) exactly match kare — chahe aap Windows/macOS (jo case-insensitive filesystems hain) par develop kar rahe ho.

```typescript
// File actually disk par: vehicleService.ts

import { VehicleService } from './VehicleService'; // galat casing
// Mac/Windows par locally yeh chal jaata hai (case-insensitive filesystem)
// lekin Linux CI server par (case-sensitive) yeh build break kar deta hai —
// forceConsistentCasingInFileNames isko locally hi catch kar leta hai
```

---

### 1.3 Primitive & special types

- **Primitives:** `string`, `number`, `boolean`, `null`, `undefined`, `bigint`, `symbol`.
- **Special types:** `any`, `unknown`, `void`, `never`.

### 1.4 `any` vs `unknown` vs `void` vs `never` 🎯

Yeh four-way comparison sabse common TypeScript interview questions mein se ek hai.

| Type | Meaning | Doosre types mein **assign** ho sakta hai? | Doosre types **se** assign ho sakta hai? | Typical use |
|---|---|---|---|---|
| `any` | "Type checking band kar do" | Haan, kisi bhi type mein | Haan, kisi bhi type se | Legacy JS interop, last resort |
| `unknown` | "Kuch bhi ho sakta hai — use karne se pehle prove karo" | Nahi (narrowing/assertion ke bina) | Haan, kisi bhi type se | External/untrusted input: API responses, `JSON.parse`, catch blocks |
| `void` | "Koi meaningful return value nahi" | Sirf `void`/`any`/`undefined` mein | N/A (return position) | Callback/function return type |
| `never` | "Yeh path unreachable hai / koi value satisfy nahi kar sakti" | Har jagah assignable (bottom type) | `never` khud ke alawa kuch bhi nahi | Exhaustiveness checks, hamesha throw karne wale functions |

**Key gotcha:** `any` ek saath top type *aur* bottom type dono hai — isi wajah se yeh dangerous hai, aur TS 3.0 mein `unknown` ko type-safe alternative ke tor par introduce kiya gaya tha.

**Dealer Dashboard example:**

```typescript
async function fetchVehicleById(id: string): Promise<unknown> {
  const res = await fetch(`/api/vehicles/${id}`);
  return res.json(); // hume abhi shape genuinely pata nahi hai
}

function isVehicle(v: unknown): v is Vehicle {
  return typeof v === 'object' && v !== null && 'vin' in v && 'model' in v;
}

const raw = await fetchVehicleById('123');
if (isVehicle(raw)) {
  console.log(raw.model); // safe — Vehicle mein narrow ho gaya
}
```

**Common mistake:** `any` ko "red squiggly line fix karne ka escape hatch" samajhna, `unknown` + narrowing use karne ke bajaye — yeh exactly wahi bug class reintroduce karta hai jise prevent karne ke liye TypeScript exist karta hai.

🎯 **Follow-up: `unknown` kyun exist karta hai jab humare paas already `any` hai?**
A: `unknown` aapko force karta hai ki value ke saath kuch bhi karne se pehle narrow karo (`typeof`, ek type guard, ya `zod` jaisa schema validator), jisse type system sound rehta hai aur phir bhi I/O boundaries par "mujhe is type ka pata nahi" allow ho jaata hai.

### 1.5 Type inference vs type annotation

```typescript
let message = "Hello";      // string infer hua
let age: number = 25;       // explicitly annotate kiya
```

**Senior rule of thumb:** local variables ke liye inference prefer karo (kam noise, same safety), lekin **module boundaries** par — function parameters, exported return types, public class members — explicit annotations prefer karo. Boundary par inference implementation change hone par silently widen/narrow ho sakta hai, jisse consumers bina kisi visible signal ke break ho jaate hain.

### 1.6 Type assertions

```typescript
let someValue: any = "Hello";
let strLength: number = (someValue as string).length;
```

Assertions (`as`) compiler ko batate hain "trust me," aur **zero runtime validation** karte hain. `as unknown as T` ke through double assertion, TS ke assertion-compatibility check ko bhi sidestep kar deta hai — interviewers isko instantly pakad lete hain, ek red flag ke tor par. Jab bhi data ek runtime boundary cross kare (HTTP response, `localStorage`, third-party library), assertions ke bajaye type guards ya validation library (`zod`, `io-ts`) prefer karo.

### 1.7 Type aliases vs interfaces 🎯

| Feature | `interface` | `type` |
|---|---|---|
| Extending | `extends`, multiple inheritance support karta hai | Intersection (`&`) ke through, `extends` se nahi |
| Declaration merging | Haan — multiple `interface X {}` blocks merge ho jaate hain | Nahi |
| Object shapes | Haan | Haan |
| Unions/tuples/primitives | Nahi | Haan — `type T = string \| number` |
| Mapped/conditional types | Nahi | Haan |

**Practical default:** un object/class contracts ke liye `interface` use karo jinhe extend ya merge kiya ja sakta ho (public DTOs, Angular `@Input` bags). Unions, function signatures, aur type-level utilities ke liye `type` use karo. Dono structurally ek-doosre ke compatible hain — ek `interface` `type` alias ko extend kar sakta hai aur vice versa.

> Note: type aliases ko kabhi-kabhi "not extendable" describe kiya jaata hai. Yeh sirf `extends` keyword ke liye true hai — yeh intersections (`type C = A & B`) ke through fine compose hote hain, isliye yeh ek oversimplification hai, hard limitation nahi.

### 1.8 Optional & readonly properties

```typescript
interface Dealer {
  name: string;
  region?: string;          // optional
}

interface Vehicle {
  readonly vin: string;     // create hone ke baad reassign nahi ho sakti
}
```

**Gotcha:** `readonly` **shallow** hota hai. Yeh property ko khud *rebind* hone se rokta hai, lekin uske andar jo nested object/array point ho raha hai use mutate hone se nahi rokta. Deep immutability ke liye `Readonly<T>` ko recursively use karo, ya real update patterns ke liye Immer jaisi library use karo.

### 1.9 Function & method overloading

```typescript
function toDisplayPrice(amount: number): string;
function toDisplayPrice(amount: string): string;
function toDisplayPrice(amount: any): string {
  return `$${amount}`;
}
```

TS overloads purely ek **compile-time construct** hain — neeche ek hi real JS function hota hai (yeh "implementation signature" hoti hai, jo callers ko dikhti hi nahi). Yeh C# se fundamentally different hai, jahan overloads genuinely distinct methods hote hain jo CLR dwara compile time par resolve hote hain. TS mein aap ek single implementation ke liye multiple *call-site shapes* describe kar rahe ho, multiple methods create nahi kar rahe.

---

## Part 2 — Intermediate Type System

### 2.1 Regular functions vs arrow functions

| Feature | Regular function | Arrow function |
|---|---|---|
| `this` binding | Dynamic (caller par depend karta hai) | Lexical (enclosing scope se inherit) |
| `arguments` object | Available | Available nahi |
| Classes mein use | Un methods ke liye preferred jinhe override karna ho | Callbacks ke liye achha jinhe captured `this` chahiye (event handlers) |
| `new`-able | Haan | Nahi |

**Angular-relevant gotcha:** ek arrow-function class property (`onSave = () => {...}`) template event bindings ke liye `this` ko correctly bind karta hai, lekin **har instance ke liye ek naya function** create karta hai — scale par real memory cost, aur `@HostListener`-style decorator binding ko todta hai jisko ek real prototype method chahiye hoti hai.

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

**Gotcha:** same key ke liye conflicting property types wale do types ko intersect karna (jaise `{ a: string } & { a: number }`) us property ko `never` mein collapse kar deta hai — error **nahi** deta. Classic interview trap.

### 2.3 Tuples

```typescript
let coordinate: [number, number] = [28.6, 77.2];
let readonlyPair: readonly [number, number] = [10, 20];
```

**Labeled & variadic tuples** (modern TS):

```typescript
type Point = [x: number, y: number]; // labels sirf documentation hain, koi runtime effect nahi

type Concat<T extends unknown[], U extends unknown[]> = [...T, ...U];
type Combined = Concat<[1, 2], [3, 4]>; // [1, 2, 3, 4]
```

### 2.4 `readonly T[]` / `ReadonlyArray<T>` — defensive API design

**Problem yeh hai:** ek plain `T[]` parameter callers ko yeh nahi batata ki function unka array mutate karega ya nahi.

```typescript
function sortedTotal(prices: number[]): number {
  prices.sort((a, b) => a - b); // legal — lekin CALLER ka array mutate kar deta hai
  return prices.reduce((sum, p) => sum + p, 0);
}

const cart = [30, 10, 20];
sortedTotal(cart);
console.log(cart); // [10, 20, 30] — surprise mutation
```

**Fix yeh hai:**

```typescript
function sortedTotal(prices: readonly number[]): number {
  // prices.sort(...);  // compile error — mutating methods type se hata diye gaye
  return [...prices].sort((a, b) => a - b).reduce((sum, p) => sum + p, 0);
}
```

- `readonly T[]` aur `ReadonlyArray<T>` same type hai, do spellings.
- Yeh ek **compile-time-only contract** hai — koi `Object.freeze` involved nahi hai. Ek mutable `T[]`, `readonly T[]` parameter mein freely assign ho jaata hai, ulta kabhi nahi.
- Angular ke `OnPush`/immutable-state patterns ke saath naturally pair karta hai: `readonly T[]` accept karne ke liye typed ek function guaranteed hai ki accidental in-place mutation ka source nahi banega jo reference-equality change detection todhta hai.
- **Boundary case:** yeh shallow hai, exactly `Readonly<T>` ki tarah — `readonly Point[]` `push`/`arr[0] = ...` rokta hai, lekin `arr[0].x = 5` nahi rokta jab tak `Point` khud readonly na ho.

### 2.5 Enums — aur senior devs inhe kyun avoid karte hain

```typescript
enum OrderStatus { Pending, Approved, Rejected }
```

**Problems jo senior interviewers expect karte hain ki aap raise karo:**
- Enums real runtime JS objects generate karte hain (reverse-mapping objects), jo bundle size mein add hote hain.
- `const enum` values ko compile time par inline karke isse avoid karta hai — lekin yeh **`isolatedModules` ke saath incompatible hai**, jo modern bundlers (esbuild, swc, Vite, aur Angular ka esbuild-based builder v17 se) require karte hain.
- Yeh literal unions jitna cleanly tree-shake nahi hote.

**Modern replacement — literal union + optional `satisfies`-checked value map:**

```typescript
type OrderStatus = 'pending' | 'approved' | 'rejected';

const OrderStatusValues = {
  Pending: 'pending',
  Approved: 'approved',
  Rejected: 'rejected',
} as const satisfies Record<string, OrderStatus>;
```

Yeh exhaustiveness checking, zero runtime cost, aur esbuild-based Angular builds ke saath full compatibility deta hai.

### 2.6 Structural typing vs nominal typing 🎯

Yeh single most common "nominally-typed language se aa rahe ho" conceptual question hai.

- **C#/Java nominally typed hain** — identical members wali do classes bhi incompatible rehti hain jab tak koi ek explicitly doosre ko implement/extend na kare. Type identity *declared name* par based hoti hai.
- **TypeScript structurally typed hai** (duck typing, compile time par enforced) — do types compatible hote hain agar unki *shapes* match karti hain, naam ya declared relationship kuch bhi ho.

```typescript
interface DealerLocation { lat: number; lng: number; }
class MapPin { lat = 0; lng = 0; }

function plot(p: DealerLocation) { console.log(p.lat, p.lng); }

plot(new MapPin());        // OK — same shape
plot({ lat: 1, lng: 2 });  // OK — structurally match karta hai
```

**Mention karne layak practical consequences:**
- **Excess property checks** sirf **object literals** par fire hoti hain jo directly assign ho rahe hain, variables par nahi:
  ```typescript
  plot({ lat: 1, lng: 2, extra: true }); // error: 'extra' does not exist
  const p = { lat: 1, lng: 2, extra: true };
  plot(p); // fine compile ho jaata hai — structurally check hota hai, fresh literal ke tor par nahi
  ```
- `private`/`protected` members ek *nominal-ish* check mein participate karte hain — different declarations se identically-named private members wali do classes structurally compatible **nahi** maani jaati.
- `unique symbol` aur **branded/tagged types** TS ka idiomatic tareeka hain nominal typing simulate karne ka, jab aapko do structurally-identical types ko accidentally mix hone se rokna ho.

### 2.7 Branded (nominal) types — full worked example

**Problem yeh hai:** `DealerId` aur `VehicleId` type level par dono plain `string` hain, isliye TypeScript aapko khushi-khushi ek ko doosre ki jagah pass karne deta hai:

```typescript
type DealerId = string;
type VehicleId = string;

function getDealer(id: DealerId) { /* ... */ }

const vehicleId: VehicleId = 'veh_123';
getDealer(vehicleId); // compiles — silently wrong
```

**Fix yeh hai — ek branded type plus ek constructor function:**

```typescript
type DealerId = string & { readonly __brand: 'DealerId' };
type VehicleId = string & { readonly __brand: 'VehicleId' };

// Constructor function hi ek sanctioned tareeka hai ek branded value produce karne ka.
// Real validation yahan hoti hai.
function toDealerId(raw: string): DealerId {
  if (!raw) throw new Error('DealerId cannot be empty');
  return raw as DealerId; // codebase mein woh ek deliberate, encapsulated assertion
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
| `typeof` | `if (typeof x === "string")` | Sirf primitives |
| `instanceof` | `if (err instanceof Error)` | Classes |
| `in` operator | `if ("vin" in obj)` | Property presence |
| Equality narrowing | `if (status === "approved")` | Literal unions narrow karta hai |
| Discriminant property | `if (shape.kind === "circle")` | Neeche discriminated unions dekhein |
| User-defined type predicate | `function isVehicle(x): x is Vehicle` | Custom runtime validation logic ke liye sirf yehi mechanism hai |
| Assertion functions | `function assertDefined(x): asserts x is NonNullable<...>` | Call ke baad *enclosing scope ke baaki hisse ke liye* narrow karta hai, predicates ke ulat jo sirf ek conditional ke andar narrow karte hain |

```typescript
function assertIsDefined<T>(val: T): asserts val is NonNullable<T> {
  if (val === undefined || val === null) {
    throw new Error('Expected value to be defined');
  }
}

function process(vin?: string) {
  assertIsDefined(vin);
  console.log(vin.toUpperCase()); // function ke baaki hisse ke liye string mein narrow ho gaya
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

**Important follow-up:** jab koi `LeaseSale` variant add kare aur `total()` update karna bhool jaaye to kya hota hai? Exhaustiveness guard ke bina, yeh silently compile ho jaata hai aur runtime par `undefined` return karta hai. `never`-based guard se fix karo:

```typescript
interface LeaseSale { kind: 'lease'; amount: number; leaseTermMonths: number; }
type Sale = CashSale | FinancedSale | LeaseSale;

function total(sale: Sale): number {
  switch (sale.kind) {
    case 'cash': return sale.amount;
    case 'financed': return sale.amount;
    case 'lease': return sale.amount;
    default:
      // Agar upar koi case miss hai, to 'sale' yahan `never` NAHI hoga,
      // isliye yeh compile fail ho jaata hai.
      const _exhaustiveCheck: never = sale;
      return _exhaustiveCheck;
  }
}
```

Yeh pattern (discriminated union + `never` exhaustiveness check) TS seniority ka ek strongest signal hai — Dealer Dashboard mein API response types, NgRx actions, aur state machines (jaise `OrderState = 'draft' | 'submitted' | 'approved' | 'delivered'`) ke liye naturally fit hota hai.

### 2.10 Optional chaining & nullish coalescing

```typescript
console.log(dealer.address?.city);      // address missing ho to undefined
const displayName = dealer.name ?? 'Unnamed Dealer';
```

**Gotcha:** `??` `||` jaisa nahi hai. `0`, `""`, aur `false` `??` ke under valid values hain, lekin `||` ke under incorrectly replace ho jaate hain. Purane `||`-based default logic ko bina re-audit kiye migrate karte waqt ek classic bug (jaise, `discount: 0` wali vehicle ko `0` hi rakhna chahiye, default par fall back nahi karna chahiye).

### 2.11 `strictNullChecks` aur `strict` family of flags 🎯

`strict: true` ek bundle flag hai. Yeh kya-kya on karta hai:

| Flag | Effect | Real-world impact |
|---|---|---|
| `strictNullChecks` | `null`/`undefined` default se har type ka part nahi hote | Single sabse bada bug-catcher; explicit `T \| null` aur narrowing force karta hai |
| `noImplicitAny` | Inferred `any` par error deta hai | Silent type-safety holes ko prevent karta hai, especially untyped params par |
| `strictFunctionTypes` | Function parameters contravariantly check hote hain | Unsound function assignment ko prevent karta hai |
| `strictBindCallApply` | `.bind`/`.call`/`.apply` function signature ke against type-checked hote hain | In rarely-typed APIs par wrong-arg bugs catch karta hai |
| `strictPropertyInitialization` | Class properties ko constructor mein initialize karna hoga, ya explicitly `undefined` allow karna hoga | Common Angular pain point — DI-populated fields ko `!` ya constructor init chahiye |
| `noImplicitThis` | `this` implicitly `any` hone par error deta hai | Callbacks ke tor par use hone wale regular functions ke liye relevant |
| `useUnknownInCatchVariables` | `catch (e)` `unknown` type ka hota hai, `any` nahi | Safe error handling force karta hai — Part 5 dekhein |

**Flags jo `strict` mein included NAHI hain lekin real senior-grade configs mein essential hain:**
- `noUncheckedIndexedAccess` — `arr[i]`/`record[key]` ko `T` ke bajaye `T | undefined` return karata hai.
- `exactOptionalPropertyTypes` — "key absent ho sakti hai" ko "key present ho sakti hai `undefined` value ke saath" se distinguish karta hai.
- `noFallthroughCasesInSwitch` — `switch` mein missing `break`/`return` catch karta hai.

🎯 **Q: Aapki team ek legacy migration speed up karne ke liye `strictNullChecks` disable karna chahti hai. Aap kya push back karoge?**
A: Aap poori app ke har nullable path par compile-time guarantees lose kar dete ho, sirf migrated files par nahi — koi bhi possibly-null value ek latent `TypeError` ban jaata hai. Zyada tar modern third-party `.d.ts` files bhi assume karti hain ki yeh on hai, isliye library inference bhi kam trustworthy ho jaata hai. Incremental, scoped adoption prefer karo, blanket disable ke bajaye.

---

## Part 3 — Advanced Types & Generics

### 3.1 Generics & generic constraints

```typescript
function identity<T>(arg: T): T { return arg; }

function getLength<T extends { length: number }>(arg: T) { return arg.length; }
```

**Generic defaults aur ek-doosre ko reference karne wale constraints** (typed HTTP clients/reducers mein common):

```typescript
function getField<T, K extends keyof T = keyof T>(obj: T, key: K): T[K] {
  return obj[key];
}
```

**Dealer Dashboard example — ek typed repository:**

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

### 3.2 Generic variance (covariance/contravariance)

C# se aa rahe ho to explicitly raise karne layak hai, kyunki yeh directly `in`/`out` variance annotations (`IEnumerable<out T>`, `IComparer<in T>`) se bridge karta hai.

- **Covariance:** agar `Dog` `Animal` ka subtype hai, to kya `Dog[]` `Animal[]` ka subtype hai? TypeScript arrays aur return positions ke liye **haan** kehta hai — technically unsound hai (aap ek `Animal[]`-typed reference ke through us `Dog[]` mein `Cat` push kar sakte ho), lekin pragmatic hai, real JS usage ke saath match karta hai.
- **Contravariance:** function *parameter* types ko safely opposite direction mein narrow hona chahiye. `strictFunctionTypes` ke under, standalone function types ke liye parameters contravariantly check hote hain, jo unsound case ko catch karta hai:

```typescript
type AnimalHandler = (a: Animal) => void;
type DogHandler = (d: Dog) => void;

let handler: AnimalHandler;
let dogHandler: DogHandler = (d) => console.log(d.bark());
// handler = dogHandler; // strictFunctionTypes ke bina unsound compile hota — handler ko ab Cat ke saath call kiya ja sakta tha
```

**Known quirk:** method syntax (`interface X { fn(a: Animal): void }`) `strictFunctionTypes` ke under bhi backward-compatibility reasons ki wajah se *bivariantly* check hota hai, jabki property syntax with function type (`interface X { fn: (a: Animal) => void }`) contravariantly (strictly) check hota hai. Senior level par ek obscure lekin real distinguishing question.

C# ke ulat, TypeScript mein **user generics par explicit `in`/`out` variance annotations nahi hain** — variance type parameter ke use hone ke tareeke se *structurally inferred* hoti hai, explicitly declared nahi.

### 3.3 `keyof`, `typeof`, aur indexed access types

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

**`as` ke saath key remapping, aur `+`/`-` modifiers** (TS 4.1+):

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

**Unions ke upar distributive conditional types** — ek frequently-tested subtlety. Jab checked type ek *naked* type parameter ho, TS conditional ko har union member par individually distribute karta hai:

```typescript
type ToArray<T> = T extends any ? T[] : never;
type Result = ToArray<string | number>; // string[] | number[]  (distributed)

type ToArrayNonDist<T> = [T] extends [any] ? T[] : never;
type Result2 = ToArrayNonDist<string | number>; // (string | number)[]  (wrapping opt-out karta hai)
```

Yeh exactly wahi tareeka hai jisse built-in `Exclude`/`Extract`/`NonNullable` implement hote hain — isko samajhna explain karta hai ki *yeh utilities kyun kaam karti hain*, sirf yeh nahi ki kya karti hain.

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

Har senior TS engineer eventually isko hit karta hai jab ek "clever" recursive type likhta hai (deep params extractor, `DeepPartial`, JSON-path types).

**Yeh kyun hota hai:** compiler conditional/recursive types ko expansion ke through evaluate karta hai, jiski ek hard internal recursion-depth limit hai (roughly ~50 instantiation levels) genuinely infinite type-level recursion se protect karne ke liye. Ek naive recursive counter/concatenation type ek large ya unbounded structure par us limit ko cross kar jaata hai.

**Mitigation strategies:**
1. **Recursion depth explicitly cap karo:**
   ```typescript
   type DeepPartialBounded<T, Depth extends number = 5> =
     Depth extends 0 ? T
     : T extends object ? { [K in keyof T]?: DeepPartialBounded<T[K], Prev[Depth]> }
     : T;
   ```
2. **Circularity ko explicitly break karo** — genuinely circular graphs (`Order`/`Customer`) ke liye, flat DTO shape model karo jo aapko actually chahiye (`OrderSummary` ek flat `customerId: string` ke saath) poore circular domain graph ko deep-transform karne ke bajaye.
3. **Pre-built, well-tested utility types prefer karo** (`Partial`, `Pick`, ya `type-fest` jaisi vetted library) hand-rolled unbounded recursion ke bajaye.
4. **Base case simplify karo** — tuple mein wrap karna (`[T] extends [U]`) distribution se opt out karke instantiations ki count dramatically reduce kar sakta hai.

### 3.8 Utility types deep dive

| Utility | Effect | Simplified implementation |
|---|---|---|
| `Partial<T>` | Saari properties optional | `{ [K in keyof T]?: T[K] }` |
| `Required<T>` | Saari properties mandatory | `{ [K in keyof T]-?: T[K] }` |
| `Readonly<T>` | Saari properties readonly | `{ readonly [K in keyof T]: T[K] }` |
| `Pick<T, K>` | Keys ka ek subset select karo | `{ [P in K]: T[P] }` |
| `Omit<T, K>` | Keys ka ek subset remove karo | `Pick<T, Exclude<keyof T, K>>` |
| `Record<K, T>` | Key union + value type se object type banao | `{ [P in K]: T }` |
| `NonNullable<T>` | `null`/`undefined` strip karo | `T extends null \| undefined ? never : T` |
| `Extract<T, U>` | `U` mein assignable union members rakho | `T extends U ? T : never` |
| `Exclude<T, U>` | `U` mein assignable union members remove karo | `T extends U ? never : T` |
| `ReturnType<T>` | Function ka return type | `T extends (...a: any[]) => infer R ? R : never` |
| `Parameters<T>` | Function ka parameter tuple | `T extends (...a: infer P) => any ? P : never` |
| `InstanceType<T>` | Ek constructor ka instance type | `T extends new (...a: any[]) => infer R ? R : any` |

Interviewers aksar aapse inmein se ek ko **scratch se implement** karne ko kehte hain — right-hand column pata hona chahiye, sirf naam nahi.

```typescript
interface Vehicle { vin: string; model: string; price: number; discount?: number; }

type VehicleSummary = Pick<Vehicle, 'vin' | 'model'>;
type VehicleNoDiscount = Omit<Vehicle, 'discount'>;
type VehicleRegistry = Record<string, Vehicle>;
```

**Utility types jo aksar miss ho jaate hain:**

```typescript
// Awaited<T> — nested Promise types ko unwrap karta hai (TS 4.5 se critical)
async function fetchVehicle(): Promise<Vehicle> { return {} as Vehicle; }
type Fetched = Awaited<ReturnType<typeof fetchVehicle>>; // Vehicle, Promise<Vehicle> nahi

// DeepPartial built-in nahi hai — ek common "write this" task
type DeepPartial<T> = T extends object ? { [K in keyof T]?: DeepPartial<T[K]> } : T;
```

### 3.9 `as const` aur `satisfies` 🎯

```typescript
const statuses = ['pending', 'approved', 'rejected'] as const;
// readonly ["pending", "approved", "rejected"]

const dealer = { name: 'Downtown Motors', region: 'North' } satisfies { name: string; region: string };
```

**`satisfies` kyun matter karta hai — aur type annotation se kaise different hai:**

```typescript
// Annotation variable ko WIDEN kar deti hai — literal info lose ho jaati hai
const config1: Record<string, string | number> = { retries: 3, mode: 'fast' };
config1.retries; // type: string | number (widened)

// satisfies type ke against VALIDATE karta hai lekin narrower inferred type KEEP karta hai
const config2 = { retries: 3, mode: 'fast' } satisfies Record<string, string | number>;
config2.retries; // type: number (preserved!)
```

`satisfies` dono duniya ka best deta hai: compile-time validation ki shape ek contract conform karti hai, *aur* downstream use ke liye precise, narrow inferred types. Yeh ab constant config objects ko type karne ka idiomatic tareeka hai.

### 3.10 Ambient declarations, module augmentation & declaration merging

```typescript
// Ambient declaration (.d.ts)
declare function jQuery(selector: string): any;

// Module augmentation — third-party library ke types extend karna
declare module 'some-library' {
  interface SomeInterface { newMethod(): void; }
}

// Declaration merging
interface Window { dealerConfig: string; }
interface Window { featureFlags: string[]; }
// Ab Window mein dono properties merge ho gayi hain.
```

Triple-slash directives modern ESM projects mein largely legacy hain. Third-party types extend karne ka modern tareeka (jaise, Angular Universal backend mein Express ke `Request` object mein custom property add karna) **module augmentation** hai.

### 3.11 Decorators & metadata (Angular relevance) 🎯

Decorators functions hote hain jo classes/members/parameters par declaration time par `@expression` syntax ke through apply hote hain, declarative metadata aur behavior injection enable karte hain.

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
- Angular ke decorators (`@Component`, `@NgModule`, `@Injectable`, `@Input`) **`reflect-metadata`** aur `emitDecoratorMetadata`/`experimentalDecorators` compiler flags ke saath combine karke dependency injection implement karte hain — DI runtime par constructor parameter types ko emitted metadata padhkar resolve karta hai. Isi wajah se Angular DI silently break ho jaata hai agar aap injection token ke tor par ek interface (compile time par erase ho jaata hai, koi runtime trace nahi) use karo, class ya `InjectionToken` ki jagah.
- **Standards-track decorators** (TC39 Stage 3, TypeScript 5.0+ mein default) ki us legacy `experimentalDecorators: true` model se **different runtime semantics** hoti hain jo Angular historically use karta rahaa hai. Yeh ek live migration concern hai — naye decorators `(target, key, descriptor)` ke bajaye `(value, context)` receive karne wale plain functions hote hain, `reflect-metadata` par rely nahi karte, aur class fields ke saath differently compose hote hain. Ek project/library boundary mein legacy aur standard decorator semantics mix karna subtle bugs deta hai.

### 3.12 Module resolution: ESM vs CommonJS

| Aspect | CommonJS (CJS) | ES Modules (ESM) |
|---|---|---|
| Syntax | `require()` / `module.exports` | `import` / `export` |
| Loading | Synchronous, runtime-resolved | Static, compile time par analyzable (tree-shaking enable karta hai) |
| `tsconfig` setting | `"module": "CommonJS"` | `"module": "ESNext"` / `"NodeNext"` / `"Bundler"` |

**Practical gotchas:**
- Angular/Vite frontend ke liye `moduleResolution: "Bundler"` pick karo (bundler ko resolution handle karne deta hai). Actual Node.js backend service ke liye `"NodeNext"` pick karo (Node ke real ESM runtime rules match karne ke liye).
- Tree-shaking ko **static, analyzable ESM `import`/`export`** chahiye — dynamic `require()` ya CommonJS re-exports isko defeat kar dete hain, isi wajah se library authors ESM-only ya dual builds ki taraf push kiye jaate hain.
- Angular CLI (v17 se esbuild-based) poore build graph mein ESM expect karta hai; ek transitive CommonJS dependency well-known `CommonJS or AMD dependencies can cause optimization bailouts` warning trigger karti hai.

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
| `public` (default) | Kahin se bhi accessible |
| `private` | Sirf same class ke andar |
| `protected` | Class aur subclasses |

**Senior nuance vs C#:** TS ke `private`/`protected` **compile-time only** hote hain — emitted JS mein erase ho jaate hain, isliye runtime code bracket notation (`instance['taxId']`) ke through access kar sakta hai, ya `JSON.stringify` ke through dekh sakta hai. *True* runtime privacy ke liye native JS private fields (`#field`) use karo, jo JS engine dwara khud enforce hote hain:

```typescript
class Account {
  #balance = 0; // truly private at runtime, Object.keys() ko bhi invisible
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

**`override` keyword (TS 4.3+):** iske bina, base-class method rename ya remove karna subclass "override" ko silently orphan kar deta hai — yeh bas ek naya unrelated method ban jaata hai. `tsconfig.json` mein `noImplicitOverride` enable karo explicit annotations force karne ke liye:

```typescript
class PremiumDealer extends Dealer {
  override greet() {      // compiler verify karta hai ki Dealer.greet actually exist karta hai
    super.greet();
  }
}
```

### 4.3 Abstract classes vs interfaces

| Feature | Abstract class | Interface |
|---|---|---|
| Methods | Concrete aur abstract dono | Sirf signatures |
| Instantiation | Nahi kar sakte | Nahi kar sakte (runtime par "type" bhi nahi) |
| Properties | Defaults/init logic ke saath ho sakti hain | Sirf type definitions |
| Multiple inheritance | Nahi (sirf single class inheritance) | Haan (`interface C extends A, B`) |
| Runtime existence | Haan — emitted output mein real JS class | Nahi — fully erased |

**Interview framing:** abstract class choose karo jab subclasses actual reusable implementation share karti hain (template-method pattern). Interface choose karo pure contract ke liye, especially jab multiple unrelated classes ko satisfy karna ho.

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

**Gotcha:** `implements` sirf **public** shape check karta hai — private member behavior verify nahi karta, aur yeh koi runtime check bhi nahi hai (fully erased). `implements` (compile-time contract check) ko actually behavior inherit karne ke saath confuse mat karo.

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

Mixins multiple class inheritance ki lack ka TS/JS jawab hain — conceptually C# extension methods ya default interface methods jaisa, lekin runtime class-factory functions ke tor par implement hota hai, language feature ke tor par nahi.

### 4.6 Private constructors & singletons

**Senior nuance:** Angular mein, classic GoF singleton pattern ke bajaye DI-scoped singletons (`@Injectable({ providedIn: 'root' })`) prefer karo. Angular ka injector already har scope ke liye ek single instance guarantee karta hai, testable hai (DI ke through mockable), aur hidden global state se bachata hai.

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

`noUncheckedIndexedAccess` ke saath pair karo: iske bina, `translations['missingKey']` `string` ke tor par type hota hai halaanki runtime par actually `undefined` hota hai — production ke `undefined.toUpperCase()`-style crashes ka bahut common source.

### 4.8 The `this` type & fluent APIs

```typescript
class QueryBuilder {
  withStatus(status: string): this { /* ... */ return this; }
}
```

`this` return karne wala method, subclass instance par call hone par correctly *subclass* type return karta hai — isi se fluent/chainable builder APIs subclass-safe ban jaate hain.

### 4.9 Parameter properties shorthand

```typescript
class VehicleService {
  // `http`/`router` ko private fields ke tor par ek line mein declare AND assign karta hai
  constructor(private http: HttpClient, private router: Router) {}
}
```

Field declare karne aur constructor body mein assign karne ke equivalent pure syntax sugar hai — lekin Angular/NestJS constructor DI code mein dominant style hai.

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

**JS se carry hui gotcha:** older JS targets mein compile karte waqt `Error` ko subclass karna custom error class ke against `instanceof` checks todh sakta hai — iski wajah yeh hai ki class inheritance ES5 tak kaise downlevel hoti hai. `target: ES2015` ya usse newer par yeh issue nahi hai; apne specific target ke liye verify kar lena.

### 5.2 Typed catch clauses (`unknown` in catch) 🎯

TypeScript 4.4 se, `strict` ke under (specifically `useUnknownInCatchVariables`), `catch (error)` `error` ko `unknown` ke tor par type karta hai, `any` nahi — kyunki JS kisi bhi value ka `throw` allow karta hai (`throw "a string"`, `throw 42`, `throw { code: 500 }` sab valid hain).

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

Structured non-`Error` values throw karne wali APIs ke liye (kuch HTTP clients mein common), isko type guard ke saath pair karo:

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

- **Project references** (`references` + `composite: true`) large codebase ko independently type-checked/cached projects mein split karne dete hain, incremental builds enable karte hain — Nx/Angular monorepos mein critical.
- **`skipLibCheck: true`** `node_modules` mein `.d.ts` files ka type-checking skip kar deta hai, build time often significantly kam karta hai — cost yeh hai ki purely third-party type definitions mein originate hone wale type errors catch nahi hote.
- Deep conditional/recursive types type checker ko measurably slow kar sakte hain ya compiler ki recursion-depth limit hit kar sakte hain — "clever" type-level programming ka ek known real-world cost.
- **`isolatedModules: true`** single-file transpilers (esbuild, swc, Babel) ke liye required hai jo files ko poori program type information ke bina independently transpile karte hain — cross-file type knowledge chahne wale constructs ko forbid karta hai (jaise, `export type` ke bina type re-export karna).

### 6.2 Runtime performance: erasure, enums & bundle size

- **Types runtime par 100% erase ho jaate hain** — types, generics, interfaces, ya type aliases use karne ka zero runtime cost hota hai.
- **Real runtime cost wale constructs:** non-`const` enums (object + reverse mapping emit karte hain), decorators + `reflect-metadata`, namespaces (IIFE wrappers emit karte hain). Parameter properties negligible hain (bas trivial constructor assignment).
- Enums ke bajaye literal unions prefer karo, aur classes ke bajaye plain interfaces/types prefer karo jab sirf compile-time shape chahiye ho — har `class` real constructor/prototype JS emit karta hai, jabki `interface`/`type` kuch bhi emit nahi karta.

---

## Part 7 — Best Practices Cheat Sheet

- Day one se `strict` on karo (ideally `noUncheckedIndexedAccess` aur `exactOptionalPropertyTypes` bhi) — baad mein strictness retrofit karna kaafi zyada expensive hai.
- Har I/O boundary par (HTTP responses, `JSON.parse`, third-party callbacks) `any` ke bajaye `unknown` prefer karo; use karne se pehle explicitly narrow karo.
- New code ke liye `enum` ke bajaye literal union types prefer karo; `const enum` ko sirf tab reserve karo jab aap build pipeline ko fully control karte ho.
- Multiple variants wale kisi bhi domain modeling ke liye discriminated unions + exhaustiveness (`never`) checks use karo (API responses, NgRx actions, state machines).
- Typed constant objects/config ke liye annotation ke bajaye `satisfies` use karo, literal type precision preserve karne ke liye.
- Untrusted data ko runtime par ek schema library (`zod`, `io-ts`, class-validator) ke saath validate karo, type assertion par trust karne ke bajaye.
- Function/module boundary types explicit rakho (parameters aur exported return types); baaki sab internal inference ko handle karne do.
- `as` assertions ko last resort ke tor par treat karo; `as unknown as T` kabhi bhi bina justification comment ke chain mat karo.
- Un inputs par `readonly`/`Readonly<T>` use karo jinhe mutate karne ka intend nahi hai, aur immutable update patterns (spread, `structuredClone`, immer) prefer karo — especially Angular ke change-detection-sensitive code mein.
- Kisi bhi class hierarchy par `noImplicitOverride` enable karo silent override drift catch karne ke liye.

---

## Part 8 — Common Pitfalls

- **`any` ko "red squiggly fix karo" samajhna** `unknown` + narrowing use karne ke bajaye.
- **Yeh assume karna ki `readonly` deep hai** — yeh shallow hai; nested objects/arrays mutable rehte hain.
- **Discriminated unions par exhaustiveness checks bhool jaana** — naya variant silently compile ho jaata hai aur compile error ke bajaye runtime par `undefined` produce karta hai.
- **`??` ko `||` ke saath confuse karna** — `||` `0`, `""`, `false` ko incorrectly "missing" treat karta hai.
- **Excess property check blind spot** — intermediate variable ke through assign karna literal freshness check ko bypass kar deta hai.
- **`private`/`protected` par real security ke liye rely karna** — yeh compile-time only hain; runtime enforcement chahiye to `#privateFields` use karo.
- **`const enum` + `isolatedModules`/modern bundlers** — kai current build setups (esbuild/swc, Angular ka esbuild builder) mein incompatible hai.
- **Yeh assume karna ki index signatures presence guarantee karte hain** — `noUncheckedIndexedAccess` ke bina, `dict[key]` `T` ke tor par type hota hai, `T | undefined` nahi.
- **Yeh assume karna ki caught errors hamesha `Error` instances hote hain** — JS mein `throw` kisi bhi value ko accept karta hai; narrowing ke bina `.message` access karne wala code non-`Error` throws par crash ho jaayega.
- **Legacy aur standard decorator semantics ko mix karna** ek project/library boundary mein.
- **Type-level logic ko over-engineer karna** (deeply recursive conditional types) jo technically kaam karta hai lekin marginal type-safety gain ke liye IDE responsiveness aur compiler performance ko tank kar deta hai.

---

## Part 9 — Rapid-Fire Interview Q&A

**Q: External API response parse karne wale ek function ke liye, `any` ke bajaye `unknown` kyun choose karoge?**
A: `any` type checking ko entirely disable kar deta hai, isliye parsed result par koi bhi property access ya method call compile ho jaata hai chahe wrong ho, bug runtime tak deferred ho jaata hai. `unknown` caller ko force karta hai ki value ke saath kuch bhi karne se pehle narrow kare — exactly wahi boundary jahaan malformed payloads se bugs aane ka chance sabse zyada aur sabse costly hota hai.

**Q: Aap kaise guarantee karte ho ki ek discriminated union par `switch` naye variants add hone par bhi exhaustive rahe?**
A: Ek `default` branch add karo jo remaining narrowed value ko `never` type wale variable mein assign kare. Agar har case handled hai, compiler ne union ko `default` tak pahunchne se pehle `never` tak narrow kar diya hoga, isliye assignment type-check ho jaata hai. Naya variant add hone aur case miss hone par, assignment ek compile error ban jaata hai — silent runtime bug ko build failure mein badal deta hai.

**Q: Aaj `interface` aur `type` ke beech practical difference kya hai, aur aap default kisko karte ho?**
A: Dono object shapes describe karte hain aur largely structurally interchangeable hain. Real differences: interfaces declaration merging aur `extends`-based multiple inheritance support karte hain; type aliases unions, tuples, aur mapped/conditional types express kar sakte hain jo interfaces nahi kar sakte. Object/class contracts ke liye `interface` default karo jinhe extend ya merge kiya ja sakta ho (public DTOs, Angular component inputs), aur unions, function signatures, type-level utilities ke liye `type`.

**Q: Aapki team ek legacy migration speed up karne ke liye `strictNullChecks` disable karna chahti hai. Aap kya push back karoge?**
A: `strictNullChecks` single highest-value strict flag hai — iske bina, compiler kahin bhi ek guaranteed value ko possibly-missing value se distinguish nahi kar sakta. Isse off karna sirf migrated files nahi, poore codebase mein `TypeError` bug class ko silently reintroduce kar deta hai, aur modern `.d.ts` files third-party libraries se inference bhi kam trustworthy bana deti hain. Blanket disable ke bajaye scoped, incremental adoption better path hai.

**Q: Structural typing explain karo aur ek jagah bataao jahaan yeh surprising result cause karta hai.**
A: TypeScript types ko shape se compare karta hai, declared name se nahi — compatible members wala koi bhi object ek type satisfy karta hai. Surprising case: excess property checks sirf directly assign hone wale object literals par apply hote hain; literal ko pehle variable mein assign karo aur phir pass karo, to check fire nahi hota, aur extra/typo hui property silently slip through ho jaati hai — kyunki yeh ab ek structural assignability check hai, literal freshness check nahi.

**Q: TypeScript vs C# mein generics kaise differ karte hain, aur practice mein yeh kahaan matter karta hai?**
A: C# generics reified hote hain — CLR ko runtime par concrete type argument pata hota hai. TypeScript generics compile time par fully erase ho jaate hain — runtime par `T` exist nahi karta, isliye generic parameter ke against runtime type-checking ya `new T()` jaise patterns directly possible nahi hain. C# se generic factories port karte waqt aapko explicit constructor parameter (`new (...args: any[]) => T`) chahiye hota hai.

**Q: esbuild ke saath build ki gayi modern Angular app mein `enum` use karne ka risk kya hai, aur aap iske bajaye kya use karte ho?**
A: Regular enums real runtime objects reverse mappings ke saath emit karte hain, jo bundle size mein add hote hain aur tree-shaking ko defeat karte hain. `const enum` values ko inline karke isse avoid karta hai, lekin `isolatedModules` ke saath incompatible hai jo esbuild-based builds require karte hain. Idiomatic replacement ek literal union type hai, optionally ek `as const satisfies Record<...>` object ke saath paired — same type safety zero runtime cost ke saath.

---

## Quick Revision Sheet

- **`any`** = checking band. **`unknown`** = checking on, use se pehle narrow karo. **`never`** = unreachable/bottom type. **`void`** = koi meaningful return nahi.
- **`interface`**: mergeable, `extends`-based, object contracts. **`type`**: unions/tuples/mapped types, mergeable nahi.
- **`readonly`** shallow hai — nested data mutable rehta hai.
- **Structural typing**: shape matter karta hai, declared name nahi. Excess-property checks sirf literals par fire hoti hain, variables par nahi.
- **Branded types**: `string & { __brand: 'X' }` + constructor function = simulated nominal typing.
- **Discriminated unions + `never` exhaustiveness check** = naye variants add karne ke liye compile-time safety net.
- **`??`** `0`/`""`/`false` respect karta hai; **`||`** nahi karta.
- **`strict: true`** on karta hai: `strictNullChecks`, `noImplicitAny`, `strictFunctionTypes`, `strictBindCallApply`, `strictPropertyInitialization`, `noImplicitThis`, `useUnknownInCatchVariables` — lekin `noUncheckedIndexedAccess` ya `exactOptionalPropertyTypes` NAHI, jo separately add karne chahiye.
- **`satisfies`** type ke against validate karta hai narrow inferred type ko keep karte hue; plain annotation widen kar deta hai.
- **Enums** ka runtime cost hai aur `isolatedModules` ke saath break hote hain; literal unions prefer karo.
- **`catch (e)`** `strict` ke under `unknown` hai — `.message` use karne se pehle hamesha `instanceof Error` ya type guard ke saath narrow karo.
- **Angular DI** `reflect-metadata` + decorator metadata par depend karta hai — injection token ke tor par kabhi interface use mat karo (runtime par erase ho jaata hai); class ya `InjectionToken` use karo.
- **Types runtime par 100% erase ho jaate hain** — zero cost, except enums, decorators+metadata, aur namespaces ke, jo real JS emit karte hain.
- **`TS2589`** (recursive type too deep) → recursion depth bound karo, circularity break karo, ya built-in/vetted utility types prefer karo.

---

## `tsconfig.json` Quick Reference — sab `compilerOptions` ek jagah

| Option | Kya karta hai | Kyun matter karta hai |
|---|---|---|
| `target` | JS output kis version mein compile hogi | `ES2022` = chhota, modern output; purana target = zyada, uglier downleveled code |
| `module` | Compiled output mein import/export kis format mein emit hongi | `ESNext` tree-shaking enable karta hai; `CommonJS` bundler ke bina Node ke liye |
| `moduleResolution` | TS files kaise resolve karta hai | `Bundler` = modern esbuild/Vite behavior; `NodeNext` = strict Node ESM rules |
| `lib` | Kaunse built-in type declarations include hon | `DOM` ke bina `document`/`window` compile error dete hain |
| `strict` | Strict flags ka poora bundle on karta hai | Master switch — bina isके `noImplicitAny` jaisi cheezein silently pass ho jaati hain |
| `noUncheckedIndexedAccess` | Index access mein `\| undefined` add karta hai | `arr[0]` jaisi cheezein safe force karta hai, jhoothi guarantee nahi deta |
| `exactOptionalPropertyTypes` | "absent key" vs "undefined value wali key" distinguish karta hai | Precise API contracts ke liye |
| `esModuleInterop` | CommonJS packages ka clean default import allow karta hai | `import express from 'express'` `import * as express` ke bajaye |
| `skipLibCheck` | `node_modules` ke `.d.ts` files ka type-check skip karta hai | Build time kam karta hai |
| `isolatedModules` | Cross-file type knowledge chahne wale constructs forbid karta hai | esbuild/swc jaise single-file transpilers ke liye required |
| `forceConsistentCasingInFileNames` | File-name casing ko imports ke across enforce karta hai | Case-sensitive Linux CI par build breaks se bachata hai |
