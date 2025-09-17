import { Component, Input } from "@angular/core";
import { FormGroup, ReactiveFormsModule } from "@angular/forms";
import { InputErrorComponent } from "./input-error.component";


@Component({
  selector: 'etextarea',
  template: `
        <label for="{{ name }}" class="form-label">{{ arialabel ?? smartName(name) }}:</label>
        <div [formGroup]="formGroup" [class]="'input-group' + (formGroup.controls[name].touched ? ' was-validated' : '')">
          <textarea
            class="form-control"
            formControlName="{{ name }}"
            name="{{ name }}"
            placeholder="{{ placeholder }}"
            [pattern]="pattern ?? ''"
            [maxlength]="maxlength ?? null"></textarea>
            @if (symbole) {
              <div class="input-group-append">
                <span class="input-group-text">{{ symbole }}</span>
              </div>
            }
          </div>
          @if (formGroup.controls[name].errors && formGroup.controls[name].touched) {
            <div>
              @if (formGroup.controls[name].errors!.required) {
              <small class="text-danger">{{ arialabel ?? smartName(name) }} obligatoire.</small>
              }
              @if (formGroup.controls[name].errors!.maxlength) {
              <small class="text-danger">Taille maximal dépassée.</small>
              }
            </div>
            }
            @if (errorObj) {
            <input-error [errorObj]="errorObj" [controlName]="name"></input-error>
            }
        `,
  imports: [ReactiveFormsModule, InputErrorComponent]
})
export class ETextAreaComponent {
  @Input() inputId?: string
  @Input() arialabel?: string
  @Input() symbole?: string
  @Input() name: string = ""
  @Input() formGroup: FormGroup<any> | any;
  @Input() placeholder = "0.0"
  @Input() textMask?: string = "separator.6"
  @Input() decimalMarker: "." | "," | [".", ","] = ".";
  @Input() customPatterns: any
  @Input() spChar: string[] = []
  @Input() type: "text" | "number" | "date" | "checkbox" = "text"
  @Input() errorObj?: any;
  @Input() pattern?: string;
  @Input() maxlength?: number;

  constructor() {

  }

  /**
   * This method is transforming a string that is camelCase to a more human readable format.
   * For example: "temperatureMin" will be transformed to "Temperature Min"
   * @param arg0 name of the input
   */
  smartName(arg0: string): string | undefined {
    if (!arg0) {
      return undefined;
    }
    const words = arg0.replace(/([a-z])([A-Z])/g, '$1 $2').split(' ');
    return words.map(word => word.charAt(0).toUpperCase() + word.slice(1)).join(' ');
  }
}