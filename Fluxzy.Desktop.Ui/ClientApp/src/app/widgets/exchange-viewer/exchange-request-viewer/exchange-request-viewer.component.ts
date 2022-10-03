import {Component, Input, OnInit} from '@angular/core';
import {ExchangeInfo, FormattingResult} from "../../../core/models/auto-generated";

@Component({
    selector: 'app-exchange-request-viewer',
    templateUrl: './exchange-request-viewer.component.html',
    styleUrls: ['./exchange-request-viewer.component.scss']
})
export class ExchangeRequestViewerComponent implements OnInit {

    @Input() isRequestTabSelected: (name: string) => boolean;

    @Input() setSelectedRequestTab: (tabName: string, formatingResult: FormattingResult, fromOther: boolean) => void;

    @Input() requestFormattingResults: FormattingResult[];

    @Input() requestOtherText: string;

    @Input() currentRequestTabView: string;

    @Input() exchange: ExchangeInfo;

    @Input() ofType: (name: string) => boolean;

    @Input() requestFormattingResult: FormattingResult;

    constructor() {
    }

    ngOnInit(): void {
    }

}
