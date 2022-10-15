import {
    ChangeDetectorRef,
    Component,
    Injectable,
    Input,
    OnInit,
} from '@angular/core';
import { BsModalRef, ModalOptions } from 'ngx-bootstrap/modal';
import { Observable, Subject, take, finalize, tap } from 'rxjs';
import { Filter } from '../../../core/models/auto-generated';
import { ApiService } from '../../../services/api.service';

@Component({
    selector: 'app-filter-edit',
    templateUrl: './filter-edit.component.html',
    styleUrls: ['./filter-edit.component.scss'],
})
export class FilterEditComponent implements OnInit, IValidationSource {
    public filter: Filter;

    public validationState: boolean | null = null;
    public validationMessages: string[] = [];
    public validationSource: IValidationSource;

    public targets: ValidationTargetComponent<Filter>[] = [];
    public callBack :  (f : Filter | null) => void ;

    public isEdit : boolean;

    constructor(
        public bsModalRef: BsModalRef,
        public options: ModalOptions,
        private apiService: ApiService,
        private cd: ChangeDetectorRef
    ) {
        this.filter = this.options.initialState.filter as Filter;
        this.callBack = this.options.initialState.callBack as (f : Filter | null) => void ;
        this.isEdit = this.options.initialState.isEdit as boolean;
        this.validationSource = this;

        console.log('received filter');
        console.log(this.filter);
    }

    ngOnInit(): void {}

    public register(target: ValidationTargetComponent<Filter>): void {
        this.targets.push(target);
    }


    public cancel() : void {
        this.callBack(null);
        this.bsModalRef.hide();
    }

    public save(): void {
        this.validationState = null;
        this.validationMessages.length = 0;

        for (const target of this.targets) {
            const message = target.validate();

            if (message) {
                this.validationState = false;
                this.validationMessages.push(message);
            }
        }

        if (this.validationState === null) {
            this.validationState = true;

            this.apiService.filterValidate(this.filter)
                .pipe(
                    tap(f => this.callBack(f))
                ).subscribe();
            this.bsModalRef.hide();
        }
    }
}

export interface IValidationSource {
    register: (target: ValidationTargetComponent<Filter>) => void;
}

@Component({
    template: '',
})
export abstract class ValidationTargetComponent<T extends Filter> implements OnInit {
    @Input() public validationSource: IValidationSource;
    @Input() public filter: T;

    protected constructor() {}

    ngOnInit(): void {
        this.validationSource.register(this);
        this.filterInit();
    }

    public abstract filterInit(): void;

    public abstract validate(): string | null;
}
