# NATS Consumer QC Verification Guide

## Overview

Simple verification checklist for QC testing using [NATS NUI](https://github.com/nats-nui/nui) - the NATS management GUI tool.

**Goal**: Verify NATS consumers are working correctly without needing code access.

---

## 🛠️ **Setup Requirements**

### NATS NUI Installation
- [ ] Install NATS NUI from [GitHub releases](https://github.com/nats-nui/nui/releases)
- [ ] Connect to NATS server using environment-specific URL
- [ ] Verify connection shows "Connected" status

### Environment Connection URLs
| Environment | NATS URL | Notes |
|------------|----------|-------|
| LOCAL | `nats://10.10.50.122:4222` | Local development |
| SIT | `nats://vn-sc-nats-uat.nebula.prod` | Testing environment |
| PROD | `nats://vn-sc-nats-prod.nebula.prod` | Production |

---

## ✅ **Consumer Verification Checklist**

### 1. **Consumer Existence & Health**

#### Check Consumer Registration
- [ ] **Navigate to "Streams" tab in NATS NUI**
- [ ] **Locate target stream** (e.g., `api_events_vg_local2`)
- [ ] **Click on stream to view details**
- [ ] **Check "Consumers" section shows expected consumers:**
  - `VG_LOCAL2_API_BET_CREATED_CONSUMER` (for BetSubscriber)
  - `VG_LOCAL2_API_BET_SETTLED_CONSUMER` (for BetSettledSubscriber)

#### Verify Consumer Health
- [ ] **Consumer Status = "Active"**
- [ ] **Consumer Type = "Pull"**
- [ ] **Ack Policy = "Explicit"**
- [ ] **Max Deliver = 10**

### 2. **Message Processing Verification**

#### Test Message Publishing
- [ ] **Go to "Pub/Sub" tab in NATS NUI**
- [ ] **Publish test message to subject** (e.g., `vg.local2.api.bet.settled`)
- [ ] **Use sample JSON payload:**
```json
{
  "GameRoundId": "test-123",
  "PlayerId": 12345,
  "OperatorId": "test-op",
  "SiteName": "test-site",
  "ValidBetAmount": 100.00,
  "TotalAmount": 100.00,
  "TotalWin": 50.00,
  "GameTypeId": 1
}
```

#### Verify Message Consumption
- [ ] **Return to "Streams" → [StreamName] → "Messages"**
- [ ] **Confirm message appears in stream**
- [ ] **Check message is marked as "Acknowledged" within 30 seconds**
- [ ] **No error messages appear in application logs**

### 3. **Performance Monitoring**

#### Consumer Metrics Check
- [ ] **In stream details, check consumer metrics:**
  - **Delivered Messages**: Should increase after test message
  - **Acknowledged Messages**: Should equal delivered messages
  - **Pending Messages**: Should be 0 or low (<10)
  - **Redelivered Messages**: Should be 0 or minimal

#### Warning Signs (FAIL if detected)
- [ ] **❌ Pending Messages > 50**: Consumer overwhelmed
- [ ] **❌ Redelivered Count increasing**: Messages timing out
- [ ] **❌ Consumer shows "Inactive"**: Consumer not running
- [ ] **❌ Ack Pending > 10**: Messages taking too long to process

### 4. **Error Handling Verification**

#### Test Invalid Messages
- [ ] **Publish malformed JSON to test error handling:**
```json
{
  "invalid": "json structure"
  // Missing required fields
}
```

#### Expected Behavior
- [ ] **Message should be NAK'd (not acknowledged)**
- [ ] **Consumer should remain active**
- [ ] **No consumer crashes or restarts**
- [ ] **Error logged in application monitoring (Sentry)**

### 5. **Load Testing (Optional)**

#### Burst Message Test
- [ ] **Publish 10 messages rapidly**
- [ ] **All messages processed within 5 minutes**
- [ ] **No pending messages remain**
- [ ] **Consumer remains stable**

---

## 🚨 **Critical Issues (Immediate Escalation)**

### Consumer Problems
- [ ] **Consumer not visible in NATS NUI** → Escalate to Development
- [ ] **Consumer status "Inactive"** → Check application deployment
- [ ] **Pending messages increasing** → Check application logs for errors
- [ ] **Redelivered count > 50** → Timeout or processing issues

### Message Flow Issues
- [ ] **Messages not being consumed** → Check application health
- [ ] **All messages being NAK'd** → Configuration or code issue
- [ ] **Consumer restarts frequently** → Application stability issue

---

## 📋 **Environment-Specific Verification**

### Stream Names by Environment
| Environment | Stream Name | Subject Pattern |
|------------|-------------|-----------------|
| LOCAL | `api_events_vg_local` | `vg.local.api.*` |
| LOCAL_1 | `api_events_vg_local1` | `vg.local1.api.*` |
| LOCAL_2 | `api_events_vg_local2` | `vg.local2.api.*` |
| SIT | `api_events_vg_sit` | `vg.sit.api.*` |
| PROD | `api_events_vg_prod` | `vg.prod.api.*` |

### Subject Names to Test
- **Bet Messages**: `{env}.api.bet.created`
- **Bet Settled Messages**: `{env}.api.bet.settled`

Example: `vg.local2.api.bet.settled`

---

## 🎯 **Quick Pass/Fail Criteria**

### ✅ PASS Conditions
- All consumers visible and "Active"
- Test messages acknowledged within 30 seconds
- Pending messages = 0
- No redelivered messages during test
- Consumer metrics increasing normally

### ❌ FAIL Conditions
- Consumer missing or "Inactive"
- Messages not acknowledged after 1 minute
- Pending messages > 10
- Redelivered messages > 5 during test
- Application errors in logs

---

## 📞 **Support Information**

### When to Escalate
- **Immediate**: Consumer not running, messages not processing
- **High Priority**: High pending/redelivered counts
- **Medium Priority**: Slow processing (>30 seconds per message)

### Required Information for Escalation
1. Environment name
2. Consumer name
3. Screenshot from NATS NUI
4. Test message details
5. Timestamp of issue

---

**Tool**: [NATS NUI](https://github.com/nats-nui/nui) - Free NATS management GUI  
**Version**: 1.0  
**Created**: January 2025  
**Purpose**: Simplified QC verification using NATS NUI interface 