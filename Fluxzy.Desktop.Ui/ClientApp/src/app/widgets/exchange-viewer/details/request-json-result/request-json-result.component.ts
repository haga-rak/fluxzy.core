import { Component, Input, OnInit } from '@angular/core';
import { RequestJsonResult } from '../../../../core/models/auto-generated';
import { SystemCallService } from '../../../../core/services/system-call.service';

@Component({
    selector: 'app-request-json-result',
    templateUrl: './request-json-result.component.html',
    styleUrls: ['./request-json-result.component.scss'],
})
export class RequestJsonResultComponent implements OnInit {
    @Input('formatter') public model: RequestJsonResult;

    public content : string ; 
    public alreadyFormatted : boolean; 


    constructor(private systemCallService: SystemCallService) {}

    ngOnInit(): void {
      this.content = this.model.formattedBody ; 
      this.alreadyFormatted = this.model.formattedBody === this.model.rawBody; 

    }

    public setClipboard(text : string) : void {
      this.systemCallService.setClipBoard(text); 
    }
}
