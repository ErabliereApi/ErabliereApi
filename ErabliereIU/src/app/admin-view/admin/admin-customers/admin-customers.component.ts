import { Component, OnInit } from '@angular/core';
import { CustomerListComponent } from "./customer-list/customer-list.component";
import { ErabliereApi } from "src/core/erabliereapi.service";
import { Customer } from "src/model/customer";
import { ModifierCustomerComponent } from "./modifier-customer/modifier-customer.component";

@Component({
  selector: 'admin-customers',
  imports: [
    CustomerListComponent,
    ModifierCustomerComponent
  ],
  templateUrl: './admin-customers.component.html'
})
export class AdminCustomersComponent implements OnInit {
  customers: Customer[] = [];
  customerAModifier: Customer | null = null;

  constructor(private readonly _api: ErabliereApi) { }

  ngOnInit() {
    this.chargerCustomers();
  }

  chargerCustomers() {
    this._api.getCustomersAdminExpandAccess().then(customers => {
      this.customers = customers;
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
}
