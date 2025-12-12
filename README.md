# Clbio - Project Management Backend API

Clbio is a modern project management tool designed to help teams manage projects, boards, and tasks efficiently, similar to Jira. This repository contains the RESTful API backend services developed using **.NET 9**, following **Clean Architecture** principles.

## About the Project

This project is designed with scalability, performance, and maintainability in mind. Users can create workspaces, add Kanban boards, manage tasks using drag-and-drop logic, and interact with team members in real-time.

### Key Features

* **Advanced Authentication:**
    * Secure session management based on JWT (JSON Web Token).
    * Refresh Token mechanism for seamless user experience.
    * Google Sign-In (OAuth2) integration.
    * Email verification and secure password reset flows.
* **Workspace & Permission Management (RBAC):**
    * Global and Workspace-based role management (Owner, PrivilegedMember, Member).
    * Granular permission controls via Attribute-based authorization.
* **Kanban Management:**
    * Hierarchy: Workspace -> Board -> Column -> Task.
    * Task movement and reordering capabilities.
* **Real-Time Interaction:**
    * Instant updates using SignalR and Redis Backplane (Task assignments, comments, etc., are reflected immediately).
    * Online user tracking (Presence system).
* **Security & Logging:**
    * IP-based Rate Limiting.
    * Security Headers (Helmet-like protection).
    * Soft Delete at the database level.
    * Audit Logging for user activities.

## Tech Stack

* **Platform:** .NET 9.0
* **Architecture:** Clean Architecture (Onion)
* **Database:** PostgreSQL (Entity Framework Core 9)
* **Caching & Pub/Sub:** Redis (StackExchange.Redis)
* **Real-Time Communication:** SignalR
* **Containerization:** Docker & Docker Compose
* **Testing:** xUnit, Moq, FluentAssertions
* **Documentation:** Swagger / OpenAPI

## Architecture Structure

The project consists of 5 main layers where dependencies flow from the inside out:

1.  **Clbio.Domain:** Contains Entities, Enums, and core business rules. It has no external dependencies.
2.  **Clbio.Abstractions:** Interfaces and abstractions are defined here. It determines contracts between layers.
3.  **Clbio.Application:** The location where business logic is processed. Services, DTOs, Validations, and AutoMapper profiles are located here.
4.  **Clbio.Infrastructure:** Contains concrete implementations that communicate with the outside world, such as Database access, Redis connection, and Email service.
5.  **Clbio.API:** The gateway to the outside world. It includes Controllers, Middleware, and IoC (Dependency Injection) configuration.

## Installation and Execution

You can follow the steps below to run the project in your local environment.

### Prerequisites
* Docker Desktop (Recommended)
* .NET 9.0 SDK (If Docker is not used)
* PostgreSQL and Redis (If Docker is not used)

### Step 1: Clone the Repository
```bash
git clone [https://github.com/yourusername/clbio-backend.git](https://github.com/yourusername/clbio-backend.git)
cd clbio-backend
```
### Step 2: Set Environment Variables

Rename the .env.example file to .env and fill in the necessary fields (JWT Secret, DB password, etc.).
```bash
cp .env.example .env
```
### Step 3: Run with Docker (Easiest Method)
Run the following command in the project root directory. This command will stand up the API, PostgreSQL, Redis, and pgAdmin containers.
```bash
docker compose up -d --build
```
The API will run at: `http://localhost:8080` 
Swagger Documentation: `http://localhost:8080/swagger`

### Step 4: Database Migrations

In the Development environment, migrations are applied automatically when the application starts `(DatabaseMigrator.cs)`. If you wish to trigger them manually:
```bash
dotnet ef database update --project Clbio.Infrastructure --startup-project Clbio.API
```

### Tests

The project includes comprehensive Unit Test and Integration Test structures. To run the tests:
```bash
dotnet test
```

### Security Measures

**Rate Limiting**: Request limiting based on ID for authenticated users and IP for anonymous users.

**Soft Delete**: Data is marked with an `IsDeleted` flag and is not physically deleted. Deleted data is automatically filtered out via Global Query Filter.

**Secure Encryption**: Passwords are hashed using the `PBKDF2` algorithm with salting.
