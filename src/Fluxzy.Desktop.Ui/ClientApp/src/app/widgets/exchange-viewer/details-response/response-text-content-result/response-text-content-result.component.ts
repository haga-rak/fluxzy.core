import { Component, Input, OnInit } from '@angular/core';
import { ExchangeContextInfo, ExchangeInfo, ResponseTextContentResult } from '../../../../core/models/auto-generated';
import { SystemCallService } from '../../../../core/services/system-call.service';

@Component({
    selector: 'app-response-text-content-result',
    templateUrl: './response-text-content-result.component.html',
    styleUrls: ['./response-text-content-result.component.scss'],
})
export class ResponseTextContentResultComponent implements OnInit {
  
  @Input() public exchange : ExchangeInfo ; 
  @Input('formatter') public model: ResponseTextContentResult;
  @Input() public context: ExchangeContextInfo;

    constructor(private systemCallService : SystemCallService) {}

    ngOnInit(): void {}
    
    public setClipboard(text : string) : void {
      this.systemCallService.setClipBoard(text); 
    }
}
