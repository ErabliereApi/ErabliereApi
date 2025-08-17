import { Component, Input } from "@angular/core";

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
    @Input() click?: () => void | Promise<void>;
    @Input() tooltip?: string;

    async onClick() {
        this.inProgress = true;

        if (typeof this.click === "function") {
            console.log("Button clicked is function");
            this.click();
        } else if (this.click) {
            console.log("Button clicked is promise");
            const clickPromise = this.click as Promise<void>;

            if (!clickPromise) {
                console.log("Button clicked is not a promise");
            }

            clickPromise.finally(() => {
                this.inProgress = false;
            });

            await clickPromise;
        }
    }
}
