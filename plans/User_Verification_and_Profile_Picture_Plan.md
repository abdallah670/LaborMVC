# User Verification & Profile Picture Implementation Plan

**Generated:** March 11, 2026  
**Project:** LaborMVC (ASP.NET Core MVC)  

---

## 📋 Overview

This plan outlines the implementation of a complete user verification system including:
1. Profile Picture Upload with Cropping
2. Email Verification Flow
3. Phone Number Verification (SMS)
4. ID Document Upload + Selfie Verification (KYC)

---

## 🖼️ Feature 1: Profile Picture Upload

### User Story
As a user, I want to upload a profile picture so that other users can identify me on the platform.

### Technical Requirements

#### 1.1 Backend Implementation

**A. Add ProfilePictureUrl to User Entity**
```csharp
// Already exists in AppUser.cs - verify it's being used
public string? ProfilePictureUrl { get; set; }
```

**B. Create File Upload Controller**
```csharp
[Route("api/upload")]
public class UploadController : ControllerBase
{
    private readonly IFileUploadValidationService _fileUploadService;
    private readonly IUserService _userService;
    
    [HttpPost("profile-picture")]
    public async Task<IActionResult> UploadProfilePicture(IFormFile file)
    {
        // 1. Validate file using existing FileUploadValidationService
        // 2. Resize image to multiple sizes (thumbnail, medium, full)
        // 3. Save to storage (local/Azure Blob)
        // 4. Update user's ProfilePictureUrl
        // 5. Return URLs
    }
}
```

**C. Image Processing Service**
```csharp
public interface IImageProcessingService
{
    Task<ProcessedImage> ProcessProfilePictureAsync(IFormFile file);
    Task<byte[]> ResizeAsync(byte[] imageData, int width, int height);
    Task<byte[]> CropToSquareAsync(byte[] imageData);
}

public class ImageProcessingService : IImageProcessingService
{
    // Use ImageSharp or SkiaSharp for cross-platform image processing
    // Generate: thumbnail (100x100), medium (300x300), full (800x800)
}
```

**D. Storage Options**

| Option | Pros | Cons | Recommendation |
|--------|------|------|----------------|
| Local Storage | Simple, fast | Doesn't scale, backup needed | Dev only |
| Azure Blob Storage | Scalable, CDN, cheap | External dependency | ✅ Production |
| AWS S3 | Same as Azure | AWS dependency | Alternative |

**Configuration:**
```json
{
  "Storage": {
    "Provider": "AzureBlob",
    "AzureBlob": {
      "ConnectionString": "DefaultEndpointsProtocol=https;...",
      "ContainerName": "profile-pictures",
      "CdnUrl": "https://cdn.labortasks.com"
    }
  }
}
```

#### 1.2 Frontend Implementation

**A. Profile Picture Upload Component**
```html
<!-- Views/Account/UploadProfilePicture.cshtml -->
<div class="profile-picture-upload">
    <div class="current-picture">
        <img src="@Model.CurrentPictureUrl" alt="Profile" id="preview-image" />
    </div>
    
    <div class="upload-controls">
        <input type="file" id="file-input" accept="image/*" />
        <button id="upload-btn" class="btn btn-primary">Upload</button>
    </div>
    
    <!-- Cropper container -->
    <div id="cropper-container" style="display:none;">
        <img id="cropper-image" />
        <button id="crop-btn" class="btn btn-success">Crop & Save</button>
        <button id="cancel-crop-btn" class="btn btn-secondary">Cancel</button>
    </div>
</div>
```

**B. JavaScript with Cropper.js**
```javascript
// Use Cropper.js library for image cropping
// 1. User selects file
// 2. Show cropper interface with fixed 1:1 aspect ratio
// 3. User adjusts crop area
// 4. Upload cropped image blob to server
```

#### 1.3 Validation & Security

| Validation | Implementation |
|------------|---------------|
| File Type | JPEG, PNG, GIF only |
| File Size | Max 5MB |
| Dimensions | Min 200x200, Max 4000x4000 |
| Content Scan | Use existing FileUploadValidationService |
| Virus Scan | Pass through ClamAV when implemented |

---

## 📧 Feature 2: Email Verification Flow

### User Story
As a user, I want to verify my email address to ensure account security and enable full platform access.

### Current State
```csharp
// Program.cs - Currently disabled
options.SignIn.RequireConfirmedEmail = false;
```

### Implementation

#### 2.1 Backend Changes

**A. Enable Email Verification**
```csharp
// Update Program.cs
options.SignIn.RequireConfirmedEmail = true;
options.User.RequireUniqueEmail = true;
```

