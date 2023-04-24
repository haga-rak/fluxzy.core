import { HttpClient } from '@angular/common/http';
import { Injectable } from '@angular/core';
import {
    Observable,
    take,
    map,
    tap,
    Subject,
    finalize,
    filter,
    interval,
    switchAll,
    switchMap,
    catchError,
    of, delay, BehaviorSubject, pipe, Subscription
} from 'rxjs';
import {
    Action,
    AnyFilter,
    ArchiveMetaInformation,
    Certificate,
    CertificateOnStore,
    CertificateValidationResult, CertificateWizardStatus,
    CommentUpdateModel,
    ConnectionInfo, ConnectionSetupStepModel,
    ContextMenuAction, CurlCommandResult, DescriptionInfo,
    ExchangeBrowsingState, ExchangeInfo, ExchangeMetricInfo,
    ExchangeState,
    FileContentDelete,
    FileSaveViewModel,
    FileState,
    Filter,
    FilterTemplate,
    FluxzySettingsHolder,
    FormatterContainerViewModel,
    FormattingResult,
    ForwardMessage, FullUrlSearchViewModel, HarExportRequest, IPEndPoint,
    MultipartItem,
    NetworkInterfaceInfo, QuickActionResult, RequestSetupStepModel, ResponseSetupStepModel,
    Rule,
    RuleContainer,
    SaveFileMultipartActionModel, SazExportRequest,
    StoredFilter,
    Tag,
    TagGlobalApplyModel,
    TagUpdateModel,
    TrunkState, UiSetting,
    UiState, UiUserSetting,
    ValidationError
} from '../core/models/auto-generated';
import {FilterHolder} from "../settings/manage-filters/manage-filters.component";
import {IWithName} from "../core/models/model-extensions";
import {BackFailureDialog} from "../core/services/menu-service.service";
import {ElectronService} from "../core/services";

@Injectable({
  providedIn: 'root'
})
export class ApiService {
    private forwardMessages$ = new Subject<ForwardMessage>();
    private loop$ = new BehaviorSubject<any>(null);
    private _lastSub: Subscription;

    constructor(private httpClient: HttpClient, private electronService : ElectronService)
    {
        this.loopForwardMessage();
    }

    public loopForwardMessage() : void {

        if (this._lastSub) {
            this._lastSub.unsubscribe();
            this._lastSub = null ;
        }

        this._lastSub = this.forwardMessageConsume()
            .pipe(
                take(1),
                tap(messages => {
                    for (const message of messages) {
                        this.forwardMessages$.next(message);
                    }
                }),
                catchError(err => {
                    const result = this.electronService.showBackendFailureDialog('Fluxzy backend cannot be reached!');

                    if (result === BackFailureDialog.Retry) {
                     //   this.loopForwardMessage();
                        return of (0);
                    }

                    if (result === BackFailureDialog.Close) {
                    }

                    return of(1) ;
                }),
                tap(t => {
                    if (t !== 1)
                        this.loopForwardMessage();
                }),
                finalize(() => {
                    // Run a messagebox here
                })
            ).subscribe();
    }


    public registerEvent<T>(name : string, callback : (arg : T) => void ){

        this.forwardMessages$.asObservable()
            .pipe(
                filter(t => t.type === name),
                tap(m => callback(m.payload as T))
            ).subscribe();
    }

    public trunkDelete(fileContentDelete : FileContentDelete ) : Observable<TrunkState> {
        return this.httpClient.post<TrunkState>(`api/file-content/delete`, fileContentDelete)
            .pipe(
                take(1),
                ) ;
    }

    public trunkClear() : Observable<TrunkState> {
        return this.httpClient.delete<TrunkState>(`api/file-content`)
            .pipe(
                take(1),
                ) ;
    }

    public readTrunkState(workingDirectory: string) : Observable<TrunkState> {
         return this.httpClient.post<TrunkState>(`api/file-content/read`, null)
        .pipe(
            take(1)

            );
    }

    public fileOpen(fileName : string) : Observable<UiState> {
        return this.httpClient.post<UiState>(`api/file/open`, { fileName })
            .pipe(
                take(1)
            );
    }

    public fileNew() : Observable<UiState> {
        return this.httpClient.post<UiState>(`api/file/new`, null)
            .pipe(
                take(1)
            );
    }

    public fileSave() : Observable<UiState> {
        return this.httpClient.post<UiState>(`api/file/save`, null)
            .pipe(
                take(1)
            );
    }
    public fileSaveAs(model : FileSaveViewModel) : Observable<UiState> {
        return this.httpClient.post<UiState>(`api/file/save-as`, model)
            .pipe(
                take(1)
            );
    }

