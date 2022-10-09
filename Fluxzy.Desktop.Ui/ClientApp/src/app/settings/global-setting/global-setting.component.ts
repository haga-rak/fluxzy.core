import {
    ChangeDetectorRef,
    Component,
    OnInit,
    TemplateRef,
    ViewChild,
} from '@angular/core';
import { BsModalRef, BsModalService } from 'ngx-bootstrap/modal';
import { filter, tap } from 'rxjs';
import { FluxzySettingsHolder } from '../../core/models/auto-generated';
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
    }
}
