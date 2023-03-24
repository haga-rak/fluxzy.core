import {Component, Input, OnChanges, OnInit, SimpleChanges} from '@angular/core';
import {
    BreakPointContextInfo,
    BreakPointContextStepInfo,
    ConnectionSetupStepModel,
    ExchangeInfo
} from "../../../core/models/auto-generated";
import {ApiService} from "../../../services/api.service";

@Component({
    selector: '[app-authority]',
    templateUrl: './connection-setup-step.component.html',
    styleUrls: ['./connection-setup-step.component.scss']
})
export class ConnectionSetupStepComponent implements OnInit, OnChanges {
    @Input() public context : BreakPointContextInfo ;
    @Input() public stepInfo : BreakPointContextStepInfo;
    public model: ConnectionSetupStepModel | null;
    public done : boolean = false;


    constructor(private apiService : ApiService) {
    }

    ngOnInit(): void {
        this.setupModel();
        console.log(this.stepInfo)
    }

    ngOnChanges(changes: SimpleChanges): void {
        this.setupModel();
    }

    private setupModel() : void {
        this.model = this.stepInfo.model as ConnectionSetupStepModel | null;
        this.done = this.stepInfo.status == 'AlreadyRun'

    }

    saveAndContinue() {
        if (!this.model) {
            return;
        }

        this.apiService.breakPointEndPointSet(this.context.exchangeId, this.model)
            .subscribe() ;
    }

    skip() {
        this.apiService.breakPointContinueOnce(this.context.exchangeId)
            .subscribe() ;
    }
}
