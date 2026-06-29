#Requires -Module @{ ModuleName='Pester'; ModuleVersion='5.0.0' }

BeforeAll {
    $moduleDir = Split-Path -Parent $PSScriptRoot
    Import-Module (Join-Path $moduleDir 'Lc-Http.psm1') -Force
    Import-Module (Join-Path $moduleDir 'Lc-Auth.psm1') -Force
    Import-Module (Join-Path $moduleDir 'Lc-EventFixtures.psm1') -Force
}

Describe 'Lc-EventFixtures: New-LcSmokeTag' {
    It 'generates tag with SMOKE prefix + Wave 9.a prefix + timestamp + random4' {
        $tag = New-LcSmokeTag -Prefix '9a'
        $tag | Should -Match '^\[SMOKE-9a-\d{8}T\d{6}-[0-9A-F]{4}\]$'
    }
    It 'generates a new tag each call' {
        $t1 = New-LcSmokeTag
        Start-Sleep -Milliseconds 1100  # ensure timestamp differs (second granularity)
        $t2 = New-LcSmokeTag
        $t1 | Should -Not -Be $t2
    }
    It 'persists current tag for subsequent fixture calls' {
        $tag = New-LcSmokeTag -Prefix 'tx'
        Get-LcCurrentRunTag | Should -Be $tag
    }
}

Describe 'Lc-EventFixtures: New-LcFreeEvent' {
    BeforeEach {
        New-LcSmokeTag -Prefix 'unit' | Out-Null
    }

    It 'tags the event title with the current run tag' {
        $script:capturedBody = $null
        Mock -ModuleName Lc-EventFixtures Invoke-LcPost {
            param($Path, $Body, $Bearer)
            $script:capturedBody = $Body
            return [pscustomobject]@{
                Success    = $true
                StatusCode = 201
                Body       = 'aaaaaaaa-bbbb-cccc-dddd-eeeeeeeeeeee'
                Error      = $null
            }
        }
        $r = New-LcFreeEvent
        $r.Success | Should -BeTrue
        $script:capturedBody.title | Should -Match '^\[SMOKE-unit-'
        $script:capturedBody.isFreeEvent | Should -BeTrue
    }

    It 'extracts eventId from raw GUID string response' {
        Mock -ModuleName Lc-EventFixtures Invoke-LcPost {
            return [pscustomobject]@{
                Success    = $true
                StatusCode = 201
                Body       = '"11111111-2222-3333-4444-555555555555"'
                Error      = $null
            }
        }
        $r = New-LcFreeEvent
        $r.EventId | Should -Be '11111111-2222-3333-4444-555555555555'
    }

    It 'extracts eventId from object response with id field' {
        Mock -ModuleName Lc-EventFixtures Invoke-LcPost {
            return [pscustomobject]@{
                Success    = $true
                StatusCode = 201
                Body       = [pscustomobject]@{ id = 'abc-id' }
                Error      = $null
            }
        }
        $r = New-LcFreeEvent
        $r.EventId | Should -Be 'abc-id'
    }

    It 'returns failure on HTTP error' {
        Mock -ModuleName Lc-EventFixtures Invoke-LcPost {
            return [pscustomobject]@{
                Success    = $false
                StatusCode = 400
                Body       = $null
                Error      = 'HTTP 400 validation error'
            }
        }
        $r = New-LcFreeEvent
        $r.Success | Should -BeFalse
        $r.EventId | Should -BeNullOrEmpty
        $r.Error | Should -Match 'Create-event failed'
    }

    It 'sets isFreeEvent=true and isOnline=false' {
        $script:capturedBody = $null
        Mock -ModuleName Lc-EventFixtures Invoke-LcPost {
            param($Path, $Body, $Bearer)
            $script:capturedBody = $Body
            return [pscustomobject]@{
                Success = $true; StatusCode = 201
                Body = '"id"'; Error = $null
            }
        }
        New-LcFreeEvent | Out-Null
        $script:capturedBody.isFreeEvent | Should -BeTrue
        $script:capturedBody.isOnline | Should -BeFalse
        $script:capturedBody.requiresRegistration | Should -BeTrue
    }
}

