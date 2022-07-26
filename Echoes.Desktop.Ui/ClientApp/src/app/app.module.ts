import { BrowserModule } from '@angular/platform-browser';
import { NgModule } from '@angular/core';
import { FormsModule } from '@angular/forms';
import { HttpClientModule, HttpClient } from '@angular/common/http';
import { CoreModule } from './core/core.module';

// NG Translate
import { TranslateModule, TranslateLoader } from '@ngx-translate/core';
import { TranslateHttpLoader } from '@ngx-translate/http-loader';


import { AppComponent } from './app.component';
import { MenuComponent } from './menu/menu.component';
import { BrowserAnimationsModule } from '@angular/platform-browser/animations';
import { ToggleComponent } from './widgets/toggle/toggle.component';
import { RouterModule, Routes } from '@angular/router';
import { HomeComponent } from './home/home.component';
import { StatusBarComponent } from './status-bar/status-bar.component';
import { FilterComponent } from './filter/filter.component';
import { BsDropdownModule } from 'ngx-bootstrap/dropdown';
import { TabsModule } from 'ngx-bootstrap/tabs';


// AoT requires an exported function for factories
const httpLoaderFactory = (http: HttpClient): TranslateHttpLoader =>  new TranslateHttpLoader(http, './assets/i18n/', '.json');



@NgModule({
    declarations: [AppComponent, MenuComponent, ToggleComponent, HomeComponent, StatusBarComponent, FilterComponent],
    imports: [
        BrowserModule,
        FormsModule,
        HttpClientModule,
        CoreModule,
        BsDropdownModule.forRoot(),
        TabsModule.forRoot(),
        RouterModule.forRoot([
            {
              path: '',
              redirectTo: 'home',
              pathMatch: 'full'
            },
            {
              path: 'home',
              component: HomeComponent
            }]),
        TranslateModule.forRoot({
            loader: {
                provide: TranslateLoader,
                useFactory: httpLoaderFactory,
                deps: [HttpClient]
            }
        }),
        BrowserAnimationsModule, 
       
        
    ],
    providers: [],
    bootstrap: [AppComponent]
})
export class AppModule {}
