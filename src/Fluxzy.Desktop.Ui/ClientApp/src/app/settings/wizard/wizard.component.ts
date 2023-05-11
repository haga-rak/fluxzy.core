import {ChangeDetectorRef, Component, OnDestroy, OnInit} from '@angular/core';
import {BsModalRef, ModalOptions} from 'ngx-bootstrap/modal';
import {ApiService} from '../../services/api.service';
import {CertificateWizardStatus} from "../../core/models/auto-generated";
import {filter, switchMap, take, tap} from "rxjs";
import {SystemCallService} from "../../core/services/system-call.service";

@Component({
    selector: 'app-wizard',
    templateUrl: './wizard.component.html',
    styleUrls: ['./wizard.component.scss']
})
export class WizardComponent implements OnInit, OnDestroy {
    public certificateWizardStatus: CertificateWizardStatus;
    private readonly callback: (arg : any) => void;

    constructor(
        public bsModalRef: BsModalRef,
        private apiService: ApiService,
        public cd: ChangeDetectorRef,
        private systemCallService : SystemCallService,
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

    saveToFile(friendlyName: string) {
        this.systemCallService.requestFileSave(`${friendlyName}.cer`)
            .pipe(
                take(1),
                filter(t => !!t),
              switchMap(t => this.apiService.systemSaveCaCertificate( { fileName : t } )),
            ).subscribe();
    }
}
