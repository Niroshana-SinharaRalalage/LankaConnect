#Requires -Module @{ ModuleName='Pester'; ModuleVersion='5.0.0' }

BeforeAll {
    $moduleDir = Split-Path -Parent $PSScriptRoot
    Import-Module (Join-Path $moduleDir 'Lc-Assertion.psm1') -Force
}

Describe 'Lc-Assertion: HTTP status assertions' {
    It 'Assert-Http200 returns true for 200 result' {
        $r = [pscustomobject]@{ StatusCode = 200; Body = @{}; Error = $null; Method = 'GET'; Url = '/x' }
        Assert-Http200 -Result $r | Should -BeTrue
    }
    It 'Assert-Http200 throws on 404' {
        $r = [pscustomobject]@{ StatusCode = 404; Body = $null; Error = 'HTTP 404'; Method = 'GET'; Url = '/x' }
        { Assert-Http200 -Result $r } | Should -Throw -ExpectedMessage '*expected=200 actual=404*'
    }
    It 'Assert-Http201 returns true for 201 result' {
        $r = [pscustomobject]@{ StatusCode = 201; Body = @{}; Error = $null; Method = 'POST'; Url = '/x' }
        Assert-Http201 -Result $r | Should -BeTrue
    }
    It 'Assert-Http204 returns true for 204 result' {
        $r = [pscustomobject]@{ StatusCode = 204; Body = $null; Error = $null; Method = 'DELETE'; Url = '/x' }
        Assert-Http204 -Result $r | Should -BeTrue
    }
    It 'Assert-Http403 returns true for 403 (inverted assertion for SKIP_PERMISSION endpoints)' {
        $r = [pscustomobject]@{ StatusCode = 403; Body = $null; Error = 'HTTP 403'; Method = 'GET'; Url = '/x' }
        Assert-Http403 -Result $r | Should -BeTrue
    }
    It 'Assert-Http404 returns true for 404' {
        $r = [pscustomobject]@{ StatusCode = 404; Body = $null; Error = 'HTTP 404'; Method = 'GET'; Url = '/x' }
        Assert-Http404 -Result $r | Should -BeTrue
    }
    It 'throws on null result' {
        # Pester's parameter binding for [Parameter(Mandatory)]$Result rejects $null before our custom message fires.
        { Assert-Http200 -Result $null } | Should -Throw
    }
}

Describe 'Lc-Assertion: Assert-JsonField + Assert-JsonPath' {
    It 'Assert-JsonField matches top-level field' {
        $r = [pscustomobject]@{ StatusCode = 200; Body = [pscustomobject]@{ status = 'Confirmed' } }
        Assert-JsonField -Result $r -FieldName 'status' -ExpectedValue 'Confirmed' | Should -BeTrue
    }
    It 'Assert-JsonField throws on mismatch' {
        $r = [pscustomobject]@{ StatusCode = 200; Body = [pscustomobject]@{ status = 'Pending' } }
        { Assert-JsonField -Result $r -FieldName 'status' -ExpectedValue 'Confirmed' } | Should -Throw
    }
    It 'Assert-JsonPath descends nested paths' {
        $r = [pscustomobject]@{ StatusCode = 200; Body = [pscustomobject]@{ user = [pscustomobject]@{ email = 'foo@bar.com' } } }
        Assert-JsonPath -Result $r -Path 'user.email' -ExpectedValue 'foo@bar.com' | Should -BeTrue
    }
    It 'Assert-JsonPath throws on null in path' {
        $r = [pscustomobject]@{ StatusCode = 200; Body = [pscustomobject]@{ user = $null } }
        { Assert-JsonPath -Result $r -Path 'user.email' -ExpectedValue 'foo' } | Should -Throw -ExpectedMessage '*null along path*'
    }
    It 'Get-LcJsonPath returns null when path missing' {
        Get-LcJsonPath -Object ([pscustomobject]@{ a = $null }) -Path 'a.b' | Should -BeNullOrEmpty
    }
    It 'Get-LcJsonPath returns nested value' {
        $obj = [pscustomobject]@{ a = [pscustomobject]@{ b = 'c' } }
        Get-LcJsonPath -Object $obj -Path 'a.b' | Should -Be 'c'
    }
}

Describe 'Lc-Assertion: Assert-CountIncremented (canonical S2 mutator pattern)' {
    It 'passes when post = pre + 1 (default delta)' {
        $pre = [pscustomobject]@{ Body = [pscustomobject]@{ currentRegistrations = 5 } }
        $post = [pscustomobject]@{ Body = [pscustomobject]@{ currentRegistrations = 6 } }
        Assert-CountIncremented -Pre $pre -Post $post -Path 'currentRegistrations' | Should -BeTrue
    }
    It 'passes with custom delta' {
        $pre = [pscustomobject]@{ Body = [pscustomobject]@{ count = 10 } }
        $post = [pscustomobject]@{ Body = [pscustomobject]@{ count = 13 } }
        Assert-CountIncremented -Pre $pre -Post $post -Path 'count' -Delta 3 | Should -BeTrue
    }
    It 'throws when no change' {
        $pre = [pscustomobject]@{ Body = [pscustomobject]@{ count = 5 } }
        $post = [pscustomobject]@{ Body = [pscustomobject]@{ count = 5 } }
        { Assert-CountIncremented -Pre $pre -Post $post -Path 'count' } | Should -Throw
    }
    It 'throws when missing field' {
        $pre = [pscustomobject]@{ Body = [pscustomobject]@{ other = 1 } }
        $post = [pscustomobject]@{ Body = [pscustomobject]@{ other = 2 } }
        { Assert-CountIncremented -Pre $pre -Post $post -Path 'count' } | Should -Throw
    }
}

Describe 'Lc-Assertion: Assert-AuditFieldsFresh + Updated' {
    It 'passes when audit fields are fresh' {
        $now = [datetime]::UtcNow.AddSeconds(-5).ToString('o')
        $r = [pscustomobject]@{ Body = [pscustomobject]@{ createdAt = $now; updatedAt = $now } }
        Assert-AuditFieldsFresh -Result $r | Should -BeTrue
    }
    It 'throws when createdAt is stale' {
        $stale = [datetime]::UtcNow.AddMinutes(-5).ToString('o')
        $r = [pscustomobject]@{ Body = [pscustomobject]@{ createdAt = $stale; updatedAt = $stale } }
        { Assert-AuditFieldsFresh -Result $r -MaxAgeSeconds 60 } | Should -Throw
    }
    It 'throws when missing createdAt' {
        $r = [pscustomobject]@{ Body = [pscustomobject]@{ updatedAt = [datetime]::UtcNow.ToString('o') } }
        { Assert-AuditFieldsFresh -Result $r } | Should -Throw -ExpectedMessage '*createdAt*'
    }
    It 'Assert-AuditFieldsUpdated passes when updatedAt > createdAt' {
        $c = [datetime]::UtcNow.AddSeconds(-10).ToString('o')
        $u = [datetime]::UtcNow.ToString('o')
        $r = [pscustomobject]@{ Body = [pscustomobject]@{ createdAt = $c; updatedAt = $u } }
        Assert-AuditFieldsUpdated -Result $r -MinDeltaMs 100 | Should -BeTrue
    }
    It 'Assert-AuditFieldsUpdated throws when updatedAt == createdAt' {
        $c = [datetime]::UtcNow.ToString('o')
        $r = [pscustomobject]@{ Body = [pscustomobject]@{ createdAt = $c; updatedAt = $c } }
        { Assert-AuditFieldsUpdated -Result $r -MinDeltaMs 100 } | Should -Throw
    }
}
