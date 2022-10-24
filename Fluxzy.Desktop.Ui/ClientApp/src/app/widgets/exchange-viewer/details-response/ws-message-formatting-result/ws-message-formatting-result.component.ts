import {Component, Input, OnInit} from '@angular/core';
import {ExchangeContextInfo, ExchangeInfo, ResponseTextContentResult, WsMessageFormattingResult} from "../../../../core/models/auto-generated";
import {SystemCallService} from "../../../../core/services/system-call.service";

@Component({
  selector: 'app-ws-message-formatting-result',
  templateUrl: './ws-message-formatting-result.component.html',
  styleUrls: ['./ws-message-formatting-result.component.scss']
})
export class WsMessageFormattingResultComponent  implements OnInit {
    @Input() public exchange : ExchangeInfo ;
    @Input('formatter') public model: WsMessageFormattingResult;
    @Input() public context: ExchangeContextInfo;

    constructor(private systemCallService : SystemCallService) {}

    ngOnInit(): void {}

    public setClipboard(text : string) : void {
        this.systemCallService.setClipBoard(text);
    }
}
