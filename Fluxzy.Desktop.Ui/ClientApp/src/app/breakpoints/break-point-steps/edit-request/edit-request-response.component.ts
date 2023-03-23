import {ChangeDetectorRef, Component, Input, OnInit, SimpleChanges, ViewChild} from '@angular/core';
import {
    BreakPointContextInfo,
    BreakPointContextStepInfo,
    RequestSetupStepModel
} from "../../../core/models/auto-generated";
import {ApiService} from "../../../services/api.service";
import {filter, tap} from "rxjs";
import {HeaderEditorComponent} from "../../../shared/header-editor/header-editor.component";
import {SystemCallService} from "../../../core/services/system-call.service";
import {HeaderService} from "../../../shared/header-editor/header.service";

@Component({
    selector: 'app-edit-request',
    templateUrl: './edit-request-response.component.html',
    styleUrls: ['./edit-request-response.component.scss']
})
export class EditRequestResponseComponent implements OnInit {

    @Input() public context : BreakPointContextInfo ;
    @Input() public stepInfo : BreakPointContextStepInfo;
    @Input() public isRequest = true;

    public model: RequestSetupStepModel | null;
    public done : boolean = false;

    public headerShown = true;

    @ViewChild('editor') editor : HeaderEditorComponent;

    constructor(private apiService : ApiService, private cd : ChangeDetectorRef, private systemCallService : SystemCallService ,
                private headerService : HeaderService) {
    }

    ngOnInit(): void {
        this.setupModel();
    }

    public showHeader(value : boolean) {
        this.headerShown = value;
        this.cd.detectChanges();
    }

    ngOnChanges(changes: SimpleChanges): void {
        this.setupModel();
    }

    private setupModel() : void {
        this.model = this.stepInfo.model as RequestSetupStepModel | null;
        this.done = this.stepInfo.status == 'AlreadyRun'
    }

    selectAFile() {
        this.systemCallService.requestFileOpen('', ['*'])
            .pipe(
                filter(t => !!t),
                tap(t => this.model.fileName = t)
            )
            .subscribe();
    }

    saveAndContinue() {
        if (!this.model)
            return;

        if (this.isRequest) {
            this.apiService.breakPointSetRequest(this.context.exchangeId, this.model)
                .subscribe();
        }
        else{

            this.apiService.breakPointSetResponse(this.context.exchangeId, this.model)
                .subscribe();
        }

    }

    continue() {
        if (this.isRequest) {
            this.apiService.breakPointContinueRequest(this.context.exchangeId)
                .subscribe();
        }
        else{

            this.apiService.breakPointContinueResponse(this.context.exchangeId)
                .subscribe();
        }
    }

    modelChange(content : any) {
    }
}
