
import { Component, Input, OnChanges, OnInit, SimpleChange, SimpleChanges } from '@angular/core';
import { MarkdownRendererComponent } from 'src/generic/eapi-markdown.component';
import { Conversation, Message } from 'src/model/conversation';
import { formatDistanceToNow } from 'date-fns';
import { fr } from 'date-fns/locale';
import { ErabliereApi } from 'src/core/erabliereapi.service';

@Component({
    selector: 'erabliereai-message-list',
    template: `
        <ul class="list-unstyled text-white">
          @for (message of messages; track message; let i = $index) {
            <li class="d-flex justify-content-between mb-4">
              <div class="card mask-custom">
                <div class="card-header d-flex justify-content-between p-3"
                  style="border-bottom: 1px solid rgba(255,255,255,.3); min-width: 250px;">
                  <p class="fw-bold mb-0">{{ message.isUser ? "Vous" : "ErabliereAI" }}</p>
                  <p class="text-light small mb-0"><i class="far fa-clock"></i> {{ formatMessageDate(message.createdAt)
                }}</p>
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
    imports: [MarkdownRendererComponent],
})
export class MessageListComponent implements OnInit, OnChanges {
    @Input() conversation?: Conversation;
    @Input() messages?: Message[];
    @Input() enableTranslation: boolean = false;

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
}