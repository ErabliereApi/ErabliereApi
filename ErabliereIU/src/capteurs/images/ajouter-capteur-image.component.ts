import {Component, EventEmitter, Input, Output} from '@angular/core';
import {InputErrorComponent} from "../../formsComponents/input-error.component";
import {FormControl, ReactiveFormsModule, UntypedFormBuilder, UntypedFormGroup, Validators} from "@angular/forms";
import {OnlyDigitsDirective} from "../../directives/only-digits.directive";
import {PostCapteurImage} from "../../model/postCapteurImage";
import {ErabliereApi} from "../../core/erabliereapi.service";

@Component({
  selector: 'ajouter-capteur-image',
  standalone: true,
    imports: [
        InputErrorComponent,
        ReactiveFormsModule,
        OnlyDigitsDirective
    ],
  templateUrl: './ajouter-capteur-image.component.html'
})
export class AjouterCapteurImageComponent {
    ajouterImageCapteurForm: UntypedFormGroup;
    errorObj: any;
    generalError: string;

    @Input() idErabliere!: string;
    @Output() masquer = new EventEmitter();
    @Output() rechargerCapteursImage = new EventEmitter();

    constructor(private readonly _api: ErabliereApi, private _formBuilder: UntypedFormBuilder) {
        this.generalError = '';
        this.ajouterImageCapteurForm = this._formBuilder.group({
            nom: new FormControl(
                '',
                {
                    validators: [
                        Validators.required,
                        Validators.maxLength(50)
                    ],
                    updateOn: 'blur'
                }
            ),
            url: new FormControl(
                '',
                {
                    validators: [
                        Validators.required,
                        Validators.maxLength(2000),
                        Validators.pattern(/^rtsp:\/\/[-a-zA-Z0-9@:%._+~#=]{1,256}\.[a-zA-Z0-9()]{1,6}\b([-a-zA-Z0-9()@:%_+.~#?&/=]*)$/)
                    ],
                    updateOn: 'blur'
                }
            ),
            port: new FormControl(
                '',
                {
                    validators: [
                        Validators.required,
                        Validators.maxLength(5)
                    ],
                    updateOn: "blur"
                }
            ),
            nomDUtilisateur: new FormControl(
                '',
                {
                    validators: [
                        Validators.maxLength(200)
                    ],
                    updateOn: "blur"
                }
            ),
            motDePasse: new FormControl(
                '',
                {
                    validators: [
                        Validators.maxLength(200)
                    ],
                    updateOn: "blur"
                }
            )
        })
    }
    validateForm() {
        const form = document.getElementById('ajouter-capteur-image');
        this.ajouterImageCapteurForm.updateValueAndValidity();
        form?.classList.add('was-validated');
    }
    ajouter() {
        let capteurImage: PostCapteurImage;
        this.validateForm();
        if(this.ajouterImageCapteurForm.valid) {
            capteurImage = {
                nom: this.ajouterImageCapteurForm.controls['nom'].value,
                url: this.ajouterImageCapteurForm.controls['url'].value,
                port: this.ajouterImageCapteurForm.controls['port'].value,
                identifiant: this.ajouterImageCapteurForm.controls['nomDUtilisateur'].value,
                motDePasse: this.ajouterImageCapteurForm.controls['motDePasse'].value
            }

            this._api.postCapteurImage(this.idErabliere, capteurImage).then(() => {
                this.masquer.emit();
                this.rechargerCapteursImage.emit();
            }).catch(error => {
                if (error.status == 400) {
                    this.errorObj = error;
                    this.generalError = '';
                }
                else {
                    this.generalError = "Une erreur est survenue lors de l'ajout du capteur d'images. Veuillez réessayer plus tard.";
                }
            });
        }
    }
    annuler() {
        this.masquer.emit();
    }
}
