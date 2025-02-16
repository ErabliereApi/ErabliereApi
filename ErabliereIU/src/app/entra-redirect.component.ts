// This component is part of @azure/msal-angular and can be imported and bootstrapped
import { Component, OnInit } from "@angular/core";
import { MsalService } from "@azure/msal-angular";

@Component({
    selector: 'entra-redirect',
    template: '',
    standalone: true
})
export class EntraRedirectComponent implements OnInit {
  
  constructor(private readonly authService: MsalService) { }
  
  ngOnInit(): void {    
      this.authService.handleRedirectObservable().subscribe();
  }
  
}