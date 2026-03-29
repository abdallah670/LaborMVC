# Labor Marketplace System

A comprehensive **Labor Marketplace Platform** built with ASP.NET Core MVC that connects workers with job posters. This platform enables users to post tasks, apply for work, manage bookings, process secure payments through Stripe, and communicate in real-time.

## 🏗️ Architecture

The project follows a **3-Layer Architecture** with clear separation of concerns:

```
┌─────────────────────────────────────────────────────────────┐
│                    LaborPL (Presentation Layer)              │
│  - Controllers • Views • Middleware • Health Checks         │
└────────────────────────┬────────────────────────────────────┘
                         │
┌────────────────────────▼────────────────────────────────────┐
│                  LaborBLL (Business Logic Layer)             │
│  - Services • ViewModels • Hubs • Validators • Jobs         │
└────────────────────────┬────────────────────────────────────┘
                         │
┌────────────────────────▼────────────────────────────────────┐
│                   LaborDAL (Data Access Layer)               │
│  - Entities • Repositories • DbContext • Migrations         │
└─────────────────────────────────────────────────────────────┘
```

## ✨ Features

### 👤 User Management
- **Multi-Role System**: Admin, Worker, and Poster roles with flexible combinations
- **Identity Authentication**: ASP.NET Core Identity with secure password policies
- **Email Verification**: Code-based and link-based email verification
- **Phone Verification**: SMS verification using Twilio
- **ID Verification**: KYC document upload and admin approval workflow
- **Profile Management**: Profile pictures, bio, skills, location with GPS coordinates

### 📋 Task Management
- **Post Tasks**: Create detailed task listings with descriptions, budgets, and requirements
- **Task Categories**: Organized by categories (Plumbing, Electrical, Cleaning, etc.)
- **Search & Filter**: Find tasks by location, category, budget type, and keywords
- **Geographic Search**: SQL Server spatial queries using NetTopologySuite
- **Task Status Tracking**: Open → Assigned → In Progress → Completed → Cancelled

### 📝 Application System
- **Apply for Tasks**: Workers can submit applications with proposed budget and estimated hours
- **Application Management**: Posters can accept or reject applications
- **Bulk Applications**: Workers can apply to multiple tasks

### 📅 Booking System
- **Schedule Bookings**: Fixed time slots with start/end times
- **Status Workflow**: Scheduled → In Progress → Completed (Worker) → Confirmed (Poster)
- **Cancellation System**: Time-window based cancellations with penalty calculation
- **No-Show Detection**: Automated detection of missed appointments

### 💳 Payment Processing
- **Stripe Integration**: Secure payment processing with Stripe
- **Escrow-Style Holding**: Funds held until job completion
- **Stripe Connect**: Worker onboarding for receiving payouts
- **Payment States**: Pending → Held → Released/Refunded
- **Audit Logging**: Complete payment transaction history

### 💬 Real-Time Communication
- **SignalR Hubs**: Real-time messaging between users
- **Chat System**: Direct messaging tied to bookings
- **Typing Indicators**: Real-time user status
- **Unread Counts**: Message notification badges

### ⭐ Rating & Reviews
- **Two-Way Ratings**: Both workers and posters can rate each other
- **Review Comments**: Detailed feedback with comments
- **Average Ratings**: User profiles display average ratings

### ⚖️ Dispute Resolution
- **Dispute Filing**: Users can raise disputes for bookings
- **Admin Dashboard**: Admins can review and resolve disputes
- **Resolution Types**: Full refund, partial refund, or no action

### 🔒 Security Features
- **File Upload Security**: Malware scanning, signature validation, executable blocking
- **Rate Limiting**: Endpoint-specific rate limits (Login, Payment, General)
- **Security Headers**: XSS protection, CSP, HSTS
- **Audit Logging**: Track all critical operations
- **Soft Deletes**: Data retention with soft delete pattern
- **Input Validation**: FluentValidation for all inputs

### 🔄 Background Processing
- **Hangfire Jobs**: Background job processing
- **Recurring Jobs**: No-show detection, payment release
- **Outbox Pattern**: Reliable message processing
- **Saga Pattern**: Distributed transaction orchestration

