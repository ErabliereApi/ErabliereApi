import { Component, OnInit } from "@angular/core";
import { ActivatedRoute } from "@angular/router"
import { ErabliereApi } from "src/core/erabliereapi.service";
import { Appareil } from "src/model/appareil";
import { CopyTextButtonComponent } from "src/generic/copy-text-button.component";

@Component({
  selector: 'appareils',
  templateUrl: './appareils.component.html',
  imports: [CopyTextButtonComponent]
})
export class AppareilsComponent implements OnInit {
    appareils: Appareil[] = [];
    private erabliereId: any;

    constructor(private readonly api: ErabliereApi, private readonly route: ActivatedRoute) {

    }

    ngOnInit(): void {
        this.route.params.subscribe(params => {
            this.erabliereId = params['idErabliereSelectionee'];
            this.api.getAppareils(this.erabliereId).then(appareils => {
                this.appareils = appareils;
            });
        });
    }
}