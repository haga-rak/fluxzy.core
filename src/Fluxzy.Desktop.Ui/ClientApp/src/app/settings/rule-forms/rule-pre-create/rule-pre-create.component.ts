import {ChangeDetectorRef, Component, OnDestroy, OnInit} from '@angular/core';
import {BsModalRef, ModalOptions} from "ngx-bootstrap/modal";
import {ApiService} from "../../../services/api.service";
import {Action, Filter, FilterTemplate, Rule} from "../../../core/models/auto-generated";
import {BehaviorSubject, combineLatest, Subject, takeUntil, tap} from "rxjs";
import {SearchByKeyword} from "../../../core/models/filter-constants";

@Component({
    selector: 'app-rule-pre-create',
    templateUrl: './rule-pre-create.component.html',
    styleUrls: ['./rule-pre-create.component.scss']
})
export class RulePreCreateComponent implements OnInit, OnDestroy {

    public callBack :  (action : Action | null) => void ;
    public actions : Action [] ;
    public filteredActions : Action [];

    public searchString : string = '' ;
    public searchString$ = new BehaviorSubject<string>('');

    private componentDestroyed$: Subject<boolean> = new Subject();

    ngOnDestroy() {
        this.componentDestroyed$.next(true);
        this.componentDestroyed$.complete();

        console.log('destroyed');
    }

    constructor(
        public bsModalRef: BsModalRef,
        public options: ModalOptions,
        private apiService: ApiService,
        private cd: ChangeDetectorRef) {
    }

    ngOnInit(): void {
        combineLatest(
        [
                    this.apiService.actionGetTemplates()
                        .pipe(
                            tap(t => this.actions = t)
                        ),
                    this.searchString$.asObservable()
                ]
        ).pipe(
            tap(t => this.filteredActions = this.applyFilter(t[0], t[1])),
            tap( _ => this.cd.detectChanges()),
            takeUntil(this.componentDestroyed$),
        ).subscribe() ;
    }

    public cancel() : void {
        this.callBack(null);
        this.bsModalRef.hide();
    }

    public searchStringChanged() : void {
        this.searchString$.next(this.searchString);
    }

    public select(action: Action) : void {
        this.callBack(action);
        this.bsModalRef.hide();
    }


    public applyFilter(originals : Action[], searchString : string) : Action[] {
        if (!searchString)
            return originals;

        return originals.filter(s => this.searchFunc(s));
    }

    public searchFunc = (action : Action) : boolean => {
        if (!this.searchString)
            return false;


        let flatSearchString = '';

        if (action.friendlyName) {
            flatSearchString += (action.friendlyName + ' ') ;
        }

        if (action.typeKind) {
            flatSearchString += (action.typeKind + ' ') ;
        }

        const lowerSearchString = this.searchString.toLocaleLowerCase();

        let res= SearchByKeyword(flatSearchString, lowerSearchString) ;
        return res;
    }

}
