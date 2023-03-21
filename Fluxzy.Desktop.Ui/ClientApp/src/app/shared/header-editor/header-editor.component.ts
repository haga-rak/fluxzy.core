import {
    AfterViewInit,
    ChangeDetectorRef,
    Component, EventEmitter, Input, OnChanges, OnDestroy, OnInit, Output, SimpleChanges,
    ViewEncapsulation
} from '@angular/core';
import {BehaviorSubject, combineLatest, debounceTime, Subject, Subscription, tap} from "rxjs";
import {
    Header,
    HeaderValidationResult, IEditableHeaderOption,
    InArray,
    NormalizeHeader,
    ParseHeaderLine,
    replaceAll, RequestLine, ResponseLine,
    WarningHeaders
} from "./header-utils";
import * as _ from "lodash";
import {HeaderQuickEditHandler} from "./header-quick-edit-handler";
import {HeaderService} from "./header.service";

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

    private headerSelected$ = new BehaviorSubject<Header | null>(null);
    private validationResult$ = new Subject<HeaderValidationResult>() ;
    private changeDetector$ = new Subject<string>();
    public editableOptions : IEditableHeaderOption[] | null  = null;

    public content: string = '';
    public blockId: string;
    private _subscription: Subscription;
    private handler: HeaderQuickEditHandler;

    constructor(private cd: ChangeDetectorRef, private headerService : HeaderService) {
        this.blockId = 'yoyo';
        this.handler  = new HeaderQuickEditHandler(() => headerService.openAddHeaderDialog({
             name : '', value : '', edit : false
            }) );

        // raise eventEmitter when changeDetoctor$ contains change in 100ms
        this._subscription = this.changeDetector$
            .asObservable()
            .pipe(
                debounceTime(800),
                tap(s => this.modelChange.emit(s))
            ).subscribe();

        combineLatest([
            this.validationResult$.asObservable(),
            this.headerSelected$.asObservable()
        ]).pipe(
            tap(t => {
                this.editableOptions = this.handler.GetEditableHeaderOptions(t[0], t[1], this.isRequest)
            })
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
        this.headerSelected$.pipe(
            tap(t => this.headerSelected.emit(t))
        ).subscribe();
    }

    // Update view from the model
    private propagateModelChange() {
        const result = HeaderEditorComponent.validate(this.model, this.isRequest);
        const element = document.querySelector('#' + this.blockId);
        const caret = this.getCaret(element);
        this.content = result.htmlModel.join('\n');
        this.validationResult$.next(result);
        this.cd.detectChanges();
        this.setCaret(caret, element);
    }


    private static validate(model: string, isRequest: boolean): HeaderValidationResult {
        const res = new HeaderValidationResult ({
            valid: false,
            errorMessages: [],
            model,
            htmlModel: [],
            headers: [],
            isRequest,
            requestLine: null,
            responseLine: null
        });

        const originalLines = model.replaceAll('\r', '').split('\n');

        if (originalLines.length == 0) {
            // empty lines, throw error
            res.errorMessages.push("Empty lines in header");
            res.htmlModel.push(this.getLineWithError("  ", 'Header line missing'));
            return res;
        }

        const firstLine = originalLines[0];
        let firstLineInvalid = false;

        if (isRequest) {
            res.requestLine = this.isValidRequestLine(firstLine);
            if (!res.requestLine) {
                res.htmlModel.push(this.getLineWithError(firstLine, 'Invalid request line'));
                res.errorMessages.push("Invalid request line");
                firstLineInvalid = true;
            }
        } else {
            res.responseLine = this.isValidResponseLine(firstLine);

            if (!res.responseLine) {
                res.htmlModel.push(this.getLineWithError(firstLine, 'Invalid request line'));
                res.errorMessages.push("Invalid response line");
                firstLineInvalid = true;
            }
        }

        if (!firstLineInvalid) {
            res.htmlModel.push(firstLine);
        }

        res.valid = true;

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
            const headerValue = headerParts.slice(1, headerParts.length).join(': ');

            if (headerName.trim().length == 0) {
                res.errorMessages.push("Invalid header name");
                res.htmlModel.push(this.getHeaderOnError(headerName, headerValue, 'Cannot be empty'));

                res.headers.push({
                    name: headerName,
                    value: headerValue
                })

                continue;
            }

            if (headerValue.trim().length == 0) {
                res.errorMessages.push("Invalid header value");
                res.htmlModel.push(this.getHeaderOnError(headerName, headerValue, 'Cannot be empty'));

                res.headers.push({
                    name: headerName,
                    value: headerValue
                })
                continue;
            }

            if (headerName.indexOf(' ') >= 0) {
                res.errorMessages.push("Invalid header name");
                res.htmlModel.push(this.getHeaderOnError(headerName, headerValue, 'Header name cannot contain spaces'));

                res.headers.push({
                    name: headerName,
                    value: headerValue
                })

                continue;
            }

            if (InArray(headerName, WarningHeaders)) {
                res.errorMessages.push("Transport header will be ignored");
                res.htmlModel.push(this.getHeaderOnWarning(headerName, headerValue, 'Transport related header will be ignored'));

                res.headers.push({
                    name: headerName,
                    value: headerValue
                })

                continue;
            }

            res.headers.push({
                name: headerName,
                value: headerValue
            })
            res.htmlModel.push(`<span class="good-header">${headerName.trim()}</span>: ${headerValue}`);
        }

        return res;
    }

    private static getLineWithError(lineContent: string, message: string): string {
        return `<span class="error" title="${message}">${lineContent}</span>`;
    }

    private static getHeaderOnError(headerName: string, headerValue: string, message: string): string {
        return `<span class="error good-header" title="${message}">${headerName}</span>: ${headerValue}`;
    }

    private static getHeaderOnWarning(headerName: string, headerValue: string, message: string): string {
        return `<span class="warning good-header" title="${message}">${headerName}</span>: ${headerValue}`;
    }

    private static isValidRequestLine(line: string): RequestLine | null {
        const parts = line.split(" ").filter(t => t.trim().length > 0);
        if (parts.length != 3) {
            return null;
        }

        const validHttpMethods = ["GET", "POST", "PUT", "DELETE", "PATCH", "HEAD", "OPTIONS", "TRACE"];
        const methodIndex = validHttpMethods.indexOf(parts[0]?.toUpperCase());

        if (methodIndex < 0) {
            return null;
        }

        if (parts[2] !== "HTTP/1.1") {
            return null;
        }
        return new RequestLine(validHttpMethods[methodIndex], parts[1]);
    }

    private static isValidResponseLine(line: string): ResponseLine | null {
        const parts = line.split(" ").filter(t => t.trim().length > 0);
        if (parts.length != 3) {
            return null;
        }

        let validHttpMethods = ["HTTP/1.1", "HTTP/2"];

        if (!validHttpMethods.includes(parts[0])) {
            return null;
        }

        if (!parts[1].match(/^\d{3}$/)) {
            return null;
        }

        return new ResponseLine(parseInt(parts[1]));
    }

    public onNameChange(event: any) {
        let newModel = event.target.textContent;
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

            let range = this._createRange(element, {count: chars}, null);

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
        } else if (node && chars.count > 0) {
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
        element.addEventListener("paste", function (e) {
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

    reEvaluateHeaderLine($event: any) {
        this.propagateModelChange();
        const selection = window.getSelection();
        const selectedHeader = this.getCurrentHeader(selection);
        this.headerSelected$.next(selectedHeader);
    }

    private getCurrentHeader(selection: Selection): Header | null {
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


            const result = ParseHeaderLine(_.trimEnd(text, '\n'));

            return result;
        }

        return null;
    }

    doAction(item: IEditableHeaderOption) {

    }
}


class Cursor {
}

