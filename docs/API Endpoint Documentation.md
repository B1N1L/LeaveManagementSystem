# API Endpoint Documentation
## Leave Management System

All requests go through the API Gateway at `http://localhost:5000`.
Protected endpoints require `Authorization: Bearer {token}` header.

---

## API Endpoints

### Authentication (via Gateway)
| Method | Endpoint | Auth | Description |

| POST | /api/auth/login | None | Login and get JWT token |  
| GET | /api/auth/users/{id} | Bearer | Get user by ID |  
| POST | /api/auth/users | Bearer (Manager) | Create new user |  
| GET | /api/auth/users/{managerId}/employees | Bearer (Manager) | Get team members |  

### Leave Management (via Gateway)
| Method | Endpoint | Auth | Description |

| GET | /api/leave/balance | Bearer (Employee/Manager) | Get own leave balance |  
| POST | /api/leave/apply | Bearer (Employee) | Apply for leave |  
| GET | /api/leave/history | Bearer (Employee) | View leave history |  
| PATCH | /api/leave/{id}/cancel | Bearer (Employee) | Cancel pending leave |  
| GET | /api/leave/team | Bearer (Manager) | View team leave requests |  
| PATCH | /api/leave/{id}/action | Bearer (Manager) | Approve or reject leave |  
| GET | /api/leave/team/balance/{employeeId} | Bearer (Manager) | View employee balance |  

### Notifications (via Gateway)
| Method | Endpoint | Auth | Description |

| GET | /api/notification | Bearer | Get notifications |  
| GET | /api/notification/employee/{id} | Bearer (Manager) | Get employee notifications |  

## Authentication

### POST /api/auth/login
Login and receive JWT token. No authentication required.

**Request:**
```json
{
    "email": "manas@company.com",
    "password": "NAGP2026"
}
```

**Response 200 OK:**
```json
{
    "token": "eyJhbGciOiJIUzI1NiIsInR5cCI6IkpXVCJ9...",
    "fullName": "Manas",
    "role": "Manager",
    "userId": 1
}
```

**Response 401 Unauthorized:**
```json
{
    "message": "Invalid email or password."
}
```

**Response 400 Bad Request:**
```json
{
    "message": "Email and password are required."
}
```

---

### POST /api/auth/users
Create a new user. Manager only.

**Request:**
```json
{
    "fullName": "Bhavesh",
    "email": "bhavesh@company.com",
    "password": "NAGP2026",
    "role": "Employee",
    "managerId": 1
}
```

**Response 201 Created:**
```json
{
    "id": 4,
    "fullName": "Bhavesh",
    "email": "bhavesh@company.com",
    "role": "Employee",
    "managerId": 1,
    "createdAt": "2026-05-31T10:00:00Z"
}
```

**Response 400 Bad Request:**
```json
{
    "message": "A user with this email already exists."
}
```

**Response 403 Forbidden:**
```json
{}
```

---

### GET /api/auth/users/{id}
Get user by ID.

**Response 200 OK:**
```json
{
    "id": 2,
    "fullName": "Binil",
    "email": "binil@company.com",
    "role": "Employee",
    "managerId": 1
}
```

**Response 404 Not Found:**
```json
{
    "message": "User not found."
}
```

---

### GET /api/auth/users/{managerId}/employees
Get all employees under a manager. Manager only.

**Response 200 OK:**
```json
[
    {
        "id": 2,
        "fullName": "Binil",
        "email": "binil@company.com",
        "role": "Employee",
        "managerId": 1
    },
    {
        "id": 3,
        "fullName": "Rohit",
        "email": "rohit@company.com",
        "role": "Employee",
        "managerId": 1
    }
]
```

---

## Leave Management

### GET /api/leave/balance
Get own leave balance. Employee and Manager.

**Response 200 OK:**
```json
{
    "employeeId": 2,
    "year": 2026,
    "totalSickLeaves": 10,
    "usedSickLeaves": 0,
    "remainingSickLeaves": 10,
    "totalCasualLeaves": 12,
    "usedCasualLeaves": 3,
    "remainingCasualLeaves": 9,
    "totalPrivilegeLeaves": 15,
    "usedPrivilegeLeaves": 0,
    "remainingPrivilegeLeaves": 15
}
```

---

### POST /api/leave/apply
Apply for leave. Employee only.

**Request:**
```json
{
    "leaveType": "Casual",
    "startDate": "2026-07-01",
    "endDate": "2026-07-03",
    "reason": "Family function"
}
```

**Response 201 Created:**
```json
{
    "id": 1,
    "employeeId": 2,
    "managerId": 1,
    "leaveType": "Casual",
    "startDate": "2026-07-01T00:00:00Z",
    "endDate": "2026-07-03T00:00:00Z",
    "totalDays": 3,
    "status": "Pending",
    "reason": "Family function",
    "rejectionReason": null,
    "appliedOn": "2026-05-31T10:00:00Z",
    "actedOn": null
}
```

