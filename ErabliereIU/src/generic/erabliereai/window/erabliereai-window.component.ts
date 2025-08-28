import { Component, EventEmitter, HostListener, Input, OnInit, Output } from '@angular/core';
import { ErabliereApi } from 'src/core/erabliereapi.service';
import { formatDistanceToNow } from 'date-fns';
import { fr } from 'date-fns/locale';
import { Conversation, Message, PromptResponse } from 'src/model/conversation';
import { FormsModule } from '@angular/forms';

import { MessageListComponent } from "../messageList/message-list.component";

@Component({
    selector: 'app-erabliereai-window',
    templateUrl: './erabliereai-window.component.html',
    styleUrls: ['./erabliereai-window.component.css'],
    imports: [FormsModule, MessageListComponent],
})
export class ErabliereAiWindowComponent implements OnInit {
    @Output() closeChatWindowEvent = new EventEmitter<boolean>();
    aiIsThinking = false;
    top = 8;
    skip = 0;
    displaySearch = false;
    search = '';
    lastSearch = '';
    @Input() isModal = false;

    constructor(private readonly api: ErabliereApi) {
        this.conversations = [];
        this.messages = [];
        this.typePrompt = 'Chat';
    }

    ngOnInit(): void {
        this.fetchConversations();
    }

    conversations: Conversation[];
    currentConversation?: Conversation;
    messages: Message[];
    typePrompt: string;

    fetchConversations() {
        this.api.getConversations(this.search, this.top, this.skip).then(async (conversations) => {
            if (conversations) {
                await this.handleFetchConcersationsResult(conversations);
            }
            else {
                this.conversations = [];
                this.currentConversation = undefined;
                this.messages = [];
            }
            this.lastSearch = this.search;
        });
    }

    newMessage = '';

    private async handleFetchConcersationsResult(conversations: Conversation[]) {
        if (this.currentConversation == null || this.search != this.lastSearch) {
            this.conversations = conversations;
            if (this.conversations.length > 0) {
                this.currentConversation = this.conversations[0];
                const currentMessages = await this.api.getMessages(this.currentConversation.id);
                if (currentMessages) {
                    this.messages = currentMessages;
                }
                else {
                    this.messages = [];
                }
            }
        }
        else {
            let newConversations = conversations.find((c) => {
                return c.id === this.currentConversation?.id;
            });
            if (newConversations) {
                this.currentConversation = newConversations;
                const currentMessages = await this.api.getMessages(this.currentConversation.id);
                if (currentMessages) {
                    this.messages = currentMessages;
                }
                else {
                    this.messages = [];
                }
            }
            else {
                this.conversations = conversations;
            }
        }
    }

    updateNewMessage($event: Event) {
        this.newMessage = ($event.target as HTMLInputElement).value;
    }

    sendMessage() {
        const prompt = {
            Prompt: this.newMessage,
            ConversationId: this.currentConversation?.id,
            PromptType: this.typePrompt,
            SystemMessage: this.currentConversation?.systemMessage ?? this.currentSystemPhrase
        };
        this.aiIsThinking = true;
        this.api.postPrompt(prompt).then((response: PromptResponse) => {
            this.newMessage = '';
            this.aiIsThinking = false;
            const newMessages = response.conversation?.messages;
            if (newMessages) {
                this.messages = newMessages;
            }
            if (this.currentConversation == null) {
                this.currentConversation = response.conversation;
                if (this.currentConversation) {
                    this.conversations.unshift(this.currentConversation);
                }
            }
        }).catch((error) => {
            this.aiIsThinking = false;
            console.error(error);
            alert('Error sending message ' + JSON.stringify(error));
        });
    }

    async selectConversation(conversation: any) {
        this.currentConversation = conversation;
        if (this.currentConversation) {
            this.currentSystemPhrase = this.currentConversation.systemMessage ?? this.currentSystemPhrase;
            const currentMessages = await this.api.getMessages(this.currentConversation.id);
            if (currentMessages) {
                this.messages = currentMessages;
            }
            else {
                this.messages = [];
            }
        }
        else {
            this.messages = [];
        }
    }

