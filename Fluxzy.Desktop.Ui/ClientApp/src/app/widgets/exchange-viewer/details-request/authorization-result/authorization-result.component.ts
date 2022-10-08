import { Component, Input, OnInit } from '@angular/core';
import { AuthorizationResult } from '../../../../core/models/auto-generated';

@Component({
    selector: 'app-authorization-result',
    templateUrl: './authorization-result.component.html',
    styleUrls: ['./authorization-result.component.scss'],
})
export class AuthorizationResultComponent implements OnInit {
    @Input('formatter') public model: AuthorizationResult;

    constructor() {}

    ngOnInit(): void {}
}
