# NATS Consumer Implementation Guide

## Overview

This guide explains how to implement NATS JetStream consumers in the API Event Consumer service, following established best practices to avoid common pitfalls like consumer leaks, poor error handling, and ACK timeout issues.

## Quick Reference: Timeout Settings

| Setting | Value | Purpose |
|---------|-------|---------|
| **NATS ACK Wait** | 30 seconds | Maximum time for message acknowledgment |
| **HTTP Timeouts** | 10-25 seconds | Must be < NATS ACK wait time |
| **Database Timeouts** | 5-10 seconds | MongoDB query timeouts |
| **Message Processing** | < 25 seconds | Safe processing window |

⚠️ **Critical Rule**: All operations in message handlers must complete within 30 seconds or messages will be redelivered.

## Current Implementation Architecture

### Core Components

1. **NatsConnectionService** - Manages NATS connections and consumer lifecycle
2. **Event Subscribers** - Business logic handlers (BetSubscriber, BetSettledSubscriber)
3. **Stream Configuration** - Centralized stream management with retention policies
4. **Consumer Management** - Automatic consumer creation with stable naming

### Implementation Flow

```
Service Startup
    ↓
Initialize NATS Connection
    ↓
Configure JetStream (per subscriber)
    ↓
Create/Update Stream
    ↓
Create/Update Consumer (with stable name)
    ↓
Start Subscription
    ↓
Begin Message Processing
```

## How We Avoid Consumer Leaks

### Problem: GUID-Based Consumer Names
**❌ Wrong Approach:**
```csharp
// Creates new consumer on every restart
var consumerId = $"consumer_{Guid.NewGuid():N}";
```

### Solution: Deterministic Consumer Names
**✅ Current Implementation:**
```csharp
private string GetConsumerName(string subject)
{
    // Convert subject to stable consumer name
    // "vg.local2.api.bet.created" → "VG_LOCAL2_API_BET_CREATED_CONSUMER"
    var cleanSubject = subject.Replace(".", "_").Replace("*", "ALL").ToUpper();
    return $"{cleanSubject}_CONSUMER";
}
```

**Benefits:**
- Same consumer name across all service restarts
- No accumulation of abandoned consumers
- Easy identification in NATS admin tools
- Predictable resource usage

### Consumer Lifecycle Management

```csharp
private void ConfigureSubscription(IJetStream jsContext, string streamName, string subject, 
    Func<object?, MsgHandlerEventArgs, Task<bool>> handler)
{
    // Generate stable consumer name
    var consumerName = GetConsumerName(subject);
    
    // Store for cleanup tracking
    currentConsumerName = consumerName;
    currentStreamName = streamName;

    // Create durable consumer configuration
    var consumerConfig = ConsumerConfiguration.Builder()
        .WithName(consumerName)           // Stable name
        .WithDurable(consumerName)        // Survives restarts
        .WithDeliverPolicy(DeliverPolicy.All)  // No data loss
        .WithAckPolicy(AckPolicy.Explicit)     // Manual acknowledgment
        .WithMaxDeliver(10)               // Retry failed messages
        .Build();

    // Create or update consumer (idempotent operation)
    jsManagement.AddOrUpdateConsumer(streamName, consumerConfig);
}
```

## Error Handling Best Practices

### Connection-Level Error Handling

```csharp
public void InitializeConnection()
{
    try
    {
        var options = ConnectionFactory.GetDefaultOptions();
        options.Url = streamSettings.Host;
        options.AllowReconnect = true;
        options.MaxReconnect = 10;
        options.ReconnectWait = 10000;
        
        // Event handlers for connection monitoring
        options.DisconnectedEventHandler += (_, _) =>
        {
            sentryClient.CaptureException(new Exception("Disconnected from NATS server"));
        };
        options.ReconnectedEventHandler += (_, _) =>
        {
            sentryClient.CaptureException(new Exception("Reconnected to NATS server"));
        };
        
        connection = new ConnectionFactory().CreateConnection(options);
    }
    catch (Exception ex)
    {
        sentryClient.CaptureException(ex);
        throw; // Let the application handle startup failure
    }
}
```

### Message-Level Error Handling

