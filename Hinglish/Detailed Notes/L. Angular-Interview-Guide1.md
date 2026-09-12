# Angular Senior/Lead Interview Guide — Addendum

> Yeh `L__Angular-Interview-Guide.md` (already 2000+ lines, standalone/Signals/zoneless CD/NgRx/module-federation/a11y sab cover karta hai) ka ek **supplement** hai, replacement nahi. Maine poore existing doc ko topic-by-topic verify kiya aur sirf woh cheezein add ki hain jo genuinely missing thi — koi bhi cheez jo already achhi tarah covered hai (Signals, zoneless CD, DI, NgRx, module federation, a11y, hydration) yahaan repeat nahi ki gayi. Har section ko us tarah tag kiya gaya hai jaise original guide ke gap-analysis pass ne kiya tha, taaki dono docs consistently read ho sakein.

## What This Covers (New Gaps Identified)

1. [Signal-Based Component APIs: input() / output() / model()](#signal-based-component-apis-input--output--model)
2. [hostDirectives (Angular 15+)](#hostdirectives-angular-15)
3. [afterRender / afterNextRender (Angular 16+)](#afterrender--afternextrender-angular-16)
4. [NgOptimizedImage (Angular 15+)](#ngoptimizedimage-angular-15)
5. [Router View Transitions (Angular 17+)](#router-view-transitions-angular-17)
6. [Internationalization (i18n)](#internationalization-i18n)
7. [Sample Interview Q&A (Addendum)](#sample-interview-qa-addendum)

---

## [gaps] Signal-Based Component APIs: input() / output() / model()

Existing guide `@Input()`/`@Output()` decorators aur Signals ko separately cover karta hai, lekin unke beech ke **naye pul** — `input()`, `output()`, aur `model()` functions (Angular 17.1–17.3+) — ko nahi. Yeh 2026 ke senior Angular interviews mein consistently poocha jaata hai kyunki yeh "naya idiomatic tareeka kya hai component APIs likhne ka" question ka direct jawab hai, exactly waise hi jaise `inject()` ne constructor injection ke against poocha jaata hai.

**`input()`** — decorator-based `@Input()` ka signal-based replacement. Result ek **read-only Signal** hai, plain property nahi:

```typescript
import { Component, input, computed } from '@angular/core';

@Component({ selector: 'app-user-card', standalone: true, template: `{{ initials() }}` })
export class UserCardComponent {
  name = input.required<string>();        // required — no default, must be bound
  role = input<string>('viewer');          // optional with default value
  initials = computed(() => this.name().split(' ').map(w => w[0]).join(''));
}
```

**`output()`** — `@Output() + EventEmitter` ka replacement, plain function API ke saath:

```typescript
import { Component, output } from '@angular/core';

@Component({ selector: 'app-item', standalone: true, template: `<button (click)="deleted.emit(id)">Delete</button>` })
export class ItemComponent {
  deleted = output<number>();
}
```

**`model()`** — two-way binding ke liye — `@Input()` + matching `@Output() xChange` ke boilerplate ko replace karta hai ek hi writable Signal mein:

```typescript
import { Component, model } from '@angular/core';

@Component({ selector: 'app-counter', standalone: true, template: `<button (click)="count.set(count() + 1)">{{ count() }}</button>` })
export class CounterComponent {
  count = model(0); // parent: <app-counter [(count)]="value" />
}
```

**Interviewers yeh kyun probe karte hain:**
- `input()`/`output()` **plain properties nahi hain** — `name()` call karna padta hai, `this.name` nahi (ek common migration bug). Template mein bhi `{{ name() }}`.
- `input.required<T>()` compile-time enforcement deta hai ki caller value bind kare — `@Input()` decorator ke saath yeh sirf runtime `@Input({ required: true })` ke through tha (Angular 16+), ab type-level bhi cleaner hai.
- `output()` internally still ek RxJS-compatible `Subject`-jaisa hi hai (`.subscribe()` bhi karne layak), lekin API surface `EventEmitter` extend nahi karta — yeh deliberately decoupled hai taaki future mein implementation change ho sake bina public API break kiye.
- `model()` do-tarफा binding ke liye `@Input`/`@Output` pair likhne ka poora ceremony eliminate karta hai — parent side par `[(count)]="value"` syntax same "banana in a box" desugaring use karta hai jo `[(ngModel)]` use karta hai.
- Yeh sab **decorator-based `@Input`/`@Output` ke saath coexist** karte hain — yeh ek hard breaking migration nahi hai, dono styles ek hi codebase/component mein mix ho sakte hain, jaise standalone aur NgModule-based components coexist karte hain.

---

## [gaps] hostDirectives (Angular 15+)

Existing guide mixins ko (TS guide ki tarah) discuss nahi karta Angular context mein — `hostDirectives` Angular ka behavior-composition primitive hai, aur senior-level "composition over inheritance in Angular" question ka direct, concrete jawab hai.

Traditionally, agar aapko multiple reusable behaviors (jaise, tooltip logic + draggable logic) ek component par combine karne the, aapke paas do options the: inheritance (fragile, single-parent-only) ya manual delegation boilerplate. `hostDirectives` ek component/directive ko doosri directives ko apne host element par directly apply karne deta hai, unki functionality "inherit" karte hue bina class inheritance ke:

```typescript
@Directive({ selector: '[appHighlightable]', standalone: true })
export class HighlightableDirective {
  @HostBinding('style.backgroundColor') color = 'yellow';
}

@Component({
  selector: 'app-card',
  standalone: true,
  template: `<ng-content />`,
  hostDirectives: [
    { directive: HighlightableDirective, inputs: ['color'] }, // exposes `color` as a CardComponent input too
  ],
})
export class CardComponent {}
```

Key points jo interviewers expect karte hain:
- Yeh **composition** hai, inheritance nahi — `CardComponent` `HighlightableDirective` extend nahi karta; woh directive ka ek instance apne host element par silently apply hota hai.
- `inputs`/`outputs` arrays explicitly control karte hain ki host directive ke kaunse `@Input`/`@Output` (ya `input()`/`output()`) outer component ke public API se re-export ho.
- Multiple `hostDirectives` combine ho sakti hain ek hi component par — multiple unrelated behaviors (draggable + resizable + highlightable) ko compose karne ka ek clean tareeka, jo pehle sirf mixins-jaisi manual delegation se possible tha.

---

## [gaps] afterRender / afterNextRender (Angular 16+)

Existing guide classic lifecycle hooks (`ngOnInit`, `ngAfterViewInit`, etc.) cover karta hai, lekin naye render-timing hooks nahi jo specifically `ngAfterViewInit`/`ngAfterViewChecked` ki ek real limitation solve karte hain: woh zoneless/SSR-safe nahi hain aur DOM measurement ke liye guaranteed-post-paint timing nahi dete.

```typescript
import { Component, afterNextRender, afterRender, ElementRef, inject } from '@angular/core';

@Component({ selector: 'app-chart', standalone: true, template: `<canvas #canvas></canvas>` })
export class ChartComponent {
  private el = inject(ElementRef);

  constructor() {
    afterNextRender(() => {
      // runs ONCE, after the component (and its children) have rendered for the first time —
      // safe place to measure DOM / initialize a third-party library that needs real dimensions
      this.initChartLibrary(this.el.nativeElement);
    });

    afterRender(() => {
      // runs after EVERY change detection cycle's render — use sparingly, real perf cost
    });
  }
}
```

Interview framing: `ngAfterViewInit` DOM-present guarantee deta hai, lekin **browser-rendered/painted** guarantee nahi (especially SSR ke under, jahan `ngAfterViewInit` server par bhi fire hota hai, jahan koi real browser rendering hoti hi nahi). `afterNextRender`/`afterRender` **sirf browser mein run hote hain** (SSR ke under automatically skip ho jaate hain — no manual `isPlatformBrowser` check chahiye), aur specifically un cases ke liye design kiye gaye hain jahan aapko third-party DOM libraries (charting libs, map libs) initialize karni ho jo actual layout par depend karti hain. Yeh zoneless change detection ke saath bhi correctly kaam karte hain, jabki classic lifecycle-hook-plus-`setTimeout(0)` hacks (jo yeh timing problem pehle solve karte the) zone-dependent the.

---

## [gaps] NgOptimizedImage (Angular 15+)

Existing Performance section bundle size/lazy loading/`@defer`/virtual scrolling cover karta hai — sab genuinely important, lekin sabse common single Core Web Vitals regression (unoptimized `<img>` tags, poor LCP) ke liye Angular ke built-in fix ko miss kar jaata hai.

```html
<!-- Before -->
<img src="hero.jpg" alt="Hero banner" />

<!-- After -->
<img ngSrc="hero.jpg" width="1200" height="600" priority alt="Hero banner" />
```

```typescript
import { NgOptimizedImage } from '@angular/common';
// component imports: [NgOptimizedImage]
```

Kya karta hai:
- `width`/`height` **enforce** karta hai (build/runtime warning agar missing) taaki browser layout space pehle se reserve kar sake — **Cumulative Layout Shift (CLS)** prevent karta hai.
- `priority` attribute above-the-fold hero images ke liye `fetchpriority="high"` set karta hai aur `loading="eager"` use karta hai — **Largest Contentful Paint (LCP)** improve karta hai; non-priority images automatically `loading="lazy"` ho jaati hain.
- Optionally ek image loader (Cloudinary/Imgix/custom CDN loader) configure kiya ja sakta hai automatic `srcset` generation ke liye, responsive images ke liye.

Interview framing: "Yeh ek deliberately opinionated, `<img>` ka drop-in-ish replacement hai jo common LCP/CLS mistakes ko ya toh build warning ya runtime dev-mode console warning bana deta hai — production Lighthouse scores ke liye single-highest-ROI Angular-specific change jo aap ek existing app mein retrofit kar sakte ho."

---

## [gaps] Router View Transitions (Angular 17+)

Existing guide routing, guards, lazy loading cover karta hai, lekin route-transition animations (browser-native View Transitions API integration) nahi — jo `@Component` `animations` array ke saath overlap karta hai lekin genuinely different mechanism hai.

```typescript
import { provideRouter, withViewTransitions } from '@angular/router';

bootstrapApplication(AppComponent, {
  providers: [provideRouter(routes, withViewTransitions())],
});
```

Yeh browser-native **View Transitions API** ko wire karta hai route navigation ke saath — jab enabled ho, navigation ke dauraan browser automatically old/new DOM states ke beech ek smooth crossfade/morph animation perform karta hai, bina manual `@angular/animations` triggers likhe. Yeh browsers mein progressively enhance hota hai (unsupported browsers mein silently normal instant navigation ho jaati hai — no polyfill needed, no broken experience).

Interview framing: yeh `@angular/animations` (component-level, JS-driven, fine-grained control) ka replacement **nahi** hai — yeh specifically **route-level page transitions** ke liye hai jahan aap full-page-navigation-feel wali smoothness chahte ho minimal code ke saath; complex component-internal animations ke liye `@angular/animations` (ya CSS transitions) still sahi tool hai.

---

## [gaps] Internationalization (i18n)

Existing guide mein i18n ka **zero** coverage hai — enterprise/global-audience apps ke liye yeh ek common lead-level requirement hai aur original notes mein bilkul absent tha.

Angular ka built-in i18n **compile-time** approach hai (runtime string-swapping libraries jaise `ngx-translate` ke against ek deliberate design choice):

```html
<h1 i18n="@@welcomeHeader">Welcome to our app</h1>
<p i18n="header|Greeting shown to logged-in users@@greeting">Hello, {{ name }}!</p>
```

```bash
ng extract-i18n --output-path src/locale   # generates messages.xlf — translators fill this in
```

```json
// angular.json — one build target per locale
"i18n": {
  "sourceLocale": "en-US",
  "locales": { "hi": "src/locale/messages.hi.xlf", "fr": "src/locale/messages.fr.xlf" }
}
```

Har locale ke liye build time par ek **separate, fully-localized bundle** produce hota hai — koi runtime translation-lookup overhead nahi, koi missing-key runtime fallback bug class nahi (missing translations **build time par** fail hoti hain), lekin trade-off yeh hai ki language switch karne ke liye ek full page reload (different locale bundle load karna) chahiye, runtime ek SPA ke andar instant switch nahi.

**Interview framing:** "Angular ka native i18n runtime libraries se trade a different set of problems karta hai — aapko missing translations compile time par milti hain runtime mein silently English fallback hone ke bajaye, aur per-locale bundles ka matlab hai koi translation-loading JS overhead nahi, lekin cost par ki language switching ek reload maangta hai. Runtime library (`ngx-translate`/`@angular/localize` ke `$localize` tagged templates programmatically) tab better fit hoti hai jab aapko ek hi session mein instant, no-reload language switching chahiye ho — e.g. ek user-facing language toggle — jabki compile-time i18n locale-per-market deployments (alag country domains/subdomains) ke liye better fit hai."

---

## Sample Interview Q&A (Addendum)

**Q: `input()` signal function `@Input()` decorator se practically kaise differ karta hai, aur aap migrate kyun/kab karoge?**
A: `@Input()` ek plain class property create karta hai jise Angular externally set karta hai; `input()` ek read-only **Signal** return karta hai, isliye value ko `this.name()` se read karna padta hai, `this.name` se nahi — dono template aur class code mein. Practical benefit yeh hai ki `input()`-based values automatically fine-grained reactivity mein integrate ho jaate hain (`computed()`, `effect()` ke saath directly compose ho sakte hain change-detection boilerplate ke bina), aur `input.required<T>()` ek naye, cleaner compile-time-enforced required-input pattern deta hai. Main naye components ke liye `input()`/`output()`/`model()` use karunga, aur existing `@Input()`-heavy components ko sirf tab migrate karunga jab woh anyway touch ho rahe hon — yeh ek forced big-bang migration nahi hai, dono styles same codebase mein coexist karte hain.

**Q: `hostDirectives` `@Component` inheritance (ek component ko doosre se extend karna) se better kab hai?**
A: Jab aapko multiple, independent, reusable behaviors ek component par combine karne hon — jaise draggable + resizable + highlightable — `hostDirectives` unhe freely compose hone deta hai, jabki class inheritance Angular mein single-parent-only hoti hai aur behaviors ko ek rigid hierarchy mein force kar deti hai jo unrelated concerns ko couple kar deti hai. Yeh "favor composition over inheritance" principle ka ek Angular-specific, first-class-supported implementation hai, ad-hoc manual delegation boilerplate likhne ke bajaye.

**Q: `NgOptimizedImage` ka `priority` attribute kya karta hai, aur aap kaunsi images par ise lagaoge?**
A: `priority` browser ko `fetchpriority="high"` aur eager loading signal karta hai, jo us image ko LCP (Largest Contentful Paint) candidate ki tarah treat karne ka instruction hai — yeh sirf viewport ke **above-the-fold** ek-do sabse important images par lagana chahiye (hero banner, primary product image), kyunki bahut saari images par ise lagana unke saare priority signals ko diminish kar deta hai (agar sab high-priority hain to koi bhi actually high-priority nahi hai). Baaki saari images automatically default lazy-loading behavior use karti hain, jo unhe viewport ke paas aane tak defer karti hai.

---

## How This Addendum Fits With the Existing Guide

Priority order for study, if time-limited: the existing 2000+ line guide already covers the topics interviewers spend the most time on (Signals fundamentals, change detection, RxJS, NgRx, DI, security). This addendum's six topics are individually lower-frequency but each is a distinct "have you kept current" signal — `input()`/`output()`/`model()` and `NgOptimizedImage` are the two most likely to actually come up in a 2026 interview; `hostDirectives`, `afterRender`, view transitions, and i18n are more situational (come up if the role specifically touches component libraries, third-party DOM integrations, page transitions, or global/enterprise apps respectively).
