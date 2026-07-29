export class Conversation {
    id: any;
    userId?: string;
    name?: string;
    systemMessage?: string;
    createdOn?: Date;
    isPublic: boolean = false;
    lastMessageDate?: Date;
    messages?: Message[];
}

export class Message {
    id: any;
    conversationId?: any;
    content?: string;
    createdAt?: Date;
    isUser?: boolean;
    refusal?: string;
    imageUri?: string;
    messageParts?: MessagePart[];
    /** Voir MessageTypes. Nul pour les messages antérieurs aux outils, ce qui se lit comme du texte. */
    messageType?: string;
    /** Le nom de l'outil, pour un message d'appel ou de résultat d'outil. */
    toolName?: string;
    /** Relie un résultat d'outil à l'appel correspondant. */
    toolCallId?: string;
    /** Vrai lorsque la réponse a été construite à partir de données réelles lues par les outils. */
    usedLiveData?: boolean;
}

/** Les valeurs de Message.messageType, alignées sur ErabliereApi.Donnees.Contantes.TypesMessage. */
export const MessageTypes = {
    texte: 'Texte',
    appelOutil: 'AppelOutil',
    resultatOutil: 'ResultatOutil'
} as const;

/** Vrai pour une trace technique d'outil, qui ne s'affiche pas comme un tour de parole. */
export function isToolMessage(message: Message): boolean {
    return message.messageType === MessageTypes.appelOutil ||
        message.messageType === MessageTypes.resultatOutil;
}

export class PromptResponse {
    prompt: any;
    conversation?: Conversation;
    response?: any;
}

/** Corps de la requête POST /ErabliereAI/Prompt */
export interface PostPrompt {
    Prompt: string;
    ConversationId?: any;
    PromptType?: string;
    SystemMessage?: string;
    /** L'érablière consultée au moment d'écrire le prompt, pour que le modèle cesse de deviner les identifiants. */
    ErabliereId?: string;
    ErabliereNom?: string;
    /** Identifiant généré par le client pour suivre l'activité des outils pendant l'attente. */
    ActivityId?: string;
}

/** Réponse de GET /ErabliereAI/Capabilities */
export interface ErabliereAICapabilities {
    toolsEnabled: boolean;
    plan: string;
    planGateEnabled: boolean;
    plansGrantingAccess: string[];
    subscriptionUrl?: string;
}

/** Réponse de GET /ErabliereAI/Prompt/Status/{activityId} */
export interface ToolActivity {
    steps: ToolActivityStep[];
    completed: boolean;
}

export interface ToolActivityStep {
    round: number;
    toolName?: string;
    label: string;
}

export class MessagePart {
    id: any;
    messageId?: any;
    content?: string;
    contentByte?: number[];
    contentType?: string;
}
