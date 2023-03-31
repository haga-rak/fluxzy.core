import {
    ChangeDetectorRef,
    Component,
    ElementRef,
    EventEmitter,
    HostListener,
    Input, OnChanges,
    OnInit,
    Output,
    SimpleChanges
} from '@angular/core';
import {QuickAction, QuickActionResult} from "../../../../core/models/auto-generated";
import {ApiService} from "../../../../services/api.service";
import {QuickActionService} from "../../../../services/quick-action.service";
import {globalStringSearch} from "../../../../core/models/functions";
import {BehaviorSubject, combineLatest, filter, tap} from "rxjs";

@Component({
    selector: '[app-completion]',
    templateUrl: './completion.component.html',
    styleUrls: ['./completion.component.scss']
})
export class CompletionComponent implements OnInit, OnChanges {
    @Input() public searchString = '';

    private unfilteredActions : QuickAction[] | null = null;
    private filteredActions$ = new BehaviorSubject<QuickAction[] | null>(null);

    public filteredActions : QuickAction[] | null = null;
    public focusedAction : QuickAction | null = null;

    @Output() public onClickOutSide: EventEmitter<any> = new EventEmitter<any>();
    @Output() public onValidate: EventEmitter<any> = new EventEmitter<any>();

    private focusedIndex$ = new BehaviorSubject<number>(0);


    constructor(private eRef: ElementRef, private cd: ChangeDetectorRef, private apiService: ApiService,
                private quickActionService: QuickActionService) {
    }

    ngOnChanges(changes: SimpleChanges): void {
        this.runChanges();
        console.log('change run') ;
    }

    ngOnInit(): void {

        combineLatest([this.filteredActions$.pipe(tap(t => this.filteredActions = t), filter(t => !!t)),this.focusedIndex$])
            .pipe(tap(t => {
                const index = t[1];
                const actions = t[0];

                if (index < 0)
                    return;

                if (index >= actions.length)
                    return;

                this.focusedAction = actions[index];
            }))
            .subscribe(() => this.cd.detectChanges());


        this.quickActionService.quickActionResult$
            .subscribe((res) => {
                this.unfilteredActions = res.actions;
                this.runChanges();
            });



    }

    runChanges() : void {
        if (!this.unfilteredActions) {
            this.filteredActions$.next(null);
            return;
        }

        if (!this.searchString) {
            this.filteredActions$.next([]);
            return ;
        }

        this.focusedIndex$.next(0);

        this.filteredActions$.next(this.unfilteredActions.filter((action) => {
            const inputString = action.label +  ' ' + action.keywords.join(' ');
            return globalStringSearch(this.searchString,inputString);

        }).slice(0, 20));
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

    @HostListener('document:keydown.arrowDown', ['$event'])
    onArrowDown(event: KeyboardEvent) {
        if (!event)
            return;

        if (this.focusedIndex$.value < this.filteredActions!.length - 1)
            this.focusedIndex$.next(this.focusedIndex$.value + 1);
    }

    @HostListener('document:keydown.arrowUp', ['$event'])
    onArrowUp(event: KeyboardEvent) {
        if (!event)
            return;

        if (this.focusedIndex$.value > 0)
            this.focusedIndex$.next(this.focusedIndex$.value - 1);
    }


    @HostListener('document:keydown.enter', ['$event'])
    return(event: KeyboardEvent) {
        if (!event)
            return;

        if (!this.focusedAction)
            return;

        this.selectionAction(this.focusedAction);
    }

    selectionAction(item: QuickAction) {

        if  (item.type === 'ClientOperation') {
            this.quickActionService.executeQuickAction(item.id);
        }

        if  (item.type === 'Filter') {
            this.apiService.filterApplyToview(item.quickActionPayload.filter!).subscribe() ;
        }

        this.onValidate.emit(null);

        this.onClickOutSide.emit(null);
    }
}
