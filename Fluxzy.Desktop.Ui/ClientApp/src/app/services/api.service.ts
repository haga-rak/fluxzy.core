import { HttpClient } from '@angular/common/http';
import { Injectable } from '@angular/core';
import { HttpTransportType, HubConnection, HubConnectionBuilder } from '@microsoft/signalr';
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
    of, delay, BehaviorSubject, pipe
} from 'rxjs';
import {
    Action,
    ConnectionInfo, ContextMenuAction,
    ExchangeBrowsingState,
    ExchangeState,
    FileContentDelete,
    FileSaveViewModel,
    FileState,
    Filter, FilterTemplate,
    FluxzySettingsHolder,
    FormatterContainerViewModel,
    FormattingResult, ForwardMessage,
    MultipartItem, Rule, RuleContainer,
    SaveFileMultipartActionModel,
    StoredFilter,
    TrunkState,
    UiState
} from '../core/models/auto-generated';
import {FilterHolder} from "../settings/manage-filters/manage-filters.component";

@Injectable({
  providedIn: 'root'
})
// This service is responsible of delivering http service towards the .NET web service
export class ApiService {
    private forwardMessages$ = new Subject<ForwardMessage>();
    private loop$ = new BehaviorSubject<any>(null);

    constructor(private httpClient: HttpClient)
    {
        this.loopForwardMessage();
    }

    public loopForwardMessage() : void {

        this.loop$.asObservable()
            .pipe(
              //  tap(_ => console.log('triggered')),
                switchMap(_ =>  this.forwardMessageConsume().
                        pipe(
                            catchError(err =>  of([]).pipe(delay(2000)))
                        )
                ),
                tap(messages => {
                    for (const message of messages) {
                        this.forwardMessages$.next(message);
                    }
                }),
                tap((_) => this.loop$.next(null))
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

    public proxyOn() : Observable<UiState> {
        return this.httpClient.post<UiState>(`api/proxy/on`, null)
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

    public exchangeSaveRequestBody(exchangeId: number, fileName : string) : Observable<boolean> {
        return this.httpClient.post<boolean>(`api/exchange/${exchangeId}/save-request-body`, {
            fileName : fileName
        }).pipe(take(1));
    }

    public exchangeSaveResponseBody(exchangeId: number, fileName : string, decode : boolean) : Observable<boolean> {
        return this.httpClient.post<boolean>(`api/exchange/${exchangeId}/save-response-body?decode=${decode}`, {
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

    public connectionGet(connectionId: number) : Observable<ConnectionInfo> {
        return this.httpClient.get<ConnectionInfo>(`api/connection/${connectionId}`).pipe(take(1));
    }

    public settingGet() : Observable<FluxzySettingsHolder> {
        return this.httpClient.get<FluxzySettingsHolder>(`api/setting`).pipe(take(1));
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

    public filterValidate(filter: Filter) : Observable<Filter> {
        return this.httpClient.post<Filter>(`api/filter/validate`, filter).pipe(take(1));
    }

    public filterApplyToview(filter: Filter) : Observable<boolean> {
        return this.httpClient.post<boolean>(`api/filter/apply-to-view`, filter).pipe(take(1));
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

    public actionValidate(action: Action) : Observable<Action> {
        return this.httpClient.post<Action>(`api/rule/action/validate`,action).pipe(take(1)) ;
    }

    public actionGetTemplates() : Observable<Action[]> {
        return this.httpClient.get<Action[]>(`api/rule/action`).pipe(take(1)) ;
    }
}
