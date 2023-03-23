import {
    AddHeaderOption, EditHeaderOption,
    EditRequestLineOption,
    EditResponseLineOption,
    Header,
    HeaderValidationResult,
    IEditableHeaderOption,
    RemoveHeaderOption, RequestLine
} from "./header-utils";
import {Observable} from "rxjs";

export class HeaderQuickEditHandler {

    constructor (
        private addHeaderCallBack : (() => Observable<Header | null>),
        private editHeaderCallBack : ((header : Header) => Observable<Header | null>),
        private requestLineCallBack : ((model : RequestLine) => Observable<RequestLine | null>)
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
            if (headerValidationResult.requestLine && headerValidationResult.valid) {
                result.push(new EditRequestLineOption(this.requestLineCallBack));
            }

        } else {
            result.push(new EditResponseLineOption());
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
