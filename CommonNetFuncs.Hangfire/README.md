# CommonNetFuncs.Hangfire

[![License](https://img.shields.io/github/license/NickScarpitti/common-net-funcs.svg)](http://opensource.org/licenses/MIT)
[![Build](https://github.com/NickScarpitti/common-net-funcs/actions/workflows/dotnet.yml/badge.svg)](https://github.com/NickScarpitti/common-net-funcs/actions/workflows/dotnet.yml)
[![NuGet Version](https://img.shields.io/nuget/v/CommonNetFuncs.Hangfire)](https://www.nuget.org/packages/CommonNetFuncs.Hangfire/)
[![nuget](https://img.shields.io/nuget/dt/CommonNetFuncs.Hangfire)](https://www.nuget.org/packages/CommonNetFuncs.Hangfire/)

This project contains helper methods and utilities for integrating Hangfire background job processing into ASP.NET Core applications with enhanced security, error handling, and graceful shutdown capabilities.

## Contents

- [CommonNetFuncs.Hangfire](#commonnetfuncshangfire)
  - [Contents](#contents)
  - [HangfireAuthorizationFilter](#hangfireauthorizationfilter)
    - [HangfireAuthorizationFilter Usage Examples](#hangfireauthorizationfilter-usage-examples)
      - [Basic Setup with Roles](#basic-setup-with-roles)
      - [Authentication Only (No Role Restrictions)](#authentication-only-no-role-restrictions)
  - [HangfireJobException](#hangfirejobexception)
    - [HangfireJobException Usage Examples](#hangfirejobexception-usage-examples)
      - [Basic Job Exception](#basic-job-exception)
      - [Exception with Retry Control](#exception-with-retry-control)
      - [Exception with Operation Context](#exception-with-operation-context)
      - [Full Context Exception](#full-context-exception)
  - [HangfireShutdownMonitor](#hangfireshutdownmonitor)
    - [HangfireShutdownMonitor Usage Examples](#hangfireshutdownmonitor-usage-examples)
      - [Register Shutdown Monitor](#register-shutdown-monitor)
  - [WaitForHangfireJobsToComplete](#waitforhangfirejobstocomplete)
    - [WaitForHangfireJobsToComplete Usage Examples](#waitforhangfirejobstocomplete-usage-examples)
      - [Wait for Jobs with Defaults](#wait-for-jobs-with-defaults)
      - [Wait for Jobs with Custom Settings](#wait-for-jobs-with-custom-settings)

---

## HangfireAuthorizationFilter

Authorization filter for Hangfire Dashboard that requires authenticated users with specific roles to access the dashboard.

### HangfireAuthorizationFilter Usage Examples

<details>
<summary><h4>Usage Examples</h4></summary>

#### Basic Setup with Roles

Restrict dashboard access to specific roles (Admin, Manager, etc.)

```cs
using CommonNetFuncs.Hangfire;
using Hangfire;
using System.Collections.Frozen;

// In Program.cs or Startup.cs
var allowedRoles = new HashSet<string> { "Admin", "Manager" }.ToFrozenSet();

app.UseHangfireDashboard("/hangfire", new DashboardOptions
{
    Authorization = new[] { new HangfireAuthorizationFilter(allowedRoles) }
});
```

#### Authentication Only (No Role Restrictions)

Allow any authenticated user to access the dashboard

```cs
using CommonNetFuncs.Hangfire;
using Hangfire;
using System.Collections.Frozen;

// Empty set = any authenticated user can access
var allowedRoles = new HashSet<string>().ToFrozenSet();

app.UseHangfireDashboard("/hangfire", new DashboardOptions
{
    Authorization = new[] { new HangfireAuthorizationFilter(allowedRoles) }
});
```

</details>

---

## HangfireJobException

Custom exception for Hangfire background jobs that provides context about the job operation and controls retry behavior.

**Properties:**
- `OperationName` - The name of the operation that failed
- `EntityId` - The ID of the entity being processed when the failure occurred
- `AllowRetry` - Whether Hangfire should retry the job (defaults to `true`)

### HangfireJobException Usage Examples

<details>
<summary><h4>Usage Examples</h4></summary>

#### Basic Job Exception

Throw a simple exception that allows retry

```cs
using CommonNetFuncs.Hangfire;

public async Task ProcessItem(int itemId)
{
    if (!await ValidateItem(itemId))
    {
        throw new HangfireJobException("Item validation failed");
    }
}
```

#### Exception with Retry Control

Throw an exception that prevents retry for permanent failures

```cs
using CommonNetFuncs.Hangfire;

public async Task ProcessPayment(int paymentId)
{
    var payment = await GetPayment(paymentId);
    
    if (payment.Status == PaymentStatus.AlreadyProcessed)
    {
        // Don't retry - this is a permanent failure condition
        throw new HangfireJobException(
            "Payment has already been processed", 
            allowRetry: false);
    }
}
```

#### Exception with Operation Context

Provide operation name for better logging and debugging

```cs
using CommonNetFuncs.Hangfire;

public async Task SendEmailNotification(int userId)
{
    try
    {
        await emailService.SendNotificationAsync(userId);
    }
    catch (SmtpException ex)
    {
        throw new HangfireJobException(
            "Failed to send email notification",
            operationName: "SendEmailNotification",
            allowRetry: true);
    }
}
```

#### Full Context Exception

Include all context information for comprehensive error tracking

```cs
using CommonNetFuncs.Hangfire;

public async Task UpdateCustomerData(int customerId)
{
    try
    {
        var customer = await dbContext.Customers.FindAsync(customerId);
        
        if (customer == null)
        {
            throw new HangfireJobException(
                message: "Customer not found in database",
                operationName: "UpdateCustomerData",
                entityId: customerId,
                allowRetry: false); // Don't retry if customer doesn't exist
        }
        
        // Process customer...
    }
    catch (DbUpdateException ex)
    {
        throw new HangfireJobException(
            message: "Database update failed",
            operationName: "UpdateCustomerData",
            entityId: customerId,
            allowRetry: true); // Retry transient database errors
    }
}
```

</details>

---

## HangfireShutdownMonitor

Monitors Hangfire jobs during application shutdown to ensure graceful termination and log pending jobs.

**Features:**
- Logs pending job counts during shutdown (processing, enqueued, scheduled)
- Properly disposes of the BackgroundJobServer
- Handles errors gracefully during shutdown

### HangfireShutdownMonitor Usage Examples

<details>
<summary><h4>Usage Examples</h4></summary>

#### Register Shutdown Monitor

Add the shutdown monitor as a hosted service in your ASP.NET Core application

```cs
using CommonNetFuncs.Hangfire;

// In Program.cs or Startup.cs
builder.Services.AddHostedService<HangfireShutdownMonitor>();

// Configure Hangfire as normal
builder.Services.AddHangfire(configuration => configuration
    .SetDataCompatibilityLevel(CompatibilityLevel.Version_180)
    .UseSimpleAssemblyNameTypeSerializer()
    .UseRecommendedSerializerSettings()
    .UseSqlServerStorage(connectionString));

builder.Services.AddHangfireServer();
```

The monitor will automatically log pending jobs when the application shuts down:
```
[INFO] Application shutting down with no pending Hangfire jobs
```
or
```
[WARN] Application shutting down with 5 pending Hangfire job(s): 2 processing, 2 enqueued, 1 scheduled. 
       Jobs will be persisted in database and resumed by next instance.
```

</details>

---

## WaitForHangfireJobsToComplete

Utility method to wait for all Hangfire jobs to complete before continuing execution. Useful for graceful shutdown scenarios or integration tests.

**Parameters:**
- `checkIntervalSeconds` - How often to check job status (default: 5 seconds)
- `maxWaitMinutes` - Maximum time to wait for jobs (default: 60 minutes)

### WaitForHangfireJobsToComplete Usage Examples

<details>
<summary><h4>Usage Examples</h4></summary>

#### Wait for Jobs with Defaults

Wait up to 60 minutes, checking every 5 seconds

```cs
using CommonNetFuncs.Hangfire;

public async Task ShutdownGracefully()
{
    logger.Info("Starting graceful shutdown...");
    
    // Wait for all Hangfire jobs to complete
    await WaitForHangfireJobsToComplete.WaitForAllHangfireJobsToComplete();
    
    logger.Info("All jobs completed, continuing shutdown");
}
```

#### Wait for Jobs with Custom Settings

Wait up to 10 minutes, checking every 10 seconds

```cs
using CommonNetFuncs.Hangfire;

public async Task RunIntegrationTest()
{
    // Enqueue test jobs
    BackgroundJob.Enqueue(() => ProcessTestData());
    BackgroundJob.Enqueue(() => ValidateResults());
    
    // Wait for jobs to complete before asserting results
    // Check every 10 seconds, timeout after 10 minutes
    await WaitForHangfireJobsToComplete.WaitForAllHangfireJobsToComplete(
        checkIntervalSeconds: 10, 
        maxWaitMinutes: 10);
    
    // Assert test results...
}
```

The method logs progress while waiting:
```
[INFO] Waiting for 3 Hangfire job(s): 2 enqueued, 1 processing, 0 scheduled
[INFO] Waiting for 1 Hangfire job(s): 0 enqueued, 1 processing, 0 scheduled
[INFO] No pending Hangfire jobs found
```

If the timeout is exceeded:
```
[WARN] Maximum wait time of 10 minutes exceeded. Some jobs may still be pending.
```

</details>

---

## Installation

Install via NuGet:

```bash
dotnet add package CommonNetFuncs.Hangfire
```

## Dependencies

- Hangfire.AspNetCore (>= 1.8.22)
- Hangfire.Core (>= 1.8.22)
- NLog.Extensions.Logging (>= 6.1.1)

## License

This project is licensed under the MIT License - see the [LICENSE](https://github.com/NickScarpitti/common-net-funcs/blob/main/LICENSE) file for details.
