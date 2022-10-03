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
import { TooltipModule } from 'ngx-bootstrap/tooltip';
import { ExchangeViewerComponent } from './widgets/exchange-viewer/exchange-viewer.component';
import { VerticalSeparatorDirective } from './directives/vertical-separator.directive';
import { FilterHeaderViewComponent } from './widgets/filter-header-view/filter-header-view.component';
import { ExchangeTableViewComponent } from './widgets/exchange-table-view/exchange-table-view.component';
import { PerfectScrollbarConfigInterface, PerfectScrollbarModule, PERFECT_SCROLLBAR_CONFIG } from 'ngx-perfect-scrollbar';
import { HeaderViewerComponent } from './widgets/exchange-viewer/details/header-viewer/header-viewer.component';
import { RawRequestHeaderResultComponent } from './widgets/exchange-viewer/details/raw-request-header-result/raw-request-header-result.component';
import { AngularSplitModule } from 'angular-split';
import { QueryStringResultComponent } from './widgets/exchange-viewer/details/query-string-result/query-string-result.component';
import { RequestCookieResultComponent } from './widgets/exchange-viewer/details/request-cookie-result/request-cookie-result.component';
import { RequestJsonResultComponent } from './widgets/exchange-viewer/details/request-json-result/request-json-result.component';
import { RequestTextBodyResultComponent } from './widgets/exchange-viewer/details/request-text-body-result/request-text-body-result.component';
import { RequestBodyAnalysisResultComponent } from './widgets/exchange-viewer/details/request-body-analysis-result/request-body-analysis-result.component';
import { FormUrlEncodedResultComponent } from './widgets/exchange-viewer/details/form-url-encoded-result/form-url-encoded-result.component';


// AoT requires an exported function for factories
const httpLoaderFactory = (http: HttpClient): TranslateHttpLoader =>  new TranslateHttpLoader(http, './assets/i18n/', '.json');

const DEFAULT_PERFECT_SCROLLBAR_CONFIG: PerfectScrollbarConfigInterface = {
    suppressScrollX: true
  };

@NgModule({
    declarations: [AppComponent, MenuComponent, ToggleComponent, HomeComponent, StatusBarComponent, FilterComponent, ExchangeViewerComponent, VerticalSeparatorDirective, FilterHeaderViewComponent, ExchangeTableViewComponent, HeaderViewerComponent, RawRequestHeaderResultComponent, QueryStringResultComponent, RequestCookieResultComponent, RequestJsonResultComponent, RequestTextBodyResultComponent, RequestBodyAnalysisResultComponent, FormUrlEncodedResultComponent],
    imports: [
        BrowserModule,
        FormsModule,
        HttpClientModule,
        CoreModule,
        AngularSplitModule,
        BsDropdownModule.forRoot(),
        TabsModule.forRoot(),
        TooltipModule.forRoot(),
        PerfectScrollbarModule,
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
    providers: [ {
        provide: PERFECT_SCROLLBAR_CONFIG,
        useValue: DEFAULT_PERFECT_SCROLLBAR_CONFIG
    }],
    bootstrap: [AppComponent]
})
export class AppModule {}
