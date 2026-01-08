import { Component, EventEmitter, Output, Input } from "@angular/core";

@Component({
    selector: 'ebutton',
    template: `
        <button 
            id="{{buttonId}}" 
            class="btn btn-{{type}} btn-{{size}}" 
            type="button" 
            (click)="onClick()" 
            [disabled]="disabled || inProgress" 
            [title]="tooltip ?? ''">
            <ng-content></ng-content>
            @if (inProgress) {
                <span class="spinner-border spinner-border-sm ms-2" role="status" aria-hidden="true"></span>
            }
        </button>
    `,
})
export class EButtonComponent {
    @Input() buttonId: string = "";
    @Input() inProgress: boolean = false;
    @Input() text: string = "";
    @Input() disabled: boolean = false;
    @Input() type: "primary" | "outline-primary" | "secondary" | "outline-secondary" | "success" | "danger" | "warning" | "info" | "outline-info" | "outline-warning" | "outline-success" | "light" | "dark" = "primary";
    @Input() size: "sm" | "md" | "lg" = "md";
    @Output() clicked = new EventEmitter<void>();
    @Input() tooltip?: string;

    async onClick() {
        if (this.disabled || this.inProgress) {
            return;
        }
        this.clicked.emit();
    }
}
