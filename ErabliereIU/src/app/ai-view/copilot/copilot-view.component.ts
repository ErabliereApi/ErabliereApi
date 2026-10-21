import { ChangeDetectionStrategy, Component, OnInit, computed, signal } from '@angular/core';
import { FormsModule } from '@angular/forms';
import { CopilotChat, connectAgentContext, provideCopilotChatLabels, registerFrontendTool } from '@copilotkit/angular';
import { z } from 'zod';
import { ErabliereApi } from 'src/core/erabliereapi.service';
import { Erabliere } from 'src/model/erabliere';

/**
 * ErabliereAI à travers CopilotKit (preuve de concept), à comparer avec la
 * conversation classique de /ai.
 *
 * L'érablière choisie ici est transmise à l'agent comme contexte AG-UI ; l'API
 * l'écrit dans la phrase système, exactement comme le fait la conversation
 * classique avec ErabliereId. Ce n'est jamais une autorisation : les outils
 * appelés ensuite sont authentifiés comme l'utilisateur.
 */
@Component({
    selector: 'app-copilot-view',
    changeDetection: ChangeDetectionStrategy.Eager,
    imports: [CopilotChat, FormsModule],
    providers: [
        provideCopilotChatLabels({
            chatInputPlaceholder: 'Posez une question sur votre érablière…',
            welcomeMessageText: 'Bonjour! Je suis ErabliereAI. Je peux lire vos érablières, capteurs, alertes et notes.',
            chatDisclaimerText: 'Aperçu CopilotKit : les conversations ne sont pas sauvegardées.',
            assistantMessageToolbarCopyMessageLabel: 'Copier',
            assistantMessageToolbarRegenerateLabel: 'Régénérer',
            userMessageToolbarCopyMessageLabel: 'Copier',
            userMessageToolbarEditMessageLabel: 'Modifier'
        })
    ],
    templateUrl: './copilot-view.component.html'
})
export class CopilotViewComponent implements OnInit {
    readonly erablieres = signal<Erabliere[]>([]);
    readonly erabliereId = signal<string | undefined>(undefined);
    readonly erabliere = computed(() => this.erablieres().find(e => e.id === this.erabliereId()));

    constructor(private readonly api: ErabliereApi) {
        // Relu à chaque changement de sélection : c'est ce que l'API reçoit dans
        // le contexte AG-UI, entrée « erabliere ».
        connectAgentContext(() => ({
            description: 'erabliere',
            value: JSON.stringify(this.erabliere() ? { id: this.erabliere()?.id, nom: this.erabliere()?.nom } : {})
        }));

        // Un outil exécuté dans le navigateur : l'agent peut changer l'érablière
        // affichée, ce que la conversation classique ne sait pas faire.
        registerFrontendTool({
            name: 'selectionner_erabliere',
            description: "Sélectionne l'érablière affichée dans l'application, lorsque l'utilisateur demande de changer d'érablière.",
            parameters: z.object({
                erabliereId: z.string().describe("Identifiant de l'érablière à sélectionner, tel que retourné par list_erablieres.")
            }),
            handler: async ({ erabliereId }) => {
                const erabliere = this.erablieres().find(e => e.id === erabliereId);
                if (!erabliere) {
                    return { selectionnee: false, raison: "Cette érablière ne fait pas partie de la liste de l'utilisateur." };
                }
                this.erabliereId.set(erabliere.id);
                return { selectionnee: true, nom: erabliere.nom };
            }
        });
    }

    ngOnInit(): void {
        this.api.getErablieres(50)
            .then(erablieres => this.erablieres.set(erablieres))
            .catch(err => console.error(err));
    }
}
