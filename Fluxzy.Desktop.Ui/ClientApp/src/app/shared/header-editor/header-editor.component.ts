import {
    AfterViewInit,
    ChangeDetectorRef,
    Component, EventEmitter, Input, OnChanges, OnDestroy, OnInit, Output, SimpleChanges,
    ViewEncapsulation
} from '@angular/core';
import {
    BehaviorSubject,
    combineLatest,
    debounceTime,
    distinct,
    filter,
    map,
    Subject,
    Subscription,
    take,
    tap
} from "rxjs";
import {
    Header,
    HeaderValidationResult, IEditableHeaderOption,
    InArray,
    NormalizeHeader,
    ParseHeaderLine, RedirectionModel,
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
    @Output() public modelChange = new EventEmitter<string>();

    @Input() public isRequest: boolean;
    @Input() public readonly = false;

    private headerSelected$ = new BehaviorSubject<Header | null>(null);
    private validationResult$ = new Subject<HeaderValidationResult>() ;
    private changeDetector$ = new Subject<string>();
    public editableOptions : IEditableHeaderOption[] | null  = null;

    public content: string = '';
    public blockId: string;
    private _subscription: Subscription;
    private handler: HeaderQuickEditHandler;

    public validationResult: HeaderValidationResult | null = null;
    public headerSelected: Header | null = null

    constructor(private cd: ChangeDetectorRef, private headerService : HeaderService) {
        this.blockId = 'yoyo';
        this.handler  = new HeaderQuickEditHandler(
            () => headerService.openAddHeaderDialog({
             name : '', value : '', edit : false,
            }) ,
            (header : Header) => headerService.openAddHeaderDialog({
                name : header.name, value :header.value, edit : true
            }),
            (model : RequestLine) => headerService.openEditRequestLineDialog(
                {
                    url : model.url, method : model.method
                }
            ).pipe(
                map(m => {
                    if (!m){
                        return null ;
                    }

                    return new RequestLine(m.method, m.url);
                })
            ),
            (model : ResponseLine) => headerService.openEditResponseLineDialog(
                {
                    statusText : model.statusText, statusCode : model.status
                }
            ).pipe(
                map(m => {
                    if (!m){
                        return null ;
                    }

                    return new ResponseLine(m.statusCode, m.statusText);
                })
            ),
            () => headerService.openSetRedirectionDialog(
                {
                    statusCode : "302",
                    location : "/redirect_uri"
                }
            ).pipe(
                map(m => {
                    if (!m){
                        return null ;
                    }

                    return {
                        statusCode : m.statusCode,
                        location : m.location
                    }
                })
            )
        );

        // raise eventEmitter when changeDetoctor$ contains change in 100ms
        this._subscription = this.changeDetector$
            .asObservable()
            .pipe(
                debounceTime(80),
                tap(s => this.modelChange.emit(s)),
            ).subscribe();

        this.validationResult$.pipe(
            tap(t => this.validationResult = t)
        ).subscribe();

        combineLatest([
            this.validationResult$.asObservable(),
            this.headerSelected$.asObservable()
        ]).pipe(
            tap(t => {
                this.editableOptions = this.handler.GetEditableHeaderOptions(t[0], t[1], this.isRequest)
            })
        ).subscribe();

        this.modelChange.asObservable()
            .pipe(
                tap(t => this.model = t),
                tap(t => this.propagateModelChange()),
                tap(_ => this.cd.detectChanges())
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
            tap(t => this.headerSelected = t)
        ).subscribe();
    }

    // Update view from the model
    private propagateModelChange() {

        const result = HeaderEditorComponent.validate(this.model, this.isRequest);
        let futureContent =  result.htmlModel.join('\n');

        if (futureContent === this.content)
            return;  // nochange

        const element = document.querySelector('#' + this.blockId);

        const caret = this.getCaret(element);

        this.content = futureContent;
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
            else{
                res.htmlModel.push(`<span class="highlight">${res.requestLine.method}</span> <span class="highlight-2">${res.requestLine.url}</span> HTTP/1.1`);
            }
        } else {
            res.responseLine = this.isValidResponseLine(firstLine);

            if (!res.responseLine) {
                res.htmlModel.push(this.getLineWithError(firstLine, 'Invalid request line'));
                res.errorMessages.push("Invalid response line");
                firstLineInvalid = true;
            }
            else{
                res.htmlModel.push(`HTTP/1.1 <span class="highlight">${res.responseLine.status}</span> ${res.responseLine.statusText}`);
            }
        }

        if (!firstLineInvalid) {
            // res.htmlModel.push(firstLine);
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
                    value: headerValue,
                    id : Math.random()
                })

                continue;
            }

            if (headerValue.trim().length == 0) {
                res.errorMessages.push("Invalid header value");
                res.htmlModel.push(this.getHeaderOnError(headerName, headerValue, 'Cannot be empty'));

                res.headers.push({
                    name: headerName,
                    value: headerValue,
                    id : Math.random()
                })
                continue;
            }

            if (headerName.indexOf(' ') >= 0) {
                res.errorMessages.push("Invalid header name");
                res.htmlModel.push(this.getHeaderOnError(headerName, headerValue, 'Header name cannot contain spaces'));

                res.headers.push({
                    name: headerName,
                    value: headerValue,
                    id : Math.random()
                })

                continue;
            }

            if (InArray(headerName, WarningHeaders)) {
                res.errorMessages.push("Transport and content related hear will be ignored");
                res.htmlModel.push(this.getHeaderOnWarning(headerName, headerValue, 'Transport and content related hear will be ignored'));

                res.headers.push({
                    name: headerName,
                    value: headerValue,
                    id : Math.random()
                })

                continue;
            }

            res.headers.push({
                name: headerName,
                value: headerValue,
                id : Math.random()
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

        const validHttpMethods = ["GET", "POST", "PUT", "DELETE", "PATCH", "HEAD", "OPTIONS", "TRACE", "LINK", "UNLINK", "PURGE", "LOCK", "UNLOCK", "PROPFIND", "VIEW"];
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
        if (parts.length < 3) {
            return null;
        }

        let validHttpVersion = ["HTTP/1.0", "HTTP/1.1", "HTTP/2"];

        if (!validHttpVersion.includes(parts[0])) {
            return null;
        }

        if (!parts[1].match(/^\d{3}$/)) {
            return null;
        }

        return new ResponseLine(parseInt(parts[1]),
            parts.slice(2, parts.length).join(" "));
    }

    public textChanged(event: any) {
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
        let keyBoardEvent = $event as KeyboardEvent;

        if (keyBoardEvent && (keyBoardEvent.ctrlKey || keyBoardEvent.metaKey || keyBoardEvent.altKey || keyBoardEvent.key === 'Control' || keyBoardEvent.key === 'Cmd')) {
            return;
        }

        // this.propagateModelChange();
        const selection = window.getSelection();
        const selectedHeader = HeaderEditorComponent.getCurrentHeader(selection);
        this.headerSelected$.next(selectedHeader);
    }

    private static getCurrentHeader(selection: Selection): Header | null {
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


            const result = ParseHeaderLine(_.trimEnd(text, '\n'),0);

            return result;
        }

        return null;
    }

    doAction(item: IEditableHeaderOption) {

        if (!this.validationResult || !this.validationResult.valid)
            return;

        item.applyTransform(this.validationResult, this.headerSelected)
            .pipe(
                take(1),
                filter(t => !!t),
                tap(t => this.modelChange.emit(t))
            ).subscribe();

    }
}


class Cursor {
}
