# NATS Consumer Implementation Guide

## Overview

This guide explains how to implement NATS JetStream consumers in the API Event Consumer service, following established best practices to avoid common pitfalls like consumer leaks and poor error handling.

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

### ✅ Configuration

- [ ] Add new subjects to `StreamSettings` class
- [ ] Use environment-specific appsettings files
- [ ] Set appropriate retention policies per environment
- [ ] Configure monitoring and alerting for consumer health

### ✅ Testing

- [ ] Unit test your subscriber logic
- [ ] Mock external dependencies
- [ ] Test both success and failure scenarios
- [ ] Verify message deserialization handling
- [ ] Test configuration validation

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

## Migration Guide for Existing Consumers

If you have existing consumers that don't follow these patterns:

1. **Identify Current Consumer Names**: Check NATS admin for existing consumers
2. **Plan Migration**: Decide whether to migrate or recreate consumers
3. **Update Code**: Implement stable naming and error handling
4. **Test Thoroughly**: Verify no message loss during migration
5. **Clean Up**: Remove old consumers after successful migration

---

**Document Version**: 1.0  
**Last Updated**: January 2025  
**Author**: Development Team 