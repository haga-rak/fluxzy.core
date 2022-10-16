import {ChangeDetectorRef, Component, HostListener, OnInit} from '@angular/core';
import {ContextMenuAction, ContextMenuModel, ContextMenuService} from "../../services/context-menu.service";
import {tap} from "rxjs";

@Component({
    selector: 'app-context-menu',
    templateUrl: './context-menu.component.html',
    styleUrls: ['./context-menu.component.scss']
})
export class ContextMenuComponent implements OnInit {
    public contextMenuModel : ContextMenuModel ;

    constructor(private contextMenuService : ContextMenuService, private cd : ChangeDetectorRef) {
    }

    ngOnInit(): void {
        this.contextMenuService.getContextMenuModel()
            .pipe(
                tap(t => this.contextMenuModel = t)
            ).subscribe();
    }

    public getTop() : number {
        if (!this.contextMenuModel) {
            return 0 ;
        }

        return this.contextMenuModel.coordinate.y;
    }

    public triggered(event: MouseEvent) : void {
        let data = event as any ;
        data.menuWasClicked = true;
        console.log('catched');
    }

    @HostListener('document:mousedown', ['$event'])
    public documentClick(event: MouseEvent) :void {

        let data = event as any ;
        if (data.menuWasClicked)
            return;

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
