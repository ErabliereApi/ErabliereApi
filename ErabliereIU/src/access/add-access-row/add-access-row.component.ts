import {Component, EventEmitter, Input, OnInit, Output} from '@angular/core';
import {FormBuilder, FormControl, FormsModule, ReactiveFormsModule, UntypedFormGroup, Validators} from "@angular/forms";
import {SelectCustomerComponent} from "../../customer/select-customer.component";
import {CustomerAccess} from "../../model/customerAccess";
import {Customer} from "../../model/customer";

@Component({
  selector: 'tr[add-access-row]',
  standalone: true,
    imports: [
        FormsModule,
        SelectCustomerComponent,
        ReactiveFormsModule
    ],
  templateUrl: './add-access-row.component.html'
})
export class AddAccessRowComponent implements OnInit {
    @Input() idErabliere?: string;

    @Output() accesAAnnuler = new EventEmitter<CustomerAccess>();
    @Output() accesAAjout = new EventEmitter<CustomerAccess>();

    formGroup: UntypedFormGroup;

    acces: CustomerAccess = new CustomerAccess();
    customerNom?: string;

    constructor(private formBuilder: FormBuilder) {
        this.formGroup = this.formBuilder.group({
            customer: new FormControl(null,
                {
                    validators: [Validators.required],
                    updateOn: 'blur'
                }),
            lecture: new FormControl<boolean>(false),
            creation: new FormControl<boolean>(false),
            modification: new FormControl<boolean>(false),
            suppression: new FormControl<boolean>(false)
        });
    }

    ngOnInit() {
        this.acces.idErabliere = this.idErabliere;
    }

    customerSelected(customer?: Customer) {
    this.acces.idCustomer = customer?.id;
    this.customerNom = customer?.name;
    }

    signalerAjout() {
        this.formGroup.controls.customer.markAsTouched();
        if (this.formGroup.valid) {
            this.acces.access =
                (this.formGroup?.controls.lecture.value ? 1 : 0)
                +   (this.formGroup.controls.creation.value ? 2 : 0)
                +   (this.formGroup.controls.modification.value ? 4 : 0)
                +   (this.formGroup.controls.suppression.value ? 8 : 0);

            this.accesAAjout.emit(this.acces);
        }
    }

    signalerAnnulation() {
        this.accesAAnnuler.emit(this.acces)
    }
}
