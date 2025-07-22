import {Note} from "./note";

export class Rappel {
    id: any;
    idErabliere: any;
    isActive?: boolean;
    dateRappel?: string | null;
    dateRappelFin?: string | null;
    periodicite?: string | null;
    noteId?: any;
    note?: Note;
}
