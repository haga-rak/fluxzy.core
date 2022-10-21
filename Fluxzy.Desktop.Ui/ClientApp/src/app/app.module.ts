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
import { ModalModule } from 'ngx-bootstrap/modal';
import { ToggleComponent } from './widgets/toggle/toggle.component';
import { RouterModule, Routes } from '@angular/router';
import { HomeComponent } from './home/home.component';
import { StatusBarComponent } from './status-bar/status-bar.component';
import { BsDropdownModule } from 'ngx-bootstrap/dropdown';
import { TabsModule } from 'ngx-bootstrap/tabs';
import { TooltipModule } from 'ngx-bootstrap/tooltip';
import { ExchangeViewerComponent } from './widgets/exchange-viewer/exchange-viewer.component';
import { VerticalSeparatorDirective } from './directives/vertical-separator.directive';
import { FilterHeaderViewComponent } from './widgets/filter-header-view/filter-header-view.component';
import { ExchangeTableViewComponent } from './widgets/exchange-table-view/exchange-table-view.component';
import {
    PerfectScrollbarConfigInterface,
    PerfectScrollbarModule,
    PERFECT_SCROLLBAR_CONFIG,
} from 'ngx-perfect-scrollbar';
import { HeaderViewerComponent } from './widgets/exchange-viewer/details-request/header-viewer/header-viewer.component';
import { RawRequestHeaderResultComponent } from './widgets/exchange-viewer/details-request/raw-request-header-result/raw-request-header-result.component';
import { AngularSplitModule } from 'angular-split';
import { QueryStringResultComponent } from './widgets/exchange-viewer/details-request/query-string-result/query-string-result.component';
import { RequestCookieResultComponent } from './widgets/exchange-viewer/details-request/request-cookie-result/request-cookie-result.component';
import { RequestJsonResultComponent } from './widgets/exchange-viewer/details-request/request-json-result/request-json-result.component';
import { RequestTextBodyResultComponent } from './widgets/exchange-viewer/details-request/request-text-body-result/request-text-body-result.component';
import { RequestBodyAnalysisResultComponent } from './widgets/exchange-viewer/details-request/request-body-analysis-result/request-body-analysis-result.component';
import { FormUrlEncodedResultComponent } from './widgets/exchange-viewer/details-request/form-url-encoded-result/form-url-encoded-result.component';
import { ExchangeViewerHeaderComponent } from './widgets/exchange-viewer-header/exchange-viewer-header.component';
import { MultipartFormContentResultComponent } from './widgets/exchange-viewer/details-request/multipart-form-content-result/multipart-form-content-result.component';
import { ExchangeConnectivityComponent } from './widgets/exchange-connectivity/exchange-connectivity.component';
import { ResponseSummaryComponent } from './widgets/exchange-viewer/details-response/response-summary/response-summary.component';
import { ArraySortPipe } from './directives/array-sort.pipe';
import { ResponseBodySummaryResultComponent } from './widgets/exchange-viewer/details-response/response-body-summary-result/response-body-summary-result.component';
import { ResponseTextContentResultComponent } from './widgets/exchange-viewer/details-response/response-text-content-result/response-text-content-result.component';
import { ResponseJsonResultComponent } from './widgets/exchange-viewer/details-response/response-json-result/response-json-result.component';
import { CodeViewComponent } from './shared/code-view/code-view.component';
import { AuthorizationResultComponent } from './widgets/exchange-viewer/details-request/authorization-result/authorization-result.component';
import { AuthorizationBearerResultComponent } from './widgets/exchange-viewer/details-request/authorization-bearer-result/authorization-bearer-result.component';
import { SetCookieResultComponent } from './widgets/exchange-viewer/details-response/set-cookie-result/set-cookie-result.component';
import { GlobalSettingComponent } from './settings/global-setting/global-setting.component';
import { ManageFiltersComponent } from './settings/manage-filters/manage-filters.component';
import { FilterEditComponent } from './settings/filter-forms/filter-edit/filter-edit.component';
import { MethodFilterFormComponent } from './settings/filter-forms/child-forms/method-filter-form/method-filter-form.component';
import { HostFilterFormComponent } from './settings/filter-forms/child-forms/host-filter-form/host-filter-form.component';
import { FilterCollectionFormComponent } from './settings/filter-forms/child-forms/filter-collection-form/filter-collection-form.component';
import { FilterPreCreateComponent } from './settings/filter-forms/filter-pre-create/filter-pre-create.component';
import {AccordionModule} from "ngx-bootstrap/accordion";
import { FilterRenderComponent } from './settings/filter-forms/filter-edit/filter-render/filter-render.component';
import { FullUrlFilterFormComponent } from './settings/filter-forms/child-forms/full-url-filter-form/full-url-filter-form.component';
import { StringFilterFormComponent } from './settings/filter-forms/child-forms/string-filter-form/string-filter-form.component';
import { PathFilterFormComponent } from './settings/filter-forms/child-forms/path-filter-form/path-filter-form.component';
import { IpEgressFilterFormComponent } from './settings/filter-forms/child-forms/ip-egress-filter-form/ip-egress-filter-form.component';
import { FuncFilterPipe } from './directives/func-filter.pipe';
import { ContextMenuComponent } from './shared/context-menu/context-menu.component';
import { RequestHeaderFilterFormComponent } from './settings/filter-forms/child-forms/request-header-filer-form/request-header-filter-form.component';
import {TypeaheadModule} from "ngx-bootstrap/typeahead";
import { ResponseHeaderFilterFormComponent } from './settings/filter-forms/child-forms/response-header-filter-form/response-header-filter-form.component';
import { ManageRulesComponent } from './settings/manage-rules/manage-rules.component';

