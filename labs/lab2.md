## Lab 2: Run container app in App Service (Linux)

![lab2 architecture](./msa-lab2.png)

### 2.1 Deploy container to App Service

Create a new App service and deploy container to App Service. Note that `App Service Plan` and a `Web App` are created automatically during creation. 

Use following `Web App` name: `<unique-prefix>searchsvc.azurewebsites.net`. (e.g., iksearchsvc.azurewebsites.net)

> :warning: Hard coded secrets/credentials in your application is not recommended for security. Instead, it is recommended to pass secrets via _ENVIRONMENT_ variables in App service configuration.

// capture

Test your application. Just like the previous local test, it simulate to search the web content.

```bash
curl -s https://iksearchsvc.azurewebsites.net/api/search/web | jq
```
```bash
{
  "title": "web",
  "url": "https://dotnet.microsoft.com/",
  "snippet": "Free. Cross-platform. Open source.",
  "log": "2021-07-30T09:08:59Z, result for \"dotnet\", from backend 80, process time 0 msec",
  "time": 0
}
```

### 2.2 Deploy more apps to App Service

Deploy 3 more apps with same container.

- iksearchweb.azurewebsites.net
- iksearchimages.azurewebsites.net
- iksearchvideos.azurewebsites.net

Add `HTTP_ENDPOINT` environment variable and the value in `iksearchsvc`'s configuration. This will simulate to search the web, images and videos contents.

```
https://iksearchweb.azurewebsites.net/api/search/web;https://iksearchimages.azurewebsites.net/api/search/images;https://iksearchvideos.azurewebsites.net/api/search/videos
```

Test with new search api endpoint (frontend). FYI, Frontend servce api (/api/web) will call 3 backend search apis (web, images, videos) and return aggregate results.

```bash
curl -s https://iksearchsvc.azurewebsites.net/api/web/seq | jq
```

### 2.3 Test performance using simulated environment

Test search with simulated random delay (10 ~ 100 ms)

```bash
curl -s https://iksearchsvc.azurewebsites.net/api/web/seq?delay=true | jq
```

Run `Apache Benchmarks` to see the performance comparison between sequential and concurrent backend requests.

```bash
ab -n 100 -c 2 https://iksearchsvc.azurewebsites.net/api/web/seq?delay=true
```

```bash
ab -n 100 -c 2 https://iksearchsvc.azurewebsites.net/api/web/para?delay=true
```

### 2.4 Monitor your application

Open __Application Insight__ and review the performance. __Application Insight__ is a great observability tool for monitoring performance, troubleshooting and understanding user behaviors.

// capture

### 2.5 Deploy application safely with b/g deployment (Optional)

Modify you're application and push new container to ACR.

> update the line 22 in `app/Controllers/WebController.cs` to `private const string _version = "v2";`

Create a _test slot_ for `iksearchsvc` and deploy the new container to the _test slot_.

// capture

Swap slots to deploy new app from _test_ to _production_ slot.

Run following command before swapping slots.

```
watch -n 1 curl -s https://iksearchsvc.azurewebsites.net/api/web
```

### 2.6 Advanced: Private connectiviety with Azure Web App (Optional)

Reference [document](https://docs.microsoft.com/en-us/azure/app-service/networking/private-endpoint)

### 2.7 Advanced: Referencing KV for Azure Web App (Optional)

Reference [document](https://docs.microsoft.com/en-us/azure/app-service/app-service-key-vault-references)