**B. Verification Token Generation**
```csharp
// Already exists in VerificationService - enhance it
public class VerificationService : IVerificationService
{
    public async Task<Response<bool>> SendEmailVerificationAsync(string userId)
    {
        var user = await _userManager.FindByIdAsync(userId);
        var token = await _userManager.GenerateEmailConfirmationTokenAsync(user);
        
        // Create verification link
        var callbackUrl = Url.Action(
            "ConfirmEmail", 
            "Account", 
            new { userId = user.Id, token = token }, 
            protocol: HttpContext.Request.Scheme);
        
        // Send email using existing SendGridEmailService
        await _emailService.SendVerificationCodeAsync(
            user.Email, 
            user.FirstName, 
            callbackUrl);
        
        // Store token expiry
        user.EmailVerificationToken = token;
        user.EmailVerificationExpiry = DateTime.UtcNow.AddHours(24);
        await _userManager.UpdateAsync(user);
    }
}
```

**C. Email Confirmation Endpoint**
```csharp
[HttpGet]
[AllowAnonymous]
public async Task<IActionResult> ConfirmEmail(string userId, string token)
{
    if (userId == null || token == null)
    {
        return RedirectToAction("Index", "Home");
    }

    var user = await _userManager.FindByIdAsync(userId);
    if (user == null)
    {
        return NotFound($"Unable to load user with ID '{userId}'.");
    }

    // Check token expiry
    if (user.EmailVerificationExpiry < DateTime.UtcNow)
    {
        TempData["Error"] = "Verification link has expired. Please request a new one.";
        return RedirectToAction("Profile");
    }

    var result = await _userManager.ConfirmEmailAsync(user, token);
    
    if (result.Succeeded)
    {
        TempData["Success"] = "Thank you for confirming your email!";
        
        // Upgrade verification tier
        await _verificationService.UpdateVerificationTierAsync(userId);
    }
    else
    {
        TempData["Error"] = "Error confirming your email.";
    }
    
    return RedirectToAction("Profile");
}
```

**D. Resend Verification Email**
```csharp
[HttpPost]
[Authorize]
public async Task<IActionResult> ResendEmailConfirmation()
{
    var user = await _userManager.GetUserAsync(User);
    
    if (user.EmailConfirmed)
    {
        TempData["Info"] = "Your email is already confirmed.";
        return RedirectToAction("Profile");
    }
    
    // Rate limit: max 3 resends per hour
    var canResend = await _verificationService.CanResendEmailAsync(user.Id);
    if (!canResend)
    {
        TempData["Error"] = "Please wait before requesting another email.";
        return RedirectToAction("Profile");
    }
    
    await _verificationService.SendEmailVerificationAsync(user.Id);
    TempData["Success"] = "Verification email sent. Please check your inbox.";
    
    return RedirectToAction("Profile");
}
```

#### 2.2 Email Template

**Verification Email Design:**
```html
<!DOCTYPE html>
<html>
<head>
    <style>
        .email-container { max-width: 600px; margin: 0 auto; font-family: Arial, sans-serif; }
        .header { background: #2563eb; color: white; padding: 20px; text-align: center; }
        .content { padding: 30px; background: #f9fafb; }
        .button { background: #2563eb; color: white; padding: 12px 24px; 
                  text-decoration: none; border-radius: 5px; display: inline-block; }
        .footer { padding: 20px; text-align: center; color: #6b7280; font-size: 12px; }
    </style>
</head>
<body>
    <div class="email-container">
        <div class="header">
            <h1>Verify Your Email</h1>
        </div>
        <div class="content">
            <h2>Hi {{UserName}},</h2>
            <p>Thank you for joining Labor Marketplace! Please verify your email address to complete your registration.</p>
            <p style="text-align: center; margin: 30px 0;">
                <a href="{{VerificationLink}}" class="button">Verify Email Address</a>
            </p>
            <p>Or copy and paste this link into your browser:</p>
            <p style="word-break: break-all;">{{VerificationLink}}</p>
            <p>This link will expire in 24 hours.</p>
            <p>If you didn't create an account, you can safely ignore this email.</p>
        </div>
        <div class="footer">
            <p>&copy; 2026 Labor Marketplace. All rights reserved.</p>
        </div>
    </div>
</body>
</html>
```

#### 2.3 UI Integration

**Profile Page - Email Section:**
```html
<div class="verification-section">
    <h4>Email Verification</h4>
    <div class="d-flex align-items-center">
        <span class="email-address">@Model.Email</span>
        @if (Model.IsEmailVerified)
        {
            <span class="badge bg-success ms-2">
                <i class="bi bi-check-circle"></i> Verified
            </span>
        }
        else
        {
            <span class="badge bg-warning ms-2">
                <i class="bi bi-exclamation-circle"></i> Not Verified
            </span>
            <form asp-action="ResendEmailConfirmation" method="post" class="ms-2">
                <button type="submit" class="btn btn-sm btn-outline-primary">
                    Resend Email
                </button>
            </form>
        }
    </div>
    @if (!Model.IsEmailVerified)
    {
        <div class="alert alert-info mt-2">
            <i class="bi bi-info-circle"></i>
            Please verify your email to unlock full platform features.
        </div>
    }
</div>
```

---

## 📱 Feature 3: Phone Number Verification

### User Story
As a user, I want to verify my phone number via SMS to add an extra layer of security and enable SMS notifications.

