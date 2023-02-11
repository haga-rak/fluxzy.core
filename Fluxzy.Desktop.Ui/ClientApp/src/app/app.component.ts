import { Component } from '@angular/core';
import { ElectronService } from './core/services';
import { TranslateService } from '@ngx-translate/core';
import { APP_CONFIG } from '../environments/environment';

@Component({
    selector: 'app-root',
    templateUrl: './app.component.html',
    styleUrls: ['./app.component.scss']
})
export class AppComponent {
    constructor(
        private electronService: ElectronService,
        private translate: TranslateService
        ) {
            this.translate.setDefaultLang('en');
            if (electronService.isElectron) {

                if (APP_CONFIG.production) {
                    console.log = () => {}
                    console.log = () => {}
                }
                //console.log(process.env);
                // console.log('Electron ipcRenderer', this.electronService.ipcRenderer);

            }
        }
    }
