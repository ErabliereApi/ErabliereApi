import { ChangeDetectionStrategy, Component } from "@angular/core";

@Component({
    selector: 'app-ai-images',
    changeDetection: ChangeDetectionStrategy.Eager,
    template: `<p>La génération d'image arrive bientôt! Déjà disponible via l'API /ErabliereAI/Images.</p>`,
})
export class AiImagesComponent { }