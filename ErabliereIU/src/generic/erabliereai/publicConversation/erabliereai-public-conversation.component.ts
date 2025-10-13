import { Component, OnInit } from '@angular/core';
import { ActivatedRoute } from '@angular/router';
import { ErabliereApi } from 'src/core/erabliereapi.service';
import { MessageListComponent } from '../messageList/message-list.component';
import { Conversation } from 'src/model/conversation';

@Component({
    selector: 'app-erabliereai-public-conversation',
    template: `
        <div class="gradient-custom">
            <div class="container py-5">
                <div class="text-center mb-4">
                    <h3 class="fw-bold text-black" style="opacity: 75%;">{{conversation?.name}}</h3>
                    <p class="text-white mb-0">
                        Phrase système : {{ conversation?.systemMessage }}
                    </p>
                </div>
                <erabliereai-message-list [conversation]="conversation" [isPublicDisplay]="true"></erabliereai-message-list>
            </div>
        </div>
    `,
    imports: [MessageListComponent],
})
export class ErabliereAiPublicConversationComponent implements OnInit {
    conversationId: any;
    conversation?: Conversation;
    constructor(private readonly router: ActivatedRoute, private readonly api: ErabliereApi) {
        this.router.params.subscribe((params) => {
            this.conversationId = params['conversationId'];
        });
    }

    ngOnInit() {
        this.api.getPublicConversation(this.conversationId).then((conversation) => {
            if (conversation) {
                this.conversation = conversation;
            }
        });
    }
}