import { Injectable } from '@angular/core';
import { ErabliereApi } from 'src/core/erabliereapi.service';
import { Conversation, ErabliereAICapabilities, Message, PostPrompt } from 'src/model/conversation';
import { ErabliereContextService } from './erabliere-context.service';

/**
 * Holds the state of an ErabliereAI chat and talks to the API on behalf of the view.
 *
 * Provided at the component level, not in root: the chat widget and the /ai route
 * each get their own conversation state.
 */
@Injectable()
export class ErabliereAiChatService {
    /** Intervalle entre deux lectures de l'avancement, en millisecondes. */
    private static readonly statusPollingInterval = 1000;

    conversations: Conversation[] = [];
    currentConversation?: Conversation;
    messages: Message[] = [];
    aiIsThinking = false;
    typePrompt = 'Chat';
    currentSystemPhrase: string = "";

    /** Ce que l'assistant a le droit de faire, chargé à l'ouverture de la conversation. */
    capabilities?: ErabliereAICapabilities;

    /** Ce que l'assistant est en train de faire, affiché pendant l'attente. */
    activityLabel = '';

    top = 8;
    skip = 0;
    search = '';
    private lastSearch = '';
    private statusPolling?: ReturnType<typeof setInterval>;

    constructor(
        private readonly api: ErabliereApi,
        private readonly erabliereContext: ErabliereContextService) { }

    /**
     * Load the first page of conversations, and the messages of the selected one.
     */
    async fetchConversations(): Promise<void> {
        const conversations = await this.api.getConversations(this.search, this.top, this.skip);

        if (this.currentConversation == null || this.search != this.lastSearch) {
            this.conversations = conversations;
            if (this.conversations.length > 0) {
                await this.selectConversation(this.conversations[0], false);
            }
        }
        else {
            const refreshed = conversations.find(c => c.id === this.currentConversation?.id);
            if (refreshed) {
                await this.selectConversation(refreshed, false);
            }
            else {
                this.conversations = conversations;
            }
        }

        this.lastSearch = this.search;
    }

    /**
     * Charge ce que le forfait de l'utilisateur ouvre. Un échec laisse simplement
     * l'interface muette sur le sujet plutôt que de casser la conversation.
     */
    async fetchCapabilities(): Promise<void> {
        try {
            this.capabilities = await this.api.getErabliereAICapabilities();
        }
        catch {
            this.capabilities = undefined;
        }
    }

    /** Vrai lorsqu'il faut inviter discrètement l'utilisateur à s'abonner. */
    get shouldSuggestUpgrade(): boolean {
        return this.capabilities?.planGateEnabled === true && this.capabilities?.toolsEnabled === false;
    }

    /**
     * Select a conversation, or start a new one when null is passed.
     */
    async selectConversation(conversation?: Conversation, updateSystemPhrase = true): Promise<void> {
        this.currentConversation = conversation ?? undefined;

        if (!this.currentConversation) {
            this.messages = [];
            return;
        }

        if (updateSystemPhrase) {
            this.currentSystemPhrase = this.currentConversation.systemMessage ?? this.currentSystemPhrase;
        }

        this.messages = await this.api.getMessages(this.currentConversation.id) ?? [];
    }

    /**
     * Send a prompt and refresh the messages of the conversation with the answer.
     */
    async sendMessage(newMessage: string): Promise<void> {
        const context = this.erabliereContext.getContext();
        const activityId = this.newActivityId();

        const prompt: PostPrompt = {
            Prompt: newMessage,
            ConversationId: this.currentConversation?.id,
            PromptType: this.typePrompt,
            SystemMessage: this.currentConversation?.systemMessage ?? this.currentSystemPhrase,
            ErabliereId: context?.id,
            ErabliereNom: context?.nom,
            ActivityId: activityId
        };

        this.aiIsThinking = true;
        this.startStatusPolling(activityId);

        try {
            const response = await this.api.postPrompt(prompt);

            const conversation = response.conversation;

            if (this.currentConversation == null && conversation) {
                this.currentConversation = conversation;
                this.conversations.unshift(conversation);
            }

            // La réponse ne porte pas toujours les messages, on les recharge au besoin
            // pour que la liste affiche l'échange qui vient d'avoir lieu.
            this.messages = conversation?.messages
                ?? await this.api.getMessages(this.currentConversation?.id) ?? [];
        }
        finally {
            this.stopStatusPolling();
            this.aiIsThinking = false;
        }
    }

    async deleteConversation(conversation: Conversation): Promise<void> {
        await this.api.deleteConversation(conversation.id);

        if (conversation.id === this.currentConversation?.id) {
            this.currentConversation = undefined;
            this.messages = [];
        }

        await this.fetchConversations();
    }

    async loadMore(): Promise<void> {
        this.skip += this.top;
        const conversations = await this.api.getConversations(this.search, this.top, this.skip);
        this.conversations = this.conversations.concat(conversations);
    }

    /**
     * Reset the paging before a new search.
     */
    updateSearch(search: string): void {
        this.skip = 0;
        this.top = 8;
        this.search = search;
    }

    async clearSearch(): Promise<void> {
        this.search = '';
        await this.fetchConversations();
    }

    async toggleVisibilityCurrentConversation(): Promise<void> {
        if (this.currentConversation == null) {
            return;
        }

        const newState = !this.currentConversation.isPublic;
        await this.api.patchConversation(this.currentConversation.id, { isPublic: newState });
        this.currentConversation.isPublic = newState;
    }

    getShareLink(): string | undefined {
        if (this.currentConversation == null) {
            return undefined;
        }

        return `${window.location.origin}/ai/public/${this.currentConversation.id}`;
    }

    /**
     * Translate a message in place, the message list only renders what it is given.
     */
    async translateMessage(index: number): Promise<void> {
        const message = this.messages[index];

        if (!message?.content) {
            return;
        }

        const response = await this.api.traduire(message.content);
        this.messages[index].content = response[0].translations[0].text;
    }

    resetSystemPhrase(): void {
        this.currentSystemPhrase = "";
    }

    /**
     * Interroge l'avancement du prompt pendant que la réponse se construit.
     *
     * Le suivi est du confort : si l'instance interrogée n'est pas celle qui traite
     * le prompt, la réponse revient vide et l'interface garde son libellé générique.
     */
    private startStatusPolling(activityId: string): void {
        this.stopStatusPolling();
        this.activityLabel = '';

        this.statusPolling = setInterval(() => {
            this.api.getPromptStatus(activityId)
                .then(activity => {
                    if (activity?.completed) {
                        this.stopStatusPolling();
                        return;
                    }

                    const lastStep = activity?.steps?.at(-1);
                    this.activityLabel = lastStep?.label ?? '';
                })
                .catch(() => { /* un libellé manquant ne coûte rien */ });
        }, ErabliereAiChatService.statusPollingInterval);
    }

    private stopStatusPolling(): void {
        if (this.statusPolling) {
            clearInterval(this.statusPolling);
            this.statusPolling = undefined;
        }

        this.activityLabel = '';
    }

    private newActivityId(): string {
        if (typeof crypto !== 'undefined' && typeof crypto.randomUUID === 'function') {
            return crypto.randomUUID();
        }

        // Contexte non sécurisé : l'identifiant ne sert qu'à retrouver un suivi
        // d'affichage, une valeur pseudo aléatoire suffit.
        return `${Date.now().toString(16)}-${Math.floor(Math.random() * 0xffffffff).toString(16)}`;
    }
}
