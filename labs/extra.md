## Troubleshooting

### Debugging app

Running application in producton environment.

> see [doc](https://docs.microsoft.com/en-us/dotnet/core/tools/dotnet-run) for more options
```
dotnet run --launch-profile production
```

Launching app in different port.

```
export APP_PORT=5001
dotnet run
```

Debugging app settings

```
curl http://<ip or fqdn>/debug
```

### AKS

Installing curl in Pod.

```
apt -qq update
apt update
apt install -y curl
```