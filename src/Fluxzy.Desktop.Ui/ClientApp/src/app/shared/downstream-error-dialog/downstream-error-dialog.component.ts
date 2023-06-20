import {Component, OnInit} from '@angular/core';
import {BsModalRef, ModalOptions} from "ngx-bootstrap/modal";
import {MessageDialogModel} from "../error-dialog/message-dialog.component";
import {ApiService} from "../../services/api.service";
import {switchMap, tap} from "rxjs";
import {DownstreamErrorInfo} from "../../core/models/auto-generated";

@Component({
    selector: 'app-downstream-error-dialog',
    templateUrl: './downstream-error-dialog.component.html',
    styleUrls: ['./downstream-error-dialog.component.scss']
})
export class DownstreamErrorDialogComponent implements OnInit {

    public errors: DownstreamErrorInfo[] | null = null;
    public index : number = 0 ;
    public limit : number = 10 ;
    public readonly limitFinal : number = 10 ;

    constructor(
        public bsModalRef: BsModalRef,
        public options: ModalOptions,
        private apiService : ApiService,
    ) {
    }

    computeBoundary () : void {
        this.limit =  this.index + this.limitFinal ;

        if (this.limit > this.errors.length) {
            this.limit = this.errors.length ;
        }
    }

    next (): void {
        this.index += this.limitFinal ;
        this.computeBoundary() ;
    }

    previous (): void {
        this.index -= this.limitFinal ;

        if (this.index < 0) {
            this.index = 0 ;
        }

        this.computeBoundary() ;
    }

    public goFirst () : void {
        this.index = 0 ;
        this.computeBoundary() ;
    }

    public goLast () : void {
        this.index = this.errors.length - this.limitFinal ;
        this.computeBoundary() ;
    }

    public canGoFirst() : boolean {
        return this.index > 0 ;
    }

    public canGoLast() : boolean {
        return this.index + this.limitFinal < this.errors.length ;
    }

    public canGoNext () : boolean {
        return this.index + this.limitFinal < this.errors.length ;
    }

    public canGoPrevious () : boolean {
        return this.index > 0 ;
    }



    ngOnInit(): void {
        this.apiService.readErrors()
            .pipe(
                tap(t => this.errors = t),
            ).subscribe() ;
    }

    close() {
        this.bsModalRef.hide();
    }

    refresh() {
        this.errors = null ;
        this.apiService.readErrors()
            .pipe(
                tap(t => this.errors = t),
                tap(t => this.computeBoundary())
            ).subscribe() ;
    }

    clearAll() {
        this.errors = null ;

        this.apiService.clearErrors()
            .pipe(
                switchMap(t => this.apiService.readErrors()),
                 tap(t => this.errors = t),
                 tap(t => this.index = 0),
                 tap(t => this.computeBoundary())
            ).subscribe() ;
    }
}
