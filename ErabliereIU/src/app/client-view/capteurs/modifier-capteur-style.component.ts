import { Component, Input, Output, EventEmitter, OnInit } from '@angular/core';
import { Capteur, CapteurStyle } from 'src/model/capteur';
import { GraphPanelComponent } from "../donnees/sub-panel/graph-panel.component";
import { FormBuilder, FormsModule, ReactiveFormsModule } from '@angular/forms';

@Component({
    selector: 'app-modifier-capteur-style',
    template: `
        <div class="row">
            <div class="col-6">
            <form [formGroup]="formGroup" class="g3 row">
                <div class="from-group col-md-4">
                    <label for="backgroundColor">Couleur de fond</label>
                    <input 
                        type="color" 
                        class="form-control form-control-color" 
                        id="backgroundColor" 
                        name="backgroundColor"
                        formControlName="backgroundColor"
                        [formControl]="formGroup.backgroundColor" 
                        (change)="updateStyle()">
                </div>
                <div class="from-group col-md-4">
                    <label for="color">Couleur du texte</label>
                    <input 
                        type="color" 
                        class="form-control form-control-color" 
                        id="color" 
                        name="color"
                        formControlName="color"
                        [formControl]="formGroup.color" 
                        (change)="updateStyle()">
                </div>
                <div class="from-group col-md-4">
                    <label for="borderColor">Couleur de la bordure</label>
                    <input 
                        type="color" 
                        name="borderColor" 
                        class="form-control form-control-color" 
                        id="borderColor" 
                        formControlName="borderColor"
                        [formControl]="formGroup.borderColor" 
                        (change)="updateStyle()">
                </div>
                <div class="form-check col-md-4">
                    <input 
                        type="checkbox" 
                        name="fill" 
                        id="fill" 
                        formControlName="fill" 
                        [formControl]="formGroup.fill" 
                        (change)="updateStyle()">
                    <label for="fill">Remplir</label>
                </div>
                <div class="from-group col-md-4">
                    <label for="pointBackgroundColor">Couleur du point</label>
                    <input 
                        type="color" 
                        name="pointBackgroundColor" 
                        class="form-control form-control-color" 
                        id="pointBackgroundColor" 
                        formControlName="pointBackgroundColor"
                        [formControl]="formGroup.pointBackgroundColor" 
                        (change)="updateStyle()">
                </div>
                <div class="from-group col-md-4">
                    <label for="pointBorderColor">Couleur de la bordure du point</label>
                    <input 
                        type="color" 
                        name="pointCoderColor" 
                        class="form-control form-control-color" 
                        id="pointBorderColor" 
                        formControlName="pointBorderColor"
                        [formControl]="formGroup.pointBorderColor" 
                        (change)="updateStyle()">
                </div>
                <div class="input-group col-md-4">
                    <label for="tension">Tension</label>
                    <input 
                        type="number" 
                        name="tension" 
                        id="tension"
                        formControlName="tension" 
                        [formControl]="formGroup.tension" 
                        (change)="updateStyle()">
                </div>
                <div class="from-group col-md-4">
                    <label for="dSetBorderColors">Couleur de la bordure du dataset</label>
                    <input 
                        type="color" 
                        name="dSetBorderColors" 
                        class="form-control form-control-color" 
                        id="dSetBorderColors"
                        formControlName="dSetBorderColors" 
                        [formControl]="formGroup.dSetBorderColors" 
                        (change)="updateStyle()">
                </div>
                <div class="input-check col-md-4">
                    <input 
                        type="checkbox" 
                        name="useGradient" 
                        id="useGradient" 
                        formControlName="useGradient"
                        [formControl]="formGroup.useGradient" 
                        (change)="updateStyle()">
                    <label for="useGradient">Utiliser un dégradé</label>
                </div>
                <div class="input-group">
                    <label for="g1Stop">Dégradé 1</label>
                    <input 
                        type="number" 
                        name="g1Stop" 
                        id="g1Stop" 
                        formControlName="g1Stop"
                        [formControl]="formGroup.g1Stop" 
                        (change)="updateStyle()">
                    <label for="g1Color">Couleur</label>
                    <input 
                        type="color" 
                        name="g1Color" 
                        class="form-control form-control-color" 
                        id="g1Color" 
                        formControlName="g1Color"
                        [formControl]="formGroup.g1Color" 
                        (change)="updateStyle()">
                </div>
                <div class="input-group">
                    <label for="g2Stop">Dégradé 2</label>
                    <input 
                        type="number" 
                        name="g2Stop" 
                        id="g2Stop" 
                        formControlName="g2Stop"
                        [formControl]="formGroup.g2Stop" 
                        (change)="updateStyle()">
                    <label for="g2Color">Couleur</label>
                    <input 
                        type="color" 
                        name="g2Color" 
                        class="form-control form-control-color" 
                        id="g2Color" 
                        formControlName="g2Color"
                        [formControl]="formGroup.g2Color" 
                        (change)="updateStyle()">
                </div>
                <div class="input-group">
                    <label for="g3Stop">Dégradé 3</label>
                    <input type="number" name="g3Stop" id="g3Stop" formControlName="g3Stop" [formControl]="formGroup.g3Stop" (change)="updateStyle()">
                    <label for="g3Color">Couleur</label>
                    <input type="color" name="g3Color" class="form-control form-control-color" id="g3Color" formControlName="g3Color" [formControl]="formGroup.g3Color" (change)="updateStyle()">
                </div>
            </form>
            </div>
            <div class="col-6">
                @if (this.inputCapteur) {
                    <graph-panel
                        [titre]="inputCapteur.nom"
                        [symbole]="inputCapteur.symbole"
                        [idCapteur]="inputCapteur.id"
                        [batteryLevel]="inputCapteur.batteryLevel"
                        [online]="inputCapteur.online"
                        [displayMin]="inputCapteur.displayMin"
                        [displayMax]="inputCapteur.displayMax"
                        [capteurStyle]="inputCapteur.capteurStyle"
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
    @Input() inputCapteur?: Capteur;
    @Output() styleUpdated = new EventEmitter<CapteurStyle>();
    formGroup: any;
    stylesInst?: CapteurStyle;

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

        if (this.inputCapteur?.capteurStyle) {
            this.stylesInst = { ...this.inputCapteur?.capteurStyle };
        }
    }

    updateStyle() {
        if (!this.formGroup) {
            this.styleUpdated.emit(this.formGroup);
        }
    }
}