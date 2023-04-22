import {ChangeDetectorRef, Component, OnDestroy, OnInit} from '@angular/core';
import {BsModalRef, ModalOptions} from 'ngx-bootstrap/modal';
import {ApiService} from '../../services/api.service';
import {CertificateWizardStatus} from "../../core/models/auto-generated";
import {switchMap, take, tap} from "rxjs";

@Component({
    selector: 'app-wizard',
    templateUrl: './wizard.component.html',
    styleUrls: ['./wizard.component.scss']
})
export class WizardComponent implements OnInit, OnDestroy {
    public certificateWizardStatus: CertificateWizardStatus;
    private callback: (arg : any) => void;

    constructor(
        public bsModalRef: BsModalRef,
        private apiService: ApiService,
        public cd: ChangeDetectorRef,
        public options: ModalOptions
    ) {
        this.certificateWizardStatus = options.initialState.certificateWizardStatus as CertificateWizardStatus;
        this.callback = options.initialState.callback as (arg : any) => void;
    }

    ngOnInit(): void {

    }

    public installCertificate() : void {

        this.certificateWizardStatus = null ;
        this.cd.detectChanges() ;

        this.apiService.wizardInstallCertificate()
            .pipe(
                take(1),
                switchMap(t => this.apiService.wizardShouldAskCertificate()) ,
                tap(t => this.certificateWizardStatus = t),
                tap(_ => this.cd.detectChanges())
            ).subscribe();
    }

    public neverAskAgain() : void {
        this.bsModalRef.hide();
        this.apiService.wizardRefuse()
            .pipe(
                take(1),
            ).subscribe();
    }

    ngOnDestroy(): void {
        this.callback(this.certificateWizardStatus?.installed ?? false);
    }
}
