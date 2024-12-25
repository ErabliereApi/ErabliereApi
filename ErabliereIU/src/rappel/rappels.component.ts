import { Component, OnInit, Input, SimpleChanges, OnChanges } from '@angular/core';
import { NgFor } from '@angular/common';
import { Note } from 'src/model/note';
import { ErabliereApi } from 'src/core/erabliereapi.service';
import { RappelComponent } from './rappel.component';
import { AuthorisationFactoryService } from 'src/authorisation/authorisation-factory-service';

@Component({
    selector: 'app-rappels',
    imports: [
        NgFor,
        RappelComponent
    ],
    styleUrls: ['./rappels.component.css'],
    templateUrl: './rappels.component.html'
})
export class RappelsComponent implements OnChanges {
  @Input() idErabliereSelectionnee: any;
  todayReminders: Note[] = [];
  isLogged: boolean = false;

  constructor(private erabliereapiService: ErabliereApi, private _authService: AuthorisationFactoryService) {
    if (this._authService.getAuthorisationService().type == "AuthDisabled") {
      this.isLogged = true;
    }
    else {
      this._authService.getAuthorisationService().loginChanged.subscribe(loggedIn => {
        this.isLogged = loggedIn;
      });
    }
  }

  async ngOnChanges(changes: SimpleChanges) {
    if (changes.idErabliereSelectionnee &&
      changes.idErabliereSelectionnee.currentValue !== changes.idErabliereSelectionnee.previousValue &&
      changes.idErabliereSelectionnee.currentValue &&
      this.isLogged) {
      try {
        this.todayReminders = await this.erabliereapiService.getActiveRappelNotes(this.idErabliereSelectionnee);
        await this.erabliereapiService.putNotePeriodiciteDue(this.idErabliereSelectionnee);
      } catch (error) {
        console.error('Error getting today\'s reminders', error);
      }
    }
  }
}
