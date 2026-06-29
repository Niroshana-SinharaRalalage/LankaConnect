#Requires -Module @{ ModuleName='Pester'; ModuleVersion='5.0.0' }

BeforeAll {
    $moduleDir = Split-Path -Parent $PSScriptRoot
    Import-Module (Join-Path $moduleDir 'Lc-Http.psm1') -Force
    Import-Module (Join-Path $moduleDir 'Lc-Auth.psm1') -Force
    Import-Module (Join-Path $moduleDir 'Lc-EventFixtures.psm1') -Force  # for Get-LcCurrentRunTag
    Import-Module (Join-Path $moduleDir 'Lc-CommonFixtures.psm1') -Force
}

Describe 'Lc-CommonFixtures: New-LcNewsletter' {
    BeforeEach {
        New-LcSmokeTag -Prefix 'nl-unit' | Out-Null
    }

    It 'creates a newsletter with tagged title' {
        $script:capturedBody = $null
        Mock -ModuleName Lc-CommonFixtures Invoke-LcPost {
            param($Path, $Body, $Bearer)
            $script:capturedBody = $Body
            return [pscustomobject]@{
                Success    = $true
                StatusCode = 201
                Body       = '"newsletter-id-123"'
                Error      = $null
            }
        }
        $r = New-LcNewsletter
        $r.Success | Should -BeTrue
        $r.NewsletterId | Should -Be 'newsletter-id-123'
        $script:capturedBody.title | Should -Match '^\[SMOKE-nl-unit-'
        $script:capturedBody.targetAllLocations | Should -BeTrue
    }

    It 'respects TargetAllLocations parameter' {
        $script:capturedBody = $null
        Mock -ModuleName Lc-CommonFixtures Invoke-LcPost {
            param($Path, $Body, $Bearer)
            $script:capturedBody = $Body
            return [pscustomobject]@{ Success = $true; StatusCode = 201; Body = '"id"'; Error = $null }
        }
        New-LcNewsletter -TargetAllLocations $false | Out-Null
        $script:capturedBody.targetAllLocations | Should -BeFalse
    }

    It 'returns failure on HTTP error' {
        Mock -ModuleName Lc-CommonFixtures Invoke-LcPost {
            return [pscustomobject]@{
                Success    = $false
                StatusCode = 500
                Body       = $null
                Error      = 'HTTP 500'
            }
        }
        $r = New-LcNewsletter
        $r.Success | Should -BeFalse
        $r.NewsletterId | Should -BeNullOrEmpty
        $r.Error | Should -Match 'Create-newsletter failed'
    }
}

Describe 'Lc-CommonFixtures: Remove-LcNewsletterByTag' {
    BeforeEach { New-LcSmokeTag -Prefix 'nl-cleanup' | Out-Null }

    It 'cleans up tagged newsletters' {
        $script:deleteCalls = @()
        $tag = Get-LcCurrentRunTag
        Mock -ModuleName Lc-CommonFixtures Invoke-LcGet {
            return [pscustomobject]@{
                Success    = $true
                StatusCode = 200
                Body       = [pscustomobject]@{
                    items = @(
                        [pscustomobject]@{ id = 'n1'; title = "$tag newsletter1" },
                        [pscustomobject]@{ id = 'n2'; title = 'other' }
                    )
                }
                Error      = $null
            }
        }
        Mock -ModuleName Lc-CommonFixtures Invoke-LcDelete {
            param($Path)
            $script:deleteCalls += $Path
            return [pscustomobject]@{ Success = $true; StatusCode = 204 }
        }
        $r = Remove-LcNewsletterByTag
        $r.Found | Should -Be 1
        $r.Deleted | Should -Be 1
        $script:deleteCalls[0] | Should -Be '/api/newsletters/n1'
    }
}
