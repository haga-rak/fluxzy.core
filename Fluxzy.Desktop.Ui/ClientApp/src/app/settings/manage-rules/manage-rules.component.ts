import {ChangeDetectorRef, Component, OnInit} from '@angular/core';
import {BsModalRef, ModalOptions} from "ngx-bootstrap/modal";
import {ApiService} from "../../services/api.service";
import {filter, switchMap, take, tap} from "rxjs";
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
        this.dialogService.openRuleCreate()
            .pipe(
                filter(t => !!t),
                switchMap(t => this.apiService.ruleValidate(t)),
                tap(t => this.ruleContainers.push( {
                    rule : t,
                    enabled : true
                })),
                tap(_ => this.cd.detectChanges())
            ).subscribe();
    }

    public deleteRule(rule: Rule) {

        _.remove(this.ruleContainers, t => t.rule.identifier === rule.identifier) ;
        this.cd.detectChanges();
    }

    public editRule(rule: Rule) : void {
        this.apiService.ruleValidate(rule)
            .pipe(
                filter(t => !!t),
                switchMap(t => this.dialogService.openRuleEdit(t, true)),
                filter(t => !!t),
                take(1),
                tap(rule => {

                    const index = _.findIndex(this.ruleContainers, a => a.rule.identifier === rule.identifier) ;
                    if (index >= 0) {
                        this.ruleContainers[index].rule = rule;
                    }
                }),
                tap(_ => this.cd.detectChanges())
            ).subscribe();
    }
}
