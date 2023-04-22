import { Component, Input, OnInit } from '@angular/core';
import {
    ExchangeContextInfo,
    ExchangeInfo,
    ResponseJsonResult,
} from '../../../../core/models/auto-generated';
import { SystemCallService } from '../../../../core/services/system-call.service';

@Component({
    selector: 'app-response-json-result',
    templateUrl: './response-json-result.component.html',
    styleUrls: ['./response-json-result.component.scss'],
})
export class ResponseJsonResultComponent implements OnInit {
    @Input() public exchange: ExchangeInfo;
    @Input('formatter') public model: ResponseJsonResult;
    @Input() public context: ExchangeContextInfo;

    public alreadyFormatted : boolean = false; 
    public title: string ; 

    constructor(private systemCallService : SystemCallService) {}

    ngOnInit(): void {
      this.alreadyFormatted = this.context.responseBodyText === this.model.formattedContent; 
      this.title = !this.alreadyFormatted ? 'This JSON has been formatted to provide better readability.' : ''; 
    }
    
    public setClipboard(text : string) : void {
      this.systemCallService.setClipBoard(text); 
    }
}
