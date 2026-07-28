import { formatDistanceToNow } from 'date-fns';
import { fr } from 'date-fns/locale';

/**
 * Format a message or conversation date as a distance to now, in french.
 * Shared by the chat window and the message list so both render dates the same way.
 */
export function formatMessageDate(date?: Date | string): string {
    if (!date) {
        return '';
    }
    return formatDistanceToNow(new Date(date), { addSuffix: true, locale: fr });
}
