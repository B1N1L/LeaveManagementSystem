# Inter-Service Communication
## Leave Management System

---

## 1. Communication Overview
API Gateway ──HTTP──► User Service
API Gateway ──HTTP──► Leave Service
API Gateway ──HTTP──► Notification Service
Leave Service ──HTTP──► User Service
User Service ──RabbitMQ──► Leave Service
Leave Service ──RabbitMQ──► Notification Service

---

## 2. Synchronous Communication (HTTP)

### 2.1 API Gateway → All Services
**Protocol:** HTTP  
**Discovery:** Consul (Gateway queries Consul for service addresses)  
**Pattern:** Request/Response  
**Resilience:** Circuit Breaker via Ocelot QoS
- Opens after 3 consecutive failures
- Stays open for 30 seconds
- Timeout per request: 10 seconds

### 2.2 Leave Service → User Service
**Protocol:** HTTP  
**When:** Two scenarios:
1. Employee applies for leave — Leave Service calls User Service to get employee's ManagerId
2. Manager approves/rejects — Leave Service calls User Service to get employee's name for notification

**Endpoint called:** `GET /api/auth/users/{userId}`  
**Resilience:** Custom Circuit Breaker (`Shared.Resilience.CircuitBreaker`)
- Opens after 3 consecutive failures
- Stays open for 30 seconds
- On open: returns null — operation fails with a clear error message to client
- Logs all state transitions (failure count, open, half-open, closed)

**What happens if User Service is down:**
- Apply leave fails gracefully with message "Employee not found"
- Circuit opens after 3 attempts to prevent flooding
- After 30 seconds circuit half-opens and retries automatically

---

## 3. Asynchronous Communication (RabbitMQ)

### 3.1 User Service → Leave Service (user-created queue)

**Trigger:** Manager creates a new user  
**Publisher:** User Service (`UserCreatedPublisher`)  
**Consumer:** Leave Service (`UserCreatedConsumer`)  
**Queue:** `user-created` (durable, persistent messages)

**Message Shape:**
```json
{
    "userId": 4,
    "fullName": "Bhavesh",
    "email": "bhavesh@company.com",
    "role": "Employee"
}
```

**Consumer Action:** Initializes leave balance for the new user
- Sick Leave: 10 days
- Casual Leave: 12 days
- Privilege Leave: 15 days

**Failure Handling:**
- On processing failure → BasicNack → message requeued
- If Leave Service is down → messages queue up in RabbitMQ
- When Leave Service restarts → processes all queued messages automatically

---

### 3.2 Leave Service → Notification Service (leave-notifications queue)

**Trigger:** Manager approves or rejects a leave request  
**Publisher:** Leave Service (`RabbitMQPublisher`)  
**Consumer:** Notification Service (`LeaveNotificationConsumer`)  
**Queue:** `leave-notifications` (durable, persistent messages)

**Message Shape:**
```json
{
    "eventType": "LeaveApproved",
    "employeeId": 2,
    "employeeName": "Binil",
    "leaveType": "Casual",
    "startDate": "2026-07-01T00:00:00Z",
    "endDate": "2026-07-03T00:00:00Z",
    "totalDays": 3,
    "rejectionReason": null,
    "actedOn": "2026-05-31T11:00:00Z"
}
```

**Consumer Action:** Logs notification + stores in memory

**Failure Handling:**
- On processing failure → BasicNack → message requeued
- Messages are persistent (survive RabbitMQ restart)
- Manual acknowledgement — messages not lost if service crashes mid-processing

---

## 4. Service Discovery (Consul)

All services register with Consul on startup:

| Service | Registration Name | Health Check URL |
|---|---|---|
| User Service | user-service | http://user-service:5001/health |
| Leave Service | leave-service | http://leave-service:5002/health |
| Notification Service | notification-service | http://notification-service:5003/health |
| API Gateway | api-gateway | http://api-gateway:5000/health |

**Registration timing:** Services register AFTER Kestrel is fully started
(using `IHostedLifecycleService.StartedAsync`) to prevent health check failures
on initial registration.

**Health check interval:** Every 10 seconds  
**Deregister after:** 2 minutes of failed health checks

---

## 5. Assumptions

1. **Reporting Manager auto-fetched:** When an employee applies for leave, the system automatically fetches their manager from the User Service instead of asking the employee to specify. This prevents employees from submitting leaves to the wrong manager.

2. **Leave days auto-calculated:** The number of leave days is calculated by the system (excluding weekends) rather than being specified by the employee. This prevents errors and ensures consistency.

3. **Notification is log-based:** No email or SMS integration. Notifications are logged by the Notification Service and stored in memory.

4. **Leave balance per calendar year:** Each employee gets a fresh balance at the start of each year. The system does not carry over unused leave days.

5. **In-memory notifications:** Notification Service stores notifications in a ConcurrentQueue (max 200). Data resets on service restart. A persistent store would be added in a production system.

6. **Working days only:** Leave calculations exclude Saturdays and Sundays.

7. **JWT secret shared:** All services use the same JWT secret so any service can validate tokens issued by User Service without calling back to User Service.

8. **Separate DB per service:** Each microservice owns its own PostgreSQL instance. No shared database.