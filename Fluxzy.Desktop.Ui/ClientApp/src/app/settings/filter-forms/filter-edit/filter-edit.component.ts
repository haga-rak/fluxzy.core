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

    public targets: ValidationTarget<Filter>[] = [];

    constructor(
        public bsModalRef: BsModalRef,
        public options: ModalOptions,
        private apiService: ApiService,
        private cd: ChangeDetectorRef
    ) {
        this.filter = this.options.initialState.filter as Filter;
        this.validationSource = this;
        console.log('received filter');
        console.log(this.filter);
    }

    ngOnInit(): void {}

    public register(target: ValidationTarget<Filter>): void {
        this.targets.push(target);
    }

    public get<T>(item: any): T {
        return item as T;
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
        }
    }
}

export interface IValidationSource {
    register: (target: ValidationTarget<Filter>) => void;
}

@Component({
    template: '',
})
export abstract class ValidationTarget<T extends Filter> implements OnInit {
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
