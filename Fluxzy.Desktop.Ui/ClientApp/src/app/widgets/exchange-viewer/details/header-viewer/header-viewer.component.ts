import { Component, Input, OnInit } from '@angular/core';
import { ExchangeInfo } from '../../../../core/models/auto-generated';

@Component({
    selector: 'app-header-viewer',
    templateUrl: './header-viewer.component.html',
    styleUrls: ['./header-viewer.component.scss'],
})
export class HeaderViewerComponent implements OnInit {

  @Input("exchange") public exchange : ExchangeInfo ; 

    constructor() {}

    ngOnInit(): void {}
}
