import { ChangeDetectionStrategy, Component, EventEmitter, HostListener, Input, OnInit, Output } from '@angular/core';
import { Conversation } from 'src/model/conversation';
import { FormsModule } from '@angular/forms';
import { ErabliereAiChatService } from 'src/core/erabliereai/erabliereai-chat.service';
import { formatMessageDate } from 'src/core/erabliereai/message-date';

import { MessageListComponent } from "../messageList/message-list.component";

/**
 * The chat window. It only wires DOM events to the chat service, the state and
 * the API calls live in ErabliereAiChatService.
 */
@Component({
    selector: 'app-erabliereai-window',
    templateUrl: './erabliereai-window.component.html',
    styleUrls: ['./erabliereai-window.component.css'],
    imports: [FormsModule, MessageListComponent],
    changeDetection: ChangeDetectionStrategy.Eager,
    providers: [ErabliereAiChatService],
})
export class ErabliereAiWindowComponent implements OnInit {
    @Output() closeChatWindowEvent = new EventEmitter<boolean>();
    @Input() isModal = false;

    displaySearch = false;
    chatConfig = false;
    newMessage = '';

    protected readonly formatMessageDate = formatMessageDate;

    constructor(readonly chat: ErabliereAiChatService) { }

    ngOnInit(): void {
        this.fetchConversations();
    }

    fetchConversations() {
        this.chat.fetchConversations();
    }

    updateNewMessage($event: Event) {
        this.newMessage = ($event.target as HTMLInputElement).value;
    }

    sendMessage() {
        this.chat.sendMessage(this.newMessage).then(() => {
            this.newMessage = '';
        }).catch((error) => {
            console.error(error);
            alert('Error sending message ' + JSON.stringify(error));
        });
    }

    selectConversation(conversation?: Conversation) {
        this.chat.selectConversation(conversation);
    }

    keyDownNewConversation($event: KeyboardEvent) {
        if ($event.key === 'Enter') {
            this.selectConversation(undefined);
        }
    }

    deleteConversation(conversation: Conversation) {
        if (confirm('Êtes vous sur de vouloir supprimer cette conversation?')) {
            this.chat.deleteConversation(conversation);
        }
    }

    updateMessageType($event: Event) {
        this.chat.typePrompt = ($event.target as HTMLInputElement).value;
    }

    searchConversation($event: Event) {
        this.chat.updateSearch(($event.target as HTMLInputElement).value);
    }

    hideDisplaySearch() {
        this.displaySearch = !this.displaySearch;

        if (!this.displaySearch) {
            this.chat.clearSearch();
        }
    }

    keyDownHideDisplaySearch($event: KeyboardEvent) {
        if ($event.key === 'Enter') {
            this.hideDisplaySearch();
        }
    }

    loadMore() {
        this.chat.loadMore();
    }

    ellipsisText(text: string, nbChar: number) {
        if (text == null) {
            return '';
        }
        if (text.length > nbChar) {
            return text.slice(0, nbChar) + '...';
        }
        else {
            return text;
        }
    }

    updateChatConfig($event: Event) {
        this.chat.currentSystemPhrase = ($event.target as HTMLInputElement).value;
    }

    toggleChatConfig() {
        this.chatConfig = !this.chatConfig;
    }

    resetChatConfig() {
        this.chat.resetSystemPhrase();
    }

    toggleVisibilityCurrentConversation() {
        this.chat.toggleVisibilityCurrentConversation().catch((error: any) => {
            console.error(error);
            alert('Error updating conversation ' + JSON.stringify(error));
        });
    }

    copyShareLink() {
        const shareLink = this.chat.getShareLink();

        if (!shareLink) {
            return;
        }

        navigator.clipboard.writeText(shareLink).catch((error) => {
            console.error(error);
            alert('Erreur lors de la copie du lien de partage ' + JSON.stringify(error));
        });
    }

    traduire(index: number) {
        this.chat.translateMessage(index).catch((error: any) => {
            alert('Error sending message ' + JSON.stringify(error));
        });
    }

    keyUpSelectConversation($event: KeyboardEvent, conversation: Conversation) {
        if ($event.key === 'Enter') {
            this.selectConversation(conversation);
        }
    }

    keyUpDeleteConversation($event: KeyboardEvent, conversation: Conversation) {
        if ($event.key === 'Enter') {
            this.deleteConversation(conversation);
        }
    }

    keyDownLoadMore($event: KeyboardEvent) {
        if ($event.key === 'Enter') {
            this.loadMore();
        }
    }

    @HostListener('document:keydown', ['$event'])
    handleKeyboardEvent(event: KeyboardEvent) {
        if (event.key === 'Escape') {
            this.closeChatWindowEvent.emit(true);
        }
    }
}
