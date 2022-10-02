import { Component, Input, OnInit } from '@angular/core';
import { RequestCookieResult } from '../../../../core/models/auto-generated';

@Component({
    selector: 'app-request-cookie-result',
    templateUrl: './request-cookie-result.component.html',
    styleUrls: ['./request-cookie-result.component.scss'],
})
export class RequestCookieResultComponent implements OnInit {
    @Input('formatter') public model: RequestCookieResult;
    constructor() {}

    ngOnInit(): void {}
}