### Implementation

#### 3.1 Entity Updates

```csharp
// AppUser.cs - Add/verify these properties exist
public class AppUser : IdentityUser
{
    // Existing
    public string? PhoneVerificationCode { get; set; }
    public DateTime? PhoneVerificationExpiry { get; set; }
    public bool IsPhoneVerified { get; set; } = false;
    
    // Add new
    public string? PhoneNumberCountryCode { get; set; } = "+20"; // Default Egypt
    public DateTime? LastPhoneVerificationAttempt { get; set; }
    public int PhoneVerificationAttempts { get; set; } = 0;
}
```

#### 3.2 Backend Implementation

**A. Send Verification SMS**
```csharp
public class VerificationService : IVerificationService
{
    private readonly ISmsService _smsService;
    private readonly ILogger<VerificationService> _logger;
    
    public async Task<Response<bool>> SendPhoneVerificationAsync(string userId, string phoneNumber)
    {
        var user = await _userManager.FindByIdAsync(userId);
        
        // Rate limiting
        if (user.LastPhoneVerificationAttempt.HasValue && 
            user.LastPhoneVerificationAttempt.Value > DateTime.UtcNow.AddMinutes(1))
        {
            return Response<bool>.Failure("Please wait 1 minute before requesting a new code.");
        }
        
        if (user.PhoneVerificationAttempts >= 5)
        {
            return Response<bool>.Failure("Too many attempts. Please try again later.");
        }
        
        // Generate 6-digit code
        var code = new Random().Next(100000, 999999).ToString();
        
        // Store code (hashed for security)
        user.PhoneVerificationCode = _userManager.PasswordHasher.HashPassword(user, code);
        user.PhoneVerificationExpiry = DateTime.UtcNow.AddMinutes(10);
        user.LastPhoneVerificationAttempt = DateTime.UtcNow;
        user.PhoneVerificationAttempts++;
        user.PhoneNumber = phoneNumber;
        
        await _userManager.UpdateAsync(user);
        
        // Send SMS via Twilio
        var message = $"Your Labor Marketplace verification code is: {code}. Valid for 10 minutes.";
        await _smsService.SendSmsAsync(phoneNumber, message);
        
        _logger.LogInformation("Phone verification code sent to user {UserId}", userId);
        
        return Response<bool>.Success(true, "Verification code sent.");
    }
}
```

**B. Verify Phone Code**
```csharp
public async Task<Response<bool>> VerifyPhoneAsync(string userId, string code)
{
    var user = await _userManager.FindByIdAsync(userId);
    
    if (user.PhoneVerificationExpiry < DateTime.UtcNow)
    {
        return Response<bool>.Failure("Verification code has expired. Please request a new one.");
    }
    
    // Verify code
    var result = _userManager.PasswordHasher.VerifyHashedPassword(
        user, user.PhoneVerificationCode, code);
    
    if (result != PasswordVerificationResult.Success)
    {
        return Response<bool>.Failure("Invalid verification code.");
    }
    
    // Mark as verified
    user.IsPhoneVerified = true;
    user.PhoneVerificationCode = null;
    user.PhoneVerificationExpiry = null;
    user.PhoneVerificationAttempts = 0;
    
    await _userManager.UpdateAsync(user);
    await _verificationService.UpdateVerificationTierAsync(userId);
    
    return Response<bool>.Success(true, "Phone number verified successfully.");
}
```

**C. Controller Endpoints**
```csharp
[HttpPost]
[Authorize]
public async Task<IActionResult> SendPhoneVerification([FromBody] SendPhoneVerificationRequest request)
{
    var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
    
    // Validate phone number format
    if (!PhoneNumberUtil.IsValidNumber(request.PhoneNumber, request.CountryCode))
    {
        return BadRequest(Response<bool>.Failure("Invalid phone number format."));
    }
    
    var result = await _verificationService.SendPhoneVerificationAsync(
        userId, 
        request.PhoneNumber);
    
    return Ok(result);
}

[HttpPost]
[Authorize]
public async Task<IActionResult> VerifyPhone([FromBody] VerifyPhoneRequest request)
{
    var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
    
    var result = await _verificationService.VerifyPhoneAsync(userId, request.Code);
    
    return Ok(result);
}
```

#### 3.3 Frontend Implementation

