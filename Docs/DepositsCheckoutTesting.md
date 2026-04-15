# Testing the Deposits Checkout Endpoint

## Endpoint

```
POST /api/deposits/checkout
Authorization: Bearer <jwt-token>
Content-Type: application/json
```

## Request Payload

```json
{
  "propertyId": "YOUR-PROPERTY-GUID",
  "listingId":  "YOUR-LISTING-GUID",
  "amount": 500.00,
  "idempotencyKey": "deposit-test-001"
}
```

**Rules:**
- `propertyId` — must exist in the `Properties` table
- `listingId` — must exist, type `Rent`, and `IsPublished = true`
- `amount` — positive decimal; converted to cents for Stripe (e.g. `500.00` → 50000 AUD cents)
- `idempotencyKey` — unique string up to 100 chars; reusing the same key returns the existing session

## Flow

1. Service checks idempotency — returns existing session if already created
2. Validates `PropertyId` and `ListingId` in the DB
3. Creates a `Deposit` record (status = `Pending`)
4. Calls Stripe to create a hosted checkout session
5. Saves `StripeSessionId` and `StripeSessionUrl` back to the deposit
6. Returns `DepositResponse` including `sessionUrl`

## Response (`200 OK`)

```json
{
  "id": "...",
  "userId": "...",
  "propertyId": "...",
  "listingId": "...",
  "amount": 500.00,
  "currency": "AUD",
  "stripeSessionId": "cs_test_...",
  "status": "Pending",
  "paidAtUtc": null,
  "sessionUrl": "https://checkout.stripe.com/pay/cs_test_..."
}
```

## Completing Payment (No Frontend Needed)

1. Copy `sessionUrl` from the response
2. Paste it into any browser — Stripe hosts the checkout page
3. Use the Stripe test card:
   - Number: `4242 4242 4242 4242`
   - Expiry: any future date (e.g. `12/29`)
   - CVC: any 3 digits

## Webhook (Local Testing)

Use the Stripe CLI to forward events to your local API:

```bash
stripe listen --forward-to localhost:<port>/api/deposits/webhook
```

Use the printed webhook secret as `StripeSettings:WebhookSecret` in `appsettings`.  
When payment completes, Stripe fires `checkout.session.completed` → deposit status set to `Paid` + confirmation email sent.
