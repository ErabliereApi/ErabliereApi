
import { Component, Input, OnChanges, OnInit, SimpleChange, SimpleChanges } from '@angular/core';
import { MarkdownRendererComponent } from 'src/generic/eapi-markdown.component';
import { Conversation, Message } from 'src/model/conversation';
import { formatDistanceToNow } from 'date-fns';
import { fr } from 'date-fns/locale';
import { ErabliereApi } from 'src/core/erabliereapi.service';
import { EButtonComponent } from "src/generic/ebutton.component";
import { marked } from 'marked';

@Component({
    selector: 'erabliereai-message-list',
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
                  <button class="btn btn-link" (click)="traduire(message.content, i)">
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
export class MessageListComponent implements OnInit, OnChanges {
    @Input() conversation?: Conversation;
    @Input() messages?: Message[];
    @Input() enableTranslation: boolean = false;
    @Input() isPublicDisplay: boolean = false;

    constructor(private readonly api: ErabliereApi) { }

    ngOnInit(): void {
        if (this.conversation) {
            if (this.conversation.messages) {
                this.messages = this.conversation.messages;
            }
            else {
                this.api.getMessages(this.conversation.id).then((messages) => {
                    if (messages) {
                        this.messages = messages;
                    }
                });
            }
        }
    }

    ngOnChanges(changes: SimpleChanges): void {
        const conversationChange: SimpleChange = changes['conversation'];
        if (conversationChange?.currentValue) {
            this.conversation = conversationChange.currentValue;
            this.messages = conversationChange.currentValue.messages;
            if (this.messages == null && this.conversation?.id) {
                this.api.getMessages(this.conversation.id).then((messages) => {
                    if (messages) {
                        this.messages = messages;
                    }
                });
            }
        }
    }

    formatMessageDate(date?: Date | string) {
        if (!date) {
            return '';
        }
        return formatDistanceToNow(new Date(date), { addSuffix: true, locale: fr });
    }

    traduire(message: string | undefined, index: number) {
        if (!message) {
            return;
        }
        this.api.traduire(message).then((response: any) => {
            if (this.messages == null) {
                return;
            }
            this.messages[index].content = response[0].translations[0].text;
        }).catch((error: any) => {
            alert('Error sending message ' + JSON.stringify(error));
        });
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