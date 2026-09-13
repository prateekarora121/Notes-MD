import { Routes } from '@angular/router';
import { AppStandaloneComponent } from './standalone/app-standalone/app-standalone.component';
import { AppPhase3SignalsComponent } from './standalone/app-phase3-signals/app-phase3-signals.component';
import { AppPhase4NewSyntaxComponent } from './standalone/app-phase4-new-syntax/app-phase4-new-syntax.component';
import { AppPhase6ReactiveFormComponent } from './standalone/app-phase6-reactive-form/app-phase6-reactive-form.component';

export const routes: Routes = [{ path: 'standalone', component: AppStandaloneComponent },
    { path: 'signals', component: AppPhase3SignalsComponent },
    {path:'syntax',component:AppPhase4NewSyntaxComponent},
    {path:'reactive-form',component:AppPhase6ReactiveFormComponent}
];

