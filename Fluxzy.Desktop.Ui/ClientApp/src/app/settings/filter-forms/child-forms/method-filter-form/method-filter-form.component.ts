import { Component, Input, OnInit } from '@angular/core';
import { Observable } from 'rxjs';
import { Filter, MethodFilter } from '../../../../core/models/auto-generated';

@Component({
    selector: 'app-method-filter-form',
    templateUrl: './method-filter-form.component.html',
    styleUrls: ['./method-filter-form.component.scss'],
})
export class MethodFilterFormComponent implements OnInit {
    @Input() rawFilter : Filter ; 
    @Input() validateRequest : Observable<boolean> ; 

    public filter : MethodFilter ; 

    constructor() {}

    ngOnInit(): void {
        this.filter = this.rawFilter as MethodFilter ; 
    }
}
