import { Component, EventEmitter, Input, Output } from "@angular/core";

@Component({
    selector: 'ebutton',
    template: `
        <button class="btn btn-{{type}}" type="button" (click)="onClick()" [disabled]="disabled || inProgress" [title]="tooltip">
            <ng-content></ng-content>
            @if (inProgress) {
                <span class="spinner-border spinner-border-sm ms-2" role="status" aria-hidden="true"></span>
            }
        </button>
    `,
})
export class EButtonComponent {
    inProgress: boolean = false;
    @Input() text: string = "";
    @Input() disabled: boolean = false;
    @Input() type: "primary" | "secondary" | "success" | "danger" | "warning" | "info" | "light" | "dark" = "primary";
    @Output() clicked = new EventEmitter<void>();
    @Input() tooltip?: string;

    async onClick() {
        if (this.disabled || this.inProgress) {
            return;
        }
        this.clicked.emit();
    }
}
