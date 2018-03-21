import { Injectable } from '@angular/core';
import { Http, Headers, RequestOptions, Response } from '@angular/http/';
import { Observable } from 'rxjs/Observable';
import 'rxjs/add/operator/map';
import 'rxjs/add/operator/catch';
import 'rxjs/add/observable/throw';
import {BehaviorSubject} from 'rxjs/BehaviorSubject';
import { tokenNotExpired, JwtHelper } from 'angular2-jwt';
import { User } from '../_models/User';

@Injectable()
export class AuthService {

    baseUrl = 'http://localhost:5000/api/auth/';
    userToken: any;
    decodedToken: any;
    currentUser: User;
    jwtHelper: JwtHelper = new JwtHelper();
    private photoUrl = new BehaviorSubject<string>('../../assets/user.png');
    currentPhotoUrl = this.photoUrl.asObservable();

    constructor(private http: Http) { }

    changeMemberPhoto(photoUrl: string) {
        this.photoUrl.next(photoUrl);
    }

    login(model: any) {
        return this.http.post(this.baseUrl + 'login', model, this.requestOptions()).map((reponse: Response) => {
            const user = reponse.json();

            if (user) {
                localStorage.setItem('token', user.tokenString);
                localStorage.setItem('user', JSON.stringify(user.user));
                this.decodedToken = this.jwtHelper.decodeToken(user.tokenString);
                this.currentUser = user.user;
                this.userToken = user.tokenString;
                this.changeMemberPhoto(this.currentUser.photoUrl);
            }
        }).catch(this.handleError);
    }

    register(model: any) {
        return this.http.post(this.baseUrl + 'register', model, this.requestOptions()).catch(this.handleError);
    }

    loggedIn(){
        return tokenNotExpired('token');
    }

    private requestOptions() {
        const headers = new Headers({'Content-type': 'application/json'});
        return new RequestOptions({headers: headers});
    }

    private handleError(error: any){
        const applicationError  = error.headers.get('Application-Error');
        if(applicationError){
            return Observable.throw(applicationError);            
        }

        const serverError = error.json();
        let modelStateErrors = '';

        if(serverError){
            for(const key in serverError){
                if(serverError[key]){
                    modelStateErrors += serverError[key] + '\n';
                }
            }
        }

        return Observable.throw(
            modelStateErrors || "Server error"
        );
    }
}
