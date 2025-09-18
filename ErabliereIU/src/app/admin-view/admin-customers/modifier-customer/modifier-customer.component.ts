import { Component, EventEmitter, Input, Output, OnInit } from '@angular/core';
import { Customer } from "src/model/customer";
import { ErabliereApi } from "src/core/erabliereapi.service";
import { FormControl, ReactiveFormsModule, UntypedFormBuilder, UntypedFormGroup, Validators } from "@angular/forms";
import { InputErrorComponent } from 'src/generic/input-error.component';

@Component({
    selector: 'modifier-customer-modal',
    imports: [
        InputErrorComponent,
        ReactiveFormsModule
    ],
    templateUrl: './modifier-customer.component.html'
})
export class ModifierCustomerComponent implements OnInit {

    @Input() customer: Customer | null = null;
    @Output() needToUpdate: EventEmitter<boolean> = new EventEmitter();

    customerForm: UntypedFormGroup;
    errorObj?: any;
    generalError?: string | null;

    constructor(private readonly _api: ErabliereApi, private readonly fb: UntypedFormBuilder) {
        this.customerForm = this.fb.group({});
    }

    ngOnInit(): void {
        this.initializeForm();
    }

    initializeForm() {
        this.customerForm = this.fb.group({
            nom: new FormControl(
                this.customer?.name,
                {
                    validators: [Validators.required],
                    updateOn: 'blur'
                }),
            email: new FormControl(
                {
                    value: this.customer?.email,
                    disabled: true
                })
        });
    }

    validateForm() {
        const form = document.getElementById("modifier-customer");
        this.customerForm.updateValueAndValidity();
        form?.classList.add('was-validated');
    }

    onModifier() {
        if (this.customer) {
            this.validateForm();

            if (this.customerForm.valid)
                this.customer.name = this.customerForm.controls['nom'].value;

            this._api.putCustomer(this.customer.id, this.customer)
                .then(r => {
                    this.errorObj = null;
                    this.generalError = null;
                    this.customerForm.reset();
                    this.needToUpdate.emit(true);
                })
                .catch(e => {
                    if (e.status == 400) {
                        this.errorObj = e;
                        this.generalError = "Le nom ne doit pas être vide";
                    } else if (e.status == 404) {
                        this.errorObj = null;
                        this.generalError = "L'utilisateur n'existe pas."
                    } else {
                        this.errorObj = null;
                        this.generalError = "Une erreur est survenue."
                    }
                });
        }
    }

    onAnnuler() {
        this.needToUpdate.emit(false);
    }

    acceptTerms(deviceUniqueName: any) {
        this._api.adminAcceptTermsForDevices(deviceUniqueName)
            .then(() => {
                this.errorObj = null;
                this.generalError = null;
                this.customerForm.reset();
                this.needToUpdate.emit(true);
            })
            .catch(e => {
                if (e.status == 404) {
                    this.generalError = "L'utilisateur n'existe pas."
                } else {
                    this.generalError = "Une erreur est survenue."
                }
            });
    }

    isUUID(arg0: any) {
        const regex = /^[0-9a-f]{8}-[0-9a-f]{4}-[0-9a-f]{4}-[0-9a-f]{4}-[0-9a-f]{12}$/i;
        return typeof arg0 === 'string' && arg0.length == 36 && regex.test(arg0);
    }
}
