import { Component, Input } from "@angular/core";

@Component({
    selector: 'copy-id-button',
    template: `
        <button type="button" class="btn btn-outline-primary btn-sm" style="width: fit-content;"
                (click)="copyText($event, text)" [title]="title">
        &#x2398;
        </button>
    `
})
export class CopyTextButtonComponent {
    @Input() text: string | undefined | null = '';
    @Input() title: string = 'Copier';

    copyText(event: MouseEvent, text?: string | null) {
        const button = event.target as HTMLButtonElement;
        
        if (!text) {
            button.innerText = "Aucun texte à copier";
            setTimeout(() => {
                const button = event.target as HTMLButtonElement;
                button.innerHTML = "&#x2398;";
            }, 750);
            return;
        }

        button.innerText = "Copié!";
        navigator.clipboard.writeText(text);
        setTimeout(() => {
            button.innerHTML = "&#x2398;"
        }, 750);
    }
}