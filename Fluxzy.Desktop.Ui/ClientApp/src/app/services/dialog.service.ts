import { Injectable } from '@angular/core';
import { BsModalRef, BsModalService, ModalOptions } from 'ngx-bootstrap/modal';
import {filter, finalize, map, Observable, Subject, switchMap, take, tap} from 'rxjs';
import {Action, Filter, Rule} from '../core/models/auto-generated';
import { MenuService } from '../core/services/menu-service.service';
import { FilterEditComponent } from '../settings/filter-forms/filter-edit/filter-edit.component';
import { GlobalSettingComponent } from '../settings/global-setting/global-setting.component';
import { ManageFiltersComponent } from '../settings/manage-filters/manage-filters.component';
import {ApiService} from "./api.service";
import {FilterPreCreateComponent} from "../settings/filter-forms/filter-pre-create/filter-pre-create.component";
import {ManageRulesComponent} from "../settings/manage-rules/manage-rules.component";
import {RulePreCreateComponent} from "../settings/rule-forms/rule-pre-create/rule-pre-create.component";
import {RuleEditComponent} from "../settings/rule-forms/rule-edit/rule-edit.component";

@Injectable({
    providedIn: 'root',
})
export class DialogService {
    bsModalRef?: BsModalRef;
    constructor(
        private modalService: BsModalService,
        private menuService: MenuService,
        private apiService : ApiService
    ) {
    }

    public init(): void {

      this.menuService
      .getApplicationMenuEvents()
      .pipe(
          filter((t) => t.menuId === 'global-settings'),
          tap((t) => this.openGlobalSettings())
      )
      .subscribe();

      this.menuService
      .getApplicationMenuEvents()
      .pipe(
          filter((t) => t.menuId === 'manage-filters'),
          tap((t) => this.openManageFilters(false))
      )
      .subscribe();

      this.menuService
      .getApplicationMenuEvents()
      .pipe(
          filter((t) => t.menuId === 'manage-rules'),
          tap((t) => this.openManageRules())
      )
      .subscribe();
    }

    public openGlobalSettings(): void {
        const config: ModalOptions = {
            initialState: {
                list: [
                    'Open a modal with component',
                    'Pass your data',
                    'Do something else',
                    '...',
                ],
                title: 'Modal with component',
            },
            ignoreBackdropClick : true
        };

        this.bsModalRef = this.modalService.show(
            GlobalSettingComponent,
            config
        );
        this.bsModalRef.content.closeBtnName = 'Close';
    }

    public openManageRules() {
        const config: ModalOptions = {
            initialState: {
            },
            ignoreBackdropClick : true
        };
        this.bsModalRef = this.modalService.show(
            ManageRulesComponent,
            config
        );
        this.bsModalRef.content.closeBtnName = 'Close';
    }

    public openManageFilters(selectMode: boolean): Observable<Filter | null> {

        const subject = new Subject<Filter | null>() ;
        const callBack = (f : Filter | null) => {  subject.next(f); subject.complete()};

        const config: ModalOptions = {
            initialState: {
                selectMode,
                callBack
            },
            ignoreBackdropClick : true
        };

        this.bsModalRef = this.modalService.show(
            ManageFiltersComponent,
            config
        );

        this.bsModalRef.content.closeBtnName = 'Close';

        return subject.asObservable();
    }

    public openFilterEdit(filter: Filter, isEdit : boolean = true): Observable<Filter | null> {
        const copyFilter = JSON.parse(JSON.stringify(filter)) ;
        const subject = new Subject<Filter | null>() ;

        const callBack = (f : Filter | null) => {  subject.next(f); subject.complete()};

        const config: ModalOptions = {
            initialState: {
                filter : copyFilter,
                callBack,
                isEdit
            },
            ignoreBackdropClick : true
        };

        this.bsModalRef = this.modalService.show(
            FilterEditComponent,
            config
        );

        this.bsModalRef.content.closeBtnName = 'Close';
        return subject.asObservable();
    }

    public openFilterCreate() : Observable<Filter | null> {
        const subject = new Subject<Filter | null>() ;

        this.openFilterPreCreate()
            .pipe(
                take(1),
                filter(t => !!t),
                filter(t => {
                    if (t.preMadeFilter) {
                        // No edit for premade filter
                        subject.next(t);
                        subject.complete();
                        return false;
                    }
                    return true;
                }),
                switchMap(t => this.openFilterEdit(t)),
                tap(t=>  {
                    subject.next(t);
                    subject.complete();
                }),
                finalize(() => {
                    if (!subject.closed)
                        subject.complete();
                })
            ).subscribe();

        return subject.asObservable();
    }

    public openFilterPreCreate(): Observable<Filter | null> {
        const subject = new Subject<Filter | null>() ;

        const callBack = (f : Filter | null) => {  subject.next(f); subject.complete()};

        const config: ModalOptions = {
            initialState: {
                callBack,
                isEdit : false
            },
            ignoreBackdropClick : true
        };

        this.bsModalRef = this.modalService.show(
            FilterPreCreateComponent,
            config
        );

        this.bsModalRef.content.closeBtnName = 'Close';
        return subject.asObservable();
    }

    public openRuleCreate() : Observable<Rule | null> {

        return this.openRulePreCreate()
            .pipe(
                filter(t => !!t),
                switchMap(t => this.openRuleCreateFromAction(t)),
                take(1)
            );

    }

    public openRulePreCreate(): Observable<Action | null> {
        const subject = new Subject<Action | null>() ;

        const callBack = (f : Action | null) => {  subject.next(f); subject.complete()};

        const config: ModalOptions = {
            initialState: {
                callBack,
                isEdit : false
            },
            ignoreBackdropClick : true
        };

        this.bsModalRef = this.modalService.show(
            RulePreCreateComponent,
            config
        );

        this.bsModalRef.content.closeBtnName = 'Close';
        return subject.asObservable();
    }

    public openRuleCreateFromAction(action : Action) : Observable<Rule | null> {
        return this.apiService.ruleCreateFromAction(action)
            .pipe(
                filter(t => !!t),
                switchMap(r => this.openRuleEdit(r, false)),
                take(1)
            );
    }

    public openRuleEdit(rule : Rule, isEdit : boolean = false): Observable<Rule | null> {
        const subject = new Subject<Rule | null>() ;

        const callBack = (f : Rule | null) => {  subject.next(f); subject.complete()};

        const config: ModalOptions = {
            initialState: {
                callBack,
                isEdit,
                rule
            },
            ignoreBackdropClick : true
        };

        this.bsModalRef = this.modalService.show(
            RuleEditComponent,
            config
        );

        this.bsModalRef.content.closeBtnName = 'Close';
        return subject.asObservable();
    }
}
