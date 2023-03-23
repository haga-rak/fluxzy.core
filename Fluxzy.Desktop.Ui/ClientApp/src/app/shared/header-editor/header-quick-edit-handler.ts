import {
    AddHeaderOption, EditHeaderOption,
    EditRequestLineOption,
    EditResponseLineOption,
    Header,
    HeaderValidationResult,
    IEditableHeaderOption,
    RemoveHeaderOption, RequestLine, ResponseLine
} from "./header-utils";
import {Observable} from "rxjs";

export class HeaderQuickEditHandler {

    constructor (
        private addHeaderCallBack : (() => Observable<Header | null>),
        private editHeaderCallBack : ((header : Header) => Observable<Header | null>),
        private requestLineCallBack : ((model : RequestLine) => Observable<RequestLine | null>),
        private responseLineCallBack : ((model : ResponseLine) => Observable<ResponseLine | null>),
        ) {

    }

    public GetEditableHeaderOptions = (
        headerValidationResult: HeaderValidationResult, selectedHeader: Header, isRequest: boolean
    ): IEditableHeaderOption [] => {

        if (!headerValidationResult.valid) {
            return [];
        }
        const result: IEditableHeaderOption [] = [];

        if (isRequest) {
            if (headerValidationResult.requestLine && headerValidationResult.valid && headerValidationResult.isRequest) {
                result.push(new EditRequestLineOption(this.requestLineCallBack));
            }

        } else {
            if (headerValidationResult.responseLine && headerValidationResult.valid && !headerValidationResult.isRequest) {
                result.push(new EditResponseLineOption(this.responseLineCallBack));
            }
        }

        result.push(new AddHeaderOption(this.addHeaderCallBack));

        if (selectedHeader) {
            result.push(new RemoveHeaderOption());
            result.push(new EditHeaderOption(this.editHeaderCallBack));
        }

        // more option here for cookie

        return result;
    }
}
