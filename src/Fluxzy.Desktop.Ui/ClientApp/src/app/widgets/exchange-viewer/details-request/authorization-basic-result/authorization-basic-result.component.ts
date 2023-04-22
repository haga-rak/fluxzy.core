import {Component, Input, OnInit} from '@angular/core';
import {AuthorizationBasicResult, AuthorizationBearerResult} from "../../../../core/models/auto-generated";

@Component({
    selector: 'app-authorization-basic-result',
    templateUrl: './authorization-basic-result.component.html',
    styleUrls: ['./authorization-basic-result.component.scss']
})
export class AuthorizationBasicResultComponent implements OnInit {
    @Input('formatter') public model: AuthorizationBasicResult;

    constructor() {
    }

    ngOnInit(): void {
    }

}
