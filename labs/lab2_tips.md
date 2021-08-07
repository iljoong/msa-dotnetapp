## Lab 2: Tips

###  Step by step guide to deploy App Service

Following is the step by step guide to deploy an Web App/App Service

![step 1](./images/lab2_01.png)
![step 2](./images/lab2_02.png)
![step 3](./images/lab2_03.png)
![step 4](./images/lab2_04.png)
![step 5](./images/lab2_05.png)

### Change deployment after deploying Web App

If you accidentally create with wrong registry option, you can change in the __Deployment Center__ after creation.

![Depolyment Center](./images/lab2_07.png)

### Fix errors

If you get an output like below, you haven't set `HTTP_ENDPOINT` in the configuration.
```
curl -s https://iksearchsvc.azurewebsites.net/api/web/seq | jq   
{
  "message": "Cannot assign requested address (localhost:5000)"
}
```

If you get an output like below, you didn't correctly configure `HTTP_ENDPOINT` in the configuration.
```
curl -s https://iksearchsvc.azurewebsites.net/api/web/seq | jq
{
  "message": "Name or service not known (iksearchimages.azurewebsites.net:443)"
}
```