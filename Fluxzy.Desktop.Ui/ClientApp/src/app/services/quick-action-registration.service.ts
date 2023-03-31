import {Injectable} from '@angular/core';
import {QuickActionService} from "./quick-action.service";
import {ApiService} from "./api.service";
import {DialogService} from "./dialog.service";
import {concatMap, from, of, switchMap} from "rxjs";

@Injectable({
    providedIn: 'root'
})
export class QuickActionRegistrationService {

    constructor(
        private quickActionService : QuickActionService,
        private apiService : ApiService,
        private dialogService : DialogService) {
    }

    public register() : void {

        this.quickActionService.registerLocalAction(
            "global-settings", "Settings", "Access global settings", false,
            { callBack : (exchangeIds : number []) => {
                    this.dialogService.openGlobalSettings()
                }}
        );

        this.quickActionService.registerLocalAction(
            "manage-rules", "Settings", "Manage rules", false,
            { callBack : (exchangeIds : number []) => {
                    this.dialogService.openManageRules();
                }}
        );

        this.quickActionService.registerLocalAction(
            "manage-filters", "Settings", "Manage computer saved filters", false,
            { callBack : (exchangeIds : number []) => {
                    this.dialogService.openManageFilters(false).subscribe(); ;
                }}
        );

        this.quickActionService.registerLocalAction(
            "replay-request", "Replay", "Replay selected requests", true,
            { callBack : (exchangeIds : number []) => {
                    if (exchangeIds.length){
                        from (exchangeIds).pipe(
                            concatMap(ids => this.apiService.exchangeReplay(ids, false)))
                            .subscribe();
                    }
                }}
        );
    }
}
