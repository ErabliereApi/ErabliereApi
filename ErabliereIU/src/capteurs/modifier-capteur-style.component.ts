import { Component, Input, Output, EventEmitter, OnInit } from '@angular/core';
import { Capteur, CapteurStyle } from 'src/model/capteur';
import { GraphPanelComponent } from "../donnees/sub-panel/graph-panel.component";
import { FormBuilder, FormsModule, ReactiveFormsModule } from '@angular/forms';

@Component({
    selector: 'app-modifier-capteur-style',
    template: `
        <div class="row">
            <div class="col-6">
            <form [formGroup]="formGroup" class="g3">
                <div class="from-group">
                    <label for="backgroundColor">Couleur de fond</label>
                    <input 
                        type="color" 
                        class="form-control form-control-color" 
                        id="backgroundColor" 
                        [formControl]="formGroup.backgroundColor" 
                        (change)="updateStyle()">
                </div>
                <div class="from-group">
                    <label for="color">Couleur du texte</label>
                    <input 
                        type="color" 
                        class="form-control form-control-color" 
                        id="color" 
                        [formControl]="formGroup.color" 
                        (change)="updateStyle()">
                </div>
                <div class="from-group">
                    <label for="borderColor">Couleur de la bordure</label>
                    <input type="color" class="form-control form-control-color" id="borderColor" [formControl]="formGroup.borderColor" (change)="updateStyle()">
                </div>
                <div class="form-check">
                    <input type="checkbox" id="fill" [formControl]="formGroup.fill" (change)="updateStyle()">
                    <label for="fill">Remplir</label>
                </div>
                <div class="from-group">
                    <label for="pointBackgroundColor">Couleur du point</label>
                    <input type="color" class="form-control form-control-color" id="pointBackgroundColor" [formControl]="formGroup.pointBackgroundColor" (change)="updateStyle()">
                </div>
                <div class="from-group">
                    <label for="pointBorderColor">Couleur de la bordure du point</label>
                    <input type="color" class="form-control form-control-color" id="pointBorderColor" [formControl]="formGroup.pointBorderColor" (change)="updateStyle()">
                </div>
                <div class="input-group">
                    <label for="tension">Tension</label>
                    <input type="number" id="tension" [formControl]="formGroup.tension" (change)="updateStyle()">
                </div>
                <div class="from-group">
                    <label for="dSetBorderColors">Couleur de la bordure du dataset</label>
                    <input type="color" class="form-control form-control-color" id="dSetBorderColors" [formControl]="formGroup.dSetBorderColors" (change)="updateStyle()">
                </div>
                <div class="input-check">
                    <input type="checkbox" id="useGradient" [formControl]="formGroup.useGradient" (change)="updateStyle()">
                    <label for="useGradient">Utiliser un dégradé</label>
                </div>
                <div class="input-group">
                    <label for="g1Stop">Dégradé 1</label>
                    <input type="number" id="g1Stop" [formControl]="formGroup.g1Stop" (change)="updateStyle()">
                    <label for="g1Color">Couleur</label>
                    <input type="color" class="form-control form-control-color" id="g1Color" [formControl]="formGroup.g1Color" (change)="updateStyle()">
                </div>
                <div class="input-group">
                    <label for="g2Stop">Dégradé 2</label>
                    <input type="number" id="g2Stop" [formControl]="formGroup.g2Stop" (change)="updateStyle()">
                    <label for="g2Color">Couleur</label>
                    <input type="color" class="form-control form-control-color" id="g2Color" [formControl]="formGroup.g2Color" (change)="updateStyle()">
                </div>
                <div class="input-group">
                    <label for="g3Stop">Dégradé 3</label>
                    <input type="number" id="g3Stop" [formControl]="formGroup.g3Stop" (change)="updateStyle()">
                    <label for="g3Color">Couleur</label>
                    <input type="color" class="form-control form-control-color" id="g3Color" [formControl]="formGroup.g3Color" (change)="updateStyle()">
                </div>
            </form>
            </div>
            <div class="col-6">
                @if (this.capteur) {
                    <graph-panel
                        [titre]="capteur.nom"
                        [symbole]="capteur.symbole"
                        [idCapteur]="capteur.id"
                        [batteryLevel]="capteur.batteryLevel"
                        [online]="capteur.online"
                        [displayMin]="capteur.displayMin"
                        [displayMax]="capteur.displayMax"
                    />
                }
                @else {
                    <span class="text-danger">Aucun capteur sélectionné</span>
                }
            </div>
        </div>
    `,
    imports: [GraphPanelComponent, FormsModule, ReactiveFormsModule],
})
export class ModifierCapteurStyleComponent implements OnInit {
    @Input() capteur?: Capteur;
    @Output() styleUpdated = new EventEmitter<CapteurStyle>();
    formGroup: any;

    constructor(private readonly formBuilder: FormBuilder) {
        this.formGroup = this.formBuilder.group({
            backgroundColor: [''],
            color: [''],
            borderColor: [''],
            fill: [false],
            pointBackgroundColor: [''],
            pointBorderColor: [''],
            tension: [0.5],
            dSetBorderColors: [''],
            useGradient: [false],
            g1Stop: [0],
            g1Color: [''],
            g2Stop: [0.5],
            g2Color: [''],
            g3Stop: [1],
            g3Color: [''],
        });
    }

    ngOnInit(): void {
        console.log('ModiierCapteurStyleComponent.ngOnInit()');
    }

    updateStyle() {
        if (!this.formGroup) {
            this.styleUpdated.emit(this.formGroup);
        }
    }
}