import { Component, Input, OnInit } from '@angular/core';
import { RequestJsonResult } from '../../../../core/models/auto-generated';

@Component({
    selector: 'app-request-json-result',
    templateUrl: './request-json-result.component.html',
    styleUrls: ['./request-json-result.component.scss'],
})
export class RequestJsonResultComponent implements OnInit {
    @Input('formatter') public model: RequestJsonResult;

    constructor() {}

    ngOnInit(): void {}
}
