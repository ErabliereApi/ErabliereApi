import { HttpAgent } from '@ag-ui/client';
import { CopilotKitConfig } from '@copilotkit/angular';
import { AuthorisationFactoryService } from 'src/core/authorisation/authorisation-factory-service';
import { EnvironmentService } from 'src/environments/environment.service';

/**
 * Configuration de CopilotKit (preuve de concept) : un seul agent, ErabliereAI
 * exposé en AG-UI par l'API sur /ErabliereAI/Copilot.
 *
 * L'agent est joint directement, sans Copilot Runtime Node entre les deux : c'est
 * l'API .NET qui parle AG-UI.
 */
export function erabliereCopilotConfig(
    environment: EnvironmentService,
    authFactory: AuthorisationFactoryService): CopilotKitConfig {

    const agent = new HttpAgent({
        url: environment.apiUrl + '/ErabliereAI/Copilot',
        // HttpAgent appelle fetch directement : l'intercepteur MSAL de HttpClient ne
        // le voit jamais. Le jeton est donc demandé à chaque run, ce qui couvre aussi
        // son renouvellement au cours d'une longue conversation.
        fetch: async (url, init) => {
            const token = await authFactory.getAuthorisationService().getAccessToken();
            const headers = new Headers(init.headers);
            if (token) {
                headers.set('Authorization', `Bearer ${token}`);
            }
            return fetch(url, { ...init, headers });
        }
    });

    return {
        selfManagedAgents: { default: agent },
        enableInspector: false
    };
}
