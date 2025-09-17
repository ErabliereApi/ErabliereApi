import { Component, Input, OnChanges, OnInit } from '@angular/core';
import { marked } from 'marked';
import { DomSanitizer, SafeHtml } from '@angular/platform-browser';

@Component({
    selector: 'eapi-markdown',
    template: `<div [innerHTML]="safeHtmlContent"></div>`,
    standalone: true,
})
export class MarkdownRendererComponent implements OnInit, OnChanges {
    @Input() content?: string = ''

    safeHtmlContent: SafeHtml;

    constructor(private readonly sanitizer: DomSanitizer) {
        this.safeHtmlContent = '';
    }

    ngOnInit() {
        this.renderMarkdown();
    }

    ngOnChanges() {
        this.renderMarkdown();
    }

    private renderMarkdown() {
        const promiseOrString = marked(this.content ?? '');
        let htmlContent: string = '';

        if (promiseOrString instanceof Promise) {
            promiseOrString.then((html) => {
                htmlContent = html;
            });
        } else {
            htmlContent = promiseOrString;
        }

        const sanitizeText = this.sanitizer.sanitize(1, htmlContent) ?? '';

        this.safeHtmlContent = this.sanitizer.bypassSecurityTrustHtml(sanitizeText);
    }
}