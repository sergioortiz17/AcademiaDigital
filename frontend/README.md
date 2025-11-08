## ✨ Quick Start in `Docker`

> Start the app in Docker

```bash
$ docker-compose up --build 
```

The React UI starts on port `3000` and expects an API server on port `8000` (saved in configuration).

<br />


**API Server URL** - `src/config/constant.js` 

```javascript
const config = {
    ...
    API_SERVER: 'http://localhost:8000/api/'  
};
```