**Phone Verification UI:**
```html
<div class="phone-verification-section">
    <h4>Phone Verification</h4>
    
    @if (!Model.IsPhoneVerified)
    {
        <div id="phone-input-section">
            <div class="input-group mb-3">
                <select class="form-select" id="country-code" style="max-width: 100px;">
                    <option value="+20">+20 (EG)</option>
                    <option value="+966">+966 (SA)</option>
                    <option value="+971">+971 (AE)</option>
                    <option value="+1">+1 (US)</option>
                </select>
                <input type="tel" class="form-control" id="phone-number" 
                       placeholder="Phone number" maxlength="15" />
            </div>
            <button id="send-code-btn" class="btn btn-primary">Send Verification Code</button>
        </div>
        
        <div id="code-input-section" style="display:none;">
            <p class="text-muted">Enter the 6-digit code sent to <span id="masked-phone"></span></p>
            <div class="d-flex gap-2 mb-3">
                <input type="text" class="form-control verification-code" maxlength="1" />
                <input type="text" class="form-control verification-code" maxlength="1" />
                <input type="text" class="form-control verification-code" maxlength="1" />
                <input type="text" class="form-control verification-code" maxlength="1" />
                <input type="text" class="form-control verification-code" maxlength="1" />
                <input type="text" class="form-control verification-code" maxlength="1" />
            </div>
            <button id="verify-code-btn" class="btn btn-success">Verify</button>
            <button id="resend-code-btn" class="btn btn-link">Resend Code</button>
            <p id="countdown" class="text-muted"></p>
        </div>
    }
    else
    {
        <div class="d-flex align-items-center">
            <span class="badge bg-success">
                <i class="bi bi-check-circle"></i> Verified
            </span>
            <span class="ms-2">@Model.PhoneNumber</span>
        </div>
    }
</div>
```

**JavaScript:**
```javascript
// Handle code input (auto-focus next field)
// Countdown timer for resend
// AJAX calls to backend
// Success/error handling
```

---

## 🪪 Feature 4: ID Verification with Selfie

### User Story
As a user, I want to upload my ID document and take a selfie to verify my identity (KYC), unlocking higher trust levels and platform features.

### Verification Flow

```
┌─────────────────────────────────────────────────────────────┐
│                    ID Verification Flow                     │
├─────────────────────────────────────────────────────────────┤
│ 1. User selects ID type (Passport, National ID, Driver's)  │
│ 2. User uploads front of ID document                        │
│ 3. User uploads back of ID document (if applicable)        │
│ 4. User takes live selfie                                   │
│ 5. System validates document quality and format            │
│ 6. [Optional] Automated verification via API               │
│ 7. Admin review queue for manual verification              │
│ 8. User is notified of verification result                 │
└─────────────────────────────────────────────────────────────┘
```

### Implementation

#### 4.1 Entity Design

```csharp
// New Entity: IdVerificationRequest
public class IdVerificationRequest
{
    public int Id { get; set; }
    public string UserId { get; set; } = null!;
    public AppUser User { get; set; } = null!;
    
    // Document Info
    public IdDocumentType DocumentType { get; set; }
    public string? DocumentNumber { get; set; }
    public string? DocumentCountry { get; set; }
    
    // File URLs
    public string FrontDocumentUrl { get; set; } = null!;
    public string? BackDocumentUrl { get; set; }
    public string SelfieUrl { get; set; } = null!;
    
    // Verification Status
    public VerificationStatus Status { get; set; } = VerificationStatus.Pending;
    public DateTime SubmittedAt { get; set; } = DateTime.UtcNow;
    public DateTime? ReviewedAt { get; set; }
    public string? ReviewedBy { get; set; }
    public string? RejectionReason { get; set; }
    
    // Automated Check Results
    public bool? DocumentQualityCheckPassed { get; set; }
    public bool? FaceMatchCheckPassed { get; set; }
    public float? FaceMatchConfidence { get; set; }
    
    // Metadata
    public string? IpAddress { get; set; }
    public string? UserAgent { get; set; }
}

public enum IdDocumentType
{
    Passport,
    NationalId,
    DriversLicense,
    ResidencePermit
}

public enum VerificationStatus
{
    Pending,
    InReview,
    Approved,
    Rejected,
    RequiresResubmission
}
```

#### 4.2 Backend Implementation

