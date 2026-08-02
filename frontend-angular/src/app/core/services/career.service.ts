import { Injectable } from '@angular/core';

import { HttpClient } from '@angular/common/http';

import { Observable } from 'rxjs';

import { environment } from '../../../environments/environment';

export interface Career {
  success: boolean;
  id: number;
  name: string;
  code: string;
  description: string;
  totalCredits: number;
  durationYears: number;
  isActive: boolean;
  createdAt: string;
}

@Injectable({

    providedIn:'root'

})


export class CareerService{

    private baseURL=environment.apiServer;

    constructor(private http:HttpClient){}

    getCareers():Observable<Career[]>{

        return this.http.get<Career[]>(

            `${this.baseURL}v1/careers`

        );

    }

}