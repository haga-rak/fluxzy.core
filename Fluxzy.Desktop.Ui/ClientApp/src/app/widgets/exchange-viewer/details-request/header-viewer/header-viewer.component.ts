import { Component, Input, OnInit } from '@angular/core';
import { ExchangeInfo } from '../../../../core/models/auto-generated';
import { SystemCallService } from '../../../../core/services/system-call.service';

@Component({
    selector: 'app-header-viewer',
    templateUrl: './header-viewer.component.html',
    styleUrls: ['./header-viewer.component.scss'],
})
export class HeaderViewerComponent implements OnInit {

  @Input("exchange") public exchange : ExchangeInfo ; 

    constructor(private systemCallService: SystemCallService) {}

    ngOnInit(): void {}

    public setClipboard(text : string) : void {
      this.systemCallService.setClipBoard(text); 
    }
}
