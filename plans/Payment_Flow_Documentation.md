# Labor Marketplace - Payment Flow Documentation

## Overview

This document provides a detailed explanation of the **Escrow Payment System** used in the Labor Marketplace platform. It clarifies what happens after a Poster (Client) pays for a booking, including payment states, release conditions, cancellation rules, and dispute handling.

---

## 1. Payment Flow - The Complete Journey

### High-Level Flow Diagram

```
┌─────────────────┐
│  Booking Created│  ← Worker accepts task, booking status: SCHEDULED
└────────┬────────┘
         │
         ▼
┌─────────────────┐
│  Poster Pays    │  ← Full amount held in Escrow
│  (Checkout)     │     Stripe PaymentIntent created
└────────┬────────┘     Payment Status: PENDING → HELD
         │
         ▼
┌─────────────────┐
│  Payment HELD   │  ← Money is SECURED but NOT released
│  in Escrow      │     Worker can safely start work
└────────┬────────┘
         │
         ▼
┌─────────────────┐
│  Work Starts    │  ← Worker marks "Start Work"
│                 │     Booking Status: IN_PROGRESS
└────────┬────────┘
         │
         ▼
┌─────────────────┐
│  Work Complete  │  ← Worker marks "Complete"
│  (Worker)       │     Booking Status: COMPLETED_FROM_WORKER
└────────┬────────┘
         │
         ▼
┌─────────────────┐
│  Poster Confirms│  ← Poster reviews and confirms completion
│                 │     Booking Status: COMPLETED
└────────┬────────┘
         │
         ▼
┌─────────────────┐
│  Payment        │  ← Funds released to Worker's account
│  RELEASED       │     Payment Status: RELEASED
└─────────────────┘
```

---

## 2. What Happens After Poster Pays?

### The "Held" State Explained

When a Poster pays for a booking, here's exactly what happens:

| Step | Action | System State | User Experience |
|------|--------|--------------|-----------------|
| 1 | Poster enters card details on Checkout page | Payment processing | Poster sees loading state |
| 2 | Stripe validates and authorizes payment | PaymentIntent created | Success message shown |
| 3 | Funds are **authorized** but **NOT captured** | Status: `Held` | Poster sees "Payment Secured" |
| 4 | Payment record created in database | Linked to Booking | Worker notified "Payment Secured" |
| 5 | Work can begin | Booking: `Scheduled` | Both parties proceed with confidence |

### Key Point: 🔒 The Money is SAFE

- **The money is NOT in the Worker's account yet**
- **The money is NOT available to the Poster anymore**
- **The money is HELD by Stripe in Escrow** until work is completed
- This protects both parties:
  - Worker knows payment is guaranteed
  - Poster knows money won't be released until work is done

---

## 3. Payment States Reference

### State Machine Diagram

```
                    ┌─────────────┐
         ┌─────────▶│   PENDING   │◀────────┐
         │          │  (Initial)  │         │
         │          └──────┬──────┘         │
         │                 │                │
         │                 │ Payment        │
         │                 │ Failed         │
         │                 ▼                │
         │          ┌─────────────┐         │
         │          │    HELD     │─────────┘
         │          │   (Escrow)  │   New Payment
         │          └──────┬──────┘
         │                 │
         │     ┌───────────┼───────────┐
         │     │           │           │
         │     ▼           ▼           ▼
         │ ┌───────┐  ┌────────┐  ┌──────────┐
         │ │RELEASED│  │REFUNDED│  │PARTIALLY │
         └─┤        │  │        │  │ REFUNDED │
           └────────┘  └────────┘  └──────────┘
```

### State Definitions

| State | Description | When It Happens |
|-------|-------------|-----------------|
| **Pending** | Payment initiated but not confirmed | Briefly during checkout processing |
| **Held** | ✅ Money secured in Escrow | After successful payment authorization |
| **Released** | ✅ Money transferred to Worker | After both parties confirm completion |
| **Refunded** | ✅ Full amount returned to Poster | Cancellation or dispute resolution |
| **PartiallyRefunded** | ⚠️ Partial amount returned | Late cancellation (< 2 hours before start) |

---

## 4. Cancellation & Refund Rules

### Cancellation Matrix

| Scenario | Who Cancels | Time Before Start | Poster Refund | Worker Compensation | Penalty |
|----------|-------------|-------------------|---------------|---------------------|---------|
| **Early Cancel** | Poster | > 2 hours | 100% | $0 | None |
| **Late Cancel** | Poster | < 2 hours | 50% | 50% | None |
| **Worker No-Show** | System | After start + 30 min | 100% | $0 | Worker rating penalty |
| **Mutual Cancel** | Both | Anytime | 100% | $0 | None |
| **Dispute Resolved** | Admin | Anytime | Varies | Varies | Per resolution |

### Cancellation Flow Examples

#### Example 1: Early Cancellation (More than 2 hours)
```
Booking Start: Today at 4:00 PM
Cancellation:  Today at 1:00 PM (3 hours before)

Result:
- Poster gets 100% refund
- Worker gets $0
- No penalties
- Booking Status: CANCELLED
- Payment Status: REFUNDED
```