    public fileExportHar(exportRequest : HarExportRequest) : Observable<boolean> {
        return this.httpClient.post<boolean>(`api/file/export/har`, exportRequest)
            .pipe(
                take(1)
            );
    }

    public fileExportSaz(exportRequest : SazExportRequest) : Observable<boolean> {
        return this.httpClient.post<boolean>(`api/file/export/saz`, exportRequest)
            .pipe(
                take(1)
            );
    }

    public proxyOn() : Observable<UiState> {
        return this.httpClient.post<UiState>(`api/proxy/on`, null)
            .pipe(
                take(1)
            );
    }

    public proxyOnWithSettings(saveFilter : Filter) : Observable<UiState> {
        return this.httpClient.post<UiState>(`api/proxy/on/with-settings`, saveFilter)
            .pipe(
                take(1)
            );
    }

    public proxyOff() : Observable<UiState> {
        return this.httpClient.post<UiState>(`api/proxy/off`, null)
            .pipe(
                take(1)
            );
    }

    public formattersGet(exchangeId : number) : Observable<FormatterContainerViewModel> {
        return this.httpClient.get<FormatterContainerViewModel>(`api/producers/formatters/${exchangeId}`)
            .pipe(
                take(1),
                catchError( (_) => {
                    return of({
                        contextInfo : null,
                        requests : [],
                        responses : []
                    });
                })
            );
    }

    public exchangeGet(exchangeId: number) : Observable<ExchangeInfo> {
        return this.httpClient.get<ExchangeInfo>(`api/exchange/${exchangeId}`).pipe(take(1));
    }

    public exchangeGetCurlCommandResult(exchangeId: number) : Observable<CurlCommandResult> {
        return this.httpClient.get<CurlCommandResult>(`api/exchange/${exchangeId}/curl`).pipe(take(1));
    }

    public exchangeReplay(exchangeId: number, runInLiveEdit : boolean) : Observable<boolean> {
        return this.httpClient.post<boolean>(`api/exchange/${exchangeId}/replay?runInLiveEdit=${runInLiveEdit}`, null).pipe(take(1));
    }

    public exchangeMetrics(exchangeId: number) : Observable<ExchangeMetricInfo> {
        return this.httpClient.get<ExchangeMetricInfo>(`api/exchange/${exchangeId}/metrics`).pipe(take(1));
    }

    public exchangeSaveRequestBody(exchangeId: number, fileName : string) : Observable<boolean> {
        return this.httpClient.post<boolean>(`api/exchange/${exchangeId}/save-request-body`, {
            fileName : fileName
        }).pipe(take(1));
    }

    public exchangeSaveCurlPayload(exchangeId: number, fileId : string, fileName : string) : Observable<boolean> {
        return this.httpClient.post<boolean>(`api/exchange/${exchangeId}/save-curl-payload/${fileId}`, {
            fileName : fileName
        }).pipe(take(1));
    }

    public exchangeSaveResponseBody(exchangeId: number, fileName : string, decode : boolean) : Observable<boolean> {
        return this.httpClient.post<boolean>(`api/exchange/${exchangeId}/save-response-body?decode=${decode}`, {
            fileName : fileName
        }).pipe(take(1));
    }

    public exchangeSaveWebSocketBody(exchangeId: number, messageId : number, fileName : string, direction : string) : Observable<boolean> {
        return this.httpClient.post<boolean>(`api/exchange/${exchangeId}/save-ws-body/${direction}/${messageId}`, {
            fileName : fileName
        }).pipe(take(1));
    }

    public exchangeSaveMultipartContent(exchangeId: number, fileName: string, model : MultipartItem) : Observable<FormattingResult[]> {
        let payload : SaveFileMultipartActionModel = {
            filePath : fileName,
            offset : model.offset,
            length : model.length
        };

        return this.httpClient.post<FormattingResult[]>(`api/exchange/${exchangeId}/save-multipart-Content`, payload).pipe(take(1));
    }

    public exchangeHasRequestBody(exchangeId: number) : Observable<boolean> {
        return this.httpClient.get<boolean>(`api/exchange/${exchangeId}/has-request-body`).pipe(take(1));
    }

    public exchangeHasResponseBody(exchangeId: number) : Observable<boolean> {
        return this.httpClient.get<boolean>(`api/exchange/${exchangeId}/has-response-body`).pipe(take(1));
    }

