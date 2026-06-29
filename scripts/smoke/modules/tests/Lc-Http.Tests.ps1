#Requires -Module @{ ModuleName='Pester'; ModuleVersion='5.0.0' }

BeforeAll {
    $moduleDir = Split-Path -Parent $PSScriptRoot
    Import-Module (Join-Path $moduleDir 'Lc-Http.psm1') -Force
}

Describe 'Lc-Http: base URL management' {
    It 'sets and gets base URL' {
        Set-LcHttpBaseUrl -Url 'https://example.test'
        Get-LcHttpBaseUrl | Should -Be 'https://example.test'
    }
    It 'trims trailing slashes' {
        Set-LcHttpBaseUrl -Url 'https://example.test/'
        Get-LcHttpBaseUrl | Should -Be 'https://example.test'
    }
}

Describe 'Lc-Http: retry policy configuration' {
    It 'accepts custom retry policy' {
        { Set-LcHttpRetryPolicy -MaxAttempts 5 -InitialDelayMs 100 -BackoffMultiplier 1.5 -RetryOnStatusCodes @(503) } | Should -Not -Throw
        # Reset to default
        Set-LcHttpRetryPolicy
    }
}

Describe 'Lc-Http: Invoke-LcRequest with mocked Invoke-LcHttpRaw' {
    BeforeEach {
        Set-LcHttpBaseUrl -Url 'https://example.test'
        Set-LcHttpRetryPolicy -MaxAttempts 1 -InitialDelayMs 1
        $env:LC_BEARER = $null
    }

    It 'returns success result for 200 response' {
        Mock -ModuleName Lc-Http Invoke-LcHttpRaw {
            return [pscustomobject]@{ StatusCode = 200; Body = @{ ok = $true }; Headers = @{}; NetworkError = $null }
        }
        $r = Invoke-LcRequest -Method GET -Path '/api/test'
        $r.Success | Should -BeTrue
        $r.StatusCode | Should -Be 200
        $r.Body.ok | Should -BeTrue
        $r.Error | Should -BeNullOrEmpty
        $r.AttemptCount | Should -Be 1
    }

    It 'returns failure result for 404 response' {
        Mock -ModuleName Lc-Http Invoke-LcHttpRaw {
            return [pscustomobject]@{ StatusCode = 404; Body = @{ error = 'not found' }; Headers = @{}; NetworkError = $null }
        }
        $r = Invoke-LcRequest -Method GET -Path '/api/test'
        $r.Success | Should -BeFalse
        $r.StatusCode | Should -Be 404
        $r.Error | Should -Be 'HTTP 404'
    }

    It 'retries on 503 and succeeds on second attempt' {
        Set-LcHttpRetryPolicy -MaxAttempts 3 -InitialDelayMs 1 -BackoffMultiplier 1.0 -RetryOnStatusCodes @(503)
        $script:callCount = 0
        Mock -ModuleName Lc-Http Invoke-LcHttpRaw {
            $script:callCount++
            if ($script:callCount -lt 2) {
                return [pscustomobject]@{ StatusCode = 503; Body = $null; Headers = @{}; NetworkError = $null }
            } else {
                return [pscustomobject]@{ StatusCode = 200; Body = @{ ok = $true }; Headers = @{}; NetworkError = $null }
            }
        }
        $r = Invoke-LcRequest -Method GET -Path '/api/test'
        $r.Success | Should -BeTrue
        $r.AttemptCount | Should -Be 2
    }

    It 'captures network errors as failure' {
        Mock -ModuleName Lc-Http Invoke-LcHttpRaw {
            return [pscustomobject]@{ StatusCode = 0; Body = $null; Headers = $null; NetworkError = 'Connection refused' }
        }
        $r = Invoke-LcRequest -Method GET -Path '/api/test'
        $r.Success | Should -BeFalse
        $r.StatusCode | Should -Be 0
        $r.Error | Should -Match 'Connection refused'
    }

    It 'injects bearer token from parameter' {
        Mock -ModuleName Lc-Http Invoke-LcHttpRaw {
            param($Method, $Uri, $Headers, $JsonBody, $TimeoutSec)
            return [pscustomobject]@{ StatusCode = 200; Body = $Headers; Headers = @{}; NetworkError = $null }
        }
        $r = Invoke-LcRequest -Method GET -Path '/api/test' -Bearer 'my-token'
        $r.Body['Authorization'] | Should -Be 'Bearer my-token'
    }

    It 'injects bearer token from $env:LC_BEARER when no param given' {
        $env:LC_BEARER = 'env-token'
        Mock -ModuleName Lc-Http Invoke-LcHttpRaw {
            param($Method, $Uri, $Headers, $JsonBody, $TimeoutSec)
            return [pscustomobject]@{ StatusCode = 200; Body = $Headers; Headers = @{}; NetworkError = $null }
        }
        $r = Invoke-LcRequest -Method GET -Path '/api/test'
        $r.Body['Authorization'] | Should -Be 'Bearer env-token'
        $env:LC_BEARER = $null
    }

    It 'omits Authorization header when Bearer is $null' {
        Mock -ModuleName Lc-Http Invoke-LcHttpRaw {
            param($Method, $Uri, $Headers, $JsonBody, $TimeoutSec)
            return [pscustomobject]@{ StatusCode = 200; Body = $Headers; Headers = @{}; NetworkError = $null }
        }
        $r = Invoke-LcRequest -Method GET -Path '/api/test' -Bearer $null
        $r.Body.ContainsKey('Authorization') | Should -BeFalse
    }

    It 'sends body for POST requests' {
        Mock -ModuleName Lc-Http Invoke-LcHttpRaw {
            param($Method, $Uri, $Headers, $JsonBody, $TimeoutSec)
            return [pscustomobject]@{ StatusCode = 200; Body = @{ receivedBody = $JsonBody }; Headers = @{}; NetworkError = $null }
        }
        $r = Invoke-LcRequest -Method POST -Path '/api/test' -Body @{ foo = 'bar' }
        $r.Body.receivedBody | Should -Be '{"foo":"bar"}'
    }

    It 'uses absolute URL when Path is absolute' {
        Mock -ModuleName Lc-Http Invoke-LcHttpRaw {
            param($Method, $Uri, $Headers, $JsonBody, $TimeoutSec)
            return [pscustomobject]@{ StatusCode = 200; Body = @{ uri = $Uri }; Headers = @{}; NetworkError = $null }
        }
        $r = Invoke-LcRequest -Method GET -Path 'https://other.test/path' -Bearer $null
        $r.Body.uri | Should -Be 'https://other.test/path'
    }
}

