import { Injectable } from '@angular/core';
import { Observable, Subject } from 'rxjs';
import { TrunkState } from '../core/models/auto-generated';

@Injectable({
    providedIn: 'root',
})
export class ExchangeContentService {
  
    private trunkState$ = new Subject<TrunkState>() ; 
    private trunkStateObservable$ : Observable<TrunkState> ; 

    
    constructor() {
      this.trunkStateObservable$ = this.trunkState$.asObservable() ; 
    }

    public update(trunkState : TrunkState) : void {
        this.trunkState$.next(trunkState) ; 
    }

    public getTrunkState() : Observable<TrunkState> {
      return this.trunkStateObservable$ ; 
    }
}
