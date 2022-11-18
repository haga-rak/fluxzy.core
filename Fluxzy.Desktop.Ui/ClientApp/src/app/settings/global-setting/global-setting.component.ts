import {
    AfterViewInit,
    ChangeDetectorRef,
    Component,
    ElementRef,
    OnInit,
    TemplateRef,
    ViewChild,
} from '@angular/core';
import { BsModalRef, BsModalService } from 'ngx-bootstrap/modal';
import {filter, map, Observable, pipe, switchMap, take, tap} from 'rxjs';
import {
    CertificateOnStore,
    CertificateValidationResult,
    FluxzySettingsHolder,
    NetworkInterfaceInfo
} from '../../core/models/auto-generated';
import { MenuService } from '../../core/services/menu-service.service';
import { ApiService } from '../../services/api.service';
import {SystemCallService} from "../../core/services/system-call.service";
import { PerfectScrollbarComponent } from 'ngx-perfect-scrollbar';

@Component({
    selector: 'app-global-setting',
    templateUrl: './global-setting.component.html',
    styleUrls: ['./global-setting.component.scss'],
})
export class GlobalSettingComponent implements OnInit, AfterViewInit  {
    public modalRef?: BsModalRef;

    public settingsHolder: FluxzySettingsHolder;

    public hello = 'Bonjour bonjour';
    public networkInterfaceInfos: NetworkInterfaceInfo[];

    public validationMessages : string [] ;
    private caStoreCertificates : CertificateOnStore[];
    private certificateValidationResult : CertificateValidationResult ;

    public leftMenus : LeftMenu[] = [] ;

    @ViewChild('bindingSection') bindingSection ;
    @ViewChild('rootCaSection') rootCaSection ;
    @ViewChild('rawPacket') rawPacket ;

    @ViewChild('perfectScroll') perfectScroll: PerfectScrollbarComponent;

    constructor(
        public bsModalRef: BsModalRef,
        private apiService: ApiService,
        private cd : ChangeDetectorRef,
        private systemCallService : SystemCallService
    ) {}

    ngAfterViewInit(): void {
        this.initViewRefs();

        console.log(this.leftMenus);
    }

    private initViewRefs() {
        this.leftMenus = [
            {
                targetRef: this.bindingSection,
                label: "Binding"
            },
            {
                targetRef: this.rootCaSection,
                label: "Root CA configuration"
            },
            {
                targetRef: this.rawPacket,
                label: "Raw packet capture"
            },
        ];
    }

    ngOnInit(): void {
        this.apiService
            .settingGet()
            .pipe(
                tap((t) => (this.settingsHolder = t)),
                tap((t) => this.cd.detectChanges()),
                tap( _ => this.initViewRefs())
                )
            .subscribe();

        this.apiService.settingGetEndPoints()
            .pipe(
                tap(t => this.networkInterfaceInfos = t),
                tap(_ => this.cd.detectChanges())
            ).subscribe();

        this.apiService.systemGetCertificates(true)
            .pipe(
                tap(t => this.caStoreCertificates = t),
                tap(_ => this.cd.detectChanges()),
                tap (_ => this.requestCertificateValidation())
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

    public validateCertificate() : Observable<boolean> {
        this.certificateValidationResult = null ;
        return this.apiService
            .extendedControlCheckCertificate(this.settingsHolder.startupSetting.caCertificate)
            .pipe(
                tap (t => this.certificateValidationResult = t),
                tap (_ => this.cd.detectChanges()),
                map(t => !!t.subjectName)
            );
    }

    public requestCertificateValidation() : void {
        this.validateCertificate()
            .subscribe();
    }

    public syncValidation () : boolean {
        this.validationMessages = [];

        if (this.settingsHolder.viewModel.listenType === 'SelectiveAddress'
          && this.settingsHolder.viewModel.addresses.length === 0) {
            this.validationMessages.push('You must select at least one network interface');
        }

        this.cd.detectChanges();
        return this.validationMessages.length === 0;
    }

    public save() : void {
        let syncValidation = this.syncValidation() ;

        this.validateCertificate()
            .pipe(
                map(t => t && syncValidation),
                filter (t => t),
                switchMap(t => this.apiService.settingUpdate(this.settingsHolder)),
                tap(_ => this.bsModalRef.hide())
            ).subscribe();

    }

    public selectLocalCertificate() : void {
        this.systemCallService.requestFileOpen('PKCS#12 file', ['p12', 'pfx'])
            .pipe(
                take(1),
                filter(t => !!t),
                tap(t => this.settingsHolder.startupSetting.caCertificate.pkcs12File = t),
                tap(_ => this.cd.detectChanges())
            ).subscribe();
    }

    public scrollToElement(element : ElementRef) : void {
        console.log(element)


        let top  : number = element.nativeElement.offsetTop - 50
        this.perfectScroll.directiveRef.scrollToY(top, 0);

    }

}


interface LeftMenu {
    targetRef : ElementRef ;
    label : string;
}
