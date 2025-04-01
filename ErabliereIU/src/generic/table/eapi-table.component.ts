import { NgFor } from '@angular/common';
import { Component, Input, OnInit } from '@angular/core';

@Component({
    selector: 'eapi-table',
    template: `
        <table class="table">
            <thead>
                <tr>
                    <th *ngFor="let column of columns">{{ column }}</th>
                </tr>
            </thead>
            <tbody>
                <tr *ngFor="let row of data">
                    <td *ngFor="let column of columns">{{ row[column] }}</td>
                </tr>
            </tbody>
        </table>
    `,
    styles: [
        `
            .table {
                width: 100%;
                border-collapse: collapse;
            }
            .table th, .table td {
                border: 1px solid #ddd;
                padding: 8px;
            }
            .table th {
                background-color: #f4f4f4;
                text-align: left;
            }
        `
    ],
    imports: [NgFor],
})
export class EapiTableComponent implements OnInit {
    @Input() schema: any; // OpenAPI schema
    @Input() data: any[] = [];

    columns: string[] = [];

    ngOnInit(): void {
        if (this.schema?.properties) {
            this.columns = Object.keys(this.schema.properties);
        }
    }
}