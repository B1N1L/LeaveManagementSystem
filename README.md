# Leave Management System
A microservices-based Leave Management System built with .NET 10, demonstrating enterprise-grade patterns including service discovery, distributed tracing, circuit breaker, and async messaging.


### Services
| Service | Port | Responsibility |

| API Gateway | 5000 | Single entry point, JWT validation, request routing |  
| User Service | 5001 | User management, authentication, JWT generation |  
| Leave Service | 5002 | Leave balance, apply, approve/reject, history |  
| Notification Service | 5003 | Async notification consumption and logging |

### Infrastructure
| Component | Port | Purpose |

| PostgreSQL (Users) | 5431 | User Service database |  
| PostgreSQL (Leaves) | 5432 | Leave Service database |  
| PostgreSQL (Notifications) | 5433 | Notification Service database |  
| RabbitMQ | 5672 | Async messaging between services |  
| Consul | 9500 | Service discovery and health checking |  
| Jaeger | 16686 | Distributed tracing |



## Prerequisites
- Docker Desktop
- Git

## Quick Start

### 1. Clone the Repository
```bash
git clone https://github.com/B1N1L/LeaveManagementSystem.git
cd LeaveManagementSystem
```

## Docker Hub Images

| Service | Image |

| User Service | `b1n1l/lms-user-service:latest` |  
| Leave Service | `b1n1l/lms-leave-service:latest` |  
| Notification Service | `b1n1l/lms-notification-service:latest` |  
| API Gateway | `b1n1l/lms-api-gateway:latest` |  

Pull images manually:
```bash
docker pull b1n1l/lms-user-service:latest
docker pull b1n1l/lms-leave-service:latest
docker pull b1n1l/lms-notification-service:latest
docker pull b1n1l/lms-api-gateway:latest
```

### 2. Start All Services
```bash
docker-compose up --build
```

### 3. Verify All Services are Running
| URL | Purpose |

| http://localhost:5000/health | API Gateway |  
| http://localhost:5001/health | User Service |  
| http://localhost:5002/health | Leave Service |  
| http://localhost:5003/health | Notification Service |  
| http://localhost:9500 | Consul UI |  
| http://localhost:15672 | RabbitMQ UI (guest/guest) |  
| http://localhost:16686 | Jaeger UI |

### 4. Import Postman Collection
Import `docs/LeaveManagementSystem.postman_collection.json` into Postman.
Import `docs/LMS_Docker.postman_environment.json` as the environment.
Select `LMS Docker` environment before running requests.

## Running Without Docker (Local Development)

### Prerequisites
- .NET 10 SDK
- Docker Desktop (for infrastructure only)

### Start Infrastructure
```bash
docker run --name userservice-db -e POSTGRES_USER=postgres -e POSTGRES_PASSWORD=postgres -e POSTGRES_DB=userservice_db -p 5431:5432 -d postgres:16
docker run --name leaveservice-db -e POSTGRES_USER=postgres -e POSTGRES_PASSWORD=postgres -e POSTGRES_DB=leaveservice_db -p 5432:5432 -d postgres:16
docker run --name rabbitmq -p 5672:5672 -p 15672:15672 -d rabbitmq:3-management
docker run --name consul -p 9500:8500 -d consul:1.15
docker run --name jaeger -p 16686:16686 -p 4317:4317 -d jaegertracing/all-in-one:latest
```

### Start Services
Open 4 terminals and run each service:
```bash
# Terminal 1
cd src/UserService && dotnet run

# Terminal 2
cd src/LeaveService && dotnet run

# Terminal 3
cd src/NotificationService && dotnet run

# Terminal 4
cd src/ApiGateway && dotnet run
```


## Event Flow

### User Created Event# LeaveManagementSystem
Manager creates user (User Service)
↓
UserCreated event → RabbitMQ (user-created queue)
↓
Leave Service consumes event
↓
Leave balance initialized automatically

### Leave Approved/Rejected Event
Manager approves/rejects (Leave Service)
↓
LeaveApproved/LeaveRejected event → RabbitMQ (leave-notifications queue)
↓
Notification Service consumes event
↓
Notification logged + stored in memory

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

## Future Enhancements
- **Centralized Logging:** ELK Stack (Elasticsearch, Logstash, Kibana) 
  integration for log aggregation across all services.
- **Persistent Notifications:** Replace in-memory notification store with 
  a PostgreSQL database for notification history persistence across restarts.
- **Refresh Tokens:** Add refresh token support to extend JWT sessions 
  without re-login.
- **Leave Carry Forward:** Support carrying unused leave days to next year.

## Video Recording Link

https://drive.google.com/file/d/1otUUS2z57tRHyaLVF6omnDL0FxAlIdnQ/view?usp=sharing

## Source Code Link

https://github.com/B1N1L/LeaveManagementSystem.git