import { Component, EventEmitter, Input, Output } from '@angular/core';
import { ErabliereApi } from 'src/core/erabliereapi.service';
import { Note } from 'src/model/note';
import { DatePipe } from '@angular/common';
import { Subject } from 'rxjs';

@Component({
    selector: 'note',
    templateUrl: 'note.component.html',
    imports: [DatePipe]
})

export class NoteComponent {
    @Input() note: Note;
    @Input() noteToModifySubject?: Subject<Note | null>;
    @Output() needToUpdate = new EventEmitter();
    actionError?: string | null;

    constructor(private readonly _api: ErabliereApi) {
        this.note = new Note();
    }

    selectEditNote() {
        if (this.noteToModifySubject === null) {
            console.error("noteToModifySubject is null");
            return;
        }

        this.noteToModifySubject?.next(this.note);
    }

    modifierImage() {
        // Crée un input file invisible pour sélectionner une image
        const input = document.createElement('input');
        input.type = 'file';
        input.accept = 'image/*';
        input.style.display = 'none';
        input.onchange = async (event: any) => {
            const file = event.target.files[0];
            if (!file) return;
            // Appel à l'API pour uploader l'image
            try {
                // Extraire le blob de l'image dans la balise input
                const blob = input.files != null ? input.files[0] : new Blob([]);
                const file = new File([blob], `note-${this.note.id}.png`, { type: 'image/png' });
                await this._api.putNoteImage(this.note.idErabliere, this.note.id, file);
                this.needToUpdate.emit();
            } catch (error) {
                console.error('Erreur lors de l\'upload de l\'image:', error);
                this.actionError = 'Erreur lors de l\'upload de l\'image. Veuillez réessayer.';
            }
        };
        document.body.appendChild(input);
        input.click();
        document.body.removeChild(input);
    }

    isGeneratingImage = false;
    genererUneImageParIA() {
        this.isGeneratingImage = true;
        this._api.ErabliereIAImage({
            imageCount: 1,
            prompt: `Générer une image pour la note: ${this.note.title || 'Sans titre'} avec la description: ${this.note.text || 'Aucune description'}`,
            size: '1024x1024'
        }).then(response => {
            let imageUrl = response.images[0].url;
            fetch(imageUrl)
                .then(res => res.blob())
                .then(blob => {
                    const file = new File([blob], `note-${this.note.id}.png`, { type: 'image/png' });
                    this._api.putNoteImage(this.note.idErabliere, this.note.id, file)
                        .then(() => {
                            this.needToUpdate.emit();
                            this.actionError = null;
                        })
                        .catch(err => {
                            console.error('Erreur lors de l\'upload de l\'image générée:', err);
                            this.actionError = 'Erreur lors de l\'upload de l\'image générée. Veuillez réessayer.';
                        });
                }).catch(err => {
                    console.error('Erreur lors de la récupération de l\'image générée:', err);
                    this.actionError = 'Erreur lors de la récupération de l\'image générée. Veuillez réessayer.';
                    this.isGeneratingImage = false
                }).finally(() => {
                    this.isGeneratingImage = false;
                });
        }).catch(err => {
            console.error('Erreur lors de la génération de l\'image par IA:', err);
            this.actionError = 'Erreur lors de la génération de l\'image par IA. Veuillez réessayer.';
            this.isGeneratingImage = false;
        }).finally(() => {
            this.isGeneratingImage = false;
        });
    }

    deleteNote() {
        if (confirm("Êtes-vous sûr de vouloir supprimer la note " + this.note.title + " ?")) {
            this._api.deleteNote(this.note.idErabliere, this.note.id).then(
                (data) => {
                    this.needToUpdate.emit();
                }
            );
        }
    }

    userIsInRole(arg0: string) {
        return true;
    }
}
