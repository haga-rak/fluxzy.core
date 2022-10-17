import {ChangeDetectorRef, Component, ElementRef, HostListener, OnInit, ViewChild} from '@angular/core';
import {ContextMenuModel, ContextMenuService} from "../../services/context-menu.service";
import {tap} from "rxjs";
import {ContextMenuAction} from "../../core/models/auto-generated";
import {ContextMenuExecutionService} from "../../services/context-menu-execution.service";

@Component({
    selector: 'app-context-menu',
    templateUrl: './context-menu.component.html',
    styleUrls: ['./context-menu.component.scss']
})
export class ContextMenuComponent implements OnInit {
    public contextMenuModel : ContextMenuModel ;
    public menuHide = true;
    private yCorrection = 0 ;

    @ViewChild('contextMenuBlock') contextMenuBlock:ElementRef;

    constructor(private contextMenuService : ContextMenuService, private contextMenuExchangeService : ContextMenuExecutionService, private cd : ChangeDetectorRef) {
    }

    ngOnInit(): void {
        this.contextMenuService.getContextMenuModel()
            .pipe(
                tap(t => this.contextMenuModel = t),
                tap(t => this.cd.detectChanges()),
                tap(t => this.prepareMenu() ), // Compute the position
            ).subscribe();
    }


    private prepareMenu() : void {
        const blockHeight : number = this.contextMenuBlock.nativeElement.offsetHeight;

        let targetHeight = this.contextMenuModel.coordinate.y ;

        if ((targetHeight + blockHeight+ 40) > window.innerHeight){
            this.yCorrection = -blockHeight;
        }
        else{
            this.yCorrection = 0 ;
        }

        this.menuHide = false;
        this.cd.detectChanges();
    }

    public getIcon(contextMenuAction : ContextMenuAction) : string[] {
        return [this.contextMenuService.getIconClass(contextMenuAction)];
    }

    public getTop() : number {
        if (!this.contextMenuModel) {
            return 0 ;
        }
        return this.contextMenuModel.coordinate.y + this.yCorrection;
    }

    public mouseUp(event : MouseEvent){
        event.stopPropagation();
        event.preventDefault();
    }

    public triggered(event: MouseEvent, contextMenuAction : ContextMenuAction) : void {
        let data = event as any ;
        data.menuWasClicked = true;

        event.stopPropagation();
        event.preventDefault();

        this
            .contextMenuExchangeService.executeAction(contextMenuAction, this.contextMenuModel.exchangeId)
            .pipe(
                tap(_ => {

                    this.contextMenuModel = null ;
                    this.cd.detectChanges();
                })
            ).subscribe();


    }

    @HostListener('document:mousedown', ['$event'])
    public documentMouseDown(event: MouseEvent) :void {

        let data = event as any ;
        if (data.menuWasClicked){
            return;
        }



        this.contextMenuModel = null ;
        this.cd.detectChanges();
    }

    public getLeft() : number {
        if (!this.contextMenuModel) {
            return 0 ;
        }

        return this.contextMenuModel.coordinate.x;
    }


}
