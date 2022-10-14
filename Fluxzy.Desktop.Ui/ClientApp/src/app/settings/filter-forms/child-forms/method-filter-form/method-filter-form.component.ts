import { Component, Input, OnDestroy, OnInit } from '@angular/core';
import { Observable, Subscription, tap } from 'rxjs';
import { Filter, FilterCollection, MethodFilter } from '../../../../core/models/auto-generated';
import { IValidationSource, ValidationTargetComponent } from '../../filter-edit/filter-edit.component';

@Component({
    selector: 'app-method-filter-form',
    templateUrl: './method-filter-form.component.html',
    styleUrls: ['./method-filter-form.component.scss'],
})
export class MethodFilterFormComponent extends  ValidationTargetComponent<MethodFilter> {

    public methods = ['GET', 'POST', 'PUT', 'PATCH', 'DELETE', 'COPY', 'HEAD', 'OPTIONS', 'LINK', 'UNLINK', 'PURGE', 'LOCK', 'UNLOCK', 'PROPFIND', 'VIEW'];

    constructor() {
      super();
    }

    public filterInit(): void {
    }

    public validate(): string | null {
        if (this.methods.filter(m => m === this.filter.pattern.toUpperCase()).length > 0 ){
          return '';
        }

        return `Method must be one of the following values : ${this.methods.join(", ")}`;
    }
}
