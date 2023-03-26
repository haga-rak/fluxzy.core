import {Injectable} from '@angular/core';
import {BehaviorSubject, distinctUntilChanged, Observable} from "rxjs";

@Injectable({
    providedIn: 'root'
})
export class InputService {
    private _keyBoardCtrlOn$ = new BehaviorSubject(false);
    private _keyBoardShiftOn$ = new BehaviorSubject(false);

    constructor() {
    }

    public setKeyboardCtrlOn(value : boolean) : void {
        this._keyBoardCtrlOn$.next(value);
    }

    get keyBoardCtrlOn$(): Observable<boolean> {
        return this._keyBoardCtrlOn$.asObservable().pipe(distinctUntilChanged());
    }

    public setKeyboardShiftOn(value : boolean) : void {
        this._keyBoardShiftOn$.next(value);
    }

    get keyBoardShiftOn$(): Observable<boolean> {
        return this._keyBoardShiftOn$.asObservable().pipe(distinctUntilChanged());
    }
}
