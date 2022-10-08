import { Component, Input, OnInit } from '@angular/core';
import {
    ExchangeInfo,
    ExchangeContextInfo,
    SetCookieResult,
} from '../../../../core/models/auto-generated';

@Component({
    selector: 'app-set-cookie-result',
    templateUrl: './set-cookie-result.component.html',
    styleUrls: ['./set-cookie-result.component.scss'],
})
export class SetCookieResultComponent implements OnInit {
    @Input() public exchange: ExchangeInfo;
    @Input('formatter') public model: SetCookieResult;
    @Input() public context: ExchangeContextInfo;

    public showDetailState : { [name : string ] : boolean   } = {};

    constructor() {}

    ngOnInit(): void {}
}
