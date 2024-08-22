import {Component, EventEmitter, Input, Output} from '@angular/core';
import {Customer} from "../../../model/customer";
import {CustomerAccess} from "../../../model/customerAccess";
import {Erabliere} from "../../../model/erabliere";
import {
    AdminErabliereAccessListComponent
} from "../../../access/erabliere-access-list/admin-erabliere-access-list.component";
import {
    AdminCustomerAccessListComponent
} from "../../../access/customer-access-list/admin-customer-access-list.component";
import { DatePipe } from '@angular/common';
import { formatDistanceToNow } from 'date-fns';
import { fr } from 'date-fns/locale';

@Component({
    selector: 'customer-list',
    standalone: true,
    templateUrl: './customer-list.component.html',
    imports: [
        AdminErabliereAccessListComponent,
        AdminCustomerAccessListComponent,
        DatePipe
    ],
    styleUrl: './customer-list.component.css'
})
export class CustomerListComponent {
    @Input() customers: Customer[] = [];
    @Output() customerASupprimer: EventEmitter<Customer> = new EventEmitter();
    @Output() customerAModifier: EventEmitter<Customer> = new EventEmitter();

    showAccess: { [id: string]: boolean } = {}

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
