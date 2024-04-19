import { Component, OnInit, Input, SimpleChanges } from '@angular/core';
import { NgFor } from '@angular/common';
import { Note } from 'src/model/note';
import { ErabliereApi } from 'src/core/erabliereapi.service';
import { RappelComponent } from './rappel.component';

@Component({
  selector: 'app-rappels',
  standalone: true,
  imports: [
    NgFor,
    RappelComponent
  ],
  styleUrls: ['./rappels.component.css'] ,
  templateUrl: './rappels.component.html'
})
export class RappelsComponent {
  @Input() idErabliereSelectionnee: any;
  todayReminders: Note[] = [];

  constructor(private erabliereapiService: ErabliereApi) { }

  async ngOnChanges(changes: SimpleChanges) {
    if (changes.idErabliereSelectionnee && changes.idErabliereSelectionnee.currentValue) {
      try {
        this.todayReminders = await this.getTodaysReminders(this.idErabliereSelectionnee);
      } catch (error) {
        console.error('Error getting today\'s reminders', error);
      }
    }
  }

  async getTodaysReminders(idErabliereSelectionnee:any, skip: number = 0, top?: number): Promise<Note[]> {
    const notes = await this.erabliereapiService.getNotes(idErabliereSelectionnee, skip, top);

    const today = new Date();
    today.setHours(0, 0, 0, 0);

    const todayNotes = notes.filter(note => {
      const reminderDate = note.reminderDate ? new Date(note.reminderDate) : undefined;
      if (reminderDate) {
        reminderDate.setHours(0, 0, 0, 0);
        return reminderDate.getTime() === today.getTime();
      }
      return false;
    });
    return todayNotes;
  }

}
