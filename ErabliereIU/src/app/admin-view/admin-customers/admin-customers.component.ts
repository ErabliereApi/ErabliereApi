import { Component, OnInit } from '@angular/core';
import { CustomerListComponent } from "./customer-list/customer-list.component";
import { ErabliereApi } from "src/core/erabliereapi.service";
import { Customer } from "src/model/customer";
import { ModifierCustomerComponent } from "./modifier-customer/modifier-customer.component";
import { PaginationComponent } from 'src/generic/pagination/pagination.component';

@Component({
  selector: 'admin-customers',
  imports: [
    CustomerListComponent,
    ModifierCustomerComponent,
    PaginationComponent
  ],
  templateUrl: './admin-customers.component.html'
})
export class AdminCustomersComponent implements OnInit {
  customers: Customer[] = [];
  customerAModifier: Customer | null = null;
  page: number = 1;
  totalItems: number = 0;
  pageSize: number = 10;

  constructor(private readonly _api: ErabliereApi) { }

  ngOnInit() {
    this.chargerCustomers();
  }

  chargerCustomers() {
    this._api.getCustomersAdminExpandAccess(this.page, this.pageSize).then(customers => {
      this.customers = customers.items;
      this.totalItems = customers.count ?? 0;
    }).catch(error => {
      this.customers = [];
      throw error;
    });
  }

  supprimerCustomer(customer: Customer) {
    if (confirm("Voulez-vous vraiment supprimer le compte de " + customer.name + " ? ")) {
      this._api.deleteCustomer(customer.id)
        .then(a => {
          this.chargerCustomers();
        });
    }
  }

  demarrerModifierCustomer(customer: Customer) {
    this.customerAModifier = customer;
  }

  terminerModiferCustomer(update: boolean) {
    this.customerAModifier = null;
    if (update) {
      this.chargerCustomers();
    }
  }

  onPageChange($event: number) {
    this.page = $event;
    this.chargerCustomers();
  }
}
