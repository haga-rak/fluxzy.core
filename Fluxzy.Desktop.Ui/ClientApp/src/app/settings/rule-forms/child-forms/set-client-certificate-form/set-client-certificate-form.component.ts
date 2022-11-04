import {ChangeDetectionStrategy, ChangeDetectorRef, Component} from '@angular/core';
import {ActionValidationTargetComponent} from "../../rule-edit/rule-edit.component";
import {CertificateValidationResult, SetClientCertificateAction} from "../../../../core/models/auto-generated";
import {ApiService} from "../../../../services/api.service";
import {DialogService} from "../../../../services/dialog.service";
import {SystemCallService} from "../../../../core/services/system-call.service";
import { filter, take, tap } from 'rxjs';

@Component({
    selector: 'app-set-client-certificate-form',
    templateUrl: './set-client-certificate-form.component.html',
    styleUrls: ['./set-client-certificate-form.component.scss'],
    changeDetection: ChangeDetectionStrategy.OnPush
})
export class SetClientCertificateFormComponent extends ActionValidationTargetComponent<SetClientCertificateAction> {
    public validationState = {} ;
   // public retrieveModeType : string [] =  ['FromUserStoreSerialNumber', 'FromPkcs12'];
    private certificateValidationResult: CertificateValidationResult;



    constructor(private cd : ChangeDetectorRef, private apiService : ApiService, private dialogService : DialogService,
                private systemCallService : SystemCallService) {
        super();
    }

    public actionInit(): void {
    }

    public override validate(): string | null {
        let onError = false;

        if (this.action.clientCertificate.retrieveMode === 'FromUserStoreSerialNumber') {
            if (!this.action.clientCertificate.serialNumber) {

                this.validationState['serialNumber'] = 'serial number must be defined' ;
                onError = true;
            }
        }

        if (this.action.clientCertificate.retrieveMode === 'FromPkcs12') {
            if (!this.action.clientCertificate.pkcs12File) {

                this.validationState['pkcs12File'] = 'You must select a file' ;
                onError = true;
            }
        }

        this.cd.detectChanges();

        return onError ? 'Some fields are invalid' : null;
    }

    public selectFile() : void {
        this.systemCallService.requestFileOpen('PKCS#12 file', ['p12', 'pfx'])
            .pipe(
                take(1),
                filter(t => !!t),
                tap(t => this.action.clientCertificate.pkcs12File = t),
                tap(_ => this.cd.detectChanges())
            ).subscribe();
    }

    public checkCertificate() : void {
        this.apiService.extendedControlCheckCertificate(this.action.clientCertificate)
            .pipe(
                tap(t => this.certificateValidationResult = t),
                tap(_=> this.cd.detectChanges())
            ).subscribe() ;
    }
}
