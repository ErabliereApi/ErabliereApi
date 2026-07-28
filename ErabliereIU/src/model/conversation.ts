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
}

export class MessagePart {
    id: any;
    messageId?: any;
    content?: string;
    contentByte?: number[];
    contentType?: string;
}