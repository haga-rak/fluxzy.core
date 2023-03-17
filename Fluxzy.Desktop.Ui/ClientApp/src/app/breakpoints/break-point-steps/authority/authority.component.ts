import {Component, Input, OnInit} from '@angular/core';
import {BreakPointContextInfo, BreakPointContextStepInfo, ExchangeInfo} from "../../../core/models/auto-generated";

@Component({
    selector: 'app-authority',
    templateUrl: './authority.component.html',
    styleUrls: ['./authority.component.scss']
})
export class AuthorityComponent implements OnInit {
    @Input() public context : BreakPointContextInfo ;
    @Input() public stepInfo : BreakPointContextStepInfo;

    constructor() {
    }

    ngOnInit(): void {
    }

}
