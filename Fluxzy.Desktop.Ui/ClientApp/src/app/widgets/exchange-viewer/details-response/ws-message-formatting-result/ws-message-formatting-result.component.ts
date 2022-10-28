import {Component, Input, OnInit} from '@angular/core';
import {ExchangeContextInfo, ExchangeInfo, ResponseTextContentResult, WsMessage, WsMessageFormattingResult} from "../../../../core/models/auto-generated";
import {SystemCallService} from "../../../../core/services/system-call.service";
import {filter, switchMap, take, tap} from "rxjs";
import { ApiService } from '../../../../services/api.service';
import {StatusBarService} from "../../../../services/status-bar.service";

@Component({
  selector: 'app-ws-message-formatting-result',
  templateUrl: './ws-message-formatting-result.component.html',
  styleUrls: ['./ws-message-formatting-result.component.scss']
})
export class WsMessageFormattingResultComponent  implements OnInit {
    @Input() public exchange : ExchangeInfo ;
    @Input('formatter') public model: WsMessageFormattingResult;
    @Input() public context: ExchangeContextInfo;

    constructor(private systemCallService : SystemCallService, private apiService : ApiService, private statusBarService : StatusBarService) {}

    ngOnInit(): void {}

    public setClipboard(text : string) : void {
        this.systemCallService.setClipBoard(text);
    }

    public saveWebSocketBody(message : WsMessage) : void {
        const suggestedName = `ws-${this.exchange.id}-${message.id}-${message.direction}.data`;

        this.systemCallService.requestFileOpen(suggestedName)
            .pipe(
                take(1),
                filter(f => !!f),
                switchMap(f => this.apiService.exchangeSaveWebSocketBody(this.exchange.id,
                    message.id, f, message.direction)),
                tap(_ => this.statusBarService.addMessage('Content saved') )
            ).subscribe() ;
    }
}
