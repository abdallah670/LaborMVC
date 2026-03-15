# API Keys Setup Guide

## 1. TWILIO (For SMS/Phone Verification)

### How to Get Twilio API Keys:

1. **Sign up at**: https://www.twilio.com/try-twilio
2. **Verify your email and phone number**
3. **Get your credentials** from the Console Dashboard:
   - **Account SID**: Starts with `AC...`
   - **Auth Token**: Click "Show" to reveal
   - **Phone Number**: Buy a number from Twilio (or use trial number)

### Trial Account:
- Free trial gives you $15.50 credit
- You can only send SMS to verified phone numbers
- To send to any number, upgrade to paid account

### Pricing:
- SMS: ~$0.0075 per message (varies by country)
- Phone numbers: ~$1-2 per month

### Update appsettings.json:
```json
"Twilio": {
  "AccountSid": "ACxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxx",
  "AuthToken": "347a6ce4927a23e552c4f04beb4381d0",
  "PhoneNumber": "+1234567890"
}
```

---

## 2. GMAIL (For Email)

### You Already Have Gmail Setup! ✅

Your current settings in appsettings.json:
```json
"EmailSettings": {
  "SmtpHost": "smtp.gmail.com",
  "SmtpPort": 587,
  "SmtpUser": "hnbg14006@gmail.com",
  "SmtpPass": "vlkk jmat ouli zbgs",
  "FromEmail": "hnbg14006@gmail.com",
  "FromName": "MenoPro Gym"
}
```

### How to Get Gmail App Password:

1. **Enable 2-Factor Authentication** on your Google Account
2. **Generate App Password**:
   - Go to: https://myaccount.google.com/apppasswords
   - Select "Mail" and your device
   - Copy the 16-character password
3. **Replace** `SmtpPass` with your app password

### Alternative: SendGrid (You Also Have This!)

Your SendGrid is already configured:
```json
"SendGrid": {
  "ApiKey": "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",
  "FromEmail": "ztkarmabwmtr@Gmail.com",
  "FromName": "Ezzat Karem"
}
```

SendGrid is better for production (more reliable, better deliverability).

---

## 3. ID VERIFICATION SERVICE

### Option A: Manual Review (Free - Current Setup)

The current system allows users to upload:
- Front of ID (required)
- Back of ID (optional)
- Selfie with ID (optional)

**You (Admin) review and approve/reject** from the Admin panel.

**No API needed!** ✅

### Option B: Automated ID Verification (Paid)

If you want automatic ID verification, here are options:

#### 1. **Jumio** (Popular)
- Website: https://www.jumio.com/
- Pricing: Custom (expensive, enterprise-grade)
- Features: AI-powered document verification, facial recognition

#### 2. **Onfido**
- Website: https://onfido.com/
- Pricing: Pay per check (~$2-5 per verification)
- Features: Document verification, biometric checks

#### 3. **Sumsub**
- Website: https://sumsub.com/
- Pricing: Pay per verification
- Features: KYC/AML compliance

#### 4. **ID.me** (For US)
- Website: https://www.id.me/
- Good for US market

### Recommendation:

For now, **stick with Manual Review** (it's free and works well for startups).

When you grow, consider **Onfido** or **Sumsub** for automated verification.

---

## Summary

| Service | Status | Cost | Action Needed |
|---------|--------|------|---------------|
| **Twilio (SMS)** | ❌ Not configured | $15 free trial + pay per SMS | Sign up at twilio.com |
| **Gmail (Email)** | ✅ Configured | Free | Already working! |
| **SendGrid (Email)** | ✅ Configured | Free tier (100 emails/day) | Already working! |
| **ID Verification** | ✅ Manual review | Free | No API needed |

---

## Quick Start Checklist

- [ ] Sign up for Twilio: https://www.twilio.com/try-twilio
- [ ] Get Account SID, Auth Token, and Phone Number
- [ ] Update appsettings.json with Twilio credentials
- [ ] Test phone verification
- [ ] Test email verification (already working)
- [ ] Test ID upload (already working)

---

## Testing Without Real APIs

For development, you can:

1. **Phone**: Just log the code to console instead of sending SMS
2. **Email**: Use your Gmail (already working)
3. **ID**: Manual review works immediately

Contact me if you need help setting up any of these!
