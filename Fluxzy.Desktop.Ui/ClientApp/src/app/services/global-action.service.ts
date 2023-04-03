import {Injectable} from '@angular/core';
import {SystemCallService} from "../core/services/system-call.service";
import {ApiService} from "./api.service";
import {filter, Observable, switchMap, take} from "rxjs";

@Injectable({
    providedIn: 'root'
})
export class GlobalActionService {

    constructor(private apiService : ApiService,
                private systemCallService : SystemCallService) {

    }

    public saveRequestBody(exchangeId : number) : Observable<boolean> {
        return this.apiService.exchangeGetSuggestedRequestBodyFileName(exchangeId)
            .pipe(
                take(1),
                switchMap(fileName => this.systemCallService.requestFileSave(fileName)),
                filter(t => !!t),
                switchMap(fileName => this.apiService.exchangeSaveRequestBody(exchangeId, fileName) ),
                take(1),
            ) ;
    }

    public saveResponseBody(exchangeId : number, decode : boolean = true) : Observable<boolean> {
        return this.apiService.exchangeGetSuggestedResponseBodyFileName(exchangeId)
            .pipe(
                take(1),
                switchMap(fileName => this.systemCallService.requestFileSave(fileName)),
                filter(t => !!t),
                switchMap(fileName => this.apiService.exchangeSaveResponseBody(exchangeId, fileName, decode) ),
                take(1),
            ) ;
    }
}
