import {ChangeDetectorRef, Component, ElementRef, EventEmitter, HostListener, OnInit, Output} from '@angular/core';

@Component({
    selector: '[app-completion]',
    templateUrl: './completion.component.html',
    styleUrls: ['./completion.component.scss']
})
export class CompletionComponent implements OnInit {
    public items : CompletionContent[] | null = null  ;
    @Output() public onClickOutSide : EventEmitter<any> = new EventEmitter<any>();

    constructor(private eRef: ElementRef, private cd: ChangeDetectorRef) {
    }

    ngOnInit(): void {

        const randomCategory = ['filter', 'whatever', 'setting', 'rule'] ;

        setTimeout(() => {

            this.items = [];

            for (let i = 0; i < 50; i++) {
                this.items.push({
                    category : randomCategory[Math.floor(Math.random() * 4)],
                    title : 'title ' + i
                });
            }

            this.cd.detectChanges();

        }, 1000);

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
}

export interface CompletionContent {
    category : string;
    title : string;
}
