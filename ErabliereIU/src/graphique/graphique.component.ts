import { Component, Input, OnChanges, SimpleChanges } from "@angular/core";
import { Subject } from "rxjs";
import { Erabliere } from "src/model/erabliere";

@Component({
    selector: 'graphique',
    template: `
        <div>
            <donnees-panel *ngIf="erabliere?.afficherTrioDonnees == true" 
                           [initialErabliere]="erabliere"
                           [erabliereSubject]="resetErabliere"></donnees-panel>
            <capteur-panels [capteurs]="erabliere?.capteurs"></capteur-panels>
            <barils-panel *ngIf="erabliere?.afficherSectionBaril == true" 
                          [erabliereId]="erabliere?.id"></barils-panel>
        </div>
    `
})
export class GraphiqueComponent implements OnChanges {
    
    @Input() erabliere?:Erabliere

    resetErabliere: Subject<Erabliere> = new Subject<Erabliere>();

    ngOnChanges(changes: SimpleChanges): void {
        this.resetChildForm();
    }

    resetChildForm(){
        this.resetErabliere.next(this.erabliere);
    }
}