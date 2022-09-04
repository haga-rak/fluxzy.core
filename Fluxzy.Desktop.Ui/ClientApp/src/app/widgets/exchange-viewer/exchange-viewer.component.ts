import { Component, Input, OnInit } from '@angular/core';
import { stringify } from 'querystring';
import { ExchangeInfo } from '../../core/models/auto-generated';

@Component({
    selector: 'app-exchange-viewer',
    templateUrl: './exchange-viewer.component.html',
    styleUrls: ['./exchange-viewer.component.scss']
})
export class ExchangeViewerComponent implements OnInit {
    public  currentRequestTabView : string= 'requestHeader' ; 
    
    @Input("exchange") public exchange : ExchangeInfo ; 
    
    constructor() { }
    
    ngOnInit(): void {
    }

    public autoMaxLength(str : string, maxLength : number = 144) : string {
        if (str.length > maxLength) {
            let result = str.substring(0, maxLength -3) + "..." ; 
            return result; 
        }
        return str; 
    }

    public isRequestTabSelected(name : string)  : boolean {
        return name === this.currentRequestTabView ; 
    }

    public setSelectedRequest(tabName : string) {
        this.currentRequestTabView = tabName; 
    }
    
}
