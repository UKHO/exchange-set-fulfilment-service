# Exchange Set Fulfilment Service

## Prerequisites

Please ensure the following are installed on your development machine before building EFS.

* Git
* Docker Desktop

### Cloning this repository

This repository no longer includes a submodule. ADDS Mock is now provided via a NuGet package.

### Running the solution

> [!CAUTION]
> Before opening and compiling the solution for the first time, you need to copy into the source a copy of the IIC tool distribution and blank workspace archive.

> Copy ```root3.tar.gz``` and ```xchg-7.6.war``` into the root of the ```\src\UKHO.ADDS.EFS.Builder.S100``` directory. You will have been given these files separately.

Open the EFS.sln in the root of the repository, ensure that the UKHO.ADDS.EFS.LocalHost project is set as start by default, and press f5!

### TODO
- project structure
- manual running (debug)
- visual studio container dockerfile need to knows
- solution architecture overview (diagram)


