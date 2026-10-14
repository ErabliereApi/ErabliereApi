import { Capteur } from "./capteur";

export class AlerteCapteur {
  id?: any;
  idCapteur?: any;
  nom?: string;
  envoyerA?: string;
  texterA?: string;
  minValue?: number;
  maxValue?: number;
  marge?: number;
  delaiSilenceMinutes?: number;
  enSilenceDepuis?: string;
  estEnAlerte?: boolean;
  dateChangementEtat?: string;
  lastOccurence?: string;
  dc?: string
  isEnable?: boolean;
  capteur?: Capteur;
}