```csharp
async void autoAckHandler(object? sender, MsgHandlerEventArgs args)
{
    // Validate message metadata
    if (args.Message.MetaData == null)
    {
        args.Message.Nak(); // Reject invalid messages
        return;
    }

    try
    {
        // Execute business logic
        bool success = await handler(sender, args);
        
        if (!success)
        {
            args.Message.Nak(); // Trigger redelivery
            return;
        }

        // Success - acknowledge message
        ProcessSuccessfulMessage(args);
    }
    catch (Exception ex)
    {
        sentryClient.CaptureException(ex);
        args.Message.Nak(); // Ensure redelivery on exception
    }
}
```

### Health Monitoring

```csharp
public bool IsHealthy()
{
    try
    {
        // Check connection status
        if (connection == null || connection.IsClosed())
            return false;

        // Verify subscription validity
        if (subscription == null || !subscription.IsValid)
            return false;

        // Validate consumer health
        if (currentStreamName != null && currentConsumerName != null)
        {
            var jsManagement = connection.CreateJetStreamManagementContext();
            var consumerInfo = jsManagement.GetConsumerInfo(currentStreamName, currentConsumerName);
            return consumerInfo != null;
        }

        return true;
    }
    catch (Exception ex)
    {
        sentryClient.CaptureException(ex);
        return false;
    }
}
```

## Stream Configuration Best Practices

### Current Stream Setup

```csharp
private void ConfigureStream(IJetStreamManagement jsManagement, string streamName)
{
    var streamSettings = appSettings.StreamSettings;
    var subjectPrefix = streamSettings?.SubjectPrefix ?? streamName;
    
    var streamConfig = StreamConfiguration.Builder()
        .WithName(streamName)
        .WithSubjects($"{subjectPrefix}.>")                    // Wildcard subject pattern
        .WithMaxAge(Duration.OfHours(streamSettings?.MaxAgeHours ?? 24))  // Time-based retention
        .WithDiscardPolicy(DiscardPolicy.Old)                 // Remove oldest when full
        .WithRetentionPolicy(RetentionPolicy.Limits)          // Audit-friendly retention
        .WithMaxMessages(streamSettings?.MaxMessages ?? 1000000)  // Count-based limit
        .WithStorageType(StorageType.File)                    // Persistent storage
        .Build();

    // Idempotent stream creation
    try
    {
        var streamInfo = jsManagement.GetStreamInfo(streamName);
        if (streamInfo == null)
        {
            jsManagement.AddStream(streamConfig);
        }
    }
    catch (NATSJetStreamException ex)
    {
        sentryClient.CaptureException(ex);
    }
}
```

### Why These Settings?

| Setting | Value | Reason |
|---------|-------|--------|
| `RetentionPolicy.Limits` | Default | Messages persist for audit/replay, auto-cleanup by limits |
| `DeliverPolicy.All` | Default | Process all messages, no data loss on restart |
| `StorageType.File` | Default | Persistent storage, survives server restarts |
| `MaxAge: 24 hours` | Configurable | Compliance window, automatic cleanup |
| `MaxMessages: 1M` | Configurable | Prevent unbounded growth |
| `MaxDeliver: 10` | Default | Reasonable retry attempts for failed messages |

## Setting Up New Consumers

### Step 1: Create Your Subscriber Class

```csharp
public class MyEventSubscriber : IEventSubscriber
{
    private readonly ISentryClient sentryClient;
    private readonly IAppSettings appSettings;
    private readonly IMyBusinessService myService;

    public MyEventSubscriber(
        ISentryClient sentryClient,
        IAppSettings appSettings,
        IMyBusinessService myService)
    {
        this.sentryClient = sentryClient;
        this.appSettings = appSettings;
        this.myService = myService;
    }

    // Define the subject this subscriber handles
    public string SubjectName => appSettings?.StreamSettings?.MyEventMessages ?? string.Empty;

    public async Task<bool> OnMessageReceived(object sender, MsgHandlerEventArgs args)
    {
        try
        {
            // Deserialize message
            var message = Encoding.UTF8.GetString(args.Message.Data);
            var myEvent = JsonSerializer.Deserialize<MyEvent>(message);

            if (myEvent != null)
            {
                // Process business logic
                await ProcessMyEvent(myEvent);
                return true; // Success
            }
            
            return false; // Invalid message format
        }
        catch (Exception ex)
        {
            sentryClient.CaptureException(ex);
            return false; // Will trigger NAK and redelivery
        }
    }

    private async Task ProcessMyEvent(MyEvent myEvent)
    {
        // Your business logic here
        await myService.HandleEvent(myEvent);
    }
}
```