**A. ID Verification Service**
```csharp
public interface IIdVerificationService
{
    Task<IdVerificationResult> SubmitVerificationAsync(
        string userId,
        IdVerificationRequestDto request,
        IFormFile frontDocument,
        IFormFile? backDocument,
        IFormFile selfie);
    
    Task<IdVerificationRequest?> GetPendingVerificationAsync(string userId);
    Task<bool> HasPendingVerificationAsync(string userId);
    Task<bool> IsVerifiedAsync(string userId);
}

public class IdVerificationService : IIdVerificationService
{
    private readonly IFileUploadValidationService _fileUploadService;
    private readonly IImageProcessingService _imageProcessing;
    private readonly IFaceRecognitionService _faceRecognition;
    private readonly ILogger<IdVerificationService> _logger;
    
    public async Task<IdVerificationResult> SubmitVerificationAsync(
        string userId, 
        IdVerificationRequestDto request,
        IFormFile frontDocument,
        IFormFile? backDocument,
        IFormFile selfie)
    {
        // 1. Check if user already has pending verification
        if (await HasPendingVerificationAsync(userId))
        {
            return IdVerificationResult.Failure("You already have a pending verification request.");
        }
        
        // 2. Validate document images
        var frontValidation = await _fileUploadService.ValidateFileAsync(
            frontDocument, userId, null, null);
        if (!frontValidation.IsValid)
        {
            return IdVerificationResult.Failure($"Front document: {frontValidation.ErrorMessage}");
        }
        
        // 3. Validate selfie
        var selfieValidation = await _fileUploadService.ValidateFileAsync(
            selfie, userId, null, null);
        if (!selfieValidation.IsValid)
        {
            return IdVerificationResult.Failure($"Selfie: {selfieValidation.ErrorMessage}");
        }
        
        // 4. Check document quality
        var docQuality = await _imageProcessing.CheckDocumentQualityAsync(frontDocument);
        if (!docQuality.IsValid)
        {
            return IdVerificationResult.Failure(
                $"Document quality check failed: {docQuality.Issues}");
        }
        
        // 5. Upload files to secure storage
        var frontUrl = await UploadToSecureStorageAsync(frontDocument, "id-documents");
        var backUrl = backDocument != null 
            ? await UploadToSecureStorageAsync(backDocument, "id-documents") 
            : null;
        var selfieUrl = await UploadToSecureStorageAsync(selfie, "selfies");
        
        // 6. [Optional] Automated face matching
        FaceMatchResult? faceMatch = null;
        if (_faceRecognition.IsAvailable())
        {
            faceMatch = await _faceRecognition.CompareFacesAsync(frontDocument, selfie);
        }
        
        // 7. Create verification request
        var verificationRequest = new IdVerificationRequest
        {
            UserId = userId,
            DocumentType = request.DocumentType,
            DocumentNumber = MaskDocumentNumber(request.DocumentNumber),
            DocumentCountry = request.DocumentCountry,
            FrontDocumentUrl = frontUrl,
            BackDocumentUrl = backUrl,
            SelfieUrl = selfieUrl,
            DocumentQualityCheckPassed = docQuality.IsValid,
            FaceMatchCheckPassed = faceMatch?.IsMatch,
            FaceMatchConfidence = faceMatch?.Confidence,
            IpAddress = GetClientIpAddress(),
            UserAgent = GetUserAgent()
        };
        
        // 8. Save to database
        _context.IdVerificationRequests.Add(verificationRequest);
        await _context.SaveChangesAsync();
        
        // 9. Notify admin
        await _notificationService.NotifyAdminsAsync(
            "New ID Verification Request",
            $"User {userId} has submitted ID documents for verification.");
        
        _logger.LogInformation("ID verification submitted for user {UserId}", userId);
        
        return IdVerificationResult.Success(verificationRequest.Id);
    }
}
```

**B. Face Recognition Service (Optional)**
```csharp
public interface IFaceRecognitionService
{
    Task<FaceMatchResult> CompareFacesAsync(IFormFile documentImage, IFormFile selfieImage);
    Task<bool> IsAvailableAsync();
}

// Implementation using Azure Face API or AWS Rekognition
public class AzureFaceRecognitionService : IFaceRecognitionService
{
    private readonly FaceClient _faceClient;
    
    public async Task<FaceMatchResult> CompareFacesAsync(IFormFile docImage, IFormFile selfie)
    {
        // Detect faces in both images
        // Compare using Azure Face API
        // Return confidence score
    }
}
```

**C. Admin Review Interface**
```csharp
[Authorize(Roles = "Admin")]
public class IdVerificationController : Controller
{
    [HttpGet]
    public async Task<IActionResult> PendingVerifications()
    {
        var pending = await _context.IdVerificationRequests
            .Where(v => v.Status == VerificationStatus.Pending)
            .Include(v => v.User)
            .OrderBy(v => v.SubmittedAt)
            .ToListAsync();
            
        return View(pending);
    }
    
    [HttpGet]
    public async Task<IActionResult> Review(int id)
    {
        var request = await _context.IdVerificationRequests
            .Include(v => v.User)
            .FirstOrDefaultAsync(v => v.Id == id);
            
        if (request == null)
            return NotFound();
            
        // Generate secure, time-limited URLs for viewing documents
        var viewModel = new ReviewIdVerificationViewModel
        {
            Request = request,
            FrontDocumentUrl = GenerateSecureUrl(request.FrontDocumentUrl),
            BackDocumentUrl = request.BackDocumentUrl != null 
                ? GenerateSecureUrl(request.BackDocumentUrl) 
                : null,
            SelfieUrl = GenerateSecureUrl(request.SelfieUrl)
        };
        
        return View(viewModel);
    }
    
    [HttpPost]
    public async Task<IActionResult> Approve(int id)
    {
        var request = await _context.IdVerificationRequests
            .Include(v => v.User)
            .FirstOrDefaultAsync(v => v.Id == id);
            
        if (request == null)
            return NotFound();
            
        request.Status = VerificationStatus.Approved;
        request.ReviewedAt = DateTime.UtcNow;
        request.ReviewedBy = User.FindFirstValue(ClaimTypes.NameIdentifier);
        
        // Update user verification status
        request.User.IDVerified = true;
        request.User.VerificationTier = VerificationTier.Verified;
        
        await _context.SaveChangesAsync();
        
        // Notify user
        await _notificationService.SendEmailAsync(
            request.User.Email,
            "ID Verification Approved",
            "Your ID verification has been approved. You now have full access to the platform.");
        
        return RedirectToAction("PendingVerifications");
    }
    
    [HttpPost]
    public async Task<IActionResult> Reject(int id, string reason)
    {
        var request = await _context.IdVerificationRequests
            .Include(v => v.User)
            .FirstOrDefaultAsync(v => v.Id == id);
            
        if (request == null)
            return NotFound();
            
        request.Status = VerificationStatus.Rejected;
        request.RejectionReason = reason;
        request.ReviewedAt = DateTime.UtcNow;
        request.ReviewedBy = User.FindFirstValue(ClaimTypes.NameIdentifier);
        
        await _context.SaveChangesAsync();
        
        // Notify user
        await _notificationService.SendEmailAsync(
            request.User.Email,
            "ID Verification Requires Attention",
            $"Your ID verification was not approved. Reason: {reason}. Please resubmit.");
        
        return RedirectToAction("PendingVerifications");
    }
}
```