#### Example 2: Late Cancellation (Less than 2 hours)
```
Booking Start: Today at 4:00 PM
Cancellation:  Today at 2:30 PM (1.5 hours before)

Result:
- Poster gets 50% refund
- Worker gets 50% (compensation for late notice)
- No penalties on Poster
- Booking Status: CANCELLED
- Payment Status: PARTIALLY_REFUNDED
```

#### Example 3: Worker No-Show
```
Booking Start: Today at 4:00 PM
Current Time:  Today at 4:30 PM (No check-in)

Result:
- Poster gets 100% refund
- Worker gets $0
- Worker receives rating penalty
- Worker account may be suspended after repeated offenses
- Booking Status: CANCELLED
- Payment Status: REFUNDED
```

---

## 5. Dispute Flow & Payment Freezing

### When Can a Dispute Be Raised?

- **Time Window:** Within 48 hours of task completion (or scheduled end time)
- **By Who:** Either Poster or Worker
- **Reason:** Quality issues, incomplete work, payment disagreements, etc.

### Dispute Impact on Payment

```
┌─────────────────────────────────────────┐
│     Dispute Raised by Either Party      │
└───────────────────┬─────────────────────┘
                    │
                    ▼
┌─────────────────────────────────────────┐
│   ⚠️  PAYMENT IMMEDIATELY FROZEN       │
│                                         │
│   Payment Status: HELD (Locked)         │
│   No one can access funds               │
│   Funds held until resolution           │
└───────────────────┬─────────────────────┘
                    │
        ┌───────────┴───────────┐
        │                       │
        ▼                       ▼
┌───────────────┐       ┌───────────────┐
│  Admin Review │       │  Auto-Resolve │
│  (7 days max) │       │  (if no admin)│
└───────┬───────┘       └───────┬───────┘
        │                       │
        ▼                       ▼
┌───────────────┐       ┌───────────────┐
│   RESOLVED    │◀─────▶│ 50/50 Split   │
│   Decision    │       │  After 7 days │
└───────────────┘       └───────────────┘
```

### Dispute Resolution Outcomes

| Outcome | Poster Receives | Worker Receives | Payment Status |
|---------|-----------------|-----------------|----------------|
| **Full Refund** | 100% | 0% | REFUNDED |
| **Partial Refund** | X% | (100-X)% | PARTIALLY_REFUNDED |
| **Full Payment** | 0% | 100% | RELEASED |
| **50/50 Split** | 50% | 50% | PARTIALLY_REFUNDED |

---

## 6. Auto-Release Mechanism

### What is Auto-Release?

To prevent payments from being stuck indefinitely, the system has an **automatic release** feature:

```
┌────────────────────────────────────────────┐
│  Worker Marks Complete                     │
└────────────────────┬───────────────────────┘
                     │
                     ▼
┌────────────────────────────────────────────┐
│  Poster Has 24 Hours to:                   │
│  ✓ Confirm Completion → Payment Released   │
│  ✓ Raise Dispute → Payment Frozen          │
│  ✗ Do Nothing → AUTO-RELEASE after 24h     │
└────────────────────────────────────────────┘
```

### Auto-Release Logic (Hangfire Background Job)

```csharp
IF (Booking.Status == "CompletedFromWorker") 
   AND (Time.Now > WorkerCompletedAt + 24 hours)
   AND (No Dispute Raised)
THEN
   Automatically release payment to Worker
   Booking.Status = "Completed"
   Payment.Status = "Released"
END
```

This protects workers from Posters who might intentionally delay confirmation to hold payment.

---

## 7. Implementation Details

### Key Classes & Services

| Class/Service | Purpose | Key Methods |
|---------------|---------|-------------|
| `PaymentService` | Stripe integration | `CreatePaymentIntentAsync`, `CapturePaymentAsync`, `RefundPaymentAsync` |
| `EscrowService` | Payment orchestration | `HoldPaymentAsync`, `ReleasePaymentAsync`, `ProcessCancellationAsync` |
| `Payment` (Entity) | Payment data | Stores Stripe PaymentIntent ID, status, amounts |
| `Booking` (Entity) | Booking lifecycle | Tracks booking status, links to payment |

### Stripe Integration Flow

```
1. CreatePaymentIntentAsync()
   └── Stripe.PaymentIntentCreateOptions
       ├── Amount: (amount * 100)  // Convert to cents
       ├── Currency: "usd"
       ├── CaptureMethod: "manual"  // ← Key: Don't capture immediately!
       └── Metadata: { bookingId }

2. CapturePaymentAsync()  // Called on completion
   └── Stripe.PaymentIntentService.CaptureAsync()
       └── Transfers funds to Worker's connected account

3. RefundPaymentAsync()   // Called on cancellation
   └── Stripe.RefundService.CreateAsync()
       └── Returns funds to Poster's payment method
```

### Database Schema