Describe 'Lc-EventFixtures: New-LcPaidEvent' {
    BeforeEach { New-LcSmokeTag -Prefix 'unit' | Out-Null }

    It 'sets isFreeEvent=false + ticket price' {
        $script:capturedBody = $null
        Mock -ModuleName Lc-EventFixtures Invoke-LcPost {
            param($Path, $Body, $Bearer)
            $script:capturedBody = $Body
            return [pscustomobject]@{ Success = $true; StatusCode = 201; Body = '"id"'; Error = $null }
        }
        New-LcPaidEvent -TicketPrice 18.0 | Out-Null
        $script:capturedBody.isFreeEvent | Should -BeFalse
        $script:capturedBody.ticketPriceAmount | Should -Be 18.0
        $script:capturedBody.ticketPriceCurrency | Should -Be 'USD'
    }
}

Describe 'Lc-EventFixtures: Publish-LcEvent' {
    It 'posts to /api/Events/{id}/publish' {
        $script:capturedPath = $null
        Mock -ModuleName Lc-EventFixtures Invoke-LcPost {
            param($Path, $Body, $Bearer)
            $script:capturedPath = $Path
            return [pscustomobject]@{ Success = $true; StatusCode = 200; Body = $null; Error = $null }
        }
        Publish-LcEvent -EventId 'evt-123' | Out-Null
        $script:capturedPath | Should -Be '/api/Events/evt-123/publish'
    }
}

Describe 'Lc-EventFixtures: New-LcRegistration' {
    It 'posts to /api/Events/{id}/rsvp with userId + quantity' {
        $script:capturedPath = $null
        $script:capturedBody = $null
        Mock -ModuleName Lc-EventFixtures Invoke-LcPost {
            param($Path, $Body, $Bearer)
            $script:capturedPath = $Path
            $script:capturedBody = $Body
            return [pscustomobject]@{ Success = $true; StatusCode = 204; Body = $null; Error = $null }
        }
        New-LcRegistration -EventId 'evt-x' -UserId 'usr-y' -Quantity 2 | Out-Null
        $script:capturedPath | Should -Be '/api/Events/evt-x/rsvp'
        $script:capturedBody.userId | Should -Be 'usr-y'
        $script:capturedBody.quantity | Should -Be 2
    }
}

Describe 'Lc-EventFixtures: Remove-LcFixturesByTag' {
    BeforeEach { New-LcSmokeTag -Prefix 'cleanup' | Out-Null }

    It 'lists tagged events and deletes them' {
        $script:deleteCalls = @()
        $currentTag = Get-LcCurrentRunTag
        Mock -ModuleName Lc-EventFixtures Invoke-LcGet {
            return [pscustomobject]@{
                Success    = $true
                StatusCode = 200
                Body       = [pscustomobject]@{
                    items = @(
                        [pscustomobject]@{ id = 'a'; title = "$currentTag event1" },
                        [pscustomobject]@{ id = 'b'; title = "$currentTag event2" },
                        [pscustomobject]@{ id = 'c'; title = 'unrelated event' }
                    )
                }
                Error      = $null
            }
        }
        Mock -ModuleName Lc-EventFixtures Invoke-LcDelete {
            param($Path)
            $script:deleteCalls += $Path
            return [pscustomobject]@{ Success = $true; StatusCode = 204 }
        }

        $r = Remove-LcFixturesByTag
        $r.Found | Should -Be 2
        $r.Deleted | Should -Be 2
        $r.Failed | Should -Be 0
        $script:deleteCalls.Count | Should -Be 2
    }

    It 'does not delete in DryRun mode' {
        $script:deleteCalls = @()
        $currentTag = Get-LcCurrentRunTag
        Mock -ModuleName Lc-EventFixtures Invoke-LcGet {
            return [pscustomobject]@{
                Success    = $true
                StatusCode = 200
                Body       = [pscustomobject]@{ items = @([pscustomobject]@{ id = 'a'; title = "$currentTag event1" }) }
            }
        }
        Mock -ModuleName Lc-EventFixtures Invoke-LcDelete {
            param($Path)
            $script:deleteCalls += $Path
            return [pscustomobject]@{ Success = $true; StatusCode = 204 }
        }
        $r = Remove-LcFixturesByTag -DryRun $true
        $r.Found | Should -Be 1
        $r.Deleted | Should -Be 0
        $script:deleteCalls.Count | Should -Be 0
    }

    It 'throws when no tag and no current tag set' {
        # Clear current tag by re-importing module fresh
        Remove-Module Lc-EventFixtures -Force -ErrorAction SilentlyContinue
        $moduleDir = Split-Path -Parent $PSScriptRoot
        Import-Module (Join-Path $moduleDir 'Lc-EventFixtures.psm1') -Force
        # Now no current tag
        { Remove-LcFixturesByTag -Tag '' } | Should -Throw -ExpectedMessage '*no tag specified*'
    }
}