#### 4.3 Frontend Implementation

**ID Verification Flow UI:**
```html
<!-- Views/Account/IdVerification.cshtml -->
<div class="id-verification-container">
    <h3>Identity Verification</h3>
    <p class="text-muted">Verify your identity to unlock full platform features.</p>
    
    <!-- Progress Steps -->
    <div class="verification-steps mb-4">
        <div class="step active" data-step="1">
            <div class="step-number">1</div>
            <div class="step-label">Select ID Type</div>
        </div>
        <div class="step" data-step="2">
            <div class="step-number">2</div>
            <div class="step-label">Upload Documents</div>
        </div>
        <div class="step" data-step="3">
            <div class="step-number">3</div>
            <div class="step-label">Take Selfie</div>
        </div>
        <div class="step" data-step="4">
            <div class="step-number">4</div>
            <div class="step-label">Review & Submit</div>
        </div>
    </div>
    
    <!-- Step 1: Select ID Type -->
    <div id="step-1" class="verification-step-content">
        <h5>Select your ID type</h5>
        <div class="row g-3">
            <div class="col-md-3">
                <div class="id-type-card" data-type="Passport">
                    <i class="bi bi-passport fs-1"></i>
                    <p>Passport</p>
                </div>
            </div>
            <div class="col-md-3">
                <div class="id-type-card" data-type="NationalId">
                    <i class="bi bi-card-text fs-1"></i>
                    <p>National ID</p>
                </div>
            </div>
            <div class="col-md-3">
                <div class="id-type-card" data-type="DriversLicense">
                    <i class="bi bi-car-front fs-1"></i>
                    <p>Driver's License</p>
                </div>
            </div>
        </div>
    </div>
    
    <!-- Step 2: Upload Documents -->
    <div id="step-2" class="verification-step-content" style="display:none;">
        <h5>Upload your documents</h5>
        
        <div class="document-upload mb-4">
            <label class="form-label">Front of ID</label>
            <div class="upload-area" id="front-upload">
                <input type="file" id="front-document" accept="image/*" capture="environment" />
                <div class="upload-placeholder">
                    <i class="bi bi-cloud-upload fs-2"></i>
                    <p>Tap to upload or take photo</p>
                    <small class="text-muted">JPEG, PNG - Max 5MB</small>
                </div>
                <img id="front-preview" class="preview-image" style="display:none;" />
            </div>
        </div>
        
        <div class="document-upload mb-4" id="back-upload-container">
            <label class="form-label">Back of ID</label>
            <div class="upload-area" id="back-upload">
                <input type="file" id="back-document" accept="image/*" capture="environment" />
                <div class="upload-placeholder">
                    <i class="bi bi-cloud-upload fs-2"></i>
                    <p>Tap to upload or take photo</p>
                </div>
                <img id="back-preview" class="preview-image" style="display:none;" />
            </div>
        </div>
    </div>
    
    <!-- Step 3: Take Selfie -->
    <div id="step-3" class="verification-step-content" style="display:none;">
        <h5>Take a selfie</h5>
        <p class="text-muted">Make sure your face is clearly visible and well-lit.</p>
        
        <div class="selfie-capture mb-4">
            <video id="selfie-video" autoplay playsinline style="display:none;"></video>
            <canvas id="selfie-canvas" style="display:none;"></canvas>
            <img id="selfie-preview" class="preview-image" style="display:none;" />
            
            <div class="selfie-guide">
                <div class="face-outline"></div>
                <p>Position your face within the circle</p>
            </div>
            
            <button id="capture-selfie-btn" class="btn btn-primary">
                <i class="bi bi-camera"></i> Capture
            </button>
            <button id="retake-selfie-btn" class="btn btn-secondary" style="display:none;">
                <i class="bi bi-arrow-counterclockwise"></i> Retake
            </button>
        </div>
    </div>
    
    <!-- Step 4: Review & Submit -->
    <div id="step-4" class="verification-step-content" style="display:none;">
        <h5>Review your submission</h5>
        
        <div class="review-section mb-4">
            <div class="d-flex justify-content-between mb-2">
                <span>ID Type:</span>
                <strong id="review-id-type"></strong>
            </div>
            <div class="d-flex justify-content-between mb-2">
                <span>Front Document:</span>
                <span class="text-success"><i class="bi bi-check-circle"></i> Uploaded</span>
            </div>
            <div class="d-flex justify-content-between mb-2" id="review-back-document">
                <span>Back Document:</span>
                <span class="text-success"><i class="bi bi-check-circle"></i> Uploaded</span>
            </div>
            <div class="d-flex justify-content-between mb-2">
                <span>Selfie:</span>
                <span class="text-success"><i class="bi bi-check-circle"></i> Captured</span>
            </div>
        </div>
        
        <div class="alert alert-info">
            <i class="bi bi-info-circle"></i>
            Your documents will be securely stored and reviewed within 24 hours.
        </div>
        
        <button id="submit-verification-btn" class="btn btn-success btn-lg w-100">
            Submit for Verification
        </button>
    </div>
    
    <!-- Navigation Buttons -->
    <div class="step-navigation mt-4">
        <button id="prev-btn" class="btn btn-outline-secondary" style="display:none;">Back</button>
        <button id="next-btn" class="btn btn-primary" disabled>Continue</button>
    </div>
</div>
```

