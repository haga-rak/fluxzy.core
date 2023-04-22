import {ChangeDetectorRef, Component, ElementRef, HostListener, OnInit, ViewChild} from '@angular/core';
import {ContextMenuService, Coordinate, GlobalContextMenuCoordinate} from "../../services/context-menu.service";
import {ContextMenuExecutionService} from "../../services/context-menu-execution.service";
import {InputService} from "../../services/input.service";
import {tap} from "rxjs";
import {ExchangeTableService} from "../../widgets/exchange-table-view/exchange-table.service";
import {ExchangeCellModel} from "../../widgets/exchange-table-view/exchange-cell.model";

@Component({
    selector: 'app-context-menu-table-header',
    templateUrl: './context-menu-table-header.component.html',
    styleUrls: ['./context-menu-table-header.component.scss']
})
export class ContextMenuTableHeaderComponent implements OnInit {
    public menuHide = true;
    public contextModel: GlobalContextMenuCoordinate;
    private yCorrection = 0 ;

    @ViewChild('contextMenuBlock') contextMenuBlock:ElementRef;
    public allCellModels: ExchangeCellModel[] = [];

    constructor(private contextMenuService : ContextMenuService,
                private inputService : InputService,
                private exchangeTableService : ExchangeTableService,
                private cd : ChangeDetectorRef) {
    }

    ngOnInit(): void {
        this.contextMenuService.getTableHeaderContextMenuModel()
            .pipe(
                tap(t => this.contextModel = t),
                tap(t => this.cd.detectChanges()),
                tap(t => this.prepareMenu() ), // Compute the position
            ).subscribe();

        this.exchangeTableService.allCellModels
            .pipe(
                tap(t => this.allCellModels = t.filter(f => !f.readonly)),
                tap(t => this.cd.detectChanges()),
            ).subscribe();
    }

    private prepareMenu() : void {
        const blockHeight : number = this.contextMenuBlock.nativeElement.offsetHeight;

        let targetHeight = this.contextModel.coordinate.y ;

        if ((targetHeight + blockHeight+ 40) > window.innerHeight){
            this.yCorrection = -blockHeight;
        }
        else{
            this.yCorrection = 0 ;
        }

        this.menuHide = false;
        this.cd.detectChanges();
    }

    public getLeft() : number {
        if (!this.contextModel) {
            return 0 ;
        }

        return this.contextModel.coordinate.x;
    }

    public getTop() : number {
        if (!this.contextModel) {
            return 0 ;
        }
        return this.contextModel.coordinate.y + this.yCorrection;
    }

    @HostListener('document:mousedown', ['$event'])
    public documentMouseDown(event: MouseEvent) :void {

        let data = event as any ;
        if (data.menuWasClicked){
            return;
        }
        this.contextModel = null ;
        this.menuHide = true;
        this.cd.detectChanges();
    }

    changeVisibility(cellModel: ExchangeCellModel) {
        cellModel.hide = !cellModel.hide;
        this.exchangeTableService.updateCellModel(cellModel);
    }
}
