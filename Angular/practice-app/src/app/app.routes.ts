import { Routes } from '@angular/router';
import { AppStandaloneComponent } from './standalone/app-standalone/app-standalone.component';
import { AppPhase3SignalsComponent } from './standalone/app-phase3-signals/app-phase3-signals.component';
import { AppPhase4NewSyntaxComponent } from './standalone/app-phase4-new-syntax/app-phase4-new-syntax.component';
import { AppPhase6ReactiveFormComponent } from './standalone/app-phase6-reactive-form/app-phase6-reactive-form.component';
import { authGuardGuard } from './guards/auth-guard.guard';
import { Custom2waybindingComponent } from './custom2waybinding/custom2waybinding.component';

export const routes: Routes = [
    { path: 'standalone', component: AppStandaloneComponent },
    { path: 'signals', component: AppPhase3SignalsComponent, canActivate: [authGuardGuard] },
    { path: 'syntax', component: AppPhase4NewSyntaxComponent },
    { path: 'reactive-form', component: AppPhase6ReactiveFormComponent },
    {
        path: 'inject-demo',
        loadComponent: () => import('./Practice/inject-demo-component/inject-demo-component.component').then(m => m.InjectDemoComponentComponent),
        children: [
            {
                path: 'child-card',
                loadComponent: () => import('./Practice/inject-demo-component/Child/child-card/child-card.component').then(m => m.ChildCardComponent)
            }
        ]
    },
    {path:'banana',component:Custom2waybindingComponent,
       children: [
         {
           path: 'child',
           loadComponent: () => import('./custom2waybinding/child/child.component').then(m => m.ChildComponent  )
         }
       ]},  
];

