import { NgIf } from '@angular/common';
import { Component, Input } from '@angular/core';
import { ErabliereApi } from 'src/core/erabliereapi.service';
import { Documentation } from 'src/model/documentation';

@Component({
    selector: 'app-download-button',
    template: `
        <button (click)="downloadFile()" [disabled]="isDisabled" class="btn btn-secondary btn-sm">
            {{label}}
            <span *ngIf="isLoading" class="spinner-border spinner-border-sm" role="status" aria-hidden="true"></span>
        </button>
    `,
    imports: [NgIf]
})
export class DownloadButtonComponent {
    @Input() label: string = 'Télécharger';
    @Input() isDisabled: boolean = false;
    @Input() isLoading: boolean = false;
    @Input() documentation: Documentation | undefined;

    constructor(private readonly _api: ErabliereApi) {}

    /**
     * Download a file from the server and add the bearer token to the request
     *
     * @param doc The document to download
     */
    async downloadFile() {
        if (this.documentation === undefined) {
            return;
        }

        this.isLoading = true;
        this.isDisabled = true;

        // Get the file from the server from base64 string
        let file = await this._api.getDocumentationBase64(this.documentation.idErabliere, this.documentation.id);

        // Create a blob from the base64 string
        let byteCharacters = atob(file[0].file ?? "");
        const byteNumbers = new Array(byteCharacters.length);
        for (let i = 0; i < byteCharacters.length; i++) {
            byteNumbers[i] = byteCharacters.charCodeAt(i);
        }
        const byteArray = new Uint8Array(byteNumbers);
        const blob = new Blob([byteArray]);

        // Create a link to download the file
        let link = document.createElement('a');

        link.href = window.URL.createObjectURL(blob);

        link.download = this.documentation.title + '.' + this.documentation.fileExtension;

        // Append the link to the body
        document.body.appendChild(link);

        // Dispatch click event to download
        link.click();

        // Remove the link from the body
        document.body.removeChild(link);

        // Remove the blob from memory
        window.URL.revokeObjectURL(link.href);

        this.isLoading = false;
        this.isDisabled = false;
    }
}