import { Component, Input, OnInit } from '@angular/core';
import { ExchangeInfo } from '../../../../core/models/auto-generated';
import { ExchangeStyle } from '../../../../core/models/exchange-extensions';
import { SystemCallService } from '../../../../core/services/system-call.service';

@Component({
    selector: 'app-response-summary',
    templateUrl: './response-summary.component.html',
    styleUrls: ['./response-summary.component.scss'],
})
export class ResponseSummaryComponent implements OnInit {
  
  
    @Input() public exchange : ExchangeInfo ; 
  
    constructor(private systemCallService: SystemCallService) {}

    ngOnInit(): void {
    }

    
    public setClipboard(text : string) : void {
      this.systemCallService.setClipBoard(text); 
    }
}
