import {Injectable} from '@angular/core';
import {StatusBarService} from "./status-bar.service";
import {ApiService} from "./api.service";
import {DialogService} from "./dialog.service";
import {filter, switchMap, take, tap} from "rxjs";
import {TagGlobalApplyModel} from "../core/models/auto-generated";
import {ExchangeContentService} from "./exchange-content.service";

@Injectable({
    providedIn: 'root'
})
export class MetaInformationService {

    constructor(
        private apiService: ApiService,
        private statusBarService: StatusBarService,
        private dialogService: DialogService,
        private exchangeContentService: ExchangeContentService
    ) {
    }

    public commentMultiple(exchangeIds: number []): void {
        this.dialogService.openCommentApplyDialog('',
            exchangeIds)
            .pipe(
                take(1),
                filter(c => !!c),
                switchMap(t => this.apiService.metaInfoApplyComment(t)),
                tap(_ => this.statusBarService.addMessage("Comment applied"))
            ).subscribe();
    }

    public comment(exchangeId: number): void {
        const exchangeInfo = this.exchangeContentService.getExchangeInfo(exchangeId);

        if (!exchangeInfo) {
            return;
        }

        this.dialogService.openCommentApplyDialog(exchangeInfo.comment ?? '',
            [exchangeId])
            .pipe(
                take(1),
                filter(c => !!c),
                switchMap(t => this.apiService.metaInfoApplyComment(t)),
                tap(_ => this.statusBarService.addMessage("Comment applied"))
            ).subscribe();
    }

    public tag(exchangeId: number): void {
        const exchangeInfo = this.exchangeContentService.getExchangeInfo(exchangeId);

        if (!exchangeInfo) {
            return;
        }

        this.apiService.exchangeGet(exchangeId)
            .pipe(
                take(1),
                switchMap(e => this.dialogService.openTagApplyDialog({
                    exchangeIds: [exchangeId],
                    tagIdentifiers: e.tags.map(t => t.identifier)
                })
                    .pipe(
                        take(1),
                        filter(t => !!t),
                        switchMap(t => this.apiService.metaInfoGlobalApply(t)),
                        tap(_ => this.statusBarService.addMessage('Tag applied', 1000))
                    ))).subscribe();
    }
}
