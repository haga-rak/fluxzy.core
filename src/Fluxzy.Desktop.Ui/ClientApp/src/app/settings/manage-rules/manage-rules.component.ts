import {ChangeDetectorRef, Component, OnInit} from '@angular/core';
import {BsModalRef, ModalOptions} from "ngx-bootstrap/modal";
import {ApiService} from "../../services/api.service";
import {filter, switchMap, take, tap} from "rxjs";
import {Rule, RuleContainer} from "../../core/models/auto-generated";
import * as _ from "lodash";
import {DialogService} from "../../services/dialog.service";
import {StatusBarService} from "../../services/status-bar.service";
import {ConfirmResult, MenuService} from "../../core/services/menu-service.service";

@Component({
    selector: 'app-manage-rules',
    templateUrl: './manage-rules.component.html',
    styleUrls: ['./manage-rules.component.scss']
})
export class ManageRulesComponent implements OnInit {
    public ruleContainers: RuleContainer[];

    constructor(public bsModalRef: BsModalRef,
                public options: ModalOptions,
                private apiService : ApiService,
                private cd : ChangeDetectorRef,
                private dialogService : DialogService,
                private menuService : MenuService,
                private statusBarService : StatusBarService) {
    }

    ngOnInit(): void {
        this.apiService.ruleGetContainer()
            .pipe(
                tap(c => this.ruleContainers = c),
                tap( _ => this.cd.detectChanges())
            ).subscribe() ;
    }

    public close() : void {
        this.bsModalRef.hide();
    }

    public save() : void {
        this.apiService.ruleUpdateContainer(
            this.ruleContainers
        ).pipe(
            tap(_ => this.bsModalRef.hide()),
            tap(_ => this.statusBarService.addMessage('Rules saved')),
            tap(_ => this.cd.detectChanges())
        ).subscribe();
    }

    public createRule() : void{
        this.dialogService.openRuleCreate()
            .pipe(
                filter(t => !!t),
                switchMap(t => this.apiService.ruleValidate(t)),
                tap(t => {
                    let max = 0 ;

                    if (this.ruleContainers.length) {
                        max = _.maxBy(this.ruleContainers, c => c.rule.order).rule.order;
                    }

                    t.order = max + 1 ;

                    return this.ruleContainers.push({
                        rule: t,
                        enabled: true,
                    });
                }),
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

    public enabledDisabled(ruleContainer: RuleContainer) {

        ruleContainer.enabled = !ruleContainer.enabled ;
        this.cd.detectChanges();

    }

    public moveUp(ruleContainer: any) {
        const index = _.findIndex(this.ruleContainers, a => a.rule.identifier === ruleContainer.rule.identifier) ;

        if (index === 0)
            return;


        const temp = this.ruleContainers[index - 1] ;
        this.ruleContainers[index - 1] = ruleContainer;
        this.ruleContainers[index] = temp;

        const oldIndex = temp.rule.order ;
        temp.rule.order =  ruleContainer.rule.order ;
        ruleContainer.rule.order = oldIndex;

        this.cd.detectChanges();
    }

    public moveDown(ruleContainer: any) {
        const index = _.findIndex(this.ruleContainers, a => a.rule.identifier === ruleContainer.rule.identifier) ;

        if (index === this.ruleContainers.length - 1)
            return;

        const temp = this.ruleContainers[index + 1] ;
        this.ruleContainers[index + 1] = ruleContainer;
        this.ruleContainers[index] = temp;

        const oldIndex = temp.rule.order ;
        temp.rule.order =  ruleContainer.rule.order ;
        ruleContainer.rule.order = oldIndex;

        this.cd.detectChanges();
    }

    public changeFilter(ruleContainer: RuleContainer) : void {

    }

    public disableAll() : void {
        for (const container of this.ruleContainers) {
            container.enabled = false;
        }

        this.cd.detectChanges();
    }

    public export() : void {
        const confirmResult = this.menuService.confirm('Do you want to export only enabled rules?');

        if (confirmResult === ConfirmResult.Cancel)
            return ;

        const onlyActive = confirmResult === ConfirmResult.Yes ;

        const rules = onlyActive ?
            this.ruleContainers.filter(t => t.enabled).map(t => t.rule) :
            this.ruleContainers.map(t => t.rule).slice();

        this.apiService.ruleExport({ rules })
            .pipe(
                tap(t =>
                    this.dialogService.openStringDisplay('Rule export', t, 'rules.fluxzy.yaml'))
            ).subscribe() ;
    }

    public import() : void {
        this.dialogService.openStringEdit('Rule import', '')
            .pipe(
                filter(t => !!t),
                switchMap(t => this.apiService.ruleImport({ yamlContent : t, deleteExisting : false})),
                take(1),
                tap(t =>  {
                    const extraContainers = t.map(r => { return { rule : r, enabled : true } }) ;
                    this.ruleContainers = this.ruleContainers.concat(extraContainers) ;
                } ),
                tap(_ => this.cd.detectChanges()),
            ).subscribe() ;
    }
}
