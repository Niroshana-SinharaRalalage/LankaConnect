#Requires -Module @{ ModuleName='Pester'; ModuleVersion='5.0.0' }

BeforeAll {
    $moduleDir = Split-Path -Parent $PSScriptRoot
    Import-Module (Join-Path $moduleDir 'Lc-Http.psm1') -Force
    Import-Module (Join-Path $moduleDir 'Lc-Auth.psm1') -Force
}

Describe 'Lc-Auth: cache lifecycle' {
    BeforeEach {
        Clear-LcAuthCache
    }
    It 'starts with empty cache' {
        $cache = Get-LcAuthCache
        $cache.Bearer | Should -BeNullOrEmpty
        $cache.UserId | Should -BeNullOrEmpty
    }
    It 'Test-LcBearerExpired returns true for empty cache' {
        Test-LcBearerExpired | Should -BeTrue
    }
}

Describe 'Lc-Auth: Invoke-LcLogin' {
    BeforeEach {
        Clear-LcAuthCache
        $env:LC_BEARER = $null
        $env:LC_USER_ID = $null
    }

    It 'populates cache on successful login' {
        Mock -ModuleName Lc-Auth Invoke-LcRequest {
            return [pscustomobject]@{
                Success    = $true
                StatusCode = 200
                Body       = [pscustomobject]@{
                    accessToken     = 'test-bearer-token'
                    refreshToken    = 'test-refresh-token'
                    tokenExpiresAt  = ([datetime]::UtcNow.AddMinutes(30)).ToString('o')
                    user            = [pscustomobject]@{
                        userId = '11111111-2222-3333-4444-555555555555'
                        email  = 'test@example.com'
                    }
                }
                Error      = $null
            }
        }
        $r = Invoke-LcLogin -Email 'test@example.com' -Password 'pw'
        $r.Success | Should -BeTrue
        $r.UserId | Should -Be '11111111-2222-3333-4444-555555555555'
        $r.Bearer | Should -Be 'test-bearer-token'

        $cache = Get-LcAuthCache
        $cache.Bearer | Should -Be 'test-bearer-token'
        $cache.UserId | Should -Be '11111111-2222-3333-4444-555555555555'
        $env:LC_BEARER | Should -Be 'test-bearer-token'
    }

    It 'returns failure on auth error' {
        Mock -ModuleName Lc-Auth Invoke-LcRequest {
            return [pscustomobject]@{
                Success    = $false
                StatusCode = 401
                Body       = $null
                Error      = 'HTTP 401'
            }
        }
        $r = Invoke-LcLogin -Email 'bad@example.com' -Password 'wrong'
        $r.Success | Should -BeFalse
        $r.Bearer | Should -BeNullOrEmpty
        $r.Error | Should -Match 'Login failed'
    }

    It 'returns failure on missing accessToken in response' {
        Mock -ModuleName Lc-Auth Invoke-LcRequest {
            return [pscustomobject]@{
                Success    = $true
                StatusCode = 200
                Body       = [pscustomobject]@{ user = [pscustomobject]@{ userId = 'x' } }
                Error      = $null
            }
        }
        $r = Invoke-LcLogin -Email 'test@example.com' -Password 'pw'
        $r.Success | Should -BeFalse
        $r.Error | Should -Match 'missing accessToken'
    }
}

Describe 'Lc-Auth: Get-LcBearer caching + auto-refresh' {
    BeforeEach {
        Clear-LcAuthCache
        $env:LC_BEARER = $null
    }

    It 'refreshes when cache is empty' {
        $script:loginCalls = 0
        Mock -ModuleName Lc-Auth Invoke-LcRequest {
            $script:loginCalls++
            return [pscustomobject]@{
                Success    = $true
                StatusCode = 200
                Body       = [pscustomobject]@{
                    accessToken     = "token-$script:loginCalls"
                    tokenExpiresAt  = ([datetime]::UtcNow.AddMinutes(30)).ToString('o')
                    user            = [pscustomobject]@{ userId = 'user-1' }
                }
            }
        }

        $bearer = Get-LcBearer
        $bearer | Should -Be 'token-1'
        $script:loginCalls | Should -Be 1
    }

    It 'uses cache on subsequent calls when not expired' {
        $script:loginCalls = 0
        Mock -ModuleName Lc-Auth Invoke-LcRequest {
            $script:loginCalls++
            return [pscustomobject]@{
                Success    = $true
                StatusCode = 200
                Body       = [pscustomobject]@{
                    accessToken     = "token-$script:loginCalls"
                    tokenExpiresAt  = ([datetime]::UtcNow.AddMinutes(30)).ToString('o')
                    user            = [pscustomobject]@{ userId = 'user-1' }
                }
            }
        }

        $b1 = Get-LcBearer
        $b2 = Get-LcBearer
        $b1 | Should -Be 'token-1'
        $b2 | Should -Be 'token-1'  # Same — cached
        $script:loginCalls | Should -Be 1
    }

    It 'refreshes when token is near expiry' {
        $script:loginCalls = 0
        Mock -ModuleName Lc-Auth Invoke-LcRequest {
            $script:loginCalls++
            return [pscustomobject]@{
                Success    = $true
                StatusCode = 200
                Body       = [pscustomobject]@{
                    accessToken     = "token-$script:loginCalls"
                    # First login: token expires in 30 sec (within 60-sec grace -> immediate refresh)
                    tokenExpiresAt  = ([datetime]::UtcNow.AddSeconds(30)).ToString('o')
                    user            = [pscustomobject]@{ userId = 'user-1' }
                }
            }
        }

        $b1 = Get-LcBearer
        $b2 = Get-LcBearer  # Should re-fetch because near expiry
        $b1 | Should -Be 'token-1'
        $b2 | Should -Be 'token-2'
        $script:loginCalls | Should -Be 2
    }

    It 'throws when login fails on auto-refresh' {
        Mock -ModuleName Lc-Auth Invoke-LcRequest {
            return [pscustomobject]@{
                Success    = $false
                StatusCode = 401
                Body       = $null
                Error      = 'unauthorized'
            }
        }
        { Get-LcBearer } | Should -Throw -ExpectedMessage '*Cannot obtain bearer token*'
    }
}

Describe 'Lc-Auth: Get-LcUserId' {
    BeforeEach {
        Clear-LcAuthCache
    }
    It 'returns cached userId without re-login' {
        Mock -ModuleName Lc-Auth Invoke-LcRequest {
            return [pscustomobject]@{
                Success    = $true
                StatusCode = 200
                Body       = [pscustomobject]@{
                    accessToken     = 'token'
                    tokenExpiresAt  = ([datetime]::UtcNow.AddMinutes(30)).ToString('o')
                    user            = [pscustomobject]@{ userId = 'user-foo' }
                }
            }
        }
        $uid = Get-LcUserId
        $uid | Should -Be 'user-foo'
    }
}
