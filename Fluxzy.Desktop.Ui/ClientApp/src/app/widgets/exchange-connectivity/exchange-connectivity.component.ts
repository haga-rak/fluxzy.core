import { Component, Input, OnInit } from '@angular/core';
import { Subject } from 'rxjs';
import { ExchangeInfo } from '../../core/models/auto-generated';

@Component({
    selector: 'div[echange-connectivity]',
    templateUrl: './exchange-connectivity.component.html',
    styleUrls: ['./exchange-connectivity.component.scss'],
})
export class ExchangeConnectivityComponent implements OnInit {
  
    private $exchange: Subject<ExchangeInfo> = new Subject<ExchangeInfo>();

    @Input() public exchange: ExchangeInfo;

    constructor() {}

    ngOnInit(): void {
      console.log(this.exchange);
    }
}
