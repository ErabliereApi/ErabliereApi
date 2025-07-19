import {
    Component,
    ElementRef,
    Input,
    ViewChild,
} from '@angular/core';
import { ErabliereApi } from 'src/core/erabliereapi.service';
import { Note } from 'src/model/note';

@Component({
    selector: 'app-modal-rappel',
    imports: [],
    templateUrl: './modal-rappel.component.html'
})
export class ModalRappelComponent {
    @Input() note: Note;
    @ViewChild('modalRappel') modalRappel!: ElementRef;
    @Input() index!: number;
    error: string | null = null;

    constructor(private readonly _api: ErabliereApi) {
        this.note = new Note();
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
        throw new Error('Method not implemented.');
    }
}
