import { Component, Input, OnInit } from '@angular/core';
import { Observable } from 'rxjs';
import { EapiTableComponent } from "./table/eapi-table.component";
import { ErabliereApi } from 'src/core/erabliereapi.service';

@Component({
    selector: 'eapi-crud-page',
    template: `
        <div>
            <h1>{{ title }}</h1>
            <button (click)="create()">Create</button>
            <eapi-table [schema]="schema" [data]="items"></eapi-table>
        </div>
    `,
    styleUrls: ['./eapi-crud-page.component.css'],
    imports: [EapiTableComponent]
})
export class EapiCrudPageComponent<T> implements OnInit {
    @Input() apiUrl!: string; // Base URL for the OpenAPI endpoint
    @Input() title: string = 'CRUD Page';
    @Input() schema: any; // OpenAPI schema for the endpoint

    items: T[] = [];

    constructor(private readonly api: ErabliereApi) {}

    ngOnInit(): void {
        this.read().subscribe(data => {
            this.items = data;
        });
    }

    create(): void {
        const newItem: Partial<T> = {}; // Replace with your default object structure
        this.api.post<T>(this.apiUrl, newItem).subscribe(createdItem => {
            this.items.push(createdItem);
        });
    }

    read(): Observable<T[]> {
        return this.api.get<T[]>(this.apiUrl);
    }

    putUpdate(item: T): void {
        this.api.put<T>(`${this.apiUrl}/${(item as any).id}`, item).subscribe(updatedItem => {
            const index = this.items.findIndex(i => (i as any).id === (item as any).id);
            if (index !== -1) {
                this.items[index] = updatedItem;
            }
        });
    }

    patchUpdate(item: Partial<T>): void {
        this.api.patch<T>(`${this.apiUrl}/${(item as any).id}`, item).subscribe(updatedItem => {
            const index = this.items.findIndex(i => (i as any).id === (item as any).id);
            if (index !== -1) {
                this.items[index] = updatedItem;
            }
        });
    }

    delete(id: number): void {
        this.api.delete(`${this.apiUrl}/${id}`).subscribe(() => {
            this.items = this.items.filter(item => (item as any).id !== id);
        });
    }
}