import {escapeRegExp} from "lodash";
import {map, Observable, of} from "rxjs";

export interface Header {
    name: string;
    value: string;
}

export class HeaderValidationResult {
    isRequest : boolean;
    valid : boolean;
    model : string ;
    htmlModel : string [];
    errorMessages : string [] ;
    headers : Header [];
    requestLine : RequestLine | null;
    responseLine : ResponseLine | null;

    public constructor(init?:Partial<HeaderValidationResult>) {
        Object.assign(this, init);
    }
    public toHeaderString() : string
    {
        if (!this.valid) {
            return '';
        }

        const requestLine = this.isRequest ? this.requestLine.ToHeaderString() :
            this.responseLine.ToHeaderString();

        const headers = this.headers.map(h => `${h.name}: ${h.value}`).join('\r\n');
        const fullHeader  = requestLine + '\r\n' + headers  ;

        return fullHeader;
    }
}

export class RequestLine {
    public constructor(public method : string, public url : string) {
    }

    public ToHeaderString() : string {
        return `${this.method.toUpperCase()} ${this.url} HTTP/1.1`;
    }
}

export class ResponseLine {

    constructor(public status : number  ) {

    }

    public ToHeaderString() : string {
        return `HTTP/1.1 ${this.status} status_line`;
    }
}


export const WarningHeaders = ['Connection', 'Transfer-Encoding'];

export const InArray = (header: string, templateHeaders : string []) : boolean => {
    for (let templateHeader of templateHeaders) {
        if (templateHeader.toLowerCase() === header.toLowerCase()) {
            return true;
        }
    }
    return false;
}

export const ParseHeaderLine = (headerLine: string) : Header | null => {
    const tab = headerLine.split(': ');

    if (tab.length >= 2) {
        return {
            name: tab[0],
            value: tab.slice(1).join(': ')
        };
    }

    return null;
}

export const NormalizeHeader = (flatHeader : string) : string => {
    let res = replaceAll(flatHeader, '\r\n', '\n') ;
    return replaceAll(res, '\r', '\n');
}


export function replaceAll(str, find, replace) {
    return str.replace(new RegExp(escapeRegExp(find), 'g'), replace);
}

export interface IEditableHeaderOption {
    id : string ;
    optionName : string;

    applyTransform(validationResult : HeaderValidationResult): Observable<string | null>;
}

export class AddHeaderOption implements IEditableHeaderOption {
    id: string;
    optionName: string = 'Add Header';

    constructor(private callBack : (() => Observable<Header | null>)) {

    }

    applyTransform(validationResult : HeaderValidationResult):  Observable<string | null> {
        if (!validationResult.valid) {
            return null ;
        }

        const callBackResult = this.callBack();

        return callBackResult.pipe(
            map((header : Header | null) => {
                if (header) {
                    validationResult.headers.push(header);
                    return validationResult.toHeaderString();
                }
                else{
                    return null;
                }
            }
        ));
    }
}

export class RemoveHeaderOption implements IEditableHeaderOption {
    id: string;
    optionName: string= 'Delete Header';

    applyTransform(validationResult : HeaderValidationResult):  Observable<string | null>{

        return of(null);
    }
}

export class MoveUpOption implements IEditableHeaderOption {
    id: string;
    optionName: string = 'Edit Header';

    applyTransform(validationResult : HeaderValidationResult):  Observable<string | null> {
        return of(null);
    }
}

export class MoveDownOption implements IEditableHeaderOption {
    id: string;
    optionName: string = 'Move down';

    applyTransform(validationResult : HeaderValidationResult): Observable<string | null> {
        return of(null);
    }
}

export class EditRequestLineOption implements IEditableHeaderOption {
    id: string;
    optionName: string = 'Edit request line';

    applyTransform(validationResult : HeaderValidationResult):  Observable<string | null> {
        return of(null);
    }
}

export class EditResponseLineOption implements IEditableHeaderOption {
    id: string;
    optionName: string = 'Edit response line';

    applyTransform(validationResult : HeaderValidationResult):  Observable<string | null> {
        return of(null);
    }
}

export interface AlertOption {
    headerValidationResult : HeaderValidationResult ;
    option : IEditableHeaderOption ;
}





