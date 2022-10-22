import {ChangeDetectorRef, Component, Input, OnInit} from '@angular/core';
import {Action, Filter, Rule} from "../../../core/models/auto-generated";
import {BsModalRef, ModalOptions} from "ngx-bootstrap/modal";
import {ApiService} from "../../../services/api.service";
import {IValidationSource, ValidationTargetComponent} from "../../filter-forms/filter-edit/filter-edit.component";
import {filter, take, tap} from "rxjs";
import { DialogService } from '../../../services/dialog.service';

@Component({
    selector: 'app-rule-edit',
    templateUrl: './rule-edit.component.html',
    styleUrls: ['./rule-edit.component.scss']
})
export class RuleEditComponent implements OnInit, IActionValidationSource {
    public action: Action;
    public rule: Rule;

    public validationState: boolean | null = null;
    public validationMessages: string[] = [];
    public validationSource: IActionValidationSource;

    public targets: ActionValidationTargetComponent<Action>[] = [];
    public callBack :  (rule : Rule | null) => void ;

    public isEdit : boolean;


    constructor(
        public bsModalRef: BsModalRef,
        public options: ModalOptions,
        private apiService: ApiService,
        private cd: ChangeDetectorRef,
        private dialogService : DialogService) {

        this.rule = this.options.initialState.rule as Rule;
        this.callBack = this.options.initialState.callBack as (f : Rule | null) => void ;
        this.isEdit = this.options.initialState.isEdit as boolean;
        this.validationSource = this;
        this.action = this.rule.action;
    }

    ngOnInit(): void {

    }

    public register(target: ActionValidationTargetComponent<Action>): void {
        this.targets.push(target);
        this.cd.detectChanges();
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

            this.rule.action = this.action ;

            this.apiService.ruleValidate(this.rule)
                .pipe(
                    tap(f => this.callBack(f))
                ).subscribe();
            this.bsModalRef.hide();
        }
        else {
            this.cd.detectChanges();
        }
    }

    public changeFilter() : void {
        this.dialogService.openFilterCreate()
            .pipe(
                filter(t => !!t),
                tap (t => this.rule.filter = t),
                tap( _ => this.cd.detectChanges()),
                take (1)
            ).subscribe()
    }

    public selectLocalFilter() : void {
        this.dialogService.openManageFilters(true)
            .pipe(
                filter(t => !!t),
                tap (t => this.rule.filter = t),
                tap( _ => this.cd.detectChanges()),
                take (1)
            ).subscribe()
    }

    public changeToAnyFilter() : void {
        this.apiService.filterGetAnyTemplate()
            .pipe(
                tap(t => this.rule.filter = t),
                tap( _ => this.cd.detectChanges()),
                take (1)
            ).subscribe() ;
    }
}


export interface IActionValidationSource {
    register: (target: ActionValidationTargetComponent<Action>) => void;
}

@Component({
    template: '',
})
export abstract class ActionValidationTargetComponent<T extends Action> implements OnInit {
    @Input() public validationSource: IActionValidationSource;
    @Input() public action: T;

    protected constructor() {}

    ngOnInit(): void {
        this.validationSource.register(this);
        this.actionInit();
    }

    public abstract actionInit(): void;

    public abstract validate(): string | null;
}
