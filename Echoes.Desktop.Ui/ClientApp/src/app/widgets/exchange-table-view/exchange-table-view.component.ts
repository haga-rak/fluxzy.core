import { Component, OnInit } from '@angular/core';
import { BuildMockExchanges } from '../../core/models/exchanges-mock';

@Component({
    selector: 'app-exchange-table-view',
    templateUrl: './exchange-table-view.component.html',
    styleUrls: ['./exchange-table-view.component.scss']
})
export class ExchangeTableViewComponent implements OnInit {
    
    public exchanges = BuildMockExchanges();
    
    constructor() { }
    
    ngOnInit(): void {
        
    }
    
}


