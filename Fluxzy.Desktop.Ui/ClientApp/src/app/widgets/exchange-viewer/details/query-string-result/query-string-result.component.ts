import { Component, Input, OnInit } from '@angular/core';
import { QueryStringResult } from '../../../../core/models/auto-generated';

@Component({
    selector: 'app-query-string-result',
    templateUrl: './query-string-result.component.html',
    styleUrls: ['./query-string-result.component.scss'],
})
export class QueryStringResultComponent implements OnInit {
    @Input('formatter') public model: QueryStringResult;

    constructor() {}

    ngOnInit(): void {}
}
