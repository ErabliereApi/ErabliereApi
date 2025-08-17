import { Component, EventEmitter, Input, Output } from "@angular/core";
import { EButtonComponent } from "../ebutton.component";

@Component({
    selector: 'emodal',
    templateUrl: './emodal.component.html',
    imports: [
        EButtonComponent
    ]
})
export class EModalComponent {
    @Input() title: string = "";
    @Output() closeModal = new EventEmitter<boolean>();
    @Input() error?: string;

    closeModalFn() {
        this.closeModal.emit(true);
    }
}