**CSS Styling:**
```css
.verification-steps {
    display: flex;
    justify-content: space-between;
    position: relative;
}

.verification-steps::before {
    content: '';
    position: absolute;
    top: 20px;
    left: 0;
    right: 0;
    height: 2px;
    background: #e5e7eb;
    z-index: 0;
}

.step {
    display: flex;
    flex-direction: column;
    align-items: center;
    position: relative;
    z-index: 1;
}

.step-number {
    width: 40px;
    height: 40px;
    border-radius: 50%;
    background: #e5e7eb;
    display: flex;
    align-items: center;
    justify-content: center;
    font-weight: bold;
    margin-bottom: 8px;
}

.step.active .step-number {
    background: #2563eb;
    color: white;
}

.step.completed .step-number {
    background: #10b981;
    color: white;
}

.id-type-card {
    border: 2px solid #e5e7eb;
    border-radius: 8px;
    padding: 20px;
    text-align: center;
    cursor: pointer;
    transition: all 0.2s;
}

.id-type-card:hover,
.id-type-card.selected {
    border-color: #2563eb;
    background: #eff6ff;
}

.upload-area {
    border: 2px dashed #d1d5db;
    border-radius: 8px;
    padding: 40px;
    text-align: center;
    cursor: pointer;
    position: relative;
    overflow: hidden;
}

.upload-area:hover {
    border-color: #2563eb;
    background: #f9fafb;
}

.upload-area input[type="file"] {
    position: absolute;
    top: 0;
    left: 0;
    width: 100%;
    height: 100%;
    opacity: 0;
    cursor: pointer;
}

.selfie-guide {
    position: relative;
    width: 300px;
    height: 300px;
    margin: 0 auto;
    border-radius: 50%;
    border: 3px dashed #2563eb;
    display: flex;
    align-items: center;
    justify-content: center;
}

.face-outline {
    width: 200px;
    height: 250px;
    border: 2px solid rgba(37, 99, 235, 0.3);
    border-radius: 50% 50% 45% 45%;
}
```

#### 4.4 Security Considerations

| Security Measure | Implementation |
|-----------------|----------------|
| Encryption at Rest | AES-256 for stored documents |
| Encryption in Transit | HTTPS only |
| Access Control | Admin-only access to documents |
| Audit Logging | Log all document access |
| Time-Limited URLs | SAS tokens with 15-min expiry |
| Automatic Deletion | Delete after verification or 30 days |
| PII Masking | Mask document numbers in logs |
| Secure Storage | Separate storage container with restricted access |

---

## 📊 Database Migrations