    public exchangeGetSuggestedRequestBodyFileName(exchangeId: number) : Observable<string> {
        return this.httpClient.get<string>(`api/exchange/${exchangeId}/suggested-request-body-file-name`).pipe(take(1));
    }

    public exchangeGetSuggestedResponseBodyFileName(exchangeId: number) : Observable<string> {
        return this.httpClient.get<string>(`api/exchange/${exchangeId}/suggested-response-body-file-name`).pipe(take(1));
    }

    public connectionGet(connectionId: number) : Observable<ConnectionInfo> {
        return this.httpClient.get<ConnectionInfo>(`api/connection/${connectionId}`).pipe(take(1));
    }

    public settingGet() : Observable<FluxzySettingsHolder> {
        return this.httpClient.get<FluxzySettingsHolder>(`api/setting`).pipe(take(1));
    }


    public settingGetEndPoints() : Observable<NetworkInterfaceInfo[]> {
        return this.httpClient.get<NetworkInterfaceInfo[]>(`api/setting/endpoint`).pipe(take(1));
    }

    public settingUpdate(model : FluxzySettingsHolder) : Observable<boolean> {
        return this.httpClient.post<boolean>(`api/setting`, model).pipe(take(1));
    }

    public viewFilterGet() : Observable<StoredFilter[]> {
        return this.httpClient.get<StoredFilter[]>(`api/view-filter/`).pipe(take(1));
    }

    public viewFilterPatch(filterHolders : FilterHolder []) : Observable<boolean> {
        return this.httpClient.patch<boolean>(`api/view-filter/store`,filterHolders).pipe(take(1));
    }

    public filterGetTemplates() : Observable<FilterTemplate[]> {
        return this.httpClient.get<FilterTemplate[]>(`api/filter/templates`).pipe(take(1));
    }

    public filterGetAnyTemplate() : Observable<AnyFilter> {
        return this.httpClient.get<AnyFilter>(`api/filter/templates/any`).pipe(take(1));
    }

    public filterValidate(filter: Filter) : Observable<Filter> {
        return this.httpClient.post<Filter>(`api/filter/validate`, filter).pipe(take(1));
    }

    public filterApplyToViewUrlSearch( model : FullUrlSearchViewModel, and : boolean) : Observable<boolean> {
        return this.httpClient.post<boolean>(`api/filter/apply/url-search?and=${and}` , model).pipe(take(1));
    }

    public filterApplyToview(filter: Filter) : Observable<boolean> {
        return this.httpClient.post<boolean>(`api/filter/apply/regular`, filter).pipe(take(1));
    }

    public filterApplyToViewAnd(filter: Filter) : Observable<boolean> {
        return this.httpClient.post<boolean>(`api/filter/apply/regular/and`, filter).pipe(take(1));
    }

    public filterApplyToViewOr(filter: Filter) : Observable<boolean> {
        return this.httpClient.post<boolean>(`api/filter/apply/regular/or`, filter).pipe(take(1));
    }

    public filterApplySource(filter: Filter) : Observable<boolean> {
        return this.httpClient.post<boolean>(`api/filter/apply/source`, filter).pipe(take(1));
    }

    public filterApplyResetSource() : Observable<boolean> {
        return this.httpClient.delete<boolean>(`api/filter/apply/source`).pipe(take(1));
    }

    public contextMenuGetActions(exchangeId : number) : Observable<ContextMenuAction[]> {
        return this.httpClient.get<ContextMenuAction[]>(`api/context-menu/${exchangeId}`).pipe(take(1));
    }

    public forwardMessageConsume() : Observable<ForwardMessage[]> {
        return this.httpClient.post<ForwardMessage[]>(`api/forward-message/consume`, null).pipe(take(1));
    }

    public ruleGetContainer() : Observable<RuleContainer[]> {
        return this.httpClient.get<RuleContainer[]>(`api/rule/container`).pipe(take(1)) ;
    }

    public ruleValidate(rule : Rule) : Observable<Rule> {
        return this.httpClient.post<Rule>(`api/rule/validation`, rule).pipe(take(1)) ;
    }

    public ruleCreateFromAction(action : Action) : Observable<Rule> {
        return this.httpClient.post<Rule>(`api/rule`, action).pipe(take(1)) ;
    }

    public ruleUpdateContainer(containers : RuleContainer[]) : Observable<boolean> {
        return this.httpClient.post<boolean>(`api/rule/container`,containers).pipe(take(1)) ;
    }

    public ruleAddToExisting(rule : Rule) : Observable<boolean> {
        return this.httpClient.post<boolean>(`api/rule/container/add`,rule).pipe(take(1)) ;
    }

