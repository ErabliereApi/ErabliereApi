import { Component, Input, OnChanges, OnInit, SimpleChanges } from '@angular/core';
import { ErabliereApi } from 'src/core/erabliereapi.service';
import { Baril } from 'src/model/baril';
import { NgFor, NgIf } from '@angular/common';

@Component({
    selector: 'barils-panel',
    template: `
        <div class="m-3 border-top py-2">
            <h3>Barils</h3>
            <h6>Id érablière {{ erabliereId }}</h6>
            <span *ngIf="errorMessage" class="text-danger">{{ errorMessage }}</span>
            <table class="table">
                <thead>
                    <tr>
                        <th>
                            Numéro
                        </th>
                        <th>
                            Date fermeture
                        </th>
                        <th>
                            Estimation
                        </th>
                        <th>
                            Résultat après classement
                        </th>
                    <tr>
                </thead>
                <tbody>
                    <tr *ngFor="let baril of barils">
                        <td>
                            {{baril.id}}
                        </td>
                        <td>
                            {{baril.df}}
                        </td>
                        <td>
                            {{baril.qe}}
                        </td>
                        <td>
                            {{baril.q}}
                        </td>
                    <tr>
                </tbody>
            </table>
        </div>
    `,
    imports: [NgFor, NgIf]
})
export class BarilsComponent implements OnInit, OnChanges {
    barils?:Array<Baril>;
    @Input() erabliereId:any
    errorMessage?: string;

    constructor(private _erabliereApi : ErabliereApi) { }

    ngOnChanges(changes: SimpleChanges): void {
        this.fetchBaril();
    }

    ngOnInit() {
        this.fetchBaril();
    }

    fetchBaril() {
        this._erabliereApi.getBarils(this.erabliereId)
                          .then(d => {
                            this.barils = d
                            this.errorMessage = undefined
                          })
                          .catch(e => {
                            this.barils = undefined
                            this.errorMessage = e
                          })
    }
}
