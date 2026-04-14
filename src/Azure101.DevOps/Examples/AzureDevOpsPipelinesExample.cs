using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Azure.DevOps.Client;
using Azure.Identity;
using Microsoft.Extensions.Logging;
using Microsoft.TeamFoundation.Build.WebApi;

namespace Azure101.DevOps.Examples
{
    /// <summary>
    /// Azure DevOps Pipelines - CI/CD Automation
    /// Demonstrates: Build pipelines, release pipelines, artifact management
    /// Production pattern: Automated testing, building, deploying code
    /// Benefit: Deploy 100s of times per day safely with automated tests
    /// </summary>
    public class AzureDevOpsPipelinesExample
    {
        private readonly BuildHttpClientBase _buildClient;
        private readonly ILogger<AzureDevOpsPipelinesExample> _logger;

        public AzureDevOpsPipelinesExample(
            string devOpsOrganization,
            ILogger<AzureDevOpsPipelinesExample> logger)
        {
            _logger = logger;
            // In real scenario, would initialize client with credentials
            // _buildClient = new BuildHttpClient(new Uri($"https://dev.azure.com/{devOpsOrganization}"), credential);
        }

        // ============================================================================
        // Pattern 1: Understanding CI/CD Pipeline Stages
        // ============================================================================
        /// <summary>
        /// Continuous Integration (CI) + Continuous Deployment (CD)
        /// </summary>
        public static class CICDPipeline
        {
            public const string PIPELINE_STAGES = @"
                CODE COMMIT
                    ↓
                TRIGGER BUILD (Continuous Integration)
                ├─ Pull latest code from Git
                ├─ Run tests (unit + integration)
                ├─ Build artifacts (DLL, NuGet package, Docker image)
                ├─ Scan for security issues
                └─ Publish artifacts if successful
                    ↓
                DEPLOY TO STAGING (Continuous Deployment)
                ├─ Deploy artifact to test environment
                ├─ Run smoke tests
                ├─ Run performance tests
                └─ If passed, approve for production
                    ↓
                DEPLOY TO PRODUCTION
                ├─ Blue-green deployment (zero downtime)
                ├─ Monitor for errors
                ├─ Rollback if needed
                └─ Notify team

                Benefits:
                ✓ Every code change automatically tested
                ✓ Fast feedback (pass/fail in 5 minutes)
                ✓ Deploy multiple times per day safely
                ✓ Automated rollback on failure";

            public const string EXAMPLE_TIMELINE = @"
                10:00 AM - Developer commits code
                10:05 AM - Build starts automatically
                10:10 AM - All tests pass ✓
                10:11 AM - Artifacts published
                10:12 AM - Deploy to staging
                10:13 AM - Smoke tests pass ✓
                10:14 AM - Ready for production (manual approval)
                10:15 AM - Deploy to production
                10:16 AM - Monitoring shows: No errors ✓

                Total time: 16 minutes from commit to production!";
        }

        // ============================================================================
        // Pattern 2: Build Pipeline Triggers
        // ============================================================================
        /// <summary>
        /// What causes a build to start
        /// </summary>
        public class PipelineTriggers
        {
            public const string COMMON_TRIGGERS = @"
                1. CONTINUOUS TRIGGER (Most Common)
                   What: Build whenever code is pushed
                   When: Every commit to main/develop branch
                   Use: Ensure every change is tested
                   Example:
                     trigger:
                       - branches:
                           include:
                             - main
                             - develop

                2. SCHEDULED TRIGGER
                   What: Build on schedule
                   When: Nightly at 2 AM
                   Use: Detect broken builds early
                   Cost: Runs even if no code changed
                   Example:
                     schedules:
                       - cron: '0 2 * * *'

                3. PULL REQUEST TRIGGER
                   What: Build when PR created
                   When: Developer opens PR
                   Use: Verify PR doesn't break main
                   Blocks: Can't merge if build fails
                   Example:
                     pr:
                       - main

