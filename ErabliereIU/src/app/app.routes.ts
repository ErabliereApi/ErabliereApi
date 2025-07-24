import { Routes } from '@angular/router';
import { AlerteComponent } from 'src/app/client-view/alerte/alerte.component';
import { AproposComponent } from 'src/app/client-view/apropos/apropos.component';
import { SigninRedirectCallbackComponent } from 'src/core/authorisation/signin-redirect/signin-redirect-callback.component';
import { SignoutRedirectCallbackComponent } from 'src/core/authorisation/signout-redirect/signout-redirect-callback.component';
import { DocumentationComponent } from 'src/app/client-view/documentation/documentation.component';
import { ErabliereComponent } from 'src/app/client-view/erablieres/erabliere.component';
import { NotesComponent } from 'src/app/client-view/notes/notes.component';
import { AdminCustomersComponent } from "src/app/admin-view/admin/admin-customers/admin-customers.component";
import { Page404Component } from "./page404/page404.component";
import { AdminViewComponent } from "./admin-view/admin-view.component";
import { AdminErablieresComponent } from "src/app/admin-view/admin/admin-erablieres/admin-erablieres.component";
import { ClientViewComponent } from "./client-view/client-view.component";
import { GestionCapteursComponent } from "src/app/client-view/capteurs/gestion-capteurs.component";
import { ReportsComponent } from 'src/app/client-view/rapport/rapports.component';
import { AdminAPIKeysComponent } from 'src/app/admin-view/admin/admin-apikeys/admin-apikeys.component';
import { MapViewComponent } from './map-view/map-view.component';
import { ErablieresMapComponent } from 'src/app/map-view/map/erablieres-map.component';
import { AdminGuard } from './guard/admin.guard';
import { Page401Component } from './page401/page401.component';
import { AdminHologramComponent } from 'src/app/admin-view/admin/admin-hologram/admin-hologram.component';
import { ErabliereAiWindowComponent } from 'src/generic/erabliereai/window/erabliereai-window.component';
import { ErabliereAiPublicConversationComponent } from 'src/generic/erabliereai/publicConversation/erabliereai-public-conversation.component';
import { AiViewComponent } from './ai-view/ai-view.component';
import { UserProfileComponent } from './client-view/user-profile/user-profile.component';
import { AuthenticatedUserGard } from './guard/authenticated-user.gard';
import { TermeAndConditionComponent } from './app.termes-and-conditions.component';

export const routes: Routes = [
    {
        path: 'termesandcondition',
        component: ClientViewComponent,
        children: [
            {
                path: '',
                component: TermeAndConditionComponent
            }
        ]
    },
    {
        path: 'a',
        component: AdminViewComponent,
        canActivate: [AdminGuard],
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
                path: 'hologram',
                title: 'Érablière Admin - Hologram',
                component: AdminHologramComponent
            },
            {
                path: '**',
                title: 'Érablière Admin - 404 page non trouvée',
                component: Page404Component
            }
        ]
    },
    {
        path: 'map',
        component: MapViewComponent,
        children: [
            {
                path: '',
                component: ErablieresMapComponent
            }
        ]
    },
    {
        path: 'ai',
        component: AiViewComponent,
        children: [
            {
                path: '',
                component: ErabliereAiWindowComponent
            }
        ]
    },
    {
        path: 'ai/public/:conversationId',
        component: ErabliereAiPublicConversationComponent
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
                path: 'profile',
                title: 'ÉrablièreIU - Mon profil',
                canActivate: [AuthenticatedUserGard],
                component: UserProfileComponent
            },
            {
                path: '**',
                title: 'ÉrablièreIU - 404 page non trouvée',
                component: Page404Component
            }
        ]
    },
    { path: 'signin-callback', component: SigninRedirectCallbackComponent },
    { path: 'signout-callback', component: SignoutRedirectCallbackComponent },
    {
        path: 'page401',
        component: Page401Component
    }
]
