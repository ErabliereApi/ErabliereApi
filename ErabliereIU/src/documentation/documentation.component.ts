import { Component, EventEmitter, Input, OnInit, Output } from '@angular/core'
import { AuthorisationFactoryService } from 'src/authorisation/authorisation-factory-service';
import { IAuthorisationSerivce } from 'src/authorisation/iauthorisation-service';
import { ErabliereApi } from 'src/core/erabliereapi.service';
import { EnvironmentService } from 'src/environments/environment.service';
import { Documentation } from 'src/model/documentation';
import { ErabliereApiDocument } from 'src/model/erabliereApiDocument';

@Component({
    selector: 'documentation',
    templateUrl: "./documentation.component.html"
})
export class DocumentationComponent implements OnInit {
    @Input() idErabliereSelectionee:any
    imgTypes: Array<string> = ['png', 'jpg', 'jpeg', 'gif', 'bmp']

    @Input() documentations?: Documentation[];

    @Output() needToUpdate = new EventEmitter();
    authService: IAuthorisationSerivce;
    
    constructor (private _api: ErabliereApi, 
        private _env: EnvironmentService,
        authFactoryService: AuthorisationFactoryService) {
        this.authService = authFactoryService.getAuthorisationService();
    }

    ngOnInit(): void {
        
    }

    updateDocuments() {
        this.needToUpdate.emit();
    }

    isImageType(_t11: Documentation): boolean {
        return this.imgTypes.find(t => t == _t11.fileExtension) != null;
    }

    getApiRoot() {
        return this._env.apiUrl;
    }

    /**
     * Download a file from the server and add the bearer token to the request
     * 
     * @param id 
     */
    async downloadFile(doc: ErabliereApiDocument) {
        // Get the file from the server from base64 string
        let file = await this._api.getDocumentationBase64(doc.idErabliere, doc.id);

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

        link.download = doc.title + '.' + doc.fileExtension;

        // Append the link to the body
        document.body.appendChild(link);

        // Dispatch click event to download
        link.click();

        // Remove the link from the body
        document.body.removeChild(link);

        // Remove the blob from memory
        window.URL.revokeObjectURL(link.href);
    }

    async deleteDocumentation(document: ErabliereApiDocument) {
        await this._api.deleteDocumentation(document.idErabliere, document.id);

        this.needToUpdate.emit();
    }
}