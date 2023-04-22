import { Component, Input, OnInit } from '@angular/core';
import { FormUrlEncodedResult } from '../../../../core/models/auto-generated';

@Component({
    selector: 'app-form-url-encoded-result',
    templateUrl: './form-url-encoded-result.component.html',
    styleUrls: ['./form-url-encoded-result.component.scss'],
})
export class FormUrlEncodedResultComponent implements OnInit {
    @Input('formatter') public model: FormUrlEncodedResult;
    constructor() {}

    ngOnInit(): void {}
}
