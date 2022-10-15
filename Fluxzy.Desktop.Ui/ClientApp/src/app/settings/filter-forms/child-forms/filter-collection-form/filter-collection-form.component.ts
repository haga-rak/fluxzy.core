import {ChangeDetectorRef, Component, OnInit} from '@angular/core';
import {IValidationSource, ValidationTargetComponent} from "../../filter-edit/filter-edit.component";
import {Filter, FilterCollection, HostFilter} from "../../../../core/models/auto-generated";
import {DialogService} from "../../../../services/dialog.service";
import {ApiService} from "../../../../services/api.service";
import {filter, take, tap} from "rxjs";
import * as _ from 'lodash';

@Component({
    selector: 'app-filter-collection-form',
    templateUrl: './filter-collection-form.component.html',
    styleUrls: ['./filter-collection-form.component.scss']
})
export class FilterCollectionFormComponent extends ValidationTargetComponent<FilterCollection> implements IValidationSource {
    public selectionOperatorValues = ["Or", "And"];
    public validationSource : IValidationSource ;

    constructor(private dialogService : DialogService, private apiService : ApiService, private cd : ChangeDetectorRef) {
        super();
        this.validationSource = this;
    }

    register: (target: ValidationTargetComponent<Filter>) => void;

    filterInit(): void {
    }

    validate(): string | null {
        return null;
    }

    public edit(childFilter : Filter) : void {
        this.dialogService.openFilterEdit(
            childFilter, true
        ).pipe(
            take(1),
            filter(t => !!t),
            tap(t => {
                const targetIndex = _.findIndex(this.filter.children, c => c.identifier === t.identifier);

                if (targetIndex >= 0) {
                    this.filter.children[targetIndex] = t ;
                }
            }),
            tap(_ => this.cd.detectChanges())
        ).subscribe()

    }

    public delete(childFilter : Filter) : void {
        // TODO ask confirmation
        _.remove(this.filter.children, c => c.identifier === childFilter.identifier);
        this.cd.detectChanges();
    }


    public addNewFilter() : void {
        this.dialogService.openFilterCreate()
            .pipe(
                take(1),
                filter(t => !!t),
                tap(t => this.filter.children.push(t)),
                tap(_ => this.cd.detectChanges()),
                tap(_ => console.log('children pushed'))
            ).subscribe();
    }

}
