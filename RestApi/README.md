# Ace Model REST API sample

This directory contains sample code that show how to use public REST API provided by Ace Model website.

The samples are divided into two directories:
1. PowerShell - contains PowerShell script with sample code
2. CSharp - contains C# program in .NET 5 with sample code

Both samples perform the same task:
1. Create new Work Item
2. Using newly created Work Item add new SAMPLE family if does not exist
3. Promote Work Item

The task is performed twice using two authorization methods:
1. Ace Model login and password - session cookie based
2. Ace Platform ApiKey

## Prerequisites

Before running samples you need:
1. Running Ace Model website server address - it hosts the REST API.  
   The Ace Model has to be in version 5.7.0 or higher.
2. Valid user name and password for the Ace Model website
3. ApiKey generated on Ace Platform website
4. PowerShell in version 3.0 or higher (to check run: `$PSVersionTable`)
5. .NET 5 SDK (to check run: `dotnet --version`)

Both sample projects are incomplete and require to provide Ace Model address,
login and password and Ace Platform ApiKey in their sources to run correctly.
The exact places in the code are marked with `TODO` comments.

## Running samples

### PowerShell

Remember to provide Ace Model address, login and password and Ace Platform ApiKey in the script source.

Enable running the scripts:
```powershell
Set-ExecutionPolicy -Scope CurrentUser Unrestricted
```

And run the script:
```powershell
.\SampleRestApi.ps1
```

### CSharp

Remember to provide Ace Model address, login and password and Ace Platform ApiKey in the program source.

Compile and run program executing command:
```
dotnet run SampleRestApi.csproj
```

## API Overview

Full documentation will be available in Ace Model help.  
To query master data use `none` in place of `?wi`, but remember that master is read-only.

```
GET   api/v1/wi                                          get all work items
GET   api/v1/wi/?wi                                      get work item
POST  api/v1/wi                                          create work item
PUT   api/v1/wi/?wi/promote                              promote work item
PUT   api/v1/wi/?wi/close                                close work item

GET   api/v1/wi/?wi/library/families                     get all families
GET   api/v1/wi/?wi/library/families/?code               get family
DEL   api/v1/wi/?wi/library/families/?code               remove family
POST  api/v1/wi/?wi/library/families                     create family
PUT   api/v1/wi/?wi/library/families/?code               update family

GET   api/v1/wi/?wi/library/features                     get all features
GET   api/v1/wi/?wi/library/features/?familyCode/?code   get specific one
DEL   api/v1/wi/?wi/library/features/?familyCode/?code   remove specific one
POST  api/v1/wi/?wi/library/features                     create a feature
PUT   api/v1/wi/?wi/library/features/?familyCode/?code   update a feature
```

