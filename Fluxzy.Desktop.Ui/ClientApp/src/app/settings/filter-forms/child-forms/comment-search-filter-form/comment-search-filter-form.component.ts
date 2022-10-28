import {ChangeDetectorRef, Component, OnInit} from '@angular/core';
import {StringFilterFormComponent} from "../string-filter-form/string-filter-form.component";
import {CommentSearchFilter, HostFilter} from "../../../../core/models/auto-generated";

@Component({
    selector: 'app-comment-search-filter-form',
    templateUrl: '../string-filter-form/string-filter-form.component.html',
    styleUrls: ['../string-filter-form/string-filter-form.component.scss']
})
export class CommentSearchFilterFormComponent  extends StringFilterFormComponent<CommentSearchFilter> {
    constructor(cd : ChangeDetectorRef) {
        super(cd);
    }

    getFieldName(): string | null {
        return 'Comment';
    }
}
