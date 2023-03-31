import {ChangeDetectorRef, Component, ElementRef, EventEmitter, HostListener, OnInit, Output} from '@angular/core';
import {QuickAction, QuickActionResult} from "../../../../core/models/auto-generated";
import {ApiService} from "../../../../services/api.service";
import {QuickActionService} from "../../../../services/quick-action.service";

@Component({
    selector: '[app-completion]',
    templateUrl: './completion.component.html',
    styleUrls: ['./completion.component.scss']
})
export class CompletionComponent implements OnInit {
    public quickActionResult : QuickActionResult | null = null ;
    @Output() public onClickOutSide : EventEmitter<any> = new EventEmitter<any>();

    constructor(private eRef: ElementRef, private cd: ChangeDetectorRef, private apiService: ApiService,
                private quickActionService : QuickActionService) {
    }

    ngOnInit(): void {
        this.quickActionService.quickActionResult$
            .subscribe((res) => {
                this.quickActionResult = res;
                this.cd.detectChanges();
            });
    }

    @HostListener('document:mouseup', ['$event'])
    clickOut(event) {
        if(this.eRef.nativeElement.contains(event.target)) {
        } else {
            this.onClickOutSide.emit(null);
        }
    }

    @HostListener('document:keydown.escape', ['$event'])
    onEscape(event: KeyboardEvent) {
        this.onClickOutSide.emit(null);
    }

    selectionAction(item: QuickAction) {

        if  (item.type === 'ClientOperation') {
            this.quickActionService.executeQuickAction(item.id);
        }

        if  (item.type === 'Filter') {
            this.apiService.filterApplyToview(item.quickActionPayload.filter!).subscribe() ;
        }

        this.onClickOutSide.emit(null);
    }
}
