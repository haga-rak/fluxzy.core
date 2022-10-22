import {ChangeDetectorRef, Component, OnInit} from '@angular/core';
import {BsModalRef, ModalOptions} from "ngx-bootstrap/modal";
import {ApiService} from "../../services/api.service";
import {tap} from "rxjs";
import {Rule, RuleContainer} from "../../core/models/auto-generated";
import * as _ from "lodash";
import {DialogService} from "../../services/dialog.service";

@Component({
    selector: 'app-manage-rules',
    templateUrl: './manage-rules.component.html',
    styleUrls: ['./manage-rules.component.scss']
})
export class ManageRulesComponent implements OnInit {
    private ruleContainers: RuleContainer[];

    constructor(public bsModalRef: BsModalRef, public options: ModalOptions, private apiService : ApiService, private cd : ChangeDetectorRef,
                private dialogService : DialogService) {
    }

    ngOnInit(): void {
        this.apiService.ruleGetContainer()
            .pipe(
                tap(c => this.ruleContainers = c),
                tap( _ => this.cd.detectChanges())
            ).subscribe() ;
    }

    public close() : void {

    }

    public save() : void {

    }

    public createRule() : void{
        this.dialogService.openRulePreCreate();
    }

    public deleteRule(rule: Rule) {

        _.remove(this.ruleContainers, t => t.rule.identifier === rule.identifier) ;
        this.cd.detectChanges();
    }
}
