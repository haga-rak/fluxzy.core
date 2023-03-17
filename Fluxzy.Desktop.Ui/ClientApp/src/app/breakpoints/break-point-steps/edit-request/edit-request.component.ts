import {Component, Input, OnInit, SimpleChanges} from '@angular/core';
import {
    BreakPointContextInfo,
    BreakPointContextStepInfo,
    ConnectionSetupStepModel, RequestSetupStepModel
} from "../../../core/models/auto-generated";
import {ApiService} from "../../../services/api.service";

@Component({
    selector: 'app-edit-request',
    templateUrl: './edit-request.component.html',
    styleUrls: ['./edit-request.component.scss']
})
export class EditRequestComponent implements OnInit {

    @Input() public context : BreakPointContextInfo ;
    @Input() public stepInfo : BreakPointContextStepInfo;
    public model: RequestSetupStepModel | null;
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
        this.model = this.stepInfo.model as RequestSetupStepModel | null;
        this.done = this.stepInfo.status == 'AlreadyRun'
    }
}
