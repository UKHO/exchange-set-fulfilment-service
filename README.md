# Exchange Set Fulfilment Service

## Prerequisites

Please ensure the following are installed on your development machine before building EFS.

* Git
* Docker Desktop

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

Open the EFS.sln in the root of the repository, ensure that the UKHO.ADDS.EFS.LocalHost project is set as start by default, and press f5!

### TODO
- manual running (debug)
- visual studio container dockerfile need to knows
- solution architecture overview (diagram)