**Response 400 Bad Request — Insufficient Balance:**
```json
{
    "message": "Insufficient Casual leave balance. Available: 2 days, Requested: 3 days."
}
```

**Response 400 Bad Request — Overlap:**
```json
{
    "message": "You already have a leave request overlapping these dates."
}
```

**Response 400 Bad Request — Past Date:**
```json
{
    "message": "Start date cannot be in the past."
}
```

**Response 400 Bad Request — Invalid Type:**
```json
{
    "message": "Invalid leave type. Valid types: Sick, Casual, Privilege"
}
```

---

### GET /api/leave/history
View own leave history. Employee only.
Supports query params: `?status=Pending&page=1&pageSize=10`

**Response 200 OK:**
```json
{
    "items": [
        {
            "id": 1,
            "employeeId": 2,
            "managerId": 1,
            "leaveType": "Casual",
            "startDate": "2026-07-01T00:00:00Z",
            "endDate": "2026-07-03T00:00:00Z",
            "totalDays": 3,
            "status": "Approved",
            "reason": "Family function",
            "rejectionReason": null,
            "appliedOn": "2026-05-31T10:00:00Z",
            "actedOn": "2026-05-31T11:00:00Z"
        }
    ],
    "totalCount": 1,
    "page": 1,
    "pageSize": 10,
    "totalPages": 1
}
```

---

### PATCH /api/leave/{id}/cancel
Cancel a pending leave. Employee only.

**Response 200 OK:**
```json
{
    "message": "Leave request cancelled successfully."
}
```

**Response 400 Bad Request:**
```json
{
    "message": "Only pending leave requests can be cancelled. Current status: Approved"
}
```

---

### GET /api/leave/team
View team leave requests. Manager only.
Supports query params: `?status=Pending&employeeId=2&fromDate=2026-01-01&toDate=2026-12-31`

**Response 200 OK:**
```json
[
    {
        "id": 1,
        "employeeId": 2,
        "managerId": 1,
        "leaveType": "Casual",
        "startDate": "2026-07-01T00:00:00Z",
        "endDate": "2026-07-03T00:00:00Z",
        "totalDays": 3,
        "status": "Pending",
        "reason": "Family function",
        "rejectionReason": null,
        "appliedOn": "2026-05-31T10:00:00Z",
        "actedOn": null
    }
]
```

---

### PATCH /api/leave/{id}/action
Approve or reject a leave request. Manager only.

**Request — Approve:**
```json
{
    "action": "Approve"
}
```

**Request — Reject:**
```json
{
    "action": "Reject",
    "rejectionReason": "Team is short staffed during this period"
}
```

**Response 200 OK:**
```json
{
    "message": "Leave request Approved successfully."
}
```

**Response 400 Bad Request — Missing Rejection Reason:**
```json
{
    "message": "Rejection reason is required."
}
```

**Response 400 Bad Request — Already Acted:**
```json
{
    "message": "Cannot act on a leave request with status 'Approved'."
}
```

---

### GET /api/leave/team/balance/{employeeId}
View a team member's leave balance. Manager only.

**Response 200 OK:** Same structure as GET /api/leave/balance

---

## Notifications

### GET /api/notification
Get notifications.
Managers see all notifications. Employees see only their own.
Supports query param: `?eventType=LeaveApproved`

**Response 200 OK:**
```json
{
    "count": 1,
    "notifications": [
        {
            "id": "3fa85f64-5717-4562-b3fc-2c963f66afa6",
            "eventType": "LeaveApproved",
            "employeeId": 2,
            "employeeName": "Binil",
            "leaveType": "Casual",
            "startDate": "2026-07-01T00:00:00Z",
            "endDate": "2026-07-03T00:00:00Z",
            "totalDays": 3,
            "rejectionReason": null,
            "actedOn": "2026-05-31T11:00:00Z",
            "receivedAt": "2026-05-31T11:00:01Z"
        }
    ]
}
```

---

### GET /api/notification/employee/{employeeId}
Get notifications for a specific employee. Manager only.

**Response 200 OK:** Same structure as GET /api/notification

**Response 403 Forbidden:** Employee trying to access this endpoint

---

## Health Checks

### GET /health
Available on all services. No authentication required.

**Response 200 OK:**
```json
{
    "status": "Healthy",
    "checks": [
        {
            "name": "postgresql",
            "status": "Healthy",
            "description": null
        }
    ]
}
```

---

## HTTP Status Code Reference

| Code | Meaning | When |

| 200 | OK | Successful GET, PATCH |  
| 201 | Created | Successful POST (user/leave created) |  
| 400 | Bad Request | Validation failure, business rule violation |  
| 401 | Unauthorized | Missing or invalid JWT token |  
| 403 | Forbidden | Valid token but wrong role |  
| 404 | Not Found | Resource does not exist |  
| 500 | Internal Server Error | Unexpected server error |  
| 502 | Bad Gateway | Gateway cannot reach downstream service |  
| 503 | Service Unavailable | Circuit breaker open |
