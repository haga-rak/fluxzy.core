import { Component, Input, OnInit } from '@angular/core';
import { AuthorizationBearerResult } from '../../../../core/models/auto-generated';

@Component({
    selector: 'app-authorization-bearer-result',
    templateUrl: './authorization-bearer-result.component.html',
    styleUrls: ['./authorization-bearer-result.component.scss'],
})
export class AuthorizationBearerResultComponent implements OnInit {
    @Input('formatter') public model: AuthorizationBearerResult;

    constructor() {}

    ngOnInit(): void {}
}
