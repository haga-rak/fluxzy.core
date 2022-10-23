import {Injectable} from '@angular/core';
import {delay, distinctUntilChanged, filter, Observable, of, Subject, switchMap, tap} from 'rxjs';

@Injectable({
    providedIn: 'root'
})
export class StatusBarService {
    private rawPendingMessage$ = new Subject<StatusMessage | null>() ;
    private pendingMessage$ : Observable<StatusMessage | null>;

    constructor() {
        this.rawPendingMessage$.pipe(
            filter (t => !!t),
            switchMap(t => of(null).pipe(delay(2000))),
            tap( _ => this.rawPendingMessage$.next(null)),
           // tap(_ => console.log('sending null'))
        ).subscribe();

        this.pendingMessage$ = this.rawPendingMessage$.asObservable();
    }

    public getPendingMessages(): Observable<StatusMessage | null> {
        return this.pendingMessage$;
    }

    public addMessage(content : string, delayMillis : number = 2000) : void {
        this.rawPendingMessage$.next({
            content,
            delayMillis
        });
    }
}

export interface StatusMessage {
    content : string,
    delayMillis : number
}
