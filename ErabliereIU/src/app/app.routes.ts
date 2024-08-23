import { Routes } from '@angular/router';
import { AlerteComponent } from 'src/alerte/alerte.component';
import { AproposComponent } from 'src/apropos/apropos.component';
import { SigninRedirectCallbackComponent } from 'src/authorisation/signin-redirect/signin-redirect-callback.component';
import { SignoutRedirectCallbackComponent } from 'src/authorisation/signout-redirect/signout-redirect-callback.component';
import { DocumentationComponent } from 'src/documentation/documentation.component';
import { ErabliereComponent } from 'src/erablieres/erabliere.component';
import { NotesComponent } from 'src/notes/notes.component';
import {AdminCustomersComponent} from "../admin/admin-customers/admin-customers.component";
import {Page404Component} from "./page404/page404.component";
import {AdminViewComponent} from "./admin-view/admin-view.component";
import {AdminErablieresComponent} from "../admin/admin-erablieres/admin-erablieres.component";
import {ClientViewComponent} from "./client-view/client-view.component";
import {GestionCapteursComponent} from "../capteurs/gestion-capteurs.component";
import { ReportsComponent } from 'src/rapport/rapports.component';
import { AdminAPIKeysComponent } from 'src/admin/admin-apikeys/admin-apikeys.component';

export const routes: Routes = [
    {
        path: 'a',
        component: AdminViewComponent,
        children: [
            {
                path: '',
                redirectTo: 'customers',
                pathMatch: 'full'
            },
            {
                path: 'customers',
                title: 'Érablière Admin - Clients',
                component: AdminCustomersComponent,
            },
            {
                path: 'erablieres',
                title: 'Érablière Admin - Érablières',
                component: AdminErablieresComponent,
            },
            {
                path: 'apikeys',
                title: 'Érablière Admin - API Keys',
                component: AdminAPIKeysComponent,
            },
            {
                path: '**',
                title: 'Érablière Admin - 404 page non trouvée',
                component: Page404Component
            }
        ]

    },
    {
        path: '',
        component: ClientViewComponent,
        children: [
            {
                path: '',
                redirectTo: 'e',
                pathMatch: 'full'
            },
            {
                path: 'e',
                title: 'ÉrablièreIU - Dashboard',
                component: ErabliereComponent
            },
            {
                path: 'e/:idErabliereSelectionee',
                redirectTo: 'e/:idErabliereSelectionee/graphiques',
                pathMatch: 'full'
            },
            {
                path: 'e/:idErabliereSelectionee/graphiques',
                title: 'ÉrablièreIU - Dashboard',
                component: ErabliereComponent
            },
            {
                path: 'e/:idErabliereSelectionee/alertes',
                title: 'ÉrablièreIU - Alertes',
                component: AlerteComponent
            },
            {
                path: 'e/:idErabliereSelectionee/documentation',
                title: 'ÉrablièreIU - Documentation',
                component: DocumentationComponent
            },
            {
                path: 'e/:idErabliereSelectionee/notes',
                title: 'ÉrablièreIU - Notes',
                component: NotesComponent
            },
            {
                path: 'e/:idErabliereSelectionee/capteurs',
                title: 'ÉrablièreIU - Capteurs',
                component: GestionCapteursComponent
            },
            {
                path: 'e/:idErabliereSelectionee/rapports',
                title: 'ÉrablièreIU - Rapports',
                component: ReportsComponent
            },
            {
                path: 'apropos',
                title: 'ÉrablièreIU - À propos',
                component: AproposComponent
            },
            {
                path: '**',
                title: 'ÉrablièreIU - 404 page non trouvée',
                component: Page404Component
            }
        ]
    },
    { path: 'signin-callback', component: SigninRedirectCallbackComponent },
    { path: 'signout-callback', component: SignoutRedirectCallbackComponent }
]
