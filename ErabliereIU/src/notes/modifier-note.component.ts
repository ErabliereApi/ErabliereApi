import {Component, EventEmitter, Input, OnInit, Output} from "@angular/core";
import {ErabliereApi} from "src/core/erabliereapi.service";
import {UntypedFormGroup, UntypedFormBuilder, FormControl, Validators, ReactiveFormsModule} from "@angular/forms";
import {Note} from "src/model/note";
import {InputErrorComponent} from "../formsComponents/input-error.component";
import {Subject} from "rxjs";
import {Rappel} from "../model/Rappel";
import {reminderValidator} from "./note.custom-validators";

@Component({
    selector: 'modifier-note',
    templateUrl: 'modifier-note.component.html',
    imports: [ReactiveFormsModule, InputErrorComponent]
})
export class ModifierNoteComponent implements OnInit {
    @Input() noteSubject?: Subject<Note | null>;
    @Input() idErabliereSelectionee: any
    @Output() needToUpdate = new EventEmitter();

    note?: Note | null;
    noteForm: UntypedFormGroup;

    error: string | null = null;
    errorObj: any;
    fileToLargeErrorMessage?: string | null;
    generalError?: string | null;

    constructor(private _api: ErabliereApi, private fb: UntypedFormBuilder) {
        this.noteForm = this.fb.group({});
    }

    ngOnInit(): void {
        this.noteSubject?.subscribe(note => {
            this.initializeForm();
            if (note) {
                this.note = {...note};
                if (this.note) {
                    this.noteForm.controls['title'].setValue(this.note.title);
                    this.noteForm.controls['text'].setValue(this.note.text);
                    this.noteForm.controls['noteDate'].setValue(this.note.noteDate);
                    if (this.note.rappel) {
                        this.noteForm.controls['isEditMode'].setValue(true);
                        this.noteForm.controls['dateRappel'].setValue(this.note.rappel.dateRappel ? new Date(this.note.rappel.dateRappel).toISOString().split('T')[0] : '');
                        this.noteForm.controls['dateRappelFin'].setValue(this.note.rappel.dateRappelFin ? new Date(this.note.rappel.dateRappelFin).toISOString().split('T')[0] : '');
                        this.noteForm.controls['periodicite'].setValue(this.note.rappel.periodicite);
                        this.noteForm.controls['isActive'].setValue(this.note.rappel.isActive);
                    }
                }
            }
        });
    }

    initializeForm() {
        this.noteForm = this.fb.group({
            title: new FormControl(
                '',
                {
                    validators: [Validators.required, Validators.maxLength(200)],
                    updateOn: 'blur'
                }),
            text: new FormControl(
                '',
                {
                    validators: [Validators.maxLength(2000)],
                    updateOn: 'blur'
                }),
            file: new FormControl(
                '',
                {
                    updateOn: 'blur'
                }
            ),
            fileBase64: new FormControl(
                '',
                {
                    updateOn: 'blur'
                }
            ),
            noteDate: new FormControl(
                '',
                {
                    updateOn: 'blur'
                }
            ),
            reminderEnabled: new FormControl(
                false
            ),
            dateRappel: new FormControl(
                '',
                {
                    updateOn: 'blur'
                }
            ),
            dateRappelFin: new FormControl(
                '',
                {
                    updateOn: 'blur'
                }
            ),
            periodicite: new FormControl(
                '',
                {
                    updateOn: 'blur'
                }
            ),
            isActive: new FormControl(
                false,
                {
                    updateOn: 'blur'
                }
            ),
            isEditMode: new FormControl(false)
        }, {validators: reminderValidator});
    }

    get displayReminder(): boolean {
        return this.noteForm.controls['reminderEnabled'].value;
    }

    validateForm() {
        const form = document.getElementById('modifier-note');
        this.noteForm.updateValueAndValidity();
        form?.classList.add('was-validated');
    }

    toggleActiveStatus() {
        this.noteForm.controls['isActive'].setValue(!this.noteForm.controls['isActive'].value);
    }

    onButtonAnnuleClick() {
        this.note = null;
    }

    onButtonModifierClick() {
        if (this.note) {
            this.validateForm();
            if (this.noteForm.valid) {
                this.note.title = this.noteForm.controls['title'].value;
                this.note.text = this.noteForm.controls['text'].value;
                if (this.noteForm.controls['noteDate'].value != "") {
                    this.note.noteDate = this.noteForm.controls['noteDate'].value;
                } else {
                    this.note.noteDate = null;
                }
                // Update the Rappel object and set its properties using the form values
                if (!this.note.rappel) {
                    this.note.rappel = new Rappel();
                }
                this.note.rappel.dateRappel = this.noteForm.controls['dateRappel'].value;
                this.note.rappel.dateRappelFin = this.noteForm.controls['dateRappelFin'].value;
                if (this.noteForm.controls['periodicite'].value === 'Aucune') {
                    this.note.rappel.periodicite = null;
                } else {
                    this.note.rappel.periodicite = this.noteForm.controls['periodicite'].value;
                }
                this.note.rappel.isActive = !!this.noteForm.controls['isActive'].value;

                this._api.putNote(this.idErabliereSelectionee, this.note)
                    .then(r => {
                        this.errorObj = null;
                        this.fileToLargeErrorMessage = null;
                        this.generalError = null;
                        this.noteForm.reset();
                        this.needToUpdate.emit();
                        this.noteSubject?.next(null);
                        this.note = null;
                    })
                    .catch(e => {
                        if (e.status == 400) {
                            this.errorObj = e
                            this.fileToLargeErrorMessage = null;
                            this.generalError = null;
                        } else if (e.status == 404) {
                            this.errorObj = null;
                            this.fileToLargeErrorMessage = null;
                            this.generalError = "L'érablière n'existe pas."
                        } else if (e.status == 405) {
                            this.errorObj = null;
                            this.fileToLargeErrorMessage = null;
                            this.generalError = "L'API ne permet pas de modifier une note."
                        } else if (e.status == 413) {
                            this.errorObj = null;
                            this.fileToLargeErrorMessage = "Le fichier est trop gros."
                            this.generalError = null;
                        } else {
                            this.errorObj = null;
                            this.fileToLargeErrorMessage = null;
                            this.generalError = "Une erreur est survenue."
                        }
                    });
            }
        } else {
            console.log("this.note is null");
        }
    }

    convertToBase64(event: any) {
        const file = event.target.files[0];
        const reader = new FileReader();
        reader.readAsDataURL(file);
        reader.onload = () => {
            this.noteForm.controls['fileBase64'].setValue(reader.result?.toString().split(',')[1]);
        };
    }
}
