#Requires -Version 3.0

# TODO: Fill in the Ace Model address, login and password and Ace Platform ApiKey
$address  = ""
$login    = ""
$password = ""
$apiKey   = ""

if ([string]::IsNullOrWhiteSpace($address)  -or
    [string]::IsNullOrWhiteSpace($login)    -or
    [string]::IsNullOrWhiteSpace($password) -or
    [string]::IsNullOrWhiteSpace($apiKey)) {
  throw "Sample cannot run without address and credentials"
}

# if address ends with slash remove it
if ($address.EndsWith("/") -or $address.EndsWith("\")) {
  $address = $address.Substring(0, $address.Length -1)
}
Write-Verbose "Using base address: '$address'" -Verbose


function Get-RequestParamsUsingAceModelAuthorization($baseAddress, $aceModelLogin, $aceModelPassword)
{
  Write-Verbose "Using Ace Model login and password authorization" -Verbose

  # initialize the session and get verification token
  $loginParams = @{
    Uri         = "$baseAddress/api/public/v1/auth/local/login"
    Method      = "Post"
    ContentType = "application/json;charset=UTF-8"
    Session     = "aceRestApiSession"
    Body        = (@{ Username=$aceModelLogin; Password=$aceModelPassword } | ConvertTo-Json)
  }
  $loginResponse = Invoke-RestMethod @loginParams
  $token = $loginResponse.token


  $headers = @{
    "Accept"          = "application/json, text/plain, */*"
    "Accept-Language" = "en-US,en;q=0.9,pl;q=0.8"
    # REQUIRED when using Ace Model authorization
    # You have to pass token in header with each request
    "X-RequestVerificationToken" = $token
  }

  $params = @{
    # REQUIRED when using Ace Model authorization
    # You have to pass the session cookies with each request
    WebSession  = $aceRestApiSession
    ContentType = "application/json;charset=UTF-8"
    Headers     = $headers
  }

  return $params
}

function Get-RequestParamsUsingAcePlatformAuthorization($baseAddress, $acePlatformApiKey)
{
  Write-Verbose "Using Ace Platform ApiKey authorization" -Verbose

  $headers = @{
    "Accept"          = "application/json, text/plain, */*"
    "Accept-Language" = "en-US,en;q=0.9,pl;q=0.8"
    # REQUIRED when using Ace Platform ApiKey
    # You have to pass ApkiKey in header with each request
    "Authorization" = "ApiKey $acePlatformApiKey"
  }

  $params = @{
    # no session required here
    ContentType = "application/json;charset=UTF-8"
    Headers     = $headers
  }

  return $params
}


function Create-SampleFamily($baseAddress, $requestParams)
{
  $familyCode = "SAMPLE"
  Write-Verbose "Creating family with code $familyCode" -Verbose

  # 1. Create new Work Item
  $newWorkItem = @{
    Name        = "SampleWorkItem"
    Description = "Sample description"
  }

  $requestParams.Uri    = "$baseAddress/api/v1/wi/"
  $requestParams.Method = "Post"
  $requestParams.Body   = $newWorkItem | ConvertTo-Json

  $result = Invoke-RestMethod @requestParams
  $wi     = $result.id # <-- created Work Item number used in next requests

  Write-Verbose "Created new Work Item: $wi" -Verbose


  # 2. Using newly created Work Item add new SAMPLE family if does not exist
  $requestParams.Uri    = "http://localhost:3000/api/v1/wi/$wi/library/families/$familyCode"
  $requestParams.Method = "Get"
  $requestParams.Body   = $null

  $family = $null
  try {
    $family = Invoke-RestMethod @requestParams
  } catch {
    # with the error we are getting JSON with error details
    $errors = ($_.ErrorDetails.Message | ConvertFrom-Json).errors
    foreach ($e in $errors) {
      Write-Warning "Got error: $($e.message)"
    }

    # if we've got NotFound code then proceed, otherwise just rethrow
    if ($_.Exception.Response.StatusCode -ne "NotFound") {
      throw $_
    }
  }

  # we already have SAMPLE family, so close the Work Item and return
  if ($family -ne $null) {
    Write-Verbose "Family $familyCode already exists, closing Work Item $wi" -Verbose

    $requestParams.Uri    = "$baseAddress/api/v1/wi/$wi/close"
    $requestParams.Method = "Put"
    $requestParams.Body   = $null
    $result = Invoke-RestMethod @requestParams

    return $family
  }

  # we do not have the family - create it
  $newFamily = @{
    code              = $familyCode
    description       = "Test numeric family"
    lifeCycle         = "Concept"
    familyType        = "Numeric"
    precision         = 2
    minValue          = 10
    maxValue          = 100
  }
  $requestParams.Uri    = "$baseAddress/api/v1/wi/$wi/library/families"
  $requestParams.Method = "Post"
  $requestParams.Body   = $newFamily | ConvertTo-Json
  $family = Invoke-RestMethod @requestParams

  Write-Verbose "Created new family $familyCode, promote Work Item $wi" -Verbose

  # 3. Promote Work Item
  $requestParams.Uri    = "$baseAddress/api/v1/wi/$wi/promote"
  $requestParams.Method = "Put"
  $requestParams.Body   = $null
  $result = Invoke-RestMethod @requestParams

  return $family
}


Create-SampleFamily $address (Get-RequestParamsUsingAceModelAuthorization    $address $login $password)
Create-SampleFamily $address (Get-RequestParamsUsingAcePlatformAuthorization $address $apiKey)

