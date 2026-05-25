import { Component, OnInit } from '@angular/core';
import { ErabliereApi } from 'src/core/erabliereapi.service';
import { ChirpstackSrvConfigTableComponent } from './chirpstacksrvconfig-list/chirpstacksrvconfig-table.comonent';
import { EButtonComponent } from "src/generic/ebutton.component";
import { EModalComponent } from "src/generic/modal/emodal.component";
import { GenericFormComponent } from "src/generic/forms/generic-form.component";
import { FormFieldConfig } from "src/model/form-field-config"
import { ChirpstackSrvConfig } from 'src/model/chripstacksrvconfig';

@Component({
    selector: 'admin-chirpstacksrvconfig',
    templateUrl: './admin-chirpstacksrvconfig.component.html',
    imports: [
        ChirpstackSrvConfigTableComponent, 
        EButtonComponent, 
        EModalComponent, 
        GenericFormComponent
    ]
})
export class AdminChirpstackSrvConfigComponent implements OnInit {
    configs: ChirpstackSrvConfig[] = [];
    displayAddModal: boolean = false;
    addNewFormConfig: FormFieldConfig[] = [

    ]

    constructor(private readonly api: ErabliereApi) { }

    ngOnInit(): void {
        this.getChirpstackSrvConfig();
    }

    getChirpstackSrvConfig(): void {
        this.api.getChirpstackSrvConfig().then((data) => {
            this.configs = data;
        });
    }

    deleteChirpstackSrvConfig($event: any) {
        confirm('Êtes vous sur de vouloir supprimer le serveur chirpstack Id: ' + $event.id + " ?") &&
        this.api.deleteChirpstackSrvConfig($event.id).then(() => {
            this.getChirpstackSrvConfig();
        });
    }

    displayAddChirpstackSrcModal(arg0: boolean) {
        this.displayAddModal = arg0;
    }
}
