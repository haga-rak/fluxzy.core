import {ChangeDetectorRef, Component, OnInit} from '@angular/core';
import {BsModalRef, ModalOptions} from "ngx-bootstrap/modal";
import {ElectronService} from "../../core/services";
import {ApiService} from "../../services/api.service";
import {tap} from "rxjs";
import {AppVersion} from "../../core/models/auto-generated";

@Component({
    selector: 'app-about',
    templateUrl: './about.component.html',
    styleUrls: ['./about.component.scss']
})
export class AboutComponent implements OnInit {
    public version: AppVersion | null;

    constructor(
        public bsModalRef: BsModalRef,
        public options: ModalOptions,
        private apiService: ApiService,
        private cd : ChangeDetectorRef) {
    }

    ngOnInit(): void {
        this.apiService.systemGetVersion()
            .pipe(
                tap(t => this.version = t),
                tap(t => this.cd.detectChanges())
            ).subscribe();
    }

    close() {
        this.bsModalRef.hide();
    }
}
