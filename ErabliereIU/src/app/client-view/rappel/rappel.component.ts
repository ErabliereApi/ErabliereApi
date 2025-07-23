import { Component, Input, ViewChild } from '@angular/core';
import { Note } from 'src/model/note';
import { ModalRappelComponent } from "./modal-rappel/modal-rappel.component";

@Component({
    selector: 'app-rappel',
    styleUrls: ['./rappel.component.css'],
    templateUrl: './rappel.component.html'
})
export class RappelComponent {
    @Input() note: Note;
    @ViewChild(ModalRappelComponent, { static: false }) modalRappelComponent!: ModalRappelComponent;
    openedNote: Note | null = null;

    constructor() {
        this.note = new Note();
    }

    getExcerpt(text: string | undefined, length: number = 100): string {
        return text && text.length > length ? text.slice(0, length) + '...' : text || '';
    }
}