Describe 'Lc-Http: convenience verb wrappers' {
    BeforeEach {
        Set-LcHttpRetryPolicy -MaxAttempts 1 -InitialDelayMs 1
        $env:LC_BEARER = $null
        Mock -ModuleName Lc-Http Invoke-LcHttpRaw {
            param($Method, $Uri, $Headers, $JsonBody, $TimeoutSec)
            return [pscustomobject]@{ StatusCode = 200; Body = @{ method = $Method }; Headers = @{}; NetworkError = $null }
        }
    }
    It 'Invoke-LcGet uses GET'       { (Invoke-LcGet    -Path '/x').Body.method | Should -Be 'GET' }
    It 'Invoke-LcPost uses POST'     { (Invoke-LcPost   -Path '/x' -Body @{}).Body.method | Should -Be 'POST' }
    It 'Invoke-LcPut uses PUT'       { (Invoke-LcPut    -Path '/x' -Body @{}).Body.method | Should -Be 'PUT' }
    It 'Invoke-LcPatch uses PATCH'   { (Invoke-LcPatch  -Path '/x' -Body @{}).Body.method | Should -Be 'PATCH' }
    It 'Invoke-LcDelete uses DELETE' { (Invoke-LcDelete -Path '/x').Body.method | Should -Be 'DELETE' }
}
