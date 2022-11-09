import {
    ChangeDetectorRef,
    Component,
    OnInit,
    TemplateRef,
    ViewChild,
} from '@angular/core';
import { BsModalRef, BsModalService } from 'ngx-bootstrap/modal';
import { filter, tap } from 'rxjs';
import {CertificateOnStore, FluxzySettingsHolder, NetworkInterfaceInfo} from '../../core/models/auto-generated';
import { MenuService } from '../../core/services/menu-service.service';
import { ApiService } from '../../services/api.service';

@Component({
    selector: 'app-global-setting',
    templateUrl: './global-setting.component.html',
    styleUrls: ['./global-setting.component.scss'],
})
export class GlobalSettingComponent implements OnInit {
    public modalRef?: BsModalRef;

    public settingsHolder: FluxzySettingsHolder;

    public hello = 'Bonjour bonjour';
    public networkInterfaceInfos: NetworkInterfaceInfo[];

    public validationMessages : string [] ;
    private caStoreCertificates : CertificateOnStore[];


    constructor(
        public bsModalRef: BsModalRef,
        private apiService: ApiService,
        private cd : ChangeDetectorRef
    ) {}

    ngOnInit(): void {
        this.apiService
            .settingGet()
            .pipe(tap((t) => (this.settingsHolder = t)))
            .pipe(tap((t) => this.cd.detectChanges()))
            .subscribe();

        this.apiService.settingGetEndPoints()
            .pipe(
                tap(t => this.networkInterfaceInfos = t),
                tap(_ => this.cd.detectChanges())
            ).subscribe();

        this.apiService.systemGetCertificates(true)
            .pipe(
                tap(t => this.caStoreCertificates = t),
                tap(_ => this.cd.detectChanges())
            ).subscribe();
    }

    public isInterfaceSelected(ipAddress : string) : boolean {
        let res = this.settingsHolder.viewModel.addresses.filter(a => a === ipAddress).length > 0 ;
        return res;
    }

    public selectInterface(ipAddress: string) : void {

        if (this.settingsHolder.viewModel.addresses.filter(t => t === ipAddress).length === 1){
            this.settingsHolder.viewModel.addresses = this.settingsHolder.viewModel.addresses.filter(t => t !== ipAddress);
            this.cd.detectChanges();
            return;
        }
        this.settingsHolder.viewModel.addresses.push(ipAddress)
        this.cd.detectChanges();
        // console.log(ipAddress);
        // console.log(this.settingsHolder.viewModel);
    }

    public validate () : boolean {
        this.validationMessages = [];

        if (this.settingsHolder.viewModel.listenType === 'SelectiveAddress'
          && this.settingsHolder.viewModel.addresses.length === 0) {
            this.validationMessages.push('You must select at least one network interface');
        }

        this.cd.detectChanges();



        return this.validationMessages.length === 0;
    }


    public save() : void {
        if (!this.validate())
            return;

        // Update

        this.bsModalRef.hide();
    }
}
