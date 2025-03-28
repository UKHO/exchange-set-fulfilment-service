# Exchange Set Fulfilment Service

## Prerequisites

Please ensure the following are installed on your development machine before building EFS.

* Git
* Docker Desktop or Podman

### Cloning this repository

This repository includes a submodule reference to the ADDS Mock solution. 

To initialise the submodule (pull the code):

* Open a command prompt (cmd.exe)
* Change to the repository root directory

 ```csharp
git submodule update --init --recursive
```

To update the submodule reference in this repository to the latest version of ADDS Mock:

* Open a command prompt (cmd.exe)
* Change to the repository root directory

 ```csharp
git submodule update --remote --merge
```

### Running the solution

> [!CAUTION]
> Before opening and compiling the solution for the first time, you need to copy into the source a copy of the IIC tool distribution and blank workspace archive.

> ```\src\UKHO.ADDS.EFS.Builder.S100\Assets``` - replace the ```workspace_root.zip``` file with the copy that has been given to you.

> ```\src\UKHO.ADDS.EFS.Builder.S100\Tomcat``` - replace the ```xchg-2.7.war``` file with the copy that has been given to you.

Open the EFS.sln in the root of the repository, ensure that the UKHO.ADDS.EFS.LocalHost project is set as start by default, and press f5!

If you are using Podman rather than Docker, you will need to edit the ```appsettings.json``` file in the ```UKHO.ADDS.EFS.LocalHost``` project. Change the setting:

```json
"Containers": {
    "ContainerRuntime": "podman"
    }
```



### TODO
- project structure
- manual running (debug)
- visual studio container dockerfile need to knows
- solution architecture overview (diagram)


