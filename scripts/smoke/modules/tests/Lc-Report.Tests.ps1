#Requires -Module @{ ModuleName='Pester'; ModuleVersion='5.0.0' }

BeforeAll {
    $moduleDir = Split-Path -Parent $PSScriptRoot
    Import-Module (Join-Path $moduleDir 'Lc-Report.psm1') -Force
}

Describe 'Lc-Report: New-LcReport + Add-LcResult' {
    It 'creates an empty report' {
        $r = New-LcReport -Name 'test-run'
        $r.Name | Should -Be 'test-run'
        $r.Results.Count | Should -Be 0
    }
    It 'adds PASS / FAIL / SKIP results' {
        $r = New-LcReport -Name 'mix'
        Add-LcResult -Report $r -Status PASS -Section 'CRUD' -TestName 'list events' -Endpoint 'GET /api/Events' -DurationMs 120
        Add-LcResult -Report $r -Status FAIL -Section 'RSVP' -TestName 'rsvp to event' -Endpoint 'POST /api/Events/x/rsvp' -ErrorMessage 'HTTP 500'
        Add-LcResult -Report $r -Status SKIP -Section 'Admin' -TestName 'admin force-cancel' -Endpoint 'POST /api/Admin/force-cancel' -SkipReason 'destructive — runs only with -IncludeDestructive'
        $r.Results.Count | Should -Be 3
    }
}

Describe 'Lc-Report: Get-LcReportSummary' {
    It 'counts results by status' {
        $r = New-LcReport -Name 'summary'
        Add-LcResult -Report $r -Status PASS -Section 'A' -TestName 't1'
        Add-LcResult -Report $r -Status PASS -Section 'A' -TestName 't2'
        Add-LcResult -Report $r -Status FAIL -Section 'B' -TestName 't3' -ErrorMessage 'oops'
        Add-LcResult -Report $r -Status SKIP -Section 'C' -TestName 't4' -SkipReason 'reason'
        Complete-LcReport -Report $r | Out-Null
        $s = Get-LcReportSummary -Report $r
        $s.Total | Should -Be 4
        $s.Passed | Should -Be 2
        $s.Failed | Should -Be 1
        $s.Skipped | Should -Be 1
        $s.PassRate | Should -Be 50
    }
    It 'reports 100% pass rate for all-passing report' {
        $r = New-LcReport -Name 'green'
        Add-LcResult -Report $r -Status PASS -Section 'A' -TestName 't1'
        Add-LcResult -Report $r -Status PASS -Section 'A' -TestName 't2'
        Complete-LcReport -Report $r | Out-Null
        (Get-LcReportSummary -Report $r).PassRate | Should -Be 100
    }
}

Describe 'Lc-Report: ConvertTo-LcMarkdown' {
    It 'emits valid Markdown with status header' {
        $r = New-LcReport -Name 'wave9a-events'
        Add-LcResult -Report $r -Status PASS -Section 'CRUD' -TestName 'list events' -Endpoint 'GET /api/Events'
        Complete-LcReport -Report $r | Out-Null
        $md = ConvertTo-LcMarkdown -Report $r
        $md | Should -Match '^# Smoke Report: wave9a-events'
        $md | Should -Match '\[PASS\]'
        $md | Should -Match '\| Total \| 1 \|'
        $md | Should -Match '\| Passed \| 1 \|'
    }
    It 'includes failure details' {
        $r = New-LcReport -Name 'red-report'
        Add-LcResult -Report $r -Status FAIL -Section 'RSVP' -TestName 'rsvp' -Endpoint 'POST /api/Events/x/rsvp' -ErrorMessage 'HTTP 500 dispatch failed'
        Complete-LcReport -Report $r | Out-Null
        $md = ConvertTo-LcMarkdown -Report $r
        $md | Should -Match '\[FAIL\]'
        $md | Should -Match '## Failures'
        $md | Should -Match 'HTTP 500 dispatch failed'
    }
    It 'includes per-section breakdown when sections exist' {
        $r = New-LcReport -Name 'sections'
        Add-LcResult -Report $r -Status PASS -Section 'CRUD' -TestName 't1'
        Add-LcResult -Report $r -Status PASS -Section 'RSVP' -TestName 't2'
        Complete-LcReport -Report $r | Out-Null
        $md = ConvertTo-LcMarkdown -Report $r
        $md | Should -Match '## Per-Section Results'
        $md | Should -Match '\| CRUD'
        $md | Should -Match '\| RSVP'
    }
}

Describe 'Lc-Report: ConvertTo-LcHtml' {
    It 'emits HTML with status class' {
        $r = New-LcReport -Name 'html-green'
        Add-LcResult -Report $r -Status PASS -Section 'A' -TestName 't1'
        Complete-LcReport -Report $r | Out-Null
        $html = ConvertTo-LcHtml -Report $r
        $html | Should -Match '<!DOCTYPE html>'
        $html | Should -Match "class='status pass'"
        $html | Should -Match '<title>Smoke Report: html-green</title>'
    }
    It 'emits fail status class when failures present' {
        $r = New-LcReport -Name 'html-red'
        Add-LcResult -Report $r -Status FAIL -Section 'A' -TestName 't1' -ErrorMessage 'error'
        Complete-LcReport -Report $r | Out-Null
        $html = ConvertTo-LcHtml -Report $r
        $html | Should -Match "class='status fail'"
    }
}

Describe 'Lc-Report: Save-LcReportArtifacts' {
    It 'writes .md and .html files' {
        $tempDir = Join-Path ([System.IO.Path]::GetTempPath()) "lc-report-test-$(Get-Random)"
        $r = New-LcReport -Name 'persist-test'
        Add-LcResult -Report $r -Status PASS -Section 'A' -TestName 't1'
        Complete-LcReport -Report $r | Out-Null

        $paths = Save-LcReportArtifacts -Report $r -OutputDir $tempDir
        Test-Path $paths.Markdown | Should -BeTrue
        Test-Path $paths.Html | Should -BeTrue

        $md = Get-Content $paths.Markdown -Raw
        $md | Should -Match 'persist-test'

        Remove-Item -Recurse -Force $tempDir
    }
}
