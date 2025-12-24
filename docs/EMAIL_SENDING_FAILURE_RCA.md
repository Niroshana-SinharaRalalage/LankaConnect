# Root Cause Analysis: Email Sending Failure

**Date**: 2025-12-23
**Issue**: Event publication emails failing with "Cannot access value of a failed result"
**Status**: DIAGNOSED - Template has NULL subject_template

## Problem Summary

When publishing event `0dc17180-e4c9-4768-aefe-e3044ed691fa`, emails failed for all 7 recipients with:
```
Failed to send templated email 'event-published'
Cannot access value of a failed result
```

## Root Cause

The `event-published` template in the database has a NULL `subject_template` column value. When EF Core tries to hydrate the `EmailTemplate.SubjectTemplate` value object (type `EmailSubject`), it cannot create an instance from NULL, causing a failure.

## Fix

The migration `20251221160725_SeedEventPublishedTemplate_Phase6A39.cs` should have inserted this template, but the subject was either not included or got corrupted.

**Solution**: Update the template in the database with a valid subject:

```sql
UPDATE communications.email_templates
SET subject_template = 'New Event: {{EventTitle}}'
WHERE name = 'event-published';
```
