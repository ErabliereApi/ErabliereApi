import { Component, EventEmitter, Input, OnInit, Output } from '@angular/core';
import { ErabliereApi } from 'src/core/erabliereapi.service';
import { Note } from 'src/model/note';
import { NgIf } from '@angular/common';
import { Subject } from 'rxjs';

@Component({
    selector: 'note',
    template: `
        <div id="note-{{ note.id }}" class="card mt-3">
            <h4 class="card-header">{{ note.title }}</h4>

            <div class="card-body">
                <p class="noteDescription card-text">{{ note.text }}</p>

                <p *ngIf="note.fileExtension == 'csv'" class="card-text">{{ note.decodedTextFile }}</p>

                <p class="noteDate card-text"><small class="text-muted">{{ note.noteDate }}</small></p>

                <div class="btn-group me-2">
                    <button class="btn btn-info btn-sm" (click)="selectEditNote()">Modifier</button>
                </div>
                <div class="btn-group">
                    <button class="btn btn-danger btn-sm" (click)="deleteNote()">Supprimer</button>
                </div>
            </div>

            <div *ngIf="note.fileExtension != 'csv'">
                <div class="container">
                    <img 
                        *ngIf="note.file != ''" 
                        class="card-img-bottom img-thumbnail rounded mx-auto d-block"
                        style="max-width: 50%;"
                        src="data:image/png;base64,{{ note.file }}" />
                </div>
            </div>
        </div>
    `,
    standalone: true,
    imports: [NgIf]
})

export class NoteComponent implements OnInit {
    
    constructor(private _api: ErabliereApi) {
        this.note = new Note();
    }

    ngOnInit() { }

    @Input() note: Note;

    @Input() noteToModifySubject?: Subject<Note | undefined>;

    selectEditNote() {
        if (this.noteToModifySubject == null) {
            console.error("noteToModifySubject is null");
            return;
        }

        this.noteToModifySubject?.next(this.note);
    }

    deleteNote() {
        this._api.deleteNote(this.note.idErabliere, this.note.id).then(
            (data) => {
                this.needToUpdate.emit();
            }
        );
    }

    @Output() needToUpdate = new EventEmitter();
}