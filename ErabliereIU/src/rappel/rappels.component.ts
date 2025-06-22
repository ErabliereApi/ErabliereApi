import { Component, Input, SimpleChanges, OnChanges } from '@angular/core';

import { Note } from 'src/model/note';
import { ErabliereApi } from 'src/core/erabliereapi.service';
import { RappelComponent } from './rappel.component';
import { AuthorisationFactoryService } from 'src/authorisation/authorisation-factory-service';

@Component({
  selector: 'app-rappels',
  imports: [
    RappelComponent
],
  styleUrls: ['./rappels.component.css'],
  templateUrl: './rappels.component.html'
})
export class RappelsComponent implements OnChanges {
  @Input() idErabliereSelectionnee: any;
  todayReminders: Note[] = [];
  isLogged: boolean = false;

  constructor(private readonly erabliereapiService: ErabliereApi, private readonly _authService: AuthorisationFactoryService) {
    if (this._authService.getAuthorisationService().type == "AuthDisabled") {
      this.isLogged = true;
    }
    else {
      this._authService.getAuthorisationService().loginChanged.subscribe(loggedIn => {
        this.isLogged = loggedIn;
      });
    }
  }

  ngOnChanges(changes: SimpleChanges) {
    if (changes.idErabliereSelectionnee &&
      changes.idErabliereSelectionnee.currentValue !== changes.idErabliereSelectionnee.previousValue &&
      changes.idErabliereSelectionnee.currentValue &&
      this.isLogged) {
      this.erabliereapiService.putNotePeriodiciteDue(this.idErabliereSelectionnee)
        .then(() => {
          this.erabliereapiService.getActiveRappelNotes(this.idErabliereSelectionnee)
            .then((reminder) => {
              this.todayReminders = reminder;
            }).catch((error) => {
              console.error('Error fetching getActiveRappelNotes:', error);
            });
        })
        .catch((error) => {
          console.error('Error updating putNotePeriodiciteDue:', error);
        });
    }
  }
}
