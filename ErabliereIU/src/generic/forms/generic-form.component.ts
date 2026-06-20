import { ChangeDetectionStrategy, Component, EventEmitter, Input, OnInit, Output } from '@angular/core';
import { FormBuilder, FormGroup, Validators, ValidatorFn, AbstractControl, FormsModule, ReactiveFormsModule } from '@angular/forms';
import { FormFieldConfig } from '../../model/form-field-config';
import { CommonModule, NgIf, NgSwitch } from '@angular/common';
import { NgFor } from '@angular/common';
import { EButtonComponent } from '../ebutton.component';

@Component({
  selector: 'app-generic-form',
  templateUrl: './generic-form.component.html',
  styleUrls: ['./generic-form.component.css'],
  standalone: true,
  changeDetection: ChangeDetectionStrategy.Eager,
  imports: [
    FormsModule,
    NgSwitch,
    NgIf,
    NgFor,
    ReactiveFormsModule,
    CommonModule,
    EButtonComponent
  ]
})
export class GenericFormComponent implements OnInit {
  @Input() formConfig: FormFieldConfig[] = [];
  @Input() initialData: any = {}; // Données existantes pour pré-remplir le formulaire
  @Input() submitButtonText: string = 'Soumettre';
  @Output() submitClicked = new EventEmitter<any>();

  form!: FormGroup;

  constructor(private readonly fb: FormBuilder) { }

  ngOnInit(): void {
    this.buildForm();
  }

  private buildForm(): void {
    const formGroupControls: { [key: string]: [any, ValidatorFn[]] } = {};

    // Trier les champs si un ordre est spécifié
    this.formConfig.sort((a, b) => (a.order || 0) - (b.order || 0));

    this.formConfig.forEach(field => {
      const initialValue = this.initialData[field.key] !== undefined
        ? this.initialData[field.key]
        : field.initialValue !== undefined
          ? field.initialValue
          : ''; // Valeur par défaut vide

      const validators = this.buildValidators(field.validators);
      formGroupControls[field.key] = [{ value: initialValue, disabled: field.disabled || false }, validators];
    });

    this.form = this.fb.group(formGroupControls);
  }

  private buildValidators(fieldValidators?: FormFieldConfig['validators']): ValidatorFn[] {
    const angularValidators: ValidatorFn[] = [];

    if (fieldValidators) {
      if (fieldValidators.required) {
        angularValidators.push(Validators.required);
      }
      if (fieldValidators.minLength) {
        angularValidators.push(Validators.minLength(fieldValidators.minLength));
      }
      if (fieldValidators.maxLength) {
        angularValidators.push(Validators.maxLength(fieldValidators.maxLength));
      }
      if (fieldValidators.min) {
        angularValidators.push(Validators.min(fieldValidators.min));
      }
      if (fieldValidators.max) {
        angularValidators.push(Validators.max(fieldValidators.max));
      }
      if (fieldValidators.email) {
        angularValidators.push(Validators.email);
      }
      if (fieldValidators.pattern) {
        angularValidators.push(Validators.pattern(fieldValidators.pattern));
      }
      // Ajoutez ici d'autres validateurs si vous les avez définis dans FormFieldConfig
    }
    return angularValidators;
  }

  getControl(key: string): AbstractControl | null {
    return this.form.get(key);
  }

  hasError(key: string, errorName: string): boolean {
    const control = this.getControl(key);
    return control ? control.hasError(errorName) && (control.touched || control.dirty) : false;
  }

  submitForm(): void {
    console.log("Submit generic form", this.form);
    if (this.form.valid) {
      this.submitClicked.emit(this.form.value);
    } else {
      // Marquer tous les champs comme "touched" pour afficher les erreurs
      this.form.markAllAsTouched();
      console.log('Form is invalid', this.form);
    }
  }
}