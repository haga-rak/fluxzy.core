import {Injectable} from '@angular/core';
import {
    HttpRequest,
    HttpHandler,
    HttpEvent,
    HttpInterceptor
} from '@angular/common/http';
import {Observable} from 'rxjs';
import {APP_CONFIG} from "../../environments/environment";

@Injectable()
export class BackendInterceptor implements HttpInterceptor {

    intercept(request: HttpRequest<any>, next: HttpHandler): Observable<HttpEvent<any>> {
        if (!APP_CONFIG.production)
            return next.handle(request);

        if (request.url.startsWith("/api") || request.url.startsWith("api/")) {
            const apiReq = request.clone({ url: `http://localhost:5198/${request.url}` });
            return next.handle(apiReq);
        }

        return next.handle(request); // Skip all assets
    }
}
