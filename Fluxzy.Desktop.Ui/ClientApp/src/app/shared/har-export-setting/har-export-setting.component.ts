import {ChangeDetectorRef, Component, OnInit} from '@angular/core';
import { HttpArchiveSavingSetting } from '../../core/models/auto-generated';
import {BsModalRef, ModalOptions} from "ngx-bootstrap/modal";

@Component({
    selector: 'app-har-export-setting',
    templateUrl: './har-export-setting.component.html',
    styleUrls: ['./har-export-setting.component.scss']
})
export class HarExportSettingComponent implements OnInit {
    public saveSetting : HttpArchiveSavingSetting = {
        policy : 'SkipBody',
        harLimitMaxBodyLength : 1024 * 512 ,
        default : null
    };
    private readonly callBack: (f: (HttpArchiveSavingSetting | null)) => void;

    constructor(public bsModalRef: BsModalRef,
                public options: ModalOptions,
                public cd: ChangeDetectorRef) {
        this.callBack = this.options.initialState.callBack as (f : HttpArchiveSavingSetting | null) => void ;
    }

    ngOnInit(): void {

    }

    save() {
        this.callBack(this.saveSetting) ;
        this.bsModalRef.hide();
    }

    cancel() {
        this.callBack(null);
        this.bsModalRef.hide();
    }
}