    public actionValidate(action: Action) : Observable<Action> {
        return this.httpClient.post<Action>(`api/rule/action/validate`,action).pipe(take(1)) ;
    }

    public actionGetTemplates() : Observable<Action[]> {
        return this.httpClient.get<Action[]>(`api/rule/action`).pipe(take(1)) ;
    }

    public metaInfoGet() : Observable<ArchiveMetaInformation> {
        return this.httpClient.get<ArchiveMetaInformation>(`api/meta-info`).pipe(take(1)) ;
    }

    public metaInfoCreateTag(model : TagUpdateModel) : Observable<Tag> {
        return this.httpClient.post<Tag>(`api/meta-info/tag`, model).pipe(take(1)) ;
    }

    public metaInfoUpdateTag(tagIdentifier : string, model : TagUpdateModel) : Observable<boolean> {
        return this.httpClient.patch<boolean>(`api/meta-info/tag/${tagIdentifier}`, model).pipe(take(1)) ;
    }

    public metaInfoApplyTag(tagIdentifier : string, exchangeIds : number[]) : Observable<boolean> {
        return this.httpClient.post<boolean>(`api/meta-info/tag/${tagIdentifier}`, exchangeIds).pipe(take(1)) ;
    }

    public metaInfoGlobalApply(model : TagGlobalApplyModel) : Observable<boolean> {
        return this.httpClient.post<boolean>(`api/meta-info/tag/apply`, model).pipe(take(1)) ;
    }

    public metaInfoApplyComment(model : CommentUpdateModel) : Observable<boolean> {
        return this.httpClient.post<boolean>(`api/meta-info/comment/`, model).pipe(take(1)) ;
    }

    public extendedControlCheckCertificate(certificate : Certificate) : Observable<CertificateValidationResult> {
        return this.httpClient.post<CertificateValidationResult>(`api/extended-control/certificate/`, certificate).pipe(take(1)) ;
    }

    public systemGetCertificates(caOnly : boolean = false) : Observable<CertificateOnStore[]> {
        return this.httpClient.get<CertificateOnStore[]>(`api/system/certificates?caOnly=${caOnly}`).pipe(take(1)) ;
    }

    public connectionHasRawCapture(connectionId : number) : Observable<boolean> {
        return this.httpClient.get<boolean>(`api/connection/${connectionId}/capture/check`).pipe(take(1)) ;
    }

    public connectionGetRawCapture(connectionId : number, fileName : string) : Observable<boolean> {
        return this.httpClient.post<boolean>(`api/connection/${connectionId}/capture/save`, {
            fileName
        }).pipe(take(1)) ;
    }

    public connectionGetRawCaptureKeys(connectionId: number) : Observable<string | null> {
        return this.httpClient.post<string | null>(`api/connection/${connectionId}/capture/key`, null).pipe(take(1)) ;
    }

    public connectionOpenRawCapture(connectionId: number, withKey: boolean) : Observable<boolean> {
        return this.httpClient.post<boolean>(`api/connection/${connectionId}/capture/open?withKey=${withKey}`, null).pipe(take(1)) ;
    }

    public actionLongDescription(typeKind : string) : Observable<DescriptionInfo>{
        return this.httpClient.get<DescriptionInfo>(`api/action/description/${typeKind}`).pipe(take(1)) ;
    }

    public wizardShouldAskCertificate() : Observable<CertificateWizardStatus> {
        return this.httpClient.get<CertificateWizardStatus>(`api/wizard/certificate/check`).pipe(take(1)) ;
    }

    public wizardCheckRawCapture() : Observable<boolean> {
        return this.httpClient.get<boolean>(`api/wizard/raw-capture/check`).pipe(take(1)) ;
    }

    public wizardInstallCertificate() : Observable<boolean> {
        return this.httpClient.post<boolean>(`api/wizard/certificate/install`, null).pipe(take(1)) ;
    }

    public wizardRefuse() : Observable<boolean> {
        return this.httpClient.post<boolean>(`api/wizard/certificate/refuse`, null).pipe(take(1)) ;
    }

    public wizardRevive() : Observable<boolean> {
        return this.httpClient.post<boolean>(`api/wizard/certificate/revive`, null).pipe(take(1)) ;
    }

    public breakPointActiveBreakPoints() : Observable<Rule[]> {
        return this.httpClient.get<Rule[]>(`api/breakpoint`).pipe(take(1)) ;
    }

