import { Component, EventEmitter, Input, OnChanges, Output, SimpleChanges } from '@angular/core';


@Component({
    selector: 'app-pagination',
    imports: [],
    templateUrl: './pagination.component.html'
})
export class PaginationComponent implements OnChanges {
    @Input() nombreParPage: number = 1;
    @Input() nombreElements: number = 1;
    @Output() changementDePageEvent = new EventEmitter<number>();

    pages: Array<number> = [];

    pageActuelle: number = 1;

    get nombrePages() {
        return this.pages.length;
    }

    get numeroPremierElementDeLaPage() {
        return (this.pageActuelle - 1) * this.nombreParPage + 1
    }

    get numeroDernierElementDeLaPage() {
        return this.pageActuelle * this.nombreParPage
    }

    get estDernierePage() {
        return this.nombrePages <= this.pageActuelle;
    }

    get estPremierePage() {
        return this.pageActuelle === 1
    }

    get pagesToShow(): (number | string)[] {
        const total = this.pages.length;
        const current = this.pageActuelle;
        const delta = 1; // Number of pages to show around current
        const range: (number | string)[] = [];
        let l: number = 0;

        for (let i = 1; i <= total; i++) {
            if (i === 1 || i === total || (i >= current - delta && i <= current + delta)) {
                range.push(i);
            }
        }

        for (let i = 0; i < range.length; i++) {
            if (l) {
                if ((range[i] as number) - l === 2) {
                    range.splice(i, 0, l + 1);
                } else if ((range[i] as number) - l > 2) {
                    range.splice(i, 0, '...');
                }
            }
            l = range[i] as number;
        }
        return range;
    }

    ngOnChanges(changes: SimpleChanges) {
        if (changes.nombreElements) {
            if (changes.nombreElements.currentValue) {
                this.pages = Array(Math.ceil(this.nombreElements / this.nombreParPage)).fill(null).map((_, i) => i + 1);
            } else {
                this.pages = [1];
            }
        }
    }

    pagePrecedente(): void {
        if (!this.estPremierePage) {
            --this.pageActuelle;
        }
        this.changementDePageEvent.emit(this.pageActuelle);
    }

    pageSuivante(): void {
        if (!this.estDernierePage) {
            ++this.pageActuelle;
        }
        this.changementDePageEvent.emit(this.pageActuelle);
    }

    changerPage(page: string | number): void {

        if (typeof page === 'string' && page === '...') {
            return; // Ignore ellipsis
        }
        if (typeof page === 'string') {
            page = parseInt(page, 10);
        }
        if (isNaN(page)) {
            return; // Ignore invalid page numbers
        }
        if (page >= 1 && page <= this.nombrePages && page !== this.pageActuelle) {
            this.pageActuelle = page;
            this.changementDePageEvent.emit(this.pageActuelle);
        }
    }
}
