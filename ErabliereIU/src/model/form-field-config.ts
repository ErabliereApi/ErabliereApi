import { ValidatorFn } from '@angular/forms';

export interface FormFieldConfig {
  key: string;                               // Le nom de la propriété dans l'objet de données (ex: 'nom', 'email')
  label: string;                             // Le label affiché à l'utilisateur (ex: 'Nom complet')
  type: 'text' | 'number' | 'email' | 'password' | 'date' | 'textarea' | 'select' | 'checkbox'; // Type d'input HTML
  controlType?: 'input' | 'textarea' | 'dropdown' | 'checkbox'; // Optionnel: pour des contrôles plus spécifiques si besoin
  placeholder?: string;                       // Placeholder pour l'input
  options?: { key: string, value: string }[]; // Pour les select (dropdowns)
  validators?: {                              // Définition simple des validateurs
    required?: boolean;
    minLength?: number;
    maxLength?: number;
    min?: number;
    max?: number;
    email?: boolean;
    pattern?: string;
    // ... d'autres validateurs si nécessaire
  };
  initialValue?: any;                         // Valeur initiale pour le champ
  disabled?: boolean;                         // Désactiver le champ
  order?: number;                             // Pour trier l'ordre d'affichage des champs
  class?: string;                             // Classe CSS à appliquer au conteneur du champ
}