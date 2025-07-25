import { Component, Input, SimpleChanges, OnChanges, OnInit } from '@angular/core';
import { Note } from 'src/model/note';
import { ErabliereApi } from 'src/core/erabliereapi.service';
import { RappelComponent } from './rappel.component';
import { AuthorisationFactoryService } from 'src/core/authorisation/authorisation-factory-service';
import { ActivatedRoute } from '@angular/router';
import { ModalRappelComponent } from './modal-rappel/modal-rappel.component';
import { Subject } from 'rxjs';

@Component({
  selector: 'app-rappels',
  imports: [
    RappelComponent,
    ModalRappelComponent
  ],
  templateUrl: './rappels.component.html'
})
export class RappelsComponent implements OnChanges, OnInit {
  @Input() idErabliereSelectionnee: any;
  todayReminders: Note[] = [];
  isLogged: boolean = false;
  modalNoteSubject: Subject<Note | null> = new Subject<Note | null>();
  modalNote: Note | null = null;
  chargementEnCours: boolean = false;

  constructor(private readonly erabliereapiService: ErabliereApi, private readonly _authService: AuthorisationFactoryService, private readonly _router: ActivatedRoute) {
    if (this._authService.getAuthorisationService().type == "AuthDisabled") {
      this.isLogged = true;
    }
    else {
      this._authService.getAuthorisationService().loginChanged.subscribe(loggedIn => {
        this.isLogged = loggedIn;
        this.updateAndGetRappels();
      });
    }
    this._router.queryParams.subscribe(params => {
      if (params['idErabliereSelectionnee']) {
        console.log('Query parameter idErabliereSelectionnee:', params['idErabliereSelectionnee']);
        this.idErabliereSelectionnee = params['idErabliereSelectionnee'];
        this.updateAndGetRappels();
      }
    });
  }

  ngOnInit(): void {
    this._authService.getAuthorisationService().isLoggedIn().then(isLoggedIn => {
      this.isLogged = isLoggedIn;
      this.idErabliereSelectionnee = this.idErabliereSelectionnee || this._router.snapshot.queryParams['idErabliereSelectionnee'];
      console.log('RappelsComponent ngOnInit with idErabliereSelectionnee:', this.idErabliereSelectionnee, 'isLogged:', this.isLogged);
      if (this.idErabliereSelectionnee && this.isLogged) {
        this.updateAndGetRappels();
      }
    });
  }

  ngOnChanges(changes: SimpleChanges) {
    if (changes.idErabliereSelectionnee &&
      changes.idErabliereSelectionnee.currentValue !== changes.idErabliereSelectionnee.previousValue &&
      changes.idErabliereSelectionnee.currentValue &&
      this.isLogged) {
      this.updateAndGetRappels();
    }
  }

  updateAndGetRappels() {
    if (!this.isLogged) {
      this.todayReminders = [];
      return;
    }
    this.chargementEnCours = true;
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
        this.erabliereapiService.getActiveRappelNotes(this.idErabliereSelectionnee)
          .then((reminder) => {
            this.todayReminders = reminder;
          }).catch((error) => {
            console.error('Error fetching getActiveRappelNotes:', error);
          });
      })
      .finally(() => {
        this.chargementEnCours = false;
      });
  }

  modalIsOpen = false;
  openModal(note: Note) {
    this.modalNote = note;
    this.modalIsOpen = true;
  }

  closeModal() {
    this.modalNote = null;
    this.modalIsOpen = false;
  }

  openModalOnKeyUp($event: KeyboardEvent, _t8: Note) {
    if ($event.key === 'Enter') {
      this.openModal(_t8);
    }
  }
}