### Step 2: Update Configuration

Add your subject to `StreamSettings` in `AppSettings.cs`:

```csharp
public class StreamSettings
{
    // ... existing properties
    public string? MyEventMessages { get; set; }
}
```

Update `appsettings.json`:

```json
{
  "StreamSettings": {
    "Host": "nats://10.10.50.122:4222",
    "Enable": true,
    "StreamName": "api_events_vg_local2",
    "SubjectPrefix": "vg.local2.api",
    "BetMessages": "vg.local2.api.bet.created",
    "BetSettledMessages": "vg.local2.api.bet.settled",
    "MyEventMessages": "vg.local2.api.my.event"
  }
}
```

### Step 3: Register and Configure

In your service registration (e.g., `Program.cs` or startup):

```csharp
// Register your subscriber
services.AddScoped<MyEventSubscriber>();

// Configure the consumer during application startup
var myEventSubscriber = serviceProvider.GetRequiredService<MyEventSubscriber>();
natsConnectionService.ConfigureJetStream(
    myEventSubscriber.OnMessageReceived,
    myEventSubscriber.SubjectName
);
```

### Step 4: Consumer Name Prediction

Your consumer will automatically be named based on the subject:
- Subject: `vg.local2.api.my.event`
- Consumer Name: `VG_LOCAL2_API_MY_EVENT_CONSUMER`

This name will be:
- ✅ Stable across restarts
- ✅ Descriptive and identifiable
- ✅ Unique per subject

## Configuration Guidelines

### Required StreamSettings Properties

```csharp
public class StreamSettings
{
    public string? Host { get; set; }              // NATS server URL
    public bool Enable { get; set; }               // Feature flag
    public string? StreamName { get; set; }        // JetStream stream name
    public string? SubjectPrefix { get; set; }     // Subject namespace
    public int MaxAgeHours { get; set; } = 24;     // Retention period
    public long MaxMessages { get; set; } = 1000000; // Message count limit
    
    // Subject definitions
    public string? BetMessages { get; set; }
    public string? BetSettledMessages { get; set; }
    // Add your subjects here...
}
```

### Environment-Specific Configuration

**Local Development:**
```json
{
  "StreamSettings": {
    "Host": "nats://localhost:4222",
    "StreamName": "api_events_local",
    "SubjectPrefix": "local.api",
    "MaxAgeHours": 1,
    "MaxMessages": 10000
  }
}
```

**Production:**
```json
{
  "StreamSettings": {
    "Host": "nats://production-nats.company.com:4222",
    "StreamName": "api_events_prod",
    "SubjectPrefix": "prod.api",
    "MaxAgeHours": 168,
    "MaxMessages": 10000000
  }
}
```

## HTTP Service Timeout Best Practices

### ⚠️ Critical: Avoid ACK Timeout Issues

**NATS ACK Wait Time: 30 seconds**

All HTTP operations in message handlers MUST complete within this window to prevent message redelivery.

### HTTP Service Timeout Configuration

```csharp
// ✅ Good: Timeout less than NATS ACK wait time
private async Task<TResponse> PostAsync<TRequest, TResponse>(Uri url, TRequest request)
{
    var timeout = TimeSpan.FromSeconds(appSettings.ApiSettings?.TimeoutInSeconds ?? 10);
    using var cts = new CancellationTokenSource(timeout);

    using var response = await httpClient.PostAsJsonAsync(url, request, cts.Token);
    response.EnsureSuccessStatusCode();

    var result = await response.Content.ReadFromJsonAsync<TResponse>(cancellationToken: cts.Token);
    return result ?? throw new InvalidOperationException($"Failed to deserialize response from {url}");
}
```

### Required Timeout Settings by Service Type

| Service Type | Recommended Timeout | Buffer vs NATS | Reasoning |
|-------------|-------------------|----------------|-----------|
| **Critical APIs** | 10 seconds | 20s buffer | Fast failure detection |
| **Secondary APIs** | 15 seconds | 15s buffer | Allow reasonable response time |
| **Notification APIs** | 25 seconds | 5s buffer | Last resort before NATS timeout |

