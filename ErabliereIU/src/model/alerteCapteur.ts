import { Capteur } from "./capteur";

export class AlerteCapteur {
  id?: any;
  idCapteur?: any;
  nom?: string;
  envoyerA?: string;
  texterA?: string;
  minValue?: number;
  maxValue?: number;
  dc?: string
  isEnable?: boolean;
  capteur?: Capteur;
}
