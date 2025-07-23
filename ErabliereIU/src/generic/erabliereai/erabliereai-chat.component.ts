import { Component } from '@angular/core';
import { ErabliereAiWindowComponent } from './window/erabliereai-window.component';

@Component({
    selector: 'app-chat-widget',
    templateUrl: './erabliereai-chat.component.html',
    styleUrls: ['./erabliereai-chat.component.css'],
    imports: [ErabliereAiWindowComponent],
})
export class ErabliereAIComponent {
    chatOpen = false;

    openChat() {
        this.chatOpen = true;
    }

    closeChat() {
        this.chatOpen = false;
    }
}