                4. MANUAL TRIGGER
                   What: Developer clicks 'Run'
                   When: On demand
                   Use: Test before committing";
        }

        // ============================================================================
        // Pattern 3: Build Pipeline YAML Definition
        // ============================================================================
        /// <summary>
        /// Define what a build does (Infrastructure as Code)
        /// </summary>
        public static class BuildPipelineYAML
        {
            public const string EXAMPLE = @"
                trigger:
                  - main

                pool:
                  vmImage: 'ubuntu-latest'

                variables:
                  buildConfiguration: 'Release'

                steps:
                  # Step 1: Restore NuGet packages
                  - task: NuGetToolInstaller@1
                    inputs:
                      versionSpec: '6.x'

                  - task: NuGetCommand@2
                    inputs:
                      command: 'restore'
                      feedsToUse: 'config'

                  # Step 2: Build solution
                  - task: DotNetCoreCLI@2
                    displayName: 'Build'
                    inputs:
                      command: 'build'
                      arguments: '--configuration $(buildConfiguration)'

                  # Step 3: Run unit tests
                  - task: DotNetCoreCLI@2
                    displayName: 'Run Tests'
                    inputs:
                      command: 'test'
                      arguments: '--configuration $(buildConfiguration) /p:CollectCoverage=true'

                  # Step 4: Publish code coverage
                  - task: PublishCodeCoverageResults@1
                    inputs:
                      codeCoverageTool: 'Cobertura'
                      summaryFileLocation: '$(Agent.TempDirectory)/**/coverage.cobertura.xml'

                  # Step 5: Publish artifacts
                  - task: DotNetCoreCLI@2
                    displayName: 'Publish'
                    inputs:
                      command: 'publish'
                      publishWebProjects: true
                      arguments: '--configuration $(buildConfiguration)'

                  # Step 6: Publish artifact for deployment
                  - task: PublishBuildArtifacts@1
                    inputs:
                      pathToPublish: '$(Build.ArtifactStagingDirectory)'
                      artifactName: 'drop'";

            public const string EXPLANATION = @"
                trigger: When build starts (on commits to main)
                pool: Which machines to run on (ubuntu, Windows, macOS)
                variables: Reusable values (buildConfiguration='Release')
                steps: List of tasks to execute in order

                Key Tasks:
                - NuGetToolInstaller: Set up package manager
                - NuGetCommand restore: Download dependencies
                - DotNetCoreCLI build: Compile code
                - DotNetCoreCLI test: Run unit tests
                - PublishCodeCoverageResults: Report test coverage
                - DotNetCoreCLI publish: Create deployment package
                - PublishBuildArtifacts: Store package for deployment";
        }

        // ============================================================================
        // Pattern 4: Release Pipeline (Deployment)
        // ============================================================================
        /// <summary>
        /// Automated deployment to environments
        /// </summary>
        public static class ReleasePipeline
        {
            public const string ENVIRONMENTS = @"
                ARTIFACT (from Build Pipeline)
                    ↓
                STAGE 1: STAGING (Automatic)
                ├─ Deploy artifact
                ├─ Run integration tests
                ├─ Run performance tests
                └─ If all pass → Ready for Prod
                    ↓
                STAGE 2: PRODUCTION (Manual Approval)
                ├─ Require approver to click 'Deploy'
                ├─ Deploy to prod
                ├─ Monitor for errors
                └─ Rollback available

                Benefits:
                ✓ Staging tests everything before prod
                ✓ Human gate prevents accidental deploys
                ✓ Rollback if issues detected
                ✓ Audit trail of who deployed what";

            public const string APPROVAL_PROCESS = @"
                AUTOMATIC DEPLOYMENT:
                - Build artifact created ✓
                - Tests pass ✓
                - Automatically deploy to staging

