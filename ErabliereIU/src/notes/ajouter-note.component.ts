import { Component, EventEmitter, Input, OnInit, Output } from "@angular/core";
import { ErabliereApi } from "src/core/erabliereapi.service";
import {
  UntypedFormGroup,
  UntypedFormBuilder,
  FormControl,
  Validators,
  ReactiveFormsModule
} from "@angular/forms";
import { Note } from "src/model/note";
import { InputErrorComponent } from "../generic/input-error.component";
import { Rappel } from "../model/Rappel";
import { reminderValidator } from "./note.custom-validators";
import { EinputComponent } from "src/generic/einput.component";

@Component({
    selector: 'ajouter-note',
    templateUrl: 'ajouter-note.component.html',
    imports: [ReactiveFormsModule, InputErrorComponent, EinputComponent]
})
export class AjouterNoteComponent implements OnInit {
  @Input() notes?: Note[];
  @Input() idErabliereSelectionee?: string

  @Output() needToUpdate = new EventEmitter();

  display: boolean = false;

  note: Note = new Note();
  noteForm: UntypedFormGroup;

  error: string | null = null;
  errorObj: any;
  fileToLargeErrorMessage?: string | null;
  generalError?: string | null;
  uploadInProgress: boolean = false
  totalSize?: number = 0;

  constructor(private readonly _api: ErabliereApi, private readonly fb: UntypedFormBuilder) {
    this.noteForm = this.fb.group({});
  }

  ngOnInit(): void {
    this.initializeForm();
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
          updateOn: 'blur',
        }
      ),
      dateRappelFin: new FormControl(
        '',
        {
          updateOn: 'blur'
        }
      ),
      periodicite: new FormControl(
        'Aucune',
        {
          updateOn: 'blur'
        }
      ),
    }, { validators: reminderValidator });
  }

  get displayReminder(): boolean {
    return this.noteForm.controls['reminderEnabled'].value;
  }

  onButtonAjouterClick() {
    this.display = true;
  }

  onButtonAnnuleClick() {
    this.display = false;
  }

  onButtonCreerClick() {
    if (this.note == null) {
      console.log("this.note is undefined");
      return;
    }

    if (!this.noteForm.valid) {
      this.validateForm();
      console.log(this.noteForm.errors);
      return;
    }

    this.note.idErabliere = this.idErabliereSelectionee;
    this.note.title = this.noteForm.controls['title'].value;
    this.note.text = this.noteForm.controls['text'].value;
    this.note.file = this.noteForm.controls['fileBase64'].value;
    if (this.noteForm.controls['noteDate'].value !== "") {
      this.note.noteDate = this.noteForm.controls['noteDate'].value;
    }
    else {
      this.note.noteDate = null;
    }
    if (this.noteForm.controls['reminderEnabled'].value && this.noteForm.controls['dateRappel'].value) {
      this.note.rappel = new Rappel();
      let date = new Date(this.noteForm.controls['dateRappel'].value);
      this.note.rappel.dateRappel = date.toISOString();

      if (this.noteForm.controls['dateRappelFin'].value) {
        let dateFin = new Date(this.noteForm.controls['dateRappelFin'].value);
        this.note.rappel.dateRappelFin = dateFin.toISOString();
      }
      else {
        this.note.rappel.dateRappelFin = null;
      }

      if (this.noteForm.controls['periodicite'].value === 'Aucune') {
        this.note.rappel.periodicite = null;
      } else {
        this.note.rappel.periodicite = this.noteForm.controls['periodicite'].value;
      }
    }

    this.uploadInProgress = true;

    this._api.postNote(this.idErabliereSelectionee, this.note)
      .then(r => {
        this.errorObj = null;
        this.fileToLargeErrorMessage = null;
        this.generalError = null;
        this.noteForm.reset();
        this.needToUpdate.emit();
      })
      .catch(e => {
        this.PostNoteCatchErrorMethod(e);
      }).finally(() => {
        this.uploadInProgress = false;
        this.totalSize = undefined;
      });
  }

  private PostNoteCatchErrorMethod(e: any) {
    if (e.status == 400) {
      this.errorObj = e;
      this.fileToLargeErrorMessage = null;
      this.generalError = this.errorObj.error.errors['postNote'];
    }
    else if (e.status == 413) {
      this.errorObj = null;
      this.fileToLargeErrorMessage = "Le fichier est trop gros.";
      this.generalError = null;
    }
    else {
      this.errorObj = null;
      this.fileToLargeErrorMessage = null;
      this.generalError = "Une erreur est survenue. " + this.errorObj.error.errors['postNote'];
    }
  }

  convertToBase64(event: any) {
    const file = event.target.files[0];
    const reader = new FileReader();
    reader.readAsDataURL(file);
    reader.onload = () => {
      let data = reader.result?.toString().split(',')[1];
      this.noteForm.controls['fileBase64'].setValue(data);
      this.totalSize = Math.floor((data ? (data.length * (3 / 4) / 1024 / 1024) : 0) * 1000) / 1000;
    };
  }

  validateForm() {
    const form = document.getElementById('ajouter-note');
    this.noteForm.markAllAsTouched();
    this.noteForm.updateValueAndValidity();
    form?.classList.add('was-validated');
  }
}
