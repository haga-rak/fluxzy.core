import {Component, Input, OnInit, SimpleChanges, ViewChild} from '@angular/core';
import {
    BreakPointContextInfo,
    BreakPointContextStepInfo,
    ConnectionSetupStepModel, RequestSetupStepModel
} from "../../../core/models/auto-generated";
import {ApiService} from "../../../services/api.service";
import {Subject, tap} from "rxjs";
import {Header} from "../../../shared/header-editor/header-utils";
import {HeaderEditorComponent} from "../../../shared/header-editor/header-editor.component";

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

    private selectedHeader$ = new Subject<Header | null>();
    public selectedHeader : Header | null = null;

    @ViewChild('editor') editor : HeaderEditorComponent;


    constructor(private apiService : ApiService) {
    }

    ngOnInit(): void {
        this.setupModel();

        this.selectedHeader$.pipe(
            tap(t => this.selectedHeader = t)
        ).subscribe() ;

        console.log(this.stepInfo)
    }

    ngOnChanges(changes: SimpleChanges): void {
        this.setupModel();
    }

    private setupModel() : void {
        this.model = this.stepInfo.model as RequestSetupStepModel | null;
        this.done = this.stepInfo.status == 'AlreadyRun'
    }

    dumpYoyo() {
        console.log(document.querySelector("#yoyo").textContent);
    }

    onHeaderSelected($event: Header | null) {
        this.selectedHeader$.next($event);
    }

    removeHeader(selectedHeader: Header) {
        this.editor.deleteHeader(selectedHeader);
    }
}
