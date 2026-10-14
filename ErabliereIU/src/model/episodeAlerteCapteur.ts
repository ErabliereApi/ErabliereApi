export type RaisonFinEpisode = 'RetourNormale' | 'AlerteModifiee' | 'AlerteDesactivee' | 'AlerteSupprimee';

export class EpisodeAlerteCapteur {
  id?: any;
  idAlerteCapteur?: any;
  idCapteur?: any;
  nomAlerte?: string;
  dateDebut?: string;
  valeurDebut?: number;
  dateFin?: string;
  valeurFin?: number;
  raisonFin?: RaisonFinEpisode;
}