## 🛠️ Technology Stack

| Category | Technologies |
|----------|-------------|
| **Framework** | .NET 9, ASP.NET Core MVC |
| **Database** | SQL Server 2022, Entity Framework Core 9 |
| **Spatial Data** | NetTopologySuite, SQL Server Spatial |
| **Authentication** | ASP.NET Core Identity |
| **Real-Time** | SignalR |
| **Payments** | Stripe (Stripe.net SDK), Stripe Connect |
| **SMS** | Twilio |
| **Email** | SMTP, SendGrid |
| **Background Jobs** | Hangfire |
| **Mapping** | AutoMapper |
| **Validation** | FluentValidation |
| **Logging** | Serilog |
| **Resilience** | Polly |
| **Image Processing** | SixLabors.ImageSharp |

## 📋 Prerequisites

- [.NET 9 SDK](https://dotnet.microsoft.com/download/dotnet/9.0)
- [SQL Server 2022](https://www.microsoft.com/en-us/sql-server/sql-server-downloads) (or SQL Server Express)
- [Visual Studio 2022](https://visualstudio.microsoft.com/) (recommended) or VS Code

### External Service Accounts (Optional but Recommended)

- **Stripe Account**: For payment processing ([Sign up](https://stripe.com))
- **Twilio Account**: For SMS verification ([Sign up](https://twilio.com))
- **SendGrid Account**: For email delivery ([Sign up](https://sendgrid.com))

## 🚀 Installation & Setup

### 1. Clone the Repository

```bash
git clone https://github.com/abdallah670/LaborMVC.git
cd LaborMVC
```

### 2. Configure Database Connection

Update `LaborPL/appsettings.json`:

```json
{
  "ConnectionStrings": {
    "DefaultConnection": "Server=.;Database=LaborMVC;Integrated Security=True;TrustServerCertificate=True"
  }
}
```

### 3. Configure External Services (Optional)

#### Stripe Configuration
```json
{
  "Stripe": {
    "SecretKey": "sk_test_your_secret_key",
    "PublishableKey": "pk_test_your_publishable_key",
    "WebhookSecret": "whsec_your_webhook_secret"
  }
}
```

#### Twilio Configuration
```json
{
  "Twilio": {
    "AccountSid": "your_account_sid",
    "AuthToken": "your_auth_token",
    "PhoneNumber": "your_twilio_phone_number"
  }
}
```

#### Email Configuration (SMTP)
```json
{
  "EmailSettings": {
    "SmtpHost": "smtp.gmail.com",
    "SmtpPort": 587,
    "SmtpUser": "your-email@gmail.com",
    "SmtpPass": "your-app-password",
    "FromEmail": "your-email@gmail.com",
    "FromName": "Labor Marketplace"
  }
}
```

### 4. Stripe Webhook Testing (Local Development)

To test Stripe webhooks locally (for payment confirmations, refunds, etc.), you need to use the Stripe CLI to forward events to your local server.

#### 4.1 Download Stripe CLI

Download the Stripe CLI from: https://stripe.com/docs/stripe-cli

Or using package managers:
```bash
# Windows (via scoop)
scoop install stripe

# macOS (via Homebrew)
brew install stripe/stripe-cli/stripe

# Linux
# Download from https://github.com/stripe/stripe-cli/releases/latest
```

The repository also includes a `stripe.exe` file which is the Stripe CLI binary for Windows.

#### 4.2 Login to Stripe CLI

```bash
stripe login
```

This will open a browser to authenticate with your Stripe account.

#### 4.3 Forward Webhooks to Local Server

With your application running, open a new terminal and run:

```bash
stripe listen --forward-to https://localhost:7001/api/stripe/webhook
```

Or if using HTTP:
```bash
stripe listen --forward-to http://localhost:7000/api/stripe/webhook
```

This will output a webhook signing secret (e.g., `whsec_xxxxxxxx`). Copy this and add it to your `appsettings.Development.json`:

```json
{
  "Stripe": {
    "SecretKey": "sk_test_...",
    "PublishableKey": "pk_test_...",
    "WebhookSecret": "whsec_xxxxxxxx"  // ← Copy from stripe listen output
  }
}
```

#### 4.4 Trigger Test Events (Optional)

You can trigger test events manually to simulate Stripe events:

```bash
# Trigger a payment intent succeeded event
stripe trigger payment_intent.succeeded

# Trigger a charge refunded event
stripe trigger charge.refunded

# Trigger a payout paid event
stripe trigger payout.paid
```

**Note**: Keep the `stripe listen` command running in a separate terminal while testing payments in the application.

### 5. Run Database Migrations

```bash
cd LaborPL
dotnet ef database update --project ../LaborDAL
```

Or using Package Manager Console in Visual Studio:
```powershell
Update-Database -Project LaborDAL -StartupProject LaborPL
```

### 6. Run the Application

```bash
cd LaborPL
dotnet run
```

The application will be available at:
- **HTTP**: http://localhost:7000
- **HTTPS**: https://localhost:7001

### 7. Access Hangfire Dashboard

Navigate to `/hangfire` to monitor background jobs.

## 📁 Project Structure

```
LaborMVC/
├── LaborPL/                          # Presentation Layer
│   ├── Controllers/                  # MVC Controllers
│   │   ├── AccountController.cs      # Auth & profile
│   │   ├── AdminController.cs        # Admin operations
│   │   ├── TaskController.cs         # Task management
│   │   ├── BookingController.cs      # Booking workflow
│   │   ├── ApplicationController.cs  # Task applications
│   │   ├── PaymentController.cs      # Payment processing
│   │   ├── ChatController.cs         # Messaging
│   │   └── StripeWebhookController.cs # Stripe webhooks
│   ├── Views/                        # Razor views
│   ├── Middleware/                   # Custom middleware
│   ├── HealthChecks/                 # Health check implementations
│   └── wwwroot/                      # Static files
│
├── LaborBLL/                         # Business Logic Layer
│   ├── Service/                      # Business services
│   │   ├── Implementation/           # Service implementations
│   │   │   ├── StripeService.cs
│   │   │   ├── UserService.cs
│   │   │   ├── TaskService.cs
│   │   │   ├── BookingService.cs
│   │   │   ├── PaymentService.cs
│   │   │   ├── VerificationService.cs
│   │   │   ├── SagaOrchestrator.cs
│   │   │   └── ...
│   │   └── Abstract/                 # Service interfaces
│   ├── ModelVM/                      # ViewModels
│   ├── Hubs/                         # SignalR hubs
│   │   ├── ChatHub.cs
│   │   └── DirectCatHub.cs
│   ├── Common/                       # Utilities & settings
│   └── Mapper/                       # AutoMapper profiles
│
└── LaborDAL/                         # Data Access Layer
    ├── Entities/                     # Domain entities
    │   ├── AppUser.cs
    │   ├── TaskItem.cs
    │   ├── Booking.cs
    │   ├── Payment.cs
    │   └── ...
    ├── DB/                           # Database context
    │   ├── ApplicationDbContext.cs
    │   └── DbInitializer.cs
    ├── Repo/                         # Repository pattern
    │   ├── Abstract/                 # Repository interfaces
    │   └── Implementation/           # Repository implementations
    ├── Enums/                        # Enumerations
    └── Migrations/                   # EF Core migrations
```

## 📊 Database Schema

### Core Entities

| Entity | Description |
|--------|-------------|
| **AppUser** | Extended Identity user with profile, verification, and Stripe account info |
| **TaskItem** | Task/job postings with location, budget, and status |
| **TaskApplication** | Worker applications for tasks |
| **Booking** | Scheduled work sessions with payment association |
| **Payment** | Payment records with Stripe integration |
| **Dispute** | Dispute cases for resolution |
| **Rating** | User ratings and reviews |
| **IDVerification** | KYC document submissions |
| **ChatUsers/Conversation** | Messaging system entities |

## 🔌 API Endpoints

### Account
- `GET/POST /Account/Register` - User registration
- `GET/POST /Account/Login` - User login
- `GET /Account/Logout` - Logout
- `GET /Account/MyProfile` - View/edit profile
- `POST /Account/VerifyEmailCode` - Email verification
- `POST /Account/SubmitIdVerification` - ID document submission
- `GET /Account/ConnectStripe` - Stripe Connect onboarding

### Tasks
- `GET /Task/Index` - Browse all tasks
- `GET /Task/Search` - Search tasks
- `GET /Task/Details/{id}` - Task details
- `GET/POST /Task/Create` - Create new task
- `GET/POST /Task/Edit/{id}` - Edit task
- `GET /Task/MyTasks` - My posted tasks
- `GET /Task/ByCategory/{category}` - Tasks by category

### Bookings
- `GET/POST /Booking/Create` - Create booking
- `GET /Booking/Dashboard` - Booking dashboard
- `GET /Booking/Details/{id}` - Booking details
- `POST /Booking/Cancel/{id}` - Cancel booking
- `POST /Booking/Complete/{id}` - Mark complete
- `POST /Booking/RaiseDispute/{bookingId}` - File dispute

### Applications
- `POST /Application/Create` - Apply for task
- `GET /Application/ByTask/{taskId}` - View applications
- `POST /Application/Accept/{id}` - Accept application
- `POST /Application/Reject/{id}` - Reject application

### Payments
- `GET /Payment/Checkout/{bookingId}` - Payment page
- `POST /Payment/ConfirmPayment/{bookingId}` - Confirm payment
- `POST /Payment/ReleasePayment/{bookingId}` - Release to worker
- `GET /Payment/MyPaymentHistory` - Payment history

### Admin
- `GET /Admin/Index` - Admin dashboard
- `GET /Admin/Users` - User management
- `GET /Admin/IdVerifications` - ID verification queue
- `GET /Admin/Disputes` - Dispute management
- `POST /Admin/ResolveDispute` - Resolve dispute

## 🔐 Security Considerations

### File Upload Security
- Maximum file size: 10MB
- Allowed extensions: jpg, jpeg, png, gif, pdf, doc, docx, txt, zip
- File signature validation
- Malicious content scanning
- Executable file blocking
- Rate limiting per user

### Rate Limits
- **General API**: 100 requests per minute
- **Login**: 5 attempts per 5 minutes
- **Payment**: 10 requests per minute

### Password Policy
- Minimum length: 4 characters
- Requires digit
- Lockout after 5 failed attempts (30 minutes)

## 🧪 Running Tests

```bash
# Build the solution
dotnet build LaborMVC.sln

# Run the application
dotnet run --project LaborPL
```

## 📝 Logging

The application uses **Serilog** for structured logging:

- Console output for development
- File logs: `logs/log-YYYYMMDD.txt`
- Log levels configurable in `appsettings.json`

## 🔄 Background Jobs

Configured recurring jobs:

| Job | Schedule | Description |
|-----|----------|-------------|
| **No-Show Detection** | Every 5 minutes | Detect missed appointments |
| **Payment Release** | Configurable | Auto-release held payments |
| **Outbox Processor** | Continuous | Process pending messages |

Access the Hangfire dashboard at `/hangfire` to monitor jobs.

## 🤝 Contributing

1. Fork the repository
2. Create a feature branch (`git checkout -b feature/amazing-feature`)
3. Commit your changes (`git commit -m 'Add amazing feature'`)
4. Push to the branch (`git push origin feature/amazing-feature`)
5. Open a Pull Request

## 📄 License

This project is licensed under the MIT License - see the [LICENSE](LICENSE) file for details.

## 👥 Authors

- **Abdallah** - Initial work - [abdallah670](https://github.com/abdallah670)
- **Ezzat**    - [Ezzatkarem](https://github.com/Ezzatkarem)

## 🙏 Acknowledgments

- [ASP.NET Core](https://docs.microsoft.com/en-us/aspnet/core/)
- [Entity Framework Core](https://docs.microsoft.com/en-us/ef/core/)
- [Stripe](https://stripe.com/docs)
- [Hangfire](https://www.hangfire.io/)
- [SignalR](https://docs.microsoft.com/en-us/aspnet/core/signalr/)

---

**Note**: This is a development/test environment setup. For production deployment, ensure you:
- Use strong passwords and secrets
- Enable HTTPS
- Configure proper CORS policies
- Set up proper logging and monitoring
- Use environment variables for sensitive configuration
- Configure database connection pooling
