import {ChangeDetectorRef, Component, OnInit} from '@angular/core';
import {IValidationSource, ValidationTargetComponent} from "../../filter-edit/filter-edit.component";
import {Filter, FilterCollection, HostFilter} from "../../../../core/models/auto-generated";
import {DialogService} from "../../../../services/dialog.service";
import {ApiService} from "../../../../services/api.service";
import {filter, take, tap} from "rxjs";

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
