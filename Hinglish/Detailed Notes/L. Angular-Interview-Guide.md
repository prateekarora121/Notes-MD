# Angular Senior/Lead Interview Guide

> Source notes ka title "Angular 7" (2018-era) tha, lekin modern senior interviews (2026) latest stable Angular tak ka knowledge assume karte hain (v18/19-class features: default se standalone APIs, Signals, naya control-flow syntax, `@defer`, zoneless change detection). Yeh guide aapke original notes preserve karta hai, unko reorganize karta hai, har open question ka answer deta hai, aur Angular 7 aur current Angular ke beech gap close karne ke liye add ki gayi har cheez ko clearly tag karta hai. Additions **[new content]** se prefixed hain.

## Table of Contents

1. [Core Concepts](#core-concepts)
   - [Angular vs AngularJS](#angular-vs-angularjs)
   - [Hybrid Angular Apps and Upgrading from AngularJS (ngUpgrade)](#hybrid-angular-apps-and-upgrading-from-angularjs-ngupgrade)
   - [Architecture Overview](#architecture-overview)
   - [TypeScript in Angular](#typescript-in-angular)
   - [Modules (NgModule)](#modules-ngmodule)
   - [Components](#components)
   - [[new content] Standalone Components (Angular 14+, default since v17)](#new-content-standalone-components-angular-14-default-since-v17)
   - [UI Component Libraries: Angular Material and Bootstrap](#ui-component-libraries-angular-material-and-bootstrap)
   - [Templates, Metadata, Decorators](#templates-metadata-decorators)
   - [Data Binding](#data-binding)
   - [Directives](#directives)
   - [[new content] New Control-Flow Syntax: @if / @for / @switch (Angular 17+)](#new-content-new-control-flow-syntax-if--for--switch-angular-17)
   - [ng-container, ng-template, ng-content](#ng-container-ng-template-ng-content)
   - [Pipes](#pipes)
   - [Services and Dependency Injection](#services-and-dependency-injection)
   - [[new content] The inject() Function (Angular 14+)](#new-content-the-inject-function-angular-14)
2. [Intermediate](#intermediate)
   - [Component Lifecycle Hooks](#component-lifecycle-hooks)
   - [ViewChild / ViewChildren / ContentChild / ContentChildren](#viewchild--viewchildren--contentchild--contentchildren)
   - [Component Communication](#component-communication)
   - [Routing and Navigation](#routing-and-navigation)
   - [Route Guards](#route-guards)
   - [Forms: Template-driven vs Reactive](#forms-template-driven-vs-reactive)
   - [[new content] Typed Reactive Forms (Angular 14+)](#new-content-typed-reactive-forms-angular-14)
   - [HttpClient and Interceptors](#httpclient-and-interceptors)
   - [[new content] Functional Interceptors (Angular 15+)](#new-content-functional-interceptors-angular-15)
3. [Advanced](#advanced)
   - [RxJS and Observables](#rxjs-and-observables)
   - [Subjects, BehaviorSubject, Multicasting](#subjects-behaviorsubject-multicasting)
   - [RxJS Operators Cheat Sheet](#rxjs-operators-cheat-sheet)
   - [[new content] Angular Signals (Angular 16+)](#new-content-angular-signals-angular-16)
   - [[new content] RxJS vs Signals — When to Use Which](#new-content-rxjs-vs-signals--when-to-use-which)
   - [State Management (NgRx and Alternatives)](#state-management-ngrx-and-alternatives)
   - [[new content] SignalStore / NgRx Signals (Angular 17+)](#new-content-signalstore--ngrx-signals-angular-17)
   - [[gaps] NgRx Entity Adapters, Facade Pattern, and Selector Memoization](#gaps-ngrx-entity-adapters-facade-pattern-and-selector-memoization)
   - [[new content] Dependency Injection: Hierarchical Injectors Deep Dive](#new-content-dependency-injection-hierarchical-injectors-deep-dive)
   - [[gaps] Micro-Frontends / Module Federation for Angular](#gaps-micro-frontends--module-federation-for-angular)
4. [Performance](#performance)
   - [Change Detection Deep Dive](#change-detection-deep-dive)
   - [[new content] Zoneless Change Detection (Angular 18+)](#new-content-zoneless-change-detection-angular-18)
   - [OnPush Strategy and Pitfalls](#onpush-strategy-and-pitfalls)
   - [trackBy / track in Loops](#trackby--track-in-loops)
   - [[new content] Deferrable Views: @defer (Angular 17+)](#new-content-deferrable-views-defer-angular-17)
   - [Lazy Loading and Preloading](#lazy-loading-and-preloading)
   - [Bundle Size, Tree Shaking, Build Optimization](#bundle-size-tree-shaking-build-optimization)
   - [Angular CLI vs Webpack](#angular-cli-vs-webpack)
   - [[new content] esbuild / Vite-based Application Builder (Angular 17+)](#new-content-esbuild--vite-based-application-builder-angular-17)
   - [Angular Build & Runtime Lifecycle](#angular-build--runtime-lifecycle)
   - [HTTP Request Lifecycle](#http-request-lifecycle)
5. [SSR, PWA & Cross-Cutting](#ssr-pwa--cross-cutting)
   - [Angular Universal / SSR](#angular-universal--ssr)
   - [[new content] Hydration (Angular 16+) and Event Replay (Angular 17+)](#new-content-hydration-angular-16-and-event-replay-angular-17)
   - [Progressive Web Apps and Service Workers](#progressive-web-apps-and-service-workers)
   - [Angular Security](#angular-security)
   - [[gaps] Accessibility (a11y): ARIA, CDK a11y Module, Focus Management](#gaps-accessibility-a11y-aria-cdk-a11y-module-focus-management)
   - [Deployment: Firebase and GitHub Pages](#deployment-firebase-and-github-pages)
6. [Enterprise Architecture Case Study (BFF + CQRS + .NET)](#enterprise-architecture-case-study-bff--cqrs--net)
7. [Testing](#testing)
8. [Best Practices](#best-practices)
9. [Common Pitfalls](#common-pitfalls)
10. [Version Feature Comparison Table](#version-feature-comparison-table)
11. [Sample Interview Q&A](#sample-interview-qa)
12. [Summary of Additions](#summary-of-additions)
13. [Summary of [gaps] Additions (This Pass)](#summary-of-gaps-additions-this-pass)

---

## Core Concepts

### Angular vs AngularJS

Angular (v2+) AngularJS (1.x) ka ek full TypeScript rewrite hai. Key differences:

| Aspect | AngularJS (1.x) | Angular (2+) |
|---|---|---|
| Architecture | MVC | Component-based |
| Language | JavaScript | TypeScript |
| Performance | Digest cycle, slower | AOT + Ivy, faster |
| Mobile | Optimized nahi | Optimized |
| Rendering engine | N/A | View Engine → **Ivy** (v9 se default) |

**Angular ki key features** (source se, abhi bhi accurate): component-based architecture, two-way data binding, directives & pipes, DI, routing, HttpClient, template-driven & reactive forms, lazy loading, AOT compilation.

**Ivy kya hai?** Ivy Angular ka rendering aur compilation engine hai — yeh components aur templates ko ahead of time low-level JavaScript instructions ke ek set mein compile kar deta hai, ek larger, more generic runtime interpreter par rely karne ke bajaye. Yeh Angular 9 se default engine hai (older "View Engine" ko replace karte hue), aur yeh specifically isliye highlight hota hai ki yeh kya deliver karta hai: smaller bundle sizes (better tree-shaking, kyunki har component apne khud ke minimal instructions emit karta hai large shared runtime metadata ke bajaye), faster compilation, aur better debugging (component instances browser console mein directly inspectable ban jaate hain, aur template errors source locations tak zyada precisely map hote hain).

**Follow-up jo ek interviewer poochega:** "Angular MVC nahi hai — aap isko kya kahoge?" Jawab: yeh ek component/MVVM-flavored architecture ke closer hai — component class view-model hai, template view hai, aur services/DI model/business logic layer supply karte hain. Classic MVC jaisa koi single canonical controller layer nahi hota.

### Hybrid Angular Apps and Upgrading from AngularJS (ngUpgrade)

Ek **Hybrid Angular App** ek application hai jo migration ke during same page par AngularJS (1.x) aur Angular (2+) ko side by side run karti hai — dono framework runtimes simultaneously bootstrap kiye jaate hain, aur components/services dono ke beech shared ya communicated hote hain.

Angular isko specifically support karne ke liye **`ngUpgrade`** module (package `@angular/upgrade`) ship karta hai:

```bash
npm install @angular/upgrade
```

- `UpgradeModule` hybrid app ko bootstrap karta hai aur AngularJS directives ko Angular templates se use hone deta hai, aur Angular components ko AngularJS templates se use hone deta hai (`downgradeComponent`/`downgradeInjectable` ke through).
- Typical migration strategy: existing AngularJS app ke saath Angular introduce karo, ek time mein ek route/feature/component ko Angular mein migrate karo, aur jab tak AngularJS code fully retire na ho jaaye tab tak `ngUpgrade` ko bridge ke roop mein rakho — poore app ka ek risky "big bang" rewrite attempt karne ke bajaye.
- **Yeh interviews mein kyun aata hai:** ~2016 se purani codebase wale most enterprises exactly isi migration path se through gaye. Ek senior/lead candidate se expect kiya jaata hai ki woh yeh jaane ki mechanism exist karta hai (`ngUpgrade`, `@angular/upgrade`) even personally ek migration run kiye bina bhi — yeh Angular ki history ki awareness aur legacy-modernization projects actually kaise staffed aur scoped hote hain, iska signal deta hai, jo directly un WebForms → MVC → .NET Core migrations par map hota hai jinse ek 10-year .NET developer likely gujra hoga.

### Architecture Overview

Classic (NgModule-based) building blocks, source notes ke according:

1. **Modules** (`@NgModule`) — app ko blocks mein organize karte hain.
2. **Components** (`@Component`) — UI logic aur structure.
3. **Templates & Views** — directives/bindings ke saath HTML.
4. **Directives & Pipes** — behavior modify karte hain, data transform karte hain.
5. **Services & DI** — components ke across logic share karte hain.
6. **Routing** (`RouterModule`) — navigation.
7. **Forms** — template-driven aur reactive.
8. **State Management** — NgRx ya service-based.

```mermaid
flowchart TD
    A[User Action] --> B[Component]
    B --> C[Service]
    C --> D[HTTP / API]
    D --> E[Response]
    E --> B
    B --> F[Template / View Update]
```

**[new content] Modern architecture note:** Angular 14+ se (aur Angular 17 se default project schematic mein), recommended architecture NgModules ko entirely drop kar deta hai **standalone components, directives, aur pipes** ke favor mein, routing `main.ts`/`app.config.ts` mein provider functions (`provideRouter`) ke through configured hoti hai, `RouterModule.forRoot()` ke bajaye. NgModules deprecated/removed nahi hain — wo abhi bhi kaam karte hain aur legacy codebases mein common hain — lekin "aaj ek naya Angular app kaise structure karoge" ab ek standalone, NgModule-free answer expect karta hai. Dono ke baare mein baat karne ke liye ready raho.

### TypeScript in Angular

TypeScript static typing aur OOP features ke saath JavaScript ka ek superset hai. Angular isko use karta hai: type safety, better tooling/IDE support, compile-time error detection, aur modern ES features aur decorators ke support ke liye. Ek 10-year .NET developer ke liye, mental model C# se closely map hota hai: interfaces, generics, access modifiers, aur strict null checking (`strictNullChecks`, Angular CLI projects mein `strict: true` ka part) C# nullable reference types ke analogous behave karte hain.

**Follow-up:** "Angular TypeScript ko sirf support karne ke bajaye *require* kyun karta hai?" Kyunki Angular ka compiler (AOT/Ivy) optimized instruction code generate karne aur dependency injection ki type-based token resolution ko power karne ke liye static type information aur decorator metadata par rely karta hai.

### Modules (NgModule)

```typescript
@NgModule({
  declarations: [AppComponent],
  imports: [BrowserModule],
  providers: [],
  bootstrap: [AppComponent]
})
export class AppModule { }
```

- `declarations` — is module ke owned components/directives/pipes.
- `imports` — dusre modules jinke exported declarables is module ko chahiye.
- `providers` — module-level DI registrations (legacy pattern; ab `providedIn: 'root'` preferred hai).
- `bootstrap` — root component jise Angular app start karne ke liye instantiate karta hai (sirf root module ke liye relevant).

`RouterModule.forRoot(routes)` vs `RouterModule.forChild(routes)` — `forRoot()` exactly ek baar use hota hai, app ke root routing module mein, aur additionally singleton `Router` service, location strategy, etc. configure karta hai. `forChild()` feature modules mein use hota hai aur sirf routes register karta hai router singletons ko re-create kiye bina — critical hai kyunki ek feature module mein `forRoot()` ko re-invoke karna duplicate/broken router state cause karne wala ek classic bug hota tha.

### Components

Ek component = Template (HTML) + Class (TS logic) + Styles (CSS/SCSS).

```typescript
import { Component, ViewEncapsulation, ChangeDetectionStrategy } from '@angular/core';
import { CommonModule } from '@angular/common';
import { UserService } from './user.service';
import { trigger, state, style, transition, animate } from '@angular/animations';

@Component({
  selector: 'app-user',
  templateUrl: './user.component.html',
  styleUrls: ['./user.component.css'],
  providers: [UserService],
  viewProviders: [UserService],
  encapsulation: ViewEncapsulation.Emulated,
  changeDetection: ChangeDetectionStrategy.OnPush,
  animations: [
    trigger('fade', [
      state('visible', style({ opacity: 1 })),
      state('hidden', style({ opacity: 0 })),
      transition('visible <=> hidden', [animate('300ms')])
    ])
  ],
  standalone: true,
  imports: [CommonModule],
  exportAs: 'userComp',
  host: {
    '(click)': 'onHostClick()',
    '[class.active]': 'isActive'
  }
})
export class UserComponent {
  name = 'John';
  isActive = true;
  state = 'visible';

  toggle() {
    this.state = this.state === 'visible' ? 'hidden' : 'visible';
  }

  onHostClick() {
    console.log('Host element clicked');
  }
}
```

**Har `@Component` property kya karti hai:**

| Property | Purpose |
|---|---|
| `selector` | Component ke liye HTML tag name |
| `template` | Inline HTML |
| `templateUrl` | External HTML file |
| `styles` | Inline CSS |
| `styleUrls` | External CSS |
| `providers` | Is component + uske children tak scoped services |
| `viewProviders` | Sirf is component ki *view* tak scoped services (projected content ko visible nahi) |
| `encapsulation` | CSS scoping strategy — neeche dekho |
| `changeDetection` | Change detection optimization (`Default` ya `OnPush`) |
| `animations` | Component animations |
| `standalone` | NgModule ke bina usable component |
| `imports` | Doosre standalone components/directives/pipes/modules jo is component ke template ko chahiye |
| `exportAs` | Directive ke roop mein use hone par is component ko template reference variable ke through reference karne ka naam |
| `host` | Component ke apne host element par applied bindings/listeners |

`encapsulation` options:

| Value | Behavior |
|---|---|
| `Emulated` (default) | Angular attribute selectors use karke Shadow DOM emulate karta hai; styles scoped hain lekin truly isolated nahi |
| `None` | Koi encapsulation nahi; styles global ban jaate hain |
| `ShadowDom` | Strict isolation ke liye real browser Shadow DOM use karta hai |

**`providers` vs `viewProviders`:** `providers` component ki apni view *aur* `<ng-content>` ke through usme projected kisi bhi content ko visible hote hain. `viewProviders` sirf component ke apne template ko visible hote hain, projected content ko nahi. Yeh ek classic senior-level gotcha question hai — most developers ko kabhi `viewProviders` ki zaroorat nahi padi aur woh on the spot difference explain nahi kar sakte.

**Directive vs Component:**

| Feature | Component | Directive |
|---|---|---|
| UI Rendering | Haan | Nahi |
| Decorator | `@Component` | `@Directive` |
| Template | Ek template hai | Koi template nahi |
| Example Usage | UI components | DOM behavior change |

Har component technically ek directive hai jisme ek template attached hai — yeh ek common trick interview question hai ("kya ek component ek directive hai?").

### [new content] Standalone Components (Angular 14+, default since v17)

Ek "2018 Angular 7" note set mein single biggest structural gap. Standalone components mandatory NgModule wrapper ko eliminate kar dete hain:

```typescript
@Component({
  standalone: true,
  selector: 'app-user-card',
  imports: [CommonModule, RouterLink], // import only what the template needs, directly
  template: `<div>{{ name }}</div>`
})
export class UserCardComponent {
  name = 'Jane';
}
```

`AppModule` ke bina bootstrapping:

```typescript
// main.ts
import { bootstrapApplication } from '@angular/platform-browser';
import { AppComponent } from './app/app.component';
import { appConfig } from './app/app.config';

bootstrapApplication(AppComponent, appConfig);
```

```typescript
// app.config.ts
export const appConfig: ApplicationConfig = {
  providers: [
    provideRouter(routes),
    provideHttpClient(withInterceptors([authInterceptor])),
    provideAnimations(),
  ]
};
```

**Interviews ke liye yeh kyun matter karta hai:** Angular CLI v17 se standalone-by-default projects generate karta hai. Interviewers aapse old (`NgModule` + `RouterModule.forRoot`) aur new (`bootstrapApplication` + `provideRouter`) bootstrap flows ko contrast karne ke liye kahenge, aur explain karne ke liye kahenge ki Angular ne yeh move *kyun* kiya — primarily mental model simplify karne ke liye (ab "main isko kis module mein declare karun?" nahi), tree-shakability improve karne ke liye (har component apni exact dependencies declare karta hai), aur Angular ko better align karne ke liye React/Vue/Svelte components jaise consume kiye jaate hain (self-contained, importable units) ke saath. NgModules **deprecated nahi hain** — aap standalone components ko ek NgModule-based app mein incrementally mix kar sakte ho, jo recommended migration path hai (`ng generate @angular/core:standalone` schematic assist kar sakta hai).

**Gotcha:** ek standalone component ka `imports` array ek NgModule ke `imports` ke analogous hai, lekin per-component scoped hai app-wide shared hone ke bajaye — har standalone component ko khud `CommonModule` (ya specific directives) import karna padta hai agar woh `*ngIf`/`*ngFor`/`date` jaise pipes use karta hai, NgModule apps ke unlike jahan `CommonModule` aksar ek shared module mein ek baar import kiya jaata tha.

### UI Component Libraries: Angular Material and Bootstrap

Angular ke saath paired do most common UI options:

| Library | What it is | Notes |
|---|---|---|
| **Angular Material** | Google ki official Material Design component library, specifically Angular ke liye built | Deep Angular integration (CDK par built, reactive forms ke saath naturally kaam karta hai), SCSS ke through theming, accessible-by-default components (baad mein cover kiya gaya CDK `a11y` module dekho) |
| **Bootstrap** | Ek general-purpose, framework-agnostic CSS framework | Angular-specific nahi — plain CSS classes ke through use hota hai, ya Angular-idiomatic components ke liye `ngx-bootstrap`/`ng-bootstrap` jaisi ek wrapper library ke through (jQuery-driven widgets ke bajaye directives) |

CLI schematic ke through Angular Material install karna:

```bash
ng add @angular/material
```

Yeh single command package install karta hai, ek interactive theme picker run karta hai, `angular.json`/`main.ts` mein typography/animations wire up karta hai, aur optionally gesture support (`hammerjs`) set up karta hai — CLI ecosystem mein kahin aur use hone wala wahi schematic-driven `ng add` convention (`@angular/pwa`, `@angular/ssr`, `@ngrx/store`).

**Senior framing:** Angular Material ke liye reach karo jab aapko baked-in accessibility, theming, aur Angular-native APIs wali ek component library chahiye, aur Material Design ka visual language acceptable hai; Bootstrap (ya ek wrapper) ke liye reach karo jab org ke paas already match karne ke liye Bootstrap-based design conventions/branding hai, ya team ek CSS-only approach chahti hai jo ek component framework ke JS se tied na ho.

### Templates, Metadata, Decorators

Ek **template** ek component ka HTML view hai — declarative, imperative nahi; yeh desired UI describe karta hai, aur Angular isko efficient render/update instructions mein compile karta hai (Ivy ke through).

```html
<h1>{{ title }}</h1>
<button (click)="sayHello()">Click me</button>
```

**Metadata** ek class se ek decorator (`@Component`, `@NgModule`, `@Injectable`) ke through attached information hai, jo Angular ko batati hai ki class ko kaise construct/wire karna hai.

**Decorators** functions hain jo wo metadata attach karte hain. Decorators ke bina, Angular ke paas jaanne ka koi tarika nahi hota ki ek class ko kaise instantiate karna hai ya uski dependencies kaise resolve karni hain. Key decorators: `@Component`, `@Directive`, `@Pipe`, `@NgModule`, `@Injectable`, `@Input`, `@Output`, `@HostBinding`, `@HostListener`, `@ViewChild`, `@ContentChild`.

### Data Binding

Char types:

| Type | Syntax | Direction | Example |
|---|---|---|---|
| Interpolation | `{{ value }}` | Component → View | `<h1>{{ title }}</h1>` |
| Property binding | `[property]="value"` | Component → View | `<img [src]="imageUrl">` |
| Event binding | `(event)="handler()"` | View → Component | `<button (click)="onClick()">` |
| Two-way binding | `[(ngModel)]="value"` | Both | `<input [(ngModel)]="name">` |

Two-way binding syntactic sugar hai: `[(ngModel)]="name"` `[ngModel]="name" (ngModelChange)="name=$event"` mein desugar hota hai. Yeh "banana in a box" desugaring jaanna ek frequent interview probe hai, especially jab aapko apna khud ka two-way-bindable custom component (`@Input() value` + `@Output() valueChange`) build karne ke liye kaha jaaye.

### Directives

Teen types:

1. **Component directives** — har component ek template wala directive hai.
2. **Structural directives** (`*ngIf`, `*ngFor`, `*ngSwitch`) — DOM elements add/remove karte hain. `*` sugar hai: Angular internally `*ngIf="cond"` ko `<ng-template [ngIf]="cond">...</ng-template>` mein desugar karta hai.
3. **Attribute directives** (`ngClass`, `ngStyle`, custom directives) — DOM structure alter kiye bina appearance/behavior change karte hain.

Custom directive example:

```typescript
@Directive({ selector: '[appHighlight]' })
export class HighlightDirective {
  constructor(private el: ElementRef, private renderer: Renderer2) {}

  private setColor(color: string) {
    this.renderer.setStyle(this.el.nativeElement, 'color', color);
  }
}
```

**`ElementRef` vs `Renderer2`:** `ElementRef` native DOM node (`elementRef.nativeElement`) ka direct access deta hai. `Renderer2` DOM manipulation ke liye Angular-recommended abstraction hai kyunki yeh server-side rendering aur Web Worker rendering ke under correctly kaam karta hai, jahan directly touch karne ke liye koi real DOM nahi hota. **Interview trap:** `nativeElement.style` ko directly mutate karna (jaise notes ke older `HighlightDirective` example mein dikhaya gaya hai) browser mein kaam karta hai lekin SSR ke under break ho jaata hai — production code mein hamesha `Renderer2` prefer karo, aur agar us snippet ko critique karne ke liye kaha jaaye to yeh call out karo.

`*ngIf` vs `[hidden]`:

| Feature | `*ngIf` | `[hidden]` |
|---|---|---|
| Removes from DOM? | Haan | Nahi — bas hide karta hai (`display:none`) |
| Performance impact | Expensive subtrees ke liye better (false hone par render cost nahi) | Element DOM/memory mein rehta hai; frequently toggle karna cheaper hai |
| Re-runs lifecycle hooks on toggle? | Haan (destroy/recreate karta hai) | Nahi |

`trackBy` — [Performance](#trackby--track-in-loops) dekho.

### [new content] New Control-Flow Syntax: @if / @for / @switch (Angular 17+)

Angular 17 ne ek naya built-in template control-flow syntax introduce kiya jo eventually naye code ke liye `*ngIf`/`*ngFor`/`*ngSwitch` ko replace karta hai:

```html
@if (isLoggedIn) {
  <p>Welcome back!</p>
} @else if (isGuest) {
  <p>Continue as guest</p>
} @else {
  <p>Please log in.</p>
}

@for (item of items; track item.id) {
  <li>{{ item.name }}</li>
} @empty {
  <li>No items found.</li>
}

@switch (status) {
  @case ('success') { <p>Success</p> }
  @case ('error') { <p>Error</p> }
  @default { <p>Unknown</p> }
}
```

**Senior level par yeh kyun matter karta hai:**
- `@for` mein `track` **mandatory** hai (`*ngFor` par optional `trackBy` ke unlike), jo developers ko baad mein perf problem discover karne ke bajaye upfront identity/keying ke baare mein sochne ke liye force karta hai.
- Naya syntax directly Ivy dwara compile hota hai `<ng-template>` desugaring ki zaroorat ke bina, jo structural-directive versions ke versus smaller generated code aur reportedly better runtime performance deta hai.
- `@empty` ek first-class "no data" block hai — pehle ek separate `*ngIf="items.length === 0"` sibling block ki zaroorat hoti thi.
- `*ngIf`/`*ngFor`/`*ngSwitch` **deprecated ya removed nahi hain** — dono syntaxes coexist karte hain, aur old wale 2024 se pehle build hui kisi bhi codebase mein extremely common hain. Dono ko read/maintain karne ke liye ready raho; ek codebase ko auto-convert karne ke liye migration tooling (`ng generate @angular/core:control-flow`) exist karta hai.
- `@if`/`@for` use karne ke liye `CommonModule` import ki ab zaroorat nahi hai — wo template compiler mein built-in hain, directives nahi, isliye kuch bhi import karne ki zaroorat nahi.

### ng-container, ng-template, ng-content

| Feature | Purpose | Renders in DOM? | Best Use |
|---|---|---|---|
| `ng-container` | Logical grouping, koi extra element nahi | Nahi | Structural directives apply karte waqt wrapper `<div>` pollution avoid karo |
| `ng-template` | Deferred/conditional rendering ke liye blueprint | Nahi (jab tak use na ho) | `*ngIf ... else`, reusable templates, `ngTemplateOutlet` |
| `ng-content` | Content projection (parent → child) | Haan (sirf children project karta hai) | Reusable components: cards, modals, tabs |

`ng-container` example (wrapper divs avoid karna):

```html
<ng-container *ngIf="isLoggedIn">
  <p>Welcome back, user!</p>
  <button>Logout</button>
</ng-container>
```

`else` aur `ngTemplateOutlet` ke saath `ng-template`:

```html
<p *ngIf="isLoggedIn; else showLogin">Welcome back!</p>
<ng-template #showLogin>
  <p>Please log in to continue.</p>
</ng-template>
```

```html
<ng-template #loadingTemplate>
  <p>Loading data...</p>
</ng-template>
<div *ngIf="isLoading; else content"></div>
<ng-template #content>
  <ng-container *ngTemplateOutlet="loadingTemplate"></ng-container>
</ng-template>
```

`ng-content` — basic aur multi-slot projection:

```typescript
@Component({
  selector: 'app-card',
  template: `
    <div class="card">
      <header><ng-content select="[card-title]"></ng-content></header>
      <section><ng-content></ng-content></section>
      <footer><ng-content select="[card-footer]"></ng-content></footer>
    </div>
  `
})
export class CardComponent {}
```

```html
<app-card>
  <h2 card-title>Book Title</h2>
  <p>Description here...</p>
  <button card-footer>Buy Now</button>
</app-card>
```

### Pipes

Pipes data ko **sirf display ke liye** transform karte hain, formatting logic ko component class se bahar rakhte hain aur templates ko clean rakhte hain.

```html
<p>{{ name | uppercase }}</p>
<p>{{ 1234.56 | currency:'USD' }}</p>
```

Built-in pipes: `uppercase`, `lowercase`, `titlecase`, `number`, `percent`, `currency`, `date`, `json`, `async`, `slice`, `decimal`.

Custom pipe:

```typescript
@Pipe({ name: 'reverse' })
export class ReversePipe implements PipeTransform {
  transform(value: string): string {
    return value.split('').reverse().join('');
  }
}
```

**Pure vs impure pipes:**

```typescript
@Pipe({ name: 'purePipe', pure: true })   // default — runs only when input reference changes
@Pipe({ name: 'impurePipe', pure: false }) // runs on every change detection cycle
```

- **Pure pipe** — sirf tab re-evaluate hota hai jab Angular input ki *reference* (ya uski primitive value) mein ek change detect karta hai. Cheap, predictable, default aur strongly preferred.
- **Impure pipe** — har change detection run par re-evaluate hota hai regardless of ki input actually change hua ya nahi. Built-in `async` pipe jaise pipes ke liye needed hai (jisko new emissions ke liye ek Observable/Promise poll karna padta hai) lekin otherwise ek performance red flag hai; templates mein arrays filter/sort karne ke liye custom impure pipes likhne se avoid karo — yeh poore app ke across har keystroke/tick par recompute karta hai.

**Interview trap:** "Aapko default se ek pipe use karke list filter kyun nahi karni chahiye?" Kyunki agar pipe pure hai, yeh re-run nahi hoga jab aap array ko in place mutate karte ho (koi new reference nahi), aur agar isko work around karne ke liye impure banaya jaaye, yeh ab har CD cycle par run hota hai — ek well-known Angular performance foot-gun. Fix yeh hai ki filtering ko component mein karo (on demand ek naye array mein recompute karke) ya instead RxJS/Signals-based derived state use karo.

### Services and Dependency Injection

```typescript
@Injectable({ providedIn: 'root' })
export class DataService {
  getData() { return 'Hello from Service'; }
}
```

```typescript
constructor(private dataService: DataService) { }
```

`providedIn: 'root'` vs `@NgModule` mein `providers` array:

| Feature | `providedIn: 'root'` | `providers` in `@NgModule` |
|---|---|---|
| Scope | Singleton, application-wide | Ek module/component tak scoped ho sakta hai |
| Lazy loading | Tree-shakable; achhe se kaam karta hai, sirf inject hone par instantiate hota hai | Manually manage karna padta hai; historically per lazy module duplicate instances ka risk hota tha |
| Recommendation | Most services ke liye preferred default | Jab aapko deliberately scoped/multiple instances chahiye tab use karo |

**Hierarchical DI:** Angular ka injector ek tree hai, ek single global container nahi. `providedIn: 'root'` app-wide ek instance deta hai. Ek component ke apne `providers` array mein ek service register karna us component subtree ke liye ek *new* instance create karta hai — e.g., `providers: [ValidationService]` wala ek `FormComponent` apna khud ka isolated `ValidationService` paata hai, tree mein kahin aur ki kisi bhi other instance se separate. Yeh deliberately per-feature ya per-component state isolation ke liye use hota hai (e.g., multiple independent step-form instances wala ek wizard component).

### [new content] The inject() Function (Angular 14+)

Constructor injection ab dependencies pane ka ek hi tarika nahi hai. Functional `inject()` API ab idiomatic hai, especially standalone components, functional guards/interceptors/resolvers, aur kisi bhi jagah jahan aapke paas ek class constructor nahi hota (e.g., `provideRouter` ke functional guards ke andar, ya Signal-based computed setups):

```typescript
import { inject } from '@angular/core';

@Component({ standalone: true, selector: 'app-user' })
export class UserComponent {
  private userService = inject(UserService);
  private route = inject(ActivatedRoute);
}
```

Functional route guard using `inject()` (replaces class-based `CanActivate`):

```typescript
export const authGuard: CanActivateFn = (route, state) => {
  const auth = inject(AuthService);
  const router = inject(Router);
  return auth.isLoggedIn() ? true : router.createUrlTree(['/login']);
};
```

**Interviewers isko kyun probe karte hain:** `inject()` sirf ek "injection context" ke andar kaam karta hai (constructor, field initializer, ya `runInInjectionContext` ke andar run hone wala ek function). Ek classic gotcha question: "Kya aap `setTimeout` callback ke andar `inject()` call kar sakte ho?" — Nahi, directly nahi; ek baar aap ek async callback ke andar ho to injection context chala jaata hai jab tak aap value ko pehle se capture na karo ya call ko `runInInjectionContext()` mein wrap na karo. Constructor injection fully valid rehta hai aur abhi bhi most existing class-based services/components jo use karte hain — `inject()` ek additive hai, replacement nahi, lekin functional guards/resolvers/interceptors ke liye required style hai aur consistency ke liye ab naye standalone projects mein CLI-generated default hai.

---

## Intermediate

### Component Lifecycle Hooks

```mermaid
flowchart TD
    A[constructor] --> B[ngOnChanges]
    B --> C[ngOnInit]
    C --> D[ngDoCheck]
    D --> E[ngAfterContentInit]
    E --> F[ngAfterContentChecked]
    F --> G[ngAfterViewInit]
    G --> H[ngAfterViewChecked]
    H -.repeats on every CD cycle.-> D
    H --> I[ngOnDestroy]
```

| Hook | When | Typical use |
|---|---|---|
| `ngOnChanges(changes: SimpleChanges)` | Jab bhi ek bound `@Input()` change hota hai (first call par `ngOnInit` se pehle) | Updated input-bound properties par react karo; `SimpleChanges` previous vs current values expose karta hai |
| `ngOnInit()` | Ek baar, first `ngOnChanges` ke baad | Initialization logic, e.g., data fetch karna |
| `ngDoCheck()` | Har CD cycle | Angular ke default se beyond custom change detection (mutation detection jo Angular nahi dekh sakta, e.g., in-place array mutation) |
| `ngAfterContentInit()` | Ek baar, `<ng-content>` ke through projected content initialize hone ke baad | Pehli baar projected content access karo |
| `ngAfterContentChecked()` | Projected content ki har check ke baad | Projected content mein updates par react karo |
| `ngAfterViewInit()` | Ek baar, component ki view aur child views initialize hone ke baad | Safely `@ViewChild` aur DOM elements access karo |
| `ngAfterViewChecked()` | Har view check ke baad | Rare use — repeated view updates ko respond karna |
| `ngOnDestroy()` | Component destroy hone se right pehle | Subscriptions, timers, event listeners clean up karo |

Lifecycle hooks nahi lekin closely related:
- **`constructor()`** — class instantiation par run hota hai; *sirf* DI ke liye use karo, business logic ke liye kabhi nahi. **Senior rule: constructor mein business logic (API calls, DOM access) kabhi mat daalo** — inputs abhi bound nahi hote aur injected services saare contexts mein use ke liye fully ready na ho.
- **`SimpleChanges`** (`ngOnChanges` ke andar) — har changed input ke liye `.previousValue` / `.currentValue` / `.firstChange` provide karta hai.

`constructor` vs `ngOnInit`:

| | `constructor` | `ngOnInit` |
|---|---|---|
| Runs | Class instantiate hone par | Angular input bindings set karne ke baad |
| Use for | Sirf dependency injection | API calls, initialization logic |
| Inputs available? | Nahi (abhi bound nahi) | Haan |

### ViewChild / ViewChildren / ContentChild / ContentChildren

`@ViewChild`/`@ViewChildren` **is component ke apne template** mein declared elements/components access karte hain. `@ContentChild`/`@ContentChildren` `<ng-content>` ke through **parent se projected in** elements access karte hain.

```typescript
@ViewChild(MatPaginator) paginator!: MatPaginator;
@ViewChildren(ItemComponent) items!: QueryList<ItemComponent>;
```

| | Source | Available after |
|---|---|---|
| `@ViewChild` / `@ViewChildren` | Component ka apna template | `ngAfterViewInit` |
| `@ContentChild` / `@ContentChildren` | Parent se projected content | `ngAfterContentInit` |

**Interview one-liner:** "View = jo main own karta hoon, Content = jo parent mujhe deta hai."

`@ViewChild` commonly yeh karne ke liye use hota hai: DOM properties read karna/native methods call karna (focus, scroll, measure), child component public methods ko directly call karna, ya third-party non-Angular UI libraries integrate karna jinhe ek DOM handle chahiye.

**[new content] Signal-based queries (Angular 17.3+):** `viewChild()`, `viewChildren()`, `contentChild()`, aur `contentChildren()` function-based equivalents ab exist karte hain, jo `!` non-null assertion + lifecycle timing dance require karne ke bajaye Signals return karte hain:

```typescript
export class UserComponent {
  paginator = viewChild(MatPaginator);      // Signal<MatPaginator | undefined>
  items = viewChildren(ItemComponent);      // Signal<readonly ItemComponent[]>
}
```

Yeh us historical gotcha ko remove karta hai jahan `@ViewChild` fields `ngAfterViewInit` tak `undefined` hote hain — signal form "not yet available" ko type (`| undefined`) ka ek explicit part banata hai ek runtime surprise ke bajaye, aur `computed()`/`effect()` ke saath cleanly integrate hota hai.

### Component Communication

| Direction | Mechanism |
|---|---|
| Parent → Child | `@Input()` |
| Child → Parent | `@Output()` + `EventEmitter` |
| Sibling / unrelated | Ek `Subject`/`BehaviorSubject` (ya Signal) wali shared service |
| Across routes | Route params, query params, ya router navigation `state` |

`EventEmitter` ek RxJS `Subject` ke upar ek thin wrapper hai, strictly component `@Output()` communication ke liye intended — services ke andar ek general pub/sub mechanism ke roop mein recommended **nahi** hai (wahan instead ek plain `Subject`/`BehaviorSubject` use karo).

**Senior guidance:** direct parent-child ke liye `@Input`/`@Output` prefer karo; ek shared service ke liye sirf tab reach karo jab data ko unrelated components ke across cross karna ho ya route changes ke across persist karna ho. Services ke through data pass karna cross-cutting shared state (auth, cart, theme) ke liye appropriate hai lekin simple parent-child relationships ke liye overkill hai — aur indirection add karta hai.

Template reference variables (`#var`) trivial DOM access ke liye `@ViewChild` ka ek lighter-weight alternative hain:

```html
<input #txt />
<button (click)="print(txt.value)">Print</button>
```

Inko use karo jab aapko template mein hi bas ek quick handle chahiye ho, small UI logic ke liye component-class coupling avoid karte hue.

**[version 19 upgrade]** Angular 7 mein `@Input()`/`@Output()` + `EventEmitter` hi Parent ↔ Child communication ka **sirf ek** tareeka tha. Angular 19 mein signal-based `input()`, `output()`, aur `model()` **stable** ho gaye (pehle v17.1 se developer preview mein), aur inke saath official migration schematics bhi aaye — ab yeh recommended default hain, decorators still supported hain.

*Kya badla aur implement kaise karein:*
```typescript
export class UserCardComponent {
  // v7 style — still works, but no longer the default recommendation
  @Input() name!: string;
  @Output() nameChange = new EventEmitter<string>();

  // [version 19 upgrade] — signal-based equivalents
  name = input.required<string>();          // Signal<string>, required
  age = input(0);                            // Signal<number>, with default
  selected = output<string>();                // replaces @Output() + EventEmitter

  // model() gives two-way binding in one line (parent can [(name)]="...")
  name = model<string>('');
}
```
- **Reading the value:** call it like a function — `this.name()` instead of `this.name`.
- **Two-way binding:** `model()` replaces the old `@Input() + @Output() nameChange` pair used for `[(ngModel)]`-style custom two-way bindings — the parent just does `<user-card [(name)]="userName" />`.
- **Migrating an existing codebase:** run the official schematics instead of hand-editing every component:
  ```bash
  ng generate @angular/core:signal-input-migration
  ng generate @angular/core:signal-queries-migration
  ng generate @angular/core:output-migration
  ```
- **Interview angle:** "Why move `@Input`/`@Output` to Signals?" — same fine-grained change-detection benefit as `signal()`/`computed()` elsewhere: Angular knows exactly which binding depends on which input, which is what makes zoneless change detection safe for input-driven components.

### Routing and Navigation

```typescript
const routes: Routes = [
  { path: 'home', component: HomeComponent },
  { path: 'about', component: AboutComponent }
];

@NgModule({
  imports: [RouterModule.forRoot(routes)],
  exports: [RouterModule]
})
export class AppRoutingModule { }
```

```html
<a routerLink="/home">Home</a>
<router-outlet></router-outlet>
```

Programmatic navigation:

```typescript
constructor(private router: Router) { }
navigateToHome() {
  this.router.navigate(['/home']);
}
```

Wildcard route:

```typescript
{ path: '**', component: PageNotFoundComponent }
```

Route parameters:

```typescript
{ path: 'product/:id', component: ProductComponent }
```
```typescript
constructor(private route: ActivatedRoute) {}
this.route.snapshot.paramMap.get('id');
```

Query parameters:

```typescript
this.router.navigate(['/products'], { queryParams: { category: 'electronics' } });
this.route.snapshot.queryParamMap.get('category');
```

**Note:** `snapshot` sirf navigation ke moment par value capture karta hai — agar component ko destroy/recreate hue bina different params ke saath khud par re-navigate kiya ja sakta hai (e.g., `/product/1` → `/product/2`), `snapshot` update nahi hoga. Jab same component instance param changes ke across reuse ho sakta hai to observable form use karo (`this.route.paramMap.subscribe(...)` ya `switchMap` ke through piped `this.route.paramMap`) — ek very common gotcha/bug source aur popular interview question.

Lazy loading feature modules:

```typescript
const routes: Routes = [
  { path: 'dashboard', loadChildren: () => import('./dashboard/dashboard.module').then(m => m.DashboardModule) }
];
```

**[new content] Lazy-loading standalone components/routes (Angular 14+):** modern lazy loading ko ab wrapper NgModule ki bilkul zaroorat nahi hai:

```typescript
const routes: Routes = [
  { path: 'login', loadComponent: () => import('./login/login.component').then(c => c.LoginComponent) },
  { path: 'admin', loadChildren: () => import('./admin/admin.routes').then(m => m.ADMIN_ROUTES) }
];
```
Yahan `admin.routes.ts` ek `AdminModule` ke bajaye ek plain `Routes` array export karta hai (`export const ADMIN_ROUTES: Routes = [...]`) — ek less indirection layer, aur yeh standalone components ke saath naturally compose hota hai.

### Route Guards

| Guard | Purpose |
|---|---|
| `CanActivate` | Ek route protect karo — e.g., sirf logged-in users `/dashboard` access kar sakte hain |
| `CanActivateChild` | Saare child routes protect karo, e.g. `/admin/*` ke neeche sab kuch |
| `CanLoad` (legacy) / `CanMatch` (current) | Agar user ke paas access nahi hai to ek lazy module ko load hone se bilkul prevent karo |
| `CanMatch` | Role/feature flag ke basis par decide karo ki ek route *match* bhi karta hai ya nahi — same path ke liye ek different route definition tak fall through hone allow kar sakta hai |
| `CanDeactivate` | Ek route se navigation away ko block karo — e.g., `/profile-edit` par unsaved changes |

> **`CanLoad` vs `CanMatch` par Note:** `CanLoad` ko `CanMatch` ke favor mein phase out kiya ja raha hai, jo strictly zyada capable hai — `CanMatch` non-lazy routes ko bhi gate kar sakta hai aur Angular ko *next* matching route configuration try karne deta hai agar yeh false return kare (A/B routes ya feature-flagged route swaps ke liye useful), jabki `CanLoad` sirf lazy-module loading ko prevent karta tha. Naye code ko `CanMatch` use karna chahiye.

Full modern guard-based routing config (functional style, Angular 15+):

```typescript
const routes: Routes = [
  {
    path: 'login',
    loadComponent: () => import('./login/login.component').then(c => c.LoginComponent)
  },
  {
    path: 'dashboard',
    canMatch: [authMatchGuard],
    canActivate: [authGuard],
    loadComponent: () => import('./dashboard/dashboard.component').then(c => c.DashboardComponent)
  },
  {
    path: 'admin',
    canMatch: [adminLoadGuard, adminMatchGuard],
    loadChildren: () => import('./admin/admin.routes').then(m => m.ADMIN_ROUTES)
  },
  {
    path: 'profile-edit',
    canDeactivate: [formExitGuard],
    loadComponent: () => import('./profile/profile-edit.component').then(c => c.ProfileEditComponent)
  }
];
```

**[new content] Functional guard implementation example** (source notes ne guards ko conceptually reference kiya tha lekin ek full class/functional implementation nahi dikhaya — is gap ko fill karte hue):

```typescript
export const authGuard: CanActivateFn = () => {
  const auth = inject(AuthService);
  const router = inject(Router);
  return auth.isLoggedIn() || router.createUrlTree(['/login']);
};

export const formExitGuard: CanDeactivateFn<ProfileEditComponent> = (component) => {
  if (component.form.dirty) {
    return confirm('You have unsaved changes. Leave anyway?');
  }
  return true;
};
```

Class-based guards (`implements CanActivate`) abhi bhi kaam karte hain aur older/enterprise codebases mein common hain — functional guards modern idiomatic style hain kyunki wo simple boolean checks ke liye ek extra injectable class avoid karte hain.

### Forms: Template-driven vs Reactive

| | Template-driven | Reactive |
|---|---|---|
| Control style | `ngModel` ke through template mein declared | `FormControl`/`FormGroup` ke through component class mein declared |
| Scalability | Less scalable | Complex forms ke liye enterprise standard |
| Testability | Harder (logic template mein rehta hai) | Easier (pure TS, DOM ki zaroorat nahi) |
| Dynamic fields/validation | Awkward | Natural fit |
| Use when | Small forms, simple validation | Complex forms, dynamic fields, conditional validation |

Template-driven:

```html
<form #form="ngForm">
  <input type="text" [(ngModel)]="name" name="name">
</form>
```

Reactive:

```typescript
form = new FormGroup({
  name: new FormControl('')
});
```
```html
<input type="text" [formControl]="form.controls['name']">
```

`FormBuilder` construction ko simplify karta hai:

```typescript
constructor(private fb: FormBuilder) { }
form = this.fb.group({
  name: ['', Validators.required]
});
```

Validation:

```typescript
form = new FormGroup({
  email: new FormControl('', [Validators.required, Validators.email])
});
```
```html
<p *ngIf="form.controls.email.errors?.required">Email is required</p>
```

Built-in validators: `required`, `minLength`, `maxLength`, `pattern`, `email`, `min`/`max`. Inko ek array ke through ya `Validators.compose([...])` ke through compose karo.

`FormGroup` vs `FormArray`:

```typescript
userForm = new FormGroup({
  name: new FormControl(''),
  phones: new FormArray([
    new FormControl('12345')
  ])
});
```

**Interview one-liner (source se, verbatim rakha gaya — yeh good hai):** "A `FormGroup` is a fixed, named group of controls, while a `FormArray` is a dynamic, indexed collection of controls used when the number of inputs is unknown or user-driven."

Dynamic/conditional validation:

```typescript
control.setValidators([Validators.required]);
control.clearValidators();
control.updateValueAndValidity();
```

Use hota hai jab ek field ki validity doosre par depend karti hai (e.g., "state" sirf tab required banta hai jab "country" states wala ek country ho).

Checking validity:

```typescript
form.valid           // overall
control.errors       // specific control's errors
control.touched      // user has focused and left
control.dirty        // value has changed from initial
```

`[ngModelOptions]="{standalone: true}"` Angular ko batata hai ki us `ngModel` control ko surrounding `ngForm` ke saath register *na* kare — use hota hai jab aapko ek aise input par `ngModel` ke through two-way binding chahiye jisko parent form ki overall validity/value mein participate nahi karna chahiye (e.g., markup convenience ke liye ek `<form>` ke andar sitting ek UI-only filter field).

`valueChanges` — kisi bhi `FormControl`/`FormGroup`/`FormArray` par available ek RxJS `Observable`, jo har user change par latest value emit karta hai:

```typescript
name = new FormControl('');

ngOnInit() {
  this.name.valueChanges.subscribe(value => {
    console.log('Name changed:', value);
  });
}
```

Ek `FormGroup` par, yeh kisi bhi child change par poore group ka value object emit karta hai. Iske liye use hota hai: live validation, real-time search, buttons enable/disable karna, autosave, dynamic form behavior, aur user type karte hue lists filter karna. **Yeh sirf reactive forms ke liye exist karta hai** — template-driven forms is API ko directly expose nahi karte (halaanki `ngModelChange` aapko ek similar per-control event deta hai).

Form submission:

```html
<form [formGroup]="form" (ngSubmit)="onSubmit()">
  <button type="submit">Submit</button>
</form>
```
```typescript
onSubmit() {
  console.log(this.form.value);
}
```

Reset karna: `this.form.reset();`

Custom validators — `ValidationErrors | null` return karne wala ek function, business rules ya cross-field validation ke liye use hota hai (e.g., "password" ko "confirmPassword" ke equal hona chahiye):

```typescript
export function passwordsMatch(group: AbstractControl): ValidationErrors | null {
  const pass = group.get('password')?.value;
  const confirm = group.get('confirmPassword')?.value;
  return pass === confirm ? null : { passwordMismatch: true };
}
```

Aap ek `<form>` tag ke bina Angular validators use **kar sakte ho** — standalone `FormControl`/`FormGroup` instances kisi bhi `<form>` element se independently kaam karte hain; yeh inline editable fields ya search boxes ke liye common hai.

### [new content] Typed Reactive Forms (Angular 14+)

Angular 14 se pehle, `FormGroup`/`FormControl` effectively `any`-typed the — `form.value` aapko `{ [key: string]: any }` deta tha, isliye `form.get('emial')` mein typos runtime par silently fail hote the. Angular 14 ne **strictly typed forms** introduce kiye:

```typescript
interface ProfileForm {
  name: FormControl<string>;
  email: FormControl<string>;
  phones: FormArray<FormControl<string>>;
}

const form = new FormGroup<ProfileForm>({
  name: new FormControl('', { nonNullable: true }),
  email: new FormControl('', { nonNullable: true, validators: [Validators.email] }),
  phones: new FormArray<FormControl<string>>([])
});

form.value.name; // string | undefined — typed!
```

`FormBuilder.group()` ko ek explicit type ke saath use karna, ya inference ko flow through hone dena, aapko typo'd control names aur wrong value types ke liye compile-time errors deta hai — compile-time safety ke aadi ek .NET developer ke liye ek huge win. `AbstractControl<T>`/`FormControl<T>` ne `nonNullable` option bhi introduce kiya, kyunki ek untyped `FormControl<string>` par `.reset()` pehle `null` par reset ho jaata tha, silently apparent `string` type violate karte hue — ek well-known typed-forms gotcha jo mention karne layak hai (accurate hone ke liye aapko `nonNullable: true` opt in karna hoga ya control ko `FormControl<string | null>` type karna hoga).

**Untyped forms abhi bhi exist karte hain** (`UntypedFormGroup`, `UntypedFormControl`) old code ke liye ek escape hatch/migration aid ke roop mein — agar pucha jaaye to yeh explain karne ke liye ready raho ki wo kyun exist karte hain.

### HttpClient and Interceptors

```typescript
import { HttpClient } from '@angular/common/http';
constructor(private http: HttpClient) { }

getData() {
  return this.http.get('https://api.example.com/data');
}
```

GET / POST / PUT / DELETE:

```typescript
this.http.get('https://api.example.com/users').subscribe(response => console.log(response));

const data = { name: 'John' };
this.http.post('https://api.example.com/users', data).subscribe(response => console.log(response));

const updatedData = { name: 'John Doe' };
this.http.put('https://api.example.com/users/1', updatedData).subscribe(response => console.log(response));

this.http.delete('https://api.example.com/users/1').subscribe(response => console.log(response));
```

Custom headers:

```typescript
const headers = new HttpHeaders().set('Authorization', 'Bearer token');
this.http.get('https://api.example.com/data', { headers }).subscribe();
```

`HttpParams` ke through query params:

```typescript
const params = new HttpParams().set('search', 'Angular');
this.http.get('https://api.example.com/items', { params }).subscribe();
```

`catchError` ke saath error handling:

```typescript
import { catchError } from 'rxjs/operators';
import { throwError } from 'rxjs';

this.http.get('https://api.example.com/data').pipe(
  catchError(error => {
    console.error('Error occurred:', error);
    return throwError(() => error);
  })
).subscribe();
```

**JSONP** — ek legacy technique jo ek `<script>` tag ke through cross-domain data load karti hai JSON ko ek callback mein wrap karke, sirf tab use hoti hai jab CORS supported na ho aur sirf GET requests ke liye. **[new content — currency note]** JSONP 2026 mein legacy/rarely used hai; virtually saari modern APIs CORS ko properly support karti hain, aur JSONP ke real security downsides hain (response se arbitrary script execution). Isko sirf tab mention karo jab directly pucha jaaye; isko ek current recommendation ke roop mein volunteer mat karo.

Class-based `HttpInterceptor`:

```typescript
@Injectable()
export class AuthInterceptor implements HttpInterceptor {
  // Inject a token source rather than reading storage directly — this keeps the
  // interceptor testable and leaves the storage posture a separate decision.
  constructor(private auth: TokenService) {}

  intercept(req: HttpRequest<any>, next: HttpHandler): Observable<HttpEvent<any>> {
    const token = this.auth.getAccessToken();
    if (!token) { return next.handle(req); }   // don't send "Bearer null"
    const requestWithToken = req.clone({
      setHeaders: { Authorization: `Bearer ${token}` }
    });
    return next.handle(requestWithToken);
  }
}
```

**`TokenService` ko token kahan se milta hai?** Yeh actual security decision hai, aur dono postures equivalent nahi hain:

- **`localStorage`/`sessionStorage`** — simplest, real code mein extremely common, lekin kisi bhi injected script se readable, isliye koi bhi XSS token theft ban jaata hai. Sirf low-stakes apps ke liye defensible.
- **BFF + HttpOnly cookie** — token browser JavaScript tak bilkul pohochta hi nahi; interceptor ek `Authorization` header ke bajaye `withCredentials: true` bhejta hai. Zyada backend investment, bahut smaller blast radius. Yeh kisi bhi enterprise-scale cheez ke liye defensible answer hai (BFF architecture section dekho).

Interview framing: `localStorage` version ko best practice ke roop mein present mat karo. Trade-off ko explicitly state karo aur bolo ki us app ke liye aap kaunsi posture choose karoge, aur kyun.

Registering it:

```typescript
providers: [
  { provide: HTTP_INTERCEPTORS, useClass: AuthInterceptor, multi: true }
]
```

Global error handling in an interceptor:

```typescript
intercept(req: HttpRequest<any>, next: HttpHandler) {
  return next.handle(req).pipe(
    catchError(err => {
      if (err.status === 401) { /* redirect to login */ }
      if (err.status === 500) { /* show toast */ }
      return throwError(() => err);
    })
  );
}
```

Interceptors request ke liye registration order mein execute hote hain, aur response ke liye reverse order mein — ek classic "pipeline draw karo" interview whiteboard question. Common production interceptor chain: **Auth → Loader → Error handler**. `multi: true` ke through multiple interceptors fully supported hain.

### [new content] Functional Interceptors (Angular 15+)

Class-based `HttpInterceptor` abhi bhi kaam karta hai, lekin modern idiomatic style (aur `provideHttpClient` dwara directly supported sirf ek style) ek **functional interceptor** hai:

```typescript
export const authInterceptor: HttpInterceptorFn = (req, next) => {
  const token = inject(AuthService).getToken();
  const cloned = req.clone({ setHeaders: { Authorization: `Bearer ${token}` } });
  return next(cloned);
};
```

`HTTP_INTERCEPTORS`/`multi: true` boilerplate ke bina registered:

```typescript
provideHttpClient(withInterceptors([authInterceptor, loaderInterceptor, errorInterceptor]))
```

Array mein order execution order hai — pehle jaisa hi same directional pipeline concept, bas less ceremony. Yeh wo version hai jo aapko default demonstrate karna chahiye jab tak specifically class-based legacy API ke baare mein na pucha jaaye.

---

## Advanced

### RxJS and Observables

RxJS (Reactive Extensions for JavaScript) **Observables** use karke reactive/async programming ke liye ek library hai — streams jo `next`, `error`, aur `complete` notifications ke through time ke saath zero, one, ya many values emit kar sakte hain.

```typescript
const obs = new Observable(observer => {
  observer.next('Hello');
  observer.complete();
});
obs.subscribe(data => console.log(data));
```

- **Observable** — data producer/stream.
- **Observer** — consumer (jiske paas `next()`, `error()`, `complete()` hai).

Observable vs Promise:

| Feature | Observable | Promise |
|---|---|---|
| Lazy execution | Haan — `.subscribe()` tak kuch bhi run nahi hota | Nahi — creation par immediately execute hota hai |
| Multiple values | Haan (stream) | Nahi (single resolved value) |
| Cancelable | Haan (`unsubscribe()`) | Nahi |
| Operators (map, filter, etc.) | Haan, rich operator library | Nahi |

Angular `HttpClient`, route params, reactive forms (`valueChanges`), aur event streams mein Observables ko heavily use karta hai.

**Subscribe / unsubscribe karna:**

```typescript
this.http.get(url).subscribe(
  data => console.log(data),
  err => console.log(err)
);
```

Jab tak `subscribe()` call nahi hota kuch bhi execute nahi hota — Observables lazy hote hain (default se "cold").

Unsubscribe karne / leaks avoid karne ke tarike:
- `Subscription` ko store karo aur `ngOnDestroy()` mein `.unsubscribe()` call karo.
- `takeUntil(destroySubject$)` pattern use karo.
- Template mein **`async` pipe** use karo — Angular automatically subscribe/unsubscribe karta hai.

```typescript
ngOnDestroy() {
  this.subscription.unsubscribe();
}
```

**[new content] Modern unsubscribe pattern — `takeUntilDestroyed()` (Angular 16+):** Manual `Subject`-based `takeUntil` boilerplate largely built-in `takeUntilDestroyed()` operator se supersede ho gaya hai, jo automatically Angular ke `DestroyRef` mein hook karta hai:

```typescript
export class UserComponent {
  private destroyRef = inject(DestroyRef);

  ngOnInit() {
    this.userService.getUsers()
      .pipe(takeUntilDestroyed(this.destroyRef))
      .subscribe(users => this.users = users);
  }
}
```
Jab ek injection context mein constructor/field initializer ke andar call kiya jaaye, aap `destroyRef` argument ko entirely omit kar sakte ho. Yeh "ek `destroy$` Subject add karna bhool gaya" bugs ki ek entire category ko remove karta hai — proactively mention karne layak hai kyunki yeh dikhata hai ki aap current ho.

### Subjects, BehaviorSubject, Multicasting

**Multicasting** ka matlab hai ek Observable execution multiple subscribers ke across shared hota hai, isliye sabko same emissions receive hoti hain har ek ke ek separate execution trigger karne ke bajaye. Commonly `Subject` ya `share`/`shareReplay` jaise operators ke saath implement kiya jaata hai.

**Subject** — ek Observable (subscribable) aur ek Observer dono (aap `.next()` ke saath values push karte ho).
- Last value store **nahi** karta.
- New subscribers ko previously emitted values receive **nahi** hoti.
- Sirf wo values emit karta hai jo subscription ke *baad* occur hoti hain.

```typescript
const subject = new Subject();
subject.next(1); // nobody receives (no subscriber yet)
subject.subscribe(v => console.log('A:', v));
subject.next(2); // A: 2
subject.next(3); // A: 3
```

**BehaviorSubject** — latest value store karta hai, ek initial value require karta hai, aur new subscribers ko immediately current value emit karta hai.

```typescript
const subject = new BehaviorSubject(0);
subject.subscribe(v => console.log('A:', v));
subject.next(1);
subject.next(2);
subject.subscribe(v => console.log('B:', v)); // B: 2 immediately
```

Simple rule of thumb: **Observable → data read karo; Subject → events send karo; BehaviorSubject → latest state store karo.**

| Feature | Subject | BehaviorSubject |
|---|---|---|
| Stores previous value? | Nahi | Haan |
| Requires initial value? | Nahi | Haan |
| New subscriber gets last value? | Nahi | Haan |
| Typical use case | Events, clicks, streams | App/auth/theme/form state |
| Emits immediately on subscribe? | Nahi | Haan |

**Is topic par common interview Q&A (source se, answered):**
- *`Subject` ko "multicast" kyun kaha jaata hai?* Kyunki ek emitted value simultaneously saare current subscribers ko broadcast hoti hai, ek plain function call ya Promise ke unlike jo single-consumer hota hai.
- *Shared state ke liye `BehaviorSubject` kyun preferred hai?* Yeh latest value retain karta hai aur isko new subscribers ko immediately hand karta hai — late subscribers ke liye koi "missed" state nahi, jo auth status ya theme jaisi cheezon ke liye matter karta hai jinko ek component value set hone ke well after subscribe kar sakta hai.
- *`ReplaySubject` kab use karein?* Jab new subscribers ko some/all previously emitted values chahiye, sirf latest wali nahi (e.g., last N chat messages replay karna).
- *`BehaviorSubject` ke bajaye `shareReplay` kab use karein?* Jab aap ek *source* Observable (jaise ek single HTTP call) ke result ko multiple subscribers ke among cache/share karna chahte ho, ek state container ko manually manage kiye bina — `shareReplay(1)` ek HTTP-backed Observable ko "hot" aur late subscribers ke liye cached banane ka idiomatic tarika hai, HTTP result ko manually ek `BehaviorSubject` mein push karne ke versus.

**Best short interview answer:** "Multicasting ka matlab hai ek single Observable execution ko multiple subscribers ke saath share karna. Angular mein, state sharing aksar `BehaviorSubject` use karke ki jaati hai kyunki yeh latest value store karta hai aur isko new subscribers ko immediately send karta hai — auth state, cart count, ya app settings jaise shared data ke liye useful."

### RxJS Operators Cheat Sheet

`pipe()` ek Observable ko mutate kiye bina uske upar operators chain karta hai (Observables immutable hote hain; har operator ek naya Observable return karta hai). Yeh ek processing pipeline build karta hai; `.subscribe()` tak kuch bhi execute nahi hota.

```typescript
this.http.get<User[]>('/api/users')
  .pipe(map(users => users.filter(u => u.active)))
  .subscribe(activeUsers => console.log(activeUsers));
```

`filter`:

```typescript
of(1, 2, 3, 4).pipe(filter(num => num > 2)).subscribe(console.log); // 3, 4
```

**Flattening operators — classic senior cheat sheet:**

| Operator | Behavior | Best for |
|---|---|---|
| `mergeMap` | Saare inner Observables ko concurrently run karta hai; previous wale cancel nahi karta; order guaranteed nahi | Parallel/independent API calls |
| `switchMap` | Jab ek naya source value aata hai to previous inner Observable ko cancel karta hai, sirf latest wala rakhta hai | Search-as-you-type, live filters, stale/duplicate requests avoid karna |
| `concatMap` | Inner Observables ko queue karta hai, unko strictly one after another run karte hue | Sequential operations jinhe order preserve karna zaroori hai (e.g., ordered batch saves) |
| `exhaustMap` | Naye source emissions ko ignore karta hai jab tak current inner Observable abhi bhi run ho raha hai | Duplicate submits prevent karna (e.g., ek save in flight hote hue ek "Save" button) |

```typescript
this.searchInput.valueChanges.pipe(
  debounceTime(300),
  switchMap(term => this.http.get(`/api/search?q=${term}`))
).subscribe(results => console.log(results));
```

`forkJoin` — diye gaye **saare** Observables ke complete hone ka wait karta hai, phir final values ke ek array/object ke saath ek baar emit karta hai (conceptually `Promise.all` jaisa):

```typescript
forkJoin([
  this.http.get('https://api.example.com/users'),
  this.http.get('https://api.example.com/posts')
]).subscribe(([users, posts]) => console.log(users, posts));
```

**Gotcha:** `forkJoin` sirf tab emit karta hai jab *saare* source Observables complete hon — ek Observable jo kabhi complete nahi hota (most Subjects ya ek infinite stream jaisa) `forkJoin` ko forever hang kar dega. Yeh ek favorite interviewer gotcha hai.

Push/Reactive vs Pull/Imperative — ek useful mental-model question: **pull** systems mein consumer actively data ke liye poochta hai (ordinary function calls, ek array iterate karna); **push** systems mein producer ready hone par consumer ko automatically data bhejta hai (Observables, events, Promises). Reactive/push-based systems async, UI-driven flows ke liye better scale karte hain kyunki consumers ko poll karne ki zaroorat nahi hoti.

### [new content] Angular Signals (Angular 16+)

Yeh 2018-era note set ke against sabse important gaps mein se ek hai, aur arguably 2025-2026 mein sabse zyada pucha jaane wala senior interview question hai ki "Angular mein naya kya hai".

**Signals kya hote hain:** `@angular/core` mein built-in ek fine-grained reactive primitive (RxJS nahi) jo ek value ko represent karta hai aur jab woh change hoti hai to interested consumers ko notify karta hai. Zone.js-driven change detection ke unlike, Signals Angular ko *exactly* yeh batate hain ki kaunsa binding kis piece of state par depend karta hai, jisse bahut zyada surgical DOM updates possible hote hain.

```typescript
import { signal, computed, effect } from '@angular/core';

export class CounterComponent {
  count = signal(0);
  doubled = computed(() => this.count() * 2); // auto-recomputes when count changes

  increment() {
    this.count.update(v => v + 1); // or this.count.set(v)
  }

  constructor() {
    effect(() => {
      console.log('Count changed to', this.count()); // reactive side-effect
    });
  }
}
```

```html
<p>Count: {{ count() }}</p>
<p>Doubled: {{ doubled() }}</p>
```

Value read karne ke liye function-call syntax (`count()`) note karo — yahi wo cheez hai jo Angular ke compiler ko dependencies track karne deti hai.

**Signal-based component inputs/outputs (Angular 17.1+):**

```typescript
export class UserCardComponent {
  name = input.required<string>();       // signal-based @Input, required
  age = input(0);                        // signal-based @Input with default
  selected = output<string>();           // signal-based @Output
}
```

**Signals kyun exist karte hain (woh "why" jo interviewer sunna chahta hai):** RxJS powerful hai lekin uska ek real learning curve hai, subscription-management bugs ko encourage karta hai (leaks, `async` pipe ka template mein overuse), aur — critically — Angular ka Zone.js-based change detection yeh nahi jaan sakta ki *exactly kaunsa binding* change hua, isliye woh poore component subtrees ko re-check karta hai even jab sirf ek value change hui ho. Signals Angular ko directly dependency graph de dete hain, jo **zoneless change detection** (neeche dekho) ke liye technical enabler hai aur future mein coarse component-tree walks ke instead fine-grained, sub-component-level DOM patching (glass-box change detection) ke liye bhi.

**Signals RxJS ko replace nahi karte.** Signals synchronous, "hamesha current value hoti hai" wale state ko model karte hain (UI state, derived view state). Asynchronous event streams, complex async orchestration (debouncing, retries, cancellation, multiple async sources ko combine karna) ke liye RxJS hi right tool rehta hai — yeh woh cheezein hain jinhe Signals deliberately solve karne ki koshish nahi karte. Angular interop utilities ship karta hai: `toSignal()` (Observable → Signal) aur `toObservable()` (Signal → Observable) `@angular/core/rxjs-interop` se, jo explicitly same app mein dono models ko bridge karne ke liye design kiye gaye hain.

```typescript
users = toSignal(this.userService.getUsers(), { initialValue: [] });
```

### [new content] RxJS vs Signals — Kab Kaunsa Use Karein

```mermaid
flowchart LR
    subgraph Signals
    S1[Synchronous state]
    S2[Derived/computed UI values]
    S3[Component inputs/outputs]
    end
    subgraph RxJS
    R1[Async event streams]
    R2[HTTP requests]
    R3[Debounce / retry / cancellation]
    R4[Complex operator pipelines]
    end
    Signals <-- toSignal / toObservable --> RxJS
```

| Concern | Signals | RxJS |
|---|---|---|
| Mental model | Pull-based, hamesha current value hoti hai | Push-based, time ke saath events ka stream |
| Async support | Nahi — sirf synchronous (async ke liye RxJS ke saath compose hota hai) | Haan, native |
| Cancellation / retries / debouncing | Built-in nahi hai | Rich operator support |
| Learning curve | Low | Zyada (operators, marble diagrams, subscription mgmt) |
| Change detection integration | Native, fine-grained, zoneless enable karta hai | `async` pipe ya manual subscription + CD trigger chahiye |
| Typical use | Component/view state, derived values, template bindings | HTTP calls, WebSockets, complex async pipelines, form `valueChanges` |

**Likely interview framing:** "Hum ek ko doosre ke over choose nahi kar rahe — Signals component-local state aur template bindings ke liye default banate ja rahe hain, jabki RxJS asynchronous streams aur complex operator composition ke liye backbone bana rehta hai. `toSignal`/`toObservable` interop hi wo tarika hai jisse yeh dono is transition ke dauraan same codebase mein coexist karte hain."

**[version 19 upgrade]** Angular 7 mein derived state ko manually recompute karna padta tha (constructor/lifecycle hooks mein ek dusri property se ek property set karke), aur async data-fetching state (loading/error/value) ko hamesha ek `Subject`/RxJS pipeline ke through hand-roll karna padta tha. Angular 19 ne do naye Signal primitives add kiye jo yeh dono gaps close karte hain:

**`linkedSignal()`** — ek writable Signal jo apne aap khud ko reset/recompute kar leta hai jab uska source Signal change hota hai (isse pehle aapko ek `effect()` likhna padta tha jo manually dusre signal ko reset karta):
```typescript
export class ProductListComponent {
  products = signal<Product[]>([...]);
  // selectedId automatically resets to the first product whenever the list changes
  selectedId = linkedSignal(() => this.products()[0]?.id);
}
```

**`resource()` (experimental)** — Signals ko asynchronous operations ke saath bridge karta hai; ek "async-aware `computed()`" ki tarah socho jo `loading`/`error`/`value` states khud manage karta hai:
```typescript
export class UserDetailComponent {
  userId = input.required<string>();

  userResource = resource({
    request: () => ({ id: this.userId() }),
    loader: ({ request }) => fetch(`/api/users/${request.id}`).then(r => r.json()),
  });
  // userResource.value(), userResource.isLoading(), userResource.error()
}
```
- **Kab use karein:** `linkedSignal` jahan bhi aapko ek "reset when the source changes" pattern chahiye (selected tab/item, form default recompute). `resource()` simple fetch-on-input-change data loading ke liye — abhi bhi complex retry/debounce/cancellation orchestration ke liye RxJS + `HttpClient` prefer karo, kyunki `resource()` experimental hai aur RxJS jitna operator support nahi deta.
- **Interview angle:** Yeh dono 2026-era interviews mein "what's the newest thing in Angular" follow-up ke liye good talking points hain — reasonable hai inhe "still evolving/experimental" ki tarah frame karna agar directly stability ke baare mein poocha jaaye.

### State Management (NgRx aur Alternatives)

State management application-wide data ko manage karta hai aur components ko consistent rakhta hai. Common approaches, roughly formality/complexity ke increasing order mein:

- **Service-based state** (simple; ek service jo `BehaviorSubject` ya Signal hold karti hai)
- **RxJS with `BehaviorSubject`**
- **NgRx** (Redux pattern, RxJS-based)
- **Akita, NGXS, Apollo GraphQL (client cache)** — alternatives ke roop mein mention kiya gaya

**NgRx** — RxJS ke upar Redux-pattern state management.

| Concept | Role |
|---|---|
| Store | State ke liye central, single source of truth |
| Actions | Plain objects jo describe karte hain ki *kya hua* |
| Reducers | Pure functions jo action se new state compute karte hain |
| Effects | Side effects handle karte hain (API calls, etc.), aur further actions dispatch karte hain |
| Selectors | State ke specific, memoized slices retrieve karte hain |

```typescript
export const increment = createAction('INCREMENT');

const counterReducer = createReducer(
  initialState,
  on(increment, state => ({ count: state.count + 1 }))
);

@Injectable()
export class MyEffects {
  loadData$ = createEffect(() => this.actions$.pipe(
    ofType(loadData),
    mergeMap(() => this.http.get('/api/data').pipe(map(data => loadDataSuccess({ data }))))
  ));

  constructor(private actions$: Actions, private http: HttpClient) { }
}

export const selectCount = (state: AppState) => state.count;
```

Setup: `ng add @ngrx/store`.

NgRx vs `BehaviorSubject`-based state:

| Feature | NgRx | BehaviorSubject |
|---|---|---|
| Complexity | Zyada | Kam |
| Structure | Actions, reducers, effects | Simple service-based state |
| Performance at scale | Large apps ke liye optimized | Small/medium apps ke liye theek-thaak |
| Side effects | Effects ke through handle hota hai | Service methods mein handle hota hai |

**Senior guidance ki NgRx kab reach karein:** NgRx ki ceremony (actions/reducers/effects/selectors, aur boilerplate — even naye `createActionGroup`/`createFeature` helpers ke saath bhi) large teams aur complex, cross-cutting shared state wale apps mein pay off karti hai, jahan strict unidirectional data flow, time-travel debugging aur bahut contributors ke across strong conventions ki zarurat hoti hai. Small-to-medium apps ya well-bounded feature state ke liye, signal/service-based store usually onboard aur maintain karne mein simpler hota hai — interviewers chahte hain ki aap apna choice justify karo, NgRx ko reflexively default na bana do.

### [new content] SignalStore / NgRx Signals (Angular 17+)

NgRx ab `@ngrx/signals` ship karta hai, jo local ya feature state ke liye classic Actions/Reducers/Effects pattern ka ek lighter-weight, Signal-native alternative hai, jiska goal NgRx ka boilerplate kam karna hai while good DX (devtools, testability) maintain karte hue:

```typescript
export const CounterStore = signalStore(
  { providedIn: 'root' },
  withState({ count: 0 }),
  withComputed(({ count }) => ({
    doubled: computed(() => count() * 2)
  })),
  withMethods((store) => ({
    increment() { patchState(store, { count: store.count() + 1 }); }
  }))
);
```

**Yeh kyun matter karta hai:** yeh us sawal ka modern jawab hai ki "kya classic NgRx most feature state ke liye overkill nahi hai?" — SignalStore actions/reducers/effects se bahut kam ceremony ke saath structured, testable state management deta hai, aur zarurat padne par cross-cutting/global state ke liye classic NgRx store ke saath bhi interoperate karta hai. 2026 mein interviewers se expect karo ki woh aapse classic NgRx aur SignalStore ko contrast karne ke liye kahein aur yeh articulate karne ke liye kahein ki kab kaunsa appropriate hai — complex async orchestration (retries, cancellation races, sagas-like flows) ke liye classic NgRx effects abhi bhi zyada mature choice hain, jabki SignalStore simpler feature/component-local state ke liye suit karta hai.

### [gaps] NgRx Entity Adapters, Facade Pattern, aur Selector Memoization

Upar wala existing NgRx coverage Store/Actions/Reducers/Effects/Selectors tak isolated concepts ke roop mein hi ruk jaata hai. Teen cheezein jo senior-level review missing flag karta hai: `@ngrx/entity` ke normalized CRUD helpers, components ko NgRx internals se decouple karne ke liye facade pattern, aur *kaise* `createSelector` actually memoization achieve karta hai (sirf yeh nahi ki karta hai).

**`@ngrx/entity` — `EntityAdapter`.** Most real NgRx state ID se keyed records ka collection hota hai (users, orders, products). Isko array (`User[]`) ke roop mein hand-roll karne ka matlab hai har update/delete ek `O(n)` array scan aur manual immutable-copy dance ban jaata hai. `EntityAdapter` collection ko dictionary-shaped state (`{ ids: string[], entities: { [id]: User } }`) mein normalize kar deta hai aur free mein typed CRUD reducer helpers plus selectors generate kar deta hai:

```typescript
import { createEntityAdapter, EntityState } from '@ngrx/entity';
import { createReducer, on } from '@ngrx/store';

interface User { id: string; name: string; email: string; }

// EntityState<User> = { ids: string[]; entities: { [id: string]: User } }
interface UserState extends EntityState<User> {
  loading: boolean;
  selectedUserId: string | null;
}

const adapter = createEntityAdapter<User>({
  selectId: (user) => user.id,          // defaults to `entity.id` if omitted
  sortComparer: (a, b) => a.name.localeCompare(b.name), // optional, keeps `ids` sorted
});

const initialState: UserState = adapter.getInitialState({
  loading: false,
  selectedUserId: null,
});

const userReducer = createReducer(
  initialState,
  on(loadUsersSuccess, (state, { users }) => adapter.setAll(users, state)),
  on(addUser,          (state, { user })  => adapter.addOne(user, state)),
  on(updateUser,       (state, { update }) => adapter.updateOne(update, state)),
  on(deleteUser,       (state, { id })    => adapter.removeOne(id, state)),
  on(upsertManyUsers,  (state, { users }) => adapter.upsertMany(users, state)),
);
```

`adapter.getSelectors()` char canonical selectors generate kar deta hai isliye aapko phir kabhi `Object.values(entities)` hand-write nahi karna padta:

```typescript
const { selectIds, selectEntities, selectAll, selectTotal } = adapter.getSelectors();

export const selectUserIds     = createSelector(selectUserState, selectIds);
export const selectUserEntities = createSelector(selectUserState, selectEntities); // dictionary, O(1) lookup by id
export const selectAllUsers    = createSelector(selectUserState, selectAll);       // User[]
export const selectUserCount   = createSelector(selectUserState, selectTotal);
```

**`EntityAdapter` senior level par kyun matter karta hai:** `selectEntities` `Array.find()` scan ke instead O(1) lookup-by-id (`entities[userId]`) deta hai — jab list kuch sau items se aage badh jaati hai aur frequently lookup hoti hai (e.g., har render par `selectedUserId` ko `User` mein resolve karna) to yeh ek real performance difference banata hai. Yeh update semantics ko bhi standardize karta hai (`updateOne` ek proper immutable merge karta hai) isliye team mein koi bhi feature-wise slightly-different, subtly-buggy array-splicing logic reinvent nahi karta.

**Facade pattern.** Direct `Store` access (`store.select(...)`, `store.dispatch(...)`) ko ek injectable service ke peeche wrap karna, taaki components kabhi bhi directly NgRx symbols import na karein:

```typescript
@Injectable({ providedIn: 'root' })
export class UserFacade {
  private store = inject(Store);

  users$ = this.store.select(selectAllUsers);
  loading$ = this.store.select(selectUserLoading);
  selectedUser$ = this.store.select(selectSelectedUser);

  loadUsers(): void {
    this.store.dispatch(loadUsers());
  }

  selectUser(id: string): void {
    this.store.dispatch(selectUser({ id }));
  }
}
```

```typescript
@Component({ /* ... */ })
export class UserListComponent {
  private facade = inject(UserFacade);
  users$ = this.facade.users$;

  onSelect(id: string) {
    this.facade.selectUser(id);
  }
}
```

**Interviewers iske baare mein kyun poochte hain:** facade pattern larger apps ke liye NgRx ki apni documented recommendation hai. Benefits: (1) components generic `Store` type aur raw action/selector imports ke instead ek small, testable, app-specific API surface par depend karte hain; (2) state-management implementation ko swap karna (e.g., ek feature ko `@ngrx/signals` ke `SignalStore` mein migrate karna) sirf facade ko rewrite karne ki zarurat rakhta hai, har consuming component ko nahi; (3) component unit tests mein facade ko mock karna (`{ provide: UserFacade, useValue: fakeFacade }`) har test ke liye right selector wiring ke saath `MockStore` khada karne se dramatically easier hai. Trade-off jo senior candidates ko acknowledge karna chahiye: har feature ke liye indirection aur boilerplate ka ek extra layer — yeh worth it hota hai jab ek feature ka state ek-do se zyada components consume karte hain, aur single-component-only slice of state ke liye arguably unnecessary hai.

**`createSelector` kaise memoize karta hai.** Yeh selectors ke peeche ka mechanical "why" hai jise zyadatar candidates use kar sakte hain lekin explain nahi kar sakte:

```typescript
export const selectUserState = (state: AppState) => state.users;
export const selectFilterText = (state: AppState) => state.ui.filterText;

export const selectFilteredUsers = createSelector(
  selectAllUsers,
  selectFilterText,
  (users, filterText) => users.filter(u => u.name.includes(filterText)) // "projector" function
);
```

`createSelector` **last set of input arguments** (reference se, `===` use karke) aur **last result** cache karta hai. Har call par yeh har input selector (`selectAllUsers`, `selectFilterText`) ko re-invoke karta hai aur reference equality use karke har returned value ko previous call ki value se compare karta hai:

- Agar **har** input ka result last time se `===` hai, to projector function **re-run nahi** hota — cached result immediately return ho jaata hai.
- Agar **koi bhi** input ka result reference se different hai, to projector ek baar re-run hota hai, aur new result (aur new input references) next time ke liye cache ban jaate hain.

Yahi exact wajah hai ki immutable updates selector performance ke liye bhi waisi hi matter karte hain jaise `OnPush` ke liye karte hain: agar ek reducer `state.users` ko in place mutate kar deta hai naya array/object return karne ke instead, to `selectAllUsers` hamesha *same reference* return karta rahega, isliye `===` "unchanged" bolega even jab underlying data change ho gaya ho (is case mein recompute ko correctly skip karna hi sahi hai kyunki actually kuch update hona hi nahi chahiye tha) — lekin conversely, agar ek reducer har action par ek *naya* array/object banata hai even jab data logically identical ho (e.g., ek reducer jo unconditionally `return { ...state }` karta hai), to selector cache har dispatch par defeat ho jaata hai aur projector needlessly recompute karta hai. `createSelector` ka memoization cache size **1** hota hai — yeh sirf sabse recent call yaad rakhta hai, isliye same selector ko alternating argument sets ke saath call karna (selector factory se banaye gaye parameterized selectors ke saath common) cache ko thrash kar sakta hai aur har baar recompute kar sakta hai; yeh ek real, frequently-tested gotcha hai ("mera selector memoize kyun nahi ho raha?").

```mermaid
flowchart TD
    A[Selector called] --> B{All input selector<br/>results === last time?}
    B -->|Yes| C[Return cached result<br/>— projector NOT called]
    B -->|No| D[Run projector function]
    D --> E[Cache new inputs + result]
    E --> F[Return new result]
```

### [new content] Dependency Injection: Hierarchical Injectors Deep Dive

Source notes DI basics cover karte hain lekin injector tree ko fully unpack nahi karte, jise senior interviews directly probe karte hain (e.g., "agar aap same token ko do levels par provide karo to kya hota hai?").

```mermaid
flowchart TD
    R[Platform Injector] --> Root[Root / ModuleInjector - providedIn: root]
    Root --> Route[Route Injector - providers in a lazy-loaded route config]
    Route --> Comp[Component Injector - providers in @Component]
    Comp --> Child[Child Component Injector - viewProviders]
```

Key rules:
- Angular kisi dependency ko resolve karne ke liye requesting component/directive se injector tree mein **upar** walk karta hai jab tak usse requested token ke liye provider na mil jaaye, phir stop kar deta hai — *nearest* provider hi jeetta hai.
- Ek component ke `providers` array mein registered service us component ke liye fresh instantiate hoti hai (aur uske descendants dwara share hoti hai) — even agar same class kahin aur `providedIn: 'root'` bhi ho; component-level registration us subtree ke liye root wali ko shadow kar deti hai.
- `viewProviders` `providers` se exactly ek tarike se differ karta hai: yeh `<ng-content>` ke through projected content ko invisible hota hai — projected content apni dependencies *apne khud ke* origin ke injector se resolve karta hai, host component ke view injector se nahi. Yeh almost sabko trip up kar deta hai jinhone practice mein isse face nahi kiya hota.
- **Multi-providers** (`{ provide: TOKEN, useClass: X, multi: true }`) — `HTTP_INTERCEPTORS` jaisi cheezon ke liye use hote hain — inse multiple values ek token ke under accumulate ho jaate hain, iske instead ki last registration previous ones ko overwrite kar de.
- **`@Optional()`, `@Self()`, `@SkipSelf()`, `@Host()`** — DI resolution modifiers. Agar koi provider nahi milta to `@Optional()` throw karne ke instead `null` return karta hai; `@Self()` resolution ko sirf requesting injector tak restrict kar deta hai (upar walk nahi hota); `@SkipSelf()` local injector ko skip kar deta hai aur search ek level upar se start karta hai (classic use: ek `ControlContainer` pattern jahan child ko *parent* ka instance chahiye, apna nahi); `@Host()` walk ko current component ki host boundary par stop kar deta hai. Yeh advanced hain lekin kisi bhi senior-level DI mastery claim karne wale ke liye real interview material hain.

### [gaps] Micro-Frontends / Module Federation for Angular

Existing notes mein kahin bhi cover nahi kiya gaya, aur ek increasingly common lead-level system-design question hai jab kisi org mein ek se zyada Angular teams same product surface mein ship kar rahi hoti hain.

**Yeh kya problem solve karta hai:** ek team dwara owned ek single large Angular app organizationally scale nahi karti jab multiple independent teams ko apni-apni product ki areas build aur deploy karni ho (e.g., "Checkout" team, "Account" team, "Search" team) bina ek shared release train coordinate kiye. Micro-frontends se har team UI ka apna piece own, build, test aur **independently deploy** kar sakti hai, jo runtime par ek overall application ("shell") mein compose hota hai.

**Webpack Module Federation** (original mechanism, aur newer tooling ke under bhi abhi tak wahi mechanism jis par zyadatar Angular Module Federation setups build hote hain) ek webpack build (ek "remote") ko runtime par specific modules expose karne deta hai, aur doosri build (the "host"/shell) unko consume karti hai **bina dono ko saath compile kiye** — shell ko build time par remote ke source code ki zarurat nahi hoti, sirf ek runtime manifest URL ki:

```javascript
// remote's webpack.config.js (checkout-app) — exposes a module
new ModuleFederationPlugin({
  name: 'checkoutApp',
  filename: 'remoteEntry.js',
  exposes: {
    './CheckoutModule': './src/app/checkout/checkout.module.ts',
  },
  shared: ['@angular/core', '@angular/common', '@angular/router'],
});

// shell's webpack.config.js — consumes it
new ModuleFederationPlugin({
  name: 'shell',
  remotes: {
    checkoutApp: 'checkoutApp@https://checkout.example.com/remoteEntry.js',
  },
  shared: ['@angular/core', '@angular/common', '@angular/router'],
});
```

Angular ke liye specifically, Angular CLI ke abstractions ke against raw webpack Module Federation config ko hand-roll karna painful hai, isliye ecosystem standard **`@angular-architects/module-federation`** schematic hai, jo isko Angular CLI builder ke upar wire karta hai aur (apni modern form mein) build time par remote ka URL jaane bina bhi dynamic remote loading support karta hai:

```typescript
// shell's routes — lazy-load a remote exposed module, resolved at runtime
{
  path: 'checkout',
  loadChildren: () =>
    loadRemoteModule({
      type: 'module',
      remoteEntry: 'https://checkout.example.com/remoteEntry.js',
      exposedModule: './CheckoutModule',
    }).then(m => m.CheckoutModule),
}
```

```mermaid
flowchart TD
    Shell[Shell App - routing shell, auth, layout] -->|loads at runtime| RemoteA[Remote: Checkout App - own repo, own pipeline]
    Shell -->|loads at runtime| RemoteB[Remote: Account App - own repo, own pipeline]
    Shell -->|loads at runtime| RemoteC[Remote: Search App - own repo, own pipeline]
    RemoteA -.shared deps.-> Shared["@angular/core, @angular/common (singleton, shared)"]
    RemoteB -.shared deps.-> Shared
    RemoteC -.shared deps.-> Shared
```

**Key mechanics jo ek interviewer aapse jaanna expect karta hai:**
- `shared` config wahi hai jo har remote ko apna `@angular/core`/`@angular/common`/`@angular/router` ka full copy ship karne se rokta hai — inhe runtime par host aur remotes ke across singletons ki tarah negotiate kiya jaata hai, jisse duplicate framework instances avoid hote hain (jo otherwise federation boundary ke across DI, routing aur change detection ko break kar dete) aur bundle size bloated hone se bachta hai.
- Har remote ek independently buildable, independently deployable artifact hota hai — ek remote ki team apne khud ke URL par naya version ship kar sakti hai bina shell ko bilkul bhi rebuild ya redeploy kiye, jab tak exposed module ka public contract break nahi hota.
- Version skew hi real operational cost hai: agar shell aur remote kisi shared singleton dependency ke major version par disagree karte hain, to Module Federation runtime par warn/fail karega, shell ke build time par nahi — jo monorepo ke compile-time type error se kahin zyada baad ka, catch karna harder failure point hai.

**Yeh complexity kab worth hoti hai vs. ek monorepo/Nx workspace:** Module Federation apna cost sirf tab earn karta hai jab teams ko **independent deployability** chahiye ho — matlab, team B ko team A par wait kiye ya usse release coordinate kiye bina production mein ship karna aana chahiye. Agar real constraint sirf "kaafi teams, ek codebase, fast builds aur enforced boundaries chahiye" hai — independent runtime deployment nahi — to well-defined library boundaries (`nx.json` tags/lint rules jo enforce karein ki kaunsi libs kis par depend kar sakti hain) wala ek **Nx monorepo** organizational benefit ka zyadatar hissa (clear ownership, enforced boundaries, independently buildable/testable libs) dramatically kam operational complexity ke saath de deta hai: ek build pipeline, ek versioning story, koi runtime dependency-negotiation risk nahi, aur jab shared contract change hota hai to compile-time (runtime nahi) par breakage. Senior/lead framing jo ek interviewer sunna chahta hai: "Module Federation ek *organizational/deployment* problem solve karta hai, code-organization problem nahi — pehle Nx module boundaries ke liye reach karo, aur Module Federation sirf tab add karo jab independent deployment cadence genuinely ek business requirement ho, sirf ek nice-to-have nahi."

| Concern | Nx Monorepo (single deployable) | Module Federation (micro-frontends) |
|---|---|---|
| Team independence | Shared build/release pipeline; lint boundaries ke through enforced | Per team true independent build + deploy |
| Build/version coordination | Repo ke across Angular/shared deps ka single version | Runtime version negotiation; skew ek real risk hai |
| Failure mode for a breaking change | Compile-time (CI fail hota hai) | Often runtime (loaded remote shell ke saath incompatible) |
| Operational complexity | Kam — ek CI/CD pipeline | Zyada — per-remote pipelines, runtime manifest hosting, shared-dependency governance |
| Best fit | Zyadatar orgs, multiple teams ke saath bhi, jo release cadence share kar sakte hain | Large orgs jahan teams ko *must* independent schedules par deploy karna hota hai |

(Is candidate ke background par Note: yeh actual hands-on Angular 4/7 experience ke relative forward-looking material hai — isse honestly conceptual/architectural knowledge ki tarah flag karo, claimed production experience ki tarah nahi, jab tak aapne genuinely isse implement na kiya ho.)

---

## Performance

### Change Detection Deep Dive

Change Detection (CD) woh mechanism hai jo Angular data changes detect karne aur accordingly DOM update karne ke liye use karta hai.

**Yeh internally kaise kaam karta hai (classic Zone.js-based model):**

1. **Zone.js** async browser APIs (`setTimeout`, Promises, DOM events, XHR/fetch, etc.) ko monkey-patch karta hai. Jab bhi inme se koi fire hota hai, Zone.js Angular ko notify karta hai, jo ek CD pass trigger karta hai.
2. **CD cycle** — Angular root component se start hota hai aur component tree ko top-down walk karta hai, har component ke bindings check karta hai, previous vs current values compare karta hai, aur jahan change hua wahan DOM patch karta hai. Isse often **dirty checking** bola jaata hai, halaanki Angular ka Ivy-based check zyada "bound expressions ko diff karne" jaisa hai, deep object comparison nahi.
3. **Default strategy** (`ChangeDetectionStrategy.Default`) har triggering async event par tree ke har component ko check karta hai — simple hai, lekin scale par potentially expensive.

```mermaid
sequenceDiagram
    participant Browser
    participant ZoneJS as Zone.js
    participant Angular
    participant DOM

    Browser->>ZoneJS: async event (click, HTTP response, timer)
    ZoneJS->>Angular: notify "app may be dirty"
    Angular->>Angular: walk component tree top-down
    Angular->>Angular: compare previous vs current bound values
    Angular->>DOM: patch changed bindings only
```

**Change detection ko kya trigger karta hai:** DOM events, HTTP responses, `setTimeout`/`setInterval`, Promise resolution, `@Input` changes, aur manual triggers (`markForCheck()`, `detectChanges()`).

`ChangeDetectorRef` ke through manual control:

```typescript
constructor(private cd: ChangeDetectorRef) {}

this.cd.detectChanges();  // synchronously run CD now
this.cd.markForCheck();   // mark this (and OnPush ancestors) dirty for the next cycle
```

**"Angular change detection explain karo" ka senior-level answer:** "Angular ek change detection mechanism use karta hai jo historically Zone.js se powered hota hai, jo async APIs ko patch karta hai aur jab bhi koi async operation complete hoti hai to Angular ko notify karta hai. Angular fir component tree ko top-down walk karta hai, previous aur current bound values compare karta hai, aur jahan changes milte hain wahan DOM patch karta hai. By default yeh har async event par entire tree check karta hai; `OnPush` ke saath, ek component skip ho jaata hai jab tak uska koi `@Input` reference change na ho, koi event uske andar se originate na hua ho, koi `async`-piped Observable emit na hua ho, ya `markForCheck()`/`detectChanges()` manually call na kiya gaya ho. Isse large component trees mein per cycle hone wala kaam significantly kam ho jaata hai. Change detection unidirectional, top-down hai, aur — Ivy ke baad se — instruction level par highly optimized hai."

### [new content] Zoneless Change Detection (Angular 18+)

Angular 18 ne **experimental zoneless change detection** ship kiya (`provideExperimentalZonelessChangeDetection()`; jo v19/v20 mein aur mature hua), jisse Zone.js dependency poori tarah remove ho gayi.

**Yeh kyun matter karta hai:**
- Zone.js almost har async browser API ko patch karta hai, jiska real runtime cost hota hai aur yeh subtle bugs ka source ban sakta hai (e.g., third-party libraries ka monkey-patched globals ke under weirdly behave karna, ya Angular ke zone ke *bahar* async operations ka silently CD trigger na karna — woh classic "mera UI update kyun nahi hua" bug jab koi non-patched async API ya Web Worker use kiya jaata hai).
- Zone.js ko remove karne se initial bundle/parse cost ka ek non-trivial chunk bhi remove ho jaata hai.
- Zoneless CD yeh precisely jaanne ke liye **Signals** par rely karta hai ki kab re-render karna hai — yeh Signals investment ka direct payoff hai: Signals use karne wale components (aur `async` pipe, aur manual `markForCheck()`) ko explicitly notify kiya ja sakta hai, bina Zone.js ko yeh "guess" karne diye ki kuch async hua hai.

```typescript
// app.config.ts
export const appConfig: ApplicationConfig = {
  providers: [
    provideExperimentalZonelessChangeDetection(),
    // ...
  ]
};
```

**Proactively raise karne wala gotcha:** ek zoneless app mein, woh code jo Signal write ke *bahar* aur Angular ke notification mechanisms ke bahar component state ko mutate karta hai (e.g., ek raw un-tracked callback jo ek plain class field ko mutate karta hai) re-render trigger nahi karega — aapko us state ko Signal ki tarah model karna hoga, `markForCheck()` explicitly use karna hoga, ya use kisi aisi cheez se route karna hoga jise Angular watch kar raha ho (`async` pipe se driven ek Observable zones se independent CD trigger karta rehta hai). Yeh currently evolving area hai — isse mark karo "(specific job ke stack mein Angular version ke against exact stability/GA status verify karo)" kyunki is training ki knowledge mein last widely verified stable release tak yeh abhi bhi experimental marked tha.

### OnPush Strategy aur Pitfalls

```typescript
@Component({
  selector: 'app-demo',
  changeDetection: ChangeDetectionStrategy.OnPush
})
export class DemoComponent { }
```

`OnPush` ke saath, Angular component ko **sirf** tab check karta hai jab:
1. Koi `@Input()` **reference** change hoti hai.
2. Koi event component ke andar se originate hota hai (e.g., uske apne template mein bound koi click handler).
3. `async` pipe ke through bound koi Observable emit karta hai.
4. Aap manually `markForCheck()` ya `detectChanges()` call karte ho.

| | Default | OnPush |
|---|---|---|
| Entire component tree check karta hai? | Haan | Nahi |
| Performance | Scale par slower | Faster |
| Triggered by | Kahin bhi, koi bhi change | `@Input` reference change, own event, async-piped emission, ya manual trigger |

**#1 OnPush gotcha (source se, correctly called out):** OnPush `@Input` bindings ko **reference** se compare karta hai, deep equality se nahi.

```typescript
this.user.name = 'John';                      // ❌ mutates in place — reference unchanged, OnPush component NOT re-checked
this.user = { ...this.user, name: 'John' };    // ✅ new object reference — triggers re-check
```

Isi wajah se OnPush naturally **immutable data patterns** (spread operators, NgRx reducers se naye arrays/objects return karna, Signals) ke saath pair hota hai. 500+ components ke saath complex/heavy DOM mein, Default strategy visibly performance degrade kar sakti hai; large enterprise apps mein OnPush standard hai.

**[new content] OnPush + Signals interaction:** jab koi component apne template mein directly ek Signal read karta hai (`{{ mySignal() }}`), Angular automatically jaan jaata hai ki `OnPush` ke under bhi us binding ko re-check karna hai, bina kisi `@Input` reference change ya manual `markForCheck()` ki zarurat ke — Signal ki apni change notification hi OnPush ki requirements ko natively satisfy kar deti hai. Isse Signals `OnPush` ke liye plain class fields se kahin zyada ergonomic complement ban jaate hain, aur yeh ek achha example hai ki Signals aur existing CD strategies ko compete karne ke instead interlock karne ke liye design kiya gaya tha.

### trackBy / track in Loops

`trackBy` ke bina, Angular ki default identity tracking object reference se hoti hai; agar aap ek array ko naye reference se replace karte ho (even mostly-unchanged items ke saath), to Angular un DOM nodes ko destroy aur recreate kar sakta hai jinki zarurat nahi thi.

```html
<li *ngFor="let item of items; trackBy: trackByFn">{{ item.name }}</li>
```
```typescript
trackByFn(index: number, item: any) {
  return item.id;
}
```

Example: ek array ke ek item ke ek field ko update karna (`items = [...this.items]` jisme `item[1].name` change hua) — `trackBy` ke bina, Angular dono `<li>` elements ko tear down aur rebuild kar sakta hai; `id` par keyed `trackBy` ke saath, yeh sirf changed item ke binding ko patch karta hai, unchanged items ke DOM nodes ko untouched chhod deta hai. Net effect: kam DOM churn, better performance, especially large/frequently-updated lists ke liye.

Naye `@for` control-flow syntax mein, `track` **mandatory** hai, optional nahi — `@for (item of items; track item.id) { ... }` — jo developer discipline par rely karne ke instead is optimization ko by default force karta hai.

**[new content] Virtual scrolling (CDK):** genuinely large lists (hundreds/thousands rows) ke liye, sirf `trackBy` kaafi nahi hai — `@angular/cdk/scrolling` se `cdk-virtual-scroll-viewport` use karo, jo total list size chahe kuch bhi ho, sirf currently viewport mein visible DOM nodes (plus ek small buffer) render karta hai:

```html
<cdk-virtual-scroll-viewport itemSize="50" class="viewport">
  <div *cdkVirtualFor="let item of items">{{ item.name }}</div>
</cdk-virtual-scroll-viewport>
```

Jab interviewer "aap `*ngFor` ko kaise optimize karte ho" se aage push karke "agar 50,000 rows hon to" pooche, tab yehi correct answer hai — `trackBy` *update* cost kam karta hai, virtual scrolling off-screen DOM ko bilkul bhi materialize na karke *render* cost kam karta hai.

### [new content] Deferrable Views: @defer (Angular 17+)

`@defer` **template regions** (sirf routes/modules nahi) ke liye ek built-in, declarative lazy-loading mechanism hai — yeh deferred content (aur uske component/directive/pipe dependencies) ko ek separate JS chunk mein split karta hai jo sirf tab load hota hai jab koi trigger condition fire hoti hai.

```html
@defer (on viewport) {
  <heavy-chart [data]="chartData" />
} @placeholder {
  <div class="skeleton"></div>
} @loading (minimum 500ms) {
  <spinner />
} @error {
  <p>Failed to load chart.</p>
}
```

Common triggers: `on idle` (default — jab browser idle ho tab load hota hai), `on viewport` (jab block scroll karke view mein aata hai, `IntersectionObserver` use karke, tab load hota hai), `on interaction` (specified trigger element par click/keydown par load hota hai), `on hover`, `on timer(2s)`, aur `when someCondition` (ek custom boolean expression/Signal).

**Senior interview ke liye yeh kyun matter karta hai:** yeh directly Core Web Vitals ko target karta hai — heavy, below-the-fold, ya rarely-interacted-with UI (charts, modals, comment sections, admin-only widgets) ko ab initial bundle mein ship hone ki zarurat nahi hai sirf isliye ki woh critical content ke saath same component template mein declared hai. `@defer` se pehle, isse achieve karne ke liye manually ek separate lazy route mein split karna padta tha ya `ViewContainerRef.createComponent()` ke saath ek dynamically-imported component banana padta tha — same effect ke liye kaafi zyada ceremony. `@defer` ab woh pehli cheez hai jise reach karna chahiye jab poocha jaaye "route-level lazy loading se aage initial bundle size kaise reduce karoge."

### Lazy Loading aur Preloading

Lazy loading ek feature module (ya, modern Angular mein, ek route-level standalone component) ki loading ko us waqt tak defer karta hai jab tak uska route visit nahi hota, jisse initial bundle shrink ho jaata hai:

```typescript
const routes: Routes = [
  { path: 'dashboard', loadChildren: () => import('./dashboard/dashboard.module').then(m => m.DashboardModule) }
];
```

**Preloading** initial app stable hone *ke baad* background mein quietly lazy modules load karke "lazy route par first navigation slow feel hota hai" wali problem solve karta hai, isliye jab tak user click karta hai, module already cached hota hai:

```typescript
@NgModule({
  imports: [
    RouterModule.forRoot(routes, { preloadingStrategy: PreloadAllModules })
  ],
  exports: [RouterModule]
})
export class AppRoutingModule {}
```

`PreloadAllModules` Angular ki built-in strategy hai; aap sab kuch preload karne ke instead sirf specific routes ko selectively preload karne ke liye ek **custom `PreloadingStrategy`** bhi likh sakte ho (e.g., user role ya "likely next page" heuristic ke basis par) — agar poocha jaaye "kya sab kuch preload karna hamesha achha idea hai?" to yeh mention karne ke liye ek good follow-up hai (answer: nahi — constrained/mobile networks par, sab kuch preload karna critical initial resources ke saath compete kar sakta hai; usage analytics ke basis par selective preloading zyada sophisticated answer hai).

### Bundle Size, Tree Shaking, Build Optimization

- **Tree shaking** — build process (esbuild/webpack) final bundle se unused components, services aur dead code ko remove kar deta hai.
- **Bundle size analyze karna:**
  ```
  ng build --stats-json
  npx webpack-bundle-analyzer dist/stats.json
  ```
  **[new content note]** naye esbuild-based builder ke saath, `--stats-json` output format aur analyzer tooling legacy webpack builder se thoda different ho sakta hai; apne project ki `angular.json` `builder` setting ke against exact flag verify karo.
- **Differential loading** — historically, Angular CLI do bundles generate karta tha: ek modern browsers (ES2015+) ke liye aur ek older browsers (ES5) ke liye, jo automatically `<script type="module">` / `nomodule` ke through serve hote the. **[new content — currency note]** recent Angular versions mein, modern browser support essentially universal hone aur Angular ke apne minimum supported ECMAScript target aage badh jaane ki wajah se, legacy ES5 differential bundles CLI ke default output se effectively obsolete/removed ho gaye hain — agar current interview mein "Angular 7" build pipeline discuss ho raha ho to isse outdated ki tarah call out karo (exact CLI version verify karo jahan ES5 output drop hua tha).
- **Angular DevTools** — component trees aur change-detection cycles inspect karne ke liye browser extension, aur profile karta hai ki har cycle mein kaunse components check/re-render hote hain.
- `angular.json` mein **Budgets** — jab bundle sizes thresholds exceed karte hain to warnings/errors configure karo (e.g., agar `main` bundle X KB exceed kare to build fail karo), CI mein enforced.

### Angular CLI vs Webpack

Angular mein naye developers ke liye ek frequent confusion point: **Angular CLI** aur **Webpack** different layers par operate karte hain aur really competitors nahi hain.

| Aspect | Angular CLI | Webpack |
|---|---|---|
| Purpose | Entire Angular projects ko scaffold, build, serve aur test karta hai (`ng new`, `ng generate`, `ng build`, `ng serve`, `ng test`) | Ek general-purpose JavaScript module bundler |
| Configuration | Minimal, convention-based (`angular.json`) | Highly customizable, lekin verbose (`webpack.config.js`) |
| Relationship | Historically apne bundler ki tarah Webpack ko **internally** use karta tha | CLI (aur kaafi doosre frameworks) dwara ek building block ki tarah consume kiya jaata hai, uska replacement nahi |

Interviewers jo practical answer chahte hain: Angular CLI woh higher-level tool hai jisse developers day to day interact karte hain; Webpack, saalon tak, uske underneath chalne wala bundler tha, CLI ke builder abstraction (`@angular-devkit/build-angular:browser`) ke peeche hidden — isliye zyadatar Angular developers ko kabhi bhi `webpack.config.js` hand-write nahi karna pada (unlike, say, Create-React-App-era React projects, jinme directly Webpack customize karne ke liye often `eject` karna padta tha). Jaisa aage cover hoga, CLI ne tab se apna default bundler esbuild/Vite mein swap kar diya hai — CLI khud conceptually change nahi hua, sirf yeh change hua ki woh underneath kya delegate karta hai.

### [new content] esbuild / Vite-based Application Builder (Angular 17+)

Angular ka CLI ek webpack-based build pipeline se naye **esbuild + Vite** based builder (`@angular-devkit/build-angular:application`, jo Angular 17 se new projects ke liye default hai) mein move ho gaya hai, jisne older `browser` builder ko replace kar diya.

**Yeh kyun matter karta hai:**
- Dramatically faster cold builds aur rebuilds — esbuild Go mein likha gaya hai aur aggressively parallelize karta hai; dev-server rebuilds jo webpack ke under seconds lete the, near-instant ho sakte hain.
- `ng serve` ab dev server ki tarah **Vite** use karta hai, jo zyadatar changes ke liye near-instant HMR (hot module replacement) deta hai.
- Naya builder pehle separate "browser" aur "server" (SSR) build targets ko ek single `application` builder configuration mein unify kar deta hai, jisse un apps ke liye `angular.json` simplify ho jaata hai jinhe CSR aur SSR dono output chahiye.
- Existing webpack-specific custom configurations (custom loaders, kuch third-party webpack plugins) ka koi direct esbuild equivalent nahi ho sakta — agar ek large legacy app ko upgrade karne ki discussion ho rahi hai to yeh mention karne layak ek real migration friction point hai.

**Interview framing:** "Angular ne v17 se apna default webpack-based builder ko esbuild/Vite-based ek se replace kar diya, primarily build speed ke liye — yeh analogous hai us baat se ki kaise kaafi JS ecosystems same reason ke liye webpack se esbuild/Rollup/Vite ki taraf move hue. Yeh isse change nahi karta ki aap components kaise likhte ho, lekin yeh troubleshooting (different error output, different plugin ecosystem) aur `angular.json` builder configuration change kar deta hai."

### Angular Build & Runtime Lifecycle

**Build-time (`ng build` par kya hota hai):**

```mermaid
flowchart LR
    A[TypeScript source + templates] --> B[TS Compilation]
    B --> C[AOT Compilation: templates -> JS render instructions]
    C --> D[Tree Shaking: remove unused code]
    D --> E[Bundling & Minification]
    E --> F[dist/ output]
    F --> G[Deploy to CDN / static host]
```

1. **TypeScript compilation** — Angular compiler (`ngc`/Ivy) `.ts` → `.js` convert karta hai, aur templates + decorators ko JS instructions mein convert karta hai, build time par type errors aur (AOT ke saath) template binding errors check karte hue.
2. **AOT (Ahead-of-Time) compilation** — templates build ke dauraan JS render functions mein compile hote hain, browser mein nahi; DI metadata pre-generated hota hai. Benefits: faster startup, smaller bundles, kam runtime errors. **AOT default hai aur production builds ke liye effectively mandatory hai** (JIT — templates ko runtime par in-browser compile karna — legacy hai, mainly kuch dynamic-template edge cases aur older dev workflows ke liye relevant).
3. **Tree shaking** — unused components/services/dead code ko remove karta hai.
4. **Bundling & minification** — `main.js`, `polyfills.js`, aur (historically) `runtime.js`/`styles.css` produce karta hai, minified aur compressed, `dist/` mein output.
5. **Deploy** — static files IIS/Nginx/S3+CloudFront/kisi bhi static host se serve hoti hain; browser sirf static assets download karta hai.

**Runtime (jab user app open karta hai to kya hota hai):**

1. `index.html` load hota hai, jo `runtime.js` (webpack/module loading logic — esbuild builder ke under largely superseded/reshaped) ko reference karta hai, `main.js` (app ko bootstrap karta hai), aur `polyfills.js` (older-browser support shims).
2. `main.ts` root module/component ko bootstrap karta hai:
   ```typescript
   platformBrowserDynamic().bootstrapModule(AppModule);
   // or, standalone:
   bootstrapApplication(AppComponent, appConfig);
   ```
3. **DI setup** — Angular injector tree banata hai: root services, providers, interceptors.
4. **Root component created** — Angular `<app-root>` ko locate karta hai, `AppComponent` ko instantiate karta hai, aur lifecycle begin hota hai: `constructor → ngOnInit → ngAfterViewInit`.
5. **Router feature routes load karta hai** — lazy `loadChildren`/`loadComponent` entries sirf tab fetch hoti hain jab unka route activate hota hai.

JIT vs AOT:

| Feature | JIT (Just-In-Time) | AOT (Ahead-of-Time) |
|---|---|---|
| When compiled | Runtime par, browser mein | Deployment se pehle, build time par |
| Performance | Dheema | Tez |
| File size | Bada | Chota |
| Use case | Development iteration (Ivy ki incremental compilation se partially superseded) | Production ke liye hamesha use hota hai |

### HTTP Request Lifecycle

```mermaid
flowchart TD
    A[Component] --> B[Service]
    B --> C[HttpClient creates cold Observable]
    C --> D[Interceptor pipeline - forward order]
    D --> E[Browser Fetch/XHR]
    E --> F[Server]
    F --> G[Interceptor pipeline - reverse order]
    G --> H[subscribe / async pipe receives data]
    H --> I[Change Detection runs]
    I --> J[DOM Update]
```

1. Component ek service method call karta hai (`this.userService.getUsers()`).
2. `HttpClient.get()` ek **cold** Observable return karta hai — abhi tak koi network call nahi hui hoti.
3. Interceptor pipeline outgoing request par registration order mein chalta hai (auth headers, logging, loader-start).
4. Actual browser networking API (under the hood Fetch ya XHR) request send karta hai.
5. Server respond karta hai.
6. Interceptors response par phir se chalte hain, **reverse** order mein (error handling, logging, loader-stop).
7. Observable emit karta hai — `.subscribe()` (ya `async` pipe) component ko data deliver karta hai.
8. Change detection chalta hai, bindings diff karta hai aur zarurat ke mutabik DOM patch karta hai.

---

## SSR, PWA & Cross-Cutting

### Angular Universal / SSR

Angular Universal server-side rendering enable karta hai: app browser ko ship karne se pehle server (Node.js) par ek initial HTML snapshot render karta hai, jisse perceived load time improve hota hai aur crawlers/SEO fully rendered content dekh paate hain.

Setup (source mein reference kiya gaya legacy schematic): `ng add @nguniversal/express-engine`. **[new content — currency note]**: Angular 17 se, SSR setup directly core CLI mein integrate ho gaya hai — `ng add @angular/ssr` (ya `ng new` chalate waqt simply SSR select karna) — separate `@nguniversal` packages `@angular/ssr` / `@angular/platform-server` mein fold ho gaye hain. Agar aaj SSR setup karne ke liye poocha jaaye to modern command mention karo; `@nguniversal/express-engine` package naam legacy/deprecated hai.

Benefits: improved SEO, faster initial (perceived) page load, slow networks/devices par better performance, kyunki browser full JS bundle parse aur bootstrap hone se pehle hi meaningful content dikha deta hai.

### [new content] Hydration (Angular 16+) aur Event Replay (Angular 17+)

Classic Angular Universal SSR mein ek significant, often-glossed-over problem thi: server-rendered HTML load hone ke baad, Angular ka client-side bootstrap apni component tree aur event listeners attach karne ke liye poore DOM ko scratch se **destroy aur completely re-render** kar deta tha ("destructive rehydration") — jisse ek visible flicker aur wasted work hoti thi, kyunki server ne already correct markup produce kar diya hota tha.

**Non-destructive hydration** (`provideClientHydration()`, Angular 16/17 se stable) isse fix karta hai: Angular existing server-rendered DOM nodes ko tear down karne ke instead reuse karta hai, apna internal state aur event listeners already-painted markup par attach karta hai.

```typescript
// app.config.ts
export const appConfig: ApplicationConfig = {
  providers: [
    provideClientHydration(),
  ]
};
```

**Event replay** (Angular 17+, Chrome team ki event dispatch library par built) un user interactions (clicks, etc.) ko capture karta hai jo "HTML painted" aur "Angular fully bootstrapped and hydrated" ke beech ki window *ke dauraan* hote hain, aur hydration complete hone par unhe replay karta hai — isliye jo user page appear hote hi instant button click kar deta hai, uska click silently swallow nahi hota.

**Yeh ek strong senior talking point kyun hai:** hydration correctness exactly waisa hi nuanced, "yeh kyun matter karta hai" topic hai jo us insaan ko differentiate karta hai jisne production mein SSR ship kiya hai us insaan se jisne sirf docs padhe hain — flicker-free hydration aur event replay Angular team ke liye genuinely hard, recent engineering problems the, koi incremental sugar nahi.

**[version 19 upgrade]** Angular 7 mein SSR (Angular Universal) ka behavior binary tha — poori app ya to server par render hoti thi ya nahi, aur poori app ek hi baar mein hydrate hoti thi. Angular 19 isse do directions mein granular banata hai:

**Incremental Hydration (experimental)** — familiar `@defer` syntax use karte hue, template ke individual sections ko mark kar sakte ho taaki woh server par "grayscale"/inert state mein render ho (JS download nahi hota) aur sirf tab hydrate ho jab ek specific trigger fire ho — poori app ko ek saath hydrate karne ke bajaye:
```typescript
// app.config.ts
export const appConfig: ApplicationConfig = {
  providers: [
    provideClientHydration(withIncrementalHydration()),
  ]
};
```
```html
<!-- below-the-fold widget stays inert until it scrolls into view, then hydrates -->
@defer (hydrate on viewport) {
  <product-recommendations />
} @placeholder {
  <div class="skeleton"></div>
}
```
Supported triggers: `on viewport`, `on interaction`, `on idle`, `on immediate`, aur `never` (static content jo kabhi hydrate nahi hona chahiye, e.g. ek blog post ka body).

**Event Replay — Angular 19 se default enabled**: v17 mein introduce hua tha, ab explicit `provideClientHydration()` config ke bina bhi by default on hai.

**Route-level render mode** — ab per-route decide kar sakte ho ki woh SSR, CSR, ya prerendered ho, same app ke andar (e.g. marketing pages prerender ho jaayein, dashboard purely CSR rahe):
```typescript
// app.routes.server.ts
export const serverRouteConfig: ServerRoute[] = [
  { path: 'dashboard', renderMode: RenderMode.Client },
  { path: '**', renderMode: RenderMode.Prerender },
];
```
- **Interview angle:** "How would you optimize SSR for a page with one heavy, below-the-fold widget?" — incremental hydration is the direct answer (vs. v7-era "hydrate everything or nothing").

### Progressive Web Apps aur Service Workers

Ek **PWA** ek web app hai jisme native-app-like capabilities hoti hain: offline support, background sync, push notifications, fast/cacheable performance, installability.

```
ng add @angular/pwa
```
Yeh ek service worker registration, ek Web App Manifest, aur icons scaffold karta hai.

Ek **Service Worker** ek background script hai jo:
- Offline use ke liye resources cache karta hai.
- Configured caching strategies ke mutabik network requests ko intercept/handle karta hai.
- Push notifications enable karta hai.

Caching strategy example (`ngsw-config.json`):

```json
{
  "assetGroups": [
    {
      "name": "app",
      "installMode": "prefetch",
      "updateMode": "prefetch",
      "resources": {
        "files": ["/favicon.ico", "/index.html"],
        "urls": ["/api/**"]
      }
    }
  ]
}
```

PWA check karna: Chrome DevTools → Lighthouse audit.

Background sync — user actions ko offline store karta hai aur connectivity wapas aate hi unhe send kar deta hai. Push notifications — client ke liye server-initiated real-time updates, `@angular/service-worker` ke through enabled.

Service worker updates handle karna:

```typescript
constructor(updates: SwUpdate) {
  updates.available.subscribe(() => {
    if (confirm('New version available. Load new version?')) {
      window.location.reload();
    }
  });
}
```

**[new content] `SwUpdate` API modernization:** upar dikhaye gaye boolean-events-based `updates.available`/`updates.activated` observables legacy API shape hain; current versions `updates.versionUpdates` expose karte hain (ek discriminated-union event type ka Observable: `VersionReadyEvent`, `VersionInstallationFailedEvent`, etc.), jo *kya* hua uske baare mein zyada precise hai. Agar aapko current Angular version ke against yeh likhne ke liye kaha jaaye, to `versionUpdates` ko `VersionReadyEvent` ke liye filter karna prefer karo — target project ke Angular version ke against exact API verify karo kyunki yeh releases ke across evolve hua hai.

### Angular Security

Built-in security features: XSS protection, CSRF prevention support, CSP support, HTML/URLs/styles ki sanitization.

**XSS (Cross-Site Scripting)** — kisi page mein malicious scripts inject karna. Angular interpolation aur zyadatar property bindings ke through bound values ko auto-sanitize kar deta hai:

```html
<p>{{ userInput }}</p>              <!-- safe: auto-escaped -->
<p [innerHTML]="userInput"></p>     <!-- dangerous: bypasses much of the auto-escaping context -->
```

Explicit trust decisions ke liye `DomSanitizer`:

```typescript
constructor(private sanitizer: DomSanitizer) { }
safeUrl = this.sanitizer.bypassSecurityTrustUrl(userInput);
```

**Volunteer karne layak interview point:** `bypassSecurityTrustX` methods ko "main personally vouch kar raha hoon ki yeh input safe hai" ki tarah treat karna chahiye — inhe sirf us content par call karo jisse aap control karte ho ya jo independently server-side validate/sanitize kiya gaya ho (e.g., ek known-good CMS-rendered HTML field), kabhi directly raw user input par nahi, warna aapne wahi XSS hole reopen kar diya jisse Angular aapko protect kar raha tha.

**CSP (Content Security Policy)** — restrict karta hai ki page kaunse scripts/resources load/execute kar sakta hai, aur even agar koi injection slip ho jaaye to bhi XSS impact ko mitigate karta hai:

```html
<meta http-equiv="Content-Security-Policy" content="default-src 'self'">
```

**CSRF** — Angular ke `HttpClient` mein `HttpXsrfTokenExtractor`/`withXsrfConfiguration` ke through "double-submit cookie" pattern ke liye built-in support hai (client server dwara set ki gayi cookie read karta hai aur state-changing requests par usse header ki tarah echo karta hai) — lekin yeh out of the box sirf tab kaam karta hai jab *backend* us cookie/header pair ko set aur validate karne ke liye configured ho; yeh automatic security nahi hai, yeh ek cooperating client-server contract hai.

**CORS** — server-side concern hai (`Access-Control-Allow-Origin` headers); Angular ka HttpClient sirf request banata hai, browser enforcement (same-origin policy) hi woh cheez hai jise CORS headers relax karte hain.

**HTTP Parameter Pollution** — same parameter ko multiple baar supply karna (`?user=admin&user=guest`) taaki inconsistent server-side parsing logic ko confuse karne ki koshish ki ja sake — primarily ek backend validation concern hai, lekin ek interview vocabulary term ki tarah jaanna worth hai.

Secure authentication practices: JWTs, **HttpOnly cookies** mein stored tokens (`localStorage` mein nahi, jo kisi bhi injected script se readable hota hai — ek direct XSS mitigation), aur routes ko protect karne wale Auth Guards.

```typescript
@Injectable({ providedIn: 'root' })
export class AuthGuard implements CanActivate {
  constructor(private authService: AuthService) { }
  canActivate(): boolean {
    return this.authService.isLoggedIn();
  }
}
```

**Token storage par Note:** HttpClient section ka `AuthInterceptor` example directly `localStorage` read karne ke instead ek `TokenService` inject karta hai, precisely isliye ki *token kahan rehta hai* ek separate, explicit decision bana rahe. Yeh dono postures equivalent nahi hain: `localStorage`/`sessionStorage` simplest aur extremely common hai, lekin kisi bhi injected script se readable hota hai, isliye koi bhi XSS token theft ban jaata hai — sirf low-stakes apps ke liye defensible. BFF + HttpOnly-cookie pattern token ko browser JavaScript se poori tarah bahar rakhta hai (interceptor bearer header ke instead `withCredentials: true` send karta hai), jiski cost ek backend investment hai jiske badle mein ek bahut chota blast radius milta hai; enterprise scale par yehi defensible answer hai. Ek senior candidate ko trade-off name karna chahiye aur us specific app ke liye ek posture commit karni chahiye, `localStorage` version ko best practice ki tarah present karne ke instead.

Brute-force mitigations: API par rate limiting, login par CAPTCHA, account lockout — yeh bhi primarily backend-enforced hote hain, lekin Angular se expect kiya jaata hai ki woh resulting UX (lockout messages, CAPTCHA widgets) ko correctly surface kare.

### [gaps] Accessibility (a11y): ARIA, CDK a11y Module, Focus Management

Existing notes mein poori tarah se absent hai, aur ek genuinely common senior/lead-level topic hai — accessibility enterprise aur government-adjacent projects par frequently ek hard compliance requirement (WCAG 2.1/2.2 AA, Section 508) hoti hai, koi optional nicety nahi, aur interviewers isse use karte hain "ek working UI ship karna" ko "ek aisa UI ship karna jise sach mein sab use kar sakein" se separate karne ke liye.

**Custom components par ARIA attributes.** Native HTML elements (`<button>`, `<input>`, `<select>`) free mein built-in accessibility semantics ke saath aate hain — screen readers ko already pata hota hai ki `<button>` clickable aur focusable hota hai. `<div>`s/`<span>`s se bane custom Angular components (ek custom dropdown, ek custom tab strip, ek custom modal) mein by default **isme se kuch bhi nahi** hota aur component ke `host` metadata ya template bindings ke through bound ARIA (`role`, `aria-*`) attributes ke through explicitly declare karna zaroori hota hai:

```typescript
@Component({
  selector: 'app-custom-tabs',
  template: `
    <div role="tablist" [attr.aria-label]="ariaLabel">
      @for (tab of tabs; track tab.id) {
        <button
          role="tab"
          [id]="'tab-' + tab.id"
          [attr.aria-selected]="tab.id === activeTabId"
          [attr.aria-controls]="'panel-' + tab.id"
          [tabindex]="tab.id === activeTabId ? 0 : -1"
          (click)="selectTab(tab.id)"
          (keydown.arrowRight)="focusNextTab()"
          (keydown.arrowLeft)="focusPreviousTab()">
          {{ tab.label }}
        </button>
      }
    </div>
    @for (tab of tabs; track tab.id) {
      <div
        role="tabpanel"
        [id]="'panel-' + tab.id"
        [attr.aria-labelledby]="'tab-' + tab.id"
        [hidden]="tab.id !== activeTabId">
        <ng-container *ngTemplateOutlet="tab.content"></ng-container>
      </div>
    }
  `
})
export class CustomTabsComponent { /* ... */ }
```

Key points jo interviewers sunna chahte hain: `role="tablist"`/`role="tab"`/`role="tabpanel"` assistive tech ko batata hai ki widget *kya hai* kyunki markup generic `<div>`/`<button>` hai; `aria-selected` us state ko communicate karta hai jo visually obvious hoti hai (ek highlighted tab) lekin otherwise screen reader ke liye invisible hoti hai; roving `tabindex` (active tab par `0`, baaki par `-1`, arrow-key handlers ke saath combined) ek composite widget ko har individual tab button ke through tab karne ke instead ek single `Tab` stop ke saath keyboard-navigable banane ka standard pattern hai.

**Angular CDK ka `a11y` module** (`@angular/cdk/a11y`) pre-built primitives provide karta hai taaki teams har component ke liye yeh logic hand-roll na karein:

| Utility | Purpose |
|---|---|
| `FocusTrap` / `cdkTrapFocus` | `Tab`/`Shift+Tab` keyboard focus ko ek container ke andar confine karta hai — modals/dialogs ke liye essential hai taaki focus silently open modal ke peeche wale page content mein escape na kar sake |
| `LiveAnnouncer` | Ek ARIA live region ke through screen-reader users ko programmatically ek message announce karta hai, un state changes ke liye jinhe otherwise focus nahi milta (e.g., "3 results found," "Item added to cart," async validation errors) |
| `FocusMonitor` | Detect karta hai ki koi element *kaise* focused hua (mouse, keyboard, touch, ya programmatically) — modern UX conventions ke mutabik sirf keyboard users ke liye focus-visible outlines dikhana enable karta hai, mouse clicks ke liye nahi |
| `InteractivityChecker` | Determine karta hai ki koi element apni current state (disabled, hidden, `tabindex`) ko dekhte hue genuinely focusable/tabbable hai ya nahi, `FocusTrap`/`FocusMonitor` dwara internally use hota hai |
| `ListKeyManager` / `ActiveDescendantKeyManager` | List-like composite widgets (menus, autocomplete option lists) ke liye arrow-key navigation aur active-item tracking manage karta hai |

```typescript
import { LiveAnnouncer } from '@angular/cdk/a11y';

@Component({ /* ... */ })
export class SearchResultsComponent {
  private liveAnnouncer = inject(LiveAnnouncer);

  onResultsLoaded(count: number) {
    this.liveAnnouncer.announce(`${count} results found`, 'polite');
  }
}
```

**Modals/dynamic content ke liye focus management — woh pattern jise ek interviewer aapse end-to-end describe karne ke liye kahega:**

```typescript
import { FocusTrap, FocusTrapFactory } from '@angular/cdk/a11y';

@Component({
  selector: 'app-modal',
  template: `
    <div #modalContainer role="dialog" aria-modal="true" [attr.aria-labelledby]="titleId">
      <h2 [id]="titleId">{{ title }}</h2>
      <ng-content></ng-content>
      <button (click)="close()">Close</button>
    </div>
  `
})
export class ModalComponent implements AfterViewInit, OnDestroy {
  @ViewChild('modalContainer') containerRef!: ElementRef;
  private focusTrapFactory = inject(FocusTrapFactory);
  private focusTrap!: FocusTrap;
  private previouslyFocusedElement: HTMLElement | null = null;

  ngAfterViewInit() {
    // 1. Remember what had focus before the modal opened, to restore it later.
    this.previouslyFocusedElement = document.activeElement as HTMLElement;

    // 2. Trap Tab/Shift+Tab focus inside the modal.
    this.focusTrap = this.focusTrapFactory.create(this.containerRef.nativeElement);
    this.focusTrap.focusInitialElementWhenReady();
  }

  close() {
    this.onClose.emit();
  }

  ngOnDestroy() {
    this.focusTrap?.destroy();
    // 3. Restore focus to whatever triggered the modal, so keyboard users
    //    aren't dropped back at the top of the page (a very common a11y bug).
    this.previouslyFocusedElement?.focus();
  }
}
```

Woh teen steps jo ek senior candidate ko bina pooche narrate karne chahiye: **(1)** focus move karne se pehle triggering element ka focus capture karo, **(2)** jab dynamic content open ho tab uske andar focus trap karo (`cdk-trap-focus` directive ya programmatically `FocusTrapFactory`) aur initial focus usme move karo (usually dialog heading ya first actionable control), **(3)** close hone par, explicitly focus ko trigger element par restore karo — zyadatar home-grown modals steps 1 aur 3 mein galti karte hain, silently keyboard/screen-reader users ka focus context drop kar dete hain, jo sabse common real-world a11y bugs mein se ek hai aur ek frequent audit finding hai.

**Interview framing:** "Custom Angular widgets par accessibility automatic nahi hai — generic elements se bana kuch bhi cheez explicit ARIA roles/states, keyboard interaction handling, aur deliberate focus management maangta hai. CDK ka `a11y` module (`FocusTrap`, `LiveAnnouncer`, `FocusMonitor`) specifically isliye exist karta hai taaki teams ko har component ke liye yeh genuinely tricky, easy-to-get-subtly-wrong behaviors hand-roll na karna pade." Agar poocha jaaye ki aap isse kaise verify karoge, to baseline ki tarah automated tooling (axe-core / `@angular-eslint` a11y lint rules / Lighthouse accessibility audit) mention karo, lekin honestly batao ki automated tools shaayad real WCAG issues ka ek tihaai hi catch karte hain — actual keyboard-only aur screen-reader (NVDA/VoiceOver) manual testing hi baaki ko catch karta hai.

### Deployment: Firebase aur GitHub Pages

Neeche [Enterprise Architecture Case Study](#enterprise-architecture-case-study-bff--cqrs--net) mein cover kiye gaye full enterprise S3+CloudFront/ECS pipeline ke liye reach karne se pehle, demos, side projects aur take-home assignments ke liye do sabse lightest-weight, sabse common deployment targets jaanna worth hai — yeh interviews mein ek quick "aaj isse kaise ship karoge" follow-up ki tarah aate hain.

**Firebase Hosting par Deploy karna:**

```bash
npm install -g firebase-tools   # 1. install the Firebase CLI
firebase login                  # 2. authenticate with a Google account
firebase init                   # 3. select Hosting, point it at dist/<project-name>
firebase deploy                 # 4. build first (ng build), then push to Firebase's CDN
```

Firebase Hosting almost koi configuration ke bina free HTTPS, ek global CDN, aur easy custom-domain support deta hai — cloud infrastructure khada kiye bina quick production-like deployments ke liye ek common choice.

**GitHub Pages par Deploy karna:**

```bash
ng add angular-cli-ghpages
ng deploy --base-href=/repo-name/
```

`angular-cli-ghpages` ek CLI builder hai jo `ng deploy` ko app build karne aur `dist/` output ko automatically `gh-pages` branch mein push karne ke liye wire kar deta hai. `--base-href` flag yahan specifically matter karta hai: GitHub Pages ek project site ko ek subpath (`https://username.github.io/repo-name/`) se serve karta hai, domain root se nahi, isliye Angular router aur asset URLs ko build time par woh base path baked-in chahiye hota hai — `--base-href` bhool jaana sabse common reason hai jiski wajah se ek GitHub Pages Angular deployment broken asset/route links ke saath blank page dikhata hai.

**Senior framing:** Firebase Hosting aur GitHub Pages static SPA hosting, demos, aur portfolios ke liye appropriate hain — yeh un scenarios ke liye fit nahi hote jinhe custom Node server ke saath server-side rendering, backend proxying, ya enterprise compliance/observability requirements chahiye, jo exactly wahi jagah hai jahan neeche wala BFF/CloudFront architecture instead right answer ban jaata hai.

---

## Enterprise Architecture Case Study (BFF + CQRS + .NET)

Aapke notes mein ek full enterprise reference architecture included hai jo ek senior .NET-full-stack interview ke liye highly relevant hai, kyunki yeh Angular ko directly ek realistic .NET backend se connect karta hai. Neeche preserved aur lightly organized hai — yeh exactly wahi kism ka system-design answer hai jo ek lead-level interview mein aapse whiteboard par sketch karne ke liye kaha jaayega.

```mermaid
flowchart TD
    U[User Browser] -->|HTTPS| CF[CloudFront CDN]
    CF -->|Static Angular SPA| U
    U -->|OAuth2/PKCE redirect| Okta[Okta Identity Provider]
    Okta -->|auth code| BFF[.NET Core BFF]
    U -->|API calls, cookie session| GW[API Gateway]
    GW --> BFF
    BFF -->|CQRS orchestration| MS1[Microservice A - own DB]
    BFF --> MS2[Microservice B - own DB]
    BFF --> MS3[Microservice C - own DB]
```

**Angular frontend responsibilities:** sirf UI rendering — koi business logic nahi, koi direct microservice access nahi. **Authorization Code Flow with PKCE** use karta hai, login ke liye Okta par redirect karta hai; **kabhi access tokens store nahi karta** (na `localStorage` mein, na `sessionStorage` mein); sirf BFF ke saath communicate karta hai. Interview point: Angular ko stateless aur token-free rakhna frontend security surface area ko radically simplify kar deta hai.

**CloudFront (CDN):** S3 se built Angular static files serve karta hai, edge locations par cache karta hai, `/api/*` ko API Gateway par route karta hai, HTTPS enforce karta hai, AWS Shield + WAF se fronted hai. Latency reduce karta hai aur backend ko traffic spikes se protect karta hai.

**Okta (SSO/IdP):** central authentication/authorization — login, MFA, session management. Flow: Angular Okta par redirect karta hai → user authenticate hota hai → Okta ek authorization code return karta hai → **BFF** (Angular nahi) us code ko tokens ke liye exchange karta hai. Token types: ID token (identity), access token (API authorization), refresh token (sirf BFF ke server-side par kept). Enterprise-readiness aur identity concerns ko custom app code se externalize karne ke liye chosen.

**API Gateway:** single public backend entry point; Okta-issued JWTs validate karta hai, rate-limits karta hai, requests route karta hai, AWS WAF ke saath integrate karta hai, aur internal microservices tak kisi bhi direct client access ko prevent karta hai.

**Backend-for-Frontend (BFF):** ek backend layer jo specifically is frontend ki needs serve karne ke liye built hai — Okta ke saath token exchange, session management, API aggregation/response shaping (over-/under-fetching avoid karna), aur CQRS orchestration handle karta hai. Stack: ASP.NET Core, `HttpClientFactory`/YARP, cookie-based auth. **BFF kyun:** Angular app exactly ek backend ko call karta hai; tokens server-side rehte hain; frontend complexity substantially drop ho jaati hai.

**CQRS (Command Query Responsibility Segregation):** commands (create/update/delete) validated, transactional hote hain, aur write database ko hit karte hain; queries read-only, optimized hoti hain, aur read models/projections se serve ho sakti hain. Benefit: reads vs writes ki independent scaling, clearer intent, easier targeted optimization. **Interview one-liner:** "CQRS write consistency ko affect kiye bina read scalability allow karta hai."

**.NET Core microservices:** ek service = ek responsibility = ek database; independently deployable; typically Clean Architecture/DDD concepts ke saath built, write side par ek repository pattern aur query side par dedicated read models. Communication: internal calls ke liye synchronous HTTP, decoupled workflows ke liye SNS/SQS ke through asynchronous events.

**Security model:** zero trust, least privilege, defense in depth. Angular ke paas koi secrets nahi hote; BFF tokens ko securely store karta hai; API Gateway JWTs validate karta hai; microservices ek VPC ke andar privately baithe rehte hain, kabhi directly internet-reachable nahi. AWS mechanics: IAM roles, Secrets Manager, private subnets.

**CI/CD:** Frontend — Angular build karo, S3 par upload karo, CloudFront cache invalidate karo. Backend — .NET Core services build karo, Docker images build karo, ECR par push karo, health checks ke saath blue/green ya rolling deployments use karke ECS/EKS ke through deploy karo.

**Observability:** correlation IDs ke saath structured logs, centralized logging; latency/error-rate/throughput monitoring; Angular UI → BFF → microservices tak end-to-end distributed tracing.

**Is architecture par common interview Q&A (notes se answered):**
- *BFF kyun?* Security (tokens kabhi browser tak nahi pahunchte), API aggregation, frontend/backend decoupling.
- *CQRS kyun?* Independent read/write scaling, performance optimization, clearer responsibility separation.
- *Tokens kaise secure hote hain?* Browser ko kabhi expose nahi hote; BFF mein server-side store hote hain.
- *APIs ko version kaise karte ho?* Versioning BFF layer par handled hoti hai, jo frontend ko internal microservice API churn se insulate karta hai.
- *Agar ek downstream service fail ho jaaye to?* Circuit breakers, backoff ke saath retries, graceful degradation (hard failure ke bajaye partial UI rendering).

**Yeh architecture kab *nahi* use karna chahiye:** small applications, MVPs, low-traffic systems — yeh stack enterprise scale aur security posture ke liye optimized hai, simplicity ke liye nahi, aur notes explicitly (aur correctly) yeh baat call out karte hain. Ek senior/lead candidate ko BFF+CQRS+microservices ko universal default ki tarah present karne ke bajaye proactively yeh trade-off explicit karna chahiye — ek small app ko is tarah se over-engineer karna khud ek red flag hai jo interviewers sunte hain.

**Interview summary statement (source se, jaisa hai waisa strong):** "Angular CloudFront ke peeche ek pure UI ki tarah act karta hai, authentication Okta use karke centralized hai, API Gateway ingress secure karta hai, BFF tokens aur frontend orchestration manage karta hai, aur CQRS-based .NET Core microservices strong security boundaries ke saath independently scale karte hain."

---

## Testing

Testing ke types: **unit** (individual components/services), **integration** (units ke beech interactions), **end-to-end/E2E** (full application flows, e.g., Cypress ke through).

**Jasmine** — Angular ke liye default JS testing framework: `describe`/`it`/`expect`, assertions, spies, mocking provide karta hai; pure logic tests ke liye ek real browser rendering pipeline ke bina run hota hai.

```typescript
describe('Calculator', () => {
  it('should add numbers', () => {
    expect(1 + 1).toBe(2);
  });
});
```

**Karma** — ek test runner jo ek real browser launch karta hai (CI mein typically Chrome/Firefox headless) aur uske andar Jasmine tests execute karta hai, files ko watch karta hai aur change par re-run karta hai; `karma.conf.js` ke through configured hota hai. **[new content — currency note]:** newer Angular CLI versions (17+) unit testing ke liye Karma/Jasmine ke bajaye increasingly naye projects ko default **Jest** ya **Web Test Runner** par karte hain, aur Karma khud broader JS ecosystem mein ek maintenance-mode/deprecated project hai. Agar aap ek recent Angular version use karne wale role ke liye interview de rahe ho, to yeh ask/clarify karo ki codebase actually kaunsa test runner use karta hai — jis tarah aap ek "Angular 7" app ke liye karte, us tarah default Karma assume na karo. Jasmine ka `describe`/`it`/`expect` syntax khud often Jest ke under bhi use hota hai, kyunki Jest ka API intentionally Jasmine-compatible hai.

`TestBed` — ek test module environment configure aur initialize karne ke liye Angular ka utility:

```typescript
beforeEach(() => {
  TestBed.configureTestingModule({ declarations: [MyComponent] }).compileComponents();
});
```

Ek service ko test karna:

```typescript
let service: MyService;
beforeEach(() => {
  TestBed.configureTestingModule({ providers: [MyService] });
  service = TestBed.inject(MyService);
});

it('should return expected data', () => {
  expect(service.getData()).toEqual(expectedData);
});
```

`ComponentFixture` ke through ek component ko test karna:

```typescript
let fixture: ComponentFixture<MyComponent>;
beforeEach(() => {
  fixture = TestBed.createComponent(MyComponent);
});

it('should render title', () => {
  const compiled = fixture.nativeElement;
  expect(compiled.querySelector('h1').textContent).toContain('Welcome');
});
```

Dependencies ko mock karna:

```typescript
spyOn(myService, 'getData').and.returnValue(of(mockData));
```

`fakeAsync`/`tick` ke saath async code ko synchronously test karna:

```typescript
it('resolves after timeout', fakeAsync(() => {
  let value = false;
  setTimeout(() => value = true, 1000);
  tick(1000);
  expect(value).toBe(true);
}));
```

**E2E testing** — **Protractor** Angular ka original official E2E framework tha lekin ab **deprecated hai aur naye Angular CLI projects se remove ho gaya hai**; ecosystem **Cypress** aur **WebdriverIO** par move ho gaya hai (aur **Playwright** bhi widely adopted hai, halaanki original notes mein call out nahi kiya gaya — **[new content]**: Playwright ab 2025-2026 mein Angular E2E ke liye Cypress ke saath ek bahut common choice hai, aur agar poocha jaaye "aaj E2E ke liye kya use karoge" to naam lene layak hai kyunki Protractor fully gone hai).

**[new content] Angular Testing Library ke saath component testing:** raw `TestBed`+`ComponentFixture` ke ek alternative/complement ki tarah mention karne layak — `@testing-library/angular` components ko us tarah test karne ko encourage karta hai jis tarah ek user unke saath interact karta hai (internal implementation details/CSS selectors ke bajaye visible text/role se query karna), jo aisi tests produce karta hai jo refactors ko better survive karti hain. Required knowledge nahi hai, lekin agar aaye to current testing-philosophy awareness signal karta hai.

---

## Best Practices

- Naye code ke liye **standalone components** prefer karo; NgModule-based code ko ek disruptive rewrite mein na karke incrementally migrate karo.
- Trivial components se aage kisi bhi cheez ke liye default **`OnPush`** change detection use karo, immutable data patterns ke saath paired (ya Signals, jo OnPush ko natively satisfy karte hain).
- Saari non-trivial list rendering ke liye **`trackBy`** (ya `@for` mein mandatory `track`) use karo.
- Components mein manual `.subscribe()` ke bajaye **`async` pipe** (ya `toSignal` ke through Signals) prefer karo — yeh subscription lifecycle aur change-detection integration aapke liye handle karta hai, leak bugs ki ek whole class eliminate karta hai.
- **Components ko thin rakho** — business logic, data access, aur orchestration ko services mein push karo; components ko mostly template ↔ service wire karna chahiye.
- Simplest form se aage kisi bhi cheez ke liye **Reactive Forms** (ideally **typed**) use karo; template-driven forms ko genuinely small, simple cases ke liye reserve karo.
- Feature areas ko route ke hisaab se **lazy load** karo, aur heavy in-page content ke liye jise initial bundle mein ship karne ki zaroorat nahi hai **`@defer`** use karo.
- Cross-cutting HTTP concerns (auth headers, error handling, loading indicators) ko repeated per-call code mein nahi, **interceptors** mein centralize karo.
- Deliberately unsubscribe karo — pehle `async` pipe, phir `takeUntilDestroyed()`, aur jo cases pehle do cover nahi karte unke fallback ke liye `ngOnDestroy()` mein manual `Subscription.unsubscribe()`.
- `angular.json` mein **bundle budgets** enforce karo aur blind optimize karne ke bajaye Angular DevTools + Lighthouse se regularly profile karo.
- `bypassSecurityTrustX` sanitizer calls, user content par `[innerHTML]`, aur `localStorage`-stored tokens ko un cheezon ki tarah treat karo jinhe ek explicit, justified security decision chahiye — defaults nahi.
- State-management tooling (plain service/Signal, NgRx Signals, classic NgRx) ko actual app complexity ke proportionate choose karo — sirf NgRx ka naam drop na karo, choice ko justify karne ke liye ready raho.

## Common Pitfalls

- `OnPush` ke under **objects/arrays ko in place mutate karna** — silently change detection break karta hai kyunki input reference kabhi change nahi hota.
- **Impure pipes ko overuse karna** (ya filtering/sorting logic ko bilkul pipes ki tarah likhna) — poori app mein har CD cycle par recompute hota hai.
- Long-lived components/services mein manually-subscribed Observables se **unsubscribe karna bhool jaana** — classic memory leak source; SPAs mein compounded hota hai jahan components frequently create/destroy hote hain bina full page reloads ke jo memory ko "reset" kar sakein.
- **`route.snapshot.paramMap` use karna jab same component instance ek param-only navigation ke across reuse hota hai** — snapshot stale ho jaata hai; iske bajaye observable form use karo.
- `ngOnInit` ke bajaye **constructor mein business logic** — inputs/DI context ready hone ki guarantee nahi hoti, aur yeh testability ko undermine karta hai.
- `Renderer2` use karne ke bajaye **`ElementRef.nativeElement` styles/attributes ko directly mutate karna** — SSR aur platform-agnostic rendering ke under break ho jaata hai.
- **Ek source Observable par `forkJoin` jo kabhi complete nahi hota** — silently forever hang ho jaata hai, koi emission nahi, koi error nahi.
- Large ya frequently-updated lists par **`trackBy` ke bina `*ngFor` ko "fine" assume karna** — unnecessary DOM churn aur janky UI leads karta hai.
- Ek low-stakes app se aage kisi bhi cheez ke liye **JWTs ko `localStorage` mein store karna** — XSS-exposed; more defensible BFF/HttpOnly-cookie pattern se contrast karo (dekho [Angular Security](#angular-security) mein flagged contradiction).
- **NgRx ko default ki tarah treat karna** un apps/teams ke liye jinhe actually iski guarantees chahiye, ek deliberate choice ke bajaye — simpler apps ke liye real complexity cost add karta hai.
- **Assume karna ki Signals RxJS ko replace kar dete hain** — yeh different problems solve karte hain (synchronous view state vs asynchronous streams); interop (`toSignal`/`toObservable`) explain karne ki expectation rakho, ek wholesale migration nahi.
- Old `*ngFor` templates jinme kabhi `trackBy` nahi tha unhe port karte waqt **yeh bhool jaana ki naye `@for` syntax mein `track` mandatory hai** — migration schematic isko flag karega, lekin manual conversions silently isko omit kar sakte hain (identity-based tracking par fall back karte hue, exact wahi problem reintroduce karte hue jise `@for` ka matlab tha aapko avoid karne ke liye force karna).
- **`inject()` ko ek injection context ke bahar call karna** (e.g., ek unrelated `setTimeout`/`Promise.then` callback ke andar) — runtime par throw karta hai; dependencies ko beforehand capture karo ya `runInInjectionContext()` use karo.

## Version Feature Comparison Table

| Feature | Angular 7 (2018, source notes ka baseline) | Current Angular (2025-2026 class) |
|---|---|---|
| Component definition | Sirf NgModule-declared | Default mein Standalone; NgModules still supported |
| Bootstrap | `platformBrowserDynamic().bootstrapModule(AppModule)` | `bootstrapApplication(AppComponent, appConfig)` |
| Template control flow | `*ngIf` / `*ngFor` / `*ngSwitch` | `@if` / `@for` / `@switch` (naya syntax), old syntax still valid |
| Reactive state | Sirf RxJS (`BehaviorSubject`, services) | Signals (`signal`/`computed`/`effect`) + RxJS, interop ke saath |
| Change detection | Zone.js-based Default/OnPush | Zone.js Default/OnPush **plus** experimental zoneless (Signal-driven) |
| Dependency injection | Constructor injection | Constructor injection **aur** `inject()` function |
| Forms | Untyped `FormGroup`/`FormControl` | Strictly typed reactive forms |
| Ek template ke andar lazy content | Available nahi (sirf route/module level) | Template-region-level lazy loading ke liye `@defer` |
| Build tooling | Angular CLI ke through Webpack | esbuild + Vite-based `application` builder (v17 se default) |
| SSR | Destructive rehydration ke saath Angular Universal (`@nguniversal`) | `@angular/ssr`, non-destructive hydration + event replay |
| E2E testing | Protractor (tab-current) | Protractor removed; Cypress/Playwright/WebdriverIO |
| Rendering engine | View Engine (v9 mein Ivy shipped hua) | Ivy |
| HTTP interceptors | Class-based `HttpInterceptor` + `HTTP_INTERCEPTORS` multi-provider | `withInterceptors()` ke through functional interceptors (class-based still supported) |
| Route guards | Class-based `CanActivate` etc. | Functional guards (`CanActivateFn`, etc.); class-based still supported |

## Sample Interview Q&A

**Q: Walk me through what happens, end-to-end, when a user clicks a button that triggers an HTTP call, under `OnPush`.**
A: Click ek native DOM event hai; kyunki yeh `OnPush` component ke apne template ke andar se originate hota hai, Angular guaranteed hai ki strategy chahe kuch bhi ho, us component ke liye change detection run karega. Click handler ek service method call karta hai, jo `HttpClient.get()` call karta hai — yeh ek cold Observable return karta hai, isliye abhi kuch nahi hota. `.subscribe()` par (ya `async` pipe ke through), request interceptor chain (e.g., auth header injection) ke through forward pass hota hai, Fetch/XHR ke through bahar jaata hai, aur response same interceptors se reverse mein wapas aata hai (error handling, logging). Observable emit karta hai, subscriber callback (ya `async` pipe) result assign karta hai — critically, agar wo assignment ek object/array reference ko replace karta hai (mutate nahi), `OnPush` component correctly naya `@Input` reference pick up karta hai (agar data child tak flow karta hai) ya already checked ho raha hota hai kyunki initiating event local tha. Change detection phir bindings diff karta hai aur sirf changed DOM nodes patch karta hai.

**Q: Why would you choose Signals over a `BehaviorSubject` for a piece of component state, and when would you still reach for RxJS instead?**
A: Simple, synchronous, always-has-a-value UI state ke liye (e.g., ek toggle, ek selected tab, ek computed total), Signals simpler hain — koi subscription management nahi, aur templates unhe directly automatic, fine-grained change detection integration ke saath read karte hain (`OnPush`/zoneless ke under bhi). Main still RxJS ke liye reach karunga jab state inherently asynchronous ho ya operator composition chahiye ho — debounced search input, retryable HTTP calls, `combineLatest`/`forkJoin` ke saath multiple async sources combine karna, ya WebSocket streams — inme se koi bhi Signals natively model nahi karte. Practice mein, main dono ko `toSignal()`/`toObservable()` se bridge karta hoon, ek ko exclusively pick karne ke bajaye.

**Q: A colleague says "we should always use `ChangeDetectionStrategy.OnPush` everywhere." Do you agree?**
A: Directionally haan kisi bhi non-trivial cheez ke liye, lekin ek caveat ke saath: `OnPush` immutability ke around discipline demand karta hai — agar team naye references create karne ke bajaye objects/arrays ko in place mutate karti hai, `OnPush` components silently update hona band kar dete hain, jo us performance problem se worse bug hai jise solve karne ka matlab tha. Main ek blanket `OnPush` policy ko immutable update patterns enforce karne wale lint rules ya code review discipline ke saath pair karunga (ya teams ko Signals ki taraf push karunga, jo reference-equality gotcha ko entirely sidestep kar dete hain kyunki Signal reads individually tracked hote hain).

**Q: What's the actual difference between `providers` and `viewProviders` on a component, and can you give a real scenario where it matters?**
A: Dono ek service ko component tak scoped register karte hain (us subtree ke liye ek naya instance), lekin `providers` component ki apni view *aur* `<ng-content>` ke through usme projected kisi bhi content ko visible hota hai, jabki `viewProviders` sirf component ke apne template ko visible hota hai. Yeh matter karta hai, for example, ek reusable `TabsComponent` banate waqt jo apne template mein apne child `TabComponent`s ke beech internal tab state coordinate karne ke liye ek `TabsService` provide karta hai — agar ek consumer `ng-content` ke through ek tab mein arbitrary content project karta hai aur wo content tab ke service ki expectation rakhte hue same token inject karne ki koshish karta hai, `viewProviders` ise unse hide kar dega (by design), jabki `providers` ise expose kar dega. Isko galat karna confusing "mera projected content is service ko kyun nahi dekh sakta" bugs cause karta hai.

**Q: In the enterprise architecture with Okta + API Gateway + BFF + CQRS microservices, why does the Angular app never hold tokens?**
A: Kyunki koi bhi token jo browser JavaScript tak pahunchta hai XSS ko exposed hota hai — agar koi bhi dependency ya injected script `localStorage`/`sessionStorage` read kar sakta hai, yeh token exfiltrate kar sakta hai. Token exchange aur storage ko .NET BFF mein entirely server-side rakhkar (iske bajaye browser-BFF session ke liye HttpOnly, secure cookies use karke), Angular app mein ek successful XSS injection bhi ek bearer token steal nahi kar sakta, kyunki kahin bhi koi hai hi nahi jise browser read kar sake. Yeh ek deliberate trade-off hai — meaningfully reduced token-theft blast radius ke exchange mein added backend complexity (BFF) — enterprise-scale apps ke liye appropriate hai, aur probably ek low-stakes internal tool ke liye overkill hai.

**Q: You inherited an Angular 7 codebase using NgModules everywhere. How would you approach modernizing it without a risky big-bang rewrite?**
A: Incrementally: pehle `ng update` use karke Angular ko khud version-by-version update karo (har major version ke apne migration schematics hote hain — versions skip karna unsupported aur risky hai). Ek baar standalone components support karne wale version (14+) par ho, `ng generate @angular/core:standalone` schematics use karo components/directives/pipes ko ek time par ek feature area standalone mein convert karne ke liye, kyunki standalone aur NgModule-based code coexist kar sakte hain. Parallel mein, control-flow migration schematic ke through opportunistically `@if`/`@for` adopt karo, jahan genuinely simpler ho wahan naya state Signals mein move karo, aur jab touch ho tab `HttpInterceptor` classes ko functional interceptors se swap karo. Main poore build system (webpack → esbuild) aur poore component model ko same change mein flip karne ki koshish nahi karunga — agar kuch regress ho jaaye to us combination ko bisect karna bahut hard hai.

---

## Summary of Additions

Neeche wale **[new content]** sections "Angular 7" (2018) baseline aur current (2025-2026) senior-level Angular interview expectations ke beech gap close karne ke liye add kiye gaye:

1. **Standalone Components (Angular 14+, v17 se default)** — source notes ke baad se biggest structural shift; interviewers assume karte hain ki aap NgModules ke bina ek app bootstrap aur structure kar sakte ho.
2. **inject() Function (Angular 14+)** — functional guards/interceptors/resolvers ke liye required hai aur ab standalone apps mein idiomatic DI style hai; ek common gotcha question bhi hai (injection context rules).
3. **New Control-Flow Syntax: @if / @for / @switch (Angular 17+)** — naye code mein `*ngIf`/`*ngFor`/`*ngSwitch` ko replace karta hai; `@for` mein mandatory `track` optional `trackBy` se ek direct, testable improvement hai.
4. **Typed Reactive Forms (Angular 14+)** — ek real type-safety gap close karta hai (typo'd control names silently fail hote hain) jo compile-time safety par emphasis karne wale background ke liye highly relevant hai.
5. **Functional Interceptors (Angular 15+)** — modern `provideHttpClient(withInterceptors(...))` pattern, jo `HTTP_INTERCEPTORS`/`multi:true` boilerplate ko replace karta hai.
6. **Angular Signals (Angular 16+)** — arguably abhi single most-asked "Angular mein naya kya hai" question; ek naya reactive primitive jise Angular ka runtime khud samajhta hai.
7. **RxJS vs Signals — Kab Kaunsa Use Karein** — directly us trade-off question ka answer deta hai jo har interviewer Signals aane ke baad poochta hai.
8. **SignalStore / NgRx Signals (Angular 17+)** — classic NgRx ceremony ka lighter-weight, Signal-native alternative; interviewers expect karte hain ki aap dono ko contrast karo.
9. **Dependency Injection: Hierarchical Injectors Deep Dive** — `@Optional`/`@Self`/`@SkipSelf`/`@Host` aur multi-providers ko unpack karta hai, jinhe original notes ne sirf shallowly touch kiya tha.
10. **Zoneless Change Detection (Angular 18+)** — Zone.js ko remove karta hai, Signals se enabled; ek forward-looking lekin real topic hai (evolving/verify exact stability ki tarah flagged).
11. **Deferrable Views: @defer (Angular 17+)** — route-level code splitting se aage declarative template-region lazy loading; directly Core Web Vitals target karta hai.
12. **esbuild / Vite-based Application Builder (Angular 17+)** — CLI ka build pipeline materially change hua; troubleshooting aur `angular.json` configuration ko affect karta hai.
13. **Hydration (Angular 16+) and Event Replay (Angular 17+)** — Angular Universal SSR mein old "destructive rehydration" flicker/wasted-work problem fix karta hai; agar achhe se discuss ho to production SSR experience ka strong evidence hai.

**[version 19 upgrade] additions (this pass)** — in tagged inline call-outs mein specifically Angular 19-only features cover hui hain jo pehle kahin nahi thi, har ek exactly us jagah insert ki gayi jahan original v7 content unhe supersede karta hai:

14. **Signal-based `input()` / `output()` / `model()` — stable in v19** (Component Communication section mein) — `@Input()`/`@Output()` + `EventEmitter` decorators ko replace karta hai; `model()` two-way binding ke liye `@Input`+`@Output` pair ko ek single declaration mein collapse karta hai. Official migration schematics included.
15. **`linkedSignal()` aur `resource()` (Angular 19)** (Signals section mein) — derived/self-resetting state (`linkedSignal`) aur async data-fetching (`resource`, experimental) ke liye v7-era manual patterns (constructor recompute, hand-rolled RxJS loading/error state) ka Signals-native replacement.
16. **Incremental Hydration, default Event Replay, aur Route-level Render Mode (Angular 19)** (SSR/Hydration section mein) — poori-app-ek-saath hydration model ko `@defer`-based per-section hydration se replace karta hai; per-route SSR/CSR/prerender mix karne deta hai jo v7 ke binary SSR-on-or-off model mein possible nahi tha.

**Inline flagged contradictions (aapke review ke liye):**
- **Token storage — RESOLVED.** `AuthInterceptor` example pehle directly `localStorage.getItem('token')` read karta tha, jo elsewhere aur BFF architecture section mein "tokens kabhi localStorage/sessionStorage mein store nahi hote" guidance ko contradict karta tha. Example ab ek `TokenService` inject karta hai, isliye storage posture ek implied default ke bajaye ek explicit decision hai, aur `localStorage`-vs-BFF trade-off dono jagah state kiya gaya hai (dekho [Angular Security](#angular-security)).
- **`ng build --prod` vs `--configuration production`:** source notes different places mein `ng build --prod` (older CLI syntax) aur `ng build --configuration production` (current syntax) dono use karte hain. `--prod` current CLI versions mein deprecated/removed hai — going forward `--configuration production` (ya shorthand `-c production`) use karo; aapke notes ne dono use kiye the isliye context mein dono rakhe gaye, lekin `--prod` ko outdated flag kiya ja raha hai.
- Koi aur substantive factual contradictions nahi mile — source mein zyadatar apparent duplication (e.g., do `HttpInterceptor` examples, do `ViewChild` explanations, repeated lifecycle-hook lists) straightforward repetition thi, jise conflicting flag karne ke bajaye de-duplicate kiya gaya.

## Summary of [gaps] Additions (This Pass)

Is pass ne ek formal gap-analysis review se identified teen sections add kiye, jo earlier **[new content]** pass se distinguish karne ke liye **[gaps]** tagged hain:

1. **NgRx Entity Adapters, Facade Pattern, aur Selector Memoization** (State Management mein, SignalStore/NgRx Signals ke baad inserted) — original NgRx coverage surface-level Store/Actions/Reducers/Effects/Selectors building blocks par hi ruk gaya tha. Yeh teen real gaps close karta hai: `@ngrx/entity` ka `EntityAdapter` (normalized CRUD state + generated `selectAll`/`selectEntities` selectors, jo scale par O(1) lookups vs. array scans ke liye matter karta hai), facade pattern (components ko Store internals se decoupled rakhne ka NgRx-recommended tareeka, jo component unit tests ko bhi dramatically simpler banata hai), aur `createSelector` memoization ke peeche ki actual reference-equality mechanics — jisme ek single-slot cache ka real-world gotcha bhi included hai jo parameterized selectors se thrashed ho jaata hai ya unnecessary naye references banane wale reducers se defeated ho jaata hai.
2. **Micro-Frontends / Module Federation for Angular** (Advanced mein, DI Hierarchical Injectors deep dive ke baad inserted) — ek lead-level system-design topic jo pehle entirely absent tha. Webpack Module Federation mechanics, `@angular-architects/module-federation` schematic jo Angular teams actually use karti hain, shared-singleton dependency negotiation risk, aur — jo framing interviewers actually chahte hain — kab yeh complexity justified hai (teams ke across independent deployment cadence) versus kab enforced library boundaries wala ek Nx monorepo same organizational problem ko kaafi zyada cheaply solve kar deta hai, covers karta hai.
3. **Accessibility (a11y): ARIA, CDK a11y Module, Focus Management** (SSR, PWA & Cross-Cutting mein, Angular Security ke baad inserted) — kaafi enterprise projects par ek hard compliance requirement hone ke bawajood a11y ki prior guide mein zero coverage thi. Custom widgets par ARIA roles/states/roving-tabindex, Angular CDK ke `FocusTrap`/`LiveAnnouncer`/`FocusMonitor` primitives, aur modals/dynamic content ke liye full three-step focus-management sequence (trigger focus capture karo → focus trap aur move in karo → close par focus restore karo) jise zyadatar home-grown implementations galat karte hain, covers karta hai.

Teeno ko is candidate ke hands-on Angular 4/7 experience ke relative forward-looking/conceptual mark kiya gaya hai jahan relevant ho (particularly Module Federation), version-sensitive content ke liye guide ke baaki hisse mein use ki gayi honest framing ke consistent.

## Quick "Most Frequently Asked" Cheat List

Agar time kam hai, yeh woh concepts hain jo almost har senior/lead Angular interview mein show up karte hain — har ek ke saath ek short explanation aur is guide mein wo detail kahan milega:

1. **Ivy vs View Engine** (v9 mein default, v13 mein View Engine removal) — Ivy Angular ka rendering/compilation pipeline hai jo View Engine ko replace karta hai: smaller bundles (better tree-shaking), faster incremental compilation, browser devtools mein directly inspectable components, aur better template type-checking. v9 mein default bana, aur v13 tak View Engine ko codebase se poori tarah hata diya gaya — ab sirf Ivy exist karta hai. **Interview mein poochne ka common tareeka:** "Ivy kya solve karta hai jo View Engine nahi karta tha?" — answer: bundle size, compile speed, aur debuggability, kyunki Ivy components ko locally compile karta hai (View Engine puri app ko globally compile karta tha). Dekho [Architecture Overview](#architecture-overview) aur [Version Feature Comparison Table](#version-feature-comparison-table).

2. **Standalone Components** (v14 preview → v15 stable → v17/v19 default) — components/directives/pipes ko `NgModule` ke bina declare karne ki ability, `imports: [...]` directly component decorator par. Yeh is puri guide ka single biggest structural shift hai: v7 mein har cheez `NgModule`-declared hoti thi, aur ab poori app `bootstrapApplication()` se bina ek bhi `NgModule` likhe bootstrap ho sakti hai. **Interview mein poochne ka common tareeka:** "Standalone components NgModules ka replacement hain ya coexist karte hain?" — answer: dono coexist kar sakte hain (incremental migration ke liye zaroori), lekin v17 se naye CLI-generated apps default standalone hote hain. Dekho [[new content] Standalone Components](#new-content-standalone-components-angular-14-default-since-v17).

3. **Signals** (`signal`/`computed`/`effect`, v16 preview → stable, plus `linkedSignal`/`resource` v19 mein) — ek fine-grained reactive primitive jo Angular ke runtime ko exactly batata hai ki kaunsi value change hui, jisse surgical DOM updates aur zoneless change detection possible hote hain. `linkedSignal()` self-resetting derived state ke liye hai, `resource()` async data-fetching ko Signals ke andar wrap karta hai. **Interview mein poochne ka common tareeka:** "Signals RxJS ko replace karte hain?" — answer: nahi, Signals synchronous view state ke liye hain, RxJS asynchronous streams ke liye; dono `toSignal()`/`toObservable()` se interop karte hain. Dekho [[new content] Angular Signals](#new-content-angular-signals-angular-16) aur [[new content] RxJS vs Signals](#new-content-rxjs-vs-signals--kab-kaunsa-use-karein).

4. **New control-flow syntax `@if`/`@for`/`@switch`** (v17) vs. old structural directives — `*ngIf`/`*ngFor`/`*ngSwitch` ko replace karta hai; naya syntax Ivy ke through directly compile hota hai (`<ng-template>` desugaring ki zaroorat nahi), aur `@for` mein `track` expression **mandatory** hai (jabki purane `*ngFor` mein `trackBy` optional tha). Benchmarks mein up to ~90% faster loop rendering. **Interview mein poochne ka common tareeka:** "`@for` mein `track` kyun mandatory kiya gaya?" — answer: kyunki `trackBy` ko optional chhodna ek common performance pitfall tha (large lists par unnecessary DOM churn); mandatory karke Angular team ne developers ko sahi pattern follow karne ke liye force kiya. Dekho [[new content] New Control-Flow Syntax](#new-content-new-control-flow-syntax-if--for--switch-angular-17) aur [trackBy / track in Loops](#trackby--track-in-loops).

5. **Zoneless change detection** (v18, experimental) aur Signals se iska relation — Zone.js dependency ko poori tarah remove karta hai; change detection ab async APIs ko monkey-patch karke trigger hone ke bajaye Signals/explicit triggers se driven hota hai. Yeh sirf tab safely kaam karta hai jab state changes Signals ke through track ho rahe hon — agar koi code Signal ke bahar state mutate karta hai, zoneless CD us change ko miss kar sakta hai. **Interview mein poochne ka common tareeka:** "Zoneless CD adopt karne se pehle kya check karoge?" — answer: ki saara reactive state Signals (ya `markForCheck`/`OnPush`-compatible patterns) use kar raha ho, kyunki Zone.js ka "automatically detect har async operation" safety net ab nahi hai. Dekho [[new content] Zoneless Change Detection](#new-content-zoneless-change-detection-angular-18).

6. **Typed Reactive Forms** (v14) aur jo type-safety gap yeh close karta hai — v7 mein `FormGroup`/`FormControl` untyped the (`form.value` `any` type ka hota tha), isliye `form.get('emial')` (typo) silently `null` return karta tha compile error ke bajaye. v14 se `FormGroup`/`FormControl` generics ke saath properly typed hain, isliye typo'd control names ab compile-time par fail hote hain. **Interview mein poochne ka common tareeka:** "Typed forms se pehle kya problem thi aur kaise fix hui?" — answer: runtime `null`/`undefined` bugs jo silently through nikal jaate the, ab compiler catch karta hai. Dekho [[new content] Typed Reactive Forms](#new-content-typed-reactive-forms-angular-14).

7. **`@defer` / deferrable views** (v17) aur **incremental hydration** (v19) — `@defer` template-region-level lazy loading enable karta hai (route-level se aage), triggers jaise `on viewport`/`on interaction`/`on idle` ke saath. Incremental hydration isi `@defer` syntax ko SSR ke saath combine karta hai — server-rendered sections tab tak inert/"grayscale" rehte hain jab tak unka trigger fire na ho, taaki poori app ek saath hydrate hone ke bajaye sirf zaroori parts pehle interactive hon. **Interview mein poochne ka common tareeka:** "Ek heavy, below-the-fold widget wale page ko kaise optimize karoge?" — answer: `@defer (hydrate on viewport)` exactly is use-case ke liye hai. Dekho [[new content] Deferrable Views: @defer](#new-content-deferrable-views-defer-angular-17) aur [[new content] Hydration aur Event Replay](#new-content-hydration-angular-16-aur-event-replay-angular-17).

8. **Functional interceptors/guards** aur **`inject()` function** (v14–15) — class-based `HttpInterceptor` (+ `HTTP_INTERCEPTORS`/`multi: true` boilerplate) aur class-based `CanActivate` guards ko simple functions se replace karta hai (`HttpInterceptorFn` + `withInterceptors()`, `CanActivateFn`), jo `inject()` use karke dependencies grab karte hain constructor ke bina. Yeh standalone apps mein idiomatic DI style hai. **Interview mein poochne ka common tareeka:** "`inject()` kab fail hota hai?" — answer: jab ek injection context ke bahar call kiya jaaye (e.g., ek `setTimeout` callback ke andar) — runtime error throw hota hai; dependencies ko function ke top par pehle se capture karna hota hai ya `runInInjectionContext()` use karna padta hai. Dekho [[new content] The inject() Function](#new-content-the-inject-function-angular-14), [[new content] Functional Interceptors](#new-content-functional-interceptors-angular-15), aur [Route Guards](#route-guards).