    public breakPointAdd(filter: Filter) : Observable<boolean> {
        return this.httpClient.post<boolean>(`api/breakpoint`, filter).pipe(take(1)) ;
    }

    public breakPointBreakAll() : Observable<boolean> {
        return this.httpClient.post<boolean>(`api/breakpoint/all`, null).pipe(take(1)) ;
    }

    public breakPointDelete(filterId: string) : Observable<boolean> {
        return this.httpClient.delete<boolean>(`api/breakpoint/${filterId}`).pipe(take(1)) ;
    }

    public breakPointDeleteAll() : Observable<boolean> {
        return this.httpClient.delete<boolean>(`api/breakpoint/delete/all`).pipe(take(1)) ;
    }

    public breakPointDeleteMultiple(filterIds: string[]) : Observable<boolean> {
        return this.httpClient.post<boolean>(`api/breakpoint/delete`, filterIds).pipe(take(1)) ;
    }

    public breakPointResumeDeleteAll() : Observable<boolean> {
        return this.httpClient.delete<boolean>(`api/breakpoint/clear`).pipe(take(1)) ;
    }

    public breakPointDeleteAllDone() : Observable<boolean> {
        return this.httpClient.delete<boolean>(`api/breakpoint/clear-done`).pipe(take(1)) ;
    }

    public breakPointContinueAll() : Observable<boolean> {
        return this.httpClient.post<boolean>(`api/breakpoint/continue-all`, null).pipe(take(1)) ;
    }

    public breakPointContinueUntilEnd(exchangeId : number) : Observable<boolean> {
        return this.httpClient.post<boolean>(`api/breakpoint/${exchangeId}/continue`, null).pipe(take(1)) ;
    }
    public breakPointContinueUntilBreakPoint(exchangeId : number, location : string) : Observable<boolean> {
        return this.httpClient.post<boolean>(`api/breakpoint/${exchangeId}/continue/until/${location}`, null).pipe(take(1)) ;
    }

    public breakPointContinueOnce(exchangeId : number) : Observable<boolean> {
        return this.httpClient.post<boolean>(`api/breakpoint/${exchangeId}/continue/once`, null).pipe(take(1)) ;
    }

    public breakPointEndPointSet(exchangeId : number, model : ConnectionSetupStepModel) : Observable<boolean> {
        return this.httpClient.post<boolean>(`api/breakpoint/${exchangeId}/endpoint`, model).pipe(take(1)) ;
    }

    public breakPointEndPointContinue(exchangeId : number) : Observable<boolean> {
        return this.httpClient.post<boolean>(`api/breakpoint/${exchangeId}/endpoint/continue`, null).pipe(take(1)) ;
    }

    public breakPointSetRequest(exchangeId : number, model : RequestSetupStepModel) : Observable<boolean> {
        return this.httpClient.post<boolean>(`api/breakpoint/${exchangeId}/request`, model).pipe(take(1)) ;
    }

    public breakPointSetResponse(exchangeId : number, model : ResponseSetupStepModel) : Observable<boolean> {
        return this.httpClient.post<boolean>(`api/breakpoint/${exchangeId}/response`, model).pipe(take(1)) ;
    }

    public breakPointContinueRequest(exchangeId : number) : Observable<boolean> {
        return this.httpClient.post<boolean>(`api/breakpoint/${exchangeId}/request/continue`, null).pipe(take(1)) ;
    }

    public breakPointContinueResponse(exchangeId : number) : Observable<boolean> {
        return this.httpClient.post<boolean>(`api/breakpoint/${exchangeId}/response/continue`, null).pipe(take(1)) ;
    }

    public quickActionList() : Observable<QuickActionResult> {
        return this.httpClient.get<QuickActionResult>(`api/quickaction`).pipe(take(1));
    }

    public quickActionListStatic() : Observable<QuickActionResult> {
        return this.httpClient.get<QuickActionResult>(`api/quickaction/static`).pipe(take(1));
    }

    public uiSettingGet(key : string) : Observable<string> {
        return  this.httpClient.get<UiSetting>(`api/setting/ui/${key}`).pipe(
            take(1), map((setting : UiSetting) => setting.value));
    }

    public uiSettingHasKey(key : string) : Observable<boolean> {
        return this.httpClient.get<boolean>(`api/setting/ui/${key}/contains`).pipe(take(1));
    }

    public uiSettingUpdate(key : string, value : string) : Observable<boolean> {
        const payload : UiSetting = {
            value
        } ;
        
        return this.httpClient
            .put<boolean>(`api/setting/ui/${key}`,payload).pipe(take(1));
    }
}