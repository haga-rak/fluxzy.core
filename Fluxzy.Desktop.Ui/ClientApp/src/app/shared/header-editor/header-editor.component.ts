import {
    AfterViewInit,
    ChangeDetectorRef,
    Component, EventEmitter, Input, OnChanges, OnDestroy, OnInit, Output, SimpleChanges,
    ViewEncapsulation
} from '@angular/core';
import {debounceTime, Subject, Subscription, tap} from "rxjs";
import {Header, InArray, NormalizeHeader, ParseHeaderLine, replaceAll, WarningHeaders} from "./header-utils";
import * as _ from "lodash";

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
    @Output() public headerSelected = new EventEmitter<Header>();

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

    // Update view from the model
    private propagateModelChange() {
        let result = this.validate(this.model);
        const element = document.querySelector('#' + this.blockId);

        const caret = this.getCaret(element);
        this.content = result.htmlModel.join('\n');
        this.cd.detectChanges();
        this.setCaret(caret, element);
    }

    private validate(model : string) : HeaderValidationResult {
        const res  : HeaderValidationResult = {
            valid : false,
            errorMessages : [],
            model,
            htmlModel :  []
        };

        const originalLines = replaceAll(model, '\r', '').split('\n');

        if (originalLines.length == 0) {
            // empty lines, throw error
            res.errorMessages.push("Empty lines in header");
            res.htmlModel.push(this.getLineWithError("  ", 'Header line missing'));
            return ;
        }

        const firstLine = originalLines[0];
        let firstLineInvalid = false;

        if (this.isRequest && !this.isValidRequestLine(firstLine)) {
            res.htmlModel.push(this.getLineWithError(firstLine, 'Invalid request line'));
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
            const headerParts = headerLine.split(": ");

            if (headerParts.length <= 1) {
                res.errorMessages.push("Invalid header line");
                res.htmlModel.push(this.getLineWithError(headerLine, 'Header must be a key value separated by \': \' '));
                continue;
            }

            const headerName = headerParts[0];
            const headerValue =  headerParts.slice(1, headerParts.length).join(': ');

            if (headerName.trim().length == 0) {
                res.errorMessages.push("Invalid header name");
                res.htmlModel.push(this.getHeaderOnError(headerName, headerValue, 'Cannot be empty'));
                continue;
            }

            if (headerValue.trim().length == 0) {
                res.errorMessages.push("Invalid header value");
                res.htmlModel.push(this.getHeaderOnError(headerName, headerValue, 'Cannot be empty'));
                continue;
            }

            if (headerName.indexOf(' ') >= 0) {
                res.errorMessages.push("Invalid header name");
                res.htmlModel.push(this.getHeaderOnError(headerName, headerValue, 'Header name cannot contain spaces'));
                continue;
            }

            if (InArray(headerName, WarningHeaders)) {
                res.errorMessages.push("Transport header will be ignored");
                res.htmlModel.push(this.getHeaderOnWarning(headerName, headerValue, 'Transport related header will be ignored'));
                continue;
            }

            res.htmlModel.push(`<span class="good-header">${headerName.trim()}</span>: ${headerValue}`);
        }

        return res;
    }

    private getLineWithError(lineContent : string, message : string) : string {
        return `<span class="error" title="${message}">${lineContent}</span>`;
    }

    private getHeaderOnError(headerName  : string, headerValue : string, message : string) : string {
        return `<span class="error good-header" title="${message}">${headerName}</span>: ${headerValue}`;
    }

    private getHeaderOnWarning(headerName  : string, headerValue : string, message : string) : string {
        return `<span class="warning good-header" title="${message}">${headerName}</span>: ${headerValue}`;
    }

    public isValidRequestLine(line : string) : boolean {
        const parts = line.split(" ").filter(t => t.trim().length > 0);
        if (parts.length != 3) {
            return false;
        }

        let validHttpMethods = ["GET", "POST", "PUT", "DELETE", "PATCH", "HEAD", "OPTIONS", "TRACE"];

        if (validHttpMethods.indexOf(parts[0]?.toUpperCase()) < 0) {
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
        this.changeDetector$.next(newModel);
    }

    private getCaret(parentElement) {
        if (!parentElement)
            return;

        const selection = window.getSelection();
        let charCount = -1,
            node;

        if (selection.focusNode) {
            if (this._isChildOf(selection.focusNode, parentElement)) {
                node = selection.focusNode;
                charCount = selection.focusOffset;

                while (node) {
                    if (node === parentElement) {
                        break;
                    }

                    if (node.previousSibling) {
                        node = node.previousSibling;
                        charCount += node.textContent.length;
                    } else {
                        node = node.parentNode;
                        if (node === null) {
                            break;
                        }
                    }
                }
            }
        }

        return charCount;
    }

    private setCaret(chars, element) {

        if (!element)
            return;

        if (chars >= 0) {
            const selection = window.getSelection();

            let range = this._createRange(element, { count: chars }, null);

            if (range) {
                range.collapse(false);
                selection.removeAllRanges();
                selection.addRange(range);

            }
        }
    }

    private _createRange(node, chars, range) {
        if (!range) {
            range = document.createRange()
            range.selectNode(node);
            range.setStart(node, 0);
        }

        if (chars.count === 0) {
            range.setEnd(node, chars.count);
        } else if (node && chars.count >0) {
            if (node.nodeType === Node.TEXT_NODE) {
                if (node.textContent.length < chars.count) {
                    chars.count -= node.textContent.length;
                } else {
                    range.setEnd(node, chars.count);
                    chars.count = 0;
                }
            } else {
                for (let lp = 0; lp < node.childNodes.length; lp++) {
                    range = this._createRange(node.childNodes[lp], chars, range);

                    if (chars.count === 0) {
                        break;
                    }
                }
            }
        }

        return range;
    }

    private _isChildOf(node, parentElement) {
        while (node !== null) {
            if (node === parentElement) {
                return true;
            }
            node = node.parentNode;
        }

        return false;
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

    onEdit($event: KeyboardEvent) {
        if ($event.key === 'Enter') {
            document.execCommand('insertLineBreak');
            $event.preventDefault();
        }
    }

    reEvaluateHeaderLine($event: MouseEvent) {
        const selection = window.getSelection();
        const selectedHeader = this.getCurrentHeader(selection);

        this.headerSelected.emit(selectedHeader);
    }

    private getCurrentHeader(selection: Selection) : Header | null {
        if (selection.focusNode) {
            let text = selection.focusNode.textContent;


            {
                let previousSibling = selection.focusNode.previousSibling;
                while (previousSibling != null && previousSibling.textContent.indexOf(('\n')) < 0) {
                    text = previousSibling.textContent + text;
                    previousSibling = previousSibling.previousSibling;
                }
            }


            {
                let nextSibling = selection.focusNode.nextSibling;

                if (!nextSibling) {
                    nextSibling = selection.focusNode.parentNode.nextSibling;

                    while (nextSibling != null) {

                        let index = nextSibling.textContent.indexOf(('\n'));

                        if (index < 0) {

                            text = text + nextSibling.textContent;
                            nextSibling = nextSibling.nextSibling;
                        }
                        if (index > 0) {
                            text = text + nextSibling.textContent.substring(0, index);
                            nextSibling = null;
                        }

                        if (index === 0)
                            break;
                    }
                }

            }


            const result = ParseHeaderLine(_.trimEnd( text, '\n'));

            return result;
        }

        return null ;
    }

    public deleteHeader(header : Header) : void{

        const flattenheader = `${header.name}: ${header.value}` ;
        let normalizedFlatHeader =  NormalizeHeader(this.model);
        const array = normalizedFlatHeader.split('\n').filter(l => l !== flattenheader);
        const newModel = array.join('\n') ;
        this.modelChange.emit(newModel);
    }
}

interface HeaderValidationResult {
    valid : boolean;
    model : string ;
    htmlModel : string [];
    errorMessages : string [] ;
}


class Cursor {
}

