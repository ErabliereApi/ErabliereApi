import { Component, EventEmitter, Input, OnChanges, OnInit, Output, SimpleChanges } from '@angular/core';
import { Customer } from "src/model/customer";
import { CustomerAccess } from "src/model/customerAccess";
import { Erabliere } from "src/model/erabliere";
import {
    AdminCustomerAccessListComponent
} from "src/app/admin-view/access/customer-access-list/admin-customer-access-list.component";
import { DatePipe } from '@angular/common';
import { formatDistanceToNow } from 'date-fns';
import { fr } from 'date-fns/locale';

@Component({
    selector: 'customer-list',
    templateUrl: './customer-list.component.html',
    imports: [
        AdminCustomerAccessListComponent,
        DatePipe
    ],
    styleUrl: './customer-list.component.css'
})
export class CustomerListComponent implements OnChanges, OnInit {
    @Input() customers: Customer[] = [];
    customersFiltred: Customer[] = [];
    @Output() customerASupprimer: EventEmitter<Customer> = new EventEmitter();
    @Output() customerAModifier: EventEmitter<Customer> = new EventEmitter();

    showAccess: { [id: string]: boolean } = {}

    ngOnInit(): void {
        console.log("Initializing CustomerListComponent with customers:", this.customers);
        this.customersFiltred = [...this.customers];
        console.log("Initial customers:", this.customersFiltred);
    }

    ngOnChanges(changes: SimpleChanges): void {
        if (changes['customers']) {
            console.log("Customers changed:", changes['customers'].currentValue);
            this.customersFiltred = [...changes['customers'].currentValue];
            console.log("Updated customersFiltred:", this.customersFiltred);
        }
    }

    filterCustomers(arg0: any) {
        arg0 = arg0.target.value;
        console.log("Filtering customers with:", arg0);
        if (!arg0) {
            this.customersFiltred = [...this.customers];
            console.log("No filter applied, showing all customers.");
            return;
        }
        this.customersFiltred = this.customers.filter(customer => {
            return customer.name?.toLowerCase().includes(arg0.toLowerCase()) ||
                customer.email?.toLowerCase().includes(arg0.toLowerCase()) ||
                customer.id?.toString().includes(arg0.toString());
        });
    }

    toggleAccess(id: string): void {
        this.showAccess[id] = !this.showAccess[id];
    }

    MAJAcces(acces: CustomerAccess[], erabliere: Erabliere) {
        erabliere.customerErablieres = acces;
    }

    signalerSuppression(customer: Customer) {
        this.customerASupprimer.emit(customer);
    }

    signalerModification(customer: Customer) {
        this.customerAModifier.emit(customer);
    }

    formatMessageDate(date: Date | string | undefined): string {
        if (!date) {
            return '';
        }
        return formatDistanceToNow(new Date(date), { addSuffix: true, locale: fr });
    }
}
