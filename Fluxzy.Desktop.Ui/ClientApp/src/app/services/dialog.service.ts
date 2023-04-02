import { Injectable } from '@angular/core';
import { BsModalRef, BsModalService, ModalOptions } from 'ngx-bootstrap/modal';
import {BehaviorSubject, delay, filter, finalize, map, Observable, Subject, switchMap, take, tap} from 'rxjs';
import {
    Action, CertificateWizardStatus,
    CommentUpdateModel,
    Filter,
    HttpArchiveSavingSetting,
    Rule,
    Tag,
    TagGlobalApplyModel
} from '../core/models/auto-generated';
import { MenuService } from '../core/services/menu-service.service';
import { FilterEditComponent } from '../settings/filter-forms/filter-edit/filter-edit.component';
import { GlobalSettingComponent } from '../settings/global-setting/global-setting.component';
import { ManageFiltersComponent } from '../settings/manage-filters/manage-filters.component';
import {ApiService} from "./api.service";
import {FilterPreCreateComponent} from "../settings/filter-forms/filter-pre-create/filter-pre-create.component";
import {ManageRulesComponent} from "../settings/manage-rules/manage-rules.component";
import {RulePreCreateComponent} from "../settings/rule-forms/rule-pre-create/rule-pre-create.component";
import {RuleEditComponent} from "../settings/rule-forms/rule-edit/rule-edit.component";
import {CreateTagComponent} from "../settings/tags/create-tag/create-tag.component";
import {WaitDialogComponent} from "../shared/wait-dialog/wait-dialog.component";
import {CommentApplyComponent} from "../shared/comment-apply/comment-apply.component";
import {TagApplyComponent} from "../shared/tag-apply/tag-apply.component";
import {HarExportSettingComponent} from "../shared/har-export-setting/har-export-setting.component";
import {WizardComponent} from "../settings/wizard/wizard.component";
import {BreakPointDialogComponent} from "../breakpoints/break-point-dialog/break-point-dialog.component";
import {BreakPointService} from "../breakpoints/break-point.service";
import {DisplayStringComponent} from "../widgets/display-string/display-string.component";

@Injectable({
    providedIn: 'root',
})
export class DialogService {
    bsModalRef?: BsModalRef;
    waitModalRef?: BsModalRef;
    private breakPointDialog: any;
    constructor(
        private modalService: BsModalService,
        private apiService : ApiService
    ) {
    }

    public init(): void {
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

    public openWizardDialog(certificateWizardStatus : CertificateWizardStatus) : Observable<any> {
        const subject = new Subject<any>();
        const callback = (t : any) => { subject.next(t); subject.complete()};

        const config: ModalOptions = {
            initialState: {
                certificateWizardStatus,
                callback
            },
            ignoreBackdropClick : true
        };

        this.bsModalRef = this.modalService.show(
            WizardComponent,
            config
        );

        this.bsModalRef.content.closeBtnName = 'Close';

        return subject.asObservable().pipe(take(1));
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

        return subject.asObservable().pipe(take(1));;
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
        return subject.asObservable().pipe(take(1));;
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

        return subject.asObservable().pipe(take(1));;
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
        return subject.asObservable().pipe(take(1));;
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
        return subject.asObservable().pipe(take(1));;
    }


    public openRuleCreateFromActionWithDialog(action: Action): Observable<any> {
        return this.openRuleCreateFromAction(action).
        pipe(
            take(1),
            filter(t => !!t),
            switchMap(t => this.apiService.ruleAddToExisting(t))
            // We need to apply this immediately
        );

        // this.openRuleCreateFromAction(action)
        //     .pipe(
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
        return subject.asObservable().pipe(take(1));;
    }

    public openTagCreate() : Observable<Tag | null> {
        const subject = new Subject<Tag | null>() ;

        const callBack = (f : Tag | null) => {  subject.next(f); subject.complete()};

        const config: ModalOptions = {
            class: 'little-down modal-dialog-small',
            initialState: {
                callBack,
            },
            ignoreBackdropClick : true
        };

        this.bsModalRef = this.modalService.show(
            CreateTagComponent,
            config
        );

        this.bsModalRef.content.closeBtnName = 'Close';
        return subject.asObservable().pipe(take(1));;
    }

    public openHarExportSettingDialog() : Observable<HttpArchiveSavingSetting | null> {
        const subject = new Subject<HttpArchiveSavingSetting | null>() ;
        const callBack = (f : HttpArchiveSavingSetting | null) => {  subject.next(f); subject.complete()};

        const config: ModalOptions = {
            class: 'little-down modal-dialog-small',
            initialState: {
                callBack,
            },
            ignoreBackdropClick : true
        };

        this.bsModalRef = this.modalService.show(
            HarExportSettingComponent,
            config
        );

        this.bsModalRef.content.closeBtnName = 'Close';
        return subject.asObservable().pipe(take(1));;
    }

    public openWaitDialog(message : string) : Observable<any> {
        const completeListener = new BehaviorSubject<any | null>(null) ;
        const observableResult = completeListener.asObservable().pipe(
            filter (t => !!t),
            delay(500) // TODO : this delay is necessary to avoid a bug in the modal service for early calling close
        );

        const config: ModalOptions = {
            class: 'little-down modal-dialog-small',
            initialState: {
                message,
                completeListener
            },
            ignoreBackdropClick : true
        };
        this.waitModalRef = this.modalService.show(
            WaitDialogComponent,
            config
        );

        return observableResult;
    }

    public closeWaitDialog() : void {
        console.log('finalize called');
        if (this.waitModalRef) {
            this.waitModalRef.hide();
            this.waitModalRef = null ;
        }
    }

    public openCommentApplyDialog(comment : string, exchangeIds : number[]) : Observable<CommentUpdateModel | null> {
        const subject = new Subject<CommentUpdateModel>() ;
        const callBack = (f : CommentUpdateModel | null) => {  subject.next(f); subject.complete()};
        const commentUpdateModel  : CommentUpdateModel = {
            comment,
            exchangeIds
        }

        const config: ModalOptions = {
            class: 'little-down modal-dialog-small',
            initialState: {
                class: 'little-down modal-dialog-small',
                commentUpdateModel,
                callBack
            },
            ignoreBackdropClick : true
        };

        this.waitModalRef = this.modalService.show(
            CommentApplyComponent,
            config
        );

        return subject.asObservable().pipe(take(1));
    }

    public openTagApplyDialog(model : TagGlobalApplyModel) : Observable<TagGlobalApplyModel | null> {
        const subject = new Subject<TagGlobalApplyModel>() ;
        const callBack = (f : TagGlobalApplyModel | null) => {  subject.next(f); subject.complete()};
        const tagApplyModel  : TagGlobalApplyModel =  model;

        const config: ModalOptions = {
            class: 'little-down modal-dialog-small',
            initialState: {
                class: 'little-down modal-dialog-small',
                tagApplyModel,
                callBack
            },
            ignoreBackdropClick : true
        };

        this.waitModalRef = this.modalService.show(
            TagApplyComponent,
            config
        );

        return subject.asObservable().pipe(take(1));
    }

    public openStringDisplay(title : string, value : string) : void {
        const config: ModalOptions = {
            class: 'little-down modal-dialog-small',
            initialState: {
                class: 'little-down modal-dialog-small',
                title,
                value
            },
            ignoreBackdropClick : false
        };

        this.waitModalRef = this.modalService.show(
            DisplayStringComponent,
            config
        );
    }

}
