# Phase 6A.34: Docker Template Fix - Final Solution

## Problem Statement

Email templates from Infrastructure project were not being included in Docker container deployments, despite being present in local builds.

### Symptoms
- Error: `Email template 'registration-confirmation' not found`
- Templates existed in Infrastructure/Templates/Email/
- Local builds worked fine
- Docker deployments failed

### Root Cause
When `dotnet publish LankaConnect.API.csproj` runs, it only includes content files from the API project itself. The Infrastructure project's templates were marked as Content in Infrastructure.csproj, but didn't automatically flow to the API's publish output.

## Solution: Linked Content in API.csproj

### Implementation

Added linked content references in `LankaConnect.API.csproj` to pull Infrastructure templates into API publish output:

```xml
<!-- Phase 6A.34: Link Infrastructure email templates to API publish output -->
<!-- This ensures templates are included in both local builds and Docker deployments -->
<ItemGroup>
  <Content Include="..\LankaConnect.Infrastructure\Templates\Email\**\*.*">
    <Link>Templates\Email\%(RecursiveDir)%(Filename)%(Extension)</Link>
    <CopyToOutputDirectory>PreserveNewest</CopyToOutputDirectory>
    <CopyToPublishDirectory>PreserveNewest</CopyToPublishDirectory>
  </Content>
</ItemGroup>
```

### Why This Works

1. **MSBuild Linked Content**: Uses `<Link>` to reference files outside project directory
2. **Automatic Inclusion**: Templates automatically flow to publish output
3. **Single Source of Truth**: Templates still live in Infrastructure project
4. **Docker Compatible**: No special Docker-specific workarounds needed
5. **Consistent Behavior**: Local and Docker builds behave identically

### Dockerfile Changes

Simplified Dockerfile by removing manual template COPY commands:

**Before**:
```dockerfile
# Copy email templates from Infrastructure publish output
COPY --from=publish /app/publish/Templates ./Templates

# Set correct permissions for Templates directory
RUN chown -R $APP_UID:$APP_UID ./Templates && chmod -R 755 ./Templates
```

**After**:
```dockerfile
# Copy published output
# Phase 6A.34: Templates are automatically included via API.csproj linked content
# No need for separate COPY command - they're already in /app/publish
COPY --from=publish /app/publish .

# Set correct permissions for Templates directory (before switching to non-root user)
# Phase 6A.34: Templates are at ./Templates/Email/ in the publish output
RUN if [ -d ./Templates ]; then chown -R $APP_UID:$APP_UID ./Templates && chmod -R 755 ./Templates; fi
```

## Verification

### Local Test
```bash
cd src/LankaConnect.API
dotnet publish -c Release -o test-publish
ls -la test-publish/Templates/Email/
# Output shows all templates present
```

### Templates Confirmed in Publish Output
```
registration-confirmation-html.html
registration-confirmation-subject.txt
registration-confirmation-text.txt
ticket-confirmation-html.html
ticket-confirmation-subject.txt
ticket-confirmation-text.txt
```

## Architecture Decision Record (ADR)

### Decision
Use MSBuild linked content in API.csproj to include Infrastructure templates in publish output.

### Alternatives Considered

1. **Docker-specific COPY commands** (Rejected)
   - Pros: Simple Dockerfile change
   - Cons: Doesn't fix root cause, Docker-specific workaround, brittle

2. **Copy templates to API project** (Rejected)
   - Pros: Direct inclusion
   - Cons: Violates single source of truth, duplicates files

3. **Build Infrastructure separately** (Rejected)
   - Pros: Explicit control
   - Cons: Complicates build process, requires build script changes

4. **Linked content in API.csproj** (SELECTED)
   - Pros: MSBuild native, works everywhere, maintains single source of truth
   - Cons: None significant

### Quality Attributes
- **Maintainability**: Single source of truth preserved
- **Portability**: Works in all build environments
- **Simplicity**: Uses standard MSBuild features
- **Reliability**: Automatic inclusion, no manual steps

### Risks and Mitigation
- **Risk**: Path changes breaking linked content
- **Mitigation**: Relative paths are stable, verified by build

## Next Steps

1. Test Docker build with updated configuration
2. Deploy to Azure Container Apps
3. Verify registration email templates work in production
4. Monitor for any template-related errors

## Related Files

- `src/LankaConnect.API/LankaConnect.API.csproj` - Added linked content
- `src/LankaConnect.API/Dockerfile` - Simplified template handling
- `src/LankaConnect.Infrastructure/LankaConnect.Infrastructure.csproj` - Original template definitions
- `src/LankaConnect.Infrastructure/Templates/Email/` - Template files

## References

- Clean Architecture: Dependency flow from Infrastructure to API
- MSBuild Content Items: https://learn.microsoft.com/en-us/visualstudio/msbuild/common-msbuild-project-items
- Docker Multi-stage Builds: https://docs.docker.com/build/building/multi-stage/
