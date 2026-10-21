import { Routes } from '@angular/router';
import { COPILOT_KIT_CONFIG, CopilotKit, CopilotkitAgentFactory, CopilotkitThreadsFactory } from '@copilotkit/angular';
import { AuthorisationFactoryService } from 'src/core/authorisation/authorisation-factory-service';
import { erabliereCopilotConfig } from 'src/core/erabliereai/erabliere-copilot.config';
import { EnvironmentService } from 'src/environments/environment.service';
import { CopilotViewComponent } from './copilot-view.component';

/**
 * Routes de l'aperçu CopilotKit, chargées à la demande.
 *
 * Les services de CopilotKit sont providedIn: 'root', mais les fournir à la racine
 * obligerait app.config.ts à importer le paquet, qui finirait alors dans le bundle
 * initial (plus de 4 Mo). Ils sont donc fournis ici, au niveau de la route, avec les
 * fabriques racines qui dépendent de CopilotKit : sans elles, celles de la racine
 * injecteraient un CopilotKit racine, sans configuration.
 */
export const COPILOT_ROUTES: Routes = [
    {
        path: '',
        component: CopilotViewComponent,
        providers: [
            {
                provide: COPILOT_KIT_CONFIG,
                useFactory: erabliereCopilotConfig,
                deps: [EnvironmentService, AuthorisationFactoryService]
            },
            CopilotKit,
            CopilotkitAgentFactory,
            CopilotkitThreadsFactory
        ]
    }
];
