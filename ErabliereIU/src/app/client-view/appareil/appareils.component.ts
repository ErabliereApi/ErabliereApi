import { Component, OnInit } from "@angular/core";
import { ActivatedRoute } from "@angular/router"
import { ErabliereApi } from "src/core/erabliereapi.service";
import { Appareil } from "src/model/appareil";
import { CopyTextButtonComponent } from "src/generic/copy-text-button.component";
import { EButtonComponent } from "src/generic/ebutton.component";

@Component({
    selector: 'appareils',
    templateUrl: './appareils.component.html',
    imports: [CopyTextButtonComponent, EButtonComponent]
})
export class AppareilsComponent implements OnInit {
    appareils?: Appareil[];
    loadingInProgress = false;
    loadingNmapResultInProgress = false;
    deleteAppareilInProgress = false;
    private erabliereId: any;

    constructor(private readonly api: ErabliereApi, private readonly route: ActivatedRoute) {

    }

    ngOnInit(): void {
        this.route.params.subscribe(params => {
            this.appareils = undefined;
            this.erabliereId = params['idErabliereSelectionee'];
            this.loadingInProgress = true;
            this.getAppareilsList();
        });
    }

    private getAppareilsList() {
        this.api.getAppareils(this.erabliereId).then(appareils => {
            this.appareils = appareils;
        }).catch(err => {
            this.appareils = undefined;
        }).finally(() => {
            this.loadingInProgress = false;
        });
    }

    importerScanNmap() {
        // Ask the user to upload a nmap result file
        const input = document.createElement('input');
        input.type = 'file';
        input.accept = '.xml';
        input.onchange = (event) => {
            const file = (event.target as HTMLInputElement).files?.[0];
            if (file) {
                this.loadingNmapResultInProgress = true;
                this.api.importerScanNmap(this.erabliereId, file).then(() => {
                    this.getAppareilsList();
                }).catch(err => {
                    console.error(err);
                }).finally(() => {
                    this.loadingNmapResultInProgress = false;
                });
            }
        };
        input.click();
    }

    supprimerTousLesAppareils() {
        if (confirm('Êtes-vous certain de vouloir supprimer tous les appareils?')) {
            this.deleteAppareilInProgress = true;
            this.api.supprimerTousLesAppareils(this.erabliereId).then(() => {
                this.appareils = [];
            }).catch(err => {
                console.error(err);
            }).finally(() => {
                this.deleteAppareilInProgress = false;
            });
        }
    }
}