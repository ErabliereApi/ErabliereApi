import { Component, Input } from "@angular/core";
import { FormGroup, ReactiveFormsModule } from "@angular/forms";


@Component({
    selector: 'einput',
    template: `
        <label for="nom" class="form-label">{{ arialabel ?? smartName(name) }}:</label>
        <div [formGroup]="formGroup" class="input-group">
          <input
            class="form-control"
            type="{{ type }}"
            formControlName="{{ name }}"
            name="{{ name }}"
            placeholder="{{ placeholder }}">
            @if (symbole) {
              <div class="input-group-append">
                <span class="input-group-text">{{ symbole }}</span>
              </div>
            }
          </div>
        `,
    imports: [ReactiveFormsModule]
})
export class EinputComponent {
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

    constructor() {

    }

    /**
     * This method is transforming a string that is camelCase to a more human readable format.
     * For example: "temperatureMin" will be transformed to "Temperature Min"
     * @param arg0 name of the input
     */
    smartName(arg0: string): string|undefined {
        if (!arg0) {
            return undefined;
        }
        const words = arg0.replace(/([a-z])([A-Z])/g, '$1 $2').split(' ');
        return words.map(word => word.charAt(0).toUpperCase() + word.slice(1)).join(' ');
    }
}