```csharp
// Migration for new entities
public partial class AddUserVerificationEnhancements : Migration
{
    protected override void Up(MigrationBuilder migrationBuilder)
    {
        // Add columns to AppUser
        migrationBuilder.AddColumn<bool>(
            name: "IsPhoneVerified",
            table: "AspNetUsers",
            type: "bit",
            nullable: false,
            defaultValue: false);
            
        migrationBuilder.AddColumn<string>(
            name: "PhoneNumberCountryCode",
            table: "AspNetUsers",
            type: "nvarchar(10)",
            nullable: true);
            
        // Create IdVerificationRequests table
        migrationBuilder.CreateTable(
            name: "IdVerificationRequests",
            columns: table => new
            {
                Id = table.Column<int>(type: "int", nullable: false)
                    .Annotation("SqlServer:Identity", "1, 1"),
                UserId = table.Column<string>(type: "nvarchar(450)", nullable: false),
                DocumentType = table.Column<int>(type: "int", nullable: false),
                DocumentNumber = table.Column<string>(type: "nvarchar(max)", nullable: true),
                DocumentCountry = table.Column<string>(type: "nvarchar(max)", nullable: true),
                FrontDocumentUrl = table.Column<string>(type: "nvarchar(max)", nullable: false),
                BackDocumentUrl = table.Column<string>(type: "nvarchar(max)", nullable: true),
                SelfieUrl = table.Column<string>(type: "nvarchar(max)", nullable: false),
                Status = table.Column<int>(type: "int", nullable: false),
                SubmittedAt = table.Column<DateTime>(type: "datetime2", nullable: false),
                ReviewedAt = table.Column<DateTime>(type: "datetime2", nullable: true),
                ReviewedBy = table.Column<string>(type: "nvarchar(max)", nullable: true),
                RejectionReason = table.Column<string>(type: "nvarchar(max)", nullable: true),
                DocumentQualityCheckPassed = table.Column<bool>(type: "bit", nullable: true),
                FaceMatchCheckPassed = table.Column<bool>(type: "bit", nullable: true),
                FaceMatchConfidence = table.Column<float>(type: "real", nullable: true),
                IpAddress = table.Column<string>(type: "nvarchar(max)", nullable: true),
                UserAgent = table.Column<string>(type: "nvarchar(max)", nullable: true)
            },
            constraints: table =>
            {
                table.PrimaryKey("PK_IdVerificationRequests", x => x.Id);
                table.ForeignKey(
                    name: "FK_IdVerificationRequests_AspNetUsers_UserId",
                    column: x => x.UserId,
                    principalTable: "AspNetUsers",
                    principalColumn: "Id",
                    onDelete: ReferentialAction.Cascade);
            });
            
        migrationBuilder.CreateIndex(
            name: "IX_IdVerificationRequests_UserId",
            table: "IdVerificationRequests",
            column: "UserId");
    }
}
```

---

## 🎯 Implementation Roadmap

### Phase 1: Profile Picture Upload (Week 1)
| Task | Effort | Owner |
|------|--------|-------|
| Image processing service | 8h | Backend |
| Upload controller & API | 4h | Backend |
| Frontend cropper component | 8h | Frontend |
| Storage configuration | 4h | DevOps |
| Testing | 4h | QA |

### Phase 2: Email Verification (Week 1)
| Task | Effort | Owner |
|------|--------|-------|
| Enable email confirmation | 2h | Backend |
| Email templates | 4h | Frontend |
| Verification flow UI | 4h | Frontend |
| Resend functionality | 2h | Backend |

### Phase 3: Phone Verification (Week 2)
| Task | Effort | Owner |
|------|--------|-------|
| SMS service integration | 4h | Backend |
| Phone verification endpoints | 4h | Backend |
| Phone input component | 4h | Frontend |
| Verification code UI | 4h | Frontend |

### Phase 4: ID Verification (Weeks 2-3)
| Task | Effort | Owner |
|------|--------|-------|
| ID verification entities | 4h | Backend |
| Document upload service | 8h | Backend |
| Face recognition integration (optional) | 8h | Backend |
| Admin review interface | 8h | Full Stack |
| Multi-step verification UI | 16h | Frontend |
| Security hardening | 8h | Backend |

---

## ✅ Acceptance Criteria

### Profile Picture
- [ ] User can upload image (JPEG/PNG, max 5MB)
- [ ] Image is cropped to square with preview
- [ ] Multiple sizes generated (thumbnail, medium, full)
- [ ] File is virus scanned
- [ ] Image appears in profile immediately

### Email Verification
- [ ] Verification email sent on registration
- [ ] Email contains secure link valid for 24 hours
- [ ] User sees verification status in profile
- [ ] Unverified users see prompts to verify
- [ ] Resend email functionality with rate limiting
- [ ] Verification upgrades user tier

### Phone Verification
- [ ] User can enter phone number with country code
- [ ] 6-digit SMS code sent within 30 seconds
- [ ] Code expires after 10 minutes
- [ ] Rate limiting: 3 attempts per hour
- [ ] Success updates verification status
- [ ] Verified phone shown in profile

### ID Verification
- [ ] User selects ID type from dropdown
- [ ] Front and back document upload with preview
- [ ] Live selfie capture with face guide
- [ ] Document quality validation
- [ ] Optional face matching verification
- [ ] Secure storage with encryption
- [ ] Admin review queue interface
- [ ] User notified of approval/rejection
- [ ] Rejection includes reason
- [ ] Approved users get verified badge

---

## 🔒 Security Checklist

- [ ] All uploads validated and scanned
- [ ] Documents encrypted at rest (AES-256)
- [ ] Time-limited access URLs for viewing
- [ ] Admin access logged and audited
- [ ] PII masked in logs
- [ ] Rate limiting on all verification endpoints
- [ ] CSRF protection on forms
- [ ] Secure token generation (cryptographically random)
- [ ] Automatic cleanup of rejected documents

---

**Document Version:** 1.0  
**Last Updated:** March 11, 2026  
**Status:** Ready for Implementation