### Configuration Pattern

```csharp
// In AppSettings.cs
public class PromotionApiSettings
{
    public string? BaseUrl { get; set; }
    public PromotionEndpoints? Endpoints { get; set; }
    public int? TimeoutInSeconds { get; set; }  // ✅ Always include timeout option
}

// In appsettings.json
{
  "PromotionApiSettings": {
    "BaseUrl": "https://api.example.com",
    "TimeoutInSeconds": 10,  // ✅ Set explicit timeout
    "Endpoints": {
      "UpdateTotalTurnover": "/bonus-wins/update-total-turnover"
    }
  }
}
```

### Sequential HTTP Call Mitigation

```csharp
// ❌ Wrong: Sequential calls can exceed NATS timeout
public async Task<bool> OnMessageReceived(object sender, MsgHandlerEventArgs args)
{
    await promotionService.UpdateTotalTurnoverAsync(request);    // 10s
    await notificationService.SendPromotionProgressAsync(req);   // 25s
    // Total: 35s > 30s NATS timeout = MESSAGE REDELIVERY
}

// ✅ Better: Parallel execution
public async Task<bool> OnMessageReceived(object sender, MsgHandlerEventArgs args)
{
    var promotionTask = promotionService.UpdateTotalTurnoverAsync(request);
    var notificationTask = notificationService.SendPromotionProgressAsync(req);
    
    await Task.WhenAll(promotionTask, notificationTask);
    // Total: max(10s, 25s) = 25s < 30s NATS timeout = SUCCESS
}

// ✅ Best: Fail-fast with timeout monitoring
public async Task<bool> OnMessageReceived(object sender, MsgHandlerEventArgs args)
{
    using var cts = new CancellationTokenSource(TimeSpan.FromSeconds(25));
    
    try
    {
        await ProcessWithTimeout(args, cts.Token);
        return true;
    }
    catch (OperationCanceledException)
    {
        sentryClient.CaptureMessage("Message processing timeout exceeded");
        return false; // NAK - will retry
    }
}
```

## Database Operation Timeouts

### MongoDB Query Timeouts

```csharp
// ✅ Add timeout to MongoDB operations
public async Task<string> GetCharacterNameAsync(string memberId)
{
    var options = new FindOptions<AccountEntity> 
    { 
        MaxTime = TimeSpan.FromSeconds(5)  // Fail fast on slow DB
    };
    
    var account = await accountRepository.Collection()
        .Find(user => user.Local!.MemberId == memberId, options)
        .FirstOrDefaultAsync();
        
    return account?.Name ?? string.Empty;
}
```

## Best Practices Checklist

### ✅ Consumer Implementation

- [ ] Use descriptive subject names following convention: `{env}.{service}.{entity}.{action}`
- [ ] Return `true` for successful processing, `false` for failures
- [ ] Implement proper exception handling with Sentry logging
- [ ] Use dependency injection for business services
- [ ] Keep message processing logic focused and testable

### ✅ Error Handling

- [ ] Always catch exceptions in message handlers
- [ ] Log errors to Sentry for monitoring
- [ ] Return `false` to trigger NAK and redelivery
- [ ] Don't throw unhandled exceptions from handlers
- [ ] Implement circuit breakers for external service calls

### ✅ Timeout Management

- [ ] **CRITICAL**: All HTTP calls MUST have timeouts < 30 seconds
- [ ] Configure explicit timeouts for all external service calls
- [ ] Add MongoDB query timeouts (5-10 seconds recommended)
- [ ] Use parallel execution for independent operations
- [ ] Monitor and alert on timeout occurrences
- [ ] Test timeout scenarios in integration tests

### ✅ Configuration

- [ ] Add new subjects to `StreamSettings` class
- [ ] Use environment-specific appsettings files
- [ ] Set appropriate retention policies per environment
- [ ] Configure monitoring and alerting for consumer health
- [ ] **NEW**: Include `TimeoutInSeconds` in all API settings classes

### ✅ Testing

- [ ] Unit test your subscriber logic
- [ ] Mock external dependencies
- [ ] Test both success and failure scenarios
- [ ] Verify message deserialization handling
- [ ] Test configuration validation
- [ ] **NEW**: Test timeout scenarios and recovery

## Monitoring and Troubleshooting

### Consumer Health Checks

