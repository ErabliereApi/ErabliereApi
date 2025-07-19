import {
    Component,
    EventEmitter,
    Input,
    OnInit,
    Output,
} from '@angular/core';
import { ErabliereApi } from 'src/core/erabliereapi.service';
import { Note } from 'src/model/note';

@Component({
    selector: 'app-modal-rappel',
    imports: [],
    templateUrl: './modal-rappel.component.html'
})
export class ModalRappelComponent implements OnInit {
    @Input() note: Note = new Note();
    error: string | null = null;
    images: any;
    @Output() closeModal = new EventEmitter<boolean>();
    @Output() needToUpdate = new EventEmitter<boolean>();

    constructor(private readonly _api: ErabliereApi) {

    }

    ngOnInit(): void {
        if (!this.note) {
            console.error('Note is not provided to ModalRappelComponent');
            return;
        }
        console.log('ModalRappelComponent initialized with note:', this.note);
        this.getImages();
    }

    getImages(): void {
        if (this.note?.id && this.note.idErabliere && this.note.fileExtension) {
            this._api.getNoteImage(this.note.idErabliere, this.note.id).then(response => {
                this.error = null;
                if (response) {
                    this.note.file = this.arrayBufferToBase64(response);
                } else {
                    this.error = 'Image de la note non trouvée.';
                }
            }).catch(error => {
                console.error('Error fetching note image:', error);
                this.error = 'Error lors de la récupération de l\'image de la note.';
            });
        }
        else {
            console.warn('Note ID or Erabliere ID or file extension is missing:', this.note);
        }
    }

    arrayBufferToBase64(response: ArrayBuffer): string {
        if (!response) {
            return '';
        }
        const bytes = new Uint8Array(response);
        let binary = '';
        for (let i = 0; i < bytes.byteLength; i++) {
            binary += String.fromCharCode(bytes[i]);
        }
        return 'data:image/' + this.note.fileExtension + ';base64,' + btoa(binary);
    }

    reportRappelProchainePeriode() {
        this._api.reportRappelProchainePeriode(this.note.idErabliere, this.note.id).then(() => {
            this.error = null;
            console.log('Periodicite due updated successfully');
            this.closeModal.emit(true);
            this.needToUpdate.emit(true);
        }).catch(error => {
            console.error('Error updating periodicite due:', error);
            this.error = 'Erreur lors du repport à la prochaine période.';
        });
    }
}
