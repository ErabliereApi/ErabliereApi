import { Component, Input } from "@angular/core";
import { FormGroup, ReactiveFormsModule } from "@angular/forms";


@Component({
    selector: 'einput',
    template: `
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
}