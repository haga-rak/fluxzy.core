import {Component, Input, OnInit, SimpleChanges} from '@angular/core';
import {
    BreakPointContextInfo,
    BreakPointContextStepInfo,
    RequestSetupStepModel, ResponseSetupStepModel
} from "../../../core/models/auto-generated";
import {ApiService} from "../../../services/api.service";

@Component({
  selector: 'app-edit-response',
  templateUrl: './edit-response.component.html',
  styleUrls: ['./edit-response.component.scss']
})
export class EditResponseComponent implements OnInit {

    @Input() public context : BreakPointContextInfo ;
    @Input() public stepInfo : BreakPointContextStepInfo;
    public model: ResponseSetupStepModel | null;
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
        this.model = this.stepInfo.model as ResponseSetupStepModel | null;
        this.done = this.stepInfo.status == 'AlreadyRun'
    }

}
