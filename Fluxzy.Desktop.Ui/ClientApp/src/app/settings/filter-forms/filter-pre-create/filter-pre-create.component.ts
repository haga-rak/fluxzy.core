import {ChangeDetectorRef, Component, OnInit} from '@angular/core';
import {BsModalRef, ModalOptions} from "ngx-bootstrap/modal";
import {ApiService} from "../../../services/api.service";
import {Filter, FilterTemplate} from "../../../core/models/auto-generated";
import {BehaviorSubject, tap, pipe,combineLatest} from "rxjs";
import {SearchByKeyword} from "../../../core/models/filter-constants";

@Component({
    selector: 'app-filter-pre-create',
    templateUrl: './filter-pre-create.component.html',
    styleUrls: ['./filter-pre-create.component.scss']
})
export class FilterPreCreateComponent implements OnInit {
    public filterTemplates : FilterTemplate[] ;
    public filteredFilterTemplates : FilterTemplate[];

    public searchString : string  = '';
    public searchString$ = new BehaviorSubject<string>('');

    public callBack :  (f : Filter | null) => void ;

    constructor(
        public bsModalRef: BsModalRef,
        public options: ModalOptions,
        private apiService: ApiService,
        private cd: ChangeDetectorRef) {
        this.callBack = this.options.initialState.callBack as (f : Filter | null) => void ;
    }

    ngOnInit(): void {
        combineLatest(
            [
                this.apiService.filterGetTemplates().pipe(
                    tap(t => this.filterTemplates = t)
                    ),
                this.searchString$.asObservable()
            ]
        ).pipe(
            tap (
                t =>
                this.filteredFilterTemplates = this.applyFilter(t[0], t[1])
            )
        ).subscribe();
    }

    public select(filter : Filter) : void {
        this.callBack(filter);
        this.bsModalRef.hide();
    }

    public commonOnly(filterTemplate : FilterTemplate) : boolean {
        return filterTemplate.filter.common ;
    }

    public applyFilter(originals : FilterTemplate[], searchString : string) : FilterTemplate[] {
        if (!searchString)
            return [];

        return originals.filter(s => this.searchFunc(s));
    }

    public searchFunc = (filterTemplate : FilterTemplate) : boolean => {
        if (!this.searchString)
            return false;

        const filter = filterTemplate.filter;

        let flatSearchString = '';

        if (filter.description) {
            flatSearchString += (filter.description + ' ') ;
        }

        if (filter.friendlyName) {
            flatSearchString += (filter.friendlyName + ' ') ;
        }

        if (filter.typeKind) {
            flatSearchString += (filter.typeKind  + ' ') ;
        }

        const lowerSearchString = this.searchString.toLocaleLowerCase();

        let res= SearchByKeyword(flatSearchString, lowerSearchString) ;
        return res;
    }

    public cancel() : void {
        this.callBack(null);
        this.bsModalRef.hide() ;
    }

    public searchStringChanged() : void {
        this.searchString$.next(this.searchString);
    }
}