```csharp
// Check if specific consumer is healthy
public bool IsConsumerHealthy(string consumerName)
{
    try
    {
        var jsManagement = connection.CreateJetStreamManagementContext();
        var consumerInfo = jsManagement.GetConsumerInfo(currentStreamName, consumerName);
        return consumerInfo != null && consumerInfo.Delivered.StreamSeq > 0;
    }
    catch
    {
        return false;
    }
}
```

### Common Issues and Solutions

| Issue | Symptoms | Solution |
|-------|----------|----------|
| **Consumer Leak** | Multiple consumers with similar names | Use stable naming pattern |
| **Message Loss** | Missing transactions | Use `DeliverPolicy.All` and durable consumers |
| **Infinite Retries** | Same message failing repeatedly | Implement proper error handling, return `false` |
| **Memory Issues** | High memory usage | Use `StorageType.File` and appropriate limits |
| **Connection Drops** | Frequent disconnections | Configure reconnection settings and monitoring |
| **⚠️ ACK Timeout** | Messages redelivered multiple times | Configure HTTP timeouts < 30s, use parallel calls |
| **⚠️ Slow HTTP APIs** | Processing takes >30 seconds | Add explicit timeouts to all HTTP services |
| **⚠️ Database Hangs** | MongoDB queries never return | Add `MaxTime` to FindOptions |
| **⚠️ Sequential Delays** | Multiple API calls accumulate time | Use `Task.WhenAll()` for parallel execution |

### Timeout-Specific Troubleshooting

#### Identifying Timeout Issues

```csharp
// Add timeout monitoring to your subscribers
public async Task<bool> OnMessageReceived(object sender, MsgHandlerEventArgs args)
{
    var stopwatch = Stopwatch.StartNew();
    
    try
    {
        var result = await ProcessMessage(args);
        return result;
    }
    finally
    {
        stopwatch.Stop();
        if (stopwatch.ElapsedMilliseconds > 25000) // 25 seconds
        {
            sentryClient.CaptureMessage(
                $"Message processing took {stopwatch.ElapsedMilliseconds}ms - approaching NATS timeout",
                SentryLevel.Warning);
        }
    }
}
```

#### NATS Consumer Health Monitoring

```csharp
// Monitor consumer delivery metrics
public ConsumerHealthStatus GetConsumerHealth(string consumerName)
{
    try
    {
        var jsManagement = connection.CreateJetStreamManagementContext();
        var consumerInfo = jsManagement.GetConsumerInfo(currentStreamName, consumerName);
        
        return new ConsumerHealthStatus
        {
            IsHealthy = consumerInfo != null,
            PendingMessages = consumerInfo?.NumPending ?? 0,
            AckPendingCount = consumerInfo?.NumAckPending ?? 0,
            RedeliveredCount = consumerInfo?.NumRedelivered ?? 0,
            LastDelivery = consumerInfo?.Delivered?.Last ?? DateTime.MinValue
        };
    }
    catch (Exception ex)
    {
        sentryClient.CaptureException(ex);
        return new ConsumerHealthStatus { IsHealthy = false };
    }
}

public class ConsumerHealthStatus
{
    public bool IsHealthy { get; set; }
    public ulong PendingMessages { get; set; }
    public ulong AckPendingCount { get; set; }
    public ulong RedeliveredCount { get; set; }
    public DateTime LastDelivery { get; set; }
}
```

#### Warning Signs to Monitor

- **High AckPending Count (>10)**: Messages taking too long to process
- **Increasing Redelivered Count**: Messages consistently failing or timing out  
- **Growing Pending Messages**: Consumer can't keep up with message rate
- **No Recent Deliveries**: Consumer may be stuck or disconnected

## Migration Guide for Existing Consumers

If you have existing consumers that don't follow these patterns:

1. **Identify Current Consumer Names**: Check NATS admin for existing consumers
2. **Plan Migration**: Decide whether to migrate or recreate consumers
3. **Update Code**: Implement stable naming and error handling
4. **Test Thoroughly**: Verify no message loss during migration
5. **Clean Up**: Remove old consumers after successful migration

---

**Document Version**: 1.1  
**Last Updated**: January 2025  
**Author**: Development Team  
**Changes in v1.1**: Added HTTP timeout best practices, database timeout configuration, and timeout-specific troubleshooting guidelines 