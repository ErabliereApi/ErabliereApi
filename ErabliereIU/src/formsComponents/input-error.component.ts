import { Component, Input } from "@angular/core";


@Component({
    selector: 'input-error',
    template: `
        @if (errorObj?.error?.errors != null && errorObj.error.errors.hasOwnProperty(this.controlName)) {
          <div class="text-danger">
            @for (error of errorObj.error.errors[this.controlName]; track error) {
              <span class="invalid-feedback">{{error}}</span>
            }
          </div>
        }
        @if (errorObj?.error?.errors != null && errorObj.error.errors.hasOwnProperty('$.' + this.controlName)) {
          <div>
            @for (error of errorObj.error.errors['$.' + this.controlName]; track error) {
              <span class="invalid-feedback">{{error}}</span>
            }
          </div>
        }
        `,
    imports: []
})
export class InputErrorComponent {
    @Input() errorObj?: any
    @Input() controlName: string = ""
}