                MANUAL APPROVAL:
                - Staging deployment complete
                - Notify approvers (email/Teams)
                - Approver reviews: 'Deploy to prod?'
                - If approved: Deploy to production
                - If rejected: Stop, investigate

                Benefits of Manual Gate:
                ✓ Prevents accidental production deploys
                ✓ Time to review release notes
                ✓ Coordinate with team
                ✓ Maintain change log for compliance";
        }

        // ============================================================================
        // Pattern 5: Testing in Pipelines
        // ============================================================================
        /// <summary>
        /// Automated testing at different stages
        /// </summary>
        public static class PipelineTesting
        {
            public const string TEST_TYPES = @"
                1. UNIT TESTS (Build Stage)
                   What: Test individual functions
                   Time: < 1 second per test
                   Total: 100-1000 tests, runs in 5-10 seconds
                   Cost: Cheap, fast feedback
                   Block: Build fails if any test fails

                2. INTEGRATION TESTS (Build Stage)
                   What: Test multiple components together
                   Time: 5-30 seconds per test
                   Total: 10-50 tests, runs in 1-2 minutes
                   Cost: Slower, needs database/services
                   Block: Fail = build blocked

                3. SMOKE TESTS (Staging Stage)
                   What: Basic checks in staging environment
                   Examples:
                     - Can I log in?
                     - Can I create user?
                     - Database connection works?
                   Time: < 1 minute
                   Block: Prevent prod deploy if fails

                4. PERFORMANCE TESTS (Staging Stage)
                   What: Load test, stress test
                   Examples:
                     - 1000 concurrent users
                     - Response time < 200ms
                     - CPU < 80%
                   Time: 5-10 minutes
                   Block: Deploy blocked if degradation";

            public const string COVERAGE_GOALS = @"
                Code Coverage (% of code executed by tests):
                - 80-90%: Good (covers most critical paths)
                - 90%+: Excellent (diminishing returns)
                - < 80%: Risky (missing test coverage)

                Test Distribution:
                - 70% Unit tests (fast, focused)
                - 20% Integration tests (real scenarios)
                - 10% E2E tests (full workflows)

                Why this ratio?
                ✓ Unit tests: Fast feedback (< 10 seconds)
                ✓ Integration: Realistic scenarios
                ✓ E2E: Critical workflows only (slow)";
        }

        // ============================================================================
        // Pattern 6: Multi-Stage Deployment (Blue-Green)
        // ============================================================================
        /// <summary>
        /// Zero-downtime deployment with instant rollback
        /// </summary>
        public static class BlueGreenDeployment
        {
            public const string CONCEPT = @"
                BLUE ENVIRONMENT (Current Production)
                ├─ Running version 1.0
                ├─ Handling all user traffic
                └─ Fully tested in production

                GREEN ENVIRONMENT (New Release)
                ├─ Running version 2.0
                ├─ No user traffic yet
                ├─ Tested thoroughly
                └─ Ready to go live

                DEPLOYMENT PROCESS:
                1. Deploy new version to GREEN
                2. Run final tests on GREEN
                3. Switch traffic: BLUE → GREEN
                4. GREEN now handles all traffic
                5. BLUE idle (ready for rollback)

                If issue found in GREEN:
                → Switch back to BLUE (instant rollback)
                → No downtime, only 30 seconds total traffic impact

                Benefits:
                ✓ Zero downtime
                ✓ Easy rollback
                ✓ A/B testing capability
                ✓ Instant traffic switching";

            public const string CANARY_DEPLOYMENT = @"
                More conservative than blue-green:

                1. Deploy to 5% of servers
                2. Monitor: Are users happy? Any errors?
                3. If good: Deploy to 20%
                4. If good: Deploy to 50%
                5. If good: Deploy to 100%

                At any stage, can rollback those servers

                When to use:
                - Critical systems (banking, healthcare)
                - High traffic (1M+ requests/day)
                - Want early error detection";
        }

