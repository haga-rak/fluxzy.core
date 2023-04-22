import { Component, Input, OnInit } from '@angular/core';
import { SystemCallService } from '../../core/services/system-call.service';

@Component({
    selector: 'code-view',
    templateUrl: './code-view.component.html',
    styleUrls: ['./code-view.component.scss'],
})
export class CodeViewComponent implements OnInit {
    @Input() public content: string = '';
    @Input() public lang: string = '';
    @Input() public title: string = '';


    constructor(private systemCallService : SystemCallService) {}

    ngOnInit(): void {}

    
    public setClipboard(text : string) : void {
      this.systemCallService.setClipBoard(text); 
    }
}