**Payment Entity:**
```csharp
public class Payment
{
    public int Id { get; set; }
    public int BookingId { get; set; }
    public decimal Amount { get; set; }
    public PaymentStatus Status { get; set; }  // Pending, Held, Released, Refunded, PartiallyRefunded
    public string StripePaymentIntentId { get; set; }
    public DateTime CreatedAt { get; set; }
    public DateTime? ReleasedAt { get; set; }
    
    // Navigation
    public virtual Booking Booking { get; set; }
}
```

### Booking Status Flow

```
Scheduled → InProgress → CompletedFromWorker → Completed
    │            │              │
    ▼            ▼              ▼
Cancelled   Cancelled      Disputed
```

---

## 8. Summary Table: Poster Perspective

| Poster Action | System Response | Payment Status | What Poster Gets |
|---------------|-----------------|----------------|------------------|
| Pays for booking | Payment held in escrow | **HELD** | Work starts |
| Cancels early (>2h) | Full refund processed | REFUNDED | 100% money back |
| Cancels late (<2h) | Partial refund | PARTIALLY_REFUNDED | 50% money back |
| Confirms completion | Payment released | RELEASED | Completed service |
| Raises dispute | Payment frozen | HELD | Admin review |
| Does nothing for 24h | Auto-release to worker | RELEASED | Completed service |

---

## 9. Summary Table: Worker Perspective

| Worker Action | System Response | Payment Status | What Worker Gets |
|---------------|-----------------|----------------|------------------|
| Accepts booking | Waiting for payment | - | Nothing yet |
| Payment secured | Can start work | **HELD** | Work begins |
| Starts work | Work in progress | HELD | Work continues |
| Marks complete | Waiting for confirmation | HELD | 24h countdown |
| Poster confirms | Payment released | **RELEASED** | ✅ Money received |
| Auto-release | Payment released | **RELEASED** | ✅ Money received |
| Dispute raised | Payment frozen | HELD | Wait for resolution |

---

## 10. Quick Reference: Common Questions

### Q: Where is my money after I pay?
**A:** Your money is held safely in Escrow by Stripe. It's neither in your account nor the Worker's account yet. It's secured until work is completed.

### Q: When does the Worker get paid?
**A:** The Worker gets paid only after:
1. They mark the work as complete, AND
2. You confirm completion, OR
3. 24 hours pass without you taking action (auto-release)

### Q: Can I get a refund?
**A:** Yes, depending on when you cancel:
- More than 2 hours before start: **100% refund**
- Less than 2 hours before start: **50% refund** (Worker gets 50% for late notice)
- After start time: Depends on dispute resolution

### Q: What if the Worker doesn't show up?
**A:** If the Worker doesn't check in within 30 minutes of the start time, the system automatically flags them as a no-show. You get a **100% refund**, and the Worker receives a penalty.

### Q: What if I'm not satisfied with the work?
**A:** You can raise a dispute within 48 hours of completion. The payment will be frozen, and an admin will review the case to determine the appropriate resolution.

---

## 11. Technical Flow Diagram (For Developers)

```
┌─────────────────────────────────────────────────────────────┐
│                      PAYMENT CONTROLLER                      │
│                     (PaymentController.cs)                   │
└─────────────────────────────────────────────────────────────┘
                              │
        ┌─────────────────────┼─────────────────────┐
        ▼                     ▼                     ▼
┌──────────────┐     ┌──────────────┐     ┌──────────────┐
│   Checkout   │     │    Status    │     │   Webhook    │
│    (GET)     │     │    (GET)     │     │   (POST)     │
└──────┬───────┘     └──────────────┘     └──────────────┘
       │
       ▼
┌─────────────────────────────────────────────────────────────┐
│                    PAYMENT SERVICE                           │
│                 (PaymentService.cs)                          │
│  ┌─────────────┐  ┌─────────────┐  ┌─────────────┐          │
│  │CreatePayment│  │CapturePayment│  │RefundPayment│          │
│  │   Intent    │  │             │  │             │          │
│  └──────┬──────┘  └──────┬──────┘  └──────┬──────┘          │
└─────────┼────────────────┼────────────────┼──────────────────┘
          │                │                │
          ▼                ▼                ▼
┌─────────────────────────────────────────────────────────────┐
│                     STRIPE API                               │
│  ┌─────────────┐  ┌─────────────┐  ┌─────────────┐          │
│  │PaymentIntent│  │   Capture   │  │   Refund    │          │
│  │   Create    │  │             │  │             │          │
│  └─────────────┘  └─────────────┘  └─────────────┘          │
└─────────────────────────────────────────────────────────────┘
          │                │                │
          ▼                ▼                ▼
┌─────────────────────────────────────────────────────────────┐
│                   ESCROW SERVICE                             │
│                (EscrowService.cs)                            │
│  ┌─────────────┐  ┌─────────────┐  ┌─────────────┐          │
│  │HoldPayment  │  │ReleasePayment│  │ProcessCancel│          │
│  │             │  │             │  │   lation    │          │
│  └─────────────┘  └─────────────┘  └─────────────┘          │
└─────────────────────────────────────────────────────────────┘
```

---

**Document Version:** 1.0  
**Last Updated:** February 24, 2026  
**System:** Labor Marketplace v1.0