        // ============================================================================
        // Pattern 7: CI/CD Best Practices
        // ============================================================================
        /// <summary>
        /// Production-ready pipeline practices
        /// </summary>
        public static class CICDBestPractices
        {
            public const string PRACTICES = @"
                ✓ BUILD FAST
                  Goal: Build completes in < 5 minutes
                  If > 10 minutes: Hard to test frequently

                ✓ FAIL FAST
                  Run quick tests first (unit tests)
                  Expensive tests later (performance)

                ✓ BLOCK MERGING
                  Cannot merge PR if build fails
                  Enforces code quality

                ✓ AUTOMATED TESTING
                  Don't rely on manual testing
                  Add tests for every bug fix

                ✓ ATOMIC COMMITS
                  Each commit should be complete feature
                  Enables easy rollback

                ✓ MASTER/MAIN STAYS DEPLOYABLE
                  Main branch = production ready
                  Never commit broken code

                ✓ MONITOR PRODUCTION
                  Application Insights
                  Alert on errors
                  Auto-rollback if degradation

                ✓ SMALL CHANGES
                  Deploy small changes often
                  Easier to find issues
                  Lower risk per change";

            public const string ANTI_PATTERNS = @"
                ✗ SLOW PIPELINES
                  > 10 minutes = won't run before committing
                  → Code issues caught too late

                ✗ FLAKY TESTS
                  Tests sometimes fail randomly
                  → Ignored and disabled
                  → Issues miss detection

                ✗ MANUAL TESTING GATE
                  Deployment blocked on manual QA
                  → Slow, expensive, error-prone
                  → Create automated tests instead

                ✗ BIG DEPLOYMENTS
                  Deploy 100 features at once
                  → Impossible to find which broke
                  → Large rollback impact

                ✗ NO MONITORING
                  Deploy, hope for the best
                  → Users report issues first
                  → Could detect yourself automatically";
        }

        // ============================================================================
        // Pattern 8: Pipeline Status and Notifications
        // ============================================================================
        /// <summary>
        /// Get pipeline results and notify team
        /// </summary>
        public async Task<PipelineStatus> GetPipelineStatusAsync(
            string projectName,
            int buildId)
        {
            try
            {
                _logger.LogInformation($"→ Getting pipeline status for build {buildId}");

                // In real scenario, would call Azure DevOps API
                var status = new PipelineStatus
                {
                    BuildId = buildId,
                    Status = "Succeeded",
                    Duration = TimeSpan.FromMinutes(4),
                    TestsPassed = 147,
                    CodeCoverage = 0.87f,
                    Artifacts = new List<string> { "app.zip", "nuget-package.nupkg" }
                };

                _logger.LogInformation($"✓ Build {buildId} succeeded");
                _logger.LogInformation($"  Duration: {status.Duration.TotalMinutes:F1} minutes");
                _logger.LogInformation($"  Tests: {status.TestsPassed} passed");
                _logger.LogInformation($"  Coverage: {status.CodeCoverage:P}");

                // Send notification
                await NotifyTeamAsync(status);

                return status;
            }
            catch (Exception ex)
            {
                _logger.LogError($"✗ Pipeline query failed: {ex.Message}");
                throw;
            }
        }

        // ============================================================================
        // Helper Methods
        // ============================================================================
        private async Task NotifyTeamAsync(PipelineStatus status)
        {
            // Would send to Teams, Slack, or email
            _logger.LogInformation($"📢 Notifying team: Build {status.BuildId} {status.Status}");
            await Task.CompletedTask;
        }
    }

    // ============================================================================
    // Supporting Models
    // ============================================================================
    public class PipelineStatus
    {
        public int BuildId { get; set; }
        public string Status { get; set; }
        public TimeSpan Duration { get; set; }
        public int TestsPassed { get; set; }
        public float CodeCoverage { get; set; }
        public List<string> Artifacts { get; set; }
    }
}
