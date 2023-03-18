import {
    AfterViewInit,
    ChangeDetectorRef,
    Component, EventEmitter, Input, OnChanges, OnDestroy, OnInit, Output, SimpleChanges,
    ViewEncapsulation
} from '@angular/core';
import {debounceTime, Subject, Subscription, tap} from "rxjs";

@Component({
    selector: 'header-editor',
    templateUrl: './header-editor.component.html',
    styleUrls: ['./header-editor.component.scss'],
    encapsulation: ViewEncapsulation.None
})
export class HeaderEditorComponent implements OnInit, OnChanges, OnDestroy, AfterViewInit {
    @Input() public model: string;
    @Input() public isRequest: boolean;
    @Output() public modelChange = new EventEmitter<string>();

    private changeDetector$ = new Subject<string>() ;

    public content: string = '';
    public blockId: string;
    private _subscription: Subscription;

    constructor(private cd : ChangeDetectorRef) {
        this.blockId = 'yoyo';

        // raise eventEmitter when changeDetoctor$ contains change in 100ms
        this._subscription = this.changeDetector$
            .asObservable()
            .pipe(
                debounceTime(800),
                tap(s => this.modelChange.emit(s))
            ).subscribe();
    }

    ngOnDestroy(): void {
        this._subscription.unsubscribe();
    }

    ngOnChanges(changes: SimpleChanges): void {
        this.propagateModelChange();
    }

    ngOnInit(): void {
        this.propagateModelChange();
    }

    private propagateModelChange() {
        let result = this.validate(this.model);
        const element = document.querySelector('#' + this.blockId);

        const caret = this.getCaret(element);
        this.content = result.htmlModel.join('\n');
        this.cd.detectChanges();
        this.setCaret(element, caret);
    }

    private validate(model : string) : HeaderValidationResult {
        const res  : HeaderValidationResult = {
            valid : false,
            errorMessages : [],
            model,
            htmlModel :  []
        };

        const originalLines = model.trim().replace('\r', '').split('\n');

        if (originalLines.length == 0) {
            // empty lines, throw error
            res.errorMessages.push("Empty lines in header");
            res.htmlModel.push(this.getLineWithError("  "));
            return ;
        }

        const firstLine = originalLines[0];
        let firstLineInvalid = false;

        if (this.isRequest && !this.isValidRequestLine(firstLine)) {
            res.htmlModel.push(this.getLineWithError(firstLine));
            res.errorMessages.push("Invalid request line");
            firstLineInvalid = true;
        }

        if (!this.isRequest && !this.isValidResponseLine(firstLine)) {
            res.errorMessages.push("Invalid response line");
            firstLineInvalid = true;
        }

        if (!firstLineInvalid) {
            res.htmlModel.push(firstLine);
        }

        for (let headerLine of originalLines.slice(1, originalLines.length)) {
            if (headerLine === '') {
                res.htmlModel.push(""); // Ignore empty lines
                continue;
            }
            const headerParts = headerLine.split(":", 2);

            if (headerParts.length <= 1) {
                res.errorMessages.push("Invalid header line");
                res.htmlModel.push(this.getLineWithError(headerLine));
                continue;
            }

            const headerName = headerParts[0];
            const headerValue = headerParts[1];

            if (headerName.trim().length == 0) {
                res.errorMessages.push("Invalid header name");
                res.htmlModel.push(this.getHeaderOnError(headerName, headerValue));
                continue;
            }

            if (headerValue.trim().length == 0) {
                res.errorMessages.push("Invalid header value");
                res.htmlModel.push(this.getHeaderOnError(headerName, headerValue));
                continue;
            }

            res.htmlModel.push(`<span class="good-header">${headerName.trim()}</span>: ${headerValue.trim()}`);
        }

        return res;
    }

    private getLineWithError(lineContent : string) : string {
        return `<span class="error">${lineContent}</span>`;
    }

    private getHeaderOnError(headerName  : string, headerValue : string) : string {
        return `<span class="error">${headerName}</span>: ${headerValue}`;
    }

    public isValidRequestLine(line : string) : boolean {
        const parts = line.split(" ").filter(t => t.trim().length > 0);
        if (parts.length != 3) {
            return false;
        }

        let validHttpMethods = ["GET", "POST", "PUT", "DELETE", "PATCH", "HEAD", "OPTIONS", "TRACE"];

        if (!validHttpMethods.includes(parts[0])) {
            return false;
        }

        if (parts[2] !== "HTTP/1.1") {
            return false;
        }
        return true;
    }

    public isValidResponseLine(line : string) : boolean {
        const parts = line.split(" ").filter(t => t.trim().length > 0);
        if (parts.length != 3) {
            return false;
        }

        let validHttpMethods = ["HTTP/1.1", "HTTP/2"];

        if (!validHttpMethods.includes(parts[0])) {
            return false;
        }

        if (!parts[1].match(/^\d{3}$/)) {
            return false;
        }

        return true;
    }

    onNameChange(event: any) {
        let newModel = event.target.textContent ;
        console.log(this.getCaret(event.target));
        this.changeDetector$.next(newModel);
        this.modelChange.emit(newModel);
    }

    private getCaret(element) {
        if (!element)
            return;

        let caretOffset = 0;
        const doc = element.ownerDocument || element.document;
        const win = doc.defaultView || doc.parentWindow;
        let sel;
        if (typeof win.getSelection != "undefined") {
            sel = win.getSelection();
            if (sel.rangeCount > 0) {
                const range = win.getSelection().getRangeAt(0);
                const preCaretRange = range.cloneRange();
                preCaretRange.selectNodeContents(element);
                preCaretRange.setEnd(range.endContainer, range.endOffset);
                caretOffset = preCaretRange.toString().length;
            }
        } else if ( (sel = doc.selection) && sel.type != "Control") {
            const textRange = sel.createRange();
            const preCaretTextRange = doc.body.createTextRange();
            preCaretTextRange.moveToElementText(element);
            preCaretTextRange.setEndPoint("EndToEnd", textRange);
            caretOffset = preCaretTextRange.text.length;
        }
        return caretOffset;
    }

    private setCaret(element, caretPos : number) {
        if (!element)
            return;

        const range = document.createRange();
        const sel = window.getSelection();
        range.setStart(element.childNodes[2], 5);
        range.collapse(true);
        sel.removeAllRanges();
        sel.addRange(range);
    }

    private handlePaste(element) {
        element.addEventListener("paste", function(e) {
            // cancel paste
            e.preventDefault();

            // get text representation of clipboard
            const text = (e.originalEvent || e).clipboardData.getData('text/plain');

            // insert text manually
            document.execCommand("insertHTML", false, text);
        });
    }

    ngAfterViewInit(): void {
        this.handlePaste(document.querySelector('#' + this.blockId));
    }
}

interface HeaderValidationResult {
    valid : boolean;
    model : string ;
    htmlModel : string [];
    errorMessages : string [] ;
}


