import { Component, EventEmitter, Input, OnInit, Output } from '@angular/core'
import { ErabliereApi } from 'src/core/erabliereapi.service';
import { EnvironmentService } from 'src/environments/environment.service';
import { Documentation } from 'src/model/documentation';
import { ErabliereApiDocument } from 'src/model/erabliereApiDocument';

import { AjouterDocumentationComponent } from './ajouter-documentation.component';
import { ModifierDocumentationComponent } from './modifier-documentation.component';
import { Subject } from 'rxjs';
import { ActivatedRoute } from '@angular/router';
import { PaginationComponent } from "src/generic/pagination/pagination.component";
import { DownloadButtonComponent } from "./download-button.component";

@Component({
    selector: 'documentation',
    templateUrl: "./documentation.component.html",
    imports: [
    AjouterDocumentationComponent,
    ModifierDocumentationComponent,
    PaginationComponent,
    DownloadButtonComponent
]
})
export class DocumentationComponent implements OnInit {
    @Input() idErabliereSelectionee:any
    imgTypes: Array<string> = ['png', 'jpg', 'jpeg', 'gif', 'bmp'];

    @Input() documentations?: Documentation[];
    private _nombreParPage: number = 5;
    get nombreParPage() {
        return this._nombreParPage;
    }
    private _nombreTotal: number = 0;
    get nombreTotal() {
        return this._nombreTotal;
    }
    set nombreParPage(value: number) {
        if(value != this._nombreParPage) {
            this._nombreParPage = value;
            this.loadDocumentations();
        }
    }
    private _pageActuelle: number = 1;
    set pageActuelle(value: number) {
        if(value !== this._pageActuelle) {
            this._pageActuelle = value;
            this.loadDocumentations();
        }
    }

    @Output() needToUpdate = new EventEmitter();

    editDocumentationSubject = new Subject<ErabliereApiDocument | undefined>();

    constructor (private readonly _api: ErabliereApi,
        private readonly _env: EnvironmentService,
        private readonly route: ActivatedRoute) {
    }

    ngOnInit(): void {

        this.route.paramMap.subscribe(params => {
            this.idErabliereSelectionee = params.get('idErabliereSelectionee');
            if (this.idErabliereSelectionee) {
                this._api.getDocumentationCount(this.idErabliereSelectionee).then(count => this._nombreTotal = count);
                this.loadDocumentations();
            }
        });
        this.loadDocumentations();
    }

    loadDocumentations() {
      this._api.getDocumentations(this.idErabliereSelectionee, (this._pageActuelle - 1) * this._nombreParPage, this._nombreParPage).then(documentations => {
          this.documentations = documentations;
        });
    }

    isImageType(_t11: Documentation): boolean {
        return this.imgTypes.find(t => t == _t11.fileExtension) != null;
    }

    getApiRoot() {
        return this._env.apiUrl;
    }

    modifierDocumentation(documentation: Documentation) {
        this.editDocumentationSubject.next(documentation);
    }

    async deleteDocumentation(document: ErabliereApiDocument) {
        if (confirm("Voulez-vous vraiment supprimer ce document?")) {
            await this._api.deleteDocumentation(document.idErabliere, document.id);

            this.loadDocumentations();
        }
    }

  protected readonly console = console;
}
