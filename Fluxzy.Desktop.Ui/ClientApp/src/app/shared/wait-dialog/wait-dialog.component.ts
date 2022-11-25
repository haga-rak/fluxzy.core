import {AfterViewInit, ChangeDetectorRef, Component, OnDestroy, OnInit} from '@angular/core';
import {BsModalRef, ModalOptions} from "ngx-bootstrap/modal";
import {ApiService} from "../../services/api.service";
import {DialogService} from "../../services/dialog.service";
import {Rule} from "../../core/models/auto-generated";
import {BehaviorSubject, Subject, Subscription, take, tap} from "rxjs";

@Component({
    selector: 'app-wait-dialog',
    templateUrl: './wait-dialog.component.html',
    styleUrls: ['./wait-dialog.component.scss']
})
export class WaitDialogComponent implements OnInit, OnDestroy, AfterViewInit {
    public message: string;
    private subscription: Subscription;
    private completeListener: BehaviorSubject<any | null>;

    constructor(
        public bsModalRef: BsModalRef,
        public options: ModalOptions,
        private cd: ChangeDetectorRef) {
        this.message = this.options.initialState.message as string;
        this.completeListener = this.options.initialState.completeListener as BehaviorSubject<any | null>;
    }

    ngOnInit(): void {

        this.subscription = this.bsModalRef.onHidden.pipe(
            tap(t => this.cd.detectChanges()),
        ).subscribe();
    }

    ngAfterViewInit(): void {
        this.completeListener.next(1);
    }

    public ngOnDestroy(): void {
        this.subscription.unsubscribe()
    }

}
