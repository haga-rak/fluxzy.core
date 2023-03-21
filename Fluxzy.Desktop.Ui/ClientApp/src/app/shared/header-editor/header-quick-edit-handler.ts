import {
    AddHeaderOption,
    EditRequestLineOption,
    EditResponseLineOption,
    Header,
    HeaderValidationResult,
    IEditableHeaderOption,
    RemoveHeaderOption
} from "./header-utils";
import {Observable} from "rxjs";

export class HeaderQuickEditHandler {

    constructor (private addHeaderCallBack : (() => Observable<Header | null>)) {

    }

    public GetEditableHeaderOptions = (
        headerValidationResult: HeaderValidationResult, selectedHeader: Header, isRequest: boolean
    ): IEditableHeaderOption [] => {

        if (!headerValidationResult.valid) {
            return [];
        }
        const result: IEditableHeaderOption [] = [];

        if (isRequest) {
            result.push(new EditRequestLineOption());
        } else {
            result.push(new EditResponseLineOption());
        }



        result.push(new AddHeaderOption(this.addHeaderCallBack));

        if (selectedHeader) {
            result.push(new RemoveHeaderOption());
        }

        // more option here for cookie

        return result;
    }
}
