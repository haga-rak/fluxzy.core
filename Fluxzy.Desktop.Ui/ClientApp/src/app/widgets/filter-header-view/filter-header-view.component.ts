import { Component, OnInit } from '@angular/core';
import { DialogService } from '../../services/dialog.service';

@Component({
    selector: 'app-filter-header-view',
    templateUrl: './filter-header-view.component.html',
    styleUrls: ['./filter-header-view.component.scss'],
})
export class FilterHeaderViewComponent implements OnInit {
    constructor(private dialogService : DialogService) {}

    ngOnInit(): void {}

    public openManagedFilters() : void {
      this.dialogService.openManageFilters(true); 
    }
}
