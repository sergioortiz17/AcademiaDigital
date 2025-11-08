
# Django API Server

Simple api built with Python / Django Rest / JWT Auth. The authentication flow is built with [json web tokens](https://jwt.io).

<br />

## ✨ Quick Start in `Docker`

> Start the app in Docker

```bash
$ docker-compose up --build  
```

The API server will start using the PORT `8000`.


<br />

> **Login** - `api/users/login`

```
POST /api/users/login
Content-Type: application/json

{
    "password":"string", 
    "email":"string"
}
```

<br />

> **Logout** - `api/users/logout`

```
POST api/users/logout
Content-Type: application/json
authorization: JWT_TOKEN (returned by Login request)

{
    "token":"JWT_TOKEN"
}
```


<br />
