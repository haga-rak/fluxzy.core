import {ChangeDetectorRef, Component, Input, OnInit} from '@angular/core';
import {Action, Filter, Rule, RuleEditorDeserializeResult} from "../../../core/models/auto-generated";
import {BsModalRef, ModalOptions} from "ngx-bootstrap/modal";
import {ApiService} from "../../../services/api.service";
import {IValidationSource, ValidationTargetComponent} from "../../filter-forms/filter-edit/filter-edit.component";
import {BehaviorSubject, debounceTime, filter, Observable, of, pipe, switchMap, take, tap} from "rxjs";
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
    public longDescription: string;

    private yamlEditMode$ = new BehaviorSubject<boolean>(false);

    public yamlEditMode = false;

    public yamlContent$ = new BehaviorSubject<string | null>(null);
    public deserializeResult : RuleEditorDeserializeResult | null = null ;

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
        this.refreshDescription().subscribe();

        this.yamlEditMode$.pipe(
            tap(t => this.yamlEditMode = t),
            filter(t => t),
            switchMap(_ => this.apiService.editorSerialize(this.rule)),
            tap(t => this.yamlContent$.next(t.content)),
            tap(_ => this.cd.detectChanges())
        ).subscribe();

        this.yamlEditMode$.pipe(
            filter(t => !t),
            switchMap(t => this.refreshDescription()),
        ).subscribe();

        this.yamlContent$.pipe(
                tap(t => this.deserializeResult = null),
                debounceTime(150),
                switchMap(t => t ? this.apiService.editorDeserialize(t) : of(null)),
                tap(t => this.deserializeResult = t),
                filter(t => t && t.success),
                tap(t => this.rule = t.rule),
                tap(t => this.action = t.rule.action),
                //
                tap(_ => this.cd.detectChanges())
        ).subscribe();
    }

    private refreshDescription() : Observable<any> {
        return this.apiService.actionLongDescription(this.action.typeKind)
            .pipe(
                tap(t => this.longDescription = t.description),
                tap(_ => this.cd.detectChanges())
            );
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
        if (!this.yamlEditMode) {
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
        else{

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

    public editFilter(filterItem: Filter) {
        this.dialogService.openFilterEdit(filterItem, true)
            .pipe(
                filter(t => !!t),
                tap(f => this.rule.filter = f),
                tap(_ => this.cd.detectChanges())
            ).subscribe();
    }

    switchToYaml(b: boolean) {
        this.yamlEditMode$.next(b);
    }

    saveEditor() {

        this.apiService.ruleValidate(this.rule)
            .pipe(
                tap(f => this.callBack(f))
            ).subscribe();
        this.bsModalRef.hide();
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
