## Lab 0: Prepare this lab

You'll need to prepare following environment and provision services in order to run labs.

### Local environment

Prepare local environment for building and testing.

- basic environment
    - resource group: `msa-dotnet-rg`
    - create virtual network: `msa-vnet`
        - virtual network: `10.0.0.0/8`
        - subnet: `vm-subnet, 10.0.1.0/24`, `appsvc-subnet, 10.0.2.0/24`, `aks-subnet, 10.1.0.0/16`
- provision a VM
	- Ubuntu 18.04 (`Standard_D2as_v4`)
- install necessary tools
	- .net sdk on Ubuntu 18.04: https://docs.microsoft.com/en-us/dotnet/core/install/linux-ubuntu#1804-
	- docker: https://docs.nvidia.com/datacenter/cloud-native/container-toolkit/install-guide.html#setting-up-docker
		- add permission: `sudo usermod -a -G docker $USER`
	- kubectl: https://kubernetes.io/ko/docs/tasks/tools/install-kubectl-linux/
	- azure cli: https://docs.microsoft.com/en-us/cli/azure/install-azure-cli-linux?pivots=apt
	- extra: jq, ab
	    `sudo apt install -y jq apache2-utils`
- git clone sample app
	- `git clone https://github.com/iljoong/msa-dotnetapp`

### Azure services

Provision following Azure services before the lab.

- [ACR](https://docs.microsoft.com/en-us/azure/container-registry/container-registry-intro)
- [Application Insight](https://docs.microsoft.com/en-us/azure/azure-monitor/app/app-insights-overview)
- [Event Hub](https://docs.microsoft.com/en-us/azure/event-hubs/event-hubs-about) (optional)
- [Key Vault](https://docs.microsoft.com/en-us/azure/key-vault/general/overview) (optional)

You will create other Azure services during the lab.

- [App Services (Linux)](https://docs.microsoft.com/en-us/azure/app-service/overview)
- [AKS](https://docs.microsoft.com/en-us/azure/aks/intro-kubernetes)
