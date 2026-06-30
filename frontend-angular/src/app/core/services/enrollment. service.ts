import { Injectable } from '@angular/core';

import { HttpClient } from '@angular/common/http';

import { Observable } from 'rxjs';

import { environment } from '../../../environments/environment';


export interface EnrollmentRequest{
//EDITAR LOS CAMPOS CUANDO ESTE HABILITADO EL ENDPOINT EN BACK
    careerId:number;

    campus:string;

    shift:string;

    subjects:number[];

}

@Injectable({

    providedIn:'root'

})

export class EnrollmentService{

    private baseURL=environment.apiServer;

    constructor(private http:HttpClient){}

    enroll(

        request:EnrollmentRequest

    ):Observable<any>{

        return this.http.post(

            `${this.baseURL}v1/enrollments`, //CAMBIAR RUTA CUANDO ESTE HABILITADO EL ENDPOINT

            request

        );

    }

}