// AoT requires an exported function for factories
const httpLoaderFactory = (http: HttpClient): TranslateHttpLoader =>
    new TranslateHttpLoader(http, './assets/i18n/', '.json');


const DEFAULT_PERFECT_SCROLLBAR_CONFIG: PerfectScrollbarConfigInterface = {
    suppressScrollX: true,

};
@NgModule({
    declarations: [
        AppComponent,
        MenuComponent,
        ToggleComponent,
        HomeComponent,
        StatusBarComponent,
        ExchangeViewerComponent,
        VerticalSeparatorDirective,
        FilterHeaderViewComponent,
        ExchangeTableViewComponent,
        HeaderViewerComponent,
        RawRequestHeaderResultComponent,
        QueryStringResultComponent,
        RequestCookieResultComponent,
        RequestJsonResultComponent,
        RequestTextBodyResultComponent,
        RequestBodyAnalysisResultComponent,
        FormUrlEncodedResultComponent,
        ExchangeViewerHeaderComponent,
        MultipartFormContentResultComponent,
        ExchangeConnectivityComponent,
        ResponseSummaryComponent,
        ArraySortPipe,
        ResponseBodySummaryResultComponent,
        ResponseTextContentResultComponent,
        ResponseJsonResultComponent,
        CodeViewComponent,
        AuthorizationResultComponent,
        AuthorizationBearerResultComponent,
        SetCookieResultComponent,
        GlobalSettingComponent,
        ManageFiltersComponent,
        FilterEditComponent,
        MethodFilterFormComponent,
        HostFilterFormComponent,
        FilterCollectionFormComponent,
        FilterPreCreateComponent,
        FilterRenderComponent,
        FullUrlFilterFormComponent,
        PathFilterFormComponent,
        IpEgressFilterFormComponent,
        FuncFilterPipe,
        ContextMenuComponent,
        RequestHeaderFilterFormComponent,
        ResponseHeaderFilterFormComponent,
        ManageRulesComponent,


    ],
    imports: [
        BrowserModule,
        FormsModule,
        HttpClientModule,
        CoreModule,
        AngularSplitModule,
        BsDropdownModule.forRoot(),
        ModalModule.forRoot(),
        TabsModule.forRoot(),
        TooltipModule.forRoot(),
        PerfectScrollbarModule,
        RouterModule.forRoot([
            {
                path: '',
                redirectTo: 'home',
                pathMatch: 'full',
            },
            {
                path: 'home',
                component: HomeComponent,
            },
        ]),
        TranslateModule.forRoot({
            loader: {
                provide: TranslateLoader,
                useFactory: httpLoaderFactory,
                deps: [HttpClient],
            },
        }),
        BrowserAnimationsModule,
        AccordionModule,
        TypeaheadModule,
    ],
    providers: [
        {
            provide: PERFECT_SCROLLBAR_CONFIG,
            useValue: DEFAULT_PERFECT_SCROLLBAR_CONFIG,
        },
    ],
    bootstrap: [AppComponent],
    entryComponents : [GlobalSettingComponent, ManageFiltersComponent,FilterEditComponent,FilterPreCreateComponent,ManageRulesComponent]
})
export class AppModule {}
