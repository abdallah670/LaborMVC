# 🏗️ Labor Marketplace - Backend Architecture & Service Refactor Tracker

> **Project**: Labor Marketplace ASP.NET MVC Application  
> **Goal**: Refactor legacy services and ViewModels to modern, decoupled architecture  
> **Focus**: Security, Scalability, and Clean Code  
> **Last Updated**: March 22, 2026

---

## 📊 Progress Overview

| Module | Status | Progress |
|--------|--------|----------|
| User Service & ViewModels | ✅ Completed | 100% |
| Verification Service | 🔄 In Progress | 40% |
| Image & Storage Infrastructure | ⏳ Pending | 0% |
| Task & Application Service | ⏳ Pending | 0% |
| Booking & Payment Logic | ⏳ Pending | 0% |

---

## 👤 User Service & Profile Refactoring ✅

### Status: COMPLETED

| # | Task | Description | Status |
|---|------|-------------|--------|
| 1.1 | Create UserProfileDisplayViewModel | Read-only model with stats & ratings | ✅ |
| 1.2 | Create UserProfileUpdateModel | Input-only model for safe updates | ✅ |
| 1.3 | Refactor IUserService | Update signatures to use new ViewModels | ✅ |
| 1.4 | Implement GetProfileWithDetailsAsync | Aggregate ratings and role stats | ✅ |
| 1.5 | Migration of AccountController | Update MyProfile & Profile actions | ✅ |
| 1.6 | Migration of AdminController | Update User management actions | ✅ |
| 1.7 | Migration of BookingController | Update ProfilePoster actions | ✅ |
| 1.8 | Cleanup Obsolete Models | Remove legacy ProfileViewModel.cs | ✅ |

---

## 🛡️ Verification & Auth Refactoring 🔄

### Status: IN PROGRESS

| # | Task | Description | Status |
|---|------|-------------|--------|
| 2.1 | Refactor VerificationService | Standardize return types to Response<T> | ✅ |
| 2.2 | Implement Email Verification Code | Switch from link-based to OTP (backend) | ✅ |
| 2.3 | Phone Verification Service | Refactor SMS sending & code validation | ⏳ |
| 2.4 | ID/KYC Submission Logic | Refactor multi-file upload & review queue | ⏳ |
| 2.5 | Verification Tier Logic | Implement budget limits based on level | ⏳ |

---

## 🖼️ Infrastructure Modernization (Next Focus) ⏳

### Status: PENDING

| # | Task | Description | Status |
|---|------|-------------|--------|
| 3.1 | ImageProcessingService Refactor | Integrate SixLabors.ImageSharp for profil pics | ⏳ |
| 3.2 | StorageService Integration | Use IStorageService instead of manual IO | ⏳ |
| 3.3 | Local vs Cloud Switching | Enable easy swap to Azure Blob Storage | ⏳ |
| 3.4 | File Validation Service | Standardize mime-type & size checks | ⏳ |

---

## 📝 Task & Application Architecture ⏳

### Status: PENDING

| # | Task | Description | Status |
|---|------|-------------|--------|
| 4.1 | TaskDetailsViewModel Cleanup | Break down into smaller DTOs | ⏳ |
| 4.2 | Application Status Workflow | Implement state-machine for apps | ⏳ |
| 4.3 | Search Service Optimization | Improve spatial query performance | ⏳ |

---

## ✅ Definition of "Standardized"

For each refactored service:
- [ ] No direct dependence on `DbContext` (use Repository/UnitOfWork)
- [ ] Use `Response<T>` for all return types
- [ ] Proper logging for all critical operations
- [ ] AutoMapper for all entity-to-ViewModel conversions
- [ ] Comprehensive unit tests for business logic
