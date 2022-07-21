import { Injectable } from '@angular/core';
import { Subject } from 'rxjs';

@Injectable({
    providedIn: 'root'
})
export class ToggleService {    
    public groupState : Subject<IToogleState> = new Subject();
    constructor() { }
}

export interface IToogleState {
    groupName : string ; 
    id :  string; 
}