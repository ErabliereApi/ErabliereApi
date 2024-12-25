import { NgClass } from '@angular/common';
import { Component, Input, OnChanges, SimpleChanges } from '@angular/core';
import { ErabliereApi } from 'src/core/erabliereapi.service';
import { Capteur } from 'src/model/capteur';
import { Erabliere } from 'src/model/erabliere';
import { GraphPanelComponent } from './graph-panel.component';
import { AuthorisationFactoryService } from 'src/authorisation/authorisation-factory-service';
import { TablePanelComponent } from './table-panel.component';

@Component({
    selector: 'capteur-panels',
    templateUrl: './capteur-panels.component.html',
    imports: [GraphPanelComponent, TablePanelComponent, NgClass]
})
export class CapteurPanelsComponent implements OnChanges {
    @Input() capteurs?: Capteur[] = []
    @Input() erabliere?: Erabliere
    isLogged: boolean = false;

    public tailleGraphiques?: number = 6;

    constructor(private readonly _api: ErabliereApi, private readonly _authService: AuthorisationFactoryService) {
        if (this._authService.getAuthorisationService().type == "AuthDisabled") {
            this.isLogged = true;
        }
        else {
            this._authService.getAuthorisationService().loginChanged.subscribe(loggedIn => {
                this.isLogged = loggedIn;
            });
        }
    }

    async ngOnChanges(changes: SimpleChanges): Promise<void> {
        if (changes.capteurs) {
            let taille = this.capteurs?.find(capteur => capteur.taille)?.taille;
            if (taille) {
                this.tailleGraphiques = taille;
            } else {
                this.tailleGraphiques = 6;
            }
        }
        this.isLogged = await this._authService.getAuthorisationService().isLoggedIn();
    }

    changerDimension(taille: number) {
        this.tailleGraphiques = taille;
        if (this.capteurs) {
            for (let capteur of this.capteurs) {
                capteur.taille = taille
            }

            this._api.putCapteurs(this.erabliere?.id, this.capteurs);
        }
    }

    keyUpChangerDimension(event: KeyboardEvent, taille: number) {
        if (event.key == "Enter") {
            this.changerDimension(taille);
        }
    }
}
