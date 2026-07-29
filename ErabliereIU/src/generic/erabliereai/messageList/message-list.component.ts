import { ChangeDetectionStrategy, Component, EventEmitter, Input, Output } from '@angular/core';
import { MarkdownRendererComponent } from 'src/generic/eapi-markdown.component';
import { Message, MessageTypes, isToolMessage } from 'src/model/conversation';
import { EButtonComponent } from "src/generic/ebutton.component";
import { formatMessageDate } from 'src/core/erabliereai/message-date';
import { marked } from 'marked';

/** Un tour de parole, avec la trace des outils qui l'ont précédé. */
interface MessageRow {
    message: Message;
    /** Les résultats d'outils consultés pour construire ce message. */
    tools: Message[];
    /** L'index du message dans la liste reçue, pour l'événement de traduction. */
    index: number;
}

/**
 * Renders the messages it is given. It owns no state and calls no API:
 * loading and translating are the responsibility of the parent.
 *
 * Les messages d'outils ne sont pas des tours de parole : ils sont regroupés sous
 * la réponse qu'ils ont servi à construire, dans un bloc repliable.
 */
@Component({
    selector: 'erabliereai-message-list',
    changeDetection: ChangeDetectionStrategy.Eager,
    template: `
        <ul class="list-unstyled text-white">
          @for (row of rows; track row.message) {
            <li class="d-flex justify-content-between mb-4">
              <div class="card mask-custom">
                <div class="card-header d-flex justify-content-between p-3"
                  style="border-bottom: 1px solid rgba(255,255,255,.3); min-width: 250px;">
                  <p class="fw-bold mb-0">
                    {{ row.message.isUser ? isPublicDisplay ? "Utilisateur" : "Vous" : "ErabliereAI" }}
                    @if (row.message.usedLiveData) {
                      <span class="badge bg-success ms-2" title="Cette réponse a été construite à partir des données réelles de votre érablière">📡 Données réelles</span>
                    }
                  </p>
                  <p class="text-light small mb-0"><ebutton class="ms-2 me-2" type="info" size="sm" (clicked)="convertToWord(row.message.content)">Exporter en .doc</ebutton><i class="far fa-clock"></i> {{ formatMessageDate(row.message.createdAt)}}</p>
              </div>
              <div class="card-body">
                @if (row.tools.length > 0) {
                  <details class="mb-3 small">
                    <summary style="cursor: pointer;">🔎 {{ row.tools.length }} consultation(s) de données</summary>
                    <ul class="list-unstyled mt-2 mb-0 ps-3">
                      @for (tool of row.tools; track tool) {
                        <li class="mb-1">
                          <strong>{{ toolLabel(tool.toolName) }}</strong> — {{ toolOutcome(tool) }}
                        </li>
                      }
                    </ul>
                  </details>
                }
                <div [className]="row.message.isUser ? '' : 'mb-5'" style="white-space: pre-wrap; word-wrap: break-word;">
                  <eapi-markdown [content]="row.message.content"></eapi-markdown>
                </div>
                @if (enableTranslation && !row.message.isUser) {
                  <button class="btn btn-link" (click)="translateRequested.emit(row.index)">
                    Traduire <span style="font-size: 1.2em;">🌐</span>
                  </button>
                }
              </div>
            </div>
          </li>
        }
        </ul>
        `,
    standalone: true,
    imports: [MarkdownRendererComponent, EButtonComponent],
})
export class MessageListComponent {
    /** Les libellés affichés pour chaque outil consulté. */
    private static readonly toolLabels: Record<string, string> = {
        'list_erablieres': 'Vos érablières',
        'get_erabliere': "L'érablière",
        'list_capteurs': 'Les capteurs',
        'get_donnees_capteur': 'Les relevés de capteur',
        'get_alertes': 'Les alertes',
        'get_alertes_capteur': 'Les alertes de capteur',
        'get_notes': 'Les notes',
        'get_barils': 'Les barils',
        'get_dompeux': 'Les dompeux',
        'get_horaire': "L'horaire",
        'list_rapports': 'Les rapports',
        'get_rapport': 'Le rapport'
    };

    rows: MessageRow[] = [];

    @Input() set messages(value: Message[] | undefined) {
        this.rows = MessageListComponent.buildRows(value ?? []);
    }

    @Input() enableTranslation: boolean = false;
    @Input() isPublicDisplay: boolean = false;

    /** Emits the index of the message the user wants translated. */
    @Output() translateRequested = new EventEmitter<number>();

    protected readonly formatMessageDate = formatMessageDate;

    /**
     * Regroupe les résultats d'outils sous le message qui les suit, c'est-à-dire la
     * réponse qu'ils ont servi à construire.
     */
    private static buildRows(messages: Message[]): MessageRow[] {
        const rows: MessageRow[] = [];
        let pendingTools: Message[] = [];

        messages.forEach((message, index) => {
            if (isToolMessage(message)) {
                // Seul le résultat porte de quoi écrire une ligne lisible; les
                // arguments de l'appel n'intéressent personne dans l'interface.
                if (message.messageType === MessageTypes.resultatOutil) {
                    pendingTools.push(message);
                }
                return;
            }

            rows.push({ message: message, tools: pendingTools, index: index });
            pendingTools = [];
        });

        return rows;
    }

    toolLabel(toolName?: string): string {
        if (!toolName) {
            return 'Consultation';
        }

        return MessageListComponent.toolLabels[toolName] ?? toolName;
    }

    /** La phrase de résumé retournée par l'outil, ou le message d'erreur. */
    toolOutcome(tool: Message): string {
        if (!tool.content) {
            return 'Aucun résultat';
        }

        try {
            const envelope = JSON.parse(tool.content);
            return envelope?.summary ?? envelope?.error ?? 'Aucun résultat';
        }
        catch {
            return 'Résultat illisible';
        }
    }

    convertToWord(content?: string, fileName = 'erabliereai-message') {
        // Enveloppe HTML recommandée pour Word
        const header = '<html xmlns:o="urn:schemas-microsoft-com:office:office" ' +
            'xmlns:w="urn:schemas-microsoft-com:office:word" ' +
            'xmlns="http://www.w3.org/TR/REC-html40">';
        const footer = '';

        const contentHtml = marked.parse(content ?? "", {
                breaks: true,
                gfm: true
            });

        if (contentHtml instanceof Promise) {
            contentHtml.then(resolvedContent => {
                this.createAndDownloadDoc(header, resolvedContent, footer, fileName);
            });
            return;
        }

        this.createAndDownloadDoc(header, contentHtml, footer, fileName);
    }

    private createAndDownloadDoc(header: string, contentHtml: string, footer: string, fileName: string) {
        const html = header +
            '' +
            '' + contentHtml +
            '' +
            footer;

        // Préfixe BOM pour les problèmes d'encodage
        const blob = new Blob(['\ufeff', html], { type: 'application/msword' });

        // Téléchargement (sans dépendance)
        const url = URL.createObjectURL(blob);
        const a = document.createElement('a');
        a.href = url;
        a.download = `${fileName}.doc`;
        document.body.appendChild(a);
        a.click();
        document.body.removeChild(a);
        URL.revokeObjectURL(url);
    }
}
