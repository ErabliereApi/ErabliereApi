import { ChangeDetectionStrategy, Component, EventEmitter, Input, Output } from '@angular/core';
import { MarkdownRendererComponent } from 'src/generic/eapi-markdown.component';
import { Message } from 'src/model/conversation';
import { EButtonComponent } from "src/generic/ebutton.component";
import { formatMessageDate } from 'src/core/erabliereai/message-date';
import { marked } from 'marked';

/**
 * Renders the messages it is given. It owns no state and calls no API:
 * loading and translating are the responsibility of the parent.
 */
@Component({
    selector: 'erabliereai-message-list',
    changeDetection: ChangeDetectionStrategy.Eager,
    template: `
        <ul class="list-unstyled text-white">
          @for (message of messages; track message; let i = $index) {
            <li class="d-flex justify-content-between mb-4">
              <div class="card mask-custom">
                <div class="card-header d-flex justify-content-between p-3"
                  style="border-bottom: 1px solid rgba(255,255,255,.3); min-width: 250px;">
                  <p class="fw-bold mb-0">{{ message.isUser ? isPublicDisplay ? "Utilisateur" : "Vous" : "ErabliereAI" }}</p>
                  <p class="text-light small mb-0"><ebutton class="ms-2 me-2" type="info" size="sm" (clicked)="convertToWord(message.content)">Exporter en .doc</ebutton><i class="far fa-clock"></i> {{ formatMessageDate(message.createdAt)}}</p>
              </div>
              <div class="card-body">
                <div [className]="message.isUser ? '' : 'mb-5'" style="white-space: pre-wrap; word-wrap: break-word;">
                  <eapi-markdown [content]="message.content"></eapi-markdown>
                </div>
                @if (enableTranslation && !message.isUser) {
                  <button class="btn btn-link" (click)="translateRequested.emit(i)">
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
    @Input() messages?: Message[];
    @Input() enableTranslation: boolean = false;
    @Input() isPublicDisplay: boolean = false;

    /** Emits the index of the message the user wants translated. */
    @Output() translateRequested = new EventEmitter<number>();

    protected readonly formatMessageDate = formatMessageDate;

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