    keyDownNewConversation($event: KeyboardEvent) {
        if ($event.key === 'Enter') {
            this.selectConversation(null);
        }
    }

    newChat() {
        this.currentConversation = undefined;
        this.messages = [];
    }

    deleteConversation(c: any) {
        if (confirm('Êtes vous sur de vouloir supprimer cette conversation?')) {
            this.api.deleteConversation(c.id).then(() => {
                if (c.id === this.currentConversation?.id) {
                    this.currentConversation = undefined;
                    this.messages = [];
                }
                this.fetchConversations();
            });
        }
    }

    updateMessageType($event: Event) {
        this.typePrompt = ($event.target as HTMLInputElement).value;
    }

    formatMessageDate(date?: Date | string) {
        if (!date) {
            return '';
        }
        return formatDistanceToNow(new Date(date), { addSuffix: true, locale: fr });
    }

    @HostListener('document:keydown', ['$event'])
    handleKeyboardEvent(event: KeyboardEvent) {
        if (event.key === 'Escape') {
            this.closeChatWindowEvent.emit(true);
        }
    }

    searchConversation($event: Event) {
        this.skip = 0;
        this.top = 8;
        this.search = ($event.target as HTMLInputElement).value;
    }

    hideDisplaySearch() {
        this.displaySearch = !this.displaySearch;

        if (!this.displaySearch) {
            this.search = '';
            this.fetchConversations();
        }
    }

    keyDownHideDisplaySearch($event: KeyboardEvent) {
        if ($event.key === 'Enter') {
            this.hideDisplaySearch();
        }
    }

    loadMore() {
        this.skip += this.top;
        this.api.getConversations(this.search, this.top, this.skip).then((conversations) => {
            this.conversations = this.conversations.concat(conversations);
        });
    }

    elispseText(text: string, nbChar: number) {
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
        this.currentSystemPhrase = ($event.target as HTMLInputElement).value;
    }

    defaultSystemPhrase = "Vous êtes un acériculteur expérimenté avec des connaissance scientifique et pratique.";
    currentSystemPhrase?: string = this.defaultSystemPhrase;
    chatConfig: boolean = false;

    toggleChatConfig() {
        this.chatConfig = !this.chatConfig;
    }

    handleKeydown(event: KeyboardEvent) {
        if (event.key === 'Enter' && event.ctrlKey) {
            this.closeChatWindowEvent.emit(true);
        }
    }

    resetChatConfig() {
        this.currentSystemPhrase = this.defaultSystemPhrase
    }

    toggleVisibilityCurrentConversation() {
        console.log('toggleVisibilityCurrentConversation', this.currentConversation);
        if (this.currentConversation == null) {
            return;
        }

        const newState = !this.currentConversation.isPublic;
        this.api.patchConversation(this.currentConversation.id, { isPublic: newState }).then(() => {
            if (this.currentConversation) {
                this.currentConversation.isPublic = newState;
            }
        }).catch((error: any) => {
            console.error(error);
            alert('Error updating conversation ' + JSON.stringify(error));
        });
    }

    copyShareLink() {
        if (this.currentConversation == null) {
            return;
        }

        const shareLink = `${window.location.origin}/ai/public/${this.currentConversation.id}`;
        navigator.clipboard.writeText(shareLink).then(() => {

        }).catch((error) => {
            console.error(error);
            alert('Erreur lors de la copie du lien de partage ' + JSON.stringify(error));
        });
    }

    keyUpSelectConversation($event: KeyboardEvent, _t24: Conversation) {
        if ($event.key === 'Enter') {
            this.selectConversation(_t24);
        }
    }

    keyUpDeleteConversation($event: KeyboardEvent, _t24: Conversation) {
        if ($event.key === 'Enter') {
            this.deleteConversation(_t24);
        }
    }

    keyDownLoadMore($event: KeyboardEvent) {
        if ($event.key === 'Enter') {
            this.loadMore();
        }